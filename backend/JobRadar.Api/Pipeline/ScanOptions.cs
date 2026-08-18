namespace JobRadar.Api.Pipeline;

/// <summary>Bound from the "Scan" config section.</summary>
public sealed class ScanOptions
{
    /// <summary>Hangfire cron for the scheduled scan (read directly in Program.cs).</summary>
    public string Cron { get; set; } = "0 8 * * *";

    /// <summary>
    /// Listings whose posted date is older than this many days are skipped at
    /// ingest and hidden from the API (0 disables the cutoff). Jobs without a
    /// posted date are kept — their first-seen date stands in for freshness.
    /// </summary>
    public int MaxJobAgeDays { get; set; } = 180;

    /// <summary>UTC cutoff implied by <see cref="MaxJobAgeDays"/>, or null when disabled.</summary>
    public DateTime? PostedCutoffUtc
        => MaxJobAgeDays > 0 ? DateTime.UtcNow.AddDays(-MaxJobAgeDays) : null;
}
