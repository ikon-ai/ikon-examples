<!-- mined-from: Ikon.App.Patterns -->
# Voice Effect Chain — A Character Is A List Of Effects

`IAudioEffect` implementations are plain values, so a voice character is nothing more than a named
list of them handed to `effects:`. That makes characters data — switchable, testable, and storable
— rather than branches through the speech code.

**Order matters.** The chain runs in sequence: band-limiting before drive reads as a phone line,
the same two reversed read as a distorted voice with the highs still in it.

## When to use

Character voices in a game or story app, a radio or intercom effect, a retro or lo-fi treatment,
room ambience on a narrator. Also the shape for per-speaker treatment in a multi-character scene.

## Notes

- `effects:` is a parameter on `Audio.SpeakAsync`, `Audio.SpeakAndWaitAsync` and
  `Audio.SendSpeech`, alongside `analyzers:`.
- **The chain is captured from the FIRST chunk of a speech event** and applies to that whole
  utterance. Changing the character mid-sentence takes effect on the next one.
- Every effect has a parameterless constructor with sensible defaults plus a full one; reach for
  the full form only when a default is wrong for the character.
- `mix` is the dry/wet blend on every effect that has one — `1.0f` replaces the signal, lower
  values keep some of the original voice under it.
- Available: `ReverbAudioEffect`, `DelayAudioEffect`, `ChorusAudioEffect`, `TremoloAudioEffect`,
  `SaturationAudioEffect`, `BitCrusherAudioEffect`, `TelephoneAudioEffect`,
  `RobotVoiceAudioEffect`.
- Effects process the audio the server streams. They are not a client-side filter, so what the
  user hears is what the app sent.

## Snippet

```csharp
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
                new SaturationAudioEffect(drive: 0.5f, mix: 0.6f)],
    _ => [],
};

/// <summary>
/// The chain is captured from the FIRST chunk of a speech event, so it applies to the whole
/// utterance -- changing the character mid-sentence takes effect on the next one, not this.
/// </summary>
private async Task SpeakAsync(string text)
{
    await Audio.SpeakAsync(text, effects: ChainFor(_character.Value));
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        col.RadioGroup(label: "Voice", bind: _character, content: group =>
        {
            foreach (var name in (string[])["narrator", "phone", "robot", "hall", "retro"])
            {
                group.RadioGroupItem(value: name, content: v => v.Text(text: name));
            }
        });

        col.Button(
            onClick: async () => await SpeakAsync("The package has arrived."),
            content: v => v.Text(text: "Speak"));
    });
}
```

## See also

- `audio-reactive-visuals` — the `analyzers:` half of the same call.
- `voice-loop` — mic capture through STT, LLM and TTS playback.
