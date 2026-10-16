namespace Ikon.App.Patterns.Patterns;

// Pattern: on-device-ml-custom-node — see docs/patterns/on-device-ml-custom-node.md.
// The demo shows a note instead, because the pose-tracker node needs a React component that this
// gallery's frontend does not register. Its component is a TypeScript example in platform-typescript/sdk/sdk-doc-examples.
internal sealed class OnDeviceMlCustomNode(IAppBase app) : IPatternDemo
{
    public string Slug => "on-device-ml-custom-node";
    public string Title => "On-device ML in a custom node";
    public string Category => "Custom frontend";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "A pose model runs in the browser inside a custom node: its files are served from public/, each finished rep comes back as an action, and the set's recorded clip is uploaded with uploadFile to an app.Uploads handler. See docs/patterns/on-device-ml-custom-node.md.");

    #region example:pattern-on-device-ml-custom-node
    public sealed record RepDone(int Rep, int DepthDegrees);

    private const string ClipUploadId = "form-check.clip";

    private readonly ClientReactiveList<RepDone> _reps = new();
    private readonly ClientReactive<bool> _recording = new(false);
    private readonly ClientReactive<string?> _clipUrl = new(null);

    // Call once from Main. The clip is written straight into the private file tree without passing
    // through the app's memory. The hooks run in the uploading client's scope, so _clipUrl is set for that client.
    private void RegisterClipUpload()
    {
        app.Uploads.Register(ClipUploadId,
            onStart: args => Task.FromResult<FileUploadResult>(
                Guid.TryParse(args.UploadId, out var id) && args.MimeType.StartsWith("video/", StringComparison.Ordinal) && args.Size <= 50_000_000
                    ? new FileUploadResult { AssetUri = app.Files.Data.GetAssetUri($"clips/{id:N}.webm") }
                    : false),
            onComplete: async args => _clipUrl.Value = await app.Files.Data.GetUrlAsync($"clips/{Guid.Parse(args.UploadId):N}.webm"));
    }

    private void Render(UIView view)
    {
        view.Column(["gap-3 w-full max-w-xl"], content: col =>
        {
            col.Box(["w-full aspect-video rounded-xl overflow-hidden bg-black"], content: box =>
                box.AddNode("pose-tracker", props: new Dictionary<string, object?>
                {
                    ["recording"] = _recording.Value,
                    ["uploadActionId"] = ClipUploadId,
                    ["onRepId"] = box.CreateAction<RepDone>(args =>
                    {
                        _reps.Add(args.Value);
                        return Task.CompletedTask;
                    }),
                }, style: ["w-full h-full"]));

            col.Button([_recording.Value ? Button.OutlineMd : Button.PrimaryMd, "self-start"],
                text: _recording.Value ? "End the set and save the clip" : "Start a set",
                onClick: async () =>
                {
                    if (!_recording.Value)
                    {
                        _reps.Clear();
                        _clipUrl.Value = null;
                    }

                    _recording.Value = !_recording.Value;
                });

            col.Text([Text.Body], _reps.Count == 0 ? "No reps yet" : $"{_reps.Count} reps, deepest knee angle {_reps.Min(rep => rep.DepthDegrees)}°");

            if (_clipUrl.Value is { } url)
            {
                col.VideoUrlPlayer(["w-full rounded-xl"], url: url, controls: true, key: url);
            }
        });
    }
    #endregion
}
