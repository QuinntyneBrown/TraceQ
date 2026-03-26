using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;
using TraceQ.Infrastructure.Services;

namespace TraceQ.Infrastructure.Tests.Services;

public class SearchServiceTests
{
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly Mock<IRequirementRepository> _mockRepository;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStore>();
        _mockRepository = new Mock<IRequirementRepository>();
        var mockLogger = new Mock<ILogger<SearchService>>();

        _searchService = new SearchService(
            _mockEmbeddingService.Object,
            _mockVectorStore.Object,
            _mockRepository.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task SemanticSearchAsync_ValidRequest_CallsEmbeddingServiceAndVectorStore()
    {
        // Arrange
        var request = new SearchRequestDto { Query = "navigation system", Top = 5 };
        var queryVector = new float[] { 0.1f, 0.2f, 0.3f };
        var reqId = Guid.NewGuid();

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync("navigation system"))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 5, null))
            .ReturnsAsync(new List<(Guid id, float score)> { (reqId, 0.95f) });

        _mockRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(reqId))))
            .ReturnsAsync(new List<Requirement>
            {
                new()
                {
                    Id = reqId,
                    RequirementNumber = "REQ-001",
                    Name = "Navigation System",
                    Description = "GPS-based navigation"
                }
            });

        // Act
        var results = await _searchService.SemanticSearchAsync(request);

        // Assert
        results.Should().HaveCount(1);
        results[0].SimilarityScore.Should().Be(0.95f);
        results[0].Requirement.RequirementNumber.Should().Be("REQ-001");

        _mockEmbeddingService.Verify(e => e.GenerateEmbeddingAsync("navigation system"), Times.Once);
        _mockVectorStore.Verify(v => v.SearchAsync(queryVector, 5, null), Times.Once);
    }

    [Fact]
    public async Task SemanticSearchAsync_WithFilters_BuildsFilterDictionary()
    {
        // Arrange
        var request = new SearchRequestDto
        {
            Query = "test query",
            Top = 10,
            Filters = new SearchFiltersDto
            {
                Type = "Functional",
                State = "Approved",
                Priority = null, // should not be included
                Module = "Navigation",
                Owner = null // should not be included
            }
        };
        var queryVector = new float[] { 0.1f };

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 10, It.Is<Dictionary<string, string>>(f =>
                f.Count == 3 &&
                f["type"] == "Functional" &&
                f["state"] == "Approved" &&
                f["module"] == "Navigation")))
            .ReturnsAsync(new List<(Guid id, float score)>());

        // Act
        var results = await _searchService.SemanticSearchAsync(request);

        // Assert
        results.Should().BeEmpty();
        _mockVectorStore.Verify(v => v.SearchAsync(queryVector, 10,
            It.Is<Dictionary<string, string>>(f => f.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task SemanticSearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        var request = new SearchRequestDto { Query = "", Top = 10 };

        var act = () => _searchService.SemanticSearchAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SemanticSearchAsync_TopOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var request = new SearchRequestDto { Query = "test", Top = 0 };

        var act = () => _searchService.SemanticSearchAsync(request);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task SemanticSearchAsync_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var request = new SearchRequestDto { Query = "nonexistent", Top = 10 };
        var queryVector = new float[] { 0.1f };

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 10, null))
            .ReturnsAsync(new List<(Guid id, float score)>());

        // Act
        var results = await _searchService.SemanticSearchAsync(request);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SemanticSearchAsync_PreservesRankOrder()
    {
        // Arrange
        var request = new SearchRequestDto { Query = "test", Top = 3 };
        var queryVector = new float[] { 0.1f };
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 3, null))
            .ReturnsAsync(new List<(Guid id, float score)>
            {
                (id1, 0.95f),
                (id2, 0.85f),
                (id3, 0.75f)
            });

        _mockRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Requirement>
            {
                new() { Id = id3, RequirementNumber = "REQ-003", Name = "Third" },
                new() { Id = id1, RequirementNumber = "REQ-001", Name = "First" },
                new() { Id = id2, RequirementNumber = "REQ-002", Name = "Second" }
            });

        // Act
        var results = await _searchService.SemanticSearchAsync(request);

        // Assert - order should follow vector store rank, not repository order
        results.Should().HaveCount(3);
        results[0].Requirement.RequirementNumber.Should().Be("REQ-001");
        results[0].SimilarityScore.Should().Be(0.95f);
        results[1].Requirement.RequirementNumber.Should().Be("REQ-002");
        results[1].SimilarityScore.Should().Be(0.85f);
        results[2].Requirement.RequirementNumber.Should().Be("REQ-003");
        results[2].SimilarityScore.Should().Be(0.75f);
    }

    [Fact]
    public async Task FindSimilarAsync_ExcludesSourceRequirement()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var similarId = Guid.NewGuid();
        var queryVector = new float[] { 0.1f };

        var sourceReq = new Requirement
        {
            Id = sourceId,
            RequirementNumber = "REQ-001",
            Name = "Source Requirement",
            Description = "Description of source"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sourceId)).ReturnsAsync(sourceReq);

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 11, null))
            .ReturnsAsync(new List<(Guid id, float score)>
            {
                (sourceId, 1.0f), // self-match should be excluded
                (similarId, 0.9f)
            });

        _mockRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(similarId))))
            .ReturnsAsync(new List<Requirement>
            {
                new()
                {
                    Id = similarId,
                    RequirementNumber = "REQ-002",
                    Name = "Similar Requirement"
                }
            });

        // Act
        var results = await _searchService.FindSimilarAsync(sourceId, 10);

        // Assert
        results.Should().HaveCount(1);
        results[0].Requirement.Id.Should().Be(similarId);
        results.Should().NotContain(r => r.Requirement.Id == sourceId);
    }

    [Fact]
    public async Task FindSimilarAsync_NonExistentRequirement_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Requirement?)null);

        var act = () => _searchService.FindSimilarAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task FindSimilarAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var queryVector = new float[] { 0.1f };

        var sourceReq = new Requirement
        {
            Id = sourceId,
            RequirementNumber = "REQ-001",
            Name = "Isolated Requirement",
            Description = "No similar requirements exist"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sourceId)).ReturnsAsync(sourceReq);

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, 11, null))
            .ReturnsAsync(new List<(Guid id, float score)>());

        // Act
        var results = await _searchService.FindSimilarAsync(sourceId, 10);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task FindSimilarAsync_ReEmbedsRequirementText()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var queryVector = new float[] { 0.1f };

        var sourceReq = new Requirement
        {
            Id = sourceId,
            RequirementNumber = "REQ-001",
            Name = "Navigation System",
            Description = "GPS-based navigation for aircraft"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sourceId)).ReturnsAsync(sourceReq);

        _mockEmbeddingService
            .Setup(e => e.GenerateEmbeddingAsync("Navigation System GPS-based navigation for aircraft"))
            .ReturnsAsync(queryVector);

        _mockVectorStore
            .Setup(v => v.SearchAsync(queryVector, It.IsAny<int>(), null))
            .ReturnsAsync(new List<(Guid id, float score)>());

        // Act
        await _searchService.FindSimilarAsync(sourceId);

        // Assert - verify the correct text was embedded
        _mockEmbeddingService.Verify(
            e => e.GenerateEmbeddingAsync("Navigation System GPS-based navigation for aircraft"),
            Times.Once);
    }
}
