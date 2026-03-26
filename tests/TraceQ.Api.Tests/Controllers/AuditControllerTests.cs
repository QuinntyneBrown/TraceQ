using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;

namespace TraceQ.Api.Tests.Controllers;

public class AuditControllerTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        _mockAuditService = new Mock<IAuditService>();
        _controller = new AuditController(_mockAuditService.Object);
    }

    [Fact]
    public async Task GetLogs_DefaultParams_ReturnsOk()
    {
        var logs = new PaginatedResultDto<AuditLogEntry>
        {
            Items = new List<AuditLogEntry>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };
        _mockAuditService.Setup(s => s.GetLogsAsync(1, 50, null)).ReturnsAsync(logs);

        var result = await _controller.GetLogs();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLogs_WithEventTypeFilter_PassesFilter()
    {
        var logs = new PaginatedResultDto<AuditLogEntry>
        {
            Items = new List<AuditLogEntry>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };
        _mockAuditService.Setup(s => s.GetLogsAsync(1, 50, "Import")).ReturnsAsync(logs);

        var result = await _controller.GetLogs(eventType: "Import");

        result.Should().BeOfType<OkObjectResult>();
        _mockAuditService.Verify(s => s.GetLogsAsync(1, 50, "Import"), Times.Once);
    }

    [Fact]
    public async Task GetLogs_InvalidPageSize_ClampedToDefault()
    {
        var logs = new PaginatedResultDto<AuditLogEntry>
        {
            Items = new List<AuditLogEntry>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };
        _mockAuditService.Setup(s => s.GetLogsAsync(1, 50, null)).ReturnsAsync(logs);

        // page < 1 and pageSize < 1 should be clamped
        var result = await _controller.GetLogs(page: -1, pageSize: -5);

        result.Should().BeOfType<OkObjectResult>();
        _mockAuditService.Verify(s => s.GetLogsAsync(1, 50, null), Times.Once);
    }

    [Fact]
    public async Task GetLogs_ExcessivePageSize_ClampedTo200()
    {
        var logs = new PaginatedResultDto<AuditLogEntry>
        {
            Items = new List<AuditLogEntry>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 200
        };
        _mockAuditService.Setup(s => s.GetLogsAsync(1, 200, null)).ReturnsAsync(logs);

        var result = await _controller.GetLogs(pageSize: 500);

        result.Should().BeOfType<OkObjectResult>();
        _mockAuditService.Verify(s => s.GetLogsAsync(1, 200, null), Times.Once);
    }
}
