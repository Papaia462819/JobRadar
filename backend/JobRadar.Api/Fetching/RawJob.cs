namespace JobRadar.Api.Fetching;

/// <summary>
/// What a fetcher produces, before normalization. Fields are kept as close to
/// the source payload as possible; the pipeline cleans/derives everything else.
/// </summary>
/// <param name="RemoteHint">
/// Set when the source has an explicit remote flag (e.g. Arbeitnow); null means
/// "unknown, derive from text".
/// </param>
public sealed record RawJob(
    string SourceName,
    string ExternalId,
    string Title,
    string Company,
    string? Location,
    string Url,
    string? Description,
    DateTime? PostedDate,
    bool? RemoteHint);
