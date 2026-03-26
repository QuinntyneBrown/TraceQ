namespace TraceQ.Core.Interfaces;

public interface IVectorStore
{
    Task InitializeAsync();
    Task UpsertAsync(Guid id, float[] vector, Dictionary<string, string> payload);
    Task UpsertBatchAsync(IEnumerable<(Guid id, float[] vector, Dictionary<string, string> payload)> points);
    Task DeleteAsync(Guid id);
    Task<List<(Guid id, float score)>> SearchAsync(float[] queryVector, int top = 20, Dictionary<string, string>? filters = null);
}
