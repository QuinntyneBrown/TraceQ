namespace TraceQ.Core.DTOs;

public class SimilarityClusterDto
{
    public int ClusterId { get; set; }
    public List<ClusterMemberDto> Members { get; set; } = new();
}
