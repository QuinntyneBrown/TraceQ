namespace TraceQ.Core.Models;

public class ImportBatch
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public ICollection<ImportRecord> Records { get; set; } = new List<ImportRecord>();
}
