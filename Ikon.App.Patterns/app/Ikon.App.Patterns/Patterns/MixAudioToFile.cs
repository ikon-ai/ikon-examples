using System.Diagnostics;
using System.Globalization;

namespace Ikon.App.Patterns.Patterns;

// Pattern: mix-audio-to-file — see docs/patterns/mix-audio-to-file.md.
// In the gallery the voice and the music come from the AI mocks (silence), so the button only shows
// that the ffmpeg mix runs and stores a playable file. It needs ffmpeg on the machine running the gallery.
internal sealed class MixAudioToFile(IAppBase app) : IPatternDemo
{
    public string Slug => "mix-audio-to-file";
    public string Title => "Mix voice and music into one file";
    public string Category => "Voice & audio";

    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _error = new(null);
    private readonly ClientReactive<string?> _jingleUrl = new(null);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-md"], content: col =>
        {
            col.Button([Button.PrimaryMd, "self-start"], text: _busy.Value ? "Mixing…" : "Make a 10-second jingle at 1.2× voice speed",
                disabled: _busy.Value, onClick: async () =>
                {
                    GalleryMocks.Require();
                    _busy.Value = true;
                    _error.Value = null;

                    try
                    {
                        var path = await RenderJingleAsync("Good morning from Radio Aalto!", "Upbeat morning radio jingle, bright synths", 10, 1.2);
                        _jingleUrl.Value = await app.Files.Data.GetUrlAsync(path);
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Warning($"Jingle mix failed, the button is offered again: {ex.Message}");
                        _error.Value = ex.Message;
                    }
                    finally
                    {
                        _busy.Value = false;
                    }
                });

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], error);
            }

            if (_jingleUrl.Value is { } url)
            {
                col.AudioUrlPlayer(url: url, controls: true, preload: "auto", key: url);
            }
        });
    }

    #region example:pattern-mix-audio-to-file
    // Renders a voice-over over a music bed into one stored MP3: the music dips while the voice
    // speaks, the voice is time-stretched to voiceSpeed (0.5 or more), and the mix fades in and out.
    private async Task<string> RenderJingleAsync(string script, string musicPrompt, double seconds, double voiceSpeed)
    {
        var voice = await SpeechGenerator.GenerateAsync(script);
        using var musicGenerator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);
        var music = await musicGenerator.GenerateMusicFileAsync(new MusicGeneratorConfig
        {
            Prompt = musicPrompt,
            DurationSeconds = seconds,
            ForceInstrumental = true,
        });

        var folder = Directory.CreateTempSubdirectory("jingle-").FullName;

        try
        {
            var voicePath = Path.Combine(folder, "voice.wav");
            var musicPath = Path.Combine(folder, "music");
            var mixPath = Path.Combine(folder, "jingle.mp3");

            using (var wav = new WavFile(voice.SampleRate, voice.ChannelCount, WavFile.SampleFormat.Float))
            {
                wav.AddSamples(voice.Samples);
                wav.SaveToFile(voicePath);
            }

            await File.WriteAllBytesAsync(musicPath, await music.GetDataAsync());

            // Both inputs are converted to 44.1 kHz stereo first, so the compressor and the mixer get one
            // format. The voice starts half a second in, and apad keeps the sidechain running after the voice ends.
            var filter = string.Create(CultureInfo.InvariantCulture,
                $"[0:a]aformat=sample_rates=44100:channel_layouts=stereo,atempo={voiceSpeed:0.##},adelay=500:all=1,apad,asplit=2[sc][vo];" +
                $"[1:a]aformat=sample_rates=44100:channel_layouts=stereo[music];" +
                $"[music][sc]sidechaincompress=threshold=0.02:ratio=8:attack=20:release=400[bed];" +
                $"[bed][vo]amix=inputs=2:duration=first:normalize=0,afade=t=in:d=0.3,afade=t=out:st={seconds - 1.5:0.##}:d=1.5[mix]");

            await RunFfmpegAsync("-y", "-threads", "1", "-i", voicePath, "-i", musicPath, "-filter_threads", "1",
                "-filter_complex", filter, "-map", "[mix]", "-t", seconds.ToString(CultureInfo.InvariantCulture),
                "-c:a", "libmp3lame", "-b:a", "192k", mixPath);

            var path = $"jingles/{Guid.NewGuid():N}.mp3";
            await app.Files.Data.WriteBytesAsync(path, await File.ReadAllBytesAsync(mixPath), "audio/mpeg");
            return path;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // ffmpeg writes its progress to stderr. Reading stderr to the end keeps the process from blocking
    // on a full pipe, and when ffmpeg fails, the last lines of stderr are the error.
    private static async Task RunFfmpegAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("ffmpeg did not start");
        var log = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg exited with {process.ExitCode}: {log[^Math.Min(log.Length, 500)..]}");
        }
    }
    #endregion
}
