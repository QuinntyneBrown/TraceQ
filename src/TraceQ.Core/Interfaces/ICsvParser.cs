using TraceQ.Core.Models;

namespace TraceQ.Core.Interfaces;

public interface ICsvParser
{
    Task<List<RequirementParseResult>> ParseAsync(Stream csvStream);
}

public class RequirementParseResult
{
    public Requirement? Requirement { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string RequirementNumber { get; set; } = string.Empty;
}
