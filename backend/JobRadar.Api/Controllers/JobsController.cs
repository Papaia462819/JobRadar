using JobRadar.Api.Contracts;
using JobRadar.Api.Data;
using JobRadar.Api.Domain;
using JobRadar.Api.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(JobDbContext db, IOptions<ScanOptions> scanOptions) : ControllerBase
{
    /// <summary>
    /// List jobs with filters.
    /// source/language: accept one value or a comma-separated list ("Arbeitnow,Greenhouse").
    /// place: "timisoara" | "remote" | "either" — the location shortcut the UI uses.
    /// sort: "newest" (default) | "relevance".
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<JobListResponse>> List(
        [FromQuery] InteractionState? state,
        [FromQuery] string? source,
        [FromQuery] bool? remote,
        [FromQuery] string? language,
        [FromQuery] string? place,
        [FromQuery] string? q,
        [FromQuery] string? sort,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
    {
        var query = db.Jobs.AsNoTracking();

        if (state is not null)
            query = query.Where(j => j.InteractionState == state);

        var sources = SplitList(source);
        if (sources.Count > 0)
            query = query.Where(j => sources.Contains(j.SourceName));

        if (remote is not null)
            query = query.Where(j => j.IsRemote == remote);

        var languages = SplitList(language);
        if (languages.Count > 0)
            query = query.Where(j => languages.Contains(j.Language));

        // Postings older than the configured freshness window stay hidden even
        // if they were ingested before the cutoff existed. Null posted dates
        // pass — FirstSeenAt covers their freshness.
        var postedCutoff = scanOptions.Value.PostedCutoffUtc;
        if (postedCutoff is not null)
            query = query.Where(j => j.PostedDate == null || j.PostedDate >= postedCutoff);

        query = place?.ToLowerInvariant() switch
        {
            // "timi" matches Timișoara and Timisoara (SQLite lower() is
            // ASCII-only, but the prefix we need is plain ASCII).
            "timisoara" => query.Where(j => j.Location.ToLower().Contains("timi")),
            "remote" => query.Where(j => j.IsRemote),
            "either" => query.Where(j => j.IsRemote || j.Location.ToLower().Contains("timi")),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.ToLowerInvariant();
            query = query.Where(j =>
                j.Title.ToLower().Contains(term) ||
                j.Company.ToLower().Contains(term) ||
                j.Description.ToLower().Contains(term));
        }

        // "Newest" means the job's actual posted date; jobs in one scan all share
        // (nearly) the same FirstSeenAt, so sorting by it first made the order
        // within a scan arbitrary — a 2020 posting could land on top.
        query = sort == "relevance"
            ? query.OrderByDescending(j => j.RelevanceScore).ThenByDescending(j => j.FirstSeenAt)
            : query.OrderByDescending(j => j.PostedDate ?? j.FirstSeenAt).ThenByDescending(j => j.FirstSeenAt);

        var total = await query.CountAsync(ct);
        var jobs = await query.Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, 500)).ToListAsync(ct);

        return Ok(new JobListResponse(total, jobs.Select(j => JobDto.From(j, truncateDescription: true)).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobDto>> Get(int id, CancellationToken ct)
    {
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);
        return job is null ? NotFound() : Ok(JobDto.From(job));
    }

    [HttpPatch("{id:int}/state")]
    public async Task<ActionResult<JobDto>> UpdateState(int id, UpdateStateRequest request, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null)
            return NotFound();

        job.InteractionState = request.State;
        await db.SaveChangesAsync(ct);
        return Ok(JobDto.From(job));
    }

    private static List<string> SplitList(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>Marks every currently-new job as notified ("I've seen this report").</summary>
    [HttpPost("mark-notified")]
    public async Task<ActionResult<MarkNotifiedResponse>> MarkNotified(CancellationToken ct)
    {
        var marked = await db.Jobs
            .Where(j => !j.Notified)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Notified, true), ct);
        return Ok(new MarkNotifiedResponse(marked));
    }
}
