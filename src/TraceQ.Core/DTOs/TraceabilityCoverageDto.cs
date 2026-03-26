namespace TraceQ.Core.DTOs;

public class TraceabilityCoverageDto
{
    public double CoveragePercentage { get; set; }
    public int TotalRequirements { get; set; }
    public int TracedRequirements { get; set; }
    public List<RequirementDto> UntracedRequirements { get; set; } = new();
    public List<DistributionDto> TraceLinkDistribution { get; set; } = new();
}
