namespace TraceQ.Core.DTOs;

public class ImportRecordDto
{
    public string RequirementNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
