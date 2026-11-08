// Every [[Port]] in ikon-config.toml answers the validation script's probe with the port's own name,
// so the script can tell the ports apart. On the tls port the relay ends TLS and the app reads the
// plain stream, so the same line echo serves the tcp and tls ports.
public partial class Validation
{
    // The ports are public. They answer only a line or datagram that starts with the probe prefix, an
    // answer is never longer than what was sent, so a datagram with a forged source cannot make the
    // port send its victim more than the sender sent, and a few connections at a time are served
    private const string PortProbePrefix = "validation ";
    private const int PortProbeMaxBytes = 256;
    private const int PortMaxConnections = 8;

    private int _portConnections;

    // Bound in Main, because the ports are claimed once Main returns and the relay then forwards a
    // waiting connection at once. app.Ports is empty in a boot snapshot capture. An instance other
    // than the one with the empty identity lists the ports without opening them, so its listeners
    // bind loopback ports that get no traffic.
    private void StartPortEchoes()
    {
        foreach (var port in app.Ports.Values)
        {
            _ = port.Protocol == "udp"
                ? RunUdpEchoAsync(port, app.StoppingToken)
                : RunTcpEchoAsync(port, app.StoppingToken);
        }
    }

    // A hot reload starts the new instance while the old one still listens on the same loopback port,
    // and the old one lets go of it only when it stops
    private static readonly TimeSpan PortBindWait = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PortBindRetryDelay = TimeSpan.FromMilliseconds(250);

    private static async Task<T?> BindPortAsync<T>(AppPort port, Func<T> bind, CancellationToken stopping) where T : class
    {
        var deadline = DateTime.UtcNow + PortBindWait;

        while (true)
        {
            try
            {
                return bind();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse && DateTime.UtcNow < deadline)
            {
                Log.Instance.Debug($"Port {port.Name} waits for loopback port {port.LocalPort}: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Log.Instance.Error($"Port {port.Name} cannot listen on loopback port {port.LocalPort}: {ex.Message}");
                return null;
            }

            try
            {
                await Task.Delay(PortBindRetryDelay, stopping);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }

    private async Task RunTcpEchoAsync(AppPort port, CancellationToken stopping)
    {
        var listener = await BindPortAsync(port, () =>
        {
            var started = new TcpListener(IPAddress.Loopback, port.LocalPort);

            try
            {
                started.Start();
                return started;
            }
            catch
            {
                started.Dispose();
                throw;
            }
        }, stopping);

        if (listener is null)
        {
            return;
        }

        // Closed as the token fires, not when the accept loop next runs, so the next instance can bind
        using var stopListening = stopping.Register(listener.Stop);

        try
        {
            while (!stopping.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await listener.AcceptTcpClientAsync(stopping);
                }
                catch (SocketException ex)
                {
                    // A client that resets before the accept completes fails only its own accept
                    Log.Instance.Debug($"Port {port.Name} accept failed: {ex.Message}");
                    continue;
                }

                if (Interlocked.Increment(ref _portConnections) > PortMaxConnections)
                {
                    Interlocked.Decrement(ref _portConnections);
                    client.Dispose();
                    continue;
                }

                _ = EchoLinesAsync(port.Name, client, stopping);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || stopping.IsCancellationRequested)
        {
            // The app is stopping, which is the only way the listener ends; closing it under a pending
            // accept can fail that accept with something other than a cancellation
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task EchoLinesAsync(string name, TcpClient client, CancellationToken stopping)
    {
        using var _ = client;

        try
        {
            var stream = client.GetStream();
            var buffer = new byte[PortProbeMaxBytes];
            int length = 0;

            while (true)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(length), stopping);

                if (read == 0)
                {
                    return;
                }

                length += read;
                int newline;

                while ((newline = Array.IndexOf(buffer, (byte)'\n', 0, length)) >= 0)
                {
                    var answer = PortProbeAnswer(name, Encoding.UTF8.GetString(buffer, 0, newline).TrimEnd('\r'));

                    if (answer is not null)
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(answer + "\n"), stopping);
                    }

                    length -= newline + 1;
                    Array.Copy(buffer, newline + 1, buffer, 0, length);
                }

                // A line longer than any probe is not one
                if (length == buffer.Length)
                {
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
        {
            // A client that drops its connection ends its own echo, and nothing else depends on it
            Log.Instance.Debug($"Port {name} echo connection ended: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _portConnections);
        }
    }

    private static async Task RunUdpEchoAsync(AppPort port, CancellationToken stopping)
    {
        var udp = await BindPortAsync(port, () => new UdpClient(new IPEndPoint(IPAddress.Loopback, port.LocalPort)), stopping);

        if (udp is null)
        {
            return;
        }

        using var _ = udp;
        using var stopListening = stopping.Register(udp.Close);

        while (!stopping.IsCancellationRequested)
        {
            try
            {
                var received = await udp.ReceiveAsync(stopping);

                if (received.Buffer.Length > PortProbeMaxBytes
                    || PortProbeAnswer(port.Name, Encoding.UTF8.GetString(received.Buffer)) is not { } answer)
                {
                    continue;
                }

                await udp.SendAsync(Encoding.UTF8.GetBytes(answer), received.RemoteEndPoint, stopping);
            }
            catch (Exception ex) when (ex is OperationCanceledException || stopping.IsCancellationRequested)
            {
                // The app is stopping, and closing the socket under a pending receive can fail it with
                // something other than a cancellation
                return;
            }
            catch (SocketException ex)
            {
                // Sending to a source that has gone makes the next receive fail with a reset, but the
                // other sources are still served
                Log.Instance.Debug($"Port {port.Name} echo datagram failed: {ex.Message}");
            }
        }
    }

    private static string? PortProbeAnswer(string portName, string probe)
    {
        if (!probe.StartsWith(PortProbePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var answer = $"{portName}: {probe[PortProbePrefix.Length..]}";
        return Encoding.UTF8.GetByteCount(answer) <= Encoding.UTF8.GetByteCount(probe) ? answer : null;
    }
}
