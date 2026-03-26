namespace TraceQ.Core.DTOs;

public class DashboardLayoutDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LayoutJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
