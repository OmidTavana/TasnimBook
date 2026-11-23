using System.Net;
using System.Net.Http.Json;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Web.Services;

public sealed class ReleaseApiClient(HttpClient client, ILogger<ReleaseApiClient> logger)
{
    public async Task<(AppReleaseDto? Android, AppReleaseDto? Windows, bool Unavailable)> GetLatestAsync(CancellationToken cancellationToken)
    {
        var androidTask = GetAsync("android", cancellationToken);
        var windowsTask = GetAsync("windows", cancellationToken);
        var results = await Task.WhenAll(androidTask, windowsTask);
        return (results[0].Release, results[1].Release, results.Any(item => item.Failed));
    }

    private async Task<(AppReleaseDto? Release, bool Failed)> GetAsync(string platform, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"app-releases/latest?platform={platform}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return (null, false);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AppReleaseDto>(cancellationToken), false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not load the latest {Platform} release", platform);
            return (null, true);
        }
    }
}
