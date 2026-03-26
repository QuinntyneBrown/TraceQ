namespace TraceQ.Core.DTOs;

public class SearchResultDto
{
    public RequirementDto Requirement { get; set; } = new();
    public float SimilarityScore { get; set; }
}
