using Microsoft.AspNetCore.Mvc;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, IAuditService auditService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Search query cannot be empty." });
        }

        if (request.Top < 1 || request.Top > 100)
        {
            return BadRequest(new { error = "Top must be between 1 and 100." });
        }

        try
        {
            var results = await _searchService.SemanticSearchAsync(request);

            // Log search to audit trail
            await _auditService.LogSearchAsync(request.Query, results.Count);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during semantic search for query: {Query}", request.Query);
            return StatusCode(500, new { error = "An error occurred during search." });
        }
    }
}
