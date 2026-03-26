using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.VectorStore;

/// <summary>
/// IVectorStore implementation backed by a Qdrant vector database.
/// Supports degraded mode: if Qdrant is unreachable at initialization,
/// all operations gracefully return empty results / no-op.
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly IQdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;
    private bool _isAvailable;

    /// <summary>
    /// Maximum number of points to upsert in a single batch request.
    /// </summary>
    internal const int MaxBatchSize = 100;

    /// <summary>
    /// Indicates whether the Qdrant server was reachable during initialization.
    /// Exposed internally for testability.
    /// </summary>
    internal bool IsAvailable => _isAvailable;

    public QdrantVectorStore(
        IQdrantClient client,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStore> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation(
                "Connecting to Qdrant at {Host}:{Port}, collection '{Collection}'",
                _options.Host, _options.GrpcPort, _options.CollectionName);

            var exists = await _client.CollectionExistsAsync(_options.CollectionName);

            if (!exists)
            {
                _logger.LogInformation(
                    "Collection '{Collection}' does not exist. Creating with vector size 384, cosine distance",
                    _options.CollectionName);

                await _client.CreateCollectionAsync(
                    _options.CollectionName,
                    new VectorParams
                    {
                        Size = 384,
                        Distance = Distance.Cosine
                    });

                _logger.LogInformation("Collection '{Collection}' created successfully", _options.CollectionName);
            }
            else
            {
                _logger.LogInformation("Collection '{Collection}' already exists", _options.CollectionName);
            }

            _isAvailable = true;
            _logger.LogInformation("Qdrant vector store initialized successfully");
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            _logger.LogWarning(
                ex,
                "Qdrant is unreachable at {Host}:{Port}. Vector store running in degraded mode — " +
                "vector operations will be skipped until restart",
                _options.Host, _options.GrpcPort);
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(Guid id, float[] vector, Dictionary<string, string> payload)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Qdrant unavailable — skipping upsert for {Id}", id);
            return;
        }

        var point = CreatePointStruct(id, vector, payload);

        await _client.UpsertAsync(
            _options.CollectionName,
            new List<PointStruct> { point });

        _logger.LogDebug("Upserted point {Id} to collection '{Collection}'", id, _options.CollectionName);
    }

    /// <inheritdoc />
    public async Task UpsertBatchAsync(
        IEnumerable<(Guid id, float[] vector, Dictionary<string, string> payload)> points)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Qdrant unavailable — skipping batch upsert");
            return;
        }

        var allPoints = points.ToList();
        var chunks = ChunkPoints(allPoints, MaxBatchSize);

        foreach (var chunk in chunks)
        {
            var pointStructs = chunk
                .Select(p => CreatePointStruct(p.id, p.vector, p.payload))
                .ToList();

            await _client.UpsertAsync(_options.CollectionName, pointStructs);

            _logger.LogDebug(
                "Batch upserted {Count} points to collection '{Collection}'",
                pointStructs.Count, _options.CollectionName);
        }
    }

    /// <inheritdoc />
    public async Task<List<(Guid id, float score)>> SearchAsync(
        float[] queryVector,
        int top = 20,
        Dictionary<string, string>? filters = null)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Qdrant unavailable — returning empty search results");
            return new List<(Guid id, float score)>();
        }

        Filter? filter = null;
        if (filters is { Count: > 0 })
        {
            filter = BuildFilter(filters);
        }

        var results = await _client.SearchAsync(
            _options.CollectionName,
            vector: new ReadOnlyMemory<float>(queryVector),
            filter: filter,
            limit: (ulong)top);

        var output = new List<(Guid id, float score)>();
        foreach (var scored in results)
        {
            var guid = ParsePointIdToGuid(scored.Id);
            if (guid.HasValue)
            {
                output.Add((guid.Value, scored.Score));
            }
        }

        _logger.LogDebug("Search returned {Count} results from collection '{Collection}'",
            output.Count, _options.CollectionName);

        return output;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Qdrant unavailable — skipping delete for {Id}", id);
            return;
        }

        await _client.DeleteAsync(_options.CollectionName, id);

        _logger.LogDebug("Deleted point {Id} from collection '{Collection}'", id, _options.CollectionName);
    }

    /// <summary>
    /// Creates a Qdrant PointStruct from a Guid ID, vector, and string payload.
    /// </summary>
    private static PointStruct CreatePointStruct(
        Guid id, float[] vector, Dictionary<string, string> payload)
    {
        var point = new PointStruct
        {
            Id = id,
            Vectors = vector
        };

        foreach (var kvp in payload)
        {
            point.Payload[kvp.Key] = kvp.Value;
        }

        return point;
    }

    /// <summary>
    /// Builds a Qdrant filter from a dictionary of key-value match conditions.
    /// Each entry becomes a keyword match condition combined with AND logic.
    /// </summary>
    private static Filter BuildFilter(Dictionary<string, string> filters)
    {
        var filter = new Filter();
        foreach (var kvp in filters)
        {
            filter.Must.Add(Conditions.MatchKeyword(kvp.Key, kvp.Value));
        }
        return filter;
    }

    /// <summary>
    /// Parses a Qdrant PointId back to a .NET Guid.
    /// Returns null if the PointId format is not a UUID.
    /// </summary>
    private static Guid? ParsePointIdToGuid(PointId pointId)
    {
        if (pointId.HasUuid && Guid.TryParse(pointId.Uuid, out var guid))
        {
            return guid;
        }
        return null;
    }

    /// <summary>
    /// Splits a list of points into chunks of the specified maximum size.
    /// Exposed as internal static for unit-test verification.
    /// </summary>
    internal static List<List<T>> ChunkPoints<T>(List<T> source, int chunkSize)
    {
        var chunks = new List<List<T>>();
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            var count = Math.Min(chunkSize, source.Count - i);
            chunks.Add(source.GetRange(i, count));
        }
        return chunks;
    }
}
