using JobRadar.Api.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.Api.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController(ScanService scanService) : ControllerBase
{
    /// <summary>
    /// Trigger a fetch run immediately; returns the per-source summary.
    /// Optional ?source=Greenhouse runs a single fetcher in isolation (testing).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ScanSummary>> Scan([FromQuery] string? source, CancellationToken ct)
    {
        try
        {
            return Ok(await scanService.RunScanAsync(source, ct));
        }
        catch (ScanInProgressException)
        {
            return Conflict(new { message = "A scan is already running. Try again in a moment." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
