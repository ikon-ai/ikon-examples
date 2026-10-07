namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static async Task<string> DocSoundEffectOneShotAsync()
    {
        #region example:sound-effect-one-shot
        var effect = await SoundEffectGenerator.GenerateAsync("Thunder rumbling in the distance");
        var wavBytes = await effect.GetDataAsync();  // inline bytes, or downloaded when a large result was delivered as a URL (effect.Kind)
        // effect.MimeType, effect.DurationSeconds
        #endregion

        return $"{wavBytes.Length} bytes, {effect.MimeType}";
    }

    private async Task DocSoundEffectStreamedAsync()
    {
        #region example:sound-effect-streamed
        using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
        LiveAudioPlayback? thunder = null;

        await foreach (var audio in generator.GenerateSoundEffectAsync(new SoundEffectGeneratorConfig
        {
            Prompt = "Thunder rumbling in the distance",
            DurationSeconds = 5.0
        }))
        {
            // Opened at the first chunk, which carries the format. Generation outruns playout,
            // so let the whole effect buffer instead of holding the generator to real time.
            thunder ??= Audio.PlayLive(MediaTargets.Everyone, audio.SampleRate, audio.ChannelCount,
                new PlayOptions { Slot = "thunder" }, maxBufferAhead: TimeSpan.FromSeconds(30));

            if (!await thunder.WriteAsync(audio.Samples))
            {
                break;
            }
        }

        thunder?.Complete();   // plays out what is buffered, then ends
        #endregion
    }

    private async Task DocSpeakAsync(int clientSessionId)
    {
        #region example:speak-one-call
        // Generate speech and play it to clients — one call that returns at once. A new line
        // crossfades out the speech still playing (the interrupt behavior a voice app wants).
        // The ElevenLabs default voice is "Sarah", soft and modern. Name another when the product
        // wants it: Jessica, Lily, Matilda, Charlotte (female); George, Brian, Will (male).
        Audio.Speak(MediaTargets.Everyone, "Hello world");
        Audio.Speak(MediaTargets.Everyone, "Hello world", new SpeechOptions { Voice = "George" });

        // Pick a model, shape the delivery, or target specific clients; await Completion to wait
        // for playout. Among ElevenLabs models only Eleven3 takes Instructions; the others (the
        // default ElevenFlash25 included) fail the playback, as does any Speed but null or 1.0.
        var line = Audio.Speak(MediaTargets.To([clientSessionId]), "Hello world", new SpeechOptions
        {
            Model = SpeechGeneratorModel.Eleven3,
            Voice = "Sarah",
            Instructions = "Soft and warm, almost a whisper"
        });

        if (await line.Completion == AudioPlaybackOutcome.Failed)
        {
            Log.Instance.Warning($"Speech for client {clientSessionId} failed: {line.Error?.Message}");
        }
        #endregion
    }

    private static async Task<int> DocSpeechGenerateOneShotAsync()
    {
        #region example:speech-generate-one-shot
        var audio = await SpeechGenerator.GenerateAsync("Hello world");  // ElevenFlash25 (cheap+fast) by default
        // audio.Samples (float[]), audio.SampleRate, audio.ChannelCount
        #endregion

        return audio.Samples.Length + audio.SampleRate + audio.ChannelCount;
    }

    private async Task DocSpeechGenerateStreamedAsync()
    {
        #region example:speech-generate-streamed
        // For generator settings Audio.Speak does not expose (here Language). Audio is an app service property.
        using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
        LiveAudioPlayback? speech = null;

        await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hei maailma", Language = "fi" }))
        {
            // Slot "speech": replaces the line still speaking, and counts as the app speaking for turn detection
            speech ??= Audio.PlayLive(MediaTargets.Everyone, audio.SampleRate, audio.ChannelCount,
                new PlayOptions { Slot = "speech" }, maxBufferAhead: TimeSpan.FromSeconds(30));

            if (!await speech.WriteAsync(audio.Samples))
            {
                break;
            }
        }

        speech?.Complete();
        #endregion
    }

    private static async Task DocSpeechRecognizeBatchAsync(float[] samples)
    {
        #region example:speech-recognize-batch
        using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);

        var transcript = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
        {
            Samples = samples,
            SampleRate = 16000,
            ChannelCount = 1,
            Timestamps = SpeechTimestamps.Word,
        });

        foreach (var word in transcript.Words)   // SpeechWord: Text, Start, End, Confidence, Speaker
        {
            Log.Instance.Info($"[{word.Start.TotalSeconds:F2}] {word.Text}");
        }
        #endregion
    }

    private static void DocMicToggleButton(UIView view)
    {
        #region example:mic-toggle-button
        view.MicToggleButton();
        #endregion
    }

    private static void DocSilenceTriggeredRecognition()
    {
        #region example:silence-triggered-recognition
        var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
        var adapter = new SpeechRecognizerAdapter(recognizer, new SpeechRecognizerAdapter.Config
        {
            Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
            SilenceDuration = TimeSpan.FromMilliseconds(750),
            SilenceThreshold = 0.01f,
            MaxSpeechDuration = TimeSpan.FromSeconds(30)
        });
        #endregion

        adapter.Dispose();
    }

    private void DocRawAudioHandling()
    {
        #region example:raw-audio-handling
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            // Snapshot per-stream state here. args.ClientSessionId / args.UserId identify the client.
            _myStreamStates[args.StreamId] = new MyStreamState(args.ClientContext);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (!_myStreamStates.TryGetValue(args.StreamId, out var state)) return;
            state.AddSamples(args.Samples);
            if (args.IsLast) { /* process state.Samples */ }
        };
        #endregion
    }

    private void DocSpeechRecognition(UIView view)
    {
        #region example:speech-recognition
        // One-time setup in the app
        Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.SpeechRecognizedAsync += async args =>
        {
            // args.Text — recognized speech
            // args.ClientSessionId / args.UserId — who said it
            // ClientScope is established automatically — per-client reactive writes route correctly.
            await SendChatMessageAsync(args.Text);
        };

        // In your UI lambda:
        view.PushToTalkButton(style: [MicButton.States, "w-16 h-16 rounded-full bg-red-600 touch-none select-none"]);
        #endregion
    }
}
