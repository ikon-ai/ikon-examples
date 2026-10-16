namespace Ikon.App.Patterns.Patterns;

// Pattern: voice-group-call — see docs/patterns/voice-group-call.md.
// `app` is the App's primary-constructor handle; WireCall is what the app's Main() calls once.
internal sealed class VoiceGroupCall(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "voice-group-call";
    public string Title => "Voice group call";
    public string Category => "Voice";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-voice-group-call
    private Audio Audio { get; } = new(app);

    // One mix per person in the call: everyone's microphone but their own
    private readonly GroupAudioMixer _mixer = new();

    // Each open microphone in the call: its owner and its format, which only the begin event carries.
    // The audio handlers run concurrently, so the map is a ConcurrentDictionary.
    private readonly ConcurrentDictionary<string, (int Owner, int SampleRate, int ChannelCount)> _streams = new();

    // Who is in the call and whose microphone is open, shown to everyone
    private readonly ReactiveDictionary<int, string> _inCall = new();
    private readonly ReactiveHashSet<int> _micOpen = new();

    // The name this tab joins with
    private readonly ClientReactive<string> _name = new("");

    /// <summary>
    /// Called once from Main(): the audio events are app-wide and carry the client.
    /// </summary>
    private void WireCall()
    {
        app.OnClientLeft(async ctx => LeaveCall(ctx.ClientSessionId));

        Audio.AudioInputStreamBeginAsync += async args =>
        {
            // A microphone opened by someone outside the call is not mixed
            if (!_inCall.ContainsKey(args.ClientSessionId))
            {
                return;
            }

            _streams[args.StreamId] = (args.ClientSessionId, args.SampleRate, args.ChannelCount);
            _mixer.AddStream(args.StreamId, args.ClientSessionId);   // tagged with its owner, who never hears it
            _micOpen.Add(args.ClientSessionId);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (_streams.TryGetValue(args.StreamId, out var stream))
            {
                _mixer.WriteSamples(args.StreamId, args.Samples, stream.SampleRate, stream.ChannelCount);
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            if (_streams.TryRemove(args.StreamId, out var stream))
            {
                _mixer.RemoveStream(args.StreamId);
                _micOpen.Remove(stream.Owner);
            }
        };

        // One pump sends each person their own mix, 20 ms at a time. The frames are already mixed
        // and paced, so they go out through Audio.Raw. Send each frame inside the loop, because the
        // frames share a buffer.
        _ = Task.Run(async () =>
        {
            await foreach (var (participantId, frame) in _mixer.StreamAsync(app.StoppingToken))
            {
                await Audio.Raw.SendFrameAsync(MediaTargets.To([participantId]), frame.StreamId, frame.Samples,
                    frame.SampleRate, frame.ChannelCount, frame.IsFirst, frame.IsLast);
            }
        });
    }

    private void JoinCall(int clientId, string name)
    {
        _inCall[clientId] = name;
        _mixer.AddParticipant(clientId);
    }

    private void LeaveCall(int clientId)
    {
        if (!_inCall.Remove(clientId))
        {
            return;
        }

        _mixer.RemoveParticipant(clientId);

        // A microphone left open would still be heard by the others
        foreach (var (streamId, stream) in _streams)
        {
            if (stream.Owner == clientId && _streams.TryRemove(streamId, out _))
            {
                _mixer.RemoveStream(streamId);
            }
        }

        _micOpen.Remove(clientId);
    }

    private void Render(IView view)
    {
        var me = ReactiveScope.ClientId;
        var joined = _inCall.ContainsKey(me);

        view.Column([Card.Default, "gap-4 p-4 w-full max-w-md"], content: col =>
        {
            col.Text([Text.H3], $"In the call ({_inCall.Count})");

            if (_inCall.Count == 0)
            {
                col.Text([Text.Caption], "Nobody is in the call yet.");
            }

            foreach (var (clientId, name) in _inCall)
            {
                col.Row(["items-center justify-between"], key: $"member-{clientId}", content: row =>
                {
                    row.Text([Text.Body], clientId == me ? $"{name} (you)" : name);
                    row.Badge(_micOpen.Contains(clientId) ? "Talking" : "Muted",
                        tone: _micOpen.Contains(clientId) ? SemanticTone.Success : SemanticTone.Neutral);
                });
            }

            if (!joined)
            {
                col.TextField(label: "Your name", bind: _name, placeholder: "Name the others see");
                col.Button([Button.PrimaryMd], text: "Join the call", disabled: string.IsNullOrWhiteSpace(_name.Value),
                    onClick: async () => JoinCall(me, _name.Value.Trim()));
                return;
            }

            col.Row(["gap-2"], content: row =>
            {
                // Echo cancellation is on by default here, which a call on a loudspeaker needs
                row.MicToggleButton([Button.PrimaryMd], text: "Microphone");
                row.Button([Button.OutlineMd], text: "Leave", onClick: async () => LeaveCall(me));
            });
        });
    }
    #endregion
}

// The per-room variant of the same call, for one shared instance with many rooms. It is compiled with
// the pattern; the gallery shows the single call above.
file sealed class VoiceGroupCallRooms(IApp<SessionIdentity, ClientParameters> app)
{
    private Audio Audio { get; } = new(app);

    #region example:pattern-voice-group-call-rooms
    // One call per room in a shared instance: each room has its own mixer and its own pump
    private sealed record RoomCall(GroupAudioMixer Mixer);

    private readonly Lock _callsLock = new();
    private readonly Dictionary<string, RoomCall> _calls = new();

    // Each open microphone, with the call it was opened in
    private readonly ConcurrentDictionary<string, (RoomCall Call, int Owner, int SampleRate, int ChannelCount)> _streams = new();

    // Which room's call each client is in, shown to everyone
    private readonly ReactiveDictionary<int, string> _roomOf = new();

    private void WireRoomCalls()
    {
        app.OnClientLeft(async ctx => LeaveRoomCall(ctx.ClientSessionId));

        Audio.AudioInputStreamBeginAsync += async args =>
        {
            RoomCall? call = null;

            lock (_callsLock)
            {
                if (_roomOf.TryGetValue(args.ClientSessionId, out var room))
                {
                    _calls.TryGetValue(room, out call);
                }
            }

            if (call is null)
            {
                return;
            }

            _streams[args.StreamId] = (call, args.ClientSessionId, args.SampleRate, args.ChannelCount);
            call.Mixer.AddStream(args.StreamId, args.ClientSessionId);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (_streams.TryGetValue(args.StreamId, out var stream))
            {
                stream.Call.Mixer.WriteSamples(args.StreamId, args.Samples, stream.SampleRate, stream.ChannelCount);
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            if (_streams.TryRemove(args.StreamId, out var stream))
            {
                stream.Call.Mixer.RemoveStream(args.StreamId);
            }
        };
    }

    private void JoinRoomCall(int clientId, string room)
    {
        // A client is in one call at a time
        LeaveRoomCall(clientId);

        lock (_callsLock)
        {
            if (!_calls.TryGetValue(room, out var call))
            {
                call = new RoomCall(new GroupAudioMixer());
                _calls[room] = call;
                StartPump(call);
            }

            _roomOf[clientId] = room;
            call.Mixer.AddParticipant(clientId);
        }
    }

    private void LeaveRoomCall(int clientId)
    {
        RoomCall? emptied = null;

        lock (_callsLock)
        {
            if (!_roomOf.TryGetValue(clientId, out var room))
            {
                return;
            }

            _roomOf.Remove(clientId);

            var call = _calls[room];
            call.Mixer.RemoveParticipant(clientId);

            // A microphone left open would still be heard by the others
            foreach (var (streamId, stream) in _streams)
            {
                if (stream.Owner == clientId && _streams.TryRemove(streamId, out _))
                {
                    call.Mixer.RemoveStream(streamId);
                }
            }

            // When the last person leaves, the room's call is closed, which ends its pump
            if (!_roomOf.Values.Contains(room))
            {
                _calls.Remove(room);
                emptied = call;
            }
        }

        if (emptied is not null)
        {
            _ = emptied.Mixer.DisposeAsync().AsTask();
        }
    }

    // The same pump as the single call, one per room
    private void StartPump(RoomCall call)
    {
        _ = Task.Run(async () =>
        {
            await foreach (var (participantId, frame) in call.Mixer.StreamAsync(app.StoppingToken))
            {
                await Audio.Raw.SendFrameAsync(MediaTargets.To([participantId]), frame.StreamId, frame.Samples,
                    frame.SampleRate, frame.ChannelCount, frame.IsFirst, frame.IsLast);
            }
        });
    }
    #endregion
}
