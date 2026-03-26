using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Embeddings;

/// <summary>
/// Degraded embedding service used when the local ONNX model is unavailable.
/// Returns zero vectors so the API can keep running, while health checks report degradation.
/// </summary>
public sealed class FallbackEmbeddingService : IEmbeddingService
{
    private readonly int _embeddingDimension;
    private readonly ILogger<FallbackEmbeddingService> _logger;

    public FallbackEmbeddingService(
        IOptions<EmbeddingModelOptions> options,
        ILogger<FallbackEmbeddingService> logger)
    {
        _embeddingDimension = options.Value.EmbeddingDimension;
        _logger = logger;
        _logger.LogWarning(
            "Embedding model unavailable. Falling back to zero-vector embeddings until the local model files are restored.");
    }

    public bool IsAvailable => false;

    public Task<float[]> GenerateEmbeddingAsync(string text)
    {
        return Task.FromResult(new float[_embeddingDimension]);
    }

    public Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(
        IEnumerable<(string id, string text)> items)
    {
        var results = new Dictionary<string, float[]>();

        foreach (var (id, _) in items)
        {
            results[id] = new float[_embeddingDimension];
        }

        return Task.FromResult(results);
    }
}
