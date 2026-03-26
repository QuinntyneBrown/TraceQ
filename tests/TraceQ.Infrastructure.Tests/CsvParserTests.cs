using System.Text;
using FluentAssertions;
using TraceQ.Infrastructure.Csv;

namespace TraceQ.Infrastructure.Tests;

public class CsvParserTests
{
    private readonly CsvParser _parser = new();

    [Fact]
    public async Task ParseAsync_AllColumns_ParsesCorrectly()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,Description,Type,State,Priority,Owner,Created On,Modified On,Module,Parent Number,Traced To");
        csv.AppendLine("REQ-001,Requirement 1,Description of req 1,Functional,Approved,High,John Doe,2024-01-15,2024-02-20,Navigation,REQ-000,REQ-002;REQ-003");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.Requirement.Should().NotBeNull();

        var req = result.Requirement!;
        req.RequirementNumber.Should().Be("REQ-001");
        req.Name.Should().Be("Requirement 1");
        req.Description.Should().Be("Description of req 1");
        req.Type.Should().Be("Functional");
        req.State.Should().Be("Approved");
        req.Priority.Should().Be("High");
        req.Owner.Should().Be("John Doe");
        req.CreatedDate.Should().NotBeNull();
        req.ModifiedDate.Should().NotBeNull();
        req.Module.Should().Be("Navigation");
        req.ParentNumber.Should().Be("REQ-000");
        req.TracedTo.Should().Be("REQ-002;REQ-003");
    }

    [Fact]
    public async Task ParseAsync_OnlyRequiredColumns_ParsesSuccessfully()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name");
        csv.AppendLine("REQ-001,Requirement 1");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.Requirement.Should().NotBeNull();
        result.Requirement!.RequirementNumber.Should().Be("REQ-001");
        result.Requirement!.Name.Should().Be("Requirement 1");
        result.Requirement!.Description.Should().BeNull();
        result.Requirement!.Type.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_QuotedFieldsWithCommas_HandlesCorrectly()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,Description");
        csv.AppendLine("REQ-001,\"Requirement, with comma\",\"Description with \"\"quotes\"\" and, commas\"");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.Requirement!.Name.Should().Be("Requirement, with comma");
        result.Requirement!.Description.Should().Be("Description with \"quotes\" and, commas");
    }

    [Fact]
    public async Task ParseAsync_CaseInsensitiveColumnMatching_Works()
    {
        var csv = new StringBuilder();
        csv.AppendLine("NUMBER,NAME,DESCRIPTION,TYPE,STATE");
        csv.AppendLine("REQ-001,Test Req,Some description,Functional,Draft");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.Requirement!.RequirementNumber.Should().Be("REQ-001");
        result.Requirement!.Name.Should().Be("Test Req");
        result.Requirement!.Description.Should().Be("Some description");
        result.Requirement!.Type.Should().Be("Functional");
        result.Requirement!.State.Should().Be("Draft");
    }

    [Fact]
    public async Task ParseAsync_MissingNumberField_ReturnsFailure()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,Description");
        csv.AppendLine(",Some Name,Some Description");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("Number");
    }

    [Fact]
    public async Task ParseAsync_Utf8BomEncoding_ParsesCorrectly()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name");
        csv.AppendLine("REQ-001,Test BOM");

        var preamble = Encoding.UTF8.GetPreamble();
        var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var combined = new byte[preamble.Length + csvBytes.Length];
        Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
        Buffer.BlockCopy(csvBytes, 0, combined, preamble.Length, csvBytes.Length);

        using var stream = new MemoryStream(combined);

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].Requirement!.RequirementNumber.Should().Be("REQ-001");
    }

    [Fact]
    public async Task ParseAsync_UnmappedColumns_IgnoredGracefully()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,ExtraColumn1,ExtraColumn2,FooBar");
        csv.AppendLine("REQ-001,Test Req,extra1,extra2,foobar");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].Requirement!.RequirementNumber.Should().Be("REQ-001");
        results[0].Requirement!.Name.Should().Be("Test Req");
    }

    [Fact]
    public async Task ParseAsync_MultipleRows_AllParsed()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Number,Name,Description");
        csv.AppendLine("REQ-001,First,Desc 1");
        csv.AppendLine("REQ-002,Second,Desc 2");
        csv.AppendLine("REQ-003,Third,Desc 3");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

        var results = await _parser.ParseAsync(stream);

        results.Should().HaveCount(3);
        results.All(r => r.Success).Should().BeTrue();
        results[0].Requirement!.RequirementNumber.Should().Be("REQ-001");
        results[1].Requirement!.RequirementNumber.Should().Be("REQ-002");
        results[2].Requirement!.RequirementNumber.Should().Be("REQ-003");
    }
}
