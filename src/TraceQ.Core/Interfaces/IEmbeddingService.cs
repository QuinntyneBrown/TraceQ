namespace TraceQ.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(IEnumerable<(string id, string text)> items);
}
