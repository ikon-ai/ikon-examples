<!-- mined-from: Ikon.App.Patterns -->
# Screen And Camera Capture — One Button, Three Kinds

`CaptureButton` handles audio, camera and screen: the `kind` changes, the shape does not. What is
worth getting right is the capture **mode**, the encoder **presets**, and the fact that permission
is a four-state enum rather than a yes/no.

| | |
|---|---|
| `MediaCaptureButtonMode.Hold` | push-to-talk — releasing ends it |
| `MediaCaptureButtonMode.Toggle` | a stream with a life of its own: a screen share, a recording |

## When to use

Screen sharing, a camera feed, a recording, push-to-talk. For a single still frame,
`ClientFunctions.CaptureImageAsync` is cheaper — see `camera-capture-to-vision`.

## Notes

- **Permission is `MediaPermissionState`, not a bool**: `Granted`, `Prompt`, `Denied`,
  `Unavailable`. `Denied` is a user choice they can change; `Unavailable` means the device has no
  such capability at all. Telling someone to check their settings when their laptop simply cannot
  share a screen is the failure this distinction prevents.
- `permissionText` and `permissionDeniedText` are what the button says while asking and after a
  refusal — permission is a state the control explains, not an error thrown at the user.
- **The presets are a starting point, not a fallback.** `ClientVideoCaptureOptions.DefaultScreen`
  is 1080p30, `DefaultCamera` is 720p30, both with a key frame every 90 frames (3 s at 30 fps).
  Use `with` to adjust one field rather than building the record from scratch.
- **`KeyFrameIntervalFrames` is the worst-case join latency.** A receiver can only start decoding on
  a key frame, so it is also the resync granularity after packet loss. Lower it for a share people
  join late; leave it high for a recording nobody watches live. Lower means more bandwidth.
- `ClientAudioCaptureOptions.Default` leaves **echo cancellation off** — it is needed for two-way
  calls on a loudspeaker and is lossy when nothing is being played back, which is the common
  server-transcription case.
- **`MediaCaptureEvent.ClientContext` is populated for every capture kind**, so read
  `ClientSessionId`/`UserId` rather than keeping a streamId-to-client map of your own.
- `DeviceId` on the video options is a camera id and is **ignored for screen capture**.

## Snippet

```csharp
private readonly ClientReactive<string?> _streamId = new(null);
private readonly ClientReactive<MediaPermissionState> _permission = new(MediaPermissionState.Prompt);

private void Render(IView view)
{
    view.Row(["gap-2"], content: row =>
    {
        // Toggle is the mode for a stream with a life of its own -- a screen share, a
        // recording. Hold is for push-to-talk, where releasing ends it.
        row.CaptureButton(
            kind: MediaCaptureKind.Screen,
            captureMode: MediaCaptureButtonMode.Toggle,
            text: "Share screen",

            // The presets are the starting point, not a fallback: DefaultScreen is 1080p30 and
            // DefaultCamera 720p30, both with a key frame every 90 frames.
            videoOptions: ClientVideoCaptureOptions.DefaultScreen with
            {
                // A receiver can only start decoding on a key frame, so this is the worst-case
                // join latency for anyone arriving mid-stream. Lower it for a share people
                // join late; leave it for a recording nobody watches live.
                KeyFrameIntervalFrames = 30,
            },

            // Permission is a state, not an error: these strings are what the button says
            // while asking and after a refusal, so the control explains itself.
            permissionText: "Allow screen sharing to continue",
            permissionDeniedText: "Screen sharing is blocked in your browser settings",

            onCaptureStart: async captureEvent =>
            {
                // ClientContext is populated for every capture kind, so use ClientSessionId
                // rather than keeping a streamId-to-client map of your own.
                _streamId.SetFor(captureEvent.ClientSessionId ?? 0, captureEvent.StreamId);
            },

            onCaptureStop: async captureEvent =>
            {
                _streamId.SetFor(captureEvent.ClientSessionId ?? 0, null);
            },

            // Permission is a FOUR-state enum, not a bool: Denied is a user choice they can
            // change, Unavailable means the device has no such capability at all, and the two
            // deserve different words.
            onPermissionChanged: async permission =>
            {
                _permission.Value = permission.State;
            });

        // The camera button differs only in kind and preset -- one component, three kinds.
        row.CaptureButton(
            kind: MediaCaptureKind.Camera,
            captureMode: MediaCaptureButtonMode.Toggle,
            text: "Camera",
            videoOptions: ClientVideoCaptureOptions.DefaultCamera);

        if (_permission.Value is MediaPermissionState.Denied or MediaPermissionState.Unavailable)
        {
            row.Text(["text-destructive text-sm"], text: _permission.Value == MediaPermissionState.Denied
                ? "Permission denied — allow it in your browser settings"
                : "This device cannot share its screen");
        }
    });
}
```

## See also

- `push-to-talk-button` — the Hold-mode audio case.
- `camera-capture-to-vision` — a single frame rather than a stream.
