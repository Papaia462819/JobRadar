namespace JobRadar.Api.Fetching;

/// <summary>
/// One adapter per job source. Implementations are registered in DI
/// (see Program.cs) and executed by ScanService, which isolates failures:
/// a fetcher that throws is logged and skipped, the others still run.
/// Adding a source = adding a class + one DI registration.
/// </summary>
public interface IJobFetcher
{
    string SourceName { get; }

    Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct);
}
