using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;
using TraceQ.Core.Utilities;
using TraceQ.Infrastructure.Data;

namespace TraceQ.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly ICsvParser _csvParser;
    private readonly IRequirementRepository _requirementRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<ImportService> _logger;
    private readonly TraceQDbContext _context;

    public ImportService(
        ICsvParser csvParser,
        IRequirementRepository requirementRepository,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<ImportService> logger,
        TraceQDbContext context)
    {
        _csvParser = csvParser;
        _requirementRepository = requirementRepository;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
        _context = context;
    }

    public async Task<ImportResultDto> ImportAsync(Stream csvStream, string fileName)
    {
        _logger.LogInformation("Starting import of file: {FileName}", fileName);

        // Parse CSV
        var parseResults = await _csvParser.ParseAsync(csvStream);

        // Create import batch
        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ImportedAt = DateTime.UtcNow,
            InsertedCount = 0,
            UpdatedCount = 0,
            ErrorCount = 0,
            SkippedCount = 0
        };

        _context.ImportBatches.Add(batch);
        await _context.SaveChangesAsync();

        var importRecords = new List<ImportRecord>();

        foreach (var result in parseResults)
        {
            var record = new ImportRecord
            {
                Id = Guid.NewGuid(),
                ImportBatchId = batch.Id,
                RequirementNumber = result.RequirementNumber
            };

            if (!result.Success)
            {
                record.Status = "Error";
                record.ErrorMessage = result.ErrorMessage;
                batch.ErrorCount++;
                importRecords.Add(record);
                continue;
            }

            try
            {
                var requirement = result.Requirement!;
                requirement.TracedTo = TraceLinkParser.Normalize(requirement.TracedTo);
                var existing = await _requirementRepository.GetByNumberAsync(requirement.RequirementNumber);

                if (existing != null)
                {
                    // Check if any field changed
                    if (HasChanges(existing, requirement))
                    {
                        // Update existing
                        existing.Name = requirement.Name;
                        existing.Description = requirement.Description;
                        existing.Type = requirement.Type;
                        existing.State = requirement.State;
                        existing.Priority = requirement.Priority;
                        existing.Owner = requirement.Owner;
                        existing.CreatedDate = requirement.CreatedDate;
                        existing.ModifiedDate = requirement.ModifiedDate;
                        existing.Module = requirement.Module;
                        existing.ParentNumber = requirement.ParentNumber;
                        existing.TracedTo = requirement.TracedTo;
                        existing.IsEmbedded = false; // Mark for re-embedding
                        existing.ImportedAt = DateTime.UtcNow;
                        existing.ImportBatchId = batch.Id;

                        await _requirementRepository.UpsertAsync(existing);
                        record.Status = "Updated";
                        batch.UpdatedCount++;
                    }
                    else
                    {
                        // No changes — skip
                        record.Status = "Skipped";
                        batch.SkippedCount++;
                    }
                }
                else
                {
                    // New requirement — insert
                    requirement.Id = Guid.NewGuid();
                    requirement.ImportBatchId = batch.Id;
                    requirement.ImportedAt = DateTime.UtcNow;
                    requirement.IsEmbedded = false;

                    await _requirementRepository.UpsertAsync(requirement);
                    record.Status = "Inserted";
                    batch.InsertedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing requirement {Number}", result.RequirementNumber);
                record.Status = "Error";
                record.ErrorMessage = ex.Message;
                batch.ErrorCount++;
            }

            importRecords.Add(record);
        }

        // Save import records
        _context.ImportRecords.AddRange(importRecords);

        // Update batch counts
        _context.ImportBatches.Update(batch);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Import complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped, {Errors} errors",
            batch.InsertedCount, batch.UpdatedCount, batch.SkippedCount, batch.ErrorCount);

        // Generate embeddings before returning so imported requirements are
        // immediately searchable when the import succeeds.
        await GenerateEmbeddingsAsync();

        return new ImportResultDto
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            InsertedCount = batch.InsertedCount,
            UpdatedCount = batch.UpdatedCount,
            ErrorCount = batch.ErrorCount,
            SkippedCount = batch.SkippedCount
        };
    }

    public async Task<PaginatedResultDto<ImportBatchDto>> GetHistoryAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.ImportBatches
            .OrderByDescending(b => b.ImportedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new ImportBatchDto
            {
                Id = b.Id,
                FileName = b.FileName,
                ImportedAt = b.ImportedAt,
                InsertedCount = b.InsertedCount,
                UpdatedCount = b.UpdatedCount,
                ErrorCount = b.ErrorCount,
                SkippedCount = b.SkippedCount
            })
            .ToListAsync();

        return new PaginatedResultDto<ImportBatchDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ImportBatchDetailDto?> GetBatchDetailAsync(Guid batchId)
    {
        var batch = await _context.ImportBatches
            .Include(b => b.Records)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
            return null;

        return new ImportBatchDetailDto
        {
            Id = batch.Id,
            FileName = batch.FileName,
            ImportedAt = batch.ImportedAt,
            InsertedCount = batch.InsertedCount,
            UpdatedCount = batch.UpdatedCount,
            ErrorCount = batch.ErrorCount,
            SkippedCount = batch.SkippedCount,
            Records = batch.Records.Select(r => new ImportRecordDto
            {
                RequirementNumber = r.RequirementNumber,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage
            }).ToList()
        };
    }

    private static bool HasChanges(Requirement existing, Requirement incoming)
    {
        return existing.Name != incoming.Name
            || existing.Description != incoming.Description
            || existing.Type != incoming.Type
            || existing.State != incoming.State
            || existing.Priority != incoming.Priority
            || existing.Owner != incoming.Owner
            || existing.CreatedDate != incoming.CreatedDate
            || existing.ModifiedDate != incoming.ModifiedDate
            || existing.Module != incoming.Module
            || existing.ParentNumber != incoming.ParentNumber
            || existing.TracedTo != incoming.TracedTo;
    }

    private async Task GenerateEmbeddingsAsync()
    {
        var unembedded = await _requirementRepository.GetUnembeddedAsync();
        if (!unembedded.Any())
            return;

        var textItems = unembedded.Select(r => (
            id: r.Id.ToString(),
            text: $"{r.RequirementNumber} {r.Name} {r.Description ?? string.Empty}"
        ));

        var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(textItems);

        var points = new List<(Guid id, float[] vector, Dictionary<string, string> payload)>();
        foreach (var req in unembedded)
        {
            if (embeddings.TryGetValue(req.Id.ToString(), out var vector))
            {
                var payload = new Dictionary<string, string>
                {
                    ["requirementNumber"] = req.RequirementNumber,
                    ["name"] = req.Name,
                    ["type"] = req.Type ?? string.Empty,
                    ["state"] = req.State ?? string.Empty,
                    ["module"] = req.Module ?? string.Empty,
                    ["priority"] = req.Priority ?? string.Empty,
                    ["owner"] = req.Owner ?? string.Empty
                };
                points.Add((req.Id, vector, payload));
            }
        }

        if (points.Any())
        {
            await _vectorStore.UpsertBatchAsync(points);
            await _requirementRepository.MarkAsEmbeddedAsync(unembedded.Select(r => r.Id));
        }
    }
}
