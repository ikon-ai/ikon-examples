#region example:audio-effects-usings
// Available effects: BitCrusherAudioEffect, ChorusAudioEffect, DelayAudioEffect,
// ReverbAudioEffect, RobotVoiceAudioEffect, SaturationAudioEffect,
// TelephoneAudioEffect, TremoloAudioEffect
#endregion

file sealed class DocAudioEffects(IApp<SessionIdentity, ClientParameters> app)
{
    private Audio Audio { get; } = new(app);

    public void Run(AudioChunk chunk)
    {
        #region example:audio-effects-mixer
        // Effects run in order on one playback — Play, PlayLive or Speak; each playback creates its own effect state.
        Audio.Speak(MediaTargets.Everyone, "Is anyone down here?",
            options: new PlayOptions { Effects = [new ReverbAudioEffect(), new DelayAudioEffect()] });
        Audio.Play(MediaTargets.Everyone, chunk.Samples, chunk.SampleRate, chunk.ChannelCount,
            new PlayOptions { Effects = [new ReverbAudioEffect(), new DelayAudioEffect()] });
        #endregion
    }
}
