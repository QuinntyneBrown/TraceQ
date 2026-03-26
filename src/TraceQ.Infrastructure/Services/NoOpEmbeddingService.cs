using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Services;

/// <summary>
/// No-op implementation of IEmbeddingService used as a placeholder
/// until the real ONNX-based embedding service is implemented.
/// </summary>
public class NoOpEmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateEmbeddingAsync(string text)
    {
        return Task.FromResult(Array.Empty<float>());
    }

    public Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(IEnumerable<(string id, string text)> items)
    {
        return Task.FromResult(new Dictionary<string, float[]>());
    }
}
