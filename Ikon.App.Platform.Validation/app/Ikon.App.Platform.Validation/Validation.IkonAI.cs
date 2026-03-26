public partial class Validation
{
    // Ikon.AI section access
    private readonly Reactive<string> _ikonAIPassword = new("");
    private readonly Reactive<bool> _ikonAIUnlocked = new(false);
    private readonly Reactive<bool> _ikonAIPasswordError = new(false);

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
    private byte[]? _imageGeneratorResultData;
    private string? _imageGeneratorResultMimeType;
    private readonly Reactive<int> _imageGeneratorSteps = new(0);
    private readonly Reactive<string> _imageGeneratorQuality = new(nameof(ImageQuality.Auto));
    private readonly Reactive<string> _imageGeneratorBackground = new(nameof(ImageBackground.Auto));
    private readonly Reactive<bool> _imageGeneratorUpsamplePrompt = new(false);
    private readonly Reactive<string> _imageGeneratorStyle = new("");
    private readonly Reactive<int> _imageGeneratorCount = new(1);
    private byte[]? _imageGeneratorInputImageData;
    private string? _imageGeneratorInputImageMimeType;
    private readonly Reactive<string> _imageGeneratorInputImageName = new("");
    private readonly Reactive<List<string>> _imageGeneratorResultDataUrls = new([]);

    // SpeechGenerator state
    private readonly Reactive<string> _speechGeneratorModel = new(nameof(SpeechGeneratorModel.Gpt4OmniMiniTts));
    private readonly Reactive<string> _speechGeneratorText = new("Hello, this is a test of the speech generation system.");
    private readonly Reactive<string> _speechGeneratorVoiceId = new("alloy");
    private readonly Reactive<IReadOnlyList<string>> _speechGeneratorVoiceIds = new(["alloy", "ash", "ballad", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse"]);
    private readonly Reactive<string> _speechGeneratorLanguage = new("en-US");
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
    private byte[]? _fileConverterResultData;
    private string? _fileConverterResultName;
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

    private static List<SelectOption> GetModelOptions<T>() where T : struct, Enum
        => Enum.GetValues<T>().Select(v => new SelectOption(v.ToString(), v.ToString())).ToList();

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

    private void RenderIkonAISection(UIView view)
    {
        if (!_ikonAIUnlocked.Value && app.GlobalState.ServerRunType != ServerRunType.Local
            && !string.IsNullOrEmpty(BuildConstants.ValidationAppPassword))
        {
            view.Column([Layout.Column.Lg], content: view =>
            {
                view.Box([Card.Default, "p-6 mb-6"], content: view =>
                {
                    view.Text([Text.H2, "mb-2"], "Ikon.AI Library");
                    view.Text([Text.Caption, "mb-4"], "Enter password to access the Ikon.AI Library section.");

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Password");
                        view.TextField(
                            [Input.Default],
                            value: _ikonAIPassword.Value,
                            type: "password",
                            onValueChange: async v =>
                            {
                                _ikonAIPassword.Value = v ?? "";
                                _ikonAIPasswordError.Value = false;
                            },
                            onSubmit: async _ =>
                            {
                                if (_ikonAIPassword.Value == BuildConstants.ValidationAppPassword)
                                {
                                    _ikonAIUnlocked.Value = true;
                                    _ikonAIPasswordError.Value = false;
                                }
                                else
                                {
                                    _ikonAIPasswordError.Value = true;
                                }
                            });
                    });

                    view.Button(
                        [Button.PrimaryMd, "mt-4"],
                        label: "Unlock",
                        disabled: string.IsNullOrEmpty(_ikonAIPassword.Value),
                        onClick: async () =>
                        {
                            if (_ikonAIPassword.Value == BuildConstants.ValidationAppPassword)
                            {
                                _ikonAIUnlocked.Value = true;
                                _ikonAIPasswordError.Value = false;
                            }
                            else
                            {
                                _ikonAIPasswordError.Value = true;
                            }
                        });

                    if (_ikonAIPasswordError.Value)
                    {
                        view.Box([Alert.Danger, "mt-4"], content: view =>
                        {
                            view.Text([Alert.Description], "Incorrect password");
                        });
                    }
                });
            });
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Ikon.AI Library");
                view.Text([Text.Caption], "Showcase of all major Ikon.AI features with interactive testing. Each card demonstrates a different AI capability with configurable model selection and inputs.");
            });

            RenderChatCard(view);
            RenderClassifierCard(view);
            RenderEmbeddingGeneratorCard(view);
            RenderFileConverterCard(view);
            RenderImageGeneratorCard(view);
            RenderOCRCard(view);
            RenderRerankerCard(view);
            RenderSoundEffectGeneratorCard(view);
            RenderSpeechGeneratorCard(view);
            RenderSpeechRecognizerCard(view);
            RenderVideoEnhancerCard(view);
            RenderVideoGeneratorCard(view);
            RenderWebScraperCard(view);
            RenderWebSearcherCard(view);
        });
    }

    private void RenderClassifierCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Classifier");
            view.Text([Text.Caption, "mb-4"], "Perform content moderation and category detection with score-level transparency");

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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Classify",
                        disabled: _classifierProcessing.Value || string.IsNullOrWhiteSpace(_classifierInput.Value),
                        onClick: ClassifyTextAsync);

                    if (_classifierProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_classifierError.Value))
                {
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _classifierError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_classifierResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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
            view.Text([Text.Caption, "mb-4"], "Create vector representations for similarity search, clustering, or semantic scoring");

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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Generate Embeddings",
                        disabled: _embeddingProcessing.Value || string.IsNullOrWhiteSpace(_embeddingInput.Value),
                        onClick: GenerateEmbeddingsAsync);

                    if (_embeddingProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_embeddingError.Value))
                {
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _embeddingError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_embeddingResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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

            var embeddings = await generator.GenerateEmbeddingsAsync([_embeddingInput.Value], type);

            if (embeddings.Count > 0)
            {
                var embedding = embeddings[0];
                var preview = string.Join(", ", embedding.Take(10).Select(v => v.ToString("F4")));
                _embeddingResult.Value = $"Vector dimension: {embedding.Length}\nFirst 10 values: [{preview}, ...]";
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
            view.Text([Text.Caption, "mb-4"], "Search the web for pages and images using various search providers");

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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Search",
                        disabled: _webSearcherProcessing.Value || string.IsNullOrWhiteSpace(_webSearcherQuery.Value),
                        onClick: SearchWebAsync);

                    if (_webSearcherProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_webSearcherError.Value))
                {
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _webSearcherError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_webSearcherResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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
            view.Text([Text.Caption, "mb-4"], "Fetch and normalize website content with options for Markdown extraction");

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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Scrape",
                        disabled: _webScraperProcessing.Value || string.IsNullOrWhiteSpace(_webScraperUrl.Value),
                        onClick: ScrapeWebAsync);

                    if (_webScraperProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_webScraperError.Value))
                {
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _webScraperError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_webScraperResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Scraped Content");
                        view.Text([Text.Body, "whitespace-pre-wrap font-mono text-sm"], _webScraperResult.Value);
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
            var scraper = new WebScraper(model, useLocalCache: true);

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
            view.Text([Text.Caption, "mb-4"], "Order candidate documents by relevance to a query for improved retrieval");

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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Rerank",
                        disabled: _rerankerProcessing.Value || string.IsNullOrWhiteSpace(_rerankerQuery.Value) || string.IsNullOrWhiteSpace(_rerankerDocuments.Value),
                        onClick: RerankDocumentsAsync);

                    if (_rerankerProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_rerankerError.Value))
                {
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _rerankerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_rerankerResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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

            var items = await reranker.RerankAsync(documents, _rerankerQuery.Value);

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
            view.Text([Text.Caption, "mb-4"], "Create images from text prompts with configurable parameters");

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

                view.Row([Layout.Row.Md], content: view =>
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

                view.Row([Layout.Row.Md], content: view =>
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

                view.Row([Layout.Row.Md], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Style (optional)");
                        view.TextField(
                            [Input.Default],
                            value: _imageGeneratorStyle.Value,
                            onValueChange: async v => _imageGeneratorStyle.Value = v ?? "");
                    });

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

                view.Row([Layout.Row.InlineCenter], content: view =>
                {
                    view.Checkbox(
                        [Checkbox.Default],
                        @checked: _imageGeneratorUpsamplePrompt.Value,
                        onCheckedChange: async v => _imageGeneratorUpsamplePrompt.Value = v);
                    view.Text([Text.Body], "Upsample Prompt");
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
                        view.Row([Layout.Row.InlineCenter, "mt-2"], content: view =>
                        {
                            view.Text([Text.Caption], _imageGeneratorInputImageName.Value);
                            view.Button(
                                [Button.GhostMd, Button.Size.Icon],
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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Generate Image",
                        disabled: _imageGeneratorProcessing.Value || string.IsNullOrWhiteSpace(_imageGeneratorPrompt.Value),
                        onClick: GenerateImageAsync);

                    if (_imageGeneratorProcessing.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }

                    if (!string.IsNullOrEmpty(_imageGeneratorDownloadUrl.Value))
                    {
                        view.Button([Button.PrimaryMd],
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _imageGeneratorError.Value);
                    });
                }

                if (_imageGeneratorResultDataUrls.Value.Count > 0)
                {
                    view.Flex(["mt-4 flex-wrap gap-4"], content: view =>
                    {
                        foreach (var dataUrl in _imageGeneratorResultDataUrls.Value)
                        {
                            view.Image(["max-w-full h-auto rounded-lg border border-secondary",
                                    _imageGeneratorResultDataUrls.Value.Count > 1 ? "max-h-[400px]" : ""],
                                src: dataUrl,
                                alt: "Generated image");
                        }
                    });
                }
            });
        });
    }

    private async Task GenerateImageAsync()
    {
        _imageGeneratorProcessing.Value = true;
        _imageGeneratorError.Value = null;
        _imageGeneratorResult.Value = null;
        _imageGeneratorDownloadUrl.Value = null;
        _imageGeneratorResultData = null;
        _imageGeneratorResultMimeType = null;
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
                Style = _imageGeneratorStyle.Value,
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
                _imageGeneratorResultData = image.Data;
                _imageGeneratorResultMimeType = image.MimeType;
                _imageGeneratorResult.Value = $"Generated {results.Count} image(s)";

                var dataUrls = new List<string>();

                foreach (var r in results)
                {
                    dataUrls.Add($"data:{r.MimeType};base64,{Convert.ToBase64String(r.Data)}");
                }

                _imageGeneratorResultDataUrls.Value = dataUrls;
                _imageGeneratorResultDataUrls.NotifyUpdate();

                var ext = image.MimeType == MimeTypes.ImagePng ? "png" : "jpg";
                _imageGeneratorDownloadUrl.Value = await UploadForDownloadAsync($"generated-image.{ext}", image.Data, image.MimeType);
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

    private void RenderSpeechGeneratorCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Speech Generator");
            view.Text([Text.Caption, "mb-4"], "Synthesize speech from text with various voices and models");

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

                view.Row([Layout.Row.Md], content: view =>
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
                        view.Text([FormField.Label], "Language");
                        view.TextField(
                            [Input.Default],
                            value: _speechGeneratorLanguage.Value,
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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Generate Speech",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _speechGeneratorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_speechGeneratorResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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
                Audio.SendSpeech(audio);
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
            view.Text([Text.Caption, "mb-4"], "Convert audio to text using speech recognition models");

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

                view.Row([Layout.Row.InlineCenter, "mb-4"], content: view =>
                {
                    view.Switch(
                        [Switch.Default],
                        @checked: isContinuous,
                        onCheckedChange: value =>
                        {
                            _speechRecognizerContinuous.Value = value;

                            var options = GetSpeechRecognizerModelOptions(value);
                            if (options.All(o => o.Value != _speechRecognizerModel.Value) && options.Count > 0)
                            {
                                _speechRecognizerModel.Value = options[0].Value;
                            }

                            return Task.CompletedTask;
                        },
                        content: view => view.SwitchThumb([Switch.Thumb]));
                    view.Text([Text.Body], "Continuous");
                });

                if (isContinuous)
                {
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.CaptureButton(
                            [_speechRecognizerRecording.Value ? Button.DangerMd : Button.PrimaryMd],
                            kind: MediaCaptureKind.Audio,
                            label: _speechRecognizerRecording.Value ? "Stop Recording" : "Start Recording",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = true,
                                NoiseSuppression = true,
                                EchoCancellation = true,
                                TargetIds = [app.ClientContext.SessionId]
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
                            label: "Recognize from Sample Audio",
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
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.CaptureButton(
                            [_speechRecognizerRecording.Value ? Button.DangerMd : Button.PrimaryMd],
                            kind: MediaCaptureKind.Audio,
                            label: _speechRecognizerRecording.Value ? "Recording..." : "Hold to Record",
                            captureMode: MediaCaptureButtonMode.Hold,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = true,
                                NoiseSuppression = true,
                                EchoCancellation = true,
                                TargetIds = [app.ClientContext.SessionId]
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
                            label: "Recognize from Sample Audio",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _speechRecognizerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_speechRecognizerResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Title], "Recognized Text");
                        view.Text([Alert.Description], _speechRecognizerResult.Value);
                    });
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

        try
        {
            var model = Enum.Parse<SpeechRecognizerModel>(_speechRecognizerModel.Value);
            _speechRecognizerInstance?.Dispose();
            _speechRecognizerInstance = new SpeechRecognizer(model);

            var text = await _speechRecognizerInstance.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Language = _speechRecognizerLanguage.Value,
                SampleRate = buffer.SampleRate,
                ChannelCount = buffer.ChannelCount,
                Samples = buffer.Samples.ToArray()
            });

            _speechRecognizerResult.Value = string.IsNullOrWhiteSpace(text) ? "(No speech detected)" : text;
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

    private void StartContinuousRecognition()
    {
        _speechRecognizerChannel = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        _speechRecognizerCts = new CancellationTokenSource();
        _speechRecognizerResult.Value = null;
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

            await foreach (var text in recognizer.RecognizeContinuousSpeechAsync(config, _speechRecognizerChannel!.Reader.ReadAllAsync(cancellationToken), cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(_speechRecognizerResult.Value))
                {
                    _speechRecognizerResult.Value = text;
                }
                else
                {
                    _speechRecognizerResult.Value += " " + text;
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

        try
        {
            var audioPath = Path.Combine(app.DataDirectory, "audio.raw");
            var rawBytes = await File.ReadAllBytesAsync(audioPath);
            var samples = AudioUtils.ConvertPcm16ToFloat(rawBytes);

            var model = Enum.Parse<SpeechRecognizerModel>(_speechRecognizerModel.Value);
            _speechRecognizerInstance?.Dispose();
            _speechRecognizerInstance = new SpeechRecognizer(model);

            var text = await _speechRecognizerInstance.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Language = _speechRecognizerLanguage.Value,
                SampleRate = 16000,
                ChannelCount = 1,
                Samples = samples
            });

            _speechRecognizerResult.Value = string.IsNullOrWhiteSpace(text) ? "(No speech detected)" : text;
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
            view.Text([Text.Caption, "mb-4"], "Extract text from images or PDFs using optical character recognition");

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

                view.Row([Layout.Row.Md, "mt-4 items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Extract from Upload",
                        disabled: _ocrProcessing.Value || string.IsNullOrEmpty(_ocrFilePath),
                        onClick: PerformOCRAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        label: "Extract from Sample PDF",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _ocrError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_ocrResult.Value))
                {
                    view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], content: view =>
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
            view.Text([Text.Caption, "mb-4"], "Convert documents to PDF format");

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
                        _fileConverterResultData = null;
                        _fileConverterResultName = null;
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

                view.Row([Layout.Row.Md, "mt-4 items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Convert from Upload",
                        disabled: _fileConverterProcessing.Value || string.IsNullOrEmpty(_fileConverterFilePath),
                        onClick: ConvertFileAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        label: "Convert from Sample PPTX",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _fileConverterError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_fileConverterResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Title], "Conversion Complete");
                        view.Text([Alert.Description], _fileConverterResult.Value);
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

            _fileConverterResultData = result.Data;
            _fileConverterResultName = result.Name;
            _fileConverterResult.Value = $"Converted to {result.Name} ({result.Data.Length / 1024} KB)";
            _fileConverterDownloadUrl.Value = await UploadForDownloadAsync("converted.pdf", result.Data, MimeTypes.ApplicationPdf);
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
            view.Text([Text.Caption, "mb-4"], "Generate video clips from text prompts");

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

                view.Row([Layout.Row.Md], content: view =>
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
                    view.Row([Layout.Row.InlineCenter], content: view =>
                    {
                        view.Checkbox(
                            [Checkbox.Default],
                            @checked: _videoGeneratorGenerateAudio.Value,
                            onCheckedChange: async v => _videoGeneratorGenerateAudio.Value = v);
                        view.Text([Text.Body], "Generate Audio");
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
                            view.Row([Layout.Row.InlineCenter, "mt-2"], content: view =>
                            {
                                view.Text([Text.Caption], _videoGeneratorInputImageName.Value);
                                view.Button(
                                    [Button.GhostMd, Button.Size.Icon],
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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Generate Video",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _videoGeneratorError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoGeneratorResultUrl.Value))
                {
                    view.Box([Media.VideoContainer, "mt-4"], content: view =>
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
                config.Seed = _videoGeneratorSeed.Value;
            }

            if (_videoGeneratorGenerateAudio.Value)
            {
                config.GenerateAudio = true;
            }

            if (_videoGeneratorInputImageData != null && _videoGeneratorInputImageMimeType != null)
            {
                config.InputImages.Add(new VideoGeneratorConfig.InputImage
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
            view.Text([Text.Caption, "mb-4"], "Generate sound effects from text descriptions");

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

                view.Row([Layout.Row.Md], content: view =>
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
                    view.Row([Layout.Row.InlineCenter], content: view =>
                    {
                        view.Checkbox(
                            [Checkbox.Default],
                            @checked: _soundEffectLoop.Value,
                            onCheckedChange: async v => _soundEffectLoop.Value = v);
                        view.Text([Text.Body], "Loop");
                    });
                }

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Generate Sound Effect",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _soundEffectError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_soundEffectResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Title], "Sound Effect Generated");
                        view.Text([Alert.Description], _soundEffectResult.Value);
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

            var allSamples = new List<float>();

            await foreach (var audio in generator.GenerateSoundEffectAsync(new SoundEffectGeneratorConfig
            {
                Prompt = _soundEffectPrompt.Value,
                DurationSeconds = _soundEffectDuration.Value,
                PromptInfluence = _soundEffectPromptInfluence.Value,
                Loop = _soundEffectLoop.Value
            }))
            {
                Audio.SendSpeech(audio);
                allSamples.AddRange(audio.Samples);
            }

            var durationSeconds = (float)allSamples.Count / generator.SampleRate / generator.ChannelCount;
            _soundEffectResult.Value = $"Generated {durationSeconds:F1}s audio";

            if (allSamples.Count > 0)
            {
                var wav = new WavFile(generator.SampleRate, generator.ChannelCount, WavFile.SampleFormat.Float);
                wav.AddSamples(allSamples.ToArray());
                _soundEffectDownloadUrl.Value = await UploadForDownloadAsync("sound-effect.wav", wav.AsArray(), MimeTypes.AudioXWav);
            }
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
            view.Text([Text.Caption, "mb-4"], "Enhance videos with upscaling or FPS boosting");

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

                view.Row([Layout.Row.Md], content: view =>
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

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: "Enhance Video",
                        disabled: _videoEnhancerProcessing.Value || string.IsNullOrWhiteSpace(_videoEnhancerVideoUrl.Value),
                        onClick: EnhanceVideoAsync);

                    view.Button(
                        [Button.PrimaryMd],
                        label: "Enhance Sample Video",
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
                    view.Box([Alert.Danger, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _videoEnhancerError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_videoEnhancerResultInfo.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
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
        var config = new VideoEnhancerConfig();

        if (_videoEnhancerTargetFps.Value > 0)
        {
            config.TargetFps = _videoEnhancerTargetFps.Value;
        }

        if (_videoEnhancerStartFrame.Value > 0)
        {
            config.StartFrame = _videoEnhancerStartFrame.Value;
        }

        if (_videoEnhancerEndFrame.Value > 0)
        {
            config.EndFrame = _videoEnhancerEndFrame.Value;
        }

        return config;
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

            var config = BuildVideoEnhancerConfig();
            config.VideoUrl = _videoEnhancerVideoUrl.Value;

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

            var config = BuildVideoEnhancerConfig();
            config.VideoData = videoBytes;
            config.MimeType = "video/mp4";

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
}
