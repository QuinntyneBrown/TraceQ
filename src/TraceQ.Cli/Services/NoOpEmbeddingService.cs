using TraceQ.Core.Interfaces;

namespace TraceQ.Cli.Services;

internal sealed class NoOpEmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateEmbeddingAsync(string text) => Task.FromResult(Array.Empty<float>());

    public Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(
        IEnumerable<(string id, string text)> items) =>
        Task.FromResult(new Dictionary<string, float[]>());
}
