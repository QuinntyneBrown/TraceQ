namespace TraceQ.Core.Models;

public class ImportRecord
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public string RequirementNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Inserted, Updated, Skipped, Error
    public string? ErrorMessage { get; set; }
    public ImportBatch? ImportBatch { get; set; }
}
