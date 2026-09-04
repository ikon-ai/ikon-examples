<!-- mined-from: Vorg.Commander -->
# Push-To-Talk Button — Hold to Capture, Release to Stop

`view.PushToTalkButton` is the platform's push-to-talk primitive: hold to open the mic, release to
send. It ships the whole state sequence a voice UI needs — needs-permission, ready, pressed, hot —
so the app writes none of it.

## When to use

Walkie-talkie UIs, voice chat in noisy rooms, command-and-control apps where the operator wants
explicit control, and every "hold to talk" composer. Bandwidth-sensitive too: audio only flows
during the press.

Use `view.MicToggleButton` instead when an utterance is long or the user's hands are busy. Never
offer both for the same microphone — two ways to open one mic is the ambiguity users report as
"is it on?".

## Snippet

```csharp
private readonly ClientReactive<bool> _micBlocked = new(false);

private void Render(IView view)
{
    // `group` on the row is what lets the wave key off the button's own capture state.
    view.Row(["group items-center gap-3"], content: row =>
    {
        row.PushToTalkButton(
            text: "🎤",
            onPermissionChanged: async args =>
            {
                _micBlocked.Value = args.State != MediaPermissionState.Granted;
            });

        row.Box([MicButton.WhileCapturing, "items-center"],
            content: box => box.AudioWave());

        if (_micBlocked.Value)
        {
            row.Text([Text.Caption, "text-error-primary"],
                text: "Allow the microphone in your browser's site settings to talk");
        }
    });
}
```

## The permission step is separate — do not merge it into the hold

Until the browser has granted a microphone, the button renders itself as an **"Enable microphone"**
pill and a press *only* asks for the permission. It never starts a capture in the same press. This
is not a nicety; it is the difference between working and not:

- A permission dialog takes focus, which the page sees as the button being **released**. A hold that
  doubles as the ask is cancelled behind the dialog, so the user grants permission and discovers
  they captured nothing.
- On the way back from the dialog there is no cue at all about whether the mic is now live. The
  button answers that itself: it flashes a green **ready** ring for two seconds, then settles.
- The next press is unambiguously a talk press. One press, one meaning.

Refused or missing microphone → the button switches to a **"Microphone blocked"** state that stays
pressable (so it can explain itself) and fires `onPermissionChanged`. Handle that event: offer
typing instead, or say where the browser's site settings are. A mic that silently does nothing is
the bug users report as "the app is broken".

**Never gate a mic button behind `disabled:` for permission reasons.** A disabled button cannot ask,
so the user has no way out of the state. `disabled:` is for "the app is busy", nothing else.

## Notes

- The feedback is client-stamped on `data-ikon-capture-state` (`idle`, `pressed`, `live`, `ready`,
  `prompt`, `requesting`, `denied`, `unavailable`), so every transition lands in the frame of the
  press. Do **not** mirror capture state into a `ClientReactive<bool>` and restyle from it — that is
  a server round trip per press, and it is visibly late.
- `Theming.MicButton.Default` carries all of it. A custom style array replaces the default entirely:
  start it with `"default"` to layer on top, or include `MicButton.States` so the states survive.
- `MicButton.WhileCapturing` reveals an element (an `AudioWave`, a "listening…" caption) only while
  a capture button inside the same `group` is held.
- `holdReleaseDelayMs` defaults to 500ms — speech users release slightly before they finish.
- Server-side, hook `Audio.AudioInputFrameAsync`, or enable `Audio.UseSpeechRecognition(...)` once
  and subscribe to `Audio.SpeechRecognizedAsync` for transcripts. The button just controls when the
  stream is open.
- Same behavior on a Flutter frontend: the Dart renderer runs the identical state machine against
  the OS permission dialog.
- `view.CaptureButton(kind: MediaCaptureKind.Camera)` follows the same shape for a body-cam style
  hold. Screen capture has no persistent permission — the picker is the gesture — so it has no
  enable step.

## See also

- `voice-loop` — the seed pattern; STT + TTS round-trip on top of this button
