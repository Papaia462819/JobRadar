using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Fetching;

public sealed class ArbeitnowOptions
{
    public int MaxPages { get; set; } = 3;

    /// <summary>
    /// When true, keep only listings that look like software/IT jobs — the
    /// Arbeitnow feed is EU-wide and full of unrelated roles.
    /// </summary>
    public bool TechFilter { get; set; } = true;
}

/// <summary>
/// Arbeitnow job board API — free, no auth: https://www.arbeitnow.com/api/job-board-api
/// </summary>
public sealed partial class ArbeitnowFetcher(
    HttpClient http,
    IOptions<ArbeitnowOptions> options,
    ILogger<ArbeitnowFetcher> logger) : IJobFetcher
{
    private const string Endpoint = "https://www.arbeitnow.com/api/job-board-api";

    // Broad "is this a software job at all" net; relevance scoring happens later.
    private static readonly string[] TechTerms =
    [
        "developer", "engineer", "software", "programmer", "devops", ".net", "dotnet",
        "c#", "java", "python", "php", "javascript", "typescript", "react", "angular",
        "vue", "node", "backend", "back-end", "frontend", "front-end", "full stack",
        "fullstack", "full-stack", "qa", "tester", "testing", "data", "cloud", "sre",
        "mobile", "android", "ios", "web"
    ];

    public string SourceName => "Arbeitnow";

    public async Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct)
    {
        var opts = options.Value;
        var jobs = new List<RawJob>();

        for (var page = 1; page <= Math.Max(1, opts.MaxPages); page++)
        {
            ArbeitnowResponse? response;
            try
            {
                response = await http.GetFromJsonWithRetryAsync<ArbeitnowResponse>(
                    $"{Endpoint}?page={page}", $"Arbeitnow page {page}", JsonOpts, logger, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A bad later page must not throw away pages already fetched; a
                // failing FIRST page means the source is down — let the pipeline
                // record it as a source error.
                if (jobs.Count == 0)
                    throw;
                logger.LogError(ex, "Arbeitnow page {Page} failed — keeping the {Count} jobs fetched so far",
                    page, jobs.Count);
                break;
            }

            if (response?.Data is not { Count: > 0 })
                break;

            foreach (var item in response.Data)
            {
                if (item.Slug is null || item.Title is null)
                    continue;

                if (opts.TechFilter && !LooksLikeTech(item))
                    continue;

                jobs.Add(new RawJob(
                    SourceName: SourceName,
                    ExternalId: item.Slug,
                    Title: item.Title,
                    Company: item.CompanyName ?? "Unknown",
                    Location: item.Location,
                    Url: item.Url ?? $"https://www.arbeitnow.com/jobs/{item.Slug}",
                    Description: item.Description,
                    PostedDate: item.CreatedAt > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(item.CreatedAt).UtcDateTime
                        : null,
                    RemoteHint: item.Remote));
            }

            // Be polite between page requests.
            if (page < opts.MaxPages)
                await Task.Delay(TimeSpan.FromMilliseconds(750), ct);
        }

        logger.LogInformation("Arbeitnow: fetched {Count} jobs (tech filter: {Filter})",
            jobs.Count, opts.TechFilter);
        return jobs;
    }

    private static bool LooksLikeTech(ArbeitnowJob item)
    {
        var haystack = $"{item.Title} {string.Join(' ', item.Tags ?? [])} {string.Join(' ', item.JobTypes ?? [])}"
            .ToLowerInvariant();
        if (TechTerms.Any(haystack.Contains))
            return true;

        // "IT" only counts as a whole word — as a substring it matches half the
        // German dictionary ("arbeit", "mit", "zeit") and lets marketing/sales
        // listings through.
        return ItWordRegex().IsMatch(haystack);
    }

    [GeneratedRegex(@"\bit\b")]
    private static partial Regex ItWordRegex();

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private sealed record ArbeitnowResponse(
        [property: JsonPropertyName("data")] List<ArbeitnowJob>? Data);

    private sealed record ArbeitnowJob(
        [property: JsonPropertyName("slug")] string? Slug,
        [property: JsonPropertyName("company_name")] string? CompanyName,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("remote")] bool Remote,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("tags"), JsonConverter(typeof(LenientStringListConverter))] List<string>? Tags,
        [property: JsonPropertyName("job_types"), JsonConverter(typeof(LenientStringListConverter))] List<string>? JobTypes,
        [property: JsonPropertyName("location")] string? Location,
        [property: JsonPropertyName("created_at")] long CreatedAt);

    /// <summary>
    /// The Arbeitnow feed is inconsistent about "job_types"/"tags": usually an
    /// array, but sometimes a bare string or a PHP-style map like
    /// {"1": "professional / experienced"}. Accept all three (collecting the
    /// string values) and treat anything else as empty instead of failing the
    /// whole source.
    /// </summary>
    private sealed class LenientStringListConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return [reader.GetString()!];

                case JsonTokenType.StartArray:
                    var list = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                            list.Add(reader.GetString()!);
                        else
                            reader.Skip();
                    }
                    return list;

                case JsonTokenType.StartObject:
                    var fromObject = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        // PropertyName token; the next token is its value.
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.String)
                            fromObject.Add(reader.GetString()!);
                        else
                            reader.Skip();
                    }
                    return fromObject;

                default:
                    reader.Skip();
                    return [];
            }
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
            => throw new NotSupportedException("Read-only converter.");
    }
}
