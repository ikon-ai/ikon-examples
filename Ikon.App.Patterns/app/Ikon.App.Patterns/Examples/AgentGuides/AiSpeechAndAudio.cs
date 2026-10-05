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
        await foreach (var audio in generator.GenerateSoundEffectAsync(new SoundEffectGeneratorConfig
        {
            Prompt = "Thunder rumbling in the distance",
            DurationSeconds = 5.0
        }))
        {
            Audio.SpeakChunk(MediaTargets.Everyone, audio);
        }
        #endregion
    }

    private async Task DocSpeakAsync(int clientSessionId)
    {
        #region example:speak-one-call
        // Generate speech and play it to clients — one call. A new call fades out and
        // replaces whatever is still playing (the interrupt behavior a voice app wants).
        // Name a voice that fits the product: the bare default ("Aria") is a mature, hard read
        // that suits few apps — "Sarah" is a softer, modern one to reach for. Other voices:
        // Jessica, Lily, Matilda, Charlotte (female); George, Brian, Will (male).
        await Audio.SpeakAsync(MediaTargets.Everyone, "Hello world", voice: "Sarah");

        // Pick a model, shape the delivery, or target specific clients. Among ElevenLabs models
        // only Eleven3 takes `instructions`; the others (the default ElevenFlash25 included) throw:
        await Audio.SpeakAsync(MediaTargets.To([clientSessionId]), "Hello world", SpeechGeneratorModel.Eleven3, voice: "Sarah",
            instructions: "Soft and warm, almost a whisper", speed: 0.96);  // speed is a double, 1.0 = normal
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
        using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
        await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hei maailma", Language = "fi" }))
        {
            Audio.SpeakChunk(MediaTargets.Everyone, audio);  // Audio is an app service property
        }
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
