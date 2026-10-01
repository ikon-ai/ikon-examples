using System.Collections.Concurrent;
using Ikon.App.Platform.Validation.Protocol;

public partial class Validation
{
    // The browser sends at the same interval, so the two directions read alike.
    private const int TpStreamIntervalMs = 100;

    // Every stream and count is per client session, so the numbers side by side in a row describe
    // the same traffic, and two browsers (or two validation runs) never touch each other's streams.
    private readonly TpServerStream _tpServerReliable = new("reliable");
    private readonly TpServerStream _tpServerUnreliable = new("unreliable");

    private readonly ClientReactive<bool> _tpClientReliableRunning = new(false);
    private readonly ClientReactive<bool> _tpClientUnreliableRunning = new(false);
    private readonly TpReceiveCounters _tpFromClientReliable = new("reliable");
    private readonly TpReceiveCounters _tpFromClientUnreliable = new("unreliable");

    private bool _tpHandlersRegistered;

    private void SetupCustomMessageHandlers()
    {
        if (_tpHandlersRegistered)
        {
            return;
        }

        _tpHandlersRegistered = true;

        app.OnMessage<ProbePing>((m, senderId) =>
        {
            _tpFromClientReliable.Apply(senderId, m.Note, m.Seq, m.Origin, m.Mode, m.SentAtMs);
            return ValueTask.CompletedTask;
        });

        app.OnMessage<ProbePingUnreliable>((m, senderId) =>
        {
            _tpFromClientUnreliable.Apply(senderId, m.Note, m.Seq, m.Origin, m.Mode, m.SentAtMs);
            return ValueTask.CompletedTask;
        });

        app.OnClientLeft((clientContext, _) =>
        {
            _tpServerReliable.Stop(clientContext.SessionId);
            _tpServerUnreliable.Stop(clientContext.SessionId);
            _tpFromClientReliable.Forget(clientContext.SessionId);
            _tpFromClientUnreliable.Forget(clientContext.SessionId);
            return Task.CompletedTask;
        });
    }

    private void StartTpStream(TpServerStream stream, int clientSessionId)
    {
        var cancellation = new CancellationTokenSource();

        if (!stream.Cancellations.TryAdd(clientSessionId, cancellation))
        {
            cancellation.Dispose();
            return;
        }

        stream.Running.SetFor(clientSessionId, true);
        stream.Sent.SetFor(clientSessionId, 0);
        _ = Task.Run(() => RunTpStreamAsync(stream, clientSessionId, cancellation));
    }

    private async Task RunTpStreamAsync(TpServerStream stream, int clientSessionId, CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        string streamId = Guid.NewGuid().ToString("N");
        bool unreliable = stream.Mode == "unreliable";
        long seq = 0;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!app.GlobalState.Clients.TryGetValue(clientSessionId, out var clientContext) || clientContext.IsSoftDisconnected)
                {
                    break;
                }

                seq++;
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Reliability is a per-type schema flag, so each mode is its own message type.
                // SendMessageAsync takes no token, so an unbounded await that never completes
                // would wedge the loop past Stop.
                var send = unreliable
                    ? app.SendMessageAsync(new ProbePingUnreliable { Seq = seq, SentAtMs = now, Origin = "server", Mode = stream.Mode, Note = streamId }, clientSessionId)
                    : app.SendMessageAsync(new ProbePing { Seq = seq, SentAtMs = now, Origin = "server", Mode = stream.Mode, Note = streamId }, clientSessionId);

                // Counted at dispatch: the message can reach the browser before an update to
                // this count does, and "received" running ahead of "sent" reads as a bug.
                stream.Sent.SetFor(clientSessionId, seq);

                try
                {
                    await send.AsTask().WaitAsync(TimeSpan.FromSeconds(5), token);
                }
                catch (TimeoutException)
                {
                    Log.Instance.Warning($"Custom-message {stream.Mode} stream to client {clientSessionId} stopped: send {seq} timed out");
                    break;
                }

                await Task.Delay(TpStreamIntervalMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Stop cancels the delay; the stream ends exactly as it would on its own.
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Custom-message {stream.Mode} stream to client {clientSessionId} stopped: send {seq} failed: {ex.Message}");
        }
        finally
        {
            stream.Cancellations.TryRemove(new KeyValuePair<int, CancellationTokenSource>(clientSessionId, cancellation));
            cancellation.Dispose();
            stream.Running.SetFor(clientSessionId, false);
        }
    }

    private void RenderCustomMessagesSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Custom Messages");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Server → Client");
                RenderTpServerRow(view, _tpServerReliable);
                RenderTpServerRow(view, _tpServerUnreliable);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Client → Server");
                RenderTpClientRow(view, _tpClientReliableRunning, _tpFromClientReliable);
                RenderTpClientRow(view, _tpClientUnreliableRunning, _tpFromClientUnreliable);
            });
        });
    }

    private void RenderTpServerRow(UIView view, TpServerStream stream)
    {
        bool running = stream.Running.Value;
        int clientSessionId = ReactiveScope.ClientId;
        string prefix = $"tp-s2c-{stream.Mode}";

        view.Row([Layout.Row.Lg, "items-center py-2"], content: view =>
        {
            RenderTpRowLabel(view, stream.Mode);
            view.Button([Button.PrimaryMd], text: "Start", disabled: running,
                onClick: () => StartTpStream(stream, clientSessionId), props: TestId($"{prefix}-start"));
            view.Button([Button.OutlineMd], text: "Stop", disabled: !running,
                onClick: () => stream.Stop(clientSessionId), props: TestId($"{prefix}-stop"));
            RenderTpStats(view, view =>
            {
                RenderTpStat(view, "sent", stream.Sent.Value, $"{prefix}-sent");
                view.AddNode("tp-probe", new Dictionary<string, object?> { ["role"] = "receive", ["mode"] = stream.Mode }, style: ["min-w-0"]);
            });
        });
    }

    private void RenderTpClientRow(UIView view, ClientReactive<bool> running, TpReceiveCounters received)
    {
        string prefix = $"tp-c2s-{received.Mode}";

        view.Row([Layout.Row.Lg, "items-center py-2"], content: view =>
        {
            RenderTpRowLabel(view, received.Mode);
            view.Button([Button.PrimaryMd], text: "Start", disabled: running.Value,
                onClick: () => running.Value = true, props: TestId($"{prefix}-start"));
            view.Button([Button.OutlineMd], text: "Stop", disabled: !running.Value,
                onClick: () => running.Value = false, props: TestId($"{prefix}-stop"));
            RenderTpStats(view, view =>
            {
                view.AddNode("tp-probe", new Dictionary<string, object?> { ["role"] = "send", ["mode"] = received.Mode, ["running"] = running.Value }, style: ["min-w-0"]);
                RenderTpStat(view, "received", received.Received.Value, $"{prefix}-received");
                RenderTpStat(view, "gaps", received.Gaps.Value, $"{prefix}-gaps");
                RenderTpStat(view, "out of order", received.OutOfOrder.Value, $"{prefix}-ooo");
                RenderTpStat(view, "malformed", received.Malformed.Value, $"{prefix}-malformed");
            });
        });
    }

    // The counters wrap inside their own group, so a narrow card breaks between counters instead of
    // pushing the whole set under the buttons.
    private static void RenderTpStats(UIView view, Action<UIView> content) =>
        view.Row(["flex-1 min-w-0 flex-wrap items-baseline gap-x-5 gap-y-1"], content: content);

    private static void RenderTpRowLabel(UIView view, string mode) =>
        view.Text([Text.BodyStrong, "w-24"], mode == "reliable" ? "Reliable" : "Unreliable");

    private static void RenderTpStat(UIView view, string label, long value, string testId)
    {
        view.Row([Layout.Row.Xs, "items-baseline"], content: view =>
        {
            view.Text([Text.Body, "opacity-70"], label);
            view.Text([Text.BodyStrong, "tabular-nums"], value.ToString(), props: TestId(testId));
        });
    }

    private sealed class TpServerStream(string mode)
    {
        public string Mode { get; } = mode;
        public ClientReactive<bool> Running { get; } = new(false);
        public ClientReactive<long> Sent { get; } = new(0);
        public ConcurrentDictionary<int, CancellationTokenSource> Cancellations { get; } = new();

        public void Stop(int clientSessionId)
        {
            if (Cancellations.TryGetValue(clientSessionId, out var cancellation))
            {
                cancellation.Cancel();
            }
        }
    }

    // What the server saw of one browser's stream in one mode. A new stream id is a new stream,
    // which is how a restart is told apart from reordering even when its first message was lost.
    private sealed class TpReceiveCounters(string mode)
    {
        public string Mode { get; } = mode;
        public ClientReactive<long> Received { get; } = new(0);
        public ClientReactive<long> Gaps { get; } = new(0);
        public ClientReactive<long> OutOfOrder { get; } = new(0);
        public ClientReactive<long> Malformed { get; } = new(0);

        private readonly Lock _lock = new();
        private readonly Dictionary<int, (string StreamId, long LastSeq)> _streams = new();

        public void Apply(int senderId, string streamId, long seq, string origin, string sentMode, long sentAtMs)
        {
            lock (_lock)
            {
                if (origin != "client" || sentMode != Mode || sentAtMs <= 0 || string.IsNullOrEmpty(streamId))
                {
                    Malformed.SetFor(senderId, Malformed.ValueFor(senderId) + 1);
                    return;
                }

                bool newStream = !_streams.TryGetValue(senderId, out var current) || current.StreamId != streamId;

                if (newStream)
                {
                    current = (streamId, 0);
                    Received.SetFor(senderId, 0);
                    Gaps.SetFor(senderId, 0);
                    OutOfOrder.SetFor(senderId, 0);
                }

                if (current.LastSeq != 0 && seq > current.LastSeq + 1)
                {
                    Gaps.SetFor(senderId, Gaps.ValueFor(senderId) + (seq - current.LastSeq - 1));
                }

                if (current.LastSeq != 0 && seq <= current.LastSeq)
                {
                    OutOfOrder.SetFor(senderId, OutOfOrder.ValueFor(senderId) + 1);
                }

                _streams[senderId] = (streamId, Math.Max(current.LastSeq, seq));
                Received.SetFor(senderId, Received.ValueFor(senderId) + 1);
            }
        }

        public void Forget(int senderId)
        {
            lock (_lock)
            {
                _streams.Remove(senderId);
            }
        }
    }
}
