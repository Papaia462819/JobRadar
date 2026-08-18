using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Fetching;

// ============================================================================
// eJobs.ro scraping adapter — THE FRAGILE ONE. READ THIS WHEN IT BREAKS.
//
// eJobs is a Nuxt 3 SPA. The job cards in the server-rendered HTML are only
// hydration placeholders (title + company, no link/location), so the usable
// data lives in the SSR state payload embedded in the page. Everything this
// fetcher depends on, verified 2026-07-29:
//
//   CSS selector (AngleSharp):  script#__NUXT_DATA__
//   Layout-change canary:       .job-card-wrapper   (should be ~18-40 per page)
//   Payload format:             Nuxt "devalue" JSON — a flat ARRAY where every
//                               object property value is an INDEX into that
//                               same array (see Deref()).
//   Job objects in payload:     dicts with keys: id, title, slug, company
//                               (+ creationDate, locations, salary, …)
//   Job URL template:           https://www.ejobs.ro/user/locuri-de-munca/{slug}/{id}
//   Search pages (config):      https://www.ejobs.ro/locuri-de-munca/{search}
//                               e.g. "timisoara/it-software", "remote/it-software"
//                               (the path segment scopes city/remote — that is
//                               also how this fetcher knows the job's location,
//                               since per-job locations are only city-id refs)
//   Pagination:                 …/pagina{N}; past the last page the site
//                               replies 302 (redirects are disabled on this
//                               HttpClient, so non-200 == stop paging).
//   No description snippet exists on the results page (placeholders only), so
//   Description stays empty on first pass — deliberate: no deep-crawling of
//   detail pages. Relevance is scored from the title.
//
// If this fetcher logs "layout may have changed" and returns nothing: open a
// search URL, view source, and check the selector/keys above one by one.
// ============================================================================

public sealed class EjobsOptions
{
    /// <summary>Path segments after ejobs.ro/locuri-de-munca/ — city/remote scoped searches.</summary>
    public string[] Searches { get; set; } = ["timisoara/it-software", "remote/it-software"];

    public int MaxPagesPerSearch { get; set; } = 2;
}

public sealed class EjobsFetcher(
    HttpClient http,
    IOptions<EjobsOptions> options,
    ILogger<EjobsFetcher> logger) : IJobFetcher
{
    private const string BaseUrl = "https://www.ejobs.ro/locuri-de-munca/";

    public string SourceName => "eJobs";

    public async Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct)
    {
        var opts = options.Value;
        var jobs = new List<RawJob>();
        var parser = new HtmlParser();

        foreach (var search in opts.Searches)
        {
            try
            {
                for (var page = 1; page <= Math.Max(1, opts.MaxPagesPerSearch); page++)
                {
                    var url = BaseUrl + search.Trim('/') + (page > 1 ? $"/pagina{page}" : "");
                    using var response = await http.GetAsync(url, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        // Past the last page the site answers 302 — normal. But a
                        // non-200 on page 1 means blocked/rate-limited/moved: say so.
                        if (page == 1)
                            logger.LogWarning(
                                "eJobs: {Url} answered HTTP {Status} — rate-limited, bot-checked or the URL scheme " +
                                "changed. Skipping this search for this run.", url, (int)response.StatusCode);
                        break;
                    }

                    var html = await response.Content.ReadAsStringAsync(ct);
                    var found = ParsePage(parser, html, search);

                    if (found.Count == 0)
                    {
                        // Loud but contained: never throw layout problems up into the pipeline.
                        if (page == 1)
                            logger.LogWarning(
                                "eJobs: no job data found at {Url} — the eJobs layout may have changed. " +
                                "Check the selector notes at the top of EjobsFetcher.cs.", url);
                        break;
                    }

                    jobs.AddRange(found);
                    logger.LogInformation("eJobs[{Search}] page {Page}: {Count} jobs", search, page, found.Count);

                    await Task.Delay(900, ct); // politeness between page requests
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One failing search scope must not stop the others.
                logger.LogError(ex, "eJobs search '{Search}' failed — continuing with the rest", search);
            }
        }

        return jobs;
    }

    private List<RawJob> ParsePage(HtmlParser parser, string html, string search)
    {
        var results = new List<RawJob>();

        var document = parser.ParseDocument(html);
        var stateScript = document.QuerySelector("script#__NUXT_DATA__");
        if (stateScript is null)
            return results;

        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(stateScript.TextContent);
        }
        catch (JsonException)
        {
            return results;
        }

        using (payload)
        {
            if (payload.RootElement.ValueKind != JsonValueKind.Array)
                return results;

            // Materialize once for O(1) index lookups (JsonElement[i] is O(i)).
            var nodes = payload.RootElement.EnumerateArray().ToArray();

            // The search path itself tells us where the job is (see header note).
            var scope = search.Split('/')[0].ToLowerInvariant();
            var (location, remoteHint) = scope switch
            {
                "remote" => ("Remote, România", (bool?)true),
                "timisoara" => ("Timișoara, România", (bool?)null),
                _ => (scope, (bool?)null),
            };

            foreach (var node in nodes)
            {
                if (node.ValueKind != JsonValueKind.Object ||
                    !node.TryGetProperty("id", out var idRef) ||
                    !node.TryGetProperty("title", out var titleRef) ||
                    !node.TryGetProperty("slug", out var slugRef) ||
                    !node.TryGetProperty("company", out var companyRef))
                    continue;

                if (Deref(nodes, idRef) is not { ValueKind: JsonValueKind.Number } idEl ||
                    Deref(nodes, titleRef) is not { ValueKind: JsonValueKind.String } titleEl ||
                    Deref(nodes, slugRef) is not { ValueKind: JsonValueKind.String } slugEl)
                    continue;

                var companyName = "Unknown";
                if (Deref(nodes, companyRef) is { ValueKind: JsonValueKind.Object } companyEl &&
                    companyEl.TryGetProperty("name", out var nameRef) &&
                    Deref(nodes, nameRef) is { ValueKind: JsonValueKind.String } nameEl)
                    companyName = nameEl.GetString()!;

                DateTime? posted = null;
                if (node.TryGetProperty("creationDate", out var createdRef) &&
                    Deref(nodes, createdRef) is { ValueKind: JsonValueKind.String } createdEl &&
                    DateTimeOffset.TryParse(createdEl.GetString(), out var createdAt))
                    posted = createdAt.UtcDateTime;

                var id = idEl.GetInt64();
                results.Add(new RawJob(
                    SourceName: SourceName,
                    ExternalId: id.ToString(),
                    Title: titleEl.GetString()!,
                    Company: companyName,
                    Location: location,
                    Url: $"https://www.ejobs.ro/user/locuri-de-munca/{slugEl.GetString()}/{id}",
                    Description: null, // results page carries no snippet — see header
                    PostedDate: posted,
                    RemoteHint: remoteHint));
            }
        }

        return results;
    }

    /// <summary>
    /// Devalue lookup: object property values in __NUXT_DATA__ are indices into
    /// the flat payload array; returns the element they point at.
    /// </summary>
    private static JsonElement? Deref(JsonElement[] nodes, JsonElement indexRef)
        => indexRef.ValueKind == JsonValueKind.Number &&
           indexRef.TryGetInt32(out var i) && i >= 0 && i < nodes.Length
            ? nodes[i]
            : null;
}
