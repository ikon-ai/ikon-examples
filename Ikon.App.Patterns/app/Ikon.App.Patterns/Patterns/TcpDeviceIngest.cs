using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Ikon.App.Patterns.Patterns;

// Pattern: tcp-device-ingest — see docs/patterns/tcp-device-ingest.md.
// The Patterns app opens no TCP port, so the demo draws the device list and its button feeds a line
// through the same handler a connection uses.
internal sealed class TcpDeviceIngest(IAppBase app) : IPatternDemo
{
    public string Slug => "tcp-device-ingest";
    public string Title => "Devices that send lines over raw TCP";
    public string Category => "Device & sensors";

    public void RenderDemo(IView view)
    {
        RenderDevices((UIView)view);
        view.Button([Button.OutlineMd, "mt-3"], text: "Simulate a line from a device", onClick: async () =>
            HandleLine($"forklift-{Random.Shared.Next(1, 4)} battery={Random.Shared.Next(20, 100)}"));
    }

    #region example:pattern-tcp-device-ingest
    private sealed record DeviceStatus(string DeviceId, string Status, DateTime LastSeen);

    // A device that sends nothing for this long is disconnected, and its hold on the instance ends
    private static readonly TimeSpan SilenceLimit = TimeSpan.FromMinutes(5);

    private readonly Reactive<string?> _tcpAddress = new(null);
    private readonly ReactiveDictionary<string, DeviceStatus> _devices = new();
    private readonly Lock _listenLock = new();
    private Task<RelayEndpoint>? _listening;

    /// <summary>Call once from Main.</summary>
    private void StartTcpIngest()
    {
        // Not awaited: OnStarting must return for the instance to finish starting
        app.OnStarting(async () => _ = EnsureListeningAsync());
    }

    // GET https://{app}.ikonai.app/api/tcp-address answers "host:port". A device asks here before it
    // connects and again whenever a connection fails or is refused, because the call starts the app
    // when it is stopped, and every new instance of the app listens on a different port.
    [HttpGet("/tcp-address", Auth = EndpointAuth.Public)]
    public async Task<string> GetTcpAddress()
    {
        var endpoint = await EnsureListeningAsync();
        return $"{endpoint.PublicHost}:{endpoint.PublicPort}";
    }

    private Task<RelayEndpoint> EnsureListeningAsync()
    {
        lock (_listenLock)
        {
            // A failed attempt (the relay was unreachable) is tried again on the next call
            if (_listening is null || _listening.IsFaulted)
            {
                _listening = ListenAsync();
            }

            return _listening;
        }
    }

    private async Task<RelayEndpoint> ListenAsync()
    {
        // The name keeps the port while this instance's relay connection drops and comes back. Every
        // new instance (a restart, a deploy, a start after an idle stop, a local run) gets another one.
        var endpoint = await app.RequestEndpointAsync(EndpointProtocol.Tcp, stablePortName: "devices", ct: app.StoppingToken);
        _tcpAddress.Value = $"{endpoint.PublicHost}:{endpoint.PublicPort}";
        endpoint.Rebound += moved => _tcpAddress.Value = $"{moved.PublicHost}:{moved.PublicPort}";

        // The relay hands each connection to this port on the loopback address
        var listener = new TcpListener(IPAddress.Loopback, endpoint.LocalPort);
        listener.Start();
        _ = AcceptAsync(listener, app.StoppingToken);
        return endpoint;
    }

    private async Task AcceptAsync(TcpListener listener, CancellationToken stopping)
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(stopping);
                _ = ServeDeviceAsync(client, stopping);
            }
        }
        catch (OperationCanceledException)
        {
            // The instance is stopping; the devices reconnect to the next one through /api/tcp-address
        }
        finally
        {
            listener.Stop();
        }
    }

    // One connection: the device key on the first line, then one "<device-id> <status>" line per
    // report. Every line gets "OK" or "ERR <reason>" back.
    private async Task ServeDeviceAsync(TcpClient client, CancellationToken stopping)
    {
        using var connection = client;
        using var silence = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        silence.CancelAfter(SilenceLimit);
        var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

        try
        {
            if (!KeyMatches(await reader.ReadLineAsync(silence.Token)))
            {
                await writer.WriteLineAsync("ERR key");
                return;
            }

            // An open connection does not count as activity. With nobody in the app, the instance is stopped
            // as idle unless something holds it. Each line extends the hold, and closing the connection releases it.
            await using var hold = await app.BackgroundWork.StartAsync(SilenceLimit, "TCP devices connected");
            await writer.WriteLineAsync("OK");

            while (await reader.ReadLineAsync(silence.Token) is { } line)
            {
                silence.CancelAfter(SilenceLimit);
                await hold.ExtendAsync(SilenceLimit);
                await writer.WriteLineAsync(HandleLine(line));
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
            // The device went quiet or away, or the instance is stopping; the device connects again
        }
    }

    private string HandleLine(string line)
    {
        var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 || parts[0].Length > 64 || parts[1].Length > 200)
        {
            return "ERR expected <device-id> <status>";
        }

        _devices[parts[0]] = new DeviceStatus(parts[0], parts[1], DateTime.UtcNow);
        return "OK";
    }

    // The same check as an HTTP device key: an app secret, compared in constant time
    private bool KeyMatches(string? sentKey) =>
        sentKey is not null
        && app.Secrets.TryGet("DEVICE_KEY", out var deviceKey)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sentKey), Encoding.UTF8.GetBytes(deviceKey));

    private void RenderDevices(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Text([Text.H3], "Devices");
            col.Text([Text.Caption], _tcpAddress.Value is { } address
                ? $"Listening at {address}. Devices look it up at /api/tcp-address."
                : "The TCP port opens when the app starts.");

            if (_devices.Count == 0)
            {
                col.EmptyState([EmptyState.RootSm], title: "No device has reported yet", description: "Devices appear here once they connect and send a line.");
                return;
            }

            foreach (var device in _devices.Values.OrderBy(d => d.DeviceId, StringComparer.Ordinal))
            {
                col.Row(["items-center justify-between gap-3 flex-wrap"], key: device.DeviceId, content: row =>
                {
                    row.Text([Text.Body, "font-medium"], device.DeviceId);
                    row.Text([Text.Caption], device.Status);
                    row.Text([Text.Caption], $"{device.LastSeen:HH:mm:ss} UTC");
                });
            }
        });
    }
    #endregion
}
