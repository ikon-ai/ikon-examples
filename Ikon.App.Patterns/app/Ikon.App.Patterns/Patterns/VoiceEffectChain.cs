namespace Ikon.App.Patterns.Patterns;

// Pattern: voice-effect-chain — see docs/patterns/voice-effect-chain.md.
// The example region below is the canonical body the doc extracts.
internal sealed class VoiceEffectChain(IAppBase app) : IPatternDemo
{
    public string Slug => "voice-effect-chain";
    public string Title => "Voice effect chain";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio { get; } = new(app);

    #region example:pattern-voice-effect-chain
    private readonly ClientReactive<string> _character = new("narrator");

    /// <summary>
    /// Effects are values, so a character is just a named list. Order matters: the chain runs in
    /// sequence, and a band-limiting effect before a distortion sounds nothing like the reverse.
    /// </summary>
    private static IReadOnlyList<IAudioEffect> ChainFor(string character) => character switch
    {
        // Band-limit first, then drive: that order is what makes it read as a phone line rather
        // than a distorted voice with the highs still in it.
        "phone" => [new TelephoneAudioEffect(lowCutHz: 300, highCutHz: 3400, mix: 1.0f, drive: 0.3f)],
        "robot" => [new RobotVoiceAudioEffect(carrierFrequencyHz: 110, mix: 0.8f, drive: 0.4f)],
        "hall" => [new ReverbAudioEffect(), new DelayAudioEffect(delayMs: 180, feedback: 0.3f, mix: 0.25f)],
        "retro" => [new BitCrusherAudioEffect(bitDepth: 8, downsampleFactor: 3, mix: 0.7f),
                    new SaturationAudioEffect(drive: 3.0f, mix: 0.6f)],
        _ => [],
    };

    /// <summary>
    /// The chain belongs to the playback, so it applies to the whole utterance -- changing the
    /// character mid-sentence takes effect on the next line, not this one.
    /// </summary>
    private void SpeakLine(string text)
    {
        Audio.Speak(MediaTargets.Everyone, text, options: new PlayOptions { Effects = ChainFor(_character.Value) });
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.RadioGroup(label: "Voice", bind: _character, content: group =>
            {
                foreach (var name in (string[])["narrator", "phone", "robot", "hall", "retro"])
                {
                    // The Label around item and text names the radio and makes the text clickable.
                    group.Label([Label.Default, "flex items-center gap-2 cursor-pointer"], key: name, content: label =>
                    {
                        label.RadioGroupItem(value: name);
                        label.Text(text: name);
                    });
                }
            });

            col.Button(
                onClick: () => SpeakLine("The package has arrived."),
                content: v => v.Text(text: "Speak"));
        });
    }
    #endregion
}
