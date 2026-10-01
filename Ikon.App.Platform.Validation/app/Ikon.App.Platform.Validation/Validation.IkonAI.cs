public partial class Validation
{
    // Access gate for the password-protected sections (Ikon.AI, Payments, Database, …). One unlock
    // covers them all for the person who entered the password, and only them: shared across the
    // instance, one unlock opened every gated tab to every later visitor, and what one visitor typed
    // showed in everyone's password field.
    private readonly UserReactive<string> _sectionPassword = new("");
    private readonly UserReactive<bool> _sectionUnlocked = new(false);
    private readonly UserReactive<bool> _sectionPasswordError = new(false);

    // Chat model/region selection
    private readonly Reactive<string> _chatModel = new(nameof(LLMModel.Claude45Sonnet));
    private readonly Reactive<string> _chatRegion = new(nameof(ModelRegion.Global));

    // Classifier state
    private readonly Reactive<string> _classifierModel = new(nameof(ClassificationModel.OpenAIOmniModeration));
    private readonly Reactive<string> _classifierInput = new("Hello world!");
    private readonly Reactive<bool> _classifierProcessing = new(false);
    private readonly Reactive<string?> _classifierResult = new(null);
    private readonly Reactive<string?> _classifierError = new(null);

    // EmbeddingGenerator state
    private readonly Reactive<string> _embeddingModel = new(nameof(EmbeddingModel.OpenAI3Small));
    private readonly Reactive<string> _embeddingInput = new("Example sentence for embedding");
    private readonly Reactive<string> _embeddingType = new(nameof(EmbeddingType.Document));
    private readonly Reactive<bool> _embeddingProcessing = new(false);
    private readonly Reactive<string?> _embeddingResult = new(null);
    private readonly Reactive<string?> _embeddingError = new(null);
    private readonly Reactive<bool> _embeddingAllowTruncation = new(false);

    // WebSearcher state
    private readonly Reactive<string> _webSearcherModel = new(nameof(WebSearcherModel.Google));
    private readonly Reactive<string> _webSearcherQuery = new("Finnish ice hockey teams");
    private readonly Reactive<int> _webSearcherMaxResults = new(5);
    private readonly Reactive<bool> _webSearcherProcessing = new(false);
    private readonly Reactive<string?> _webSearcherResult = new(null);
    private readonly Reactive<string?> _webSearcherError = new(null);

    // WebScraper state
    private readonly Reactive<string> _webScraperModel = new(nameof(WebScraperModel.Jina));
    private readonly Reactive<string> _webScraperUrl = new("https://example.com");
    private readonly Reactive<string> _webScraperOutputFormat = new(nameof(WebScraperOutputFormat.Markdown));
    private readonly Reactive<bool> _webScraperProcessing = new(false);
    private readonly Reactive<string?> _webScraperResult = new(null);
    private readonly Reactive<string?> _webScraperError = new(null);

    // Reranker state
    private readonly Reactive<string> _rerankerModel = new(nameof(RerankModel.CohereRerank4Fast));
    private readonly Reactive<string> _rerankerQuery = new("What is the latest in artificial intelligence?");
    private readonly Reactive<string> _rerankerDocuments = new("Document about AI\nDocument about cooking\nDocument about space exploration");
    private readonly Reactive<bool> _rerankerProcessing = new(false);
    private readonly Reactive<string?> _rerankerResult = new(null);
    private readonly Reactive<string?> _rerankerError = new(null);
    private readonly Reactive<bool> _rerankerAllowTruncation = new(false);

    // ImageGenerator state
    private readonly Reactive<string> _imageGeneratorModel = new(nameof(ImageGeneratorModel.Gemini25FlashImage));
    private readonly Reactive<string> _imageGeneratorPrompt = new("A serene mountain landscape at sunset");
    private readonly Reactive<string> _imageGeneratorNegativePrompt = new("");
    private readonly Reactive<int> _imageGeneratorWidth = new(1024);
    private readonly Reactive<int> _imageGeneratorHeight = new(1024);
    private readonly Reactive<int> _imageGeneratorSeed = new(0);
    private readonly Reactive<bool> _imageGeneratorProcessing = new(false);
    private readonly Reactive<string?> _imageGeneratorResult = new(null);
    private readonly Reactive<string?> _imageGeneratorError = new(null);
    private readonly Reactive<string?> _imageGeneratorDownloadUrl = new(null);
    private readonly Reactive<string?> _imageGeneratorProvenance = new(null);
    private readonly Reactive<int> _imageGeneratorSteps = new(0);
    private readonly Reactive<string> _imageGeneratorQuality = new(nameof(ImageQuality.Auto));
    private readonly Reactive<string> _imageGeneratorBackground = new(nameof(ImageBackground.Auto));
    private readonly Reactive<bool> _imageGeneratorUpsamplePrompt = new(false);
    private readonly Reactive<int> _imageGeneratorCount = new(1);
    private byte[]? _imageGeneratorInputImageData;
    private string? _imageGeneratorInputImageMimeType;
    private readonly Reactive<string> _imageGeneratorInputImageName = new("");
    private readonly ReactiveList<string> _imageGeneratorResultDataUrls = new();

    // SpeechGenerator state
    private readonly Reactive<string> _speechGeneratorModel = new(nameof(SpeechGeneratorModel.Gpt4OmniMiniTts));
    private readonly Reactive<string> _speechGeneratorText = new("Hello, this is a test of the speech generation system.");
    private readonly Reactive<string> _speechGeneratorVoiceId = new("alloy");
    private readonly Reactive<IReadOnlyList<string>> _speechGeneratorVoiceIds = new(["alloy", "ash", "ballad", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse"]);
    // Empty because the card opens on an OpenAI model, and those refuse a language outright rather
    // than dropping it — the language belongs in Instructions there. ElevenLabs is the model to
    // select when the field itself is what is being exercised.
    private readonly Reactive<string> _speechGeneratorLanguage = new("");
    private readonly Reactive<string> _speechGeneratorInstructions = new("");
    private readonly Reactive<bool> _speechGeneratorProcessing = new(false);
    private readonly Reactive<string?> _speechGeneratorResult = new(null);
    private readonly Reactive<string?> _speechGeneratorError = new(null);
    private SpeechGenerator? _speechGenerator;
    private readonly Reactive<string?> _speechGeneratorDownloadUrl = new(null);

    // SpeechRecognizer state
    private readonly Reactive<string> _speechRecognizerModel = new(nameof(SpeechRecognizerModel.Whisper2));
    private readonly Reactive<string> _speechRecognizerLanguage = new("en-US");
    private readonly Reactive<bool> _speechRecognizerRecording = new(false);
    private readonly Reactive<bool> _speechRecognizerProcessing = new(false);
    private readonly Reactive<string?> _speechRecognizerResult = new(null);
    private readonly Reactive<string?> _speechRecognizerError = new(null);
    private SpeechRecognizer? _speechRecognizerInstance;
    private readonly Reactive<bool> _speechRecognizerContinuous = new(false);
    private Channel<float[]>? _speechRecognizerChannel;
    private CancellationTokenSource? _speechRecognizerCts;
    private readonly Reactive<string?> _speechRecognizerDetail = new(null);
    private readonly Reactive<bool> _speechRecognizerDiarize = new(false);
    private readonly Reactive<int> _speechRecognizerEndOfTurnSilenceMs = new(0);
    private readonly Reactive<double> _speechRecognizerEndOfTurnConfidence = new(0);
    private readonly Reactive<string?> _speechRecognizerEvents = new(null);

    private sealed class SpeechRecognizerBuffer
    {
        public List<float> Samples { get; } = [];
        public int SampleRate;
        public int ChannelCount;
    }

    private readonly Dictionary<string, SpeechRecognizerBuffer> _speechRecognizerBuffers = new();
    private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _speechRecognizerStreamInfo = new();

    // OCR state
    private readonly Reactive<string> _ocrModel = new(nameof(OCRModel.AzureDocumentIntelligence));
    private readonly Reactive<bool> _ocrProcessing = new(false);
    private readonly Reactive<string?> _ocrResult = new(null);
    private readonly Reactive<string?> _ocrError = new(null);
    private readonly Reactive<string> _ocrFileName = new("");
    private string? _ocrFilePath;
    private readonly Reactive<string?> _ocrDownloadUrl = new(null);
    private string? _ocrFullResult;

    // FileConverter state
    private readonly Reactive<string> _fileConverterModel = new(nameof(FileConverterModel.ConvertApi));
    private readonly Reactive<bool> _fileConverterProcessing = new(false);
    private readonly Reactive<string?> _fileConverterResult = new(null);
    private readonly Reactive<string?> _fileConverterError = new(null);
    private readonly Reactive<string> _fileConverterFileName = new("");
    private string? _fileConverterFilePath;
    private readonly Reactive<string?> _fileConverterDownloadUrl = new(null);

    // VideoGenerator state
    private readonly Reactive<string> _videoGeneratorModel = new(nameof(VideoGeneratorModel.Pollo20));
    private readonly Reactive<string> _videoGeneratorPrompt = new("A cat playing with a ball of yarn");
    private readonly Reactive<int> _videoGeneratorLength = new(5);
    private readonly Reactive<string> _videoGeneratorResolution = new(nameof(VideoGeneratorResolution.Resolution480p));
    private readonly Reactive<string> _videoGeneratorAspectRatio = new(nameof(VideoGeneratorAspectRatio.Ratio16x9));
    private readonly Reactive<bool> _videoGeneratorProcessing = new(false);
    private readonly Reactive<string?> _videoGeneratorResultUrl = new(null);
    private readonly Reactive<string?> _videoGeneratorError = new(null);
    private readonly Reactive<string> _videoGeneratorNegativePrompt = new("");
    private readonly Reactive<int> _videoGeneratorSeed = new(0);
    private readonly Reactive<bool> _videoGeneratorGenerateAudio = new(false);
    private byte[]? _videoGeneratorInputImageData;
    private string? _videoGeneratorInputImageMimeType;
    private readonly Reactive<string> _videoGeneratorInputImageName = new("");

    // SoundEffectGenerator state
    private readonly Reactive<string> _soundEffectModel = new(nameof(SoundEffectGeneratorModel.ElevenLabsV2));
    private readonly Reactive<string> _soundEffectPrompt = new("Thunder rumbling in the distance");
    private readonly Reactive<double> _soundEffectDuration = new(5.0);
    private readonly Reactive<bool> _soundEffectProcessing = new(false);
    private readonly Reactive<string?> _soundEffectResult = new(null);
    private readonly Reactive<string?> _soundEffectError = new(null);
    private readonly Reactive<string?> _soundEffectDownloadUrl = new(null);
    private readonly Reactive<double> _soundEffectPromptInfluence = new(0.3);
    private readonly Reactive<bool> _soundEffectLoop = new(false);
    private readonly Reactive<bool> _soundEffectBuffered = new(false);

    // MusicGenerator state
    private readonly Reactive<string> _musicModel = new(nameof(MusicGeneratorModel.ElevenLabsMusicV2));
    private readonly Reactive<string> _musicPrompt = new("An upbeat orchestral victory fanfare, bright and triumphant");
    private readonly Reactive<double> _musicDuration = new(10.0);
    private readonly Reactive<double> _musicStrength = new(0.9);
    private readonly Reactive<bool> _musicProcessing = new(false);
    private readonly Reactive<string?> _musicResult = new(null);
    private readonly Reactive<string?> _musicError = new(null);
    private readonly Reactive<string?> _musicDownloadUrl = new(null);
    private byte[]? _musicInputAudioData;
    private string? _musicInputAudioMimeType;
    private readonly Reactive<string> _musicInputAudioName = new("");

    // VideoEnhancer state
    private readonly Reactive<string> _videoEnhancerModel = new("TensorPixFpsBoost");
    private readonly Reactive<string> _videoEnhancerVideoUrl = new("");
    private readonly Reactive<bool> _videoEnhancerProcessing = new(false);
    private readonly Reactive<string?> _videoEnhancerResultUrl = new(null);
    private readonly Reactive<string?> _videoEnhancerError = new(null);
    private readonly Reactive<int> _videoEnhancerTargetFps = new(0);
    private readonly Reactive<int> _videoEnhancerStartFrame = new(0);
    private readonly Reactive<int> _videoEnhancerEndFrame = new(0);
    private readonly Reactive<string?> _videoEnhancerResultInfo = new(null);

    // ImageSegmenter state
    private readonly Reactive<string> _imageSegmenterModel = new(nameof(ImageSegmenterModel.Sam31));
    private readonly Reactive<string> _imageSegmenterPrompt = new("person");
    private readonly Reactive<bool> _imageSegmenterProcessing = new(false);
    private readonly Reactive<string?> _imageSegmenterResult = new(null);
    private readonly Reactive<string?> _imageSegmenterError = new(null);
    private readonly Reactive<string> _imageSegmenterFileName = new("");
    private string? _imageSegmenterFilePath;
    private string? _imageSegmenterFileMimeType;
    private readonly ReactiveList<string> _imageSegmenterImageDataUrls = new();

    // ImageUpscaler state
    private readonly Reactive<string> _imageUpscalerModel = new(nameof(ImageUpscalerModel.SeedVr2));
    private readonly Reactive<int> _imageUpscalerScaleFactor = new(2);
    private readonly Reactive<double> _imageUpscalerCreativity = new(0);
    private readonly Reactive<bool> _imageUpscalerProcessing = new(false);
    private readonly Reactive<string?> _imageUpscalerResult = new(null);
    private readonly Reactive<string?> _imageUpscalerError = new(null);
    private readonly Reactive<string> _imageUpscalerFileName = new("");
    private string? _imageUpscalerFilePath;
    private string? _imageUpscalerFileMimeType;
    private readonly Reactive<string?> _imageUpscalerImageDataUrl = new(null);

    // DepthEstimator state
    private readonly Reactive<string> _depthEstimatorModel = new(nameof(DepthEstimatorModel.DepthAnythingV2));
    private readonly Reactive<bool> _depthEstimatorProcessing = new(false);
    private readonly Reactive<string?> _depthEstimatorResult = new(null);
    private readonly Reactive<string?> _depthEstimatorError = new(null);
    private readonly Reactive<string> _depthEstimatorFileName = new("");
    private string? _depthEstimatorFilePath;
    private string? _depthEstimatorFileMimeType;
    private readonly Reactive<string?> _depthEstimatorImageDataUrl = new(null);

    // MeshGenerator state
    private readonly Reactive<string> _meshGeneratorModel = new(nameof(MeshGeneratorModel.Meshy6));
    private readonly Reactive<string> _meshGeneratorPrompt = new("A low-poly treasure chest");
    private readonly Reactive<bool> _meshGeneratorTexture = new(true);
    private readonly Reactive<bool> _meshGeneratorProcessing = new(false);
    private readonly Reactive<string?> _meshGeneratorResult = new(null);
    private readonly Reactive<string?> _meshGeneratorError = new(null);
    private byte[]? _meshGeneratorInputImageData;
    private string? _meshGeneratorInputImageMimeType;
    private readonly Reactive<string> _meshGeneratorInputImageName = new("");
    private MeshGeneratorResult? _meshGeneratorResultData;

    private static List<SelectOption> GetModelOptions<T>() where T : struct, Enum
        => Enum.GetValues<T>().Select(v => new SelectOption(v.ToString(), v.ToString())).ToList();

    private static Dictionary<string, object> TestId(string id) => new() { ["data-testid"] = id };

    // A Checkbox, Switch or Slider names itself from nothing; a caption rendered beside it in a
    // Row associates nothing, so the name goes on the control
    private static Dictionary<string, object> AriaLabel(string label, string? testId = null)
    {
        var props = new Dictionary<string, object> { ["aria-label"] = label };

        if (testId != null)
        {
            props["data-testid"] = testId;
        }

        return props;
    }

    private static List<SelectOption> GetSpeechRecognizerModelOptions(bool continuousMode)
    {
        return Enum.GetValues<SpeechRecognizerModel>()
            .Where(m => continuousMode || SpeechRecognizer.GetCapabilities(m).SupportsBatchRecognition)
            .Select(v => new SelectOption(v.ToString(), v.ToString()))
            .ToList();
    }

    private async Task<string?> UploadForDownloadAsync(string filename, byte[] data, string mimeType)
    {
        var uri = new AssetUri(AssetClass.CloudFilePublic, $"validation/{filename}",
            spaceId: app.GlobalState.SpaceId,
            userId: app.SessionIdentity.UserId);
        await Asset.Instance.SetBytesAsync(uri, data, new AssetMetadata(mimeType: mimeType));
        var metadata = await Asset.Instance.GetMetadataAsync(uri);
        return metadata.Url;
    }

    // The password is compiled in from IKON_VALIDATION_APP_PASSWORD, which CI sets and a laptop
    // usually does not. A cloud build without it stays locked: opening the gate there turned a
    // hand-made deploy into a public one, which is how production came to show Payments to anyone.
    private static bool PasswordConfigured => !string.IsNullOrEmpty(BuildConstants.ValidationAppPassword);

    private bool SectionsUnlocked() =>
        app.GlobalState.ServerRunType == ServerRunType.Local || (PasswordConfigured && _sectionUnlocked.Value);

    // Gates a section behind the validation app password. Returns true when the lock screen was
    // rendered — the caller should then return without drawing the real content. A single unlock
    // covers every gated section. Local runs are never gated.
    private bool RenderSectionLocked(UIView view, string title)
    {
        if (SectionsUnlocked())
        {
            return false;
        }

        if (!PasswordConfigured)
        {
            view.Box([Card.Default, "p-6 mb-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], title);
                view.Text([Text.Body], "Locked: this build has no password configured.");
            });

            return true;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], title);
                view.Text([Text.Caption, "mb-4"], $"Enter password to access the {title} section.");

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Password");
                    view.TextField(
                        [Input.Default],
                        value: _sectionPassword.Value,
                        type: "password",
                        onValueChange: async v =>
                        {
                            _sectionPassword.Value = v ?? "";
                            _sectionPasswordError.Value = false;
                        },
                        onSubmit: async submitted =>
                        {
                            if (submitted == BuildConstants.ValidationAppPassword)
                            {
                                _sectionUnlocked.Value = true;
                                _sectionPasswordError.Value = false;
                            }
                            else
                            {
                                _sectionPasswordError.Value = true;
                            }
                        });
                });

                view.Button(
                    [Button.PrimaryMd, "mt-4"],
                    text: "Unlock",
                    disabled: string.IsNullOrEmpty(_sectionPassword.Value),
                    onClick: async () =>
                    {
                        if (_sectionPassword.Value == BuildConstants.ValidationAppPassword)
                        {
                            _sectionUnlocked.Value = true;
                            _sectionPasswordError.Value = false;
                        }
                        else
                        {
                            _sectionPasswordError.Value = true;
                        }
                    });

                if (_sectionPasswordError.Value)
                {
                    view.Box([Alert.Error, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], "Incorrect password");
                    });
                }
            });
        });

        return true;
    }

    private void RenderIkonAISection(UIView view)
    {
        if (RenderSectionLocked(view, "Ikon.AI Library"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Ikon.AI Library");

            RenderChatCard(view);
            RenderClassifierCard(view);
            RenderDeciderCard(view);
            RenderDepthEstimatorCard(view);
            RenderEmbeddingGeneratorCard(view);
            RenderFileConverterCard(view);
            RenderImageGeneratorCard(view);
            RenderImageSegmenterCard(view);
            RenderImageUpscalerCard(view);
            RenderMeshGeneratorCard(view);
            RenderMeshAnimationLibraryCard(view);
            RenderMusicGeneratorCard(view);
            RenderOCRCard(view);
            RenderRerankerCard(view);
            RenderSoundEffectGeneratorCard(view);
            RenderSpeechGeneratorCard(view);
            RenderSpeechRecognizerCard(view);
            RenderVideoEnhancerCard(view);
            RenderVideoGeneratorCard(view);
            RenderVideoSegmenterCard(view);
            RenderVideoUnderstandingCard(view);
            RenderWebScraperCard(view);
            RenderWebSearcherCard(view);
        });
    }

    private void RenderClassifierCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Classifier");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _classifierModel.Value,
                        options: GetModelOptions<ClassificationModel>(),
                        onValueChange: async v => _classifierModel.Value = v ?? _classifierModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Text to Classify");
                    view.TextArea(
                        [Textarea.Default],
                        value: _classifierInput.Value,
                        onValueChange: async v => _classifierInput.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Classify",
                        props: TestId("ai-classifier-run"),
                        disabled: _classifierProcessing.Value || string.IsNullOrWhiteSpace(_classifierInput.Value),
                        onClick: ClassifyTextAsync);

                    if (_classifierProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_classifierError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-classifier-error"), content: view =>
                    {
                        view.Text([Alert.Description], _classifierError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_classifierResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-classifier-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _classifierResult.Value);
                    });
                }
            });
        });
    }

    private async Task ClassifyTextAsync()
    {
        _classifierProcessing.Value = true;
        _classifierError.Value = null;
        _classifierResult.Value = null;

        try
        {
            var model = Enum.Parse<ClassificationModel>(_classifierModel.Value);
            using var classifier = new Classifier(model);

            var result = await classifier.ClassifyAsync(_classifierInput.Value);

            var output = $"Flagged: {result.IsFlagged}";

            if (result.Details.Count > 0)
            {
                output += "\n\nDetails:";

                foreach (var detail in result.Details.Where(d => d.IsFlagged || d.Score > 0.01))
                {
                    output += $"\n  {detail.Label}: {detail.Score:F4} (Flagged: {detail.IsFlagged})";
                }
            }

            _classifierResult.Value = output;
        }
        catch (Exception ex)
        {
            _classifierError.Value = ex.Message;
        }
        finally
        {
            _classifierProcessing.Value = false;
        }
    }

    private void RenderEmbeddingGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Embedding Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _embeddingModel.Value,
                        options: GetModelOptions<EmbeddingModel>(),
                        onValueChange: async v => _embeddingModel.Value = v ?? _embeddingModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Embedding Type");
                    view.Select(
                        value: _embeddingType.Value,
                        options: GetModelOptions<EmbeddingType>(),
                        onValueChange: async v => _embeddingType.Value = v ?? _embeddingType.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Text");
                    view.TextArea(
                        [Textarea.Default],
                        value: _embeddingInput.Value,
                        onValueChange: async v => _embeddingInput.Value = v ?? "");
                });

                view.Checkbox([Checkbox.Default],
                    label: "Allow truncation (Cohere, Voyage, Jina, Vertex cut an over-long input instead of refusing it)",
                    value: _embeddingAllowTruncation.Value,
                    onValueChange: async v => _embeddingAllowTruncation.Value = v);

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Embeddings",
                        props: TestId("ai-embedding-run"),
                        disabled: _embeddingProcessing.Value || string.IsNullOrWhiteSpace(_embeddingInput.Value),
                        onClick: GenerateEmbeddingsAsync);

                    if (_embeddingProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_embeddingError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-embedding-error"), content: view =>
                    {
                        view.Text([Alert.Description], _embeddingError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_embeddingResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-embedding-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _embeddingResult.Value);
                    });
                }
            });
        });
    }

    private async Task GenerateEmbeddingsAsync()
    {
        _embeddingProcessing.Value = true;
        _embeddingError.Value = null;
        _embeddingResult.Value = null;

        try
        {
            var model = Enum.Parse<EmbeddingModel>(_embeddingModel.Value);
            var type = Enum.Parse<EmbeddingType>(_embeddingType.Value);
            using var generator = new EmbeddingGenerator(model);

            var embeddings = await generator.GenerateEmbeddingsAsync(new EmbeddingGeneratorConfig
            {
                Inputs = [_embeddingInput.Value],
                Type = type,
                AllowTruncation = _embeddingAllowTruncation.Value
            });

            if (embeddings.Count > 0)
            {
                var embedding = embeddings[0];
                var preview = string.Join(", ", embedding.Take(10).Select(v => v.ToString("F4")));
                var declared = EmbeddingGenerator.GetCapabilities(model).EmbeddingVectorSize;
                _embeddingResult.Value = $"Vector dimension: {embedding.Length} (model declares {declared}, max {generator.MaxInputCount} inputs per request)\nFirst 10 values: [{preview}, ...]";
            }
            else
            {
                _embeddingError.Value = "The model returned no embedding";
            }
        }
        catch (Exception ex)
        {
            _embeddingError.Value = ex.Message;
        }
        finally
        {
            _embeddingProcessing.Value = false;
        }
    }

    private void RenderWebSearcherCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Web Searcher");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _webSearcherModel.Value,
                        options: GetModelOptions<WebSearcherModel>(),
                        onValueChange: async v => _webSearcherModel.Value = v ?? _webSearcherModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Query");
                    view.TextField(
                        [Input.Default],
                        value: _webSearcherQuery.Value,
                        onValueChange: async v => _webSearcherQuery.Value = v ?? "");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Max Results");
                    view.TextField(
                        [Input.Default, "w-24"],
                        value: _webSearcherMaxResults.Value.ToString(),
                        type: "number",
                        onValueChange: async v =>
                        {
                            if (int.TryParse(v, out var num) && num > 0)
                            {
                                _webSearcherMaxResults.Value = num;
                            }
                        });
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Search",
                        props: TestId("ai-web-searcher-run"),
                        disabled: _webSearcherProcessing.Value || string.IsNullOrWhiteSpace(_webSearcherQuery.Value),
                        onClick: SearchWebAsync);

                    if (_webSearcherProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_webSearcherError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-web-searcher-error"), content: view =>
                    {
                        view.Text([Alert.Description], _webSearcherError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_webSearcherResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-web-searcher-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Results");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _webSearcherResult.Value);
                    });
                }
            });
        });
    }

    private async Task SearchWebAsync()
    {
        _webSearcherProcessing.Value = true;
        _webSearcherError.Value = null;
        _webSearcherResult.Value = null;

        try
        {
            var model = Enum.Parse<WebSearcherModel>(_webSearcherModel.Value);
            using var searcher = new WebSearcher(model);

            var results = await searcher.SearchPagesAsync(new SearchConfig
            {
                Query = _webSearcherQuery.Value,
                MaxResults = _webSearcherMaxResults.Value
            });

            var output = $"Found {results.Count} results:\n";

            foreach (var result in results)
            {
                output += $"\n• {result.Title}\n  {result.Url}\n";
            }

            _webSearcherResult.Value = output;
        }
        catch (Exception ex)
        {
            _webSearcherError.Value = ex.Message;
        }
        finally
        {
            _webSearcherProcessing.Value = false;
        }
    }

    private void RenderWebScraperCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Web Scraper");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _webScraperModel.Value,
                        options: GetModelOptions<WebScraperModel>(),
                        onValueChange: async v => _webScraperModel.Value = v ?? _webScraperModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "URL");
                    view.TextField(
                        [Input.Default],
                        value: _webScraperUrl.Value,
                        onValueChange: async v => _webScraperUrl.Value = v ?? "");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Output Format");
                    view.Select(
                        value: _webScraperOutputFormat.Value,
                        options: GetModelOptions<WebScraperOutputFormat>(),
                        onValueChange: async v => _webScraperOutputFormat.Value = v ?? _webScraperOutputFormat.Value);
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Scrape",
                        props: TestId("ai-web-scraper-run"),
                        disabled: _webScraperProcessing.Value || string.IsNullOrWhiteSpace(_webScraperUrl.Value),
                        onClick: ScrapeWebAsync);

                    if (_webScraperProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_webScraperError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-web-scraper-error"), content: view =>
                    {
                        view.Text([Alert.Description], _webScraperError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_webScraperResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], props: TestId("ai-web-scraper-result"), content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Scraped Content");
                        view.Text([Text.Body, "whitespace-pre-wrap text-sm"], _webScraperResult.Value);
                    });
                }
            });
        });
    }

    private async Task ScrapeWebAsync()
    {
        _webScraperProcessing.Value = true;
        _webScraperError.Value = null;
        _webScraperResult.Value = null;

        try
        {
            var model = Enum.Parse<WebScraperModel>(_webScraperModel.Value);
            var outputFormat = Enum.Parse<WebScraperOutputFormat>(_webScraperOutputFormat.Value);
            var scraper = new WebScraper(model);

            var page = await scraper.ScrapeSinglePageAsync(new SinglePageScrapeConfig
            {
                Url = _webScraperUrl.Value,
                OutputFormat = outputFormat
            });

            _webScraperResult.Value = $"Title: {page.Title}\n\n{page.Content}";
        }
        catch (Exception ex)
        {
            _webScraperError.Value = ex.Message;
        }
        finally
        {
            _webScraperProcessing.Value = false;
        }
    }

    private void RenderRerankerCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Reranker");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _rerankerModel.Value,
                        options: GetModelOptions<RerankModel>(),
                        onValueChange: async v => _rerankerModel.Value = v ?? _rerankerModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Query");
                    view.TextField(
                        [Input.Default],
                        value: _rerankerQuery.Value,
                        onValueChange: async v => _rerankerQuery.Value = v ?? "");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Documents (one per line)");
                    view.TextArea(
                        [Textarea.Default, "min-h-[120px]"],
                        value: _rerankerDocuments.Value,
                        onValueChange: async v => _rerankerDocuments.Value = v ?? "");
                });

                view.Checkbox([Checkbox.Default],
                    label: "Allow truncation (Voyage scores an over-long document on its head instead of refusing it)",
                    value: _rerankerAllowTruncation.Value,
                    onValueChange: async v => _rerankerAllowTruncation.Value = v);

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Rerank",
                        props: TestId("ai-reranker-run"),
                        disabled: _rerankerProcessing.Value || string.IsNullOrWhiteSpace(_rerankerQuery.Value) || string.IsNullOrWhiteSpace(_rerankerDocuments.Value),
                        onClick: RerankDocumentsAsync);

                    if (_rerankerProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_rerankerError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-reranker-error"), content: view =>
                    {
                        view.Text([Alert.Description], _rerankerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_rerankerResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-reranker-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Ranked Results");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _rerankerResult.Value);
                    });
                }
            });
        });
    }

    private async Task RerankDocumentsAsync()
    {
        _rerankerProcessing.Value = true;
        _rerankerError.Value = null;
        _rerankerResult.Value = null;

        try
        {
            var model = Enum.Parse<RerankModel>(_rerankerModel.Value);
            using var reranker = new Reranker(model);

            var documents = _rerankerDocuments.Value
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            var items = await reranker.RerankAsync(new RerankerConfig { Documents = documents, Query = _rerankerQuery.Value, AllowTruncation = _rerankerAllowTruncation.Value });

            var output = "";

            foreach (var item in items)
            {
                output += $"{item.Index + 1}. Score: {item.Score:F4} - {documents[item.Index]}\n";
            }

            _rerankerResult.Value = output;
        }
        catch (Exception ex)
        {
            _rerankerError.Value = ex.Message;
        }
        finally
        {
            _rerankerProcessing.Value = false;
        }
    }

    private void RenderImageGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Image Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _imageGeneratorModel.Value,
                        options: GetModelOptions<ImageGeneratorModel>(),
                        onValueChange: async v => _imageGeneratorModel.Value = v ?? _imageGeneratorModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Prompt");
                    view.TextArea(
                        [Textarea.Default],
                        value: _imageGeneratorPrompt.Value,
                        onValueChange: async v => _imageGeneratorPrompt.Value = v ?? "");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Negative Prompt (optional)");
                    view.TextField(
                        [Input.Default],
                        value: _imageGeneratorNegativePrompt.Value,
                        onValueChange: async v => _imageGeneratorNegativePrompt.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Width");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorWidth.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num > 0)
                                {
                                    _imageGeneratorWidth.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Height");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorHeight.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num > 0)
                                {
                                    _imageGeneratorHeight.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Seed (0 = random)");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorSeed.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num))
                                {
                                    _imageGeneratorSeed.Value = num;
                                }
                            });
                    });
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Steps (0 = auto)");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorSteps.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num >= 0)
                                {
                                    _imageGeneratorSteps.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Quality");
                        view.Select(
                            value: _imageGeneratorQuality.Value,
                            options: GetModelOptions<ImageQuality>(),
                            onValueChange: async v => _imageGeneratorQuality.Value = v ?? _imageGeneratorQuality.Value);
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Background");
                        view.Select(
                            value: _imageGeneratorBackground.Value,
                            options: GetModelOptions<ImageBackground>(),
                            onValueChange: async v => _imageGeneratorBackground.Value = v ?? _imageGeneratorBackground.Value);
                    });
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "w-24"], content: view =>
                    {
                        view.Text([FormField.Label], "Count (1-4)");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorCount.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num >= 1 && num <= 4)
                                {
                                    _imageGeneratorCount.Value = num;
                                }
                            });
                    });
                });

                view.Row([Layout.Row.InlineCenter, "flex-wrap"], content: view =>
                {
                    view.Checkbox(
                        [Checkbox.Default],
                        value: _imageGeneratorUpsamplePrompt.Value,
                        onValueChange: async v => _imageGeneratorUpsamplePrompt.Value = v,
                        label: "Upsample Prompt");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Input Image (optional)");

                    view.FileUpload(
                        [FileUpload.Zone.Base],
                        accept: ["image/*"],
                        multiple: false,
                        onUploadComplete: async args =>
                        {
                            if (args.LocalTempFilePath == null)
                            {
                                return;
                            }

                            _imageGeneratorInputImageData = await File.ReadAllBytesAsync(args.LocalTempFilePath);
                            _imageGeneratorInputImageMimeType = args.MimeType;
                            _imageGeneratorInputImageName.Value = args.FileName;
                        },
                        content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "image");
                                view.Text([Text.Body], "Upload input image");
                            });
                        });

                    if (!string.IsNullOrEmpty(_imageGeneratorInputImageName.Value))
                    {
                        view.Row([Layout.Row.InlineCenter, "mt-2 flex-wrap"], content: view =>
                        {
                            view.Text([Text.Caption], _imageGeneratorInputImageName.Value);
                            view.Button(
                                [Button.OutlineMd, Button.Icon],
                                onClick: async () =>
                                {
                                    _imageGeneratorInputImageData = null;
                                    _imageGeneratorInputImageMimeType = null;
                                    _imageGeneratorInputImageName.Value = "";
                                },
                                content: v => v.Icon([Icon.Default], name: "x"));
                        });
                    }
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Image",
                        props: TestId("ai-image-generator-run"),
                        disabled: _imageGeneratorProcessing.Value || string.IsNullOrWhiteSpace(_imageGeneratorPrompt.Value),
                        onClick: GenerateImageAsync);

                    if (_imageGeneratorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_imageGeneratorDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            props: TestId("ai-image-generator-result"),
                            href: _imageGeneratorDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Image");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_imageGeneratorError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-image-generator-error"), content: view =>
                    {
                        view.Text([Alert.Description], _imageGeneratorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_imageGeneratorProvenance.Value))
                {
                    view.Text([Text.Caption, "mt-4"], _imageGeneratorProvenance.Value, props: TestId("ai-image-generator-provenance"));
                }

                if (_imageGeneratorResultDataUrls.Value.Count > 0)
                {
                    view.Flex(["mt-4 flex-wrap gap-4"], content: view =>
                    {
                        foreach (var dataUrl in _imageGeneratorResultDataUrls.Value)
                        {
                            view.Image(["self-start max-w-full h-auto rounded-lg border border-secondary",
                                    _imageGeneratorResultDataUrls.Value.Count > 1 ? "max-h-[400px]" : ""],
                                src: dataUrl,
                                alt: "Generated image");
                        }
                    });
                }
            });
        });
    }

    // A Full marking must be backed by a pixel mark the detector actually finds; anything short of
    // Full is honest only when the metadata packet is really there.
    private static string DescribeImageProvenance(ProvenanceMarking marking, byte[] imageData)
    {
        var score = ImageProvenance.MeasureInvisibleMark(imageData);
        var metadata = ImageProvenance.ReadMetadataMark(imageData);
        var hasMetadata = !string.IsNullOrEmpty(metadata);
        var consistent = marking switch
        {
            ProvenanceMarking.Full => hasMetadata && score >= ImageProvenance.DetectionThreshold,
            ProvenanceMarking.MetadataOnly => hasMetadata,
            _ => false
        };

        return $"{(consistent ? "PASS" : "FAIL")} Provenance: {marking}, invisible mark z={score:F1} (threshold {ImageProvenance.DetectionThreshold:F0}), metadata mark {(hasMetadata ? "present" : "missing")}, support {ImageProvenance.GetMarkingSupport(imageData)}";
    }

    private async Task GenerateImageAsync()
    {
        _imageGeneratorProcessing.Value = true;
        _imageGeneratorError.Value = null;
        _imageGeneratorResult.Value = null;
        _imageGeneratorDownloadUrl.Value = null;
        _imageGeneratorProvenance.Value = null;
        _imageGeneratorResultDataUrls.Value = [];

        try
        {
            var model = Enum.Parse<ImageGeneratorModel>(_imageGeneratorModel.Value);
            using var generator = new ImageGenerator(model);

            var config = new ImageGeneratorConfig
            {
                Prompt = _imageGeneratorPrompt.Value,
                NegativePrompt = _imageGeneratorNegativePrompt.Value,
                Width = _imageGeneratorWidth.Value,
                Height = _imageGeneratorHeight.Value,
                Seed = _imageGeneratorSeed.Value,
                Steps = _imageGeneratorSteps.Value,
                Quality = Enum.Parse<ImageQuality>(_imageGeneratorQuality.Value),
                Background = Enum.Parse<ImageBackground>(_imageGeneratorBackground.Value),
                UpsamplePrompt = _imageGeneratorUpsamplePrompt.Value,
                Count = _imageGeneratorCount.Value
            };

            if (_imageGeneratorInputImageData != null && _imageGeneratorInputImageMimeType != null)
            {
                config.InputImages.Add(new InputImage
                {
                    Data = _imageGeneratorInputImageData,
                    MimeType = _imageGeneratorInputImageMimeType,
                    Type = InputImageType.Normal
                });
            }

            var results = await generator.GenerateImageAsync(config);

            if (results.Count > 0)
            {
                var image = results[0];
                var imageData = await image.GetDataAsync();
                _imageGeneratorResult.Value = $"Generated {results.Count} image(s)";
                _imageGeneratorProvenance.Value = DescribeImageProvenance(image.Provenance, imageData);

                var dataUrls = new List<string>();

                foreach (var r in results)
                {
                    dataUrls.Add($"data:{r.MimeType};base64,{Convert.ToBase64String(await r.GetDataAsync())}");
                }

                _imageGeneratorResultDataUrls.Value = dataUrls;
                _imageGeneratorResultDataUrls.NotifyUpdate();

                var ext = image.MimeType == MimeTypes.ImagePng ? "png" : "jpg";
                _imageGeneratorDownloadUrl.Value = await UploadForDownloadAsync($"generated-image.{ext}", imageData, image.MimeType);
            }
        }
        catch (Exception ex)
        {
            _imageGeneratorError.Value = ex.Message;
        }
        finally
        {
            _imageGeneratorProcessing.Value = false;
        }
    }

    private void RenderMusicGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Music Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _musicModel.Value,
                        options: GetModelOptions<MusicGeneratorModel>(),
                        onValueChange: async v => _musicModel.Value = v ?? _musicModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Prompt");
                    view.TextArea(
                        [Textarea.Default],
                        value: _musicPrompt.Value,
                        onValueChange: async v => _musicPrompt.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        var supportsDuration = TryGetMusicCapabilities(_musicModel.Value)?.SupportsDurationControl ?? true;

                        view.Text([FormField.Label], "Duration (seconds)");
                        view.TextField(
                            [Input.Default],
                            value: _musicDuration.Value.ToString("F0"),
                            type: "number",
                            disabled: !supportsDuration,
                            onValueChange: async v =>
                            {
                                if (double.TryParse(v, out var num) && num > 0)
                                {
                                    _musicDuration.Value = num;
                                }
                            });

                        if (!supportsDuration)
                        {
                        }
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Adherence (0-1, editing)");
                        view.TextField(
                            [Input.Default],
                            value: _musicStrength.Value.ToString("F2"),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (double.TryParse(v, out var num) && num is >= 0 and <= 1)
                                {
                                    _musicStrength.Value = num;
                                }
                            });
                    });
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Input Audio (optional, for audio-to-audio editing)");

                    view.FileUpload(
                        [FileUpload.Zone.Base],
                        accept: ["audio/*"],
                        multiple: false,
                        onUploadComplete: async args =>
                        {
                            if (args.LocalTempFilePath == null)
                            {
                                return;
                            }

                            _musicInputAudioData = await File.ReadAllBytesAsync(args.LocalTempFilePath);
                            _musicInputAudioMimeType = args.MimeType;
                            _musicInputAudioName.Value = args.FileName;
                        },
                        content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "music");
                                view.Text([Text.Body], "Upload input clip");
                            });
                        });

                    if (!string.IsNullOrEmpty(_musicInputAudioName.Value))
                    {
                        view.Row([Layout.Row.InlineCenter, "mt-2 flex-wrap"], content: view =>
                        {
                            view.Text([Text.Caption], _musicInputAudioName.Value);
                            view.Button(
                                [Button.OutlineMd, Button.Icon],
                                onClick: async () =>
                                {
                                    _musicInputAudioData = null;
                                    _musicInputAudioMimeType = null;
                                    _musicInputAudioName.Value = "";
                                },
                                content: v => v.Icon([Icon.Default], name: "x"));
                        });
                    }
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Music",
                        props: TestId("ai-music-run"),
                        disabled: _musicProcessing.Value || string.IsNullOrWhiteSpace(_musicPrompt.Value),
                        onClick: GenerateMusicAsync);

                    if (_musicProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_musicDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _musicDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Audio");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_musicError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-music-error"), content: view =>
                    {
                        view.Text([Alert.Description], _musicError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_musicResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-music-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Music Generated");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _musicResult.Value);
                    });
                }
            });
        });
    }

    private async Task GenerateMusicAsync()
    {
        _musicProcessing.Value = true;
        _musicError.Value = null;
        _musicResult.Value = null;
        _musicDownloadUrl.Value = null;

        try
        {
            var model = Enum.Parse<MusicGeneratorModel>(_musicModel.Value);
            using var generator = new MusicGenerator(model);

            var config = new MusicGeneratorConfig
            {
                Prompt = _musicPrompt.Value,
                DurationSeconds = _musicDuration.Value
            };

            if (_musicInputAudioData != null && _musicInputAudioMimeType != null)
            {
                config.InputAudios.Add(new InputAudio
                {
                    Data = _musicInputAudioData,
                    MimeType = _musicInputAudioMimeType,
                    Strength = _musicStrength.Value
                });
            }

            if (generator.SupportsStreaming)
            {
                await StreamMusicAsync(generator, config);
                return;
            }

            var result = await generator.GenerateMusicFileAsync(config);
            var musicData = await result.GetDataAsync();

            if (musicData.Length == 0)
            {
                _musicError.Value = "The model returned no audio";
                return;
            }

            var ext = result.MimeType switch
            {
                "audio/mpeg" => "mp3",
                "audio/wav" or "audio/x-wav" => "wav",
                "audio/ogg" => "ogg",
                _ => "bin"
            };

            var musicMark = MediaProvenance.ReadMetadataMark(musicData);
            _musicResult.Value = $"Generated {result.DurationSeconds:F1}s of audio ({musicData.Length} bytes, {result.MimeType})\nProvenance: {result.Provenance}, metadata mark {(string.IsNullOrEmpty(musicMark) ? "missing" : "present")}";
            _musicDownloadUrl.Value = await UploadForDownloadAsync($"generated-music.{ext}", musicData, result.MimeType);
        }
        catch (Exception ex)
        {
            _musicError.Value = ex.Message;
        }
        finally
        {
            _musicProcessing.Value = false;
        }
    }

    private static MusicGeneratorCapabilities? TryGetMusicCapabilities(string modelName)
    {
        return Enum.TryParse<MusicGeneratorModel>(modelName, out var model)
            ? MusicGenerator.GetCapabilities(model)
            : null;
    }

    private async Task StreamMusicAsync(MusicGenerator generator, MusicGeneratorConfig config)
    {
        var samples = new List<float>();

        await foreach (var audio in generator.GenerateMusicAsync(config))
        {
            Audio.SpeakChunk(MediaTargets.Everyone, audio);
            samples.AddRange(audio.Samples);
        }

        if (samples.Count == 0)
        {
            _musicError.Value = "The model returned no audio";
            return;
        }

        var durationSeconds = (double)samples.Count / generator.SampleRate / generator.ChannelCount;

        using var wav = new WavFile(generator.SampleRate, generator.ChannelCount, WavFile.SampleFormat.Float);
        wav.AddSamples(samples.ToArray());

        // Streamed chunks carry no container and so no mark; the WAV built from them is marked here
        var marked = MediaProvenance.Apply(wav.AsArray(), _musicModel.Value, out var marking);

        _musicResult.Value = $"Streamed {durationSeconds:F1}s of audio at {generator.SampleRate}Hz, {generator.ChannelCount}ch (played live)\nProvenance (MediaProvenance.Apply on the saved WAV): {marking}";
        _musicDownloadUrl.Value = await UploadForDownloadAsync("generated-music.wav", marked, MimeTypes.AudioXWav);
    }

    private void RenderSpeechGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Speech Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _speechGeneratorModel.Value,
                        options: GetModelOptions<SpeechGeneratorModel>(),
                        onValueChange: async v =>
                        {
                            if (v != null && v != _speechGeneratorModel.Value)
                            {
                                _speechGeneratorModel.Value = v;
                                UpdateSpeechGeneratorVoiceIds();
                            }
                        });
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Text");
                    view.TextArea(
                        [Textarea.Default],
                        value: _speechGeneratorText.Value,
                        onValueChange: async v => _speechGeneratorText.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Voice");
                        var voiceOptions = _speechGeneratorVoiceIds.Value
                            .Select(v => new SelectOption(v, v))
                            .ToList();
                        view.Select(
                            value: _speechGeneratorVoiceId.Value,
                            options: voiceOptions,
                            onValueChange: async v => _speechGeneratorVoiceId.Value = v ?? _speechGeneratorVoiceId.Value);
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Language (optional)");
                        view.TextField(
                            [Input.Default],
                            value: _speechGeneratorLanguage.Value,
                            placeholder: "e.g. en-US",
                            onValueChange: async v => _speechGeneratorLanguage.Value = v ?? "");
                    });
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Instructions (optional)");
                    view.TextField(
                        [Input.Default],
                        value: _speechGeneratorInstructions.Value,
                        placeholder: "e.g., Speak like a friendly assistant",
                        onValueChange: async v => _speechGeneratorInstructions.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Speech",
                        props: TestId("ai-speech-generator-run"),
                        disabled: _speechGeneratorProcessing.Value || string.IsNullOrWhiteSpace(_speechGeneratorText.Value),
                        onClick: GenerateSpeechAsync);

                    if (_speechGeneratorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_speechGeneratorDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _speechGeneratorDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Audio");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_speechGeneratorError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-speech-generator-error"), content: view =>
                    {
                        view.Text([Alert.Description], _speechGeneratorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_speechGeneratorResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-speech-generator-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Audio Generated");
                        view.Text([Alert.Description], _speechGeneratorResult.Value);
                    });
                }
            });
        });
    }

    private void UpdateSpeechGeneratorVoiceIds()
    {
        try
        {
            var model = Enum.Parse<SpeechGeneratorModel>(_speechGeneratorModel.Value);
            _speechGenerator?.Dispose();
            _speechGenerator = new SpeechGenerator(model);
            _speechGeneratorVoiceIds.Value = _speechGenerator.VoiceIds;

            if (_speechGeneratorVoiceIds.Value.Count > 0)
            {
                _speechGeneratorVoiceId.Value = _speechGeneratorVoiceIds.Value[0];
            }
        }
        catch (Exception ex)
        {
            _speechGeneratorError.Value = $"Failed to load voice IDs: {ex.Message}";
        }
    }

    private async Task GenerateSpeechAsync()
    {
        _speechGeneratorProcessing.Value = true;
        _speechGeneratorError.Value = null;
        _speechGeneratorResult.Value = null;
        _speechGeneratorDownloadUrl.Value = null;

        try
        {
            var model = Enum.Parse<SpeechGeneratorModel>(_speechGeneratorModel.Value);

            if (_speechGenerator == null || _speechGeneratorModel.Value != model.ToString())
            {
                _speechGenerator?.Dispose();
                _speechGenerator = new SpeechGenerator(model);
            }

            var config = new SpeechGeneratorConfig
            {
                Text = _speechGeneratorText.Value,
                VoiceId = _speechGeneratorVoiceId.Value,
                Language = _speechGeneratorLanguage.Value,
                Instructions = _speechGeneratorInstructions.Value
            };

            var allSamples = new List<float>();

            await foreach (var audio in _speechGenerator.GenerateSpeechAsync(config))
            {
                Audio.SpeakChunk(MediaTargets.Everyone, audio);
                allSamples.AddRange(audio.Samples);
            }

            _speechGeneratorResult.Value = $"Generated {(float)allSamples.Count / _speechGenerator.SampleRate:F1}s of audio at {_speechGenerator.SampleRate}Hz";

            if (allSamples.Count > 0)
            {
                var wav = new WavFile(_speechGenerator.SampleRate, 1, WavFile.SampleFormat.Float);
                wav.AddSamples(allSamples.ToArray());
                _speechGeneratorDownloadUrl.Value = await UploadForDownloadAsync("speech.wav", wav.AsArray(), MimeTypes.AudioXWav);
            }
        }
        catch (Exception ex)
        {
            _speechGeneratorError.Value = ex.Message;
        }
        finally
        {
            _speechGeneratorProcessing.Value = false;
        }
    }

    private void RenderSpeechRecognizerCard(UIView view)
    {
        var isContinuous = _speechRecognizerContinuous.Value;

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Speech Recognizer");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _speechRecognizerModel.Value,
                        options: GetSpeechRecognizerModelOptions(isContinuous),
                        onValueChange: async v => _speechRecognizerModel.Value = v ?? _speechRecognizerModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Language");
                    view.TextField(
                        [Input.Default],
                        value: _speechRecognizerLanguage.Value,
                        onValueChange: async v => _speechRecognizerLanguage.Value = v ?? "");
                });

                view.Row([Layout.Row.InlineCenter, "mb-4 flex-wrap"], content: view =>
                {
                    view.Switch(
                        [Switch.Default],
                        value: isContinuous,
                        onValueChange: value =>
                        {
                            _speechRecognizerContinuous.Value = value;

                            var options = GetSpeechRecognizerModelOptions(value);
                            if (options.All(o => o.Value != _speechRecognizerModel.Value) && options.Count > 0)
                            {
                                _speechRecognizerModel.Value = options[0].Value;
                            }

                            return Task.CompletedTask;
                        },
                        content: view => view.SwitchThumb([Switch.Thumb]),
                        label: "Continuous");
                });

                RenderSpeechRecognizerCapabilities(view, isContinuous);

                if (isContinuous)
                {
                    view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                    {
                        // Colour states the CLIENT truth (stamped on the press); the label states what
                        // the SERVER observed. In a validation harness the two agreeing is the check —
                        // elsewhere the label would be a needless round trip.
                        view.CaptureButton(
                            [Button.PrimaryMd, MicButton.States],
                            kind: MediaCaptureKind.Audio,
                            text: _speechRecognizerRecording.Value ? "Stop Recording" : "Start Recording",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = true,
                                NoiseSuppression = true,
                                EchoCancellation = true,
                            },
                            onCaptureStart: async e =>
                            {
                                _speechRecognizerRecording.Value = true;
                                _speechRecognizerResult.Value = null;
                                _speechRecognizerError.Value = null;
                                StartContinuousRecognition();
                            },
                            onCaptureStop: async e =>
                            {
                                _speechRecognizerRecording.Value = false;
                                StopContinuousRecognition();
                            });

                        view.Button(
                            [Button.PrimaryMd],
                            text: "Recognize from Sample Audio",
                            props: TestId("ai-speech-recognizer-sample"),
                            disabled: _speechRecognizerProcessing.Value || _speechRecognizerRecording.Value,
                            onClick: RecognizeFromSampleAsync);

                        if (_speechRecognizerRecording.Value)
                        {
                            view.Text([Text.Caption], "Listening...");
                        }

                        if (_speechRecognizerProcessing.Value)
                        {
                            view.Box([Icon.Spinner]);
                        }
                    });
                }
                else
                {
                    view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                    {
                        // Colour states the CLIENT truth (stamped on the press); the label states what
                        // the SERVER observed. In a validation harness the two agreeing is the check —
                        // elsewhere the label would be a needless round trip.
                        view.CaptureButton(
                            [Button.PrimaryMd, MicButton.States],
                            kind: MediaCaptureKind.Audio,
                            text: _speechRecognizerRecording.Value ? "Recording..." : "Hold to Record",
                            captureMode: MediaCaptureButtonMode.Hold,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = true,
                                NoiseSuppression = true,
                                EchoCancellation = true,
                            },
                            onCaptureStart: async e =>
                            {
                                _speechRecognizerRecording.Value = true;
                                _speechRecognizerResult.Value = null;
                                _speechRecognizerError.Value = null;
                            },
                            onCaptureStop: async e =>
                            {
                                _speechRecognizerRecording.Value = false;
                            });

                        view.Button(
                            [Button.PrimaryMd],
                            text: "Recognize from Sample Audio",
                            props: TestId("ai-speech-recognizer-sample"),
                            disabled: _speechRecognizerProcessing.Value || _speechRecognizerRecording.Value,
                            onClick: RecognizeFromSampleAsync);

                        if (_speechRecognizerProcessing.Value)
                        {
                            view.Box([Icon.Spinner]);
                        }
                    });
                }

                if (!string.IsNullOrEmpty(_speechRecognizerError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-speech-recognizer-error"), content: view =>
                    {
                        view.Text([Alert.Description], _speechRecognizerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_speechRecognizerResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-speech-recognizer-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Recognized Text");
                        view.Text([Alert.Description], _speechRecognizerResult.Value);

                        if (!string.IsNullOrEmpty(_speechRecognizerDetail.Value))
                        {
                            view.Text([Alert.Description, "mt-2 whitespace-pre-wrap"], _speechRecognizerDetail.Value, props: TestId("ai-speech-recognizer-detail"));
                        }
                    });
                }

                if (!string.IsNullOrEmpty(_speechRecognizerEvents.Value))
                {
                    view.Text([Text.Caption, "mt-2 whitespace-pre-wrap"], _speechRecognizerEvents.Value, props: TestId("ai-speech-recognizer-events"));
                }
            });
        });
    }

    private async Task ProcessSpeechRecognizerBufferAsync(string streamId, SpeechRecognizerBuffer buffer)
    {
        _speechRecognizerBuffers.Remove(streamId);

        if (buffer.Samples.Count == 0)
        {
            _speechRecognizerError.Value = "No audio recorded";
            return;
        }

        _speechRecognizerProcessing.Value = true;
        _speechRecognizerError.Value = null;
        _speechRecognizerResult.Value = null;
        _speechRecognizerDetail.Value = null;

        try
        {
            var model = Enum.Parse<SpeechRecognizerModel>(_speechRecognizerModel.Value);
            _speechRecognizerInstance?.Dispose();
            _speechRecognizerInstance = new SpeechRecognizer(model);

            var timestamps = GetRequestableTimestamps(model);
            var diarize = _speechRecognizerDiarize.Value && SpeechRecognizer.GetCapabilities(model).SupportsDiarization;

            var transcript = await _speechRecognizerInstance.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Language = _speechRecognizerLanguage.Value,
                SampleRate = buffer.SampleRate,
                ChannelCount = buffer.ChannelCount,
                Samples = buffer.Samples.ToArray(),
                Timestamps = timestamps,
                Diarize = diarize
            });

            _speechRecognizerDetail.Value = DescribeTranscriptDetail(transcript, timestamps, diarize);
            _speechRecognizerResult.Value = string.IsNullOrWhiteSpace(transcript.Text) ? "(No speech detected)" : transcript.Text;
        }
        catch (Exception ex)
        {
            _speechRecognizerError.Value = ex.Message;
        }
        finally
        {
            _speechRecognizerProcessing.Value = false;
        }
    }

    private static SpeechTimestamps GetRequestableTimestamps(SpeechRecognizerModel model)
    {
        var capabilities = SpeechRecognizer.GetCapabilities(model);
        var timestamps = SpeechTimestamps.None;

        if (capabilities.SupportsWordTimestamps)
        {
            timestamps |= SpeechTimestamps.Word;
        }

        if (capabilities.SupportsSegmentTimestamps)
        {
            timestamps |= SpeechTimestamps.Segment;
        }

        return timestamps;
    }

    // A granularity the model declared and was asked for must come back filled; an empty list there
    // is the silent failure this line exists to catch
    private static string DescribeTranscriptDetail(Transcript transcript, SpeechTimestamps requested, bool diarized)
    {
        var failures = new List<string>();

        if (requested.HasFlag(SpeechTimestamps.Word) && transcript.Words.Count == 0 && !string.IsNullOrWhiteSpace(transcript.Text))
        {
            failures.Add("word timestamps were requested but none came back");
        }

        if (requested.HasFlag(SpeechTimestamps.Segment) && transcript.Segments.Count == 0 && !string.IsNullOrWhiteSpace(transcript.Text))
        {
            failures.Add("segment timestamps were requested but none came back");
        }

        for (var i = 1; i < transcript.Words.Count; i++)
        {
            if (transcript.Words[i].Start < transcript.Words[i - 1].Start)
            {
                failures.Add($"word {i} starts before word {i - 1}");
                break;
            }
        }

        var firstWords = string.Join(", ", transcript.Words.Take(4).Select(w =>
            $"'{w.Text}'@{w.Start.TotalSeconds:F2}-{w.End.TotalSeconds:F2}s" + (string.IsNullOrEmpty(w.Speaker) ? "" : $" [{w.Speaker}]")));
        var speakers = transcript.Words.Select(w => w.Speaker).Concat(transcript.Segments.Select(s => s.Speaker))
            .Where(s => !string.IsNullOrEmpty(s)).Distinct().Count();

        var detail = $"Requested {requested}{(diarized ? " + diarization" : "")}: {transcript.Words.Count} words, {transcript.Segments.Count} segments"
            + (diarized ? $", {speakers} speaker(s)" : "")
            + $", duration {transcript.Duration.TotalSeconds:F2}s, language '{transcript.Language}', confidence {(transcript.Confidence > 0 ? transcript.Confidence.ToString("F2") : "not reported")}"
            + (firstWords.Length > 0 ? $"\nFirst words: {firstWords}" : "");

        return failures.Count > 0 ? $"FAIL: {string.Join("; ", failures)}\n{detail}" : $"PASS {detail}";
    }

    private void RenderSpeechRecognizerCapabilities(UIView view, bool isContinuous)
    {
        if (!Enum.TryParse<SpeechRecognizerModel>(_speechRecognizerModel.Value, out var model))
        {
            return;
        }

        var capabilities = SpeechRecognizer.GetCapabilities(model);

        view.Text([Text.Caption],
            $"Capabilities: words {capabilities.SupportsWordTimestamps}, segments {capabilities.SupportsSegmentTimestamps}, diarization {capabilities.SupportsDiarization}, continuous {capabilities.SupportsContinuousRecognition}, turn detection {capabilities.TurnDetection}, endpointing {capabilities.Endpointing}, end-of-turn confidence {capabilities.ReportsEndOfTurnConfidence}");

        if (!isContinuous)
        {
            view.Checkbox([Checkbox.Default],
                label: capabilities.SupportsDiarization ? "Diarize (label each word with a speaker)" : "Diarize (not supported by this model)",
                value: _speechRecognizerDiarize.Value && capabilities.SupportsDiarization,
                disabled: !capabilities.SupportsDiarization,
                onValueChange: async v => _speechRecognizerDiarize.Value = v);
            return;
        }

        view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
        {
            if (capabilities.Endpointing.HasFlag(SpeechEndpointing.Silence))
            {
                var range = capabilities.RangeOf(SpeechEndpointing.Silence);

                view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
                {
                    view.Text([FormField.Label], range.IsKnown
                        ? $"End-of-turn silence ms ({range.Minimum:F0}-{range.Maximum:F0}, 0 = model default)"
                        : "End-of-turn silence ms (0 = model default)");
                    view.TextField(
                        [Input.Default],
                        value: _speechRecognizerEndOfTurnSilenceMs.Value.ToString(),
                        type: "number",
                        onValueChange: async v =>
                        {
                            if (int.TryParse(v, out var ms) && ms >= 0)
                            {
                                _speechRecognizerEndOfTurnSilenceMs.Value = ms;
                            }
                        });
                });
            }

            if (capabilities.Endpointing.HasFlag(SpeechEndpointing.ConfidenceThreshold))
            {
                view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
                {
                    view.Text([FormField.Label], "End-of-turn confidence threshold (0-1, 0 = model default)");
                    view.TextField(
                        [Input.Default],
                        value: _speechRecognizerEndOfTurnConfidence.Value.ToString("F2"),
                        type: "number",
                        onValueChange: async v =>
                        {
                            if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var threshold) && threshold is >= 0 and <= 1)
                            {
                                _speechRecognizerEndOfTurnConfidence.Value = threshold;
                            }
                        });
                });
            }
        });
    }

    private void StartContinuousRecognition()
    {
        _speechRecognizerChannel = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        _speechRecognizerCts = new CancellationTokenSource();
        _speechRecognizerResult.Value = null;
        _speechRecognizerDetail.Value = null;
        _speechRecognizerError.Value = null;

        _ = RunContinuousRecognitionAsync(_speechRecognizerCts.Token);
    }

    private void StopContinuousRecognition()
    {
        _speechRecognizerChannel?.Writer.TryComplete();
        _speechRecognizerCts?.Cancel();
        _speechRecognizerChannel = null;
        _speechRecognizerCts = null;
    }

    private async Task RunContinuousRecognitionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var model = Enum.Parse<SpeechRecognizerModel>(_speechRecognizerModel.Value);
            _speechRecognizerInstance?.Dispose();
            _speechRecognizerInstance = new SpeechRecognizer(model);

            ISpeechRecognizer recognizer = _speechRecognizerInstance;

            if (!_speechRecognizerInstance.SupportsContinuousRecognition)
            {
                recognizer = new SpeechRecognizerAdapter(_speechRecognizerInstance);
            }

            var streamInfo = _speechRecognizerStreamInfo.Values.FirstOrDefault();
            var sampleRate = streamInfo.SampleRate > 0 ? streamInfo.SampleRate : 16000;
            var channelCount = streamInfo.ChannelCount > 0 ? streamInfo.ChannelCount : 1;

            var config = new RecognizeContinuousSpeechConfig
            {
                Language = _speechRecognizerLanguage.Value,
                SampleRate = sampleRate,
                ChannelCount = channelCount
            };

            // The adapter stands in for a batch-only model and takes none of the endpointing fields;
            // each field is only set where the model declares it, because setting one it lacks throws
            if (ReferenceEquals(recognizer, _speechRecognizerInstance))
            {
                var endpointing = recognizer.Endpointing;

                if (recognizer.SupportsWordTimestamps)
                {
                    config = config with { Timestamps = SpeechTimestamps.Word };
                }

                if (_speechRecognizerEndOfTurnSilenceMs.Value > 0 && endpointing.HasFlag(SpeechEndpointing.Silence))
                {
                    config = config with { EndOfTurnSilence = TimeSpan.FromMilliseconds(_speechRecognizerEndOfTurnSilenceMs.Value) };
                }

                if (_speechRecognizerEndOfTurnConfidence.Value > 0 && endpointing.HasFlag(SpeechEndpointing.ConfidenceThreshold))
                {
                    config = config with { EndOfTurnConfidenceThreshold = _speechRecognizerEndOfTurnConfidence.Value };
                }
            }

            var finals = 0;
            var endsOfTurn = 0;
            var words = 0;
            var lastEndOfTurnConfidence = 0.0;
            _speechRecognizerEvents.Value = $"Continuous on {model}: waiting for events (turn detection {recognizer.TurnDetection}, endpointing {recognizer.Endpointing})";

            await foreach (var transcriptEvent in recognizer.RecognizeContinuousSpeechAsync(config, _speechRecognizerChannel!.Reader.ReadAllAsync(cancellationToken), cancellationToken))
            {
                if (transcriptEvent.IsFinal)
                {
                    finals++;
                    words += transcriptEvent.Words.Count;
                }

                if (transcriptEvent.IsEndOfTurn)
                {
                    endsOfTurn++;
                    lastEndOfTurnConfidence = transcriptEvent.EndOfTurnConfidence;
                }

                var confidence = recognizer.ReportsEndOfTurnConfidence ? $"{lastEndOfTurnConfidence:F2}" : "not reported";
                _speechRecognizerEvents.Value = $"Continuous on {model}: {finals} final(s), {endsOfTurn} end(s) of turn (last confidence {confidence}), {words} timed word(s); last event [{transcriptEvent.Start.TotalSeconds:F2}-{transcriptEvent.End.TotalSeconds:F2}s] final={transcriptEvent.IsFinal} endOfTurn={transcriptEvent.IsEndOfTurn}";

                if (string.IsNullOrWhiteSpace(transcriptEvent.Text))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(_speechRecognizerResult.Value))
                {
                    _speechRecognizerResult.Value = transcriptEvent.Text;
                }
                else
                {
                    _speechRecognizerResult.Value += " " + transcriptEvent.Text;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _speechRecognizerError.Value = ex.Message;
        }
    }

    private async Task RecognizeFromSampleAsync()
    {
        _speechRecognizerProcessing.Value = true;
        _speechRecognizerError.Value = null;
        _speechRecognizerResult.Value = null;
        _speechRecognizerDetail.Value = null;

        try
        {
            var audioPath = Path.Combine(app.DataDirectory, "audio.raw");
            var rawBytes = await File.ReadAllBytesAsync(audioPath);
            var samples = AudioUtils.ConvertPcm16ToFloat(rawBytes);

            var model = Enum.Parse<SpeechRecognizerModel>(_speechRecognizerModel.Value);
            _speechRecognizerInstance?.Dispose();
            _speechRecognizerInstance = new SpeechRecognizer(model);

            var timestamps = GetRequestableTimestamps(model);
            var diarize = _speechRecognizerDiarize.Value && SpeechRecognizer.GetCapabilities(model).SupportsDiarization;

            var transcript = await _speechRecognizerInstance.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Language = _speechRecognizerLanguage.Value,
                SampleRate = 16000,
                ChannelCount = 1,
                Samples = samples,
                Timestamps = timestamps,
                Diarize = diarize
            });

            _speechRecognizerDetail.Value = DescribeTranscriptDetail(transcript, timestamps, diarize);
            _speechRecognizerResult.Value = string.IsNullOrWhiteSpace(transcript.Text) ? "(No speech detected)" : transcript.Text;
        }
        catch (Exception ex)
        {
            _speechRecognizerError.Value = ex.Message;
        }
        finally
        {
            _speechRecognizerProcessing.Value = false;
        }
    }

    private void RenderOCRCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "OCR");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _ocrModel.Value,
                        options: GetModelOptions<OCRModel>(),
                        onValueChange: async v => _ocrModel.Value = v ?? _ocrModel.Value);
                });

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["image/*", ".pdf"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _ocrFileName.Value = args.FileName;
                        _ocrFilePath = args.LocalTempFilePath;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "upload");
                            view.Text([Text.Body], string.IsNullOrEmpty(_ocrFileName.Value) ? "Upload image or PDF" : _ocrFileName.Value);
                            view.Text([Text.Caption], "Images or PDF files");
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Extract from Upload",
                        disabled: _ocrProcessing.Value || string.IsNullOrEmpty(_ocrFilePath),
                        onClick: PerformOCRAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Extract from Sample PDF",
                        props: TestId("ai-ocr-sample"),
                        disabled: _ocrProcessing.Value,
                        onClick: PerformOCRFromSampleAsync);

                    if (_ocrProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_ocrDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _ocrDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Full Text");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_ocrError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-ocr-error"), content: view =>
                    {
                        view.Text([Alert.Description], _ocrError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_ocrResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], props: TestId("ai-ocr-result"), content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Extracted Text");
                        view.Text([Text.Body, "whitespace-pre-wrap"], _ocrResult.Value);
                    });
                }
            });
        });
    }

    private async Task PerformOCRAsync()
    {
        if (string.IsNullOrEmpty(_ocrFilePath) || !File.Exists(_ocrFilePath))
        {
            _ocrError.Value = "File not found";
            return;
        }

        _ocrProcessing.Value = true;
        _ocrError.Value = null;
        _ocrResult.Value = null;
        _ocrDownloadUrl.Value = null;
        _ocrFullResult = null;

        try
        {
            var model = Enum.Parse<OCRModel>(_ocrModel.Value);
            var ocr = new OCR(model);

            var data = await File.ReadAllBytesAsync(_ocrFilePath);
            var result = await ocr.AnalyzeDocumentAsync(new OCRConfig { Data = data });

            await SetOCRResultAsync(result.Text);
        }
        catch (Exception ex)
        {
            _ocrError.Value = ex.Message;
        }
        finally
        {
            _ocrProcessing.Value = false;
        }
    }

    private async Task PerformOCRFromSampleAsync()
    {
        _ocrProcessing.Value = true;
        _ocrError.Value = null;
        _ocrResult.Value = null;
        _ocrDownloadUrl.Value = null;
        _ocrFullResult = null;

        try
        {
            var model = Enum.Parse<OCRModel>(_ocrModel.Value);
            var ocr = new OCR(model);

            var samplePath = Path.Combine(app.DataDirectory, "sample.pdf");
            var data = await File.ReadAllBytesAsync(samplePath);
            var result = await ocr.AnalyzeDocumentAsync(new OCRConfig { Data = data });

            await SetOCRResultAsync(result.Text);
        }
        catch (Exception ex)
        {
            _ocrError.Value = ex.Message;
        }
        finally
        {
            _ocrProcessing.Value = false;
        }
    }

    private async Task SetOCRResultAsync(string fullText)
    {
        _ocrFullResult = fullText;
        var lines = fullText.Split('\n');

        if (lines.Length > 20)
        {
            _ocrResult.Value = string.Join('\n', lines.Take(20)) + "\n...";
            var textBytes = Encoding.UTF8.GetBytes(fullText);
            _ocrDownloadUrl.Value = await UploadForDownloadAsync("ocr-result.txt", textBytes, MimeTypes.TextPlain);
        }
        else
        {
            _ocrResult.Value = fullText;
        }
    }

    private void RenderFileConverterCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "File Converter");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _fileConverterModel.Value,
                        options: GetModelOptions<FileConverterModel>(),
                        onValueChange: async v => _fileConverterModel.Value = v ?? _fileConverterModel.Value);
                });

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: [".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt", ".html"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _fileConverterFileName.Value = args.FileName;
                        _fileConverterFilePath = args.LocalTempFilePath;
                        _fileConverterResult.Value = null;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "file-text");
                            view.Text([Text.Body], string.IsNullOrEmpty(_fileConverterFileName.Value) ? "Upload document" : _fileConverterFileName.Value);
                            view.Text([Text.Caption], ".docx, .xlsx, .pptx, .txt, .html");
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Convert from Upload",
                        disabled: _fileConverterProcessing.Value || string.IsNullOrEmpty(_fileConverterFilePath),
                        onClick: ConvertFileAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Convert from Sample PPTX",
                        props: TestId("ai-file-converter-sample"),
                        disabled: _fileConverterProcessing.Value,
                        onClick: async () =>
                        {
                            _fileConverterFilePath = Path.Combine(app.DataDirectory, "sample.pptx");
                            _fileConverterFileName.Value = "sample.pptx";
                            await ConvertFileAsync();
                        });

                    if (_fileConverterProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_fileConverterDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _fileConverterDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open PDF");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_fileConverterError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-file-converter-error"), content: view =>
                    {
                        view.Text([Alert.Description], _fileConverterError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_fileConverterResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-file-converter-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Conversion Complete");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _fileConverterResult.Value);
                    });
                }
            });
        });
    }

    private async Task ConvertFileAsync()
    {
        if (string.IsNullOrEmpty(_fileConverterFilePath) || !File.Exists(_fileConverterFilePath))
        {
            _fileConverterError.Value = "File not found";
            return;
        }

        _fileConverterProcessing.Value = true;
        _fileConverterError.Value = null;
        _fileConverterResult.Value = null;
        _fileConverterDownloadUrl.Value = null;

        try
        {
            var model = Enum.Parse<FileConverterModel>(_fileConverterModel.Value);
            var converter = new FileConverter(model);

            var data = await File.ReadAllBytesAsync(_fileConverterFilePath);
            var result = await converter.ConvertToPdfAsync(new FileConverterConfig { Data = data, FileName = _fileConverterFileName.Value });

            // OpenReadAsync rather than GetDataAsync: the stream form is the one to use when the bytes are
            // passed on, and reading it here is what proves it against a real Data- or Url-delivered result
            byte[] resultData;
            long? declaredLength;

            await using (var payload = await result.OpenReadAsync())
            {
                declaredLength = payload.Length;
                using var buffer = new MemoryStream();
                await payload.Content.CopyToAsync(buffer);
                resultData = buffer.ToArray();
            }

            var isPdf = resultData.Length >= 4 && resultData[0] == '%' && resultData[1] == 'P' && resultData[2] == 'D' && resultData[3] == 'F';

            if (!isPdf)
            {
                _fileConverterError.Value = $"FAIL: the converted payload ({resultData.Length} bytes, {result.MimeType}) does not start with %PDF";
                return;
            }

            if (declaredLength is { } length && length != resultData.Length)
            {
                _fileConverterError.Value = $"FAIL: ResultPayloadStream.Length said {length} bytes but {resultData.Length} were read";
                return;
            }

            _fileConverterResult.Value = $"Converted to {result.Name} ({resultData.Length / 1024} KB)\nRead through OpenReadAsync: delivered as {result.Kind}, stream length {(declaredLength?.ToString() ?? "not declared")}, starts with %PDF";
            _fileConverterDownloadUrl.Value = await UploadForDownloadAsync("converted.pdf", resultData, MimeTypes.ApplicationPdf);
        }
        catch (Exception ex)
        {
            _fileConverterError.Value = ex.Message;
        }
        finally
        {
            _fileConverterProcessing.Value = false;
        }
    }

    private void RenderVideoGeneratorCard(UIView view)
    {
        var videoModel = Enum.TryParse<VideoGeneratorModel>(_videoGeneratorModel.Value, out var vm) ? vm : VideoGeneratorModel.Pollo20;
        var videoCapabilities = VideoGenerator.GetCapabilities(videoModel);

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Video Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _videoGeneratorModel.Value,
                        options: GetModelOptions<VideoGeneratorModel>(),
                        onValueChange: async v => _videoGeneratorModel.Value = v ?? _videoGeneratorModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Prompt");
                    view.TextArea(
                        [Textarea.Default],
                        value: _videoGeneratorPrompt.Value,
                        onValueChange: async v => _videoGeneratorPrompt.Value = v ?? "");
                });

                if (videoCapabilities.SupportsNegativePrompt)
                {
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Negative Prompt (optional)");
                        view.TextField(
                            [Input.Default],
                            value: _videoGeneratorNegativePrompt.Value,
                            onValueChange: async v => _videoGeneratorNegativePrompt.Value = v ?? "");
                    });
                }

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Length (seconds)");
                        view.TextField(
                            [Input.Default],
                            value: _videoGeneratorLength.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num > 0)
                                {
                                    _videoGeneratorLength.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Resolution");
                        view.Select(
                            value: _videoGeneratorResolution.Value,
                            options: GetModelOptions<VideoGeneratorResolution>(),
                            onValueChange: async v => _videoGeneratorResolution.Value = v ?? _videoGeneratorResolution.Value);
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Aspect Ratio");
                        view.Select(
                            value: _videoGeneratorAspectRatio.Value,
                            options: GetModelOptions<VideoGeneratorAspectRatio>(),
                            onValueChange: async v => _videoGeneratorAspectRatio.Value = v ?? _videoGeneratorAspectRatio.Value);
                    });

                    if (videoCapabilities.SupportsSeed)
                    {
                        view.Box([FormField.Root, "flex-1"], content: view =>
                        {
                            view.Text([FormField.Label], "Seed (0 = random)");
                            view.TextField(
                                [Input.Default],
                                value: _videoGeneratorSeed.Value.ToString(),
                                type: "number",
                                onValueChange: async v =>
                                {
                                    if (int.TryParse(v, out var num))
                                    {
                                        _videoGeneratorSeed.Value = num;
                                    }
                                });
                        });
                    }
                });

                if (videoCapabilities.SupportsAudio)
                {
                    view.Row([Layout.Row.InlineCenter, "flex-wrap"], content: view =>
                    {
                        view.Checkbox(
                            [Checkbox.Default],
                            value: _videoGeneratorGenerateAudio.Value,
                            onValueChange: async v => _videoGeneratorGenerateAudio.Value = v,
                            label: "Generate Audio");
                    });
                }

                if (videoCapabilities.SupportsImageToVideo)
                {
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Input Image (optional, for image-to-video)");

                        view.FileUpload(
                            [FileUpload.Zone.Base],
                            accept: ["image/*"],
                            multiple: false,
                            onUploadComplete: async args =>
                            {
                                if (args.LocalTempFilePath == null)
                                {
                                    return;
                                }

                                _videoGeneratorInputImageData = await File.ReadAllBytesAsync(args.LocalTempFilePath);
                                _videoGeneratorInputImageMimeType = args.MimeType;
                                _videoGeneratorInputImageName.Value = args.FileName;
                            },
                            content: view =>
                            {
                                view.Column([Layout.Column.Center], content: view =>
                                {
                                    view.Icon([Media.PlaceholderIcon], name: "image");
                                    view.Text([Text.Body], "Upload input image");
                                });
                            });

                        if (!string.IsNullOrEmpty(_videoGeneratorInputImageName.Value))
                        {
                            view.Row([Layout.Row.InlineCenter, "mt-2 flex-wrap"], content: view =>
                            {
                                view.Text([Text.Caption], _videoGeneratorInputImageName.Value);
                                view.Button(
                                    [Button.OutlineMd, Button.Icon],
                                    onClick: async () =>
                                    {
                                        _videoGeneratorInputImageData = null;
                                        _videoGeneratorInputImageMimeType = null;
                                        _videoGeneratorInputImageName.Value = "";
                                    },
                                    content: v => v.Icon([Icon.Default], name: "x"));
                            });
                        }
                    });
                }

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Video",
                        props: TestId("ai-video-generator-run"),
                        disabled: _videoGeneratorProcessing.Value || string.IsNullOrWhiteSpace(_videoGeneratorPrompt.Value),
                        onClick: GenerateVideoAsync);

                    if (_videoGeneratorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_videoGeneratorResultUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _videoGeneratorResultUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Video");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_videoGeneratorError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-video-generator-error"), content: view =>
                    {
                        view.Text([Alert.Description], _videoGeneratorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoGeneratorResultUrl.Value))
                {
                    view.Box([Media.VideoContainer, "mt-4"], props: TestId("ai-video-generator-result"), content: view =>
                    {
                        view.VideoUrlPlayer(
                            ["w-full h-full"],
                            url: _videoGeneratorResultUrl.Value,
                            controls: true,
                            loop: false,
                            muted: false,
                            playsInline: true);
                    });
                }
            });
        });
    }

    private async Task GenerateVideoAsync()
    {
        _videoGeneratorProcessing.Value = true;
        _videoGeneratorError.Value = null;
        _videoGeneratorResultUrl.Value = null;

        try
        {
            var model = Enum.Parse<VideoGeneratorModel>(_videoGeneratorModel.Value);
            var resolution = Enum.Parse<VideoGeneratorResolution>(_videoGeneratorResolution.Value);
            var aspectRatio = Enum.Parse<VideoGeneratorAspectRatio>(_videoGeneratorAspectRatio.Value);
            using var generator = new VideoGenerator(model);

            var config = new VideoGeneratorConfig
            {
                Prompt = _videoGeneratorPrompt.Value,
                NegativePrompt = _videoGeneratorNegativePrompt.Value,
                Length = _videoGeneratorLength.Value,
                Resolution = resolution,
                AspectRatio = aspectRatio
            };

            if (_videoGeneratorSeed.Value > 0)
            {
                config = config with { Seed = _videoGeneratorSeed.Value };
            }

            if (_videoGeneratorGenerateAudio.Value)
            {
                config = config with { GenerateAudio = true };
            }

            if (_videoGeneratorInputImageData != null && _videoGeneratorInputImageMimeType != null)
            {
                config.InputImages.Add(new InputImage
                {
                    Data = _videoGeneratorInputImageData,
                    MimeType = _videoGeneratorInputImageMimeType
                });
            }

            var result = await generator.GenerateVideoAsync(config);

            _videoGeneratorResultUrl.Value = result.Url;
        }
        catch (Exception ex)
        {
            _videoGeneratorError.Value = ex.Message;
        }
        finally
        {
            _videoGeneratorProcessing.Value = false;
        }
    }

    private void RenderSoundEffectGeneratorCard(UIView view)
    {
        var sfxModel = Enum.TryParse<SoundEffectGeneratorModel>(_soundEffectModel.Value, out var sm) ? sm : SoundEffectGeneratorModel.ElevenLabsV2;
        var sfxCapabilities = SoundEffectGenerator.GetCapabilities(sfxModel);

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Sound Effect Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _soundEffectModel.Value,
                        options: GetModelOptions<SoundEffectGeneratorModel>(),
                        onValueChange: async v => _soundEffectModel.Value = v ?? _soundEffectModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Description");
                    view.TextArea(
                        [Textarea.Default],
                        value: _soundEffectPrompt.Value,
                        onValueChange: async v => _soundEffectPrompt.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Duration (seconds)");
                        view.TextField(
                            [Input.Default],
                            value: _soundEffectDuration.Value.ToString("F1"),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (double.TryParse(v, out var num) && num > 0)
                                {
                                    _soundEffectDuration.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], $"Prompt Influence ({_soundEffectPromptInfluence.Value:F1})");
                        view.Slider(
                            [Slider.Default],
                            value: [_soundEffectPromptInfluence.Value],
                            min: 0.0,
                            max: 1.0,
                            step: 0.1,
                            ariaLabel: "Prompt Influence",
                            onValueChange: async values => _soundEffectPromptInfluence.Value = values[0],
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view => { view.SliderRange([Slider.Range]); });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });
                });

                if (sfxCapabilities.SupportsLooping)
                {
                    view.Row([Layout.Row.InlineCenter, "flex-wrap"], content: view =>
                    {
                        view.Checkbox(
                            [Checkbox.Default],
                            value: _soundEffectLoop.Value,
                            onValueChange: async v => _soundEffectLoop.Value = v,
                            label: "Loop");
                    });
                }

                view.Checkbox([Checkbox.Default],
                    label: "Buffered file (GenerateSoundEffectFileAsync, provider-marked WAV) instead of live streaming",
                    value: _soundEffectBuffered.Value,
                    onValueChange: async v => _soundEffectBuffered.Value = v);

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Sound Effect",
                        props: TestId("ai-sound-effect-run"),
                        disabled: _soundEffectProcessing.Value || string.IsNullOrWhiteSpace(_soundEffectPrompt.Value),
                        onClick: GenerateSoundEffectAsync);

                    if (_soundEffectProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_soundEffectDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _soundEffectDownloadUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Audio");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_soundEffectError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-sound-effect-error"), content: view =>
                    {
                        view.Text([Alert.Description], _soundEffectError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_soundEffectResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-sound-effect-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Sound Effect Generated");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _soundEffectResult.Value);
                    });
                }
            });
        });
    }

    private async Task GenerateSoundEffectAsync()
    {
        _soundEffectProcessing.Value = true;
        _soundEffectError.Value = null;
        _soundEffectResult.Value = null;
        _soundEffectDownloadUrl.Value = null;

        try
        {
            var model = Enum.Parse<SoundEffectGeneratorModel>(_soundEffectModel.Value);
            using var generator = new SoundEffectGenerator(model);

            var config = new SoundEffectGeneratorConfig
            {
                Prompt = _soundEffectPrompt.Value,
                DurationSeconds = _soundEffectDuration.Value,
                PromptInfluence = _soundEffectPromptInfluence.Value,
                Loop = _soundEffectLoop.Value
            };

            if (_soundEffectBuffered.Value)
            {
                var file = await generator.GenerateSoundEffectFileAsync(config);
                var fileData = await file.GetDataAsync();
                var fileMark = MediaProvenance.ReadMetadataMark(fileData);
                _soundEffectResult.Value = $"Generated {file.DurationSeconds:F1}s audio ({fileData.Length} bytes, {file.MimeType}, delivered as {file.Kind})\nProvenance: {file.Provenance}, metadata mark {(string.IsNullOrEmpty(fileMark) ? "missing" : "present")}";
                _soundEffectDownloadUrl.Value = await UploadForDownloadAsync("sound-effect.wav", fileData, file.MimeType);
                return;
            }

            var allSamples = new List<float>();

            await foreach (var audio in generator.GenerateSoundEffectAsync(config))
            {
                Audio.SpeakChunk(MediaTargets.Everyone, audio);
                allSamples.AddRange(audio.Samples);
            }

            var durationSeconds = (float)allSamples.Count / generator.SampleRate / generator.ChannelCount;

            if (allSamples.Count == 0)
            {
                _soundEffectResult.Value = $"Generated {durationSeconds:F1}s audio";
                return;
            }

            using var wav = new WavFile(generator.SampleRate, generator.ChannelCount, WavFile.SampleFormat.Float);
            wav.AddSamples(allSamples.ToArray());

            // Streamed chunks carry no container and so no mark; the WAV built from them is marked here
            var marked = MediaProvenance.Apply(wav.AsArray(), model.ToString(), out var marking);
            var readBack = MediaProvenance.ReadMetadataMark(marked);
            _soundEffectResult.Value = $"Generated {durationSeconds:F1}s audio\nProvenance (MediaProvenance.Apply on the saved WAV): {marking}, read back {(string.IsNullOrEmpty(readBack) ? "missing" : "present")}";
            _soundEffectDownloadUrl.Value = await UploadForDownloadAsync("sound-effect.wav", marked, MimeTypes.AudioXWav);
        }
        catch (Exception ex)
        {
            _soundEffectError.Value = ex.Message;
        }
        finally
        {
            _soundEffectProcessing.Value = false;
        }
    }

    private void RenderVideoEnhancerCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Video Enhancer");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _videoEnhancerModel.Value,
                        options: GetModelOptions<VideoEnhancerModel>(),
                        onValueChange: async v => _videoEnhancerModel.Value = v ?? _videoEnhancerModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Video URL");
                    view.TextField(
                        [Input.Default],
                        value: _videoEnhancerVideoUrl.Value,
                        placeholder: "https://example.com/video.mp4",
                        onValueChange: async v => _videoEnhancerVideoUrl.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Target FPS (0 = auto)");
                        view.TextField(
                            [Input.Default],
                            value: _videoEnhancerTargetFps.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num >= 0)
                                {
                                    _videoEnhancerTargetFps.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Start Frame (0 = from start)");
                        view.TextField(
                            [Input.Default],
                            value: _videoEnhancerStartFrame.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num >= 0)
                                {
                                    _videoEnhancerStartFrame.Value = num;
                                }
                            });
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "End Frame (0 = to end)");
                        view.TextField(
                            [Input.Default],
                            value: _videoEnhancerEndFrame.Value.ToString(),
                            type: "number",
                            onValueChange: async v =>
                            {
                                if (int.TryParse(v, out var num) && num >= 0)
                                {
                                    _videoEnhancerEndFrame.Value = num;
                                }
                            });
                    });
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Enhance Video",
                        disabled: _videoEnhancerProcessing.Value || string.IsNullOrWhiteSpace(_videoEnhancerVideoUrl.Value),
                        onClick: EnhanceVideoAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Enhance Sample Video",
                        props: TestId("ai-video-enhancer-sample"),
                        disabled: _videoEnhancerProcessing.Value,
                        onClick: EnhanceSampleVideoAsync);

                    if (_videoEnhancerProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_videoEnhancerResultUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
                            href: _videoEnhancerResultUrl.Value,
                            target: "_blank",
                            content: v =>
                            {
                                v.Icon([Icon.Default, "mr-2"], name: "download");
                                v.Text(text: "Open Video");
                            });
                    }
                });

                if (!string.IsNullOrEmpty(_videoEnhancerError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-video-enhancer-error"), content: view =>
                    {
                        view.Text([Alert.Description], _videoEnhancerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoEnhancerResultInfo.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-video-enhancer-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Enhancement Result");
                        view.Text([Alert.Description], _videoEnhancerResultInfo.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoEnhancerResultUrl.Value))
                {
                    view.Box([Media.VideoContainer, "mt-4"], content: view =>
                    {
                        view.VideoUrlPlayer(
                            ["w-full h-full"],
                            url: _videoEnhancerResultUrl.Value,
                            controls: true,
                            loop: false,
                            muted: false,
                            playsInline: true);
                    });
                }
            });
        });
    }

    private VideoEnhancerConfig BuildVideoEnhancerConfig()
    {
        return new VideoEnhancerConfig
        {
            TargetFps = _videoEnhancerTargetFps.Value > 0 ? _videoEnhancerTargetFps.Value : null,
            StartFrame = _videoEnhancerStartFrame.Value > 0 ? _videoEnhancerStartFrame.Value : null,
            EndFrame = _videoEnhancerEndFrame.Value > 0 ? _videoEnhancerEndFrame.Value : null
        };
    }

    private void SetVideoEnhancerResultInfo(VideoEnhancerResult result)
    {
        var parts = new List<string>();

        if (result.OutputFps.HasValue)
        {
            parts.Add($"{result.OutputFps}fps");
        }

        if (result.OutputSizeBytes.HasValue)
        {
            parts.Add($"{result.OutputSizeBytes.Value / 1024}KB");
        }

        _videoEnhancerResultInfo.Value = parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private async Task EnhanceVideoAsync()
    {
        _videoEnhancerProcessing.Value = true;
        _videoEnhancerError.Value = null;
        _videoEnhancerResultUrl.Value = null;
        _videoEnhancerResultInfo.Value = null;

        try
        {
            var model = Enum.Parse<VideoEnhancerModel>(_videoEnhancerModel.Value);
            using var enhancer = new VideoEnhancer(model);

            var config = BuildVideoEnhancerConfig() with { Url = _videoEnhancerVideoUrl.Value };

            var result = await enhancer.EnhanceVideoAsync(config);

            _videoEnhancerResultUrl.Value = result.Url;
            SetVideoEnhancerResultInfo(result);
        }
        catch (Exception ex)
        {
            _videoEnhancerError.Value = ex.Message;
        }
        finally
        {
            _videoEnhancerProcessing.Value = false;
        }
    }

    private async Task EnhanceSampleVideoAsync()
    {
        _videoEnhancerProcessing.Value = true;
        _videoEnhancerError.Value = null;
        _videoEnhancerResultUrl.Value = null;
        _videoEnhancerResultInfo.Value = null;

        try
        {
            var model = Enum.Parse<VideoEnhancerModel>(_videoEnhancerModel.Value);
            using var enhancer = new VideoEnhancer(model);

            var videoPath = Path.Combine(app.DataDirectory, "sample.mp4");
            var videoBytes = await File.ReadAllBytesAsync(videoPath);

            var config = BuildVideoEnhancerConfig() with
            {
                Data = videoBytes,
                MimeType = "video/mp4"
            };

            var result = await enhancer.EnhanceVideoAsync(config);

            _videoEnhancerResultUrl.Value = result.Url;
            SetVideoEnhancerResultInfo(result);
        }
        catch (Exception ex)
        {
            _videoEnhancerError.Value = ex.Message;
        }
        finally
        {
            _videoEnhancerProcessing.Value = false;
        }
    }

    private void RenderImageSegmenterCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-segmenter-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Image Segmenter");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _imageSegmenterModel.Value,
                        options: GetModelOptions<ImageSegmenterModel>(),
                        onValueChange: async v => _imageSegmenterModel.Value = v ?? _imageSegmenterModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Prompt (concept to segment)");
                    view.TextField(
                        [Input.Default],
                        value: _imageSegmenterPrompt.Value,
                        onValueChange: async v => _imageSegmenterPrompt.Value = v ?? "");
                });

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["image/*"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _imageSegmenterFileName.Value = args.FileName;
                        _imageSegmenterFilePath = args.LocalTempFilePath;
                        _imageSegmenterFileMimeType = args.MimeType;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "image");
                            view.Text([Text.Body], string.IsNullOrEmpty(_imageSegmenterFileName.Value) ? "Upload image" : _imageSegmenterFileName.Value);
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Segment from Upload",
                        disabled: _imageSegmenterProcessing.Value || string.IsNullOrEmpty(_imageSegmenterFilePath) || string.IsNullOrWhiteSpace(_imageSegmenterPrompt.Value),
                        onClick: SegmentImageAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Segment from Sample Image",
                        props: TestId("ai-segmenter-sample"),
                        disabled: _imageSegmenterProcessing.Value || string.IsNullOrWhiteSpace(_imageSegmenterPrompt.Value),
                        onClick: SegmentSampleImageAsync);

                    if (_imageSegmenterProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_imageSegmenterError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-segmenter-error"), content: view =>
                    {
                        view.Text([Alert.Description], _imageSegmenterError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_imageSegmenterResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-segmenter-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description, "whitespace-pre-wrap"], _imageSegmenterResult.Value);
                    });
                }

                if (_imageSegmenterImageDataUrls.Count > 0)
                {
                    view.Flex(["mt-4 flex-wrap gap-4"], content: view =>
                    {
                        foreach (var dataUrl in _imageSegmenterImageDataUrls)
                        {
                            view.Image(["self-start max-w-full h-auto max-h-[300px] rounded-lg border border-secondary"],
                                src: dataUrl,
                                alt: "Segmentation result");
                        }
                    });
                }
            });
        });
    }

    private async Task SegmentImageAsync()
    {
        if (string.IsNullOrEmpty(_imageSegmenterFilePath) || !File.Exists(_imageSegmenterFilePath))
        {
            _imageSegmenterError.Value = "File not found";
            return;
        }

        await SegmentImageCoreAsync(_imageSegmenterFilePath, _imageSegmenterFileMimeType ?? MimeTypes.ImagePng);
    }

    private async Task SegmentSampleImageAsync()
        => await SegmentImageCoreAsync(Path.Combine(app.DataDirectory, "santa.jpg"), MimeTypes.ImageJpeg);

    private async Task SegmentImageCoreAsync(string filePath, string mimeType)
    {
        _imageSegmenterProcessing.Value = true;
        _imageSegmenterError.Value = null;
        _imageSegmenterResult.Value = null;
        _imageSegmenterImageDataUrls.Clear();

        try
        {
            var model = Enum.Parse<ImageSegmenterModel>(_imageSegmenterModel.Value);
            using var segmenter = new ImageSegmenter(model);

            var data = await File.ReadAllBytesAsync(filePath);
            var result = await segmenter.SegmentImageAsync(new ImageSegmenterConfig
            {
                InputImage = new InputImage { Data = data, MimeType = mimeType },
                Prompt = _imageSegmenterPrompt.Value
            });

            var output = $"Found {result.Segments.Count} segment(s)";

            foreach (var segment in result.Segments.Where(s => s.Score != null))
            {
                output += $"\n  Score: {segment.Score:F3}";
            }

            _imageSegmenterResult.Value = output;

            var dataUrls = new List<string>();

            if (result.Preview != null)
            {
                dataUrls.Add($"data:{result.Preview.MimeType};base64,{Convert.ToBase64String(await result.Preview.GetDataAsync())}");
            }

            foreach (var segment in result.Segments)
            {
                dataUrls.Add($"data:{segment.Mask.MimeType};base64,{Convert.ToBase64String(await segment.Mask.GetDataAsync())}");
            }

            _imageSegmenterImageDataUrls.ReplaceAll(dataUrls);
        }
        catch (Exception ex)
        {
            _imageSegmenterError.Value = ex.Message;
        }
        finally
        {
            _imageSegmenterProcessing.Value = false;
        }
    }

    private void RenderImageUpscalerCard(UIView view)
    {
        // Render must not throw on an unexpected selection, or it takes the whole tab down with it
        var selectedModel = Enum.TryParse<ImageUpscalerModel>(_imageUpscalerModel.Value, out var parsed)
            ? parsed
            : ImageUpscalerModel.SeedVr2;
        var capabilities = ImageUpscaler.GetCapabilities(selectedModel);

        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-upscaler-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Image Upscaler");
            view.Text([Text.Caption, "mb-4"], $"Fidelity: {capabilities.Fidelity}");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Model");
                        view.Select(
                            value: _imageUpscalerModel.Value,
                            options: GetModelOptions<ImageUpscalerModel>(),
                            onValueChange: async v => _imageUpscalerModel.Value = v ?? _imageUpscalerModel.Value);
                    });

                    if (capabilities.SupportsScaleFactor)
                    {
                        view.Box([FormField.Root, "flex-1"], content: view =>
                        {
                            view.Text([FormField.Label], $"Scale factor (max {capabilities.MaxScaleFactor})");
                            view.TextField(
                                [Input.Default],
                                value: _imageUpscalerScaleFactor.Value.ToString(),
                                type: "number",
                                onValueChange: async v =>
                                {
                                    if (int.TryParse(v, out var num) && num > 0)
                                    {
                                        _imageUpscalerScaleFactor.Value = num;
                                    }
                                });
                        });
                    }

                    if (capabilities.SupportsCreativity)
                    {
                        view.Box([FormField.Root, "flex-1"], content: view =>
                        {
                            view.Text([FormField.Label], "Creativity (0-1)");
                            view.TextField(
                                [Input.Default],
                                value: _imageUpscalerCreativity.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                type: "number",
                                onValueChange: async v =>
                                {
                                    if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var num) && num is >= 0 and <= 1)
                                    {
                                        _imageUpscalerCreativity.Value = num;
                                    }
                                });
                        });
                    }
                });

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["image/*"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _imageUpscalerFileName.Value = args.FileName;
                        _imageUpscalerFilePath = args.LocalTempFilePath;
                        _imageUpscalerFileMimeType = args.MimeType;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "image");
                            view.Text([Text.Body], string.IsNullOrEmpty(_imageUpscalerFileName.Value) ? "Upload image" : _imageUpscalerFileName.Value);
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Upscale from Upload",
                        disabled: _imageUpscalerProcessing.Value || string.IsNullOrEmpty(_imageUpscalerFilePath),
                        onClick: UpscaleImageAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Upscale Sample Image",
                        props: TestId("ai-upscaler-sample"),
                        disabled: _imageUpscalerProcessing.Value,
                        onClick: UpscaleSampleImageAsync);

                    if (_imageUpscalerProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_imageUpscalerError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-upscaler-error"), content: view =>
                    {
                        view.Text([Alert.Description], _imageUpscalerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_imageUpscalerResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-upscaler-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description], _imageUpscalerResult.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_imageUpscalerImageDataUrl.Value))
                {
                    view.Image(["mt-4 self-start max-w-full h-auto max-h-[400px] rounded-lg border border-secondary"],
                        src: _imageUpscalerImageDataUrl.Value,
                        alt: "Upscaled image");
                }
            });
        });
    }

    private async Task UpscaleImageAsync()
    {
        if (string.IsNullOrEmpty(_imageUpscalerFilePath) || !File.Exists(_imageUpscalerFilePath))
        {
            _imageUpscalerError.Value = "File not found";
            return;
        }

        await UpscaleImageCoreAsync(_imageUpscalerFilePath, _imageUpscalerFileMimeType ?? MimeTypes.ImagePng);
    }

    private async Task UpscaleSampleImageAsync()
        => await UpscaleImageCoreAsync(Path.Combine(app.DataDirectory, "santa.jpg"), MimeTypes.ImageJpeg);

    private async Task UpscaleImageCoreAsync(string filePath, string mimeType)
    {
        _imageUpscalerProcessing.Value = true;
        _imageUpscalerError.Value = null;
        _imageUpscalerResult.Value = null;
        _imageUpscalerImageDataUrl.Value = null;

        try
        {
            var model = Enum.Parse<ImageUpscalerModel>(_imageUpscalerModel.Value);
            var capabilities = ImageUpscaler.GetCapabilities(model);
            using var imageUpscaler = new ImageUpscaler(model);

            var data = await File.ReadAllBytesAsync(filePath);
            var (inputWidth, inputHeight) = ImageUtils.GetImageDimensions(data);

            // A model rejects a knob it does not expose rather than ignoring it, so only send what
            // this one accepts
            var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
            {
                InputImage = new InputImage { Data = data, MimeType = mimeType },
                ScaleFactor = capabilities.SupportsScaleFactor ? _imageUpscalerScaleFactor.Value : 0,
                Creativity = capabilities.SupportsCreativity ? _imageUpscalerCreativity.Value : 0,
            });

            var marking = ImageProvenance.GetMarkingSupport(await result.Image.GetDataAsync());

            _imageUpscalerResult.Value =
                $"Upscaled image {inputWidth}x{inputHeight} to {result.Image.Width}x{result.Image.Height} ({result.Image.MimeType}, provenance {marking})";
            _imageUpscalerImageDataUrl.Value = $"data:{result.Image.MimeType};base64,{Convert.ToBase64String(await result.Image.GetDataAsync())}";
        }
        catch (Exception ex)
        {
            _imageUpscalerError.Value = ex.Message;
        }
        finally
        {
            _imageUpscalerProcessing.Value = false;
        }
    }

    private void RenderDepthEstimatorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-depth-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Depth Estimator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _depthEstimatorModel.Value,
                        options: GetModelOptions<DepthEstimatorModel>(),
                        onValueChange: async v => _depthEstimatorModel.Value = v ?? _depthEstimatorModel.Value);
                });

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["image/*"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _depthEstimatorFileName.Value = args.FileName;
                        _depthEstimatorFilePath = args.LocalTempFilePath;
                        _depthEstimatorFileMimeType = args.MimeType;
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "image");
                            view.Text([Text.Body], string.IsNullOrEmpty(_depthEstimatorFileName.Value) ? "Upload image" : _depthEstimatorFileName.Value);
                        });
                    });

                view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Estimate from Upload",
                        disabled: _depthEstimatorProcessing.Value || string.IsNullOrEmpty(_depthEstimatorFilePath),
                        onClick: EstimateDepthAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        text: "Estimate from Sample Image",
                        props: TestId("ai-depth-sample"),
                        disabled: _depthEstimatorProcessing.Value,
                        onClick: EstimateSampleDepthAsync);

                    if (_depthEstimatorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_depthEstimatorError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-depth-error"), content: view =>
                    {
                        view.Text([Alert.Description], _depthEstimatorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_depthEstimatorResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-depth-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description], _depthEstimatorResult.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_depthEstimatorImageDataUrl.Value))
                {
                    view.Image(["mt-4 self-start max-w-full h-auto max-h-[400px] rounded-lg border border-secondary"],
                        src: _depthEstimatorImageDataUrl.Value,
                        alt: "Depth map");
                }
            });
        });
    }

    private async Task EstimateDepthAsync()
    {
        if (string.IsNullOrEmpty(_depthEstimatorFilePath) || !File.Exists(_depthEstimatorFilePath))
        {
            _depthEstimatorError.Value = "File not found";
            return;
        }

        await EstimateDepthCoreAsync(_depthEstimatorFilePath, _depthEstimatorFileMimeType ?? MimeTypes.ImagePng);
    }

    private async Task EstimateSampleDepthAsync()
        => await EstimateDepthCoreAsync(Path.Combine(app.DataDirectory, "santa.jpg"), MimeTypes.ImageJpeg);

    private async Task EstimateDepthCoreAsync(string filePath, string mimeType)
    {
        _depthEstimatorProcessing.Value = true;
        _depthEstimatorError.Value = null;
        _depthEstimatorResult.Value = null;
        _depthEstimatorImageDataUrl.Value = null;

        try
        {
            var model = Enum.Parse<DepthEstimatorModel>(_depthEstimatorModel.Value);
            using var estimator = new DepthEstimator(model);

            var data = await File.ReadAllBytesAsync(filePath);
            var result = await estimator.EstimateDepthAsync(new DepthEstimatorConfig
            {
                InputImage = new InputImage { Data = data, MimeType = mimeType }
            });

            _depthEstimatorResult.Value = $"Depth map {result.Depth.Width}x{result.Depth.Height} ({result.Depth.MimeType})";
            _depthEstimatorImageDataUrl.Value = $"data:{result.Depth.MimeType};base64,{Convert.ToBase64String(await result.Depth.GetDataAsync())}";
        }
        catch (Exception ex)
        {
            _depthEstimatorError.Value = ex.Message;
        }
        finally
        {
            _depthEstimatorProcessing.Value = false;
        }
    }

    private void RenderMeshGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-mesh-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Mesh Generator");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Model");
                    view.Select(
                        value: _meshGeneratorModel.Value,
                        options: GetModelOptions<MeshGeneratorModel>(),
                        onValueChange: async v => _meshGeneratorModel.Value = v ?? _meshGeneratorModel.Value);
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Prompt");
                    view.TextArea(
                        [Textarea.Default],
                        value: _meshGeneratorPrompt.Value,
                        onValueChange: async v => _meshGeneratorPrompt.Value = v ?? "");
                });

                view.Row([Layout.Row.InlineCenter, "flex-wrap"], content: view =>
                {
                    view.Checkbox(
                        [Checkbox.Default],
                        value: _meshGeneratorTexture.Value,
                        onValueChange: async v => _meshGeneratorTexture.Value = v,
                        label: "Texture");
                });

                RenderMeshGeneratorOptions(view);

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Input Image (optional, image-to-mesh)");

                    view.FileUpload(
                        [FileUpload.Zone.Base],
                        accept: ["image/*"],
                        multiple: false,
                        onUploadComplete: async args =>
                        {
                            if (args.LocalTempFilePath == null)
                            {
                                return;
                            }

                            _meshGeneratorInputImageData = await File.ReadAllBytesAsync(args.LocalTempFilePath);
                            _meshGeneratorInputImageMimeType = args.MimeType;
                            _meshGeneratorInputImageName.Value = args.FileName;
                        },
                        content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "image");
                                view.Text([Text.Body], "Upload input image");
                            });
                        });

                    if (!string.IsNullOrEmpty(_meshGeneratorInputImageName.Value))
                    {
                        view.Row([Layout.Row.InlineCenter, "mt-2 flex-wrap"], content: view =>
                        {
                            view.Text([Text.Caption], _meshGeneratorInputImageName.Value);
                            view.Button(
                                [Button.OutlineMd, Button.Icon],
                                onClick: async () =>
                                {
                                    _meshGeneratorInputImageData = null;
                                    _meshGeneratorInputImageMimeType = null;
                                    _meshGeneratorInputImageName.Value = "";
                                },
                                content: v => v.Icon([Icon.Default], name: "x"));
                        });
                    }
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Generate Mesh",
                        props: TestId("ai-mesh-run"),
                        disabled: _meshGeneratorProcessing.Value
                            || (string.IsNullOrWhiteSpace(_meshGeneratorPrompt.Value) && _meshGeneratorInputImageData == null),
                        onClick: GenerateMeshAsync);

                    if (_meshGeneratorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_meshGeneratorError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-mesh-error"), content: view =>
                    {
                        view.Text([Alert.Description], _meshGeneratorError.Value);
                    });
                }

                var mesh = _meshGeneratorResultData;

                if (!string.IsNullOrEmpty(_meshGeneratorResult.Value) && mesh != null)
                {
                    view.Box([Alert.Success, "mt-4"], props: TestId("ai-mesh-result"), content: view =>
                    {
                        view.Text([Alert.Title], "Result");
                        view.Text([Alert.Description], _meshGeneratorResult.Value);
                    });

                    if (!string.IsNullOrEmpty(mesh.ThumbnailUrl))
                    {
                        view.Image(["mt-4 self-start max-w-full h-auto max-h-[300px] rounded-lg border border-secondary"],
                            src: mesh.ThumbnailUrl,
                            alt: "Mesh thumbnail");
                    }

                    view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
                    {
                        RenderMeshDownloadButton(view, "GLB", mesh.GlbUrl);
                        RenderMeshDownloadButton(view, "FBX", mesh.FbxUrl);
                        RenderMeshDownloadButton(view, "OBJ", mesh.ObjUrl);
                        RenderMeshDownloadButton(view, "MTL", mesh.MtlUrl);
                        RenderMeshDownloadButton(view, "USDZ", mesh.UsdzUrl);
                        RenderMeshDownloadButton(view, "STL", mesh.StlUrl);
                    });

                    RenderMeshOperations(view, mesh);
                }
            });
        });
    }

    private static void RenderMeshDownloadButton(UIView view, string label, string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        view.Button([Button.PrimaryMd],
            href: url,
            target: "_blank",
            content: v =>
            {
                v.Icon([Icon.Default, "mr-2"], name: "download");
                v.Text(text: label);
            });
    }

    private async Task GenerateMeshAsync()
    {
        _meshGeneratorProcessing.Value = true;
        _meshGeneratorError.Value = null;
        _meshGeneratorResult.Value = null;
        _meshGeneratorResultData = null;

        try
        {
            var model = Enum.Parse<MeshGeneratorModel>(_meshGeneratorModel.Value);
            using var generator = new MeshGenerator(model);

            var config = new MeshGeneratorConfig
            {
                Prompt = _meshGeneratorPrompt.Value,
                Texture = _meshGeneratorTexture.Value
            };

            if (_meshGeneratorInputImageData != null && _meshGeneratorInputImageMimeType != null)
            {
                config.InputImages.Add(new InputImage
                {
                    Data = _meshGeneratorInputImageData,
                    MimeType = _meshGeneratorInputImageMimeType
                });
            }

            config = ApplyMeshGeneratorOptions(config);

            var result = await generator.GenerateMeshAsync(config);

            _meshGeneratorResultData = result;
            _meshRigTaskId.Value = null;
            _meshOpsResult.Value = null;
            _meshOpsError.Value = null;
            var files = result.Files.Count > 0 ? $", {result.Files.Count} file(s)" : "";
            _meshGeneratorResult.Value = result.ExpiresAt != null
                ? $"Mesh generated (links expire {result.ExpiresAt:u}{files})"
                : $"Mesh generated{files}";
        }
        catch (Exception ex)
        {
            _meshGeneratorError.Value = ex.Message;
        }
        finally
        {
            _meshGeneratorProcessing.Value = false;
        }
    }
}
