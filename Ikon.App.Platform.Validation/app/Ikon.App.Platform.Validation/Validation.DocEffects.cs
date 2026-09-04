#region docsnippet:audio-effects-usings
using Ikon.Resonance.Effects;

// Available effects: BitCrusherAudioEffect, ChorusAudioEffect, DelayAudioEffect,
// ReverbAudioEffect, RobotVoiceAudioEffect, SaturationAudioEffect,
// TelephoneAudioEffect, TremoloAudioEffect
#endregion

// The usings above are the example; a `using` is only legal at file scope, so this file holds them
// where the compiler reads them.
file static class DocAudioEffects
{
    public static void Run(AudioChunk chunk)
    {
        #region docsnippet:audio-effects-mixer
        var mixer = new SpeechMixer();
        mixer.AddSamples(chunk, effects: [new ReverbAudioEffect(), new DelayAudioEffect()]);
        #endregion
    }
}
