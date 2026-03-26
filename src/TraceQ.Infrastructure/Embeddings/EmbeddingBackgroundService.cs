using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Embeddings;

/// <summary>
/// Background service that periodically checks for unembedded requirements
/// and generates their embeddings using the ONNX model.
/// Runs every 30 seconds. Processes requirements in batches of 50.
/// </summary>
public class EmbeddingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<EmbeddingBackgroundService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;

    public EmbeddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IEmbeddingService embeddingService,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmbeddingBackgroundService started. Polling every {Interval}s", PollingInterval.TotalSeconds);

        // Small initial delay to let the application finish starting
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnembeddedRequirementsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unembedded requirements");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("EmbeddingBackgroundService stopped");
    }

    private async Task ProcessUnembeddedRequirementsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var requirementRepository = scope.ServiceProvider.GetRequiredService<IRequirementRepository>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        if (!_embeddingService.IsAvailable || !vectorStore.IsAvailable)
        {
            _logger.LogDebug(
                "Skipping background embedding pass because dependencies are unavailable (Embeddings: {EmbeddingsAvailable}, VectorStore: {VectorStoreAvailable})",
                _embeddingService.IsAvailable,
                vectorStore.IsAvailable);
            return;
        }

        var unembedded = await requirementRepository.GetUnembeddedAsync();

        if (unembedded.Count == 0)
            return;

        _logger.LogInformation("Found {Count} unembedded requirements to process", unembedded.Count);

        // Process in batches
        var batches = unembedded
            .Select((r, i) => new { Requirement = r, Index = i })
            .GroupBy(x => x.Index / BatchSize)
            .Select(g => g.Select(x => x.Requirement).ToList())
            .ToList();

        int totalProcessed = 0;

        foreach (var batch in batches)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            // Prepare items for batch embedding
            var items = batch.Select(r =>
            {
                var text = BuildEmbeddingText(r.Name, r.Description);
                return (id: r.Id.ToString(), text);
            }).ToList();

            // Generate embeddings
            var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(items);

            // Upsert to vector store and mark as embedded
            var vectorPoints = new List<(Guid id, float[] vector, Dictionary<string, string> payload)>();
            foreach (var req in batch)
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
                        ["priority"] = req.Priority ?? string.Empty
                    };

                    vectorPoints.Add((req.Id, vector, payload));
                }
            }

            if (vectorPoints.Count > 0)
            {
                await vectorStore.UpsertBatchAsync(vectorPoints);
                var embeddedIds = batch.Select(r => r.Id);
                await requirementRepository.MarkAsEmbeddedAsync(embeddedIds);
            }

            totalProcessed += batch.Count;
            _logger.LogInformation("Embedded batch: {BatchProcessed}/{Total} requirements processed so far",
                totalProcessed, unembedded.Count);
        }

        _logger.LogInformation("Completed embedding {Total} requirements", totalProcessed);
    }

    /// <summary>
    /// Builds the text to embed from requirement name and description.
    /// </summary>
    private static string BuildEmbeddingText(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return name;

        return $"{name} {description}";
    }
}
