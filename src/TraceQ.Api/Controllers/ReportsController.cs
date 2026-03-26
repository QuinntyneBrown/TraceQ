using Microsoft.AspNetCore.Mvc;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IRequirementRepository _requirementRepository;
    private readonly ISearchService _searchService;
    private readonly ILogger<ReportsController> _logger;

    private static readonly HashSet<string> ValidDistributionFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type", "state", "priority", "module", "owner"
    };

    public ReportsController(
        IRequirementRepository requirementRepository,
        ISearchService searchService,
        ILogger<ReportsController> logger)
    {
        _requirementRepository = requirementRepository;
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet("distribution/{field}")]
    public async Task<IActionResult> GetDistribution(string field)
    {
        if (!ValidDistributionFields.Contains(field))
        {
            return BadRequest(new { error = $"Invalid field '{field}'. Valid fields are: type, state, priority, module, owner." });
        }

        var distribution = await _requirementRepository.GetDistributionAsync(field);
        return Ok(distribution);
    }

    [HttpGet("traceability")]
    public async Task<IActionResult> GetTraceability()
    {
        var coverage = await _requirementRepository.GetTraceabilityCoverageAsync();
        return Ok(coverage);
    }

    [HttpGet("similarity-clusters")]
    public async Task<IActionResult> GetSimilarityClusters([FromQuery] float threshold = 0.85f)
    {
        if (threshold < 0 || threshold > 1)
        {
            return BadRequest(new { error = "Threshold must be between 0 and 1." });
        }

        try
        {
            var clusters = await _searchService.GetSimilarityClustersAsync(threshold);
            return Ok(clusters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similarity clusters with threshold {Threshold}", threshold);
            return StatusCode(500, new { error = "An error occurred computing similarity clusters." });
        }
    }
}
