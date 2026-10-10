public partial class Validation
{
    private const string MixerMusicSlot = "mixer-music";
    private const string MixerCueSlot = "mixer-cue";
    private const int MixerCueSampleRate = 48000;

    // Per client: the music plays to the client that started it, not to everyone in the session
    private readonly ClientReactive<string> _mixerMusicResult = new("");
    private readonly ClientReactive<string> _mixerCueResult = new("");
    private readonly ClientReactive<bool> _mixerPaused = new(false);
    private readonly ClientReactive<double> _mixerVolume = new(1.0);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, AudioPlayback> _mixerMusic = new();

    private void RenderAudioMixerSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Mixer");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Play music",
                        onClick: async () => PlayMixerMusic(ReactiveScope.ClientId),
                        props: TestId("audio-mixer-music-run"));

                    view.Button([Button.OutlineMd],
                        text: _mixerPaused.Value ? "Resume" : "Pause",
                        onClick: async () => ToggleMixerPause(ReactiveScope.ClientId));

                    view.Button([Button.OutlineMd],
                        text: "Cue over it",
                        onClick: async () => await PlayMixerCueAsync(ReactiveScope.ClientId),
                        props: TestId("audio-mixer-cue-run"));

                    view.Button([Button.ErrorMd],
                        text: "Stop music",
                        onClick: async () => StopMixerMusic(ReactiveScope.ClientId));
                });

                view.Row([Layout.Row.InlineCenter, "gap-3"], content: view =>
                {
                    view.Text([Text.Caption, "w-24"], $"Music {_mixerVolume.Value:P0}");
                    view.Slider([Slider.Default, "max-w-64"],
                        value: [_mixerVolume.Value],
                        min: 0,
                        max: 1,
                        step: 0.05,
                        ariaLabel: "Music volume",
                        onValueChange: async values =>
                        {
                            _mixerVolume.Value = values[0];
                            Audio.SetSlotVolume(MediaTargets.To(ReactiveScope.ClientId), MixerMusicSlot, (float)values[0], TimeSpan.FromMilliseconds(100));
                        });
                });

                if (_mixerMusicResult.Value.Length > 0)
                {
                    view.Text([Text.Caption], _mixerMusicResult.Value, props: TestId("audio-mixer-music"));
                }

                if (_mixerCueResult.Value.Length > 0)
                {
                    view.Text([Text.Caption], _mixerCueResult.Value, props: TestId("audio-mixer-cue"));
                }
            });
        });
    }

    // A looping WAV file decoded as it plays into a slot, replacing whatever music that client had. Built
    // in memory: the platform refuses to fetch a local run's localhost URL, and seeded public files are
    // served to browsers, not readable by the app
    private void PlayMixerMusic(int clientSessionId)
    {
        var music = Audio.Play(MediaTargets.To(clientSessionId), new MemoryStream(MixerMusicWav()), "audio/wav", new PlayOptions { Slot = MixerMusicSlot, Mode = AudioMixMode.Replace, Loop = true });

        _mixerMusic[clientSessionId] = music;
        ResumeMixerSlot(clientSessionId);
        _mixerMusicResult.Value = "music: decoding the file";
        _ = ReportMixerMusicAsync(clientSessionId, music);
    }

    private async Task ReportMixerMusicAsync(int clientSessionId, AudioPlayback music)
    {
        await music.Started;

        if (!music.IsEnded)
        {
            _mixerMusicResult.SetFor(clientSessionId, "PASS music: the looping file is playing");
        }

        var outcome = await music.Completion;

        // A newer Play music replaced this one and reports for itself
        if (outcome != AudioPlaybackOutcome.Replaced)
        {
            _mixerMusicResult.SetFor(clientSessionId, outcome == AudioPlaybackOutcome.Failed
                ? $"FAIL music: {music.Error?.Message}"
                : $"music: ended {outcome}");
        }
    }

    private void StopMixerMusic(int clientSessionId)
    {
        Audio.Stop(MediaTargets.To(clientSessionId), MixerMusicSlot);
        ResumeMixerSlot(clientSessionId);
    }

    // A pause holds the slot, not the playback, so the next music would start silent under it
    private void ResumeMixerSlot(int clientSessionId)
    {
        if (_mixerPaused.Value)
        {
            Audio.Resume(MediaTargets.To(clientSessionId), MixerMusicSlot);
            _mixerPaused.Value = false;
        }
    }

    private void ToggleMixerPause(int clientSessionId)
    {
        var targets = MediaTargets.To(clientSessionId);

        if (_mixerPaused.Value)
        {
            Audio.Resume(targets, MixerMusicSlot);
        }
        else
        {
            Audio.Pause(targets, MixerMusicSlot);
        }

        _mixerPaused.Value = !_mixerPaused.Value;
    }

    // A short cue that ducks the music while it plays: the music must come through it still playing
    private async Task PlayMixerCueAsync(int clientSessionId)
    {
        if (!_mixerMusic.TryGetValue(clientSessionId, out var music) || music.IsEnded)
        {
            _mixerCueResult.Value = "FAIL cue: start the music first";
            return;
        }

        var cue = Audio.Play(MediaTargets.To(clientSessionId), MixerCueTone(), MixerCueSampleRate,
            options: new PlayOptions { Slot = MixerCueSlot, Duck = [new SlotDuck(MixerMusicSlot, 0.2f)] });

        _mixerCueResult.Value = "cue: playing, the music ducked under it";
        var outcome = await cue.Completion;

        _mixerCueResult.SetFor(clientSessionId, outcome == AudioPlaybackOutcome.Finished && !music.IsEnded
            ? "PASS cue: it finished over the ducked music, which plays on"
            : $"FAIL cue: ended {outcome}, the music {(music.IsEnded ? "ended" : "plays on")}");
    }

    private static byte[] MixerMusicWav()
    {
        const int sampleRate = 22050;
        int count = sampleRate * 2;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + count * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(count * 2);

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            double chord = Math.Sin(2 * Math.PI * 220 * t) + Math.Sin(2 * Math.PI * 261.63 * t) + Math.Sin(2 * Math.PI * 329.63 * t);
            writer.Write((short)(chord / 3 * 0.2 * short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static float[] MixerCueTone()
    {
        var samples = new float[MixerCueSampleRate * 6 / 10];

        for (int i = 0; i < samples.Length; i++)
        {
            float envelope = MathF.Min(1, MathF.Min(i, samples.Length - i) / 2400f);
            samples[i] = 0.3f * envelope * MathF.Sin(2 * MathF.PI * 880 * i / MixerCueSampleRate);
        }

        return samples;
    }
}
