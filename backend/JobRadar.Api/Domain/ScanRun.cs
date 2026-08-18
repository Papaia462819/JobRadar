namespace JobRadar.Api.Domain;

/// <summary>One execution of the fetch pipeline (manual or scheduled).</summary>
public class ScanRun
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int TotalFetched { get; set; }
    public int TotalNew { get; set; }
    public int TotalErrors { get; set; }

    /// <summary>JSON-serialized per-source results (fetched / new / error).</summary>
    public string DetailsJson { get; set; } = "[]";
}
