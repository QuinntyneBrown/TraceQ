using FluentAssertions;
using TraceQ.Core.Utilities;

namespace TraceQ.Core.Tests.Utilities;

public class TraceLinkParserTests
{
    [Fact]
    public void Parse_SemicolonDelimitedLinks_ReturnsDistinctLinks()
    {
        var links = TraceLinkParser.Parse("REQ-012;REQ-013");

        links.Should().Equal("REQ-012", "REQ-013");
    }

    [Fact]
    public void Normalize_MixedDelimiters_ReturnsCanonicalCommaSeparatedValue()
    {
        var normalized = TraceLinkParser.Normalize("REQ-001; REQ-002,REQ-003");

        normalized.Should().Be("REQ-001,REQ-002,REQ-003");
    }
}
