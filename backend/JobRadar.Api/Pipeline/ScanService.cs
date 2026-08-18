using System.Text.Json;
using JobRadar.Api.Data;
using JobRadar.Api.Domain;
using JobRadar.Api.Fetching;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Api.Pipeline;

public sealed class SourceScanResult
{
    public required string Source { get; init; }
    public int Fetched { get; set; }
    public int New { get; set; }
    public string? Error { get; set; }
}

public sealed class ScanSummary
{
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; set; }
    public List<SourceScanResult> Sources { get; init; } = [];
    public int TotalFetched => Sources.Sum(s => s.Fetched);
    public int TotalNew => Sources.Sum(s => s.New);
}

public sealed class ScanInProgressException() : Exception("A scan is already running.");

/// <summary>
/// The core pipeline: run every registered fetcher (failures isolated),
/// normalize, dedup, upsert, record the run. Used by both POST /api/scan
/// and the Hangfire daily job.
/// </summary>
public sealed class ScanService(
    IEnumerable<IJobFetcher> fetchers,
    JobNormalizer normalizer,
    JobDbContext db,
    Microsoft.Extensions.Options.IOptions<ScanOptions> scanOptions,
    ILogger<ScanService> logger)
{
    // One scan at a time, across manual + scheduled triggers.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>Full scan — this exact signature is what Hangfire's recurring job calls.</summary>
    public Task<ScanSummary> RunScanAsync(CancellationToken ct) => RunScanAsync(null, ct);

    /// <summary>
    /// Scan with an optional single-source filter (POST /api/scan?source=eJobs)
    /// so a new adapter can be tested in isolation.
    /// </summary>
    public async Task<ScanSummary> RunScanAsync(string? onlySource, CancellationToken ct)
    {
        if (!await Gate.WaitAsync(TimeSpan.Zero, ct))
            throw new ScanInProgressException();

        try
        {
            return await RunCoreAsync(onlySource, ct);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<ScanSummary> RunCoreAsync(string? onlySource, CancellationToken ct)
    {
        var selected = fetchers
            .Where(f => onlySource is null || f.SourceName.Equals(onlySource, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (selected.Count == 0)
            throw new ArgumentException(
                $"No fetcher named '{onlySource}'. Available: {string.Join(", ", fetchers.Select(f => f.SourceName))}");

        var summary = new ScanSummary { StartedAt = DateTime.UtcNow };
        var postedCutoff = scanOptions.Value.PostedCutoffUtc;
        var skippedOld = 0;

        // hash → candidate; first source to produce a hash wins, duplicates
        // (within this run or across sources) collapse here.
        var candidates = new Dictionary<string, Job>();

        foreach (var fetcher in selected)
        {
            var result = new SourceScanResult { Source = fetcher.SourceName };
            summary.Sources.Add(result);

            try
            {
                var rawJobs = await fetcher.FetchAsync(ct);
                result.Fetched = rawJobs.Count;

                foreach (var raw in rawJobs)
                {
                    var job = normalizer.Normalize(raw);

                    // Stale postings (some boards leave years-old jobs up) never
                    // enter the DB. Null posted dates pass — FirstSeenAt covers them.
                    if (postedCutoff is not null && job.PostedDate < postedCutoff)
                    {
                        skippedOld++;
                        continue;
                    }

                    candidates.TryAdd(job.DedupHash, job);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // deliberate shutdown/cancel — don't swallow
            }
            catch (Exception ex)
            {
                // A broken source must never take down the others.
                result.Error = ex.Message;
                logger.LogError(ex, "Fetcher {Source} failed", fetcher.SourceName);
            }
        }

        // Which candidates already exist? (chunked: SQLite caps parameters at 999)
        var existingHashes = new HashSet<string>();
        foreach (var chunk in candidates.Keys.Chunk(500))
        {
            var found = await db.Jobs
                .Where(j => chunk.Contains(j.DedupHash))
                .Select(j => j.DedupHash)
                .ToListAsync(ct);
            existingHashes.UnionWith(found);
        }

        var newJobs = candidates.Values
            .Where(j => !existingHashes.Contains(j.DedupHash))
            .ToList();

        db.Jobs.AddRange(newJobs);

        foreach (var group in newJobs.GroupBy(j => j.SourceName))
        {
            var source = summary.Sources.FirstOrDefault(s => s.Source == group.Key);
            if (source is not null)
                source.New = group.Count();
        }

        summary.FinishedAt = DateTime.UtcNow;

        db.ScanRuns.Add(new ScanRun
        {
            StartedAt = summary.StartedAt,
            FinishedAt = summary.FinishedAt,
            TotalFetched = summary.TotalFetched,
            TotalNew = summary.TotalNew,
            TotalErrors = summary.Sources.Count(s => s.Error is not null),
            DetailsJson = JsonSerializer.Serialize(summary.Sources)
        });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Scan finished: {Fetched} fetched, {New} new, {SkippedOld} skipped as too old, {Errors} source error(s)",
            summary.TotalFetched, summary.TotalNew, skippedOld, summary.Sources.Count(s => s.Error is not null));

        return summary;
    }
}
