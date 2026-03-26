namespace TraceQ.Core.Interfaces;

public interface IEmbeddingService
{
    bool IsAvailable { get; }
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(IEnumerable<(string id, string text)> items);
}
