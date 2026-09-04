<!-- mined-from: Ikon.App.Patterns -->
# Generated Sound Library — Keeping Clips Replayable

Two shapes of audio reach an app, and only one of them is already a file. `SoundEffectGenerator`
and `SpeechGenerator` one-shots hand back an **encoded** result — `Data` plus `MimeType` — which is
stored and replayed as it is. Raw PCM you synthesized yourself (`Ikon.Resonance.Synth`, or samples
you computed) is **not** a file until `WavFile` wraps it.

Keep the bytes in state and replay costs nothing: no second generation, and no custom player
component — `ClientFunctions.PlaySoundAsync(data, mimeType)` is the whole playback path, and it
de-duplicates per client session by content hash, so a clip is transmitted once however often it
plays.

## When to use

A soundboard, a generated effects library, a pronunciation list, anything where the same clip is
played more than once. Also the answer to "should this be `view.Audio` or a custom player" — for
fire-and-forget playback it is neither.

## Notes

- `Data` is nullable on these results: a result can arrive as a URL instead, decided by
  `ResultDelivery`. A library that stores bytes checks before storing.
- `WavFile` finalizes its header the first time the data is read, so add every sample before
  `AsArray()`/`AsStream()` and add none afterwards — a later `AddSamples` throws.
- `AsArray()` copies; `AsStream()` returns a fresh independent stream per call and survives the
  builder being disposed.
- `PlaySoundAsync` returns a playback id — hold it if you need `StopSoundAsync`.
- For continuous or real-time audio use `Audio.StreamAsync` instead; this pattern is for discrete
  clips.

## Snippet

```csharp
private sealed record Clip(string Id, string Label, byte[] Data, string MimeType);

private readonly ReactiveList<Clip> _clips = new();
private readonly Reactive<bool> _busy = new(false);

private async Task AddGeneratedAsync(string prompt)
{
    if (_busy.Value)
    {
        return;
    }

    using var _ = _busy.AsToken();

    try
    {
        // The one-shot already hands back an ENCODED file -- Data plus MimeType. Nothing to
        // wrap: store those bytes as they are. Data is nullable because a result can arrive
        // as a URL instead (ResultDelivery), so a library that keeps bytes checks first.
        var sound = await SoundEffectGenerator.GenerateAsync(prompt);

        if (sound.Data is { } data)
        {
            _clips.Add(new Clip(Guid.NewGuid().ToString("N"), prompt, data, sound.MimeType));
        }
    }
    catch (AIException)
    {
        // Generation failed; the library keeps what it already has rather than emptying.
    }
}

/// <summary>
/// The other direction: raw PCM you synthesized yourself is not a file until it is wrapped.
/// WavFile finalizes its header on first access, so add every sample before AsArray and never
/// add more afterwards.
/// </summary>
private void AddSynthesized(string label, float[] samples, int sampleRate)
{
    using var wav = new WavFile(sampleRate, channelCount: 1, WavFile.SampleFormat.Float);
    wav.AddSamples(samples);
    _clips.Add(new Clip(Guid.NewGuid().ToString("N"), label, wav.AsArray(), "audio/wav"));
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        col.Button(
            disabled: _busy.Value,
            onClick: async () => await AddGeneratedAsync("a soft chime"),
            content: v => v.Text(text: _busy.Value ? "Generating…" : "Add sound"));

        // Replay costs nothing: the bytes are in state, so no second generation and no
        // custom player component. PlaySoundAsync de-duplicates by content hash, so the same
        // clip is transmitted once per client however often it is played.
        col.Grid(["grid-cols-3 gap-2"], content: grid =>
        {
            foreach (var clip in _clips)
            {
                grid.Button(
                    key: clip.Id,
                    onClick: async () => await ClientFunctions.PlaySoundAsync(clip.Data, clip.MimeType),
                    content: v => v.Text(text: clip.Label));
            }
        });
    });
}
```

## See also

- `busy-flag-loading` — the guard around the generation call.
