using System.Text;
using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Patterns;

// Pattern: external-feed-sync — see docs/patterns/external-feed-sync.md.
internal sealed class ExternalFeedSync : IPatternDemo
{
    public string Slug => "external-feed-sync";
    public string Title => "External feed sync";
    public string Category => "Integrations";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern: when a user joins, the app fetches their calendar or RSS feed with one shared HttpClient (https only, 15 second timeout, 2 MB cap) and writes the result to a per-user reactive. See docs/patterns/external-feed-sync.md.");
}

file abstract class ExternalFeedSyncExample
{
    #region example:pattern-external-feed-sync
    private const int MaxFeedBytes = 2 * 1024 * 1024;

    // One client for the app: a new one per call runs out of sockets. The timeout covers the whole
    // request, and the User-Agent is there because many feed hosts answer 403 without one.
    private static readonly HttpClient FeedClient = CreateFeedClient();

    private readonly PersistentUserReactive<string> _feedUrl = new("");
    private readonly UserReactive<IReadOnlyList<string>> _feedTitles = new([]);
    private readonly UserReactive<string?> _feedError = new((string?)null);

    private static HttpClient CreateFeedClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("my-app/1.0");
        return client;
    }

    // Call from app.OnClientJoined, which runs in the joining user's scope, so the writes below land
    // in that user's reactives. Under a per-user SessionIdentity a [Cron] job cannot do this (see
    // daily-reminder-per-user).
    private async Task SyncFeedAsync(Context ctx)
    {
        if (ctx.IsSnapshot || string.IsNullOrEmpty(_feedUrl.Value))
        {
            return;
        }

        try
        {
            _feedTitles.Value = ParseTitles(await FetchTextAsync(_feedUrl.Value));
            _feedError.Value = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or UriFormatException)
        {
            // The previous titles stay, so a feed host that is down for a minute does not blank the screen.
            _feedError.Value = $"Could not load the feed: {ex.Message}";
        }
    }

    private static async Task<string> FetchTextAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException("The feed address must start with https://");
        }

        // Headers first, so a huge body is refused before it is downloaded.
        using var response = await FeedClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaxFeedBytes)
        {
            throw new InvalidDataException("The feed is larger than 2 MB");
        }

        // A server may omit or understate the length, so the read is capped as well.
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;

        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxFeedBytes)
            {
                throw new InvalidDataException("The feed is larger than 2 MB");
            }

            buffer.Write(chunk, 0, read);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    // An iCalendar (.ics) file lists each event's title on a SUMMARY line.
    private static List<string> ParseTitles(string text) =>
        text.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith("SUMMARY:", StringComparison.Ordinal))
            .Select(line => line["SUMMARY:".Length..])
            .Take(200)
            .ToList();
    #endregion
}
