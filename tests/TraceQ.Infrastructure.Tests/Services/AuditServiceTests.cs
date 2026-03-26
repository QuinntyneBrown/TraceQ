using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Services;

namespace TraceQ.Infrastructure.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly TraceQDbContext _context;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<TraceQDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new TraceQDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_context, mockLogger.Object);
    }

    [Fact]
    public async Task LogImportAsync_PersistsImportEventToDatabase()
    {
        // Act
        await _auditService.LogImportAsync("export.csv", 50, 10, 2);

        // Assert
        var entries = await _context.AuditLogEntries.ToListAsync();
        entries.Should().HaveCount(1);

        var entry = entries[0];
        entry.EventType.Should().Be("Import");
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var details = JsonDocument.Parse(entry.Details);
        details.RootElement.GetProperty("fileName").GetString().Should().Be("export.csv");
        details.RootElement.GetProperty("inserted").GetInt32().Should().Be(50);
        details.RootElement.GetProperty("updated").GetInt32().Should().Be(10);
        details.RootElement.GetProperty("errors").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task LogSearchAsync_PersistsSearchEventToDatabase()
    {
        // Act
        await _auditService.LogSearchAsync("thermal requirements", 15);

        // Assert
        var entries = await _context.AuditLogEntries.ToListAsync();
        entries.Should().HaveCount(1);

        var entry = entries[0];
        entry.EventType.Should().Be("Search");
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var details = JsonDocument.Parse(entry.Details);
        details.RootElement.GetProperty("query").GetString().Should().Be("thermal requirements");
        details.RootElement.GetProperty("resultCount").GetInt32().Should().Be(15);
    }

    [Fact]
    public async Task LogDeletionAsync_PersistsDeletionEventToDatabase()
    {
        // Act
        await _auditService.LogDeletionAsync("REQ-001");

        // Assert
        var entries = await _context.AuditLogEntries.ToListAsync();
        entries.Should().HaveCount(1);

        var entry = entries[0];
        entry.EventType.Should().Be("Delete");
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var details = JsonDocument.Parse(entry.Details);
        details.RootElement.GetProperty("requirementNumber").GetString().Should().Be("REQ-001");
    }

    [Fact]
    public async Task GetLogsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - create 5 entries
        for (int i = 1; i <= 5; i++)
        {
            await _auditService.LogSearchAsync($"query {i}", i * 10);
        }

        // Act - get page 2 with pageSize=2
        var result = await _auditService.GetLogsAsync(page: 2, pageSize: 2);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(2);
        // Results are ordered by Timestamp descending, so page 2 has items 3 and 2
    }

    [Fact]
    public async Task GetLogsAsync_WithEventTypeFilter_ReturnsOnlyMatchingEvents()
    {
        // Arrange - create mixed events
        await _auditService.LogImportAsync("file1.csv", 10, 5, 0);
        await _auditService.LogSearchAsync("query 1", 20);
        await _auditService.LogImportAsync("file2.csv", 20, 3, 1);
        await _auditService.LogDeletionAsync("REQ-100");
        await _auditService.LogSearchAsync("query 2", 5);

        // Act - filter by Import
        var importResult = await _auditService.GetLogsAsync(eventType: "Import");

        // Assert
        importResult.TotalCount.Should().Be(2);
        importResult.Items.Should().HaveCount(2);
        importResult.Items.Should().AllSatisfy(e => e.EventType.Should().Be("Import"));

        // Act - filter by Search
        var searchResult = await _auditService.GetLogsAsync(eventType: "Search");

        // Assert
        searchResult.TotalCount.Should().Be(2);
        searchResult.Items.Should().AllSatisfy(e => e.EventType.Should().Be("Search"));

        // Act - filter by Delete
        var deleteResult = await _auditService.GetLogsAsync(eventType: "Delete");

        // Assert
        deleteResult.TotalCount.Should().Be(1);
        deleteResult.Items.Should().AllSatisfy(e => e.EventType.Should().Be("Delete"));
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
