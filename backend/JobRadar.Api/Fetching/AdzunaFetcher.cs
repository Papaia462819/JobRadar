using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Fetching;

public sealed class AdzunaOptions
{
    public string? AppId { get; set; }
    public string? AppKey { get; set; }
    public string Country { get; set; } = "ro";
    public string[] Queries { get; set; } = [".net developer"];
    public string? Where { get; set; }
    public int ResultsPerPage { get; set; } = 50;
    public int MaxPagesPerQuery { get; set; } = 1;
}

/// <summary>
/// Adzuna search API — https://developer.adzuna.com (free app_id/app_key).
/// Missing credentials are not an error: the fetcher logs a warning and
/// returns nothing so the rest of the pipeline keeps working.
/// </summary>
public sealed class AdzunaFetcher(
    HttpClient http,
    IOptions<AdzunaOptions> options,
    ILogger<AdzunaFetcher> logger) : IJobFetcher
{
    public string SourceName => "Adzuna";

    public async Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.AppId) || string.IsNullOrWhiteSpace(opts.AppKey))
        {
            logger.LogWarning(
                "Adzuna credentials missing — skipping this source. " +
                "Get free keys at https://developer.adzuna.com and set Adzuna:AppId / Adzuna:AppKey " +
                "in appsettings.json (or Adzuna__AppId / Adzuna__AppKey environment variables).");
            return [];
        }

        var jobs = new List<RawJob>();

        foreach (var query in opts.Queries)
        {
            for (var page = 1; page <= Math.Max(1, opts.MaxPagesPerQuery); page++)
            {
                try
                {
                    var url =
                        $"https://api.adzuna.com/v1/api/jobs/{opts.Country}/search/{page}" +
                        $"?app_id={Uri.EscapeDataString(opts.AppId)}" +
                        $"&app_key={Uri.EscapeDataString(opts.AppKey)}" +
                        $"&results_per_page={opts.ResultsPerPage}" +
                        $"&what={Uri.EscapeDataString(query)}" +
                        "&content-type=application/json";

                    if (!string.IsNullOrWhiteSpace(opts.Where))
                        url += $"&where={Uri.EscapeDataString(opts.Where)}";

                    var response = await http.GetFromJsonWithRetryAsync<AdzunaResponse>(
                        url, $"Adzuna '{query}' page {page}", null, logger, ct);
                    var results = response?.Results ?? [];

                    foreach (var item in results)
                    {
                        if (item.Id is null || item.Title is null || item.RedirectUrl is null)
                            continue;

                        jobs.Add(new RawJob(
                            SourceName: SourceName,
                            ExternalId: item.Id,
                            Title: item.Title,
                            Company: item.Company?.DisplayName ?? "Unknown",
                            Location: item.Location?.DisplayName,
                            Url: item.RedirectUrl,
                            Description: item.Description,
                            PostedDate: item.Created,
                            RemoteHint: null));
                    }

                    if (results.Count < opts.ResultsPerPage)
                        break; // no further pages for this query
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One failing query/page shouldn't lose results already gathered.
                    logger.LogError(ex, "Adzuna query '{Query}' page {Page} failed", query, page);
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct); // politeness delay
            }
        }

        logger.LogInformation("Adzuna: fetched {Count} jobs across {Queries} queries",
            jobs.Count, opts.Queries.Length);
        return jobs;
    }

    private sealed record AdzunaResponse(
        [property: JsonPropertyName("results")] List<AdzunaJob>? Results);

    private sealed record AdzunaJob(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("redirect_url")] string? RedirectUrl,
        [property: JsonPropertyName("created")] DateTime? Created,
        [property: JsonPropertyName("company")] AdzunaCompany? Company,
        [property: JsonPropertyName("location")] AdzunaLocation? Location);

    private sealed record AdzunaCompany(
        [property: JsonPropertyName("display_name")] string? DisplayName);

    private sealed record AdzunaLocation(
        [property: JsonPropertyName("display_name")] string? DisplayName);
}
