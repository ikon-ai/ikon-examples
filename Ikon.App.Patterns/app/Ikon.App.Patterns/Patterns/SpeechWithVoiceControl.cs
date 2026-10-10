namespace Ikon.App.Patterns.Patterns;

// Pattern: speech-with-voice-control — see docs/patterns/speech-with-voice-control.md.
// The example region below is the canonical body the doc extracts.
internal sealed class SpeechWithVoiceControl(IAppBase app) : IPatternDemo
{
    public string Slug => "speech-with-voice-control";
    public string Title => "Speech with voice, speed and delivery";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio { get; } = new(app);

    #region example:pattern-speech-with-voice-control
    private readonly ClientReactive<string?> _error = new(null);

    /// <summary>
    /// Audio.Speak is the whole path for ordinary narration: one call that returns at once, and
    /// each line crossfades out the previous one in slot "speech". The options cover the rest --
    /// Mode = Queue for a line that must NOT interrupt, a slot of its own for a second speaker
    /// (see OverlapAsync). Drive SpeechGenerator yourself only for generator settings Speak does
    /// not expose.
    /// </summary>
    private async Task NarrateAsync(string text)
    {
        _error.Value = null;

        // Instructions need ElevenLabs v3; the default ElevenFlash25 fails the playback. No Speed:
        // ElevenLabs fails any but 1.0. Pass one only with an OpenAI, Google or Azure model, e.g.
        // Model = SpeechGeneratorModel.Gpt4OmniMiniTts, Speed = 0.95.
        var line = Audio.Speak(MediaTargets.Everyone, text, new SpeechOptions
        {
            Model = SpeechGeneratorModel.Eleven3,
            Voice = "Sarah",
            Instructions = "calm, unhurried",
        });

        // Completion never throws: a generation failure is an outcome, with the cause in Error
        if (await line.Completion == AudioPlaybackOutcome.Failed)
        {
            _error.Value = "Couldn't play that line — try again.";
        }
    }

    /// <summary>
    /// The config form, for generator settings Speak does not expose. Writing chunks into a live
    /// playback as they arrive is what lets playback start before generation finishes. Slot
    /// "speech" keeps it the app's speech: it replaces the line still speaking, as Speak would,
    /// and turn detection counts it as the app talking.
    /// </summary>
    private async Task SpeakWithConfigAsync(string line)
    {
        using var generator = new SpeechGenerator(SpeechGeneratorModel.Eleven3);
        LiveAudioPlayback? speech = null;

        try
        {
            await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig
            {
                Text = line,
                VoiceId = "Sarah",
                // Speed is honoured by OpenAI, Azure and Google's Cloud TTS voices; ElevenLabs and
                // Gemini 3.8 TTS throw NonRetryableAIException for anything but null or 1.0, so null
                // is required here.
                Speed = null,
                Instructions = "warm, close-mic",
            }))
            {
                // Generation runs ahead of playout; a generous buffer lets it finish without waiting
                speech ??= Audio.PlayLive(MediaTargets.Everyone, chunk.SampleRate, chunk.ChannelCount,
                    new PlayOptions { Slot = "speech" }, maxBufferAhead: TimeSpan.FromSeconds(30));

                if (!await speech.WriteAsync(chunk.Samples))
                {
                    return;
                }
            }

            speech?.Complete();
        }
        catch (AIException ex)
        {
            speech?.Stop();
            _error.Value = "Couldn't play that line — try again.";
            Log.Instance.Warning($"Speech failed for '{line}': {ex.Message}");
        }
    }

    /// <summary>
    /// Two voices at once. Each speaker gets a slot of its own, so neither line replaces the
    /// other: they play together, panned apart, and each speaker's next line replaces only its own.
    /// </summary>
    private async Task OverlapAsync(string lineA, string lineB)
    {
        var a = Audio.Speak(MediaTargets.Everyone, lineA, new SpeechOptions { Voice = "Sarah" },
            new PlayOptions { Slot = "voice-a", Pan = -0.5f });
        var b = Audio.Speak(MediaTargets.Everyone, lineB, new SpeechOptions { Voice = "George" },
            new PlayOptions { Slot = "voice-b", Pan = 0.5f });

        await Task.WhenAll(a.Completion, b.Completion);
    }

    /// <summary>
    /// Sound effects are the same shape with a different knob: PromptInfluence trades literal
    /// obedience against sounding good, and Loop asks for a seamlessly loopable result.
    /// </summary>
    private static async Task<SoundEffectGeneratorResult> AmbienceAsync(string prompt)
    {
        using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);

        return await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig
        {
            Prompt = prompt,
            DurationSeconds = 8,
            Loop = true,
            PromptInfluence = 0.6,
        });
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            col.Button(
                onClick: async () => await NarrateAsync("Chapter one."),
                content: v => v.Text(text: "Narrate"));

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], text: error);
            }
        });
    }
    #endregion
}
