public partial class Validation
{
    // The browsers that hear the live call, and the microphone streams that speak into it. A
    // person verifying telephony by hand talks to their own phone through these.
    private readonly ReactiveHashSet<int> _telListeners = new();
    private readonly ConcurrentDictionary<string, bool> _telMicStreams = new();
    private readonly object _telMicSync = new();
    private Channel<AudioChunk>? _telMicChannel;
    private string _telHeardBy = "";

    private void RenderTelephonyBrowserControls(UIView view)
    {
        int clientId = ReactiveScope.ClientId;

        view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
        {
            view.Switch(
                label: "Hear calls here",
                value: _telListeners.Contains(clientId),
                props: TestId("tel-call-listen"),
                onValueChange: async isOn =>
                {
                    if (isOn)
                    {
                        _telListeners.Add(clientId);
                    }
                    else
                    {
                        _telListeners.Remove(clientId);
                    }
                });

            view.MicToggleButton(
                [Button.OutlineMd, MicButton.States, "gap-2"],
                text: "Talk into the call",
                props: TestId("tel-call-mic"),
                content: view =>
                {
                    view.Icon(name: "mic", size: IconSize.Sm);
                    view.Text(text: "Talk into the call");
                },
                onCaptureStart: async capture =>
                {
                    _telMicStreams[capture.StreamId] = true;
                    OpenBrowserMicrophone();
                },
                onCaptureStop: async capture =>
                {
                    _telMicStreams.TryRemove(capture.StreamId, out _);

                    if (_telMicStreams.IsEmpty)
                    {
                        CloseBrowserMicrophone();
                    }
                });
        });
    }

    /// <summary>
    /// Carries browser microphone audio into the live call while someone is talking. Opened only then:
    /// an open speech stream holds the call's outbound silence off, which would change what every
    /// other button on the card sends.
    /// </summary>
    private void OpenBrowserMicrophone()
    {
        var call = _telActiveCall;
        var cts = _telActiveCallCts;

        if (call == null || cts == null || !call.IsConnected || _telMicStreams.IsEmpty)
        {
            return;
        }

        Channel<AudioChunk> channel;

        lock (_telMicSync)
        {
            if (_telMicChannel != null)
            {
                return;
            }

            channel = Channel.CreateBounded<AudioChunk>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });
            _telMicChannel = channel;
        }

        var ct = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await call.SpeakAsync(channel.Reader.ReadAllAsync(ct), ct);
            }
            catch (OperationCanceledException)
            {
                // The call ended; the microphone stops feeding it.
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Browser microphone on validation call {call.CallId} stopped: {ex.GetType().Name}: {ex.Message}");
            }
        }, CancellationToken.None);
    }

    private void CloseBrowserMicrophone()
    {
        Channel<AudioChunk>? channel;

        lock (_telMicSync)
        {
            channel = _telMicChannel;
            _telMicChannel = null;
        }

        channel?.Writer.TryComplete();
    }

    /// <summary>True when the stream is a "Talk into the call" microphone; its frames belong to the call.</summary>
    private bool IsCallMicrophone(string streamId) => _telMicStreams.ContainsKey(streamId);

    private void TakeCallMicrophoneFrame(string streamId, float[] samples, int sampleRate, int channelCount)
    {
        _telMicChannel?.Writer.TryWrite(new AudioChunk($"mic-{streamId}", samples, sampleRate, channelCount, false, false));
    }

    private async Task SendCallAudioToListenersAsync(string streamId, float[] frame, bool isLast)
    {
        if (_telListeners.Count == 0)
        {
            _telHeardBy = "";
            return;
        }

        var listeners = _telListeners.ToList();

        // A browser switched on mid-call has to see the start of a stream, so it restarts whenever
        // the set of listeners changes.
        var heardBy = string.Join(",", listeners.Order());
        bool isFirst = heardBy != _telHeardBy;
        _telHeardBy = heardBy;

        try
        {
            await Audio.Raw.SendFrameAsync(MediaTargets.To(listeners), streamId, frame, TelSampleRate, 1, isFirst, isLast);
        }
        catch (Exception ex)
        {
            // Only the browser's copy of the call is lost; the call itself carries on.
            Log.Instance.Debug($"Could not send call audio to the browser: {ex.Message}");
        }
    }

    private async Task CloseCallAudioStreamAsync(string streamId)
    {
        _telHeardBy = "";

        try
        {
            await Audio.Raw.CloseStreamAsync(streamId);
        }
        catch (Exception ex)
        {
            // A stream nobody listened to was never opened; there is nothing to release.
            Log.Instance.Debug($"Closing the browser copy of call audio {streamId}: {ex.Message}");
        }
    }
}
