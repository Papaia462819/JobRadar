namespace JobRadar.Api.Domain;

/// <summary>Canonical, normalized job listing (one row per deduplicated job).</summary>
public class Job
{
    public int Id { get; set; }

    /// <summary>Which fetcher first produced this job (e.g. "Arbeitnow").</summary>
    public required string SourceName { get; set; }

    /// <summary>The id the job has at its source (slug, numeric id, ...).</summary>
    public required string ExternalId { get; set; }

    public required string Title { get; set; }
    public required string Company { get; set; }
    public string Location { get; set; } = "";
    public bool IsRemote { get; set; }
    public required string Url { get; set; }

    /// <summary>Plain-text description (HTML is stripped during normalization).</summary>
    public string Description { get; set; } = "";

    /// <summary>Detected language: "ro", "en" or "de" (heuristic).</summary>
    public string Language { get; set; } = "en";

    public int RelevanceScore { get; set; }

    /// <summary>Comma-separated profile keywords that matched title/description.</summary>
    public string MatchedKeywords { get; set; } = "";

    public DateTime? PostedDate { get; set; }

    /// <summary>UTC timestamp of the scan run that first inserted this job.</summary>
    public DateTime FirstSeenAt { get; set; }

    /// <summary>A job counts as NEW while this is false; POST /api/jobs/mark-notified flips it.</summary>
    public bool Notified { get; set; }

    public InteractionState InteractionState { get; set; } = InteractionState.New;

    /// <summary>
    /// SHA-256 over normalized (company | title | city) — the same job coming
    /// from two sources collapses into one row. Unique index.
    /// </summary>
    public required string DedupHash { get; set; }
}
