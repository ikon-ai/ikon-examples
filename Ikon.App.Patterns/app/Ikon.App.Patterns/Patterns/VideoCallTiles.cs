namespace Ikon.App.Patterns.Patterns;

// Pattern: video-call-tiles — see docs/patterns/video-call-tiles.md.
// The example region below is the canonical body the doc extracts.
internal sealed class VideoCallTiles : IPatternDemo
{
    public string Slug => "video-call-tiles";
    public string Title => "Video call tiles";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    private Video Video => throw new NotImplementedException();

    #region example:pattern-video-call-tiles
    // Shared by everyone: every client draws the same tiles, in the order the cameras started
    private readonly ReactiveList<CallTile> _tiles = new();

    private sealed record CallTile(string Surface, string InputId);

    /// <summary>
    /// Wire once at setup, not per client: the input events are app-wide and carry the client.
    /// </summary>
    private void WireCameras()
    {
        Video.InputStartedAsync += async input =>
        {
            if (input.Kind != VideoSourceKind.Camera)
            {
                return;
            }

            // One relay per camera, started once: the platform forwards every frame. Everyone but
            // the owner, who previews locally, and clients who join later included; each viewer
            // starts at a keyframe the camera is asked for.
            var surface = $"tile-{input.Id}";
            Video.Play(MediaTargets.EveryoneExcept(input.ClientSessionId), surface, input);
            _tiles.Add(new CallTile(surface, input.Id));
        };

        // The relay ends SourceEnded and clears its surface on its own, maybe just after; only the tile is left.
        Video.InputEndedAsync += async input =>
        {
            _tiles.RemoveAll(tile => tile.InputId == input.Id);
        };
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.CaptureButton([Button.OutlineMd],
                kind: MediaCaptureKind.Camera,
                captureMode: MediaCaptureButtonMode.Toggle,
                text: "Camera");

            if (_tiles.Count == 0)
            {
                col.Text(["text-sm text-muted-foreground"], text: "Nobody has a camera on yet");
                return;
            }

            col.Grid(["grid-cols-1 sm:grid-cols-2 gap-2"], content: grid =>
            {
                foreach (var tile in _tiles)
                {
                    // The tile's owner sees its own camera locally, without the round trip;
                    // everyone else sees the relay, with a spinner until its first frame.
                    grid.VideoSurface(["aspect-video w-full rounded-lg bg-black"], surface: tile.Surface,
                        fit: VideoFit.Cover,
                        localPreviewStreamId: tile.InputId,
                        placeholder: tileView => tileView.Spinner(),
                        key: tile.Surface);
                }
            });
        });
    }
    #endregion
}
