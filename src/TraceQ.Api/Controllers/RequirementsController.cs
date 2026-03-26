using Microsoft.AspNetCore.Mvc;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequirementsController : ControllerBase
{
    private readonly IRequirementRepository _requirementRepository;
    private readonly IVectorStore _vectorStore;
    private readonly ISearchService _searchService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RequirementsController> _logger;

    public RequirementsController(
        IRequirementRepository requirementRepository,
        IVectorStore vectorStore,
        ISearchService searchService,
        IAuditService auditService,
        ILogger<RequirementsController> logger)
    {
        _requirementRepository = requirementRepository;
        _vectorStore = vectorStore;
        _searchService = searchService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("facets")]
    public async Task<IActionResult> GetFacets()
    {
        var facets = await _requirementRepository.GetFacetsAsync();
        return Ok(facets);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _requirementRepository.GetAllAsync(q, page, pageSize, sortBy, sortDesc);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var requirement = await _requirementRepository.GetByIdAsync(id);
        if (requirement == null)
        {
            return NotFound(new { error = $"Requirement with ID {id} not found." });
        }

        return Ok(MapToDto(requirement));
    }

    [HttpGet("{id:guid}/similar")]
    public async Task<IActionResult> GetSimilar(Guid id, [FromQuery] int top = 10)
    {
        if (top < 1 || top > 100)
        {
            top = 10;
        }

        try
        {
            var results = await _searchService.FindSimilarAsync(id, top);
            return Ok(results);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Requirement with ID {id} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar requirements for {Id}", id);
            return StatusCode(500, new { error = "An error occurred finding similar requirements." });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var requirement = await _requirementRepository.GetByIdAsync(id);
        if (requirement == null)
        {
            return NotFound(new { error = $"Requirement with ID {id} not found." });
        }

        // Delete from both SQLite and vector store
        await _requirementRepository.DeleteAsync(id);
        await _vectorStore.DeleteAsync(id);

        // Log deletion to audit trail
        await _auditService.LogDeletionAsync(requirement.RequirementNumber);

        return NoContent();
    }

    private static Core.DTOs.RequirementDto MapToDto(Core.Models.Requirement r)
    {
        return new Core.DTOs.RequirementDto
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
            TracedTo = string.IsNullOrEmpty(r.TracedTo)
                ? new List<string>()
                : r.TracedTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            IsEmbedded = r.IsEmbedded
        };
    }
}
