using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IRequirementRepository> _mockRepository;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _mockRepository = new Mock<IRequirementRepository>();
        _mockSearchService = new Mock<ISearchService>();
        var mockLogger = new Mock<ILogger<ReportsController>>();
        _controller = new ReportsController(
            _mockRepository.Object,
            _mockSearchService.Object,
            mockLogger.Object);
    }

    [Theory]
    [InlineData("type")]
    [InlineData("state")]
    [InlineData("priority")]
    [InlineData("module")]
    [InlineData("owner")]
    public async Task GetDistribution_ValidField_ReturnsOk(string field)
    {
        var distribution = new List<DistributionDto>
        {
            new() { Label = "TestValue", Count = 5 }
        };
        _mockRepository.Setup(r => r.GetDistributionAsync(field)).ReturnsAsync(distribution);

        var result = await _controller.GetDistribution(field);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<List<DistributionDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("")]
    public async Task GetDistribution_InvalidField_ReturnsBadRequest(string field)
    {
        var result = await _controller.GetDistribution(field);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetDistribution_ValidField_CaseInsensitive()
    {
        var distribution = new List<DistributionDto>();
        _mockRepository.Setup(r => r.GetDistributionAsync("TYPE")).ReturnsAsync(distribution);

        var result = await _controller.GetDistribution("TYPE");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTraceability_ReturnsOk()
    {
        var coverage = new TraceabilityCoverageDto
        {
            TotalRequirements = 100,
            TracedRequirements = 75,
            CoveragePercentage = 75.0
        };
        _mockRepository.Setup(r => r.GetTraceabilityCoverageAsync()).ReturnsAsync(coverage);

        var result = await _controller.GetTraceability();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeOfType<TraceabilityCoverageDto>().Subject;
        returned.CoveragePercentage.Should().Be(75.0);
    }

    [Fact]
    public async Task GetSimilarityClusters_ValidThreshold_ReturnsOk()
    {
        var clusters = new List<SimilarityClusterDto>
        {
            new() { ClusterId = 1, Members = new List<ClusterMemberDto>() }
        };
        _mockSearchService.Setup(s => s.GetSimilarityClustersAsync(0.85f)).ReturnsAsync(clusters);

        var result = await _controller.GetSimilarityClusters(0.85f);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<List<SimilarityClusterDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSimilarityClusters_InvalidThreshold_ReturnsBadRequest()
    {
        var result = await _controller.GetSimilarityClusters(1.5f);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
