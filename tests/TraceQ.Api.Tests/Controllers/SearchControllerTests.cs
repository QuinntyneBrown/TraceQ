using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Tests.Controllers;

public class SearchControllerTests
{
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _mockSearchService = new Mock<ISearchService>();
        _mockAuditService = new Mock<IAuditService>();
        var mockLogger = new Mock<ILogger<SearchController>>();
        _controller = new SearchController(_mockSearchService.Object, _mockAuditService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var request = new SearchRequestDto { Query = "", Top = 10 };

        var result = await _controller.Search(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_WhitespaceQuery_ReturnsBadRequest()
    {
        var request = new SearchRequestDto { Query = "   ", Top = 10 };

        var result = await _controller.Search(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_TopLessThanOne_ReturnsBadRequest()
    {
        var request = new SearchRequestDto { Query = "test query", Top = 0 };

        var result = await _controller.Search(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_TopGreaterThan100_ReturnsBadRequest()
    {
        var request = new SearchRequestDto { Query = "test query", Top = 101 };

        var result = await _controller.Search(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsOkWithResults()
    {
        var request = new SearchRequestDto { Query = "test query", Top = 10 };
        var expectedResults = new List<SearchResultDto>
        {
            new SearchResultDto
            {
                Requirement = new RequirementDto
                {
                    Id = Guid.NewGuid(),
                    RequirementNumber = "REQ-001",
                    Name = "Test Requirement"
                },
                SimilarityScore = 0.95f
            }
        };

        _mockSearchService
            .Setup(s => s.SemanticSearchAsync(It.IsAny<SearchRequestDto>()))
            .ReturnsAsync(expectedResults);

        var result = await _controller.Search(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResults = okResult.Value.Should().BeAssignableTo<List<SearchResultDto>>().Subject;
        returnedResults.Should().HaveCount(1);
        returnedResults[0].SimilarityScore.Should().Be(0.95f);
    }

    [Fact]
    public async Task Search_ValidQuery_CallsSearchService()
    {
        var request = new SearchRequestDto { Query = "test query", Top = 10 };
        _mockSearchService
            .Setup(s => s.SemanticSearchAsync(It.IsAny<SearchRequestDto>()))
            .ReturnsAsync(new List<SearchResultDto>());

        await _controller.Search(request);

        _mockSearchService.Verify(s => s.SemanticSearchAsync(
            It.Is<SearchRequestDto>(r => r.Query == "test query" && r.Top == 10)),
            Times.Once);
    }

    [Fact]
    public async Task Search_ServiceThrowsException_Returns500()
    {
        var request = new SearchRequestDto { Query = "test query", Top = 10 };
        _mockSearchService
            .Setup(s => s.SemanticSearchAsync(It.IsAny<SearchRequestDto>()))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _controller.Search(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }
}
