using TraceQ.Core.DTOs;

namespace TraceQ.Core.Interfaces;

public interface IImportService
{
    Task<ImportResultDto> ImportAsync(Stream csvStream, string fileName);
    Task<PaginatedResultDto<ImportBatchDto>> GetHistoryAsync(int page = 1, int pageSize = 20);
    Task<ImportBatchDetailDto?> GetBatchDetailAsync(Guid batchId);
}
