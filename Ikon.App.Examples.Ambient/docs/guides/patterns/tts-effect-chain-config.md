<!-- mined-from: Ikon.App.Examples.VRMChat -->
# TTS Effect Chain Config — Add/Remove/Tune Audio Effects Live

A list of `EffectEntry` objects, each holding an effect type ("Reverb", "Chorus", "RobotVoice"), the constructed `IAudioEffect`, and a `Dictionary<string, Reactive<float>>` of its parameters. Users add effects from a button row, edit each parameter via a numeric `TextField`, and removing is a single ✕. On change the effect is rebuilt with the new params; the live list is what gets passed to `Audio.SendSpeech` next.

## When to use

You're shipping a voice/audio app where users want to tweak the timbre of TTS output (or microphone playback) in real time — robot voice, telephone, reverb halls. Treating each effect as a typed entry with reactive params makes the UI free.

## Snippet

```csharp
internal class EffectEntry(string effectType, IAudioEffect effect, Dictionary<string, Reactive<float>> reactiveParams)
{
    public string EffectType { get; } = effectType;
    public IAudioEffect Effect { get; set; } = effect;
    public Dictionary<string, Reactive<float>> Params { get; } = reactiveParams;

    public Dictionary<string, float> GetParamValues()
        => Params.ToDictionary(k => k.Key, v => v.Value.Value);

    public static Dictionary<string, Reactive<float>> ToReactiveParams(Dictionary<string, float> plain)
        => plain.ToDictionary(k => k.Key, v => new Reactive<float>(v.Value));
}

private readonly ReactiveList<EffectEntry> _ttsEffects = new();

private static readonly string[] EffectTypes =
    ["Delay", "Reverb", "Chorus", "Tremolo", "BitCrusher", "Saturation", "RobotVoice", "Telephone"];

private void AddTtsEffect(string effectType)
{
    var defaultParams = GetDefaultParams(effectType);
    _ttsEffects.Add(new EffectEntry(
        effectType,
        CreateEffect(effectType, defaultParams),
        EffectEntry.ToReactiveParams(defaultParams)));
}

private void RemoveTtsEffect(EffectEntry entry) => _ttsEffects.Remove(entry);

private void UpdateTtsEffectParam(int index, string paramKey, float value)
{
    var entry = _ttsEffects[index];
    entry.Params[paramKey].Value = value;
    entry.Effect = CreateEffect(entry.EffectType, entry.GetParamValues());
}

// In the settings panel:
view.Row(style: ["flex flex-wrap gap-1 mt-2"], content: view =>
{
    foreach (var effectType in EffectTypes)
    {
        var type = effectType;
        view.Button(style: ["text-xs px-3 py-1.5 rounded-lg bg-gray-100"],
            text: $"+ {effectType}",
            onClick: async () => AddTtsEffect(type));
    }
});

// When sending speech:
var effects = _ttsEffects.Select(e => e.Effect).ToList();
await foreach (var audio in generator.GenerateSpeechAsync(config))
    Audio.SendSpeech(audio, effects, analyzers);
```

## Notes

- Defaults per effect type live in a `switch` on `effectType` returning a `Dictionary<string, float>` — keeps "what knobs does Reverb have" data-driven.
- Rebuild the `IAudioEffect` on every parameter change (cheap) instead of trying to mutate it — most effects are immutable record-style classes.
- `ReactiveList<EffectEntry>` is the chain: `Add`/`Remove` re-render the panel on their own and serialize concurrent mutations internally, so there is no count trigger and no lock to write.
- Tuning a knob mutates an existing entry in place (`entry.Effect = …`), which the list cannot see — that's fine here because the effect object isn't rendered. Call `_ttsEffects.NotifyUpdate()` if you ever render a field you mutate in place.
- The same chain pattern works for STT input (microphone monitoring): `state.EffectInstances ??= _sttEffects.Select(e => e.Effect.Create(sampleRate, channelCount)).ToList();`

## See also

- `slider-with-live-label`
- `voice-loop`
- `lora-stack-with-weight-sliders`
