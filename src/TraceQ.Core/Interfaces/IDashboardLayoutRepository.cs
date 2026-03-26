using TraceQ.Core.DTOs;

namespace TraceQ.Core.Interfaces;

public interface IDashboardLayoutRepository
{
    Task<List<DashboardLayoutDto>> GetAllAsync();
    Task<DashboardLayoutDto?> GetByIdAsync(Guid id);
    Task<DashboardLayoutDto> SaveAsync(DashboardLayoutDto layout);
    Task DeleteAsync(Guid id);
}
