using Microsoft.AspNetCore.Mvc;
using TraceQ.Core.Interfaces;

namespace TraceQ.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ILogger<ImportController> _logger;

    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    public ImportController(IImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    [HttpPost("csv")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded." });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { error = "File size exceeds the 50 MB limit." });
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".csv")
        {
            return BadRequest(new { error = "Only .csv files are accepted." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(stream, file.FileName);

            if (result.ErrorCount > 0 && result.InsertedCount == 0 && result.UpdatedCount == 0 && result.SkippedCount == 0)
            {
                return UnprocessableEntity(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import of file {FileName}", file.FileName);
            return UnprocessableEntity(new { error = "Failed to parse CSV file.", details = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _importService.GetHistoryAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{batchId:guid}")]
    public async Task<IActionResult> GetBatchDetail(Guid batchId)
    {
        var result = await _importService.GetBatchDetailAsync(batchId);
        if (result == null)
        {
            return NotFound(new { error = $"Import batch {batchId} not found." });
        }

        return Ok(result);
    }
}
