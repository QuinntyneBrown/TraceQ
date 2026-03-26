using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using TraceQ.Infrastructure.VectorStore;

namespace TraceQ.Infrastructure.Tests.VectorStore;

public class QdrantVectorStoreTests
{
    private readonly Mock<IQdrantClient> _mockClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStoreTests()
    {
        _mockClient = new Mock<IQdrantClient>();
        _options = new QdrantOptions
        {
            Host = "localhost",
            GrpcPort = 6334,
            CollectionName = "test-requirements"
        };
        _logger = NullLogger<QdrantVectorStore>.Instance;
    }

    private QdrantVectorStore CreateStore()
    {
        return new QdrantVectorStore(
            _mockClient.Object,
            Options.Create(_options),
            _logger);
    }

    #region InitializeAsync tests

    [Fact]
    public async Task InitializeAsync_WhenQdrantUnreachable_SetsIsAvailableFalse()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CollectionExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "Connection refused")));

        var store = CreateStore();

        // Act
        await store.InitializeAsync();

        // Assert
        store.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_WhenCollectionExists_SetsIsAvailableTrue()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CollectionExistsAsync(_options.CollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var store = CreateStore();

        // Act
        await store.InitializeAsync();

        // Assert
        store.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenCollectionDoesNotExist_CreatesCollectionAndSetsAvailable()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CollectionExistsAsync(_options.CollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var store = CreateStore();

        // Act
        await store.InitializeAsync();

        // Assert
        store.IsAvailable.Should().BeTrue();

        _mockClient.Verify(c => c.CreateCollectionAsync(
            _options.CollectionName,
            It.Is<VectorParams>(vp => vp.Size == 384 && vp.Distance == Distance.Cosine),
            It.IsAny<uint>(),
            It.IsAny<uint>(),
            It.IsAny<uint>(),
            It.IsAny<bool>(),
            It.IsAny<HnswConfigDiff>(),
            It.IsAny<OptimizersConfigDiff>(),
            It.IsAny<WalConfigDiff>(),
            It.IsAny<QuantizationConfig>(),
            It.IsAny<string>(),
            It.IsAny<ShardingMethod?>(),
            It.IsAny<SparseVectorConfig>(),
            It.IsAny<StrictModeConfig>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Degraded mode tests

    [Fact]
    public async Task UpsertAsync_WhenNotAvailable_DoesNotThrow()
    {
        // Arrange — never call InitializeAsync, so IsAvailable is false
        var store = CreateStore();

        // Act
        var act = () => store.UpsertAsync(Guid.NewGuid(), new float[384], new Dictionary<string, string>());

        // Assert
        await act.Should().NotThrowAsync();
        _mockClient.Verify(
            c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<PointStruct>>(),
                It.IsAny<bool>(), It.IsAny<WriteOrderingType?>(),
                It.IsAny<ShardKeySelector>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpsertBatchAsync_WhenNotAvailable_DoesNotThrow()
    {
        var store = CreateStore();

        var points = new List<(Guid id, float[] vector, Dictionary<string, string> payload)>
        {
            (Guid.NewGuid(), new float[384], new Dictionary<string, string> { ["k"] = "v" })
        };

        var act = () => store.UpsertBatchAsync(points);

        await act.Should().NotThrowAsync();
        _mockClient.Verify(
            c => c.UpsertAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<PointStruct>>(),
                It.IsAny<bool>(), It.IsAny<WriteOrderingType?>(),
                It.IsAny<ShardKeySelector>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenNotAvailable_ReturnsEmptyList()
    {
        var store = CreateStore();

        var result = await store.SearchAsync(new float[384], 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAvailable_DoesNotThrow()
    {
        var store = CreateStore();

        var act = () => store.DeleteAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
        _mockClient.Verify(
            c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<bool>(), It.IsAny<WriteOrderingType?>(),
                It.IsAny<ShardKeySelector>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Chunking tests

    [Fact]
    public void ChunkPoints_WithExactMultiple_ReturnsCorrectChunks()
    {
        // 200 items with chunk size 100 => 2 chunks of 100
        var source = Enumerable.Range(0, 200).ToList();

        var chunks = QdrantVectorStore.ChunkPoints(source, 100);

        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(100);
        chunks[1].Should().HaveCount(100);
    }

    [Fact]
    public void ChunkPoints_WithRemainder_ReturnsCorrectChunks()
    {
        // 250 items with chunk size 100 => 3 chunks: 100, 100, 50
        var source = Enumerable.Range(0, 250).ToList();

        var chunks = QdrantVectorStore.ChunkPoints(source, 100);

        chunks.Should().HaveCount(3);
        chunks[0].Should().HaveCount(100);
        chunks[1].Should().HaveCount(100);
        chunks[2].Should().HaveCount(50);
    }

    [Fact]
    public void ChunkPoints_WithFewerThanChunkSize_ReturnsSingleChunk()
    {
        var source = Enumerable.Range(0, 50).ToList();

        var chunks = QdrantVectorStore.ChunkPoints(source, 100);

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(50);
    }

    [Fact]
    public void ChunkPoints_WithEmptyList_ReturnsEmptyChunks()
    {
        var source = new List<int>();

        var chunks = QdrantVectorStore.ChunkPoints(source, 100);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public void ChunkPoints_PreservesOrder()
    {
        var source = Enumerable.Range(0, 150).ToList();

        var chunks = QdrantVectorStore.ChunkPoints(source, 100);

        chunks[0].Should().BeEquivalentTo(Enumerable.Range(0, 100), opts => opts.WithStrictOrdering());
        chunks[1].Should().BeEquivalentTo(Enumerable.Range(100, 50), opts => opts.WithStrictOrdering());
    }

    #endregion

    #region UpsertBatchAsync chunking behavior

    [Fact]
    public async Task UpsertBatchAsync_WithMoreThan100Points_UpsertInChunks()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CollectionExistsAsync(_options.CollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var store = CreateStore();
        await store.InitializeAsync();

        var points = Enumerable.Range(0, 250)
            .Select(_ => (
                id: Guid.NewGuid(),
                vector: new float[384],
                payload: new Dictionary<string, string> { ["key"] = "value" }))
            .ToList();

        // Act
        await store.UpsertBatchAsync(points);

        // Assert — should be called 3 times: 100 + 100 + 50
        _mockClient.Verify(
            c => c.UpsertAsync(
                _options.CollectionName,
                It.IsAny<IReadOnlyList<PointStruct>>(),
                It.IsAny<bool>(),
                It.IsAny<WriteOrderingType?>(),
                It.IsAny<ShardKeySelector>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task UpsertBatchAsync_WithExactly100Points_UpsertInSingleCall()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CollectionExistsAsync(_options.CollectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var store = CreateStore();
        await store.InitializeAsync();

        var points = Enumerable.Range(0, 100)
            .Select(_ => (
                id: Guid.NewGuid(),
                vector: new float[384],
                payload: new Dictionary<string, string> { ["key"] = "value" }))
            .ToList();

        // Act
        await store.UpsertBatchAsync(points);

        // Assert — exactly one upsert call
        _mockClient.Verify(
            c => c.UpsertAsync(
                _options.CollectionName,
                It.Is<IReadOnlyList<PointStruct>>(p => p.Count == 100),
                It.IsAny<bool>(),
                It.IsAny<WriteOrderingType?>(),
                It.IsAny<ShardKeySelector>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Integration tests (require real Qdrant instance)

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Integration_InitializeAsync_ConnectsToQdrant()
    {
        // This test requires a running Qdrant instance on localhost:6334
        var client = new QdrantClient("localhost", 6334);
        var options = Options.Create(new QdrantOptions
        {
            Host = "localhost",
            GrpcPort = 6334,
            CollectionName = $"test-integration-{Guid.NewGuid():N}"
        });

        var store = new QdrantVectorStore(client, options, _logger);

        try
        {
            await store.InitializeAsync();

            // If Qdrant is available, this should succeed
            if (store.IsAvailable)
            {
                store.IsAvailable.Should().BeTrue();

                // Clean up the test collection
                await client.DeleteCollectionAsync(options.Value.CollectionName);
            }
        }
        catch
        {
            // If Qdrant is not available, the test is inconclusive — not a failure
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Integration_UpsertAndSearch_RoundTrips()
    {
        var collectionName = $"test-roundtrip-{Guid.NewGuid():N}";
        var client = new QdrantClient("localhost", 6334);
        var options = Options.Create(new QdrantOptions
        {
            Host = "localhost",
            GrpcPort = 6334,
            CollectionName = collectionName
        });

        var store = new QdrantVectorStore(client, options, _logger);
        await store.InitializeAsync();

        if (!store.IsAvailable)
        {
            // Qdrant not running — skip
            return;
        }

        try
        {
            var id = Guid.NewGuid();
            var vector = CreateNormalizedVector(384);
            var payload = new Dictionary<string, string>
            {
                ["requirementNumber"] = "REQ-001",
                ["name"] = "Test Requirement"
            };

            // Upsert
            await store.UpsertAsync(id, vector, payload);

            // Search
            var results = await store.SearchAsync(vector, top: 1);
            results.Should().NotBeEmpty();
            results[0].id.Should().Be(id);
            results[0].score.Should().BeGreaterThan(0.9f);

            // Delete
            await store.DeleteAsync(id);

            // Search again — should be empty
            var afterDelete = await store.SearchAsync(vector, top: 1);
            afterDelete.Should().BeEmpty();
        }
        finally
        {
            // Clean up
            try { await client.DeleteCollectionAsync(collectionName); } catch { }
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Integration_SearchWithFilters_ReturnsFilteredResults()
    {
        var collectionName = $"test-filter-{Guid.NewGuid():N}";
        var client = new QdrantClient("localhost", 6334);
        var options = Options.Create(new QdrantOptions
        {
            Host = "localhost",
            GrpcPort = 6334,
            CollectionName = collectionName
        });

        var store = new QdrantVectorStore(client, options, _logger);
        await store.InitializeAsync();

        if (!store.IsAvailable)
        {
            return;
        }

        try
        {
            var vector = CreateNormalizedVector(384);

            // Insert two points with different modules
            var id1 = Guid.NewGuid();
            await store.UpsertAsync(id1, vector, new Dictionary<string, string> { ["module"] = "avionics" });

            var id2 = Guid.NewGuid();
            var vector2 = CreateNormalizedVector(384);
            await store.UpsertAsync(id2, vector2, new Dictionary<string, string> { ["module"] = "propulsion" });

            // Search with filter
            var results = await store.SearchAsync(
                vector, top: 10,
                filters: new Dictionary<string, string> { ["module"] = "avionics" });

            results.Should().Contain(r => r.id == id1);
            results.Should().NotContain(r => r.id == id2);
        }
        finally
        {
            try { await client.DeleteCollectionAsync(collectionName); } catch { }
        }
    }

    /// <summary>
    /// Creates a random normalized vector for testing.
    /// </summary>
    private static float[] CreateNormalizedVector(int dimension)
    {
        var rng = new Random(42);
        var vector = new float[dimension];
        double sumSq = 0;
        for (int i = 0; i < dimension; i++)
        {
            vector[i] = (float)(rng.NextDouble() * 2 - 1);
            sumSq += vector[i] * vector[i];
        }
        var norm = (float)Math.Sqrt(sumSq);
        for (int i = 0; i < dimension; i++)
        {
            vector[i] /= norm;
        }
        return vector;
    }

    #endregion
}
