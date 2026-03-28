public partial class Tori
{
    private void InitializeSpeechRecognitionForParticipant(int clientSessionId, string participantName)
    {
        if (!_speechEnabled.Value)
        {
            return;
        }

        if (_participantSpeechStates.ContainsKey(clientSessionId))
        {
            return;
        }

        try
        {
            var model = _speechModel.Value;

            var state = new ParticipantSpeechState(participantName, model);
            _participantSpeechStates[clientSessionId] = state;

            _ = RunSpeechRecognitionForParticipantAsync(state);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to initialize speech recognition for {participantName}: {ex.Message}");
        }
    }

    private async Task RunSpeechRecognitionForParticipantAsync(ParticipantSpeechState state)
    {
        var config = new RecognizeContinuousSpeechConfig
        {
            SampleRate = 48000,
            ChannelCount = 1,
            Language = _speechLanguage.Value
        };

        try
        {
            await foreach (var text in state.Adapter.RecognizeContinuousSpeechAsync(
                config,
                SilenceRemover.FilterAsync(ReadParticipantAudioAsync(state), config.SampleRate, config.ChannelCount),
                state.Cts.Token))
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var entry = new TranscriptEntry(state.ParticipantName, text, DateTime.UtcNow);
                    var list = _recognizedSpeech.Value.TakeLast(MaxTranscriptEntries - 1).ToList();
                    list.Add(entry);
                    _recognizedSpeech.Value = list;
                    _recognizedSpeechVersion.Value++;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Speech recognition error for {state.ParticipantName}: {ex.Message}");
        }
    }

    private async IAsyncEnumerable<float[]> ReadParticipantAudioAsync(
        ParticipantSpeechState state,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, state.Cts.Token);

        await foreach (var samples in state.AudioChannel.Reader.ReadAllAsync(linkedCts.Token))
        {
            yield return samples;
        }
    }

    private void StopSpeechRecognitionForParticipant(int clientSessionId)
    {
        if (_participantSpeechStates.TryGetValue(clientSessionId, out var state))
        {
            state.Dispose();
            _participantSpeechStates.Remove(clientSessionId);
        }
    }

    private void RestartSpeechRecognition()
    {
        // Stop all existing recognizers
        foreach (var state in _participantSpeechStates.Values)
        {
            state.Dispose();
        }

        _participantSpeechStates.Clear();

        // Restart for all participants who have audio enabled
        foreach (var participant in _participants.Value)
        {
            if (participant.IsAudioEnabled)
            {
                InitializeSpeechRecognitionForParticipant(participant.ClientSessionId, participant.Name);
            }
        }
    }

    private void StopSpeechRecognition()
    {
        foreach (var state in _participantSpeechStates.Values)
        {
            state.Dispose();
        }

        _participantSpeechStates.Clear();
    }
}
