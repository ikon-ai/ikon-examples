using System.Security.Cryptography;
using Ikon.App.Patterns.Protocol;
using Ikon.Sdk;

namespace Ikon.App.Patterns.Patterns;

// Pattern: device-typed-messages — see docs/patterns/device-typed-messages.md.
// No device joins the gallery, so the demo draws the station list, and its button simulates a
// report by writing to the same live view the handler writes to.
internal sealed class DeviceTypedMessages(IAppBase app) : IPatternDemo
{
    public string Slug => "device-typed-messages";
    public string Title => "A device program that sends typed messages";
    public string Category => "Device & sensors";

    public void RenderDemo(IView view)
    {
        RenderStations((UIView)view);
        view.Button([Button.OutlineMd, "mt-3"], text: "Simulate a report from a station", onClick: async () =>
        {
            var stationId = $"station-{Random.Shared.Next(1, 4)}";
            _stations[stationId] = new StationState(stationId, Math.Round(-5 + Random.Shared.NextDouble() * 25, 1), DateTime.UtcNow);
        });
    }

    #region example:pattern-device-typed-messages
    private sealed record StationState(string StationId, double TemperatureC, DateTime ReceivedAt);

    // The last report of each station, which every open screen draws
    private readonly ReactiveDictionary<string, StationState> _stations = new();

    /// <summary>Call once from Main.</summary>
    private void StartWeatherReports()
    {
        app.OnMessage<WeatherReport>(OnWeatherReportAsync);
    }

    // A device program sends one WeatherReport per reading and waits for the WeatherReportAck
    private async Task OnWeatherReportAsync(WeatherReport report, int senderId)
    {
        string? refusal = !KeyMatches(report.StationKey) ? "key"
            : report.StationId.Length is 0 or > 64 ? "stationId is required, at most 64 characters"
            : null;

        if (refusal is null)
        {
            // Use the server's clock, because a cheap device's clock drifts
            _stations[report.StationId] = new StationState(report.StationId, report.TemperatureC, DateTime.UtcNow);
        }

        // The ack goes only to the sender, because an app message always names its recipients
        await app.SendMessageAsync(new WeatherReportAck { Sequence = report.Sequence, Accepted = refusal is null, Reason = refusal ?? "" }, senderId);
    }

    // Any program can join an app that lets guests in, so every report includes the key
    private bool KeyMatches(string sentKey) =>
        app.Secrets.TryGet("STATION_KEY", out var stationKey)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sentKey), Encoding.UTF8.GetBytes(stationKey));

    private void RenderStations(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Text([Text.H3], "Weather stations");

            if (_stations.Count == 0)
            {
                col.Text([Text.Body], "No station has reported yet.");
                return;
            }

            foreach (var station in _stations.Values.OrderBy(s => s.StationId, StringComparer.Ordinal))
            {
                col.Row(["items-center justify-between gap-3 flex-wrap"], key: station.StationId, content: row =>
                {
                    row.Text([Text.Body, "font-medium"], station.StationId);
                    row.Text([Text.Body], $"{station.TemperatureC:0.0} °C");
                    row.Text([Text.Caption], $"{station.ReceivedAt:HH:mm:ss} UTC");
                });
            }
        });
    }
    #endregion

    private static double ReadThermometer() => Math.Round(-5 + Random.Shared.NextDouble() * 25, 1);

    // The device side runs in a console program of its own; here it only has to compile
    private static async Task RunStationProgramAsync()
    {
        #region example:pattern-device-typed-messages-device
        const string appId = "your-app-id";

        // Production in both, or Development for an app on *.dev.ikonai.app
        var config = (Environment.GetEnvironmentVariable("IKON_LOCAL_PORT"), Environment.GetEnvironmentVariable("IKON_API_KEY")) switch
        {
            // A local `ikon run`, on the port of the Server URL it prints. SpaceId refuses another app's run on that port
            ({ } port, _) => new IkonClientConfig
            {
                Local = new LocalConfig { Host = "localhost", HttpsPort = int.Parse(port), UserId = "station-north", SpaceId = appId },
            },
            // A private app lets no guest in, so the program connects with a key from `ikon api-key create`
            (_, { } apiKey) => new IkonClientConfig
            {
                ApiKey = new ApiKeyConfig { ApiKey = apiKey, SpaceId = appId, ExternalUserId = "station-north", BackendType = BackendType.Production },
                DeviceId = "station-north",
            },
            _ => new IkonClientConfig
            {
                Guest = new GuestConfig { SpaceId = appId, Environment = IkonBackend.EnvironmentType.Production },
                DeviceId = "station-north",
            },
        };

        await using var client = new IkonClient(config);

        // Subscribed before the connect, and kept across reconnects
        TaskCompletionSource<WeatherReportAck>? pendingAck = null;
        using var acks = client.OnMessage<WeatherReportAck>(async (ack, _) => pendingAck?.TrySetResult(ack));

        client.ReadyAsync += async e => await client.SignalReadyAsync();
        await client.ConnectAsync();

        var stationKey = Environment.GetEnvironmentVariable("STATION_KEY") ?? "";

        for (var sequence = 1; sequence <= 3; sequence++)
        {
            pendingAck = new TaskCompletionSource<WeatherReportAck>(TaskCreationOptions.RunContinuationsAsynchronously);
            await client.SendMessageAsync(new WeatherReport { StationKey = stationKey, StationId = "station-north", Sequence = sequence, TemperatureC = ReadThermometer() });

            // A TimeoutException here means the app did not answer. A real device keeps the reading and sends it again
            var ack = await pendingAck.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Console.WriteLine(ack.Accepted ? $"Report {ack.Sequence} stored" : $"Report {ack.Sequence} refused: {ack.Reason}");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        #endregion
    }
}
