using System.Text.Json;
using JobRadar.Api.Contracts;
using JobRadar.Api.Data;
using JobRadar.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController(JobDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatsDto>> Get(CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var stateCountsRaw = await db.Jobs
            .GroupBy(j => j.InteractionState)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var stateCounts = Enum.GetValues<InteractionState>().ToDictionary(
            s => JsonNamingPolicy.CamelCase.ConvertName(s.ToString()),
            s => stateCountsRaw.FirstOrDefault(c => c.State == s)?.Count ?? 0);

        var perSource = await db.Jobs
            .GroupBy(j => j.SourceName)
            .Select(g => new SourceStats(
                g.Key,
                g.Count(),
                g.Count(j => !j.Notified)))
            .ToListAsync(ct);

        var lastRun = await db.ScanRuns
            .AsNoTracking()
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefaultAsync(ct);

        return Ok(new StatsDto(
            TotalJobs: await db.Jobs.CountAsync(ct),
            NewCount: await db.Jobs.CountAsync(j => !j.Notified, ct),
            NewToday: await db.Jobs.CountAsync(j => j.FirstSeenAt >= todayUtc, ct),
            Saved: stateCounts["saved"],
            Applied: stateCounts["applied"],
            StateCounts: stateCounts,
            PerSource: perSource,
            LastScan: lastRun is null
                ? null
                : new LastScanDto(lastRun.StartedAt, lastRun.FinishedAt,
                    lastRun.TotalFetched, lastRun.TotalNew, lastRun.TotalErrors)));
    }
}
