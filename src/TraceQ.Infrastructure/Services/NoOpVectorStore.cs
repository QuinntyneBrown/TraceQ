using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Services;

/// <summary>
/// No-op implementation of IVectorStore used as a placeholder
/// until the real Qdrant-based vector store is implemented.
/// </summary>
public class NoOpVectorStore : IVectorStore
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task UpsertAsync(Guid id, float[] vector, Dictionary<string, string> payload)
    {
        return Task.CompletedTask;
    }

    public Task UpsertBatchAsync(IEnumerable<(Guid id, float[] vector, Dictionary<string, string> payload)> points)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task<List<(Guid id, float score)>> SearchAsync(float[] queryVector, int top = 20, Dictionary<string, string>? filters = null)
    {
        return Task.FromResult(new List<(Guid id, float score)>());
    }
}
