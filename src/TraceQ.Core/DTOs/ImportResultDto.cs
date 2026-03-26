namespace TraceQ.Core.DTOs;

public class ImportResultDto
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
}
