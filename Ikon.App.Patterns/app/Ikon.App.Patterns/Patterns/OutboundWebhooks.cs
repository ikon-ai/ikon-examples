using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ikon.App.Patterns.Patterns;

// Pattern: outbound-webhooks — see docs/patterns/outbound-webhooks.md.
// `app` is the App's primary-constructor handle, standing outside the region.
internal sealed class OutboundWebhooks(IAppBase app) : IPatternDemo
{
    public string Slug => "outbound-webhooks";
    public string Title => "Outbound webhooks";
    public string Category => "Platform";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern: notify other tools when something happens. A signed JSON POST per subscribed URL, retried with backoff in the background, each attempt kept in a delivery log the settings page can show — see docs/patterns/outbound-webhooks.md.");

    #region example:pattern-outbound-webhooks
    public sealed record WebhookEndpoint(string Id, string Url, string Secret);

    public sealed record WebhookAttempt(string EndpointId, string EventName, string DeliveryId, int Attempt, int? Status, string? Error, DateTimeOffset At);

    // One HttpClient for the app's lifetime: one per call runs out of sockets under load.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly TimeSpan[] Backoff = [TimeSpan.Zero, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];

    // What the settings page lists: the latest attempts, newest first.
    private readonly ReactiveList<WebhookAttempt> _attempts = new();

    // Call where the event happens. It returns at once; delivery runs in the background, so a slow or
    // failing receiver never holds up the person who caused the event.
    private void Publish(IReadOnlyList<WebhookEndpoint> endpoints, string eventName, object data)
    {
        var deliveryId = Guid.NewGuid().ToString("N");
        var body = JsonSerializer.Serialize(new { id = deliveryId, type = eventName, createdAt = DateTimeOffset.UtcNow, data });

        foreach (var endpoint in endpoints)
        {
            _ = Task.Run(() => DeliverAsync(endpoint, eventName, deliveryId, body));
        }
    }

    private async Task DeliverAsync(WebhookEndpoint endpoint, string eventName, string deliveryId, string body)
    {
        var stopping = app.StoppingToken;

        for (var attempt = 1; attempt <= Backoff.Length && !stopping.IsCancellationRequested; attempt++)
        {
            await Task.Delay(Backoff[attempt - 1], stopping).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            // The receiver checks the signature over "timestamp.body" with the shared secret, and
            // rejects an old timestamp, so a captured request cannot be replayed later.
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("Webhook-Id", deliveryId);
            request.Headers.Add("Webhook-Timestamp", timestamp);
            request.Headers.Add("Webhook-Signature", "sha256=" + Sign(endpoint.Secret, $"{timestamp}.{body}"));

            int? status = null;
            string? error = null;

            try
            {
                using var response = await Http.SendAsync(request, stopping);
                status = (int)response.StatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !stopping.IsCancellationRequested)
            {
                error = ex is TaskCanceledException ? "Timed out" : ex.Message;
            }

            _attempts.Insert(0, new WebhookAttempt(endpoint.Id, eventName, deliveryId, attempt, status, error, DateTimeOffset.UtcNow));

            if (_attempts.Count > 200)
            {
                _attempts.RemoveAt(_attempts.Count - 1);
            }

            // Delivered, or refused in a way a retry cannot change (a 4xx other than 408 and 429).
            if (status is >= 200 and < 300 || status is >= 400 and < 500 and not 408 and not 429)
            {
                return;
            }
        }
    }

    private static string Sign(string secret, string payload) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)));
    #endregion
}
