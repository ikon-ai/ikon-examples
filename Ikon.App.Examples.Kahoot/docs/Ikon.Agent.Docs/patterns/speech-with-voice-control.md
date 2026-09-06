<!-- mined-from: Ikon.App.Patterns -->
# Speech With Voice Control — SpeakAsync Until It Cannot

`Audio.SpeakAsync(text, voice:, speed:, instructions:)` is the whole path for ordinary narration:
one call, and each call **interrupts** the previous one — it fades out what is still playing and
cancels the prior generation, which is what you want for a narrator or an assistant.

Drive `SpeechGenerator` yourself only for the three things that cannot do: **overlapping speakers**,
speech that must **not** interrupt what is already playing, and **raw access** to the samples.

## When to use

Narration, an assistant that talks, character voices in a scene, generated ambience. For a clip you
will replay, store the bytes — see `generated-sound-library`.

## Notes

- **`Speed` is honoured by OpenAI and Google and ignored by ElevenLabs.** Leave it `null` to keep
  the model's own default rather than setting a value that silently does nothing.
- `Instructions` (tone, emotion, style) is likewise model-specific; unsupported models ignore it.
- `SpeechGenerator.GenerateSpeechAsync` streams `AudioChunk`s, so playback can start before
  generation finishes. `Audio.SpeakChunk(chunk)` plays one **without** interrupting, which is how
  two voices overlap.
- Failure throws `RetryableAIException`, or `NonRetryableAIException` when the input is at
  fault. Catch `AIException` for both. Render a short human
  sentence and a retry — never the provider's message, and never leave the surface blank.
- `SpeechRecognizerAdapter` wraps an `ISpeechRecognizer` to normalise sample rate and channel
  count, for feeding a recognizer audio that does not already match its expectations.
- Sound effects are the same shape with different knobs: `PromptInfluence` trades literal obedience
  against sounding good, and `Loop` asks for a seamlessly loopable result. `DurationSeconds` is
  nullable — leave it null to let the model choose.
- `Audio.SpeakAndWaitAsync` completes at the end of **playout**, not generation, and is
  pause-aware — the one to await when the next thing must not start until the line is finished.

## Snippet

```csharp
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
```

## See also

- `voice-effect-chain` — the `effects:` parameter on the same calls.
- `generated-sound-library` — storing and replaying clips.
- `voice-loop` — mic capture through STT, LLM and TTS.
