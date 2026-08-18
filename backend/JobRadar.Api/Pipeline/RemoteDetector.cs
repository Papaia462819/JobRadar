namespace JobRadar.Api.Pipeline;

/// <summary>
/// Derives "fully remote" from source hint + text. Hybrid explicitly counts
/// as NOT remote, and wins even over a source-provided remote flag.
/// </summary>
public static class RemoteDetector
{
    private static readonly string[] RemoteSignals =
    [
        "remote", "fully remote", "100% remote", "work from home", "work-from-home",
        "wfh", "telecommute", "home office", "homeoffice", "de acasă", "de acasa",
        "la distanță", "la distanta"
    ];

    private static readonly string[] HybridSignals = ["hybrid", "hibrid"];

    public static bool IsRemote(bool? sourceHint, string title, string location, string description)
    {
        var haystack = $"{title}\n{location}\n{description}".ToLowerInvariant();

        if (HybridSignals.Any(haystack.Contains))
            return false;

        if (sourceHint == true)
            return true;

        // Location saying "remote" is a stronger, cheaper signal than the body.
        var loc = location.ToLowerInvariant();
        if (loc.Contains("remote") || loc.Contains("anywhere"))
            return true;

        return RemoteSignals.Any(haystack.Contains);
    }
}
