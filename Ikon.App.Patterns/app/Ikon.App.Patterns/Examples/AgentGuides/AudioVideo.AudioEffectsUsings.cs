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
        Audio.SpeakChunk(MediaTargets.Everyone, chunk, effects: [new ReverbAudioEffect(), new DelayAudioEffect()]);
        #endregion
    }
}
