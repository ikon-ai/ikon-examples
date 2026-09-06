namespace Ikon.App.Patterns.Patterns;

// Pattern: speech-with-voice-control — see docs/patterns/speech-with-voice-control.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class SpeechWithVoiceControl : IPatternDemo
{
    public string Slug => "speech-with-voice-control";
    public string Title => "Speech with voice, speed and delivery";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio => throw new NotImplementedException();

    #region docsnippet:pattern-speech-with-voice-control
    private readonly ClientReactive<string?> _error = new(null);

    /// <summary>
    /// Audio.SpeakAsync is the whole path for ordinary narration: one call, and each one
    /// interrupts the previous. Drive SpeechGenerator yourself only for what that cannot do --
    /// overlapping speakers, speech that must NOT interrupt, or raw access to the samples.
    /// </summary>
    private async Task NarrateAsync(string text)
    {
        await Audio.SpeakAsync(MediaTargets.Everyone, text, voice: "Sarah", speed: 0.95, instructions: "calm, unhurried");
    }

    /// <summary>
    /// The config form, for generator settings SpeakAsync does not expose. Streaming chunk-by-chunk
    /// is what lets playback start before generation finishes. This does NOT give you two speakers
    /// at once: every SpeakChunk goes through the app's single speech mixer, which holds one
    /// utterance at a time, so a second voice's chunks interrupt the first with a fade. Genuine
    /// overlap means leaving the speech lane -- see OverlapAsync below.
    /// </summary>
    private async Task SpeakWithConfigAsync(string line)
    {
        using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);

        try
        {
            await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig
            {
                Text = line,
                VoiceId = "Sarah",
                // Speed is honoured by OpenAI and Google and IGNORED by ElevenLabs -- null keeps
                // the model's own default rather than pretending to set one.
                Speed = null,
                Instructions = "warm, close-mic",
            }))
            {
                Audio.SpeakChunk(MediaTargets.Everyone, chunk);
            }
        }
        catch (AIException ex)
        {
            _error.Value = "Couldn't play that line — try again.";
            Log.Instance.Warning($"Speech failed for '{line}': {ex.Message}");
        }
    }

    /// <summary>
    /// Two voices at once. The speech mixer cannot do this -- it plays one utterance at a time --
    /// so overlap is the direct lane's job: PlayClipAsync sends an independent stream per stream id,
    /// and streams play alongside each other and alongside speech.
    /// </summary>
    private async Task OverlapAsync(AudioChunk voiceA, AudioChunk voiceB)
    {
        await Task.WhenAll(
            Audio.PlayClipAsync(MediaTargets.Everyone, voiceA.Samples, voiceA.SampleRate, voiceA.ChannelCount, streamId: "voice-a"),
            Audio.PlayClipAsync(MediaTargets.Everyone, voiceB.Samples, voiceB.SampleRate, voiceB.ChannelCount, streamId: "voice-b"));
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
