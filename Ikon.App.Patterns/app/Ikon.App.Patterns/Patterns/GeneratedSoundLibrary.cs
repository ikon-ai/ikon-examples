namespace Ikon.App.Patterns.Patterns;

// Pattern: generated-sound-library — see docs/patterns/generated-sound-library.md.
// The example region below is the canonical body the doc extracts. The demo starts with an empty
// library: LoadAsync reads app files, which only a running app has.
internal sealed class GeneratedSoundLibrary(IAppBase app) : IPatternDemo
{
    public string Slug => "generated-sound-library";
    public string Title => "Generated sound library, stored and replayable";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio { get; } = new(app);

    #region example:pattern-generated-sound-library
    // What survives a restart is the stored file and its label. The AudioSound is rebuilt from the
    // file when the app starts, so a restart pays for no generation.
    private sealed record SavedClip(string Id, string Label, string Path, string MimeType);
    private sealed record Clip(SavedClip Saved, AudioSound Sound);

    // App files cannot be listed, so the library keeps its own index of what it stored
    private const string IndexPath = "sounds/index.json";

    private readonly ReactiveList<Clip> _clips = new();
    private readonly Reactive<bool> _busy = new(false);

    /// <summary>Call from app.OnStarting.</summary>
    private async Task LoadAsync()
    {
        List<SavedClip> saved;

        try
        {
            saved = JsonSerializer.Deserialize<List<SavedClip>>(await app.Files.Data.ReadTextAsync(IndexPath)) ?? [];
        }
        catch (FileNotFoundException)
        {
            // Nothing has been stored yet
            return;
        }

        var clips = await Task.WhenAll(saved.Select(async clip =>
            new Clip(clip, await Audio.CreateSoundAsync(await app.Files.Data.ReadBytesAsync(clip.Path), clip.MimeType))));
        _clips.ReplaceAll(clips);

        foreach (var clip in clips)
        {
            await clip.Sound.PreloadAsync(MediaTargets.Everyone);
        }
    }

    private async Task AddGeneratedAsync(string prompt)
    {
        if (_busy.Value)
        {
            return;
        }

        using var busy = _busy.AsToken();

        try
        {
            // The one-shot hands back an ENCODED file: Data plus MimeType, or a Url instead when
            // ResultDelivery says so. It is stored before anything else, so it is paid for once;
            // a Url is copied into the app's files, so the library does not depend on it lasting.
            var result = await SoundEffectGenerator.GenerateAsync(prompt);
            var id = Guid.NewGuid().ToString("N");
            var path = $"sounds/{id}";

            if (result.Data is { } data)
            {
                await app.Files.Data.WriteBytesAsync(path, data, result.MimeType);
            }
            else if (result.Url is { } url)
            {
                await app.Files.Data.WriteFromUrlAsync(path, url, result.MimeType);
            }
            else
            {
                return;
            }

            var encoded = result.Data ?? await app.Files.Data.ReadBytesAsync(path);
            await AddAsync(new SavedClip(id, prompt, path, result.MimeType), encoded);
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
    /// PCM is not a file, so it is written into a WAV to be stored. CreateSoundAsync throws
    /// ArgumentException for a clip longer than 30 s -- play that with Audio.Play(targets, clip).
    /// </summary>
    private async Task AddPcmAsync(string label, float[] samples, int sampleRate, int channelCount)
    {
        using var wav = new WavFile(sampleRate, channelCount, WavFile.SampleFormat.Float);
        wav.AddSamples(samples);
        var encoded = wav.AsArray();

        var id = Guid.NewGuid().ToString("N");
        var saved = new SavedClip(id, label, $"sounds/{id}.wav", "audio/wav");
        await app.Files.Data.WriteBytesAsync(saved.Path, encoded, saved.MimeType);
        await AddAsync(saved, encoded);
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

    private async Task AddAsync(SavedClip saved, byte[] encoded)
    {
        var sound = await Audio.CreateSoundAsync(encoded, saved.MimeType);
        _clips.Add(new Clip(saved, sound));
        await app.Files.Data.WriteTextAsync(IndexPath, JsonSerializer.Serialize(_clips.Select(clip => clip.Saved).ToList()));

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
                        key: clip.Saved.Id,
                        onClick: () => Audio.Play(MediaTargets.To(ReactiveScope.ClientId), clip.Sound),
                        content: v => v.Text(text: clip.Saved.Label));
                }
            });
        });
    }
    #endregion
}
