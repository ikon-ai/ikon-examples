public partial class Tori
{
    private void SetupAudioInputHandlers()
    {
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            var clientId = args.ClientSessionId;
            var state = new AudioStreamState(args.SampleRate, args.ChannelCount, clientId);
            _audioStreamStates[args.StreamId] = state;

            // Initialize speaking state for this participant
            _speakingStates[clientId] = new SpeakingState();

            UpdateParticipant(clientId, p => p with { IsAudioEnabled = true });

            // Initialize speech recognition for this participant
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

            // Calculate RMS volume and update speaking state
            UpdateSpeakingState(args.ClientSessionId, args.Samples);

            // Broadcast to all participants EXCEPT the sender
            var targetIds = _participants.Value
                .Where(p => p.ClientSessionId != args.ClientSessionId)
                .Select(p => p.ClientSessionId)
                .ToList();

            if (targetIds.Count > 0)
            {
                await Audio.SendAsync(args.Samples, state.SampleRate, state.ChannelCount, args.IsFirst, args.IsLast, args.StreamId, targetIds: targetIds);
            }

            // Send to this participant's speech recognizer
            if (_speechEnabled.Value && _participantSpeechStates.TryGetValue(state.ClientSessionId, out var speechState))
            {
                speechState.AudioChannel.Writer.TryWrite(args.Samples.ToArray());
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            if (_audioStreamStates.TryGetValue(args.StreamId, out var state))
            {
                // Stop speech recognition for this participant
                StopSpeechRecognitionForParticipant(state.ClientSessionId);
            }

            // Clean up speaking state
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
}
