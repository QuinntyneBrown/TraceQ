namespace TraceQ.Core.DTOs;

public class RequirementDto
{
    public Guid Id { get; set; }
    public string RequirementNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? State { get; set; }
    public string? Priority { get; set; }
    public string? Owner { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? Module { get; set; }
    public string? ParentNumber { get; set; }
    public List<string> TracedTo { get; set; } = new();
    public bool IsEmbedded { get; set; }
}
