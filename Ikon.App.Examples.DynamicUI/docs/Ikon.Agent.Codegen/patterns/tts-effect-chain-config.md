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

private readonly List<EffectEntry> _ttsEffects = [];
private readonly Reactive<int> _ttsEffectsCount = new(0);

private static readonly string[] EffectTypes =
    ["Delay", "Reverb", "Chorus", "Tremolo", "BitCrusher", "Saturation", "RobotVoice", "Telephone"];

private void AddTtsEffect(string effectType)
{
    var defaultParams = GetDefaultParams(effectType);
    var entry = new EffectEntry(effectType, CreateEffect(effectType, defaultParams), EffectEntry.ToReactiveParams(defaultParams));
    lock (_ttsEffectsLock) { _ttsEffects.Add(entry); _ttsEffectsCount.Value = _ttsEffects.Count; }
}

private void UpdateTtsEffectParam(int index, string paramKey, float value)
{
    lock (_ttsEffectsLock)
    {
        var entry = _ttsEffects[index];
        entry.Params[paramKey].Value = value;
        entry.Effect = CreateEffect(entry.EffectType, entry.GetParamValues());
    }
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
List<IAudioEffect> effects;
lock (_ttsEffectsLock) { effects = _ttsEffects.Select(e => e.Effect).ToList(); }
await foreach (var audio in generator.GenerateSpeechAsync(config))
    Audio.SendSpeech(audio, effects, analyzers);
```

## Notes

- Defaults per effect type live in a `switch` on `effectType` returning a `Dictionary<string, float>` — keeps "what knobs does Reverb have" data-driven.
- Rebuild the `IAudioEffect` on every parameter change (cheap) instead of trying to mutate it — most effects are immutable record-style classes.
- `_ttsEffectsCount` (a `Reactive<int>`) is the trigger for re-rendering the panel; the list itself is plain `List<>` guarded by a lock.
- The same chain pattern works for STT input (microphone monitoring): `state.EffectInstances ??= _sttEffects.Select(e => e.Effect.Create(sampleRate, channelCount)).ToList();`

## See also

- `slider-with-live-label`
- `voice-loop`
- `lora-stack-with-weight-sliders`
