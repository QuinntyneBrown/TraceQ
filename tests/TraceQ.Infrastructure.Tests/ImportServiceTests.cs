using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;
using TraceQ.Infrastructure.Csv;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Services;

namespace TraceQ.Infrastructure.Tests;

public class ImportServiceTests : IDisposable
{
    private readonly TraceQDbContext _context;
    private readonly ImportService _importService;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly RequirementRepository _requirementRepository;

    public ImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<TraceQDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new TraceQDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _requirementRepository = new RequirementRepository(_context);

        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockEmbeddingService
            .Setup(e => e.GenerateBatchEmbeddingsAsync(It.IsAny<IEnumerable<(string id, string text)>>()))
            .ReturnsAsync(new Dictionary<string, float[]>());

        _mockVectorStore = new Mock<IVectorStore>();

        var mockLogger = new Mock<ILogger<ImportService>>();
        var csvParser = new CsvParser();

        _importService = new ImportService(
            csvParser,
            _requirementRepository,
            _mockEmbeddingService.Object,
            _mockVectorStore.Object,
            mockLogger.Object,
            _context);
    }

    [Fact]
    public async Task ImportAsync_NewRequirements_AllInserted()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,Description,Type,State");
        csv.AppendLine("REQ-001,First Requirement,Description 1,Functional,Draft");
        csv.AppendLine("REQ-002,Second Requirement,Description 2,Non-Functional,Approved");
        csv.AppendLine("REQ-003,Third Requirement,Description 3,Interface,Review");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var result = await _importService.ImportAsync(stream, "test.csv");

        result.InsertedCount.Should().Be(3);
        result.UpdatedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.FileName.Should().Be("test.csv");
        result.BatchId.Should().NotBeEmpty();

        // Verify requirements exist in database
        var allReqs = await _context.Requirements.ToListAsync();
        allReqs.Should().HaveCount(3);
    }

    [Fact]
    public async Task ImportAsync_ReImportSameData_AllSkipped()
    {
        // First import
        var csv1 = new StringBuilder();
        csv1.AppendLine("Number,Name,Description,Type,State");
        csv1.AppendLine("REQ-001,First Requirement,Description 1,Functional,Draft");
        csv1.AppendLine("REQ-002,Second Requirement,Description 2,Non-Functional,Approved");

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csv1.ToString()));
        await _importService.ImportAsync(stream1, "first-import.csv");

        // Second import with same data
        var csv2 = new StringBuilder();
        csv2.AppendLine("Number,Name,Description,Type,State");
        csv2.AppendLine("REQ-001,First Requirement,Description 1,Functional,Draft");
        csv2.AppendLine("REQ-002,Second Requirement,Description 2,Non-Functional,Approved");

        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csv2.ToString()));
        var result = await _importService.ImportAsync(stream2, "second-import.csv");

        result.InsertedCount.Should().Be(0);
        result.UpdatedCount.Should().Be(0);
        result.SkippedCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_MixedNewAndExisting_CorrectCounts()
    {
        // First import: insert REQ-001 and REQ-002
        var csv1 = new StringBuilder();
        csv1.AppendLine("Number,Name,Description,Type,State");
        csv1.AppendLine("REQ-001,First Requirement,Description 1,Functional,Draft");
        csv1.AppendLine("REQ-002,Second Requirement,Description 2,Non-Functional,Approved");

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csv1.ToString()));
        await _importService.ImportAsync(stream1, "initial.csv");

        // Second import: REQ-001 updated, REQ-002 unchanged, REQ-003 new
        var csv2 = new StringBuilder();
        csv2.AppendLine("Number,Name,Description,Type,State");
        csv2.AppendLine("REQ-001,First Requirement UPDATED,Description 1 changed,Functional,Approved");
        csv2.AppendLine("REQ-002,Second Requirement,Description 2,Non-Functional,Approved");
        csv2.AppendLine("REQ-003,Third Requirement,Description 3,Interface,Draft");

        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csv2.ToString()));
        var result = await _importService.ImportAsync(stream2, "update.csv");

        result.InsertedCount.Should().Be(1);  // REQ-003
        result.UpdatedCount.Should().Be(1);   // REQ-001
        result.SkippedCount.Should().Be(1);   // REQ-002
        result.ErrorCount.Should().Be(0);

        // Verify the updated requirement
        var updated = await _requirementRepository.GetByNumberAsync("REQ-001");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("First Requirement UPDATED");
        updated.Description.Should().Be("Description 1 changed");
        updated.State.Should().Be("Approved");
        updated.IsEmbedded.Should().BeFalse();
    }

    [Fact]
    public async Task ImportAsync_UpdatedRequirement_MarksIsEmbeddedFalse()
    {
        // First import
        var csv1 = new StringBuilder();
        csv1.AppendLine("Number,Name,Description");
        csv1.AppendLine("REQ-001,Original Name,Original Description");

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csv1.ToString()));
        await _importService.ImportAsync(stream1, "initial.csv");

        // Manually set IsEmbedded to true to simulate embedding was done
        var req = await _requirementRepository.GetByNumberAsync("REQ-001");
        req!.IsEmbedded = true;
        await _context.SaveChangesAsync();

        // Second import with changes
        var csv2 = new StringBuilder();
        csv2.AppendLine("Number,Name,Description");
        csv2.AppendLine("REQ-001,Updated Name,Updated Description");

        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csv2.ToString()));
        var result = await _importService.ImportAsync(stream2, "update.csv");

        result.UpdatedCount.Should().Be(1);
        var updatedReq = await _requirementRepository.GetByNumberAsync("REQ-001");
        updatedReq!.IsEmbedded.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPaginatedBatches()
    {
        // Create two imports
        var csv1 = new StringBuilder();
        csv1.AppendLine("Number,Name");
        csv1.AppendLine("REQ-001,First");
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(csv1.ToString()));
        await _importService.ImportAsync(stream1, "batch1.csv");

        var csv2 = new StringBuilder();
        csv2.AppendLine("Number,Name");
        csv2.AppendLine("REQ-002,Second");
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(csv2.ToString()));
        await _importService.ImportAsync(stream2, "batch2.csv");

        var history = await _importService.GetHistoryAsync(1, 10);

        history.Items.Should().HaveCount(2);
        history.TotalCount.Should().Be(2);
        history.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetBatchDetailAsync_ExistingBatch_ReturnsDetail()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name");
        csv.AppendLine("REQ-001,First");
        csv.AppendLine("REQ-002,Second");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
        var importResult = await _importService.ImportAsync(stream, "detail-test.csv");

        var detail = await _importService.GetBatchDetailAsync(importResult.BatchId);

        detail.Should().NotBeNull();
        detail!.FileName.Should().Be("detail-test.csv");
        detail.Records.Should().HaveCount(2);
        detail.Records.All(r => r.Status == "Inserted").Should().BeTrue();
    }

    [Fact]
    public async Task GetBatchDetailAsync_NonExistentBatch_ReturnsNull()
    {
        var result = await _importService.GetBatchDetailAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
