namespace TraceQ.Core.DTOs;

public class ImportBatchDetailDto : ImportBatchDto
{
    public List<ImportRecordDto> Records { get; set; } = new();
}
