using TraceQ.Core.Interfaces;

namespace TraceQ.Cli.Services;

internal sealed class NoOpVectorStore : IVectorStore
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task UpsertAsync(Guid id, float[] vector, Dictionary<string, string> payload) => Task.CompletedTask;

    public Task UpsertBatchAsync(IEnumerable<(Guid id, float[] vector, Dictionary<string, string> payload)> points) =>
        Task.CompletedTask;

    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<List<(Guid id, float score)>> SearchAsync(
        float[] queryVector, int top = 20, Dictionary<string, string>? filters = null) =>
        Task.FromResult(new List<(Guid id, float score)>());
}
