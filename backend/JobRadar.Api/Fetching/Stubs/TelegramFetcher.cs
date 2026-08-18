namespace JobRadar.Api.Fetching.Stubs;

/// <summary>
/// TODO: Telegram adapter (not implemented yet).
///
/// Plan: create a bot with @BotFather, add it to the job channels/groups you
/// follow, then poll getUpdates (or use a webhook) and parse messages that
/// look like job posts (title/company/link heuristics). Config:
/// "Telegram:BotToken" + "Telegram:Channels". ExternalId = message id,
/// PostedDate = message date.
/// </summary>
public sealed class TelegramFetcher : IJobFetcher
{
    public string SourceName => "Telegram";

    public Task<IReadOnlyList<RawJob>> FetchAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RawJob>>([]);
}
