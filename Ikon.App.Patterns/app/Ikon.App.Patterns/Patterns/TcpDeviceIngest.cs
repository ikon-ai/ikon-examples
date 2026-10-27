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

    // A device that sends nothing for this long is disconnected
    private static readonly TimeSpan SilenceLimit = TimeSpan.FromMinutes(5);

    private readonly Reactive<string?> _tcpAddress = new(null);
    private readonly ReactiveDictionary<string, DeviceStatus> _devices = new();

    /// <summary>Call once from Main.</summary>
    private void StartTcpIngest()
    {
        // ikon-config.toml declares [[Port]] Name = "devices". Its address stays the same across deploys,
        // restarts and idle stops, so each device keeps it in its own configuration, and a connection
        // to it starts a stopped app.
        var devices = app.Ports["devices"];
        _tcpAddress.Value = devices.Address;
        devices.AddressChanged += port => _tcpAddress.Value = port.Address;

        // The relay hands each connection to this port on the loopback address
        var listener = new TcpListener(IPAddress.Loopback, devices.LocalPort);
        listener.Start();
        _ = AcceptAsync(listener, app.StoppingToken);
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
            // The instance is stopping; the devices connect again, which starts the next one
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

            await writer.WriteLineAsync("OK");

            // Traffic on a port counts as activity, so the instance keeps running while devices send
            while (await reader.ReadLineAsync(silence.Token) is { } line)
            {
                silence.CancelAfter(SilenceLimit);
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
            if (_tcpAddress.Value is { Length: > 0 } address)
            {
                col.CopyField(address, label: "Device address");
            }
            else
            {
                col.Text([Text.Caption], "The TCP port opens when the app starts.");
            }

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
