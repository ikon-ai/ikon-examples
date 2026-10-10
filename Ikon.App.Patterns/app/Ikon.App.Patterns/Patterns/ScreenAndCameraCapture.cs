namespace Ikon.App.Patterns.Patterns;

// Pattern: screen-and-camera-capture — see docs/patterns/screen-and-camera-capture.md.
// The example region below is the canonical body the doc extracts.
internal sealed class ScreenAndCameraCapture : IPatternDemo
{
    public string Slug => "screen-and-camera-capture";
    public string Title => "Screen and camera capture";
    public string Category => "Device & sensors";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-screen-and-camera-capture
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

                // The kind's preset fills every field left null: DefaultScreen is 1080p30 and
                // DefaultCamera 720p30, both with a key frame every 90 frames. The web client
                // shares a screen at its native size, ignoring Width and Height; Flutter applies them.
                videoOptions: new ClientVideoCaptureOptions
                {
                    // A viewer that joins or loses frames asks the capture for a key frame at
                    // once, so this only sets the steady cadence between requests. Only the web
                    // client's protocol-channel fallback reads it; over WebRTC it changes nothing.
                    KeyFrameIntervalFrames = 30,
                },

                onCaptureStart: async captureEvent =>
                {
                    // ClientContext is populated for every capture kind, so use ClientSessionId
                    // rather than keeping a streamId-to-client map of your own.
                    _streamId.SetFor(captureEvent.ClientSessionId ?? 0, captureEvent.StreamId);
                },

                onCaptureStop: async captureEvent =>
                {
                    _streamId.SetFor(captureEvent.ClientSessionId ?? 0, null);
                });

            // Same component, another kind. Screen capture has no permission step -- the
            // browser's picker asks on every use -- so the permission props belong here.
            row.CaptureButton(
                kind: MediaCaptureKind.Camera,
                captureMode: MediaCaptureButtonMode.Toggle,
                text: "Camera",

                // Permission is a state, not an error: these strings are what the button says
                // while asking and after a refusal, so the control explains itself. The denied
                // text also shows for Unavailable, so it must fit both. On the Flutter client
                // only the audio button shows them, and this callback never fires.
                permissionText: "Allow camera access to continue",
                permissionDeniedText: "Camera not available",

                // Permission is a FOUR-state enum, not a bool: Denied is a user choice they can
                // change, Unavailable means no camera or no capture API (an insecure origin, a
                // non-browser client), and the two deserve different words.
                onPermissionChanged: async permission =>
                {
                    _permission.Value = permission.State;
                });

            if (_permission.Value is MediaPermissionState.Denied or MediaPermissionState.Unavailable)
            {
                row.Text(["text-destructive text-sm"], text: _permission.Value == MediaPermissionState.Denied
                    ? "Permission denied — allow it in your browser settings"
                    : "No camera is available here");
            }
        });
    }
    #endregion
}
