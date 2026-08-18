using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Fetching;

public sealed class GreenhouseOptions
{
    /// <summary>Board tokens — the {token} in boards-api.greenhouse.io/v1/boards/{token}/jobs.</summary>
    public string[] Boards { get; set; } = [];

    /// <summary>Keep only software-looking titles (these boards also post sales/legal/HR roles).</summary>
    public bool TechFilter { get; set; } = true;
}

/// <summary>
/// Greenhouse public job-board API (free, no auth):
///   GET https://boards-api.greenhouse.io/v1/boards/{board}/jobs?content=true
/// Iterates the configured boards; a failing board is logged and skipped so it
/// never affects the other boards — the same isolation principle the pipeline
/// applies between whole fetchers.
/// </summary>
public sealed class GreenhouseFetcher(
    HttpClient http,
    IOptions<GreenhouseOptions> options,
    ILogger<GreenhouseFetcher> logger) : IJobFetcher
{
    // Same broad "is this a software job at all" net as the Arbeitnow fetcher —
    // relevance scoring against the profile happens later in the pipeline.
    private static readonly string[] TechTerms =
    [
        "developer", "engineer", "software", "programmer", "devops", ".net", "dotnet",
        "c#", "java", "python", "php", "javascript", "typescript", "react", "angular",
        "vue", "node", "backend", "back-end", "frontend", "front-end", "full stack",
        "fullstack", "full-stack", "qa", "sre", "data", "cloud", "mobile", "android",
        "ios", "web", "security", "infrastructure", "platform"
    ];

    public string SourceName => "Greenhouse";

    public async Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct)
    {
        var opts = options.Value;
        if (opts.Boards.Length == 0)
        {
            logger.LogWarning("Greenhouse: no boards configured (Greenhouse:Boards) — skipping this source.");
            return [];
        }

        var jobs = new List<RawJob>();

        foreach (var board in opts.Boards)
        {
            try
            {
                var response = await http.GetFromJsonWithRetryAsync<GreenhouseResponse>(
                    $"https://boards-api.greenhouse.io/v1/boards/{Uri.EscapeDataString(board)}/jobs?content=true",
                    $"Greenhouse[{board}]", null, logger, ct);

                var kept = 0;
                foreach (var item in response?.Jobs ?? [])
                {
                    if (item.Id == 0 || item.Title is null || item.AbsoluteUrl is null)
                        continue;
                    if (opts.TechFilter &&
                        !TechTerms.Any(t => item.Title.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    jobs.Add(new RawJob(
                        SourceName: SourceName,
                        ExternalId: item.Id.ToString(),
                        Title: item.Title,
                        Company: item.CompanyName ?? board,
                        Location: item.Location?.Name,
                        Url: item.AbsoluteUrl,
                        // `content` is HTML-*encoded* HTML (&lt;p&gt;…). One decode
                        // turns it into real HTML; the normalizer strips the tags.
                        Description: WebUtility.HtmlDecode(item.Content),
                        PostedDate: (item.FirstPublished ?? item.UpdatedAt)?.UtcDateTime,
                        RemoteHint: null)); // derived from location text ("Remote, EMEA" etc.)
                    kept++;
                }

                logger.LogInformation("Greenhouse[{Board}]: kept {Kept} tech jobs", board, kept);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One broken board must not stop the remaining boards.
                logger.LogError(ex, "Greenhouse board '{Board}' failed — continuing with the rest", board);
            }

            await Task.Delay(500, ct); // politeness between boards
        }

        return jobs;
    }

    private sealed record GreenhouseResponse(
        [property: JsonPropertyName("jobs")] List<GreenhouseJob>? Jobs);

    private sealed record GreenhouseJob(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("company_name")] string? CompanyName,
        [property: JsonPropertyName("absolute_url")] string? AbsoluteUrl,
        [property: JsonPropertyName("location")] GreenhouseLocation? Location,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("first_published")] DateTimeOffset? FirstPublished,
        [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt);

    private sealed record GreenhouseLocation(
        [property: JsonPropertyName("name")] string? Name);
}
