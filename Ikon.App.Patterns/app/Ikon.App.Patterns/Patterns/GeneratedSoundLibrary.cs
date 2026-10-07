namespace Ikon.App.Patterns.Patterns;

// Pattern: generated-sound-library — see docs/patterns/generated-sound-library.md.
// The example region below is the canonical body the doc extracts.
internal sealed class GeneratedSoundLibrary(IAppBase app) : IPatternDemo
{
    public string Slug => "generated-sound-library";
    public string Title => "Generated sound library, stored and replayable";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio { get; } = new(app);

    #region example:pattern-generated-sound-library
    private sealed record Clip(string Id, string Label, AudioSound Sound);

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
            // The one-shot hands back an ENCODED file: Data plus MimeType, or a Url instead when
            // ResultDelivery says so. Either becomes a cached sound once, decoded on the server.
            var result = await SoundEffectGenerator.GenerateAsync(prompt);

            AudioSound? sound = result switch
            {
                { Data: { } data } => await Audio.CreateSoundAsync(data, result.MimeType),
                { Url: { } url } => await Audio.CreateSoundAsync(new Uri(url)),
                _ => null
            };

            if (sound != null)
            {
                await AddAsync(prompt, sound);
            }
        }
        catch (AIException)
        {
            // Generation failed; the library keeps what it already has rather than emptying.
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException or HttpRequestException)
        {
            // Not a short WAV, MP3 or Ogg (Vorbis or Opus) clip (or its URL could not be fetched): it is left
            // out, and the library keeps what it already has.
        }
    }

    /// <summary>
    /// PCM needs no file: an AudioClip of the samples is the sound. CreateSound throws
    /// ArgumentException for a clip longer than 30 s -- play that with Audio.Play(targets, clip).
    /// </summary>
    private Task AddPcmAsync(string label, float[] samples, int sampleRate, int channelCount)
    {
        return AddAsync(label, Audio.CreateSound(new AudioClip(samples, sampleRate, channelCount)));
    }

    /// <summary>
    /// Speech is PCM too: the one-shot hands back an AudioChunk -- Samples, SampleRate, ChannelCount --
    /// so it takes the same path as samples you synthesized yourself.
    /// </summary>
    private async Task AddSpokenAsync(string text)
    {
        var speech = await SpeechGenerator.GenerateAsync(text);
        await AddPcmAsync(text, speech.Samples, speech.SampleRate, speech.ChannelCount);
    }

    private async Task AddAsync(string label, AudioSound sound)
    {
        _clips.Add(new Clip(Guid.NewGuid().ToString("N"), label, sound));

        // Sends the bytes to every client now, and to clients that join later, so even the first
        // press plays at once.
        await sound.PreloadAsync(MediaTargets.Everyone);
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                disabled: _busy.Value,
                onClick: async () => await AddGeneratedAsync("a soft chime"),
                content: v => v.Text(text: _busy.Value ? "Generating…" : "Add sound"));

            // Replay costs nothing: no second generation, no custom player component, and no
            // bytes on the wire -- each client plays its own cached copy.
            col.Grid(["grid-cols-3 gap-2"], content: grid =>
            {
                foreach (var clip in _clips)
                {
                    grid.Button(
                        key: clip.Id,
                        onClick: () => Audio.Play(MediaTargets.To(ReactiveScope.ClientId), clip.Sound),
                        content: v => v.Text(text: clip.Label));
                }
            });
        });
    }
    #endregion
}
