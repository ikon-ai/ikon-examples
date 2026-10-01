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

    private static List<SelectOption>? _videoCapableModelOptions;

    // Read from the capability table rather than listed by hand, so a model that gains video input
    // shows up here without an edit; a model the registry cannot resolve is simply left out.
    private static List<SelectOption> GetVideoCapableModelOptions()
    {
        if (_videoCapableModelOptions != null)
        {
            return _videoCapableModelOptions;
        }

        var options = new List<SelectOption>();

        foreach (var model in Enum.GetValues<LLMModel>())
        {
            try
            {
                if (Emerge.GetCapabilities(model).SupportsInputVideo)
                {
                    options.Add(new SelectOption(model.ToString(), model.DisplayName()));
                }
            }
            catch (Exception)
            {
                // An unresolvable model has no capabilities to offer; leaving it out of the picker is the right answer
            }
        }

        if (options.All(o => o.Value != nameof(LLMModel.Gemini25Flash)))
        {
            options.Insert(0, new SelectOption(nameof(LLMModel.Gemini25Flash), LLMModel.Gemini25Flash.DisplayName()));
        }

        _videoCapableModelOptions = options;
        return options;
    }

    private void RenderVideoUnderstandingCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Video Understanding (Asset URI)");

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

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Analyze Uploaded Video",
                        disabled: _videoUnderstandingProcessing.Value || string.IsNullOrEmpty(_videoUnderstandingFilePath),
                        onClick: AnalyzeUploadedVideoAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Analyze Sample Video",
                        props: TestId("ai-video-understanding-sample"),
                        disabled: _videoUnderstandingProcessing.Value,
                        onClick: AnalyzeSampleVideoAsync);

                    if (_videoUnderstandingProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_videoUnderstandingAssetInfo.Value))
                {
                    view.Text([Text.Caption], _videoUnderstandingAssetInfo.Value);
                }

                if (!string.IsNullOrEmpty(_videoUnderstandingError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-video-understanding-error"), content: view =>
                    {
                        view.Text([Alert.Description], _videoUnderstandingError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoUnderstandingResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], props: TestId("ai-video-understanding-result"), content: view =>
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
        byte[] bytes;

        try
        {
            bytes = await File.ReadAllBytesAsync(Path.Combine(app.DataDirectory, "sample.mp4"));
        }
        catch (Exception ex)
        {
            _videoUnderstandingError.Value = $"Could not read sample.mp4: {ex.Message}";
            return;
        }

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
