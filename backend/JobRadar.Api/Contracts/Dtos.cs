using JobRadar.Api.Domain;

namespace JobRadar.Api.Contracts;

public sealed record JobDto(
    int Id,
    string SourceName,
    string Title,
    string Company,
    string Location,
    bool IsRemote,
    string Url,
    string Language,
    int RelevanceScore,
    string[] MatchedKeywords,
    DateTime? PostedDate,
    DateTime FirstSeenAt,
    bool Notified,
    InteractionState State,
    string Description)
{
    public static JobDto From(Job j, bool truncateDescription = false)
    {
        var description = j.Description;
        if (truncateDescription && description.Length > 280)
            description = description[..280].TrimEnd() + "…";

        return new JobDto(
            j.Id, j.SourceName, j.Title, j.Company, j.Location, j.IsRemote, j.Url,
            j.Language, j.RelevanceScore,
            j.MatchedKeywords.Length == 0 ? [] : j.MatchedKeywords.Split(','),
            j.PostedDate, j.FirstSeenAt, j.Notified, j.InteractionState, description);
    }
}

public sealed record JobListResponse(int Total, IReadOnlyList<JobDto> Items);

public sealed record UpdateStateRequest(InteractionState State);

public sealed record MarkNotifiedResponse(int Marked);

public sealed record SourceStats(string Source, int Total, int New);

public sealed record LastScanDto(
    DateTime StartedAt,
    DateTime FinishedAt,
    int TotalFetched,
    int TotalNew,
    int TotalErrors);

public sealed record StatsDto(
    int TotalJobs,
    int NewCount,
    int NewToday,
    int Saved,
    int Applied,
    Dictionary<string, int> StateCounts,
    IReadOnlyList<SourceStats> PerSource,
    LastScanDto? LastScan);
