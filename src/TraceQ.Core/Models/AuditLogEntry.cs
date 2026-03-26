namespace TraceQ.Core.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty; // Import, Search, Delete
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
