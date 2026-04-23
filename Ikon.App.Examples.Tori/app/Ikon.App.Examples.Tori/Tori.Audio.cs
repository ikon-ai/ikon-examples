public partial class Tori
{
    private void SetupAudioInputHandlers()
    {
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            var clientId = args.ClientSessionId;
            var state = new AudioStreamState(args.SampleRate, args.ChannelCount, clientId);
            _audioStreamStates[args.StreamId] = state;

            _speakingStates[clientId] = new SpeakingState();

            if (_groupAudioMixer == null)
            {
                _groupAudioMixer = new GroupAudioMixer();
                await _groupAudioMixer.StartAsync(OnGroupAudioMixerOutputAsync);

                foreach (var p in _participants.Value)
                {
                    _groupAudioMixer.AddParticipant(p.ClientSessionId.ToString());
                }
            }

            _groupAudioMixer.AddStream(args.StreamId, clientId.ToString());

            UpdateParticipant(clientId, p => p with { IsAudioEnabled = true });

            var participant = _participants.Value.FirstOrDefault(p => p.ClientSessionId == clientId);

            if (participant != null)
            {
                InitializeSpeechRecognitionForParticipant(clientId, participant.Name);
            }
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (!_audioStreamStates.TryGetValue(args.StreamId, out var state))
            {
                return;
            }

            UpdateSpeakingState(args.ClientSessionId, args.Samples);

            _groupAudioMixer?.WriteSamples(args.StreamId, args.Samples, state.SampleRate, state.ChannelCount);

            if (_speechEnabled.Value && _participantSpeechStates.TryGetValue(state.ClientSessionId, out var speechState))
            {
                speechState.AudioChannel.Writer.TryWrite(args.Samples.ToArray());
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _groupAudioMixer?.RemoveStream(args.StreamId);

            if (_audioStreamStates.TryGetValue(args.StreamId, out var state))
            {
                StopSpeechRecognitionForParticipant(state.ClientSessionId);
            }

            if (_speakingStates.TryGetValue(args.ClientSessionId, out var speakingState) && speakingState.IsSpeaking)
            {
                speakingState.IsSpeaking = false;
                _speakingVersion.Value++;
            }

            _speakingStates.Remove(args.ClientSessionId);
            _audioStreamStates.Remove(args.StreamId);
            UpdateParticipant(args.ClientSessionId, p => p with { IsAudioEnabled = false });
        };
    }

    private async Task OnAudioCaptureStart(MediaCaptureEvent e)
    {
        _isAudioEnabled.Value = true;
        _activeAudioStreamId.Value = e.StreamId;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsAudioEnabled = true });
        }
    }

    private async Task OnAudioCaptureStop(MediaCaptureEvent e)
    {
        _isAudioEnabled.Value = false;
        _activeAudioStreamId.Value = null;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsAudioEnabled = false });
        }
    }

    private void UpdateSpeakingState(int clientSessionId, ReadOnlySpan<float> samples)
    {
        if (!_speakingStates.TryGetValue(clientSessionId, out var state))
        {
            return;
        }

        // Calculate RMS volume
        float sumSquares = 0;

        foreach (var t in samples)
        {
            sumSquares += t * t;
        }

        float rmsVolume = MathF.Sqrt(sumSquares / samples.Length);

        // Asymmetric EMA: fast attack, slow decay for smoother experience
        float alpha = rmsVolume > state.EmaVolume ? EmaAlphaUp : EmaAlphaDown;
        state.EmaVolume = (alpha * rmsVolume) + ((1 - alpha) * state.EmaVolume);
        state.LastAudioTime = DateTime.UtcNow;

        // Determine if speaking
        bool wasSpeaking = state.IsSpeaking;
        state.IsSpeaking = state.EmaVolume > SpeakingVolumeThreshold;

        // Trigger UI update if speaking status changed
        if (wasSpeaking != state.IsSpeaking)
        {
            _speakingVersion.Value++;
        }
    }

    private bool GetIsSpeaking(int clientSessionId)
    {
        if (!_speakingStates.TryGetValue(clientSessionId, out var state))
        {
            return false;
        }

        // Check if audio is recent enough
        if (DateTime.UtcNow - state.LastAudioTime > SpeakingTimeout)
        {
            return false;
        }

        return state.IsSpeaking;
    }

    private async ValueTask OnGroupAudioMixerOutputAsync(string excludeKey, AudioFrameEx frame)
    {
        if (!int.TryParse(excludeKey, out var targetId))
        {
            return;
        }

        await Audio.SendAsync(frame.Samples, frame.SampleRate, frame.ChannelCount,
            frame.IsFirst, frame.IsLast, frame.StreamId, targetIds: [targetId]);
    }
}
