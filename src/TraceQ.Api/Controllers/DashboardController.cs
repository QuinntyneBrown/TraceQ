using Microsoft.AspNetCore.Mvc;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardLayoutRepository _layoutRepository;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardLayoutRepository layoutRepository, ILogger<DashboardController> logger)
    {
        _layoutRepository = layoutRepository;
        _logger = logger;
    }

    [HttpGet("layouts")]
    public async Task<IActionResult> GetLayouts()
    {
        var layouts = await _layoutRepository.GetAllAsync();
        return Ok(layouts);
    }

    [HttpPost("layouts")]
    public async Task<IActionResult> SaveLayout([FromBody] DashboardLayoutDto layout)
    {
        if (string.IsNullOrWhiteSpace(layout.Name))
        {
            return BadRequest(new { error = "Layout name is required." });
        }

        if (string.IsNullOrWhiteSpace(layout.LayoutJson))
        {
            return BadRequest(new { error = "Layout JSON is required." });
        }

        var saved = await _layoutRepository.SaveAsync(layout);
        return CreatedAtAction(nameof(GetLayouts), new { id = saved.Id }, saved);
    }

    [HttpDelete("layouts/{id:guid}")]
    public async Task<IActionResult> DeleteLayout(Guid id)
    {
        var existing = await _layoutRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { error = $"Layout with ID {id} not found." });
        }

        await _layoutRepository.DeleteAsync(id);
        return NoContent();
    }
}
