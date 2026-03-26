namespace TraceQ.Core.Models;

public class DashboardLayout
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LayoutJson { get; set; } = string.Empty; // JSON-serialized widget positions/configs
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
