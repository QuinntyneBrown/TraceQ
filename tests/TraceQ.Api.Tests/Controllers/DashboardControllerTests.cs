using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardLayoutRepository> _mockRepo;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockRepo = new Mock<IDashboardLayoutRepository>();
        var mockLogger = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_mockRepo.Object, mockLogger.Object);
    }

    [Fact]
    public async Task GetLayouts_ReturnsOkWithLayouts()
    {
        var layouts = new List<DashboardLayoutDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Default" }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(layouts);

        var result = await _controller.GetLayouts();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<List<DashboardLayoutDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveLayout_EmptyName_ReturnsBadRequest()
    {
        var layout = new DashboardLayoutDto { Name = "", LayoutJson = "{}" };

        var result = await _controller.SaveLayout(layout);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SaveLayout_EmptyJson_ReturnsBadRequest()
    {
        var layout = new DashboardLayoutDto { Name = "Test", LayoutJson = "" };

        var result = await _controller.SaveLayout(layout);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SaveLayout_Valid_ReturnsCreated()
    {
        var layout = new DashboardLayoutDto
        {
            Name = "My Layout",
            LayoutJson = "{\"widgets\":[]}"
        };
        var saved = new DashboardLayoutDto
        {
            Id = Guid.NewGuid(),
            Name = "My Layout",
            LayoutJson = "{\"widgets\":[]}",
            CreatedAt = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.SaveAsync(It.IsAny<DashboardLayoutDto>())).ReturnsAsync(saved);

        var result = await _controller.SaveLayout(layout);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task DeleteLayout_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((DashboardLayoutDto?)null);

        var result = await _controller.DeleteLayout(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteLayout_Found_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new DashboardLayoutDto { Id = id, Name = "Test" });

        var result = await _controller.DeleteLayout(id);

        result.Should().BeOfType<NoContentResult>();
        _mockRepo.Verify(r => r.DeleteAsync(id), Times.Once);
    }
}
