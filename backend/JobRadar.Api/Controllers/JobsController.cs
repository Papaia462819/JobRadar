using JobRadar.Api.Contracts;
using JobRadar.Api.Data;
using JobRadar.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(JobDbContext db) : ControllerBase
{
    /// <summary>
    /// List jobs with filters.
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
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(j => j.SourceName == source);
        if (remote is not null)
            query = query.Where(j => j.IsRemote == remote);
        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(j => j.Language == language);

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

        query = sort == "relevance"
            ? query.OrderByDescending(j => j.RelevanceScore).ThenByDescending(j => j.FirstSeenAt)
            : query.OrderByDescending(j => j.FirstSeenAt).ThenByDescending(j => j.PostedDate);

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
