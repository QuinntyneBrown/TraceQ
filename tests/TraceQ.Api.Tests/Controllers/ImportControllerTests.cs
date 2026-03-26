using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Api.Controllers;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Tests.Controllers;

public class ImportControllerTests
{
    private readonly Mock<IImportService> _mockImportService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _mockImportService = new Mock<IImportService>();
        _mockAuditService = new Mock<IAuditService>();
        var mockLogger = new Mock<ILogger<ImportController>>();
        _controller = new ImportController(
            _mockImportService.Object,
            _mockAuditService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task ImportCsv_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.ImportCsv(null!);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ImportCsv_NonCsvFile_ReturnsBadRequest()
    {
        var file = CreateMockFile("test.txt", 100);
        var result = await _controller.ImportCsv(file);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ImportCsv_ValidCsv_ReturnsOk()
    {
        var file = CreateMockFile("export.csv", 500);
        var importResult = new ImportResultDto
        {
            BatchId = Guid.NewGuid(),
            FileName = "export.csv",
            InsertedCount = 10,
            UpdatedCount = 0,
            ErrorCount = 0,
            SkippedCount = 0
        };
        _mockImportService.Setup(s => s.ImportAsync(It.IsAny<Stream>(), "export.csv"))
            .ReturnsAsync(importResult);

        var result = await _controller.ImportCsv(file);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeOfType<ImportResultDto>().Subject;
        returned.InsertedCount.Should().Be(10);
    }

    [Fact]
    public async Task ImportCsv_ValidCsv_CallsAuditService()
    {
        var file = CreateMockFile("export.csv", 500);
        var importResult = new ImportResultDto
        {
            InsertedCount = 5,
            UpdatedCount = 3,
            ErrorCount = 1
        };
        _mockImportService.Setup(s => s.ImportAsync(It.IsAny<Stream>(), "export.csv"))
            .ReturnsAsync(importResult);

        await _controller.ImportCsv(file);

        _mockAuditService.Verify(a => a.LogImportAsync("export.csv", 5, 3, 1), Times.Once);
    }

    [Fact]
    public async Task ImportCsv_AllErrors_ReturnsUnprocessableEntity()
    {
        var file = CreateMockFile("bad.csv", 100);
        var importResult = new ImportResultDto
        {
            InsertedCount = 0,
            UpdatedCount = 0,
            ErrorCount = 5,
            SkippedCount = 0
        };
        _mockImportService.Setup(s => s.ImportAsync(It.IsAny<Stream>(), "bad.csv"))
            .ReturnsAsync(importResult);

        var result = await _controller.ImportCsv(file);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        var history = new PaginatedResultDto<ImportBatchDto>
        {
            Items = new List<ImportBatchDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockImportService.Setup(s => s.GetHistoryAsync(1, 20)).ReturnsAsync(history);

        var result = await _controller.GetHistory();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBatchDetail_NotFound_Returns404()
    {
        var batchId = Guid.NewGuid();
        _mockImportService.Setup(s => s.GetBatchDetailAsync(batchId))
            .ReturnsAsync((ImportBatchDetailDto?)null);

        var result = await _controller.GetBatchDetail(batchId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBatchDetail_Found_ReturnsOk()
    {
        var batchId = Guid.NewGuid();
        var detail = new ImportBatchDetailDto { Id = batchId, FileName = "test.csv" };
        _mockImportService.Setup(s => s.GetBatchDetailAsync(batchId)).ReturnsAsync(detail);

        var result = await _controller.GetBatchDetail(batchId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ImportBatchDetailDto>();
    }

    private static IFormFile CreateMockFile(string fileName, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[length]));
        return mock.Object;
    }
}
