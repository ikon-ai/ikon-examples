<!-- mined-from: Ikon.App.Examples.Live2DChat -->
# Audio-Reactive Visuals — Shape Data That Rides With The Audio

Attaching an `IAudioAnalyzer` to speech is the entire server side of lip-sync, waveform bars and
pulse animations. The analyzer's per-frame values travel **inside the audio frames** — the client
reads `PcmAudioFrame.AnalysisResults` alongside the samples it is already playing — so the visual
cannot drift out of sync with the sound.

The server never observes those values. Driving a visual from server-side `Reactive` state instead
arrives a frame late and floods the channel with UI diffs, which is the mistake this pattern
exists to prevent.

## When to use

A talking avatar, a mouth that moves, a waveform or level meter, a button that pulses while a sound
plays — anything whose motion must match audio the app is playing.

## Notes

- `analyzers:` is a parameter on `Audio.SpeakAsync`, `Audio.SpeakAndWaitAsync` and
  `Audio.SendSpeech`. Effects and analyzers are captured from the first chunk of a speech event.
- Hold ONE analyzer instance for the app. It declares its shape set once
  (`AudioShapeSetDeclaration`: `SetId`, `Name`, `ShapeNames`) and each frame's
  `AudioAnalysisResult` carries the matching `SetId` plus `Values` — the client pairs them by
  `SetId`, and `ShapeNames` says which value is which.
- `VisemeAnalyzer` produces `MouthOpenY` (0–1, from RMS) and `MouthForm` (−1 to +1, from spectral
  analysis).
- Analyzers may reuse their value storage between frames. Copy anything you need beyond the current
  frame — and on the server, `PcmAudioFrame.ToOwned()` when a frame must outlive the loop body.
- The rendering half is a custom client component; see `custom-react-node-embed` for the four
  parts it needs.

## Snippet

```csharp
// One analyzer instance for the app, not one per utterance: it declares the shape set once
// and the client matches frames to that declaration by SetId.
private readonly VisemeAnalyzer _visemes = new();

/// <summary>
/// Attaching an analyzer is the whole server side. The shape values ride ALONG with the audio
/// in each PcmAudioFrame -- AnalysisResults for the per-frame numbers, ShapeSetDeclarations
/// for what those numbers mean -- so the visual stays in sync with playback for free. The
/// server never sees them; a custom client component reads them off the frame.
/// </summary>
private async Task SpeakWithLipSyncAsync(string text)
{
    await Audio.SpeakAsync(text, analyzers: [_visemes]);
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        col.Button(
            onClick: async () => await SpeakWithLipSyncAsync("Hello there."),
            content: v => v.Text(text: "Speak"));

        // The avatar/visualizer is a custom node: it subscribes to the audio frames and reads
        // AnalysisResults itself. Driving it from server-side Reactive state instead would
        // arrive a frame late and flood the channel.
        col.AddNode("app.lipSyncAvatar", props: new Dictionary<string, object>
        {
            ["shapeSet"] = _visemes.ShapeSetDeclaration.Name,
        });
    });
}
```

## See also

- `custom-react-node-embed` — the client component that consumes the shape data.
- `voice-loop` — mic capture through STT, LLM and TTS playback.
