namespace Ikon.App.Patterns.Patterns;

// Pattern: generated-sound-library — see docs/patterns/generated-sound-library.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class GeneratedSoundLibrary : IPatternDemo
{
    public string Slug => "generated-sound-library";
    public string Title => "Generated sound library, stored and replayable";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-generated-sound-library
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
    #endregion
}
