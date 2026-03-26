using TraceQ.Core.DTOs;
using TraceQ.Core.Models;

namespace TraceQ.Core.Interfaces;

public interface IAuditService
{
    Task LogImportAsync(string fileName, int insertedCount, int updatedCount, int errorCount);
    Task LogSearchAsync(string queryText, int resultCount);
    Task LogDeletionAsync(string requirementNumber);
    Task<PaginatedResultDto<AuditLogEntry>> GetLogsAsync(int page = 1, int pageSize = 50, string? eventType = null);
}
