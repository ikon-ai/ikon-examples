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
        await Audio.SpeakAsync(text, voice: "Sarah", speed: 0.95, instructions: "calm, unhurried");
    }

    /// <summary>
    /// The config form, for two speakers at once. Streaming chunk-by-chunk is what lets playback
    /// start before generation finishes.
    /// </summary>
    private async Task SpeakBothAsync(string lineA, string lineB)
    {
        using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);

        try
        {
            await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig
            {
                Text = lineA,
                VoiceId = "Sarah",
                // Speed is honoured by OpenAI and Google and IGNORED by ElevenLabs -- null keeps
                // the model's own default rather than pretending to set one.
                Speed = null,
                Instructions = "warm, close-mic",
            }))
            {
                // SendSpeech does not interrupt, so two voices can overlap -- which SpeakAsync
                // deliberately cannot do.
                Audio.SendSpeech(chunk);
            }
        }
        catch (AIException ex)
        {
            _error.Value = "Couldn't play that line — try again.";
            Log.Instance.Warning($"Speech failed for '{lineA}': {ex.Message}");
        }
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
