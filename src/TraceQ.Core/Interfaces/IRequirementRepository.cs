using TraceQ.Core.DTOs;
using TraceQ.Core.Models;

namespace TraceQ.Core.Interfaces;

public interface IRequirementRepository
{
    Task<Requirement?> GetByIdAsync(Guid id);
    Task<Requirement?> GetByNumberAsync(string requirementNumber);
    Task<List<Requirement>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<PaginatedResultDto<RequirementDto>> GetAllAsync(string? query = null, int page = 1, int pageSize = 20, string? sortBy = null, bool sortDesc = false);
    Task<Requirement> UpsertAsync(Requirement requirement);
    Task DeleteAsync(Guid id);
    Task<FacetsDto> GetFacetsAsync();
    Task<List<DistributionDto>> GetDistributionAsync(string field);
    Task<TraceabilityCoverageDto> GetTraceabilityCoverageAsync();
    Task<List<Requirement>> GetUnembeddedAsync();
    Task MarkAsEmbeddedAsync(IEnumerable<Guid> ids);
}
