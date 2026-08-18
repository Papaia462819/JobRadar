using System.Net;
using System.Text.Json;

namespace JobRadar.Api.Fetching;

/// <summary>
/// GET + deserialize with a couple of retries on transient failures
/// (5xx, 408/429, network errors, timeouts) so one hiccup doesn't cost a
/// whole source for the rest of the scan.
/// </summary>
public static class HttpJsonRetry
{
    private const int Attempts = 3;

    /// <param name="label">Used in log messages instead of the URL (URLs can carry API keys).</param>
    public static async Task<T?> GetFromJsonWithRetryAsync<T>(
        this HttpClient http,
        string url,
        string label,
        JsonSerializerOptions? jsonOptions,
        ILogger logger,
        CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (attempt < Attempts && IsTransient(response.StatusCode))
                {
                    logger.LogWarning("{Label}: HTTP {Status} — retry {Attempt}/{Max}",
                        label, (int)response.StatusCode, attempt, Attempts - 1);
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<T>(jsonOptions, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < Attempts && ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning("{Label}: {Error} — retry {Attempt}/{Max}",
                    label, ex.Message, attempt, Attempts - 1);
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
        }
    }

    private static bool IsTransient(HttpStatusCode status)
        => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
           || (int)status >= 500;
}
