# Ikon.AI Library Overview

This guide summarizes the principal namespaces in the Ikon.AI .NET library for developers building AI-enabled solutions. Each section outlines module responsibilities, supported models, and usage patterns verified by automated tests.

## Emergence

`Ikon.AI.Emergence` is the recommended way to build AI workflows with typed outputs. It provides a streaming-first, C#-idiomatic API for structured object generation, tool calling, and advanced multi-agent patterns. All APIs return `IAsyncEnumerable<EmergeEvent<T>>` and non-streaming usage is achieved via the `.FinalAsync()` extension method. Emergence can target any model listed in the [LLM](#llm) section. See the [Emergence Guide](emergence-guide.md) for the full documentation.

### Object Generation

```csharp
using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;
using Ikon.Common.Core;

var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, "Tell me about John Smith."));

var (result, _) = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, context, pass =>
{
    pass.Command = "Return invented personal details about the person the user asked about.";
}).FinalAsync();

Log.Instance.Info($"Result: {Json.To(result)}");

public class PersonDetails
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

Emergence supports multi-agent patterns: `BestOf`, `Refine`, `MapReduce`, `TreeSearch`, and `EnsembleMerge`. See the [Emergence Guide](emergence-guide.md) for full documentation on all patterns.

Region support is available via `pass.Regions`:

```csharp
pass.Regions = [ModelRegion.Eu, ModelRegion.Global];
```

## Shaders

> **Note:** For new development, prefer using [Emergence](#emergence) which provides a simpler, code-first API for structured AI outputs.

`Ikon.AI.Shader` provides declarative orchestration for prompt-driven automation. Shaders encapsulate model selection, context policies, and schema expectations while allowing reuse across applications. Shaders can target any model listed in the [LLM](#llm) section.

### Text Generation

Generate structured text using a shader definition stored in code, files, or embedded resources.

```csharp
using Ikon.AI.Kernel;
using Ikon.AI.Shader;
using Ikon.Common.Core;

string shaderSource = @"
{
  ShaderVersion: 2,
  Model: {
    Name: 'Gpt5Mini',
    RequestTimeoutSeconds: 60,
    MaxOutputTokens: 4000,
    ReasoningEffort: 'Medium',
  },
  History: {
    Max: 10,
  },
  Input: {
    AssistantName: 'IkonBot',
  },
  Intents: [
    {
      Id: 'ExampleIntent',
      Passes: [
        {
          Id: 'ExamplePass',
          Context: 'You are a helpful assistant. Your name is {{ AssistantName }}.',
          Command: 'Please answer the user question.',
        }
      ]
    }
  ]
}";

var shader = new Shader.Shader(shaderSource);
var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, "Hello! What is your name?"));

var stringResult = await shader.GenerateStringAsync(context);
Log.Instance.Info($"Shader string result: {stringResult}");
```

### Object Generation

Emit strongly typed results when the shader is configured for JSON output.

```csharp
using Ikon.AI.Shader;
using Ikon.Common.Core;

string shaderSource = @"
{
  ShaderVersion: 2,
  Model: {
    Name: 'Gpt5Mini',
    RequestTimeoutSeconds: 60,
    MaxOutputTokens: 4000,
    ReasoningEffort: 'Medium',
    LogRenderedShader: true,
    UseJson: true,
  },
  History: {
    Max: 10,
  },
  Input: {
    RequestedName: null,
  },
  Intents: [
    {
      Id: 'ExampleIntent',
      Passes: [
        {
          Id: 'ExamplePass',
          Command: 'Return a JSON object with invented personal details about {{ RequestedName }}. Please give the output in JSON format like this:\n{{ ImplicitJsonExample }}',
        }
      ]
    }
  ]
}";

var shader = new Shader.Shader(shaderSource);
var state = new Dictionary<string, object?>
{
    ["RequestedName"] = "John Smith"
};

var result = await shader.GenerateObjectAsync<ExampleResponse>(state: state);
Log.Instance.Info($"Shader object result: {Json.To(result)}");

private class ExampleResponse
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

### Implicit Shaders

Implicit shaders load their source from embedded resources that share the class name. Save the shader used in `ShaderObjectExampleTest` as `<ClassName>.shader` alongside the corresponding `<ClassName>.cs` file, set the build action to **Embedded Resource**, and access it through `ShaderCache`.

```csharp
var result = await ShaderCache.Instance.GetImplicitShader().GenerateObjectAsync<ExampleResponse>(
    contexts: null,
    cancellationToken: CancellationToken.None,
    ("RequestedName", "John Smith")
);

Log.Instance.Info($"Implicit shader object result: {Json.To(result)}");
```

## LLM

> **Note:** For most use cases, prefer using [Emergence](#emergence) which provides structured outputs and higher-level patterns on top of the LLM layer.

`Ikon.AI.LLM` offers direct, streaming-level access to language models when higher-level orchestration is unnecessary.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

Pass preferred regions as an ordered list to keep inference within a geography. If omitted, the default region is `Global`.

```csharp
using Ikon.AI;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;
using Ikon.Common.Core;

var context = new KernelContext();
context = context.Add(new Instruction(InstructionType.Context, "You are a helpful assistant that helps to summarize product release notes."));
context = context.Add(new MessageBlock(MessageBlockRole.User, "Summarise the latest release highlights. Here are the notes: ..."));

using var llm = new LLM.LLM(LLMModel.Gpt5Mini, regions: [ModelRegion.Eu]);

await foreach (var llmEvent in llm.GenerateAsync(context))
{
    Log.Instance.Info($"{llmEvent.Source} | {llmEvent}");
}

var stringResult = await llm.GenerateAsync(context).AsStringAsync();
Log.Instance.Info($"String result: {stringResult}");
```

## Custom Model Endpoints

Run your own model — a self-hosted LLM behind vLLM, Ollama, TGI, or any endpoint speaking a supported provider API — and use it through the normal Ikon.AI APIs. Register the endpoint with `CustomModels` at app startup, then select the model by its registered name anywhere a model name string is accepted:

```csharp
using Ikon.AI;
using Ikon.AI.Emergence;
using Ikon.AI.LLM;

CustomModels.Instance.Register(new CustomLLMModel
{
    Name = "my-model",
    EndpointUrl = "http://gpu-box:8000/v1/chat/completions",
    Api = CustomLLMApi.OpenAICompletions,
    ApiModelName = "Qwen/Qwen2.5-32B-Instruct",
    ApiKey = "sk-local-123",           // omit for keyless endpoints (e.g. local Ollama)
    ContextWindowSize = 32768,
    SupportsJsonSchema = true,
});

var reply = await Emerge.AskAsync("Hello", "my-model");
using var llm = new LLM.LLM("my-model");
```

Custom models are supported for LLMs (`CustomLLMModel`), embeddings (`CustomEmbeddingModel`), reranking (`CustomRerankModel`), and classification (`CustomClassificationModel`); each picks one of the category's existing HTTP request formats via its `Api` enum. Other categories (image generation, speech) are not yet supported.

Key behaviors:

- **Always in-process.** Calls to custom models execute locally with your API key and never go through the Ikon RPC mechanism — the platform never sees your endpoint or key.
- **Flat per-request billing.** Usage is reported with a `.user` suffix and charged as a flat credit fee per successful request (identical for all custom models) instead of per-token provider pricing. Token counts are still reported for analytics. A failed or aborted request is not billed; each successful retry bills its own request.
- **Register at startup.** The registry is async-local (like credentials): register models on the main flow before spawning parallel work so every flow sees them. Registering the same name again replaces the previous registration.
- **Names.** A custom model name must not collide with a built-in model name and must not contain dots or whitespace.

## ImageGeneration

`Ikon.AI.ImageGeneration.ImageGenerator` creates images with negative prompts, seeding, and resolution controls.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `Gemini25FlashImage` (cheap+fast); the result is never null (throws `ImageGeneratorException` on failure):

```csharp
using Ikon.AI.ImageGeneration;

var image = await ImageGenerator.GenerateAsync("A santa dancing in the snow");
await File.WriteAllBytesAsync("santa.png", await image.GetDataAsync());
```

**Result delivery:** media results (image, music, sound effect, converted file, segmentation mask, depth map) carry a `Kind` field. By default they arrive inline (`Kind == ResultKind.Data`, `Data` non-null). When a result is returned from a remotely hosted AI function and its payload exceeds a few MB, it is automatically uploaded and arrives as a signed download URL valid for roughly one hour (`Kind == ResultKind.Url`, `Url` non-null, `Data` null) to stay within the protocol's message size limit; consumed locally in-process, large payloads stay inline. `await result.GetDataAsync()` returns the bytes either way, so prefer it over reading `Data` directly. Set `ResultDelivery = ResultDelivery.Url` in the config to always receive a URL.

Use the constructor + config form for negative prompts, resolution, seeding, batches, or input images:

```csharp
using Ikon.AI.ImageGeneration;

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
```

## ImageSegmentation

`Ikon.AI.ImageSegmentation.ImageSegmenter` segments objects from images using text, point, or box prompts (Segment Anything models). The result contains one mask image per detected object, along with confidence scores and normalized bounding boxes.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot from image bytes — defaults to `Sam31` (latest SAM revision):

```csharp
using Ikon.AI.ImageSegmentation;

var result = await ImageSegmenter.SegmentAsync(imageBytes, "image/png", "person");
await File.WriteAllBytesAsync("mask.png", await result.Segments[0].Mask.GetDataAsync());
```

Use the constructor + config form for URL input, point/box prompts, or multiple masks:

```csharp
using Ikon.AI.ImageSegmentation;

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
```

## DepthEstimation

`Ikon.AI.DepthEstimation.DepthEstimator` produces a monocular depth map from a single input image. The result is one depth-map image (closer surfaces brighter, farther surfaces darker). Useful for 3D reconstruction, parallax/relighting effects, compositing, and depth-conditioned image generation.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps). Depth Anything V2 is the fast default; Marigold is diffusion-based and higher quality but slower; MiDaS is a lightweight classic.

One-shot from image bytes — defaults to `DepthAnythingV2` (cheap+fast):

```csharp
using Ikon.AI.DepthEstimation;

var result = await DepthEstimator.EstimateAsync(imageBytes, "image/png");
await File.WriteAllBytesAsync("depth.png", await result.Depth.GetDataAsync());
```

Use the constructor + config form for URL input or the Marigold tuning fields:

```csharp
using Ikon.AI.DepthEstimation;

using var depthEstimator = new DepthEstimator(DepthEstimatorModel.DepthAnythingV2);

var result = await depthEstimator.EstimateDepthAsync(new DepthEstimatorConfig
{
    InputImage = new InputImage { Url = "https://example.com/photo.png" }
});

await File.WriteAllBytesAsync("depth.png", await result.Depth.GetDataAsync());
```

## MeshGeneration

`Ikon.AI.MeshGeneration.MeshGenerator` creates textured 3D meshes from a text prompt (no input images), a single image, or 2-4 images of the same object. The result contains URLs for the generated model in multiple formats (GLB, FBX, OBJ, USDZ). The URLs are signed and expire roughly three days after generation, so download the files promptly.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot text-to-mesh — defaults to `Meshy6` (the current Meshy generation):

```csharp
using Ikon.AI.MeshGeneration;

var mesh = await MeshGenerator.GenerateAsync("A small wooden treasure chest with brass fittings");
Log.Instance.Info($"GLB URL: {mesh.GlbUrl}");
```

Use the constructor + config form for image-to-mesh, PBR textures, or polycount/topology control:

```csharp
using Ikon.AI.MeshGeneration;

using var meshGenerator = new MeshGenerator(MeshGeneratorModel.Meshy6);

var result = await meshGenerator.GenerateMeshAsync(new MeshGeneratorConfig
{
    Prompt = "A small wooden treasure chest with brass fittings",
    TargetPolycount = 20000
});

Log.Instance.Info($"GLB URL: {result.GlbUrl}");
```

## VideoGeneration

`Ikon.AI.VideoGeneration.VideoGenerator` renders video clips with configurable length, resolution, and aspect ratio.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot text-to-video — defaults to `Veo31Fast` (cheap+fast):

```csharp
using Ikon.AI.VideoGeneration;

var video = await VideoGenerator.GenerateAsync("A santa dancing in the snow");
Log.Instance.Info($"Video URL: {video.Url}");
```

Use the constructor + config form for input images (image-to-video), length, resolution, or aspect ratio:

```csharp
using Ikon.AI.VideoGeneration;

using var generator = new VideoGenerator(VideoGeneratorModel.Pollo20);

var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
{
    Prompt = "A santa dancing in the snow",
    Resolution = VideoGeneratorResolution.Resolution1080p,
    AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
    Length = 5
});

Log.Instance.Info($"Video URL: {result.Url}");
```

## VideoEnhancement

`Ikon.AI.VideoEnhancement.VideoEnhancer` upscales and frame-interpolates existing video clips.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `TensorPixUpscale2xUltra41` (the current 2x upscale generation):

```csharp
using Ikon.AI.VideoEnhancement;

var enhanced = await VideoEnhancer.EnhanceAsync("https://example.com/input.mp4");
Log.Instance.Info($"Enhanced video URL: {enhanced.Url}");
```

Use the constructor + config form for raw video bytes, frame ranges, or a target FPS:

```csharp
using Ikon.AI.VideoEnhancement;

using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale4xUltra4);

var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
{
    Url = "https://example.com/input.mp4"
});

Log.Instance.Info($"Enhanced video URL: {result.Url}");
```

## SpeechGeneration

`Ikon.AI.SpeechGeneration.SpeechGenerator` streams synthesized speech while exposing supported voice IDs per model.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `ElevenFlash25` (cheap+fast) and returns the full clip as a single PCM `AudioChunk` (never null; throws `SpeechGeneratorException` on failure):

```csharp
using Ikon.AI.SpeechGeneration;
using Ikon.Resonance;

var audio = await SpeechGenerator.GenerateAsync("There once was a ship that put to sea.");

using var wavFile = new WavFile(audio.SampleRate, audio.ChannelCount, WavFile.SampleFormat.Float);
wavFile.AddSamples(audio.Samples);
wavFile.SaveToFile("speech.wav");
```

Use the constructor + config form for chunk-by-chunk streaming, voice discovery, language, instructions, or speed:

```csharp
using Ikon.AI.SpeechGeneration;
using Ikon.Resonance;

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
```

## SpeechRecognition

`Ikon.AI.SpeechRecognition.SpeechRecognizer` converts audio streams into text with configurable sample rates and languages.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot batch transcription — defaults to `WhisperLarge3Turbo` (cheap+fast):

```csharp
using Ikon.AI.SpeechRecognition;

string text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
```

Use the constructor + config form for PCM16 byte input, language hints, prompts, or continuous recognition:

```csharp
using Ikon.AI.SpeechRecognition;
using Ikon.Resonance;

var speechRecognizer = new SpeechRecognizer(SpeechRecognizerModel.Whisper2);

var audioBytes = await File.ReadAllBytesAsync("audio.raw");

string text = await speechRecognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
{
    Language = "en-US",
    SampleRate = 16000,
    ChannelCount = 1,
    Samples = AudioUtils.ConvertPcm16ToFloat(audioBytes)
});

Log.Instance.Info($"Recognized speech: '{text}'");
```

## SoundEffectGeneration

`Ikon.AI.SoundEffectGeneration.SoundEffectGenerator` generates sound effects from text prompts.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — returns a buffered WAV file:

```csharp
using Ikon.AI.SoundEffectGeneration;

var effect = await SoundEffectGenerator.GenerateAsync("A thunderstorm with heavy rain");
await File.WriteAllBytesAsync("thunder.wav", await effect.GetDataAsync());
```

Use the constructor + config form for duration, looping, prompt influence, or streaming PCM chunks:

```csharp
using Ikon.AI.SoundEffectGeneration;

using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);

var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig
{
    Prompt = "A thunderstorm with heavy rain",
    DurationSeconds = 5.0
});

await File.WriteAllBytesAsync("thunder.wav", await result.GetDataAsync());
```

## MusicGeneration

`Ikon.AI.MusicGeneration.MusicGenerator` generates music clips from text prompts, with optional audio-to-audio editing (input clips re-styled by the prompt). For short UI/game sound effects use `SoundEffectGenerator` instead.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `ElevenLabsMusicV2` (supports duration control and editing) and returns a buffered, encoded audio file:

```csharp
using Ikon.AI.MusicGeneration;

var music = await MusicGenerator.GenerateAsync("An upbeat 8-bit chiptune loop");
await File.WriteAllBytesAsync("music.mp3", await music.GetDataAsync());
```

Use the constructor + config form for duration control, input audio (editing), seeding, or streaming PCM chunks via `GenerateMusicAsync`:

```csharp
using Ikon.AI.MusicGeneration;

using var musicGenerator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);

var result = await musicGenerator.GenerateMusicFileAsync(new MusicGeneratorConfig
{
    Prompt = "An upbeat 8-bit chiptune loop",
    DurationSeconds = 10
});

await File.WriteAllBytesAsync("music.mp3", await result.GetDataAsync());
```

## WebScraping

`Ikon.AI.WebScraping.WebScraper` fetches and normalizes website content, with options for Markdown extraction and screenshots.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

**Local browser-based models need `Ikon.AI.Scrape`:** the LocalPuppeteer and LocalPlaywright implementations ship in the optional `Ikon.AI.Scrape` package (and the Azure and Google speech implementations elsewhere in the library ship in `Ikon.AI.Speech.Azure` and `Ikon.AI.Speech.Google`) to keep the core library lean. Add a reference to the matching capability package when your app runs those models with its own API credentials or local browsers; without it they resolve through the Ikon AI service as usual.

One-shot single page scrape — defaults to `Jina` (cheap+fast hosted reader) and returns the page as Markdown:

```csharp
using Ikon.AI.WebScraping;

var page = await WebScraper.ScrapeAsync("https://example.com");
Log.Instance.Info($"{page.Title}: {page.Content}...");
```

Use the constructor + config form for output formats, cookies, custom JavaScript, multi-page crawling, screenshots, and file downloads:

```csharp
using Ikon.AI.WebScraping;

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
```

## WebSearching

`Ikon.AI.WebSearching.WebSearcher` wraps search providers for page and image discovery.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot page search — defaults to `Google` (cheap+fast general web search):

```csharp
using Ikon.AI.WebSearching;

var results = await WebSearcher.SearchAsync("Finnish ice hockey teams", maxResults: 5);

foreach (var result in results)
{
    Log.Instance.Info($"{result.Title}: {result.Url}");
}
```

Use the constructor + config form for site-restricted search, country/language targeting, or image search:

```csharp
using Ikon.AI.WebSearching;

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
```

## FileConversion

`Ikon.AI.FileConversion.FileConverter` batches binary document conversions and handles long-running jobs transparently.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot from raw file bytes (the file name carries the source format):

```csharp
using Ikon.AI.FileConversion;

var pdf = await FileConverter.ConvertToPdfAsync(await File.ReadAllBytesAsync("brochure.docx"), "brochure.docx");
await File.WriteAllBytesAsync("brochure.pdf", await pdf.GetDataAsync());
```

Use the constructor + config form when the source is a URL or `AssetUri`, or when you need a custom timeout:

```csharp
using Ikon.AI.FileConversion;

var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
var convertedFile = await fileConverter.ConvertToPdfAsync(new FileConverterConfig
{
    Url = "https://example.com/brochure.docx",
    FileName = "brochure.docx"
});
await File.WriteAllBytesAsync("brochure.pdf", await convertedFile.GetDataAsync());
```

## OCR

`Ikon.AI.OCR.OCR` extracts selectable text and structural metadata from images or PDFs.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot from raw file bytes — defaults to `AzureDocumentIntelligence` (cheap+robust):

```csharp
using Ikon.AI.OCR;

var result = await OCR.AnalyzeAsync(await File.ReadAllBytesAsync("invoice.pdf"));
Log.Instance.Info(result.Text);
```

Use the constructor + config form when the document is a URL or `AssetUri`, or when you need page selection or word-level bounding boxes:

```csharp
using Ikon.AI.OCR;

var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
var result = await ocr.AnalyzeDocumentAsync(new OCRConfig
{
    Url = "https://example.com/invoice.pdf",
    IncludeWords = true
});

Log.Instance.Info(result.Text);
```

## Reranking

`Ikon.AI.Reranking.Reranker` orders candidate documents for relevance to a query to improve retrieval pipelines.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `CohereRerank4Fast` (cheap+fast):

```csharp
using Ikon.AI.Reranking;

var items = await Reranker.RerankAsync(
    ["Document about AI", "Document about cooking", "Document about space exploration"],
    query: "What is the latest in artificial intelligence?"
);

foreach (var item in items)
{
    Log.Instance.Info($"Index: {item.Index}, Score: {item.Score}");
}
```

Use the constructor + instance `RerankAsync` for a custom timeout or when reranking many queries with the same instance:

```csharp
using Ikon.AI.Reranking;

using var reranker = new Reranker(RerankModel.CohereRerank4Fast);
var items = await reranker.RerankAsync(new RerankerConfig { Documents = documents, Query = query, TopN = 5 });
```

## Classification

`Ikon.AI.Classification.Classifier` performs moderation and category detection with score-level transparency per safety label.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot text moderation — defaults to `OpenAIOmniModeration` (free to use):

```csharp
using Ikon.AI.Classification;

var result = await Classifier.ClassifyAsync("How to kill kittens? (not really!)");
Log.Instance.Info($"Flagged: {result.IsFlagged}");

foreach (var detail in result.Details)
{
    if (detail.IsFlagged)
    {
        Log.Instance.Info($"{detail.Label} ({detail.OriginalCategory}): {detail.Score}");
    }
}
```

Use the constructor + the instance `ClassifyAsync` overloads for image/message-part inputs, a custom `Timeout`, or classifying many inputs with the same instance:

```csharp
using Ikon.AI.Classification;

using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);

var result = await classifier.ClassifyAsync("What a nice weather!");
Log.Instance.Info($"Flagged: {result.IsFlagged}");
```

## Embeddings

`Ikon.AI.Embeddings.EmbeddingGenerator` creates vector representations for similarity search, clustering, or semantic scoring.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `OpenAI3Small` (cheap+fast) with `EmbeddingType.Generic`:

```csharp
using Ikon.AI.Embeddings;

var embeddings = await EmbeddingGenerator.EmbedAsync(
    ["Example sentence 1", "Example sentence 2", "Example sentence 3"]);

foreach (var embedding in embeddings)
{
    Log.Instance.Info($"Embedding length: {embedding.Length}");
}
```

Use the constructor + `GenerateEmbeddingsAsync` for batching control, a custom timeout, or the generator's `MaxInputCount` / `EmbeddingVectorSize` properties:

```csharp
using Ikon.AI.Embeddings;

using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);

var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(new EmbeddingGeneratorConfig
{
    Inputs = ["Example sentence 1", "Example sentence 2", "Example sentence 3"],
    Type = EmbeddingType.Document
});
```

## Kernel

`Ikon.AI.Kernel` supplies shared primitives such as `KernelContext`, `MessageBlock`, and `Instruction` that underpin shaders and direct LLM calls.

### Attaching media from the asset system

For large media that lives in the Ikon asset system, pass an `AssetUri` directly instead of reading the bytes yourself. `VideoAssetPart` lets you hand a video asset to a multimodal LLM without worrying about request-size limits:

```csharp
using Ikon.AI.Kernel;
using Ikon.Common.Core.Assets;

var assetUri = new AssetUri("assets://space/abc123/cloud-file/clips/demo.mp4");

var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, new IMessagePart[]
{
    new TextPart("Describe what happens in this clip."),
    new VideoAssetPart(assetUri),
}));
```

When the target model runs on Google Vertex (current Gemini models) and the asset is backed by GCS-aware cloud-file storage, the request references the video by its native `gs://bucket/object` URI — the bytes never transit the client. For other backends, a small video is inlined; videos above the inline ceiling throw so you can move them to cloud-file storage. Other providers (Anthropic, OpenAI) do not accept video parts and ignore `VideoAssetPart` with a warning, matching how they handle `VideoUrlPart` today.

## Chat

Multi-turn assistant conversations are built with the Emerge API in `Ikon.AI.Emergence`: carry the `KernelContext` returned by each `Emerge.Run` call (via `FinalAsync`) into the next turn and the model remembers the whole conversation — no manual history bookkeeping.

## Retrieving

`Ikon.AI.Retrieving.Retriever` provides retrieval-augmented generation primitives — vector-database-backed semantic search over indexed content, keyword extraction, an asset cache, and a set of built-in functions (`builtin.search`, `builtin.get_contents`, filter/sort helpers) that can be wired directly into a `KernelContext`.

## Database

`Ikon.AI.Database` connects to SQL databases (PostgreSQL, SQLite, BigQuery, Trino) and extracts schema metadata for text-to-SQL workflows. Use the typed factory methods on `DatabaseConnection` (`Trino`, `Postgres`, `Sqlite`, `BigQuery`) to open connections, and `DatabaseInfoExtractor` to discover tables and columns.
