namespace TraceQ.Core.DTOs;

public class ClusterMemberDto
{
    public RequirementDto Requirement { get; set; } = new();
    public Dictionary<string, float> PairwiseScores { get; set; } = new(); // key: other requirement number
}
