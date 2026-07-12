public partial class Validation
{
    // Video Understanding (Asset URI) state
    private readonly Reactive<string> _videoUnderstandingModel = new(nameof(LLMModel.Gemini25Flash));
    private readonly Reactive<string> _videoUnderstandingPrompt = new("Describe what happens in this video clip.");
    private readonly Reactive<bool> _videoUnderstandingProcessing = new(false);
    private readonly Reactive<string?> _videoUnderstandingResult = new(null);
    private readonly Reactive<string?> _videoUnderstandingError = new(null);
    private readonly Reactive<string?> _videoUnderstandingAssetInfo = new(null);
    private readonly Reactive<string> _videoUnderstandingFileName = new("");
    private string? _videoUnderstandingFilePath;

    private static List<SelectOption> GetVideoCapableModelOptions()
    {
        return
        [
            new SelectOption(nameof(LLMModel.Gemini25Flash), "Gemini 2.5 Flash"),
            new SelectOption(nameof(LLMModel.Gemini25FlashLite), "Gemini 2.5 Flash Lite"),
            new SelectOption(nameof(LLMModel.Gemini25Pro), "Gemini 2.5 Pro")
        ];
    }

    private void RenderVideoUnderstandingCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Video Understanding (Asset URI)");
            view.Text([Text.Caption, "mb-4"], "Uploads a video into platform asset storage (with a short TTL) and passes its AssetUri to a Gemini model. The library reads the asset back by URI, exercising the proxied caller-asset read path.");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _videoUnderstandingModel.Value,
                        options: GetVideoCapableModelOptions(),
                        onValueChange: async v => _videoUnderstandingModel.Value = v ?? _videoUnderstandingModel.Value);
                });

                view.TextField(
                    style: [Input.Default],
                    value: _videoUnderstandingPrompt.Value,
                    label: "Prompt",
                    placeholder: "Ask something about the video...",
                    onValueChange: async v => _videoUnderstandingPrompt.Value = v ?? "");

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["video/*"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _videoUnderstandingFileName.Value = args.FileName;
                        _videoUnderstandingFilePath = args.LocalTempFilePath;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "upload");
                            view.Text([Text.Body], string.IsNullOrEmpty(_videoUnderstandingFileName.Value) ? "Upload a video" : _videoUnderstandingFileName.Value);
                            view.Text([Text.Caption], "Video files (e.g. mp4)");
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Analyze Uploaded Video",
                        disabled: _videoUnderstandingProcessing.Value || string.IsNullOrEmpty(_videoUnderstandingFilePath),
                        onClick: AnalyzeUploadedVideoAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Analyze Sample Video",
                        disabled: _videoUnderstandingProcessing.Value,
                        onClick: AnalyzeSampleVideoAsync);

                    if (_videoUnderstandingProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_videoUnderstandingAssetInfo.Value))
                {
                    view.Text([Text.Caption, "font-mono"], _videoUnderstandingAssetInfo.Value);
                }

                if (!string.IsNullOrEmpty(_videoUnderstandingError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _videoUnderstandingError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoUnderstandingResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Model Response");
                        view.Text([Text.Body, "whitespace-pre-wrap"], _videoUnderstandingResult.Value);
                    });
                }
            });
        });
    }

    private async Task AnalyzeUploadedVideoAsync()
    {
        if (string.IsNullOrEmpty(_videoUnderstandingFilePath) || !File.Exists(_videoUnderstandingFilePath))
        {
            _videoUnderstandingError.Value = "File not found";
            return;
        }

        var bytes = await File.ReadAllBytesAsync(_videoUnderstandingFilePath);
        await RunVideoUnderstandingAsync(bytes);
    }

    private async Task AnalyzeSampleVideoAsync()
    {
        var samplePath = Path.Combine(app.DataDirectory, "sample.mp4");
        var bytes = await File.ReadAllBytesAsync(samplePath);
        await RunVideoUnderstandingAsync(bytes);
    }

    private async Task RunVideoUnderstandingAsync(byte[] videoBytes)
    {
        _videoUnderstandingProcessing.Value = true;
        _videoUnderstandingError.Value = null;
        _videoUnderstandingResult.Value = null;
        _videoUnderstandingAssetInfo.Value = null;

        try
        {
            var expiresAt = DateTime.UtcNow.AddHours(1);
            var uri = new AssetUri(AssetClass.CloudFile,
                $"validation/video-understanding/{Guid.NewGuid():N}.mp4",
                spaceId: app.GlobalState.SpaceId,
                userId: app.SessionIdentity.UserId);
            await Asset.Instance.SetBytesAsync(uri, videoBytes,
                new AssetMetadata(mimeType: MimeTypes.VideoMp4, expiresAt: expiresAt));
            _videoUnderstandingAssetInfo.Value = $"Uploaded {uri} (expires {expiresAt:u})";

            var model = Enum.Parse<LLMModel>(_videoUnderstandingModel.Value);
            var ctx = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, new IMessagePart[]
            {
                new TextPart(_videoUnderstandingPrompt.Value),
                new VideoAssetPart(uri, MimeTypes.VideoMp4)
            }));

            var (reply, _) = await Emerge.Run<VideoUnderstandingReply>(model, ctx, pass =>
            {
                pass.SystemPrompt = "You are a video understanding assistant. Answer concisely, based only on the video.";
                pass.MaxOutputTokens = 500;
            }).FinalAsync();

            if (reply is null)
            {
                _videoUnderstandingError.Value = "Generation failed";
                return;
            }

            _videoUnderstandingResult.Value = reply.Description;
        }
        catch (Exception ex)
        {
            _videoUnderstandingError.Value = ex.Message;
        }
        finally
        {
            _videoUnderstandingProcessing.Value = false;
        }
    }
}

internal sealed class VideoUnderstandingReply
{
    public string Description { get; set; } = "";
}
