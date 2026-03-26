using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;
using TraceQ.Infrastructure.Data;

namespace TraceQ.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly TraceQDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(TraceQDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogImportAsync(string fileName, int insertedCount, int updatedCount, int errorCount)
    {
        var details = JsonSerializer.Serialize(new
        {
            fileName,
            inserted = insertedCount,
            updated = updatedCount,
            errors = errorCount
        });

        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EventType = "Import",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Audit: {EventType} - FileName={FileName}, Inserted={Inserted}, Updated={Updated}, Errors={Errors}",
            entry.EventType, fileName, insertedCount, updatedCount, errorCount);
    }

    public async Task LogSearchAsync(string queryText, int resultCount)
    {
        var details = JsonSerializer.Serialize(new
        {
            query = queryText,
            resultCount
        });

        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EventType = "Search",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Audit: {EventType} - Query={Query}, ResultCount={ResultCount}",
            entry.EventType, queryText, resultCount);
    }

    public async Task LogDeletionAsync(string requirementNumber)
    {
        var details = JsonSerializer.Serialize(new
        {
            requirementNumber
        });

        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EventType = "Delete",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Audit: {EventType} - RequirementNumber={RequirementNumber}",
            entry.EventType, requirementNumber);
    }

    public async Task<PaginatedResultDto<AuditLogEntry>> GetLogsAsync(int page = 1, int pageSize = 50, string? eventType = null)
    {
        var query = _context.AuditLogEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResultDto<AuditLogEntry>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
