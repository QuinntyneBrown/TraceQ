using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;

namespace TraceQ.Api.Tests.Controllers;

public class RequirementsControllerTests
{
    private readonly Mock<IRequirementRepository> _mockRepository;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly RequirementsController _controller;

    public RequirementsControllerTests()
    {
        _mockRepository = new Mock<IRequirementRepository>();
        _mockVectorStore = new Mock<IVectorStore>();
        _mockSearchService = new Mock<ISearchService>();
        _mockAuditService = new Mock<IAuditService>();
        var mockLogger = new Mock<ILogger<RequirementsController>>();
        _controller = new RequirementsController(
            _mockRepository.Object,
            _mockVectorStore.Object,
            _mockSearchService.Object,
            _mockAuditService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task GetFacets_ReturnsOkWithFacets()
    {
        var facets = new FacetsDto
        {
            Types = new List<FacetValueDto> { new() { Value = "Functional", Count = 5 } },
            States = new List<FacetValueDto> { new() { Value = "Draft", Count = 3 } }
        };
        _mockRepository.Setup(r => r.GetFacetsAsync()).ReturnsAsync(facets);

        var result = await _controller.GetFacets();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFacets = okResult.Value.Should().BeOfType<FacetsDto>().Subject;
        returnedFacets.Types.Should().HaveCount(1);
        returnedFacets.States.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResults()
    {
        var paginatedResult = new PaginatedResultDto<RequirementDto>
        {
            Items = new List<RequirementDto>
            {
                new() { Id = Guid.NewGuid(), RequirementNumber = "REQ-001", Name = "Test" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _mockRepository
            .Setup(r => r.GetAllAsync(null, 1, 20, null, false))
            .ReturnsAsync(paginatedResult);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeOfType<PaginatedResultDto<RequirementDto>>().Subject;
        returned.Items.Should().HaveCount(1);
        returned.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var requirement = new Requirement
        {
            Id = id,
            RequirementNumber = "REQ-001",
            Name = "Test Requirement",
            Description = "A test requirement"
        };
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(requirement);

        var result = await _controller.GetById(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<RequirementDto>().Subject;
        dto.RequirementNumber.Should().Be("REQ-001");
        dto.Name.Should().Be("Test Requirement");
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Requirement?)null);

        var result = await _controller.GetById(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var requirement = new Requirement { Id = id, RequirementNumber = "REQ-001", Name = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(requirement);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        _mockVectorStore.Verify(v => v.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Requirement?)null);

        var result = await _controller.Delete(id);

        result.Should().BeOfType<NotFoundObjectResult>();
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetSimilar_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var results = new List<SearchResultDto>
        {
            new()
            {
                Requirement = new RequirementDto { Id = Guid.NewGuid(), RequirementNumber = "REQ-002", Name = "Similar" },
                SimilarityScore = 0.9f
            }
        };
        _mockSearchService.Setup(s => s.FindSimilarAsync(id, 10)).ReturnsAsync(results);

        var result = await _controller.GetSimilar(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<List<SearchResultDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSimilar_NonExistentId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockSearchService
            .Setup(s => s.FindSimilarAsync(id, 10))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.GetSimilar(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
