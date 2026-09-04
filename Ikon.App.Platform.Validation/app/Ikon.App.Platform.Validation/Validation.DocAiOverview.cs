using Ikon.AI.DepthEstimation;
using Ikon.AI.ImageSegmentation;
using Ikon.AI.ImageUpscaling;
using Ikon.AI.MeshGeneration;

// Generated holder for the fences of ikon-ai-library-overview.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
#region docsnippet:ai-object-generation-2
public class PersonDetails
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
#endregion

file static class DocAiOverview
{
    // The names the guide's prose hands the reader before the block that uses them. Each is TYPED:
    // an `object` here would make every block pass and check nothing.
    private static readonly byte[] imageBytes = [];
    private static readonly float[] samples = [];
    private static readonly string query = "";
    private static readonly List<string> documents = [];
    private static readonly SpeechRecognizer speechRecognizer = new(SpeechRecognizerModel.WhisperLarge3Turbo);

    public static async Task AiObjectGeneration()
    {
        #region docsnippet:ai-object-generation
        var context = new KernelContext();
        context = context.Add(new MessageBlock(MessageBlockRole.User, "Tell me about John Smith."));

        var (result, _) = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, context, pass =>
        {
            pass.Command = "Return invented personal details about the person the user asked about.";
        }).FinalAsync();

        Log.Instance.Info($"Result: {Json.To(result)}");
        #endregion
    }




    public static async Task AiImagegeneration()
    {
        #region docsnippet:ai-imagegeneration
        var image = await ImageGenerator.GenerateAsync("A santa dancing in the snow");
        await File.WriteAllBytesAsync("santa.png", await image.GetDataAsync());
        #endregion
    }

    public static async Task AiImagegeneration2()
    {
        #region docsnippet:ai-imagegeneration-2
        using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);

        var result = (await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
        {
            Prompt = "A santa dancing in the snow",
            NegativePrompt = "summer",
            Width = 1024,
            Height = 1024,
            Seed = 42
        })).First();

        await File.WriteAllBytesAsync("santa.png", await result.GetDataAsync());
        #endregion
    }

    public static async Task AiImagesegmentation()
    {
        #region docsnippet:ai-imagesegmentation
        var result = await ImageSegmenter.SegmentAsync(imageBytes, "image/png", "person");
        await File.WriteAllBytesAsync("mask.png", await result.Segments[0].Mask.GetDataAsync());
        #endregion
    }

    public static async Task AiImagesegmentation2()
    {
        #region docsnippet:ai-imagesegmentation-2
        using var segmenter = new ImageSegmenter(ImageSegmenterModel.Sam3);

        var result = await segmenter.SegmentImageAsync(new ImageSegmenterConfig
        {
            InputImage = new InputImage { Url = "https://example.com/photo.png" },
            Prompt = "person",
            ReturnMultipleMasks = true
        });

        foreach (var segment in result.Segments)
        {
            Log.Instance.Info($"Found segment with score {segment.Score}");
        }

        await File.WriteAllBytesAsync("mask.png", await result.Segments[0].Mask.GetDataAsync());
        #endregion
    }

    public static async Task AiImageupscaling()
    {
        #region docsnippet:ai-imageupscaling
        var result = await ImageUpscaler.UpscaleAsync(imageBytes, "image/png", scaleFactor: 4);
        await File.WriteAllBytesAsync("upscaled.png", await result.Image.GetDataAsync());
        #endregion
    }

    public static async Task AiImageupscaling2()
    {
        #region docsnippet:ai-imageupscaling-2
        using var imageUpscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);

        var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
        {
            InputImage = new InputImage { Url = "https://example.com/photo.png" },
            TargetResolution = UpscaleTargetResolution.Uhd2160
        });

        await File.WriteAllBytesAsync("upscaled.png", await result.Image.GetDataAsync());
        #endregion
    }

    public static async Task AiDepthestimation()
    {
        #region docsnippet:ai-depthestimation
        var result = await DepthEstimator.EstimateAsync(imageBytes, "image/png");
        await File.WriteAllBytesAsync("depth.png", await result.Depth.GetDataAsync());
        #endregion
    }

    public static async Task AiDepthestimation2()
    {
        #region docsnippet:ai-depthestimation-2
        using var depthEstimator = new DepthEstimator(DepthEstimatorModel.DepthAnythingV2);

        var result = await depthEstimator.EstimateDepthAsync(new DepthEstimatorConfig
        {
            InputImage = new InputImage { Url = "https://example.com/photo.png" }
        });

        await File.WriteAllBytesAsync("depth.png", await result.Depth.GetDataAsync());
        #endregion
    }

    public static async Task AiMeshgeneration()
    {
        #region docsnippet:ai-meshgeneration
        var mesh = await MeshGenerator.GenerateAsync("A small wooden treasure chest with brass fittings");
        Log.Instance.Info($"GLB URL: {mesh.GlbUrl}");
        #endregion
    }

    public static async Task AiMeshgeneration2()
    {
        #region docsnippet:ai-meshgeneration-2
        using var meshGenerator = new MeshGenerator(MeshGeneratorModel.Meshy6);

        var result = await meshGenerator.GenerateMeshAsync(new MeshGeneratorConfig
        {
            Prompt = "A small wooden treasure chest with brass fittings",
            TargetPolycount = 20000
        });

        Log.Instance.Info($"GLB URL: {result.GlbUrl}");
        #endregion
    }

    public static async Task AiVideogeneration()
    {
        #region docsnippet:ai-videogeneration
        var video = await VideoGenerator.GenerateAsync("A santa dancing in the snow");
        Log.Instance.Info($"Video URL: {video.Url}");
        #endregion
    }

    public static async Task AiVideogeneration2()
    {
        #region docsnippet:ai-videogeneration-2
        using var generator = new VideoGenerator(VideoGeneratorModel.Pollo20);

        var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
        {
            Prompt = "A santa dancing in the snow",
            Resolution = VideoGeneratorResolution.Resolution1080p,
            AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
            Length = 5
        });

        Log.Instance.Info($"Video URL: {result.Url}");
        #endregion
    }

    public static async Task AiVideoenhancement()
    {
        #region docsnippet:ai-videoenhancement
        var enhanced = await VideoEnhancer.EnhanceAsync("https://example.com/input.mp4");
        Log.Instance.Info($"Enhanced video URL: {enhanced.Url}");
        #endregion
    }

    public static async Task AiVideoenhancement2()
    {
        #region docsnippet:ai-videoenhancement-2
        using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale4xUltra4);

        var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
        {
            Url = "https://example.com/input.mp4"
        });

        Log.Instance.Info($"Enhanced video URL: {result.Url}");
        #endregion
    }

    public static async Task AiSpeechgeneration()
    {
        #region docsnippet:ai-speechgeneration
        var audio = await SpeechGenerator.GenerateAsync("There once was a ship that put to sea.");

        using var wavFile = new WavFile(audio.SampleRate, audio.ChannelCount, WavFile.SampleFormat.Float);
        wavFile.AddSamples(audio.Samples);
        wavFile.SaveToFile("speech.wav");
        #endregion
    }

    public static async Task AiSpeechgeneration2()
    {
        #region docsnippet:ai-speechgeneration-2
        using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.Gpt4OmniMiniTts);

        foreach (var voiceId in speechGenerator.VoiceIds)
        {
            Log.Instance.Info($"Voice ID: {voiceId}");
        }

        List<float> samples = [];

        var config = new SpeechGeneratorConfig
        {
            VoiceId = "ballad",
            Language = "en-US",
            Instructions = "Speak like a angry pirate.",
            Text = "There once was a ship that put to sea. The name of that ship was a Billy of Tea."
        };

        await foreach (var audio in speechGenerator.GenerateSpeechAsync(config))
        {
            samples.AddRange(audio.Samples);
        }

        using var wavFile = new WavFile(speechGenerator.SampleRate, speechGenerator.ChannelCount, WavFile.SampleFormat.Float);
        wavFile.AddSamples(samples.ToArray());
        wavFile.SaveToFile("speech.wav");
        #endregion
    }

    public static async Task AiSpeechrecognition()
    {
        #region docsnippet:ai-speechrecognition
        string text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
        #endregion
    }

    public static async Task AiSpeechrecognition2()
    {
        #region docsnippet:ai-speechrecognition-2
        var speechRecognizer = new SpeechRecognizer(SpeechRecognizerModel.Whisper2);

        var audioBytes = await File.ReadAllBytesAsync("audio.raw");

        var transcript = await speechRecognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
        {
            Language = "en-US",
            SampleRate = 16000,
            ChannelCount = 1,
            Samples = AudioUtils.ConvertPcm16ToFloat(audioBytes)
        });

        Log.Instance.Info($"Recognized speech: '{transcript.Text}'");
        #endregion
    }

    public static async Task AiTimestampsSpeakersAndConfidence()
    {
        #region docsnippet:ai-timestamps-speakers-and-confidence
        var transcript = await speechRecognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
        {
            SampleRate = 16000,
            ChannelCount = 1,
            Samples = samples,
            Timestamps = SpeechTimestamps.Word | SpeechTimestamps.Segment,
            Diarize = true
        });

        foreach (var word in transcript.Words)
        {
            Log.Instance.Info($"[{word.Start.TotalSeconds:F2}-{word.End.TotalSeconds:F2}] {word.Text} ({word.Speaker})");
        }
        #endregion
    }

    public static async Task AiTimestampsSpeakersAndConfidence2(IAsyncEnumerable<float[]> samples)
    {
        #region docsnippet:ai-timestamps-speakers-and-confidence-2
        var config = new RecognizeContinuousSpeechConfig
        {
            SampleRate = 16000,
            ChannelCount = 1,
            Timestamps = SpeechTimestamps.Word,
            InterimResults = true
        };

        await foreach (var transcriptEvent in speechRecognizer.RecognizeContinuousSpeechAsync(config, samples))
        {
            if (transcriptEvent.IsFinal)
            {
                Log.Instance.Info($"[{transcriptEvent.Start.TotalSeconds:F2}] {transcriptEvent.Text}");
            }
        }
        #endregion
    }

    public static async Task AiSoundeffectgeneration()
    {
        #region docsnippet:ai-soundeffectgeneration
        var effect = await SoundEffectGenerator.GenerateAsync("A thunderstorm with heavy rain");
        await File.WriteAllBytesAsync("thunder.wav", await effect.GetDataAsync());
        #endregion
    }

    public static async Task AiSoundeffectgeneration2()
    {
        #region docsnippet:ai-soundeffectgeneration-2
        using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);

        var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig
        {
            Prompt = "A thunderstorm with heavy rain",
            DurationSeconds = 5.0
        });

        await File.WriteAllBytesAsync("thunder.wav", await result.GetDataAsync());
        #endregion
    }

    public static async Task AiMusicgeneration()
    {
        #region docsnippet:ai-musicgeneration
        var music = await MusicGenerator.GenerateAsync("An upbeat 8-bit chiptune loop");
        await File.WriteAllBytesAsync("music.mp3", await music.GetDataAsync());
        #endregion
    }

    public static async Task AiMusicgeneration2()
    {
        #region docsnippet:ai-musicgeneration-2
        using var musicGenerator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);

        var result = await musicGenerator.GenerateMusicFileAsync(new MusicGeneratorConfig
        {
            Prompt = "An upbeat 8-bit chiptune loop",
            DurationSeconds = 10
        });

        await File.WriteAllBytesAsync("music.mp3", await result.GetDataAsync());
        #endregion
    }

    public static async Task AiWebscraping()
    {
        #region docsnippet:ai-webscraping
        var page = await WebScraper.ScrapeAsync("https://example.com");
        Log.Instance.Info($"{page.Title}: {page.Content}...");
        #endregion
    }

    public static async Task AiWebscraping2()
    {
        #region docsnippet:ai-webscraping-2
        var scraper = new WebScraper(WebScraperModel.Jina);

        var page = await scraper.ScrapeSinglePageAsync(new SinglePageScrapeConfig
        {
            Url = "https://example.com",
            OutputFormat = WebScraperOutputFormat.Markdown
        });

        Log.Instance.Info($"{page.Title}: {page.Content}...");

        var screenshot = await scraper.TakeScreenshotAsync(new ScreenshotConfig
        {
            Url = "https://example.com",
            Width = 800,
            Height = 600
        });

        await File.WriteAllBytesAsync("screenshot.png", screenshot.Data);
        #endregion
    }

    public static async Task AiWebsearching()
    {
        #region docsnippet:ai-websearching
        var results = await WebSearcher.SearchAsync("Finnish ice hockey teams", maxResults: 5);

        foreach (var result in results)
        {
            Log.Instance.Info($"{result.Title}: {result.Url}");
        }
        #endregion
    }

    public static async Task AiWebsearching2()
    {
        #region docsnippet:ai-websearching-2
        var pageSearcher = new WebSearcher(WebSearcherModel.Google);

        var pageResults = await pageSearcher.SearchPagesAsync(new SearchConfig
        {
            Query = "Finnish ice hockey teams",
            MaxResults = 5
        });

        foreach (var result in pageResults)
        {
            Log.Instance.Info($"{result.Title}: {result.Url}");
        }

        var imageSearcher = new WebSearcher(WebSearcherModel.GoogleImages);

        var imageResults = await imageSearcher.SearchImagesAsync(new SearchConfig
        {
            Query = "Coffee beans",
            MaxResults = 5
        });

        foreach (var result in imageResults)
        {
            Log.Instance.Info($"{result.Title}: {result.Url}");
        }
        #endregion
    }

    public static async Task AiFileconversion()
    {
        #region docsnippet:ai-fileconversion
        var pdf = await FileConverter.ConvertToPdfAsync(await File.ReadAllBytesAsync("brochure.docx"), "brochure.docx");
        await File.WriteAllBytesAsync("brochure.pdf", await pdf.GetDataAsync());
        #endregion
    }

    public static async Task AiFileconversion2()
    {
        #region docsnippet:ai-fileconversion-2
        var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
        var convertedFile = await fileConverter.ConvertToPdfAsync(new FileConverterConfig
        {
            Url = "https://example.com/brochure.docx",
            FileName = "brochure.docx"
        });
        await File.WriteAllBytesAsync("brochure.pdf", await convertedFile.GetDataAsync());
        #endregion
    }

    public static async Task AiOcr()
    {
        #region docsnippet:ai-ocr
        var result = await OCR.AnalyzeAsync(await File.ReadAllBytesAsync("invoice.pdf"));
        Log.Instance.Info(result.Text);
        #endregion
    }

    public static async Task AiOcr2()
    {
        #region docsnippet:ai-ocr-2
        var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
        var result = await ocr.AnalyzeDocumentAsync(new OCRConfig
        {
            Url = "https://example.com/invoice.pdf",
            Pages = "1-5",
            IncludeWords = true
        });

        Log.Instance.Info(result.Text);
        #endregion
    }

    public static async Task AiReranking()
    {
        #region docsnippet:ai-reranking
        var items = await Reranker.RerankAsync(
            ["Document about AI", "Document about cooking", "Document about space exploration"],
            query: "What is the latest in artificial intelligence?"
        );

        foreach (var item in items)
        {
            Log.Instance.Info($"Index: {item.Index}, Score: {item.Score}");
        }
        #endregion
    }

    public static async Task AiReranking2()
    {
        #region docsnippet:ai-reranking-2
        using var reranker = new Reranker(RerankModel.CohereRerank4Fast);
        var items = await reranker.RerankAsync(new RerankerConfig { Documents = documents, Query = query, TopN = 5 });
        #endregion
    }

    public static async Task AiClassification()
    {
        #region docsnippet:ai-classification
        var result = await Classifier.ClassifyAsync("How to kill kittens? (not really!)");
        Log.Instance.Info($"Flagged: {result.IsFlagged}");

        foreach (var detail in result.Details)
        {
            if (detail.IsFlagged)
            {
                Log.Instance.Info($"{detail.Label} ({detail.OriginalCategory}): {detail.Score}");
            }
        }
        #endregion
    }

    public static async Task AiClassification2()
    {
        #region docsnippet:ai-classification-2
        using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);

        var result = await classifier.ClassifyAsync("What a nice weather!");
        Log.Instance.Info($"Flagged: {result.IsFlagged}");
        #endregion
    }

    public static async Task AiEmbeddings()
    {
        #region docsnippet:ai-embeddings
        var embeddings = await EmbeddingGenerator.EmbedAsync(
            ["Example sentence 1", "Example sentence 2", "Example sentence 3"]);

        foreach (var embedding in embeddings)
        {
            Log.Instance.Info($"Embedding length: {embedding.Length}");
        }
        #endregion
    }

    public static async Task AiEmbeddings2()
    {
        #region docsnippet:ai-embeddings-2
        using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);

        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(new EmbeddingGeneratorConfig
        {
            Inputs = ["Example sentence 1", "Example sentence 2", "Example sentence 3"],
            Type = EmbeddingType.Document
        });
        #endregion
    }

    public static async Task AiAttachingMediaFromTheAssetSystem()
    {
        #region docsnippet:ai-attaching-media-from-the-asset-system
        var assetUri = new AssetUri("assets://space/abc123/cloud-file/clips/demo.mp4");

        var context = new KernelContext();
        context = context.Add(new MessageBlock(MessageBlockRole.User, new IMessagePart[]
        {
            new TextPart("Describe what happens in this clip."),
            new VideoAssetPart(assetUri),
        }));
        #endregion
    }
}
