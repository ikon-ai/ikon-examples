public partial class LearningApp
{
    private void SetupAudioInputHandlers()
    {
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            Log.Instance.Info($"[STT] Stream begin - StreamId={args.StreamId}, SampleRate={args.SampleRate}");
            IsListening.Value = true;
            InterruptSpeaking();
            var state = new SttStreamState(args.SampleRate, args.ChannelCount);
            _sttStreamStates[args.StreamId.ToString()] = state;
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (!_sttStreamStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                return;
            }

            if (args.IsFirst)
            {
                Log.Instance.Info($"[STT] First frame, samples={args.Samples.Length}");
                state.Reset();
            }

            state.AddSamples(args.Samples);

            if (args.IsLast)
            {
                state.Complete();
                RunBatchRecognitionAsync(state).RunParallel();
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            IsListening.Value = false;

            if (_sttStreamStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                state.Complete();
                _sttStreamStates.Remove(args.StreamId.ToString());
            }
        };
    }

    // Minimum RMS threshold for audio to be considered speech (not silence)
    private const float AudioSilenceThreshold = 0.01f;

    private async Task RunBatchRecognitionAsync(SttStreamState state)
    {
        try
        {
            var samples = state.GetAllSamples();

            Log.Instance.Info($"[STT] Processing {samples.Length} samples, SampleRate={state.SampleRate}, Channels={state.ChannelCount}");

            if (samples.Length == 0)
            {
                Log.Instance.Warning("[STT] No samples to process");
                return;
            }

            // Check if audio is silence or too quiet
            var rms = CalculateRms(samples);

            if (rms < AudioSilenceThreshold)
            {
                Log.Instance.Info($"[STT] Audio too quiet (RMS={rms:F4}), skipping recognition");
                return;
            }

            Log.Instance.Info($"[STT] Audio level OK (RMS={rms:F4}), proceeding with recognition");

            using var recognizer = new SpeechRecognizer(_sttModel.Value);
            var config = new RecognizeSpeechConfig
            {
                Samples = samples,
                SampleRate = state.SampleRate,
                ChannelCount = state.ChannelCount,
                Language = _currentLanguage.Value
            };

            var result = await recognizer.RecognizeBatchSpeechAsync(config);

            Log.Instance.Info($"[STT] Recognition result: '{result}'");

            if (!string.IsNullOrWhiteSpace(result))
            {
                if (TranscriptionCallback != null)
                {
                    TranscriptionCallback(result);
                }
                else
                {
                    AddChatMessage(ChatRole.User, result);
                    await ProcessUserMessageAsync(result);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Speech recognition error: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate RMS (root mean square) of audio samples to determine volume level.
    /// </summary>
    private static float CalculateRms(float[] samples)
    {
        if (samples.Length == 0)
        {
            return 0f;
        }

        double sumSquares = 0;

        foreach (var sample in samples)
        {
            sumSquares += sample * sample;
        }

        return (float)Math.Sqrt(sumSquares / samples.Length);
    }
}
