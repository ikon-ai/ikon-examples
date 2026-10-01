using Ikon.AI.VideoSegmentation;

public partial class Validation
{
    // sample.mp4 is 632x632, 24 fps, about 5 s; the box covers the centre of the first frame
    private const int VideoSegmenterSampleSize = 632;

    private readonly Reactive<string> _videoSegmenterPrompt = new("");
    private readonly Reactive<bool> _videoSegmenterProcessing = new(false);
    private readonly Reactive<string?> _videoSegmenterResult = new(null);
    private readonly Reactive<string?> _videoSegmenterError = new(null);
    private readonly Reactive<string?> _videoSegmenterVideoUrl = new(null);
    private readonly Reactive<string?> _videoSegmenterZipUrl = new(null);

    private void RenderVideoSegmenterCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-video-segmenter-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Video Segmenter");

            view.Box([FormField.Root], content: view =>
            {
                view.Text([FormField.Label], "Text concept (optional, comma-separated)");
                view.TextField(
                    [Input.Default],
                    value: _videoSegmenterPrompt.Value,
                    placeholder: "e.g. person",
                    onValueChange: async v => _videoSegmenterPrompt.Value = v ?? "");
            });

            view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "Segment Sample Video",
                    props: TestId("ai-video-segmenter-sample"),
                    disabled: _videoSegmenterProcessing.Value,
                    onClick: SegmentSampleVideoAsync);

                if (_videoSegmenterProcessing.Value)
                {
                    view.Box([Icon.Spinner]);
                }

                if (!string.IsNullOrEmpty(_videoSegmenterZipUrl.Value))
                {
                    view.Button([Button.PrimaryMd],
                        href: _videoSegmenterZipUrl.Value,
                        target: "_blank",
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "download");
                            v.Text(text: "Bounding-box frames (zip)");
                        });
                }
            });

            if (!string.IsNullOrEmpty(_videoSegmenterError.Value))
            {
                view.Box([Alert.Error, "mt-4"], props: TestId("ai-video-segmenter-error"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _videoSegmenterError.Value);
                });
            }

            if (!string.IsNullOrEmpty(_videoSegmenterResult.Value))
            {
                view.Box([Alert.Success, "mt-4"], props: TestId("ai-video-segmenter-result"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _videoSegmenterResult.Value);
                });
            }

            if (!string.IsNullOrEmpty(_videoSegmenterVideoUrl.Value))
            {
                view.Box([Media.VideoContainer, "mt-4"], content: view =>
                {
                    view.VideoUrlPlayer(
                        ["w-full h-full"],
                        url: _videoSegmenterVideoUrl.Value,
                        controls: true,
                        loop: true,
                        muted: true,
                        playsInline: true);
                });
            }
        });
    }

    private async Task SegmentSampleVideoAsync()
    {
        _videoSegmenterProcessing.Value = true;
        _videoSegmenterError.Value = null;
        _videoSegmenterResult.Value = null;
        _videoSegmenterVideoUrl.Value = null;
        _videoSegmenterZipUrl.Value = null;

        try
        {
            var videoBytes = await File.ReadAllBytesAsync(Path.Combine(app.DataDirectory, "sample.mp4"));
            var uri = new AssetUri(AssetClass.CloudFile,
                $"validation/video-segmenter/{Guid.NewGuid():N}.mp4",
                spaceId: app.GlobalState.SpaceId,
                userId: app.SessionIdentity.UserId);
            await Asset.Instance.SetBytesAsync(uri, videoBytes,
                new AssetMetadata(mimeType: MimeTypes.VideoMp4, expiresAt: DateTime.UtcNow.AddHours(1)));

            var quarter = VideoSegmenterSampleSize / 4;
            using var segmenter = new VideoSegmenter(VideoSegmenterModel.Sam3);

            var config = new VideoSegmenterConfig
            {
                AssetUri = uri,
                Prompt = string.IsNullOrWhiteSpace(_videoSegmenterPrompt.Value) ? null : _videoSegmenterPrompt.Value.Trim(),
                BoxPrompts =
                [
                    new VideoSegmenterConfig.BoxPrompt
                    {
                        FrameIndex = 0,
                        ObjectId = 1,
                        XMin = quarter,
                        YMin = quarter,
                        XMax = VideoSegmenterSampleSize - quarter,
                        YMax = VideoSegmenterSampleSize - quarter
                    }
                ],
                ApplyMask = true,
                DetectionThreshold = 0.5
            };

            var result = await segmenter.SegmentVideoAsync(config);

            if (string.IsNullOrEmpty(result.Url))
            {
                _videoSegmenterError.Value = "FAIL: the segmenter returned no video URL";
                return;
            }

            _videoSegmenterVideoUrl.Value = result.Url;
            _videoSegmenterZipUrl.Value = result.BoundingBoxFramesZipUrl;
            var size = result.SizeBytes is { } bytes ? $"{bytes / 1024} KB" : "size unknown";
            var zip = string.IsNullOrEmpty(result.BoundingBoxFramesZipUrl) ? "no bounding-box zip" : "bounding-box zip delivered";
            _videoSegmenterResult.Value = $"PASS Segmented video: {result.MimeType ?? "unknown type"}, {size}, {zip}";
        }
        catch (Exception ex)
        {
            _videoSegmenterError.Value = $"FAIL: {ex.Message}";
        }
        finally
        {
            _videoSegmenterProcessing.Value = false;
        }
    }
}
