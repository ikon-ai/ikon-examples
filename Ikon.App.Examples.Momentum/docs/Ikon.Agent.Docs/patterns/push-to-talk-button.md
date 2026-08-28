<!-- mined-from: Vorg.Commander -->
# Push-To-Talk Button — Hold to Capture, Release to Stop

A `CaptureButton` in `MediaCaptureButtonMode.Hold` that streams audio only while pressed. Visual state flips between "TALK (Hold)" idle and "TRANSMITTING" active via a `_isPttActive` reactive, with the button's color also flipping. This is fundamentally different from `Toggle` mode (one click on, one click off) — Hold guarantees the user knows the mic is hot.

## When to use

Walkie-talkie UIs, voice chat in noisy environments where you don't trust auto-detection, command-and-control apps where the operator wants explicit control. Bandwidth/server-cost-sensitive: audio only flows during the press.

## Snippet

```csharp
private readonly ClientReactive<bool> _isPttActive = new(false);
private readonly ClientReactive<string> _statusMessage = new("");

private void Render(IView view)
{
    view.CaptureButton(
        [Button.OutlineMd, "px-3 py-1.5 text-xs",
         _isPttActive.Value
            ? "bg-[#330033] border-[#ff00ff] text-[#ff00ff]"
            : "border-[#666666] text-[#888888]"],
        kind: MediaCaptureKind.Audio,
        captureMode: MediaCaptureButtonMode.Hold,
        onCaptureStart: async args =>
        {
            _isPttActive.Value = true;
            _statusMessage.Value = "Transmitting audio";
        },
        onCaptureStop: async args =>
        {
            _isPttActive.Value = false;
            _statusMessage.Value = "Talk released";
        },
        content: v => v.Text(["text-xs"], _isPttActive.Value ? "TRANSMITTING" : "TALK (Hold)"));
}
```

## Notes

- `MediaCaptureButtonMode.Hold` is the key — it ties capture lifetime to the mouse/touch press.
- The button has its own DOM-level press handling; you only get `onCaptureStart` / `onCaptureStop` callbacks.
- Style the active state aggressively (saturated color, glow) — voice-chat UIs need clear "you are live" feedback.
- For server-side handling of the audio frames, hook into `Audio.AudioInputFrameAsync` separately — the button just controls when the stream is open.
- Pair with `CaptureButton(captureMode: MediaCaptureButtonMode.Toggle)` elsewhere for "always-on" use cases like meetings.
- `CaptureButton` accepts `kind: MediaCaptureKind.Camera` too — same Hold pattern works for shoulder-mounted body cams.

## See also

- `voice-loop` — the seed pattern; STT + TTS round-trip on top of capture
