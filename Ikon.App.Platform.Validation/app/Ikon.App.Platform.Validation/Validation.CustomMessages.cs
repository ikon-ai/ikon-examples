using Ikon.App.Platform.Validation.Protocol;

// Validation tab exercising app-local custom Teleport (.tp) messages over the
// GROUP_APP_LOCAL channel — the raw transport that bypasses the Parallax reactive
// UI loop. Covers BOTH directions and BOTH reliability modes:
//   - server -> client : a start/stop loop streams ProbePing / ProbePingUnreliable
//     to the viewing client; the custom React component (tp-probe) receives them
//     directly via appMessaging.on() and prints per-mode metrics.
//   - client -> server : the component's Send buttons emit the same two types; the
//     OnMessage<T> handlers below reflect receipt back into the Parallax UI.
public partial class Validation
{
    // Server -> client stream state.
    private CancellationTokenSource? _tpStreamCts;
    private readonly Reactive<bool> _tpStreamRunning = new(false);
    private readonly Reactive<string> _tpStreamMode = new("reliable"); // "reliable" | "unreliable"
    private readonly Reactive<string> _tpStreamRateMs = new("100");
    private readonly Reactive<string> _tpStreamStatus = new("(idle)");

    // Client -> server received signal, surfaced in the Parallax UI.
    private readonly Reactive<int> _tpFromClientReliable = new(0);
    private readonly Reactive<int> _tpFromClientUnreliable = new(0);
    private readonly Reactive<string> _tpLastFromClient = new("(none yet)");

    private bool _tpHandlersRegistered;

    // Registered once from Main(). OnMessage<T> takes a single handler (payload,
    // senderId); senderId is the originating client's session id.
    private void SetupCustomMessageHandlers()
    {
        if (_tpHandlersRegistered)
        {
            return;
        }

        _tpHandlersRegistered = true;

        app.OnMessage<ProbePing>((m, senderId) =>
        {
            OnClientProbe(m.Seq, m.SentAtMs, m.Mode, senderId, reliable: true);
            return ValueTask.CompletedTask;
        });

        app.OnMessage<ProbePingUnreliable>((m, senderId) =>
        {
            OnClientProbe(m.Seq, m.SentAtMs, m.Mode, senderId, reliable: false);
            return ValueTask.CompletedTask;
        });
    }

    private void OnClientProbe(long seq, long sentAtMs, string mode, int senderId, bool reliable)
    {
        if (reliable)
        {
            _tpFromClientReliable.Value++;
        }
        else
        {
            _tpFromClientUnreliable.Value++;
        }

        long latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sentAtMs;
        _tpLastFromClient.Value = $"seq={seq} mode={mode} from=#{senderId} latency={latency}ms";
    }

    private void StartTpStream(int clientSessionId)
    {
        if (_tpStreamRunning.Value)
        {
            return;
        }

        var mode = _tpStreamMode.Value;
        var rateMs = ParseTpRate(_tpStreamRateMs.Value, 100);

        _tpStreamCts = new CancellationTokenSource();
        var token = _tpStreamCts.Token;
        _tpStreamRunning.Value = true;

        _ = Task.Run(() => RunTpStreamAsync(mode, clientSessionId, rateMs, token));
    }

    private void StopTpStream() => _tpStreamCts?.Cancel();

    private async Task RunTpStreamAsync(string mode, int clientSessionId, int rateMs, CancellationToken token)
    {
        long seq = 0;
        bool unreliable = mode == "unreliable";
        bool sendTimedOut = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // The loop's only other exit is the Stop button, which a departed
                // client cannot press — without this check the stream runs forever
                // against a dead session and holds the shared running flag.
                // Soft-disconnect counts as departed: a disconnected client cannot
                // watch the stream and can start a new one after reconnecting.
                if (!app.GlobalState.Clients.TryGetValue(clientSessionId, out var clientContext)
                    || clientContext.IsSoftDisconnected)
                {
                    Log.Instance.Info($"Custom-message stream target client {clientSessionId} disconnected — stopping stream");
                    break;
                }

                seq++;
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // The two types differ only by their schema-level `unreliable` flag,
                // which the generated codec bakes into the wire message. The send is
                // bounded because SendMessageAsync takes no token — an unbounded await
                // that never completes would wedge this loop past Stop and leave
                // _tpStreamRunning stuck until the server restarts.
                var send = unreliable
                    ? app.SendMessageAsync(
                        new ProbePingUnreliable { Seq = seq, SentAtMs = now, Origin = "server", Mode = "unreliable", Note = "stream" },
                        clientSessionId)
                    : app.SendMessageAsync(
                        new ProbePing { Seq = seq, SentAtMs = now, Origin = "server", Mode = "reliable", Note = "stream" },
                        clientSessionId);

                try
                {
                    await send.AsTask().WaitAsync(TimeSpan.FromSeconds(5), token);
                }
                catch (TimeoutException)
                {
                    sendTimedOut = true;
                    _tpStreamStatus.Value = $"send {seq} ({mode}) timed out — stopping stream";
                    Log.Instance.Warning($"Custom-message stream send timed out: mode={mode} seq={seq} client={clientSessionId}");
                    break;
                }

                _tpStreamStatus.Value = $"sent {seq} ({mode})";
                await Task.Delay(rateMs, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _tpStreamRunning.Value = false;

            if (!sendTimedOut)
            {
                _tpStreamStatus.Value = "(idle)";
            }
        }
    }

    private static int ParseTpRate(string value, int fallback)
        => int.TryParse(value, out var n) && n >= 1 ? Math.Min(n, 10000) : fallback;

    private void RenderCustomMessagesSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Custom Teleport (.tp) messages");
                view.Text([Text.BodySm, "text-tertiary mb-2"],
                    "Validates app-local custom .tp messages over the GROUP_APP_LOCAL channel, which bypasses the Parallax reactive UI loop. The component below receives server→client messages directly via appMessaging.on() and prints per-mode metrics; its Send buttons emit client→server messages, whose receipt the server reflects in the bottom panel.");
                view.Text([Text.Caption, "text-muted-foreground"],
                    "Note: on localhost the unreliable channel usually falls back to reliable (no WebRTC/UDP datagram path), so gaps / out-of-order will read 0 — those metrics become meaningful only on a lossy transport.");
            });

            // Server -> client stream controls (Parallax-native).
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Server → Client stream");
                view.Text([Text.BodySm, "text-tertiary mb-4"],
                    "A server-side loop streams the selected message type to THIS client at the chosen rate.");

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Mode");
                    view.Select(
                        value: _tpStreamMode.Value,
                        options:
                        [
                            new SelectOption("reliable", "Reliable (ProbePing)"),
                            new SelectOption("unreliable", "Unreliable (ProbePingUnreliable)")
                        ],
                        disabled: _tpStreamRunning.Value,
                        onValueChange: async v => _tpStreamMode.Value = v);
                });

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Rate (ms)");
                    view.TextField(
                        [Input.Default, "w-32"],
                        value: _tpStreamRateMs.Value,
                        type: "number",
                        step: "10",
                        min: "1",
                        disabled: _tpStreamRunning.Value,
                        onValueChange: async v => _tpStreamRateMs.Value = v);
                });

                view.Row([Layout.Row.InlineCenter, "mb-3"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Status");
                    view.Text([Text.Body], _tpStreamStatus.Value);
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button(
                        [_tpStreamRunning.Value ? Button.OutlineMd : Button.PrimaryMd],
                        text: "Start stream",
                        disabled: _tpStreamRunning.Value,
                        onClick: async () => StartTpStream(ReactiveScope.ClientId));

                    view.Button([Button.ErrorMd],
                        text: "Stop stream",
                        disabled: !_tpStreamRunning.Value,
                        onClick: async () => StopTpStream());
                });
            });

            // Custom React component — receives the stream off the reactive loop and
            // hosts the client→server Send buttons. Mount-time only (no per-tick props);
            // the .tp stream is the data channel.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Custom component (off the reactive loop)");
                view.Text([Text.BodySm, "text-tertiary mb-4"],
                    "Rendered by a custom React module subscribing via appMessaging.on(). The Send buttons emit client→server messages.");
                view.AddNode("tp-probe", new Dictionary<string, object?>(), style: ["w-full"]);
            });

            // Client -> server received signal (server-side reactives).
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Client → Server received (server-side signal)");
                view.Text([Text.BodySm, "text-tertiary mb-4"],
                    "These update from the server's app.OnMessage<T> handler when you click Send above — proving the server received the browser-originated messages.");

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-48"], "Reliable received");
                    view.Text([Text.Body], _tpFromClientReliable.Value.ToString());
                });

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-48"], "Unreliable received");
                    view.Text([Text.Body], _tpFromClientUnreliable.Value.ToString());
                });

                view.Row([Layout.Row.InlineCenter], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-48"], "Last");
                    view.Text([Text.Body], _tpLastFromClient.Value);
                });
            });
        });
    }
}
