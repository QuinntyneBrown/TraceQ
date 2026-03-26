using Microsoft.EntityFrameworkCore;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;

namespace TraceQ.Infrastructure.Data;

public class DashboardLayoutRepository : IDashboardLayoutRepository
{
    private readonly TraceQDbContext _context;

    public DashboardLayoutRepository(TraceQDbContext context)
    {
        _context = context;
    }

    public async Task<List<DashboardLayoutDto>> GetAllAsync()
    {
        return await _context.DashboardLayouts
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    public async Task<DashboardLayoutDto?> GetByIdAsync(Guid id)
    {
        var layout = await _context.DashboardLayouts.FindAsync(id);
        return layout != null ? MapToDto(layout) : null;
    }

    public async Task<DashboardLayoutDto> SaveAsync(DashboardLayoutDto dto)
    {
        var now = DateTime.UtcNow;

        if (dto.Id != Guid.Empty)
        {
            var existing = await _context.DashboardLayouts.FindAsync(dto.Id);
            if (existing != null)
            {
                existing.Name = dto.Name;
                existing.LayoutJson = dto.LayoutJson;
                existing.UpdatedAt = now;
                _context.DashboardLayouts.Update(existing);
                await _context.SaveChangesAsync();
                return MapToDto(existing);
            }
        }

        var layout = new DashboardLayout
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            LayoutJson = dto.LayoutJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.DashboardLayouts.Add(layout);
        await _context.SaveChangesAsync();
        return MapToDto(layout);
    }

    public async Task DeleteAsync(Guid id)
    {
        var layout = await _context.DashboardLayouts.FindAsync(id);
        if (layout != null)
        {
            _context.DashboardLayouts.Remove(layout);
            await _context.SaveChangesAsync();
        }
    }

    private static DashboardLayoutDto MapToDto(DashboardLayout layout)
    {
        return new DashboardLayoutDto
        {
            Id = layout.Id,
            Name = layout.Name,
            LayoutJson = layout.LayoutJson,
            CreatedAt = layout.CreatedAt,
            UpdatedAt = layout.UpdatedAt
        };
    }
}
