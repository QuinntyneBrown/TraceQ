using Microsoft.EntityFrameworkCore;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;
using TraceQ.Core.Utilities;

namespace TraceQ.Infrastructure.Data;

public class RequirementRepository : IRequirementRepository
{
    private readonly TraceQDbContext _context;

    public RequirementRepository(TraceQDbContext context)
    {
        _context = context;
    }

    public async Task<Requirement?> GetByIdAsync(Guid id)
    {
        return await _context.Requirements.FindAsync(id);
    }

    public async Task<Requirement?> GetByNumberAsync(string requirementNumber)
    {
        return await _context.Requirements
            .FirstOrDefaultAsync(r => r.RequirementNumber == requirementNumber);
    }

    public async Task<List<Requirement>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context.Requirements
            .Where(r => idList.Contains(r.Id))
            .ToListAsync();
    }

    public async Task<PaginatedResultDto<RequirementDto>> GetAllAsync(
        string? query = null, int page = 1, int pageSize = 20,
        string? sortBy = null, bool sortDesc = false)
    {
        var queryable = _context.Requirements.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            queryable = queryable.Where(r =>
                r.RequirementNumber.ToLower().Contains(lowerQuery) ||
                r.Name.ToLower().Contains(lowerQuery) ||
                (r.Description != null && r.Description.ToLower().Contains(lowerQuery)));
        }

        queryable = sortBy?.ToLowerInvariant() switch
        {
            "number" => sortDesc
                ? queryable.OrderByDescending(r => r.RequirementNumber)
                : queryable.OrderBy(r => r.RequirementNumber),
            "name" => sortDesc
                ? queryable.OrderByDescending(r => r.Name)
                : queryable.OrderBy(r => r.Name),
            "type" => sortDesc
                ? queryable.OrderByDescending(r => r.Type)
                : queryable.OrderBy(r => r.Type),
            "state" => sortDesc
                ? queryable.OrderByDescending(r => r.State)
                : queryable.OrderBy(r => r.State),
            "priority" => sortDesc
                ? queryable.OrderByDescending(r => r.Priority)
                : queryable.OrderBy(r => r.Priority),
            "modified" => sortDesc
                ? queryable.OrderByDescending(r => r.ModifiedDate)
                : queryable.OrderBy(r => r.ModifiedDate),
            _ => queryable.OrderBy(r => r.RequirementNumber)
        };

        var totalCount = await queryable.CountAsync();

        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return new PaginatedResultDto<RequirementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Requirement> UpsertAsync(Requirement requirement)
    {
        var existing = await GetByNumberAsync(requirement.RequirementNumber);
        if (existing != null)
        {
            existing.Name = requirement.Name;
            existing.Description = requirement.Description;
            existing.Type = requirement.Type;
            existing.State = requirement.State;
            existing.Priority = requirement.Priority;
            existing.Owner = requirement.Owner;
            existing.CreatedDate = requirement.CreatedDate;
            existing.ModifiedDate = requirement.ModifiedDate;
            existing.Module = requirement.Module;
            existing.ParentNumber = requirement.ParentNumber;
            existing.TracedTo = requirement.TracedTo;
            existing.IsEmbedded = requirement.IsEmbedded;
            existing.ImportedAt = requirement.ImportedAt;
            existing.ImportBatchId = requirement.ImportBatchId;
            _context.Requirements.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
        else
        {
            _context.Requirements.Add(requirement);
            await _context.SaveChangesAsync();
            return requirement;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var requirement = await _context.Requirements.FindAsync(id);
        if (requirement != null)
        {
            _context.Requirements.Remove(requirement);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<FacetsDto> GetFacetsAsync()
    {
        var facets = new FacetsDto
        {
            Types = await GetFacetValuesAsync(r => r.Type),
            States = await GetFacetValuesAsync(r => r.State),
            Priorities = await GetFacetValuesAsync(r => r.Priority),
            Modules = await GetFacetValuesAsync(r => r.Module),
            Owners = await GetFacetValuesAsync(r => r.Owner)
        };

        return facets;
    }

    public async Task<List<DistributionDto>> GetDistributionAsync(string field)
    {
        var queryable = _context.Requirements.AsQueryable();

        var groups = field.ToLowerInvariant() switch
        {
            "type" => await queryable.GroupBy(r => r.Type ?? "Unknown")
                .Select(g => new DistributionDto { Label = g.Key, Count = g.Count() })
                .ToListAsync(),
            "state" => await queryable.GroupBy(r => r.State ?? "Unknown")
                .Select(g => new DistributionDto { Label = g.Key, Count = g.Count() })
                .ToListAsync(),
            "priority" => await queryable.GroupBy(r => r.Priority ?? "Unknown")
                .Select(g => new DistributionDto { Label = g.Key, Count = g.Count() })
                .ToListAsync(),
            "module" => await queryable.GroupBy(r => r.Module ?? "Unknown")
                .Select(g => new DistributionDto { Label = g.Key, Count = g.Count() })
                .ToListAsync(),
            "owner" => await queryable.GroupBy(r => r.Owner ?? "Unknown")
                .Select(g => new DistributionDto { Label = g.Key, Count = g.Count() })
                .ToListAsync(),
            _ => new List<DistributionDto>()
        };

        return groups.OrderByDescending(g => g.Count).ToList();
    }

    public async Task<TraceabilityCoverageDto> GetTraceabilityCoverageAsync()
    {
        var total = await _context.Requirements.CountAsync();
        var traced = await _context.Requirements
            .CountAsync(r => r.TracedTo != null && r.TracedTo != string.Empty);

        var untraced = await _context.Requirements
            .Where(r => r.TracedTo == null || r.TracedTo == string.Empty)
            .Select(r => MapToDto(r))
            .ToListAsync();

        // Count distribution of trace links
        var allReqs = await _context.Requirements
            .Where(r => r.TracedTo != null && r.TracedTo != string.Empty)
            .Select(r => r.TracedTo!)
            .ToListAsync();

        var distribution = allReqs
            .Select(t => TraceLinkParser.Parse(t).Count)
            .GroupBy(count => count)
            .Select(g => new DistributionDto { Label = $"{g.Key} link(s)", Count = g.Count() })
            .OrderBy(d => d.Label)
            .ToList();

        return new TraceabilityCoverageDto
        {
            TotalRequirements = total,
            TracedRequirements = traced,
            CoveragePercentage = total > 0 ? (double)traced / total * 100 : 0,
            UntracedRequirements = untraced,
            TraceLinkDistribution = distribution
        };
    }

    public async Task<List<Requirement>> GetUnembeddedAsync()
    {
        return await _context.Requirements
            .Where(r => !r.IsEmbedded)
            .ToListAsync();
    }

    public async Task MarkAsEmbeddedAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        var requirements = await _context.Requirements
            .Where(r => idList.Contains(r.Id))
            .ToListAsync();

        foreach (var req in requirements)
        {
            req.IsEmbedded = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<List<FacetValueDto>> GetFacetValuesAsync(
        System.Linq.Expressions.Expression<Func<Requirement, string?>> selector)
    {
        return await _context.Requirements
            .Select(selector)
            .Where(v => v != null && v != string.Empty)
            .GroupBy(v => v!)
            .Select(g => new FacetValueDto { Value = g.Key, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .ToListAsync();
    }

    private static RequirementDto MapToDto(Requirement r)
    {
        return new RequirementDto
        {
            Id = r.Id,
            RequirementNumber = r.RequirementNumber,
            Name = r.Name,
            Description = r.Description,
            Type = r.Type,
            State = r.State,
            Priority = r.Priority,
            Owner = r.Owner,
            CreatedDate = r.CreatedDate,
            ModifiedDate = r.ModifiedDate,
            Module = r.Module,
            ParentNumber = r.ParentNumber,
            TracedTo = TraceLinkParser.Parse(r.TracedTo),
            IsEmbedded = r.IsEmbedded
        };
    }
}
