# Ikon.AI Library Overview

This guide summarizes the principal namespaces in the Ikon.AI .NET library for developers building AI-enabled solutions. Each section outlines module responsibilities, supported models, and usage patterns verified by automated tests.

## Emergence

`Ikon.AI.Emergence` is the recommended way to build AI workflows with typed outputs. It provides a streaming-first, C#-idiomatic API for structured object generation, tool calling, and advanced multi-agent patterns. All APIs return `IAsyncEnumerable<EmergeEvent<T>>` and non-streaming usage is achieved via the `.FinalAsync()` extension method. Emergence can target any model listed in the [LLM](#llm) section. See the [Emergence Guide](emergence-guide.md) for the full documentation.

### Object Generation

Needs the `Ikon.AI.Emergence`, `Ikon.AI.Kernel`, `Ikon.AI.LLM`, `Ikon.Common.Core` using directives.

<!-- ikon-code: ai-object-generation -->
```csharp
var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, "Tell me about John Smith."));

var (result, _) = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, context, pass =>
{
    pass.Command = "Return invented personal details about the person the user asked about.";
}).FinalAsync();

Log.Instance.Info($"Result: {Json.To(result)}");
```

with the result type declared alongside:

<!-- ikon-code: ai-object-generation-2 -->
```csharp
public class PersonDetails
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

Emergence supports multi-agent patterns: `BestOf`, `Refine`, `MapReduce`, `TreeSearch`, and `EnsembleMerge`. See the [Emergence Guide](emergence-guide.md) for full documentation on all patterns.

Region support is available via `pass.Regions`:

<!-- ikon-code: air-object-generation -->
```csharp
var result = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, pass =>
{
    pass.Command = "Return invented personal details about a person.";
    pass.Regions = [ModelRegion.Eu, ModelRegion.Global];
});
```

## LLM

> **Note:** For most use cases, prefer using [Emergence](#emergence) which provides structured outputs and higher-level patterns on top of the LLM layer.

`Ikon.AI.LLM` offers direct, streaming-level access to language models when higher-level orchestration is unnecessary.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

Pass preferred regions as an ordered list to keep inference within a geography. If omitted, the default region is `Global`.

Needs the `Ikon.AI`, `Ikon.AI.Kernel`, `Ikon.AI.LLM`, `Ikon.Common.Core` using directives.

<!-- ikon-code: aid-llm -->
```csharp
var context = new KernelContext();
context = context.Add(new Instruction(InstructionType.Context, "You are a helpful assistant that helps to summarize product release notes."));
context = context.Add(new MessageBlock(MessageBlockRole.User, "Summarise the latest release highlights. Here are the notes: ..."));

await foreach (var llmEvent in Emerge.Generate(LLMModel.Gpt5Mini, context, regions: [ModelRegion.Eu]))
{
    Log.Instance.Info($"{llmEvent.Source} | {llmEvent}");
}

var stringResult = await Emerge.Generate(LLMModel.Gpt5Mini, context).AsStringAsync();
Log.Instance.Info($"String result: {stringResult}");
```

## Custom Model Endpoints

Run your own model — a self-hosted LLM behind vLLM, Ollama, TGI, or any endpoint speaking a supported provider API — and use it through the normal Ikon.AI APIs. Register the endpoint with `CustomModels` at app startup, then select the model by its registered name anywhere a model name string is accepted:

Needs the `Ikon.AI`, `Ikon.AI.Emergence`, `Ikon.AI.LLM` using directives.

<!-- ikon-code: aid-custom-model-endpoints -->
```csharp
CustomModels.Instance.Register(new CustomLLMModel
{
    Name = "my-model",
    EndpointUrl = "http://gpu-box:8000/v1/chat/completions",
    Api = CustomLLMApi.OpenAICompletions,
    ApiModelName = "Qwen/Qwen2.5-32B-Instruct",
    ApiKey = "sk-local-123",           // omit for keyless endpoints (e.g. local Ollama)
    ContextWindowSize = 32768,
    MaxOutputTokens = 8192,            // omit when the endpoint caps nothing
    SupportsJsonSchema = true,
});

var reply = await Emerge.AskAsync("Hello", "my-model");

await foreach (var llmEvent in Emerge.Generate("my-model", new KernelContext()))
{
    Log.Instance.Info($"{llmEvent}");
}
```

Custom models are supported for LLMs (`CustomLLMModel`), embeddings (`CustomEmbeddingModel`), reranking (`CustomRerankModel`), and classification (`CustomClassificationModel`); each picks one of the category's existing HTTP request formats via its `Api` enum. Other categories (image generation, speech) are not yet supported.

Key behaviors:

- **Always in-process.** Calls to custom models execute locally with your API key and never go through the Ikon RPC mechanism — the platform never sees your endpoint or key.
- **Flat per-request billing.** Usage is reported with a `.user` suffix and charged as a flat credit fee per successful request (identical for all custom models) instead of per-token provider pricing. Token counts are still reported for analytics. A failed or aborted request is not billed; each successful retry bills its own request.
- **Register at startup.** The registry is async-local (like credentials): register models on the main flow before spawning parallel work so every flow sees them. Registering the same name again replaces the previous registration.
- **Names.** A custom model name must not collide with a built-in model name and must not contain dots or whitespace.
- **Output budget.** `MaxOutputTokens` is the largest response the endpoint will produce. `LLM` lowers `KernelContext.MaxOutputTokens` to it before sending, so a caller running the 16000-token default against a smaller model gets a shorter answer instead of a rejected request. Leave it at 0 when the endpoint caps nothing and the caller's value goes out unchanged. Built-in models carry the limit their provider publishes; read it with `Emerge.GetCapabilities(model).MaxOutputTokens` or `model.MaxOutputTokens()`.

## ImageGeneration

`Ikon.AI.ImageGeneration.ImageGenerator` creates images with negative prompts, seeding, and resolution controls.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `Gemini25FlashImage` (cheap+fast); the result is never null (throws `ImageGeneratorException` on failure):

Needs the `Ikon.AI.ImageGeneration` using directive.

<!-- ikon-code: ai-imagegeneration -->
```csharp
var image = await ImageGenerator.GenerateAsync("A santa dancing in the snow");
await File.WriteAllBytesAsync("santa.png", await image.GetDataAsync());
```

**Result delivery:** media results (image, music, sound effect, converted file, segmentation mask, depth map, upscaled image) carry a `Kind` field. By default they arrive inline (`Kind == ResultKind.Data`, `Data` non-null). When a result is returned from a remotely hosted AI function and its payload exceeds a few MB, it is automatically uploaded and arrives as a signed download URL valid for roughly one hour (`Kind == ResultKind.Url`, `Url` non-null, `Data` null) to stay within the protocol's message size limit; consumed locally in-process, large payloads stay inline. `await result.GetDataAsync()` returns the bytes either way, so prefer it over reading `Data` directly. Set `ResultDelivery = ResultDelivery.Url` in the config to always receive a URL.

Use the constructor + config form for negative prompts, resolution, seeding, batches, or input images:

Needs the `Ikon.AI.ImageGeneration` using directive.

<!-- ikon-code: ai-imagegeneration-2 -->
```csharp
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

## Provenance and Watermarking

`Ikon.AI.ImageProvenance` is what marks a generated image as generated. The image generators and the
upscaler apply it themselves — `InvisibleWatermark` and `VisibleWatermark` on the config control it —
so app code normally only *reads* the marks.

- `ImageProvenance.GetMarkingSupport(data)` returns a `ProvenanceMarking` for the format: `Full` when
  the pixel watermark survives a re-encode, `MetadataOnly` when the mark is machine-readable and
  standards-compliant but strippable by anything that rewrites the file's metadata, and `None` when
  the format carries neither. The upscaler checks this before re-marking.
- `ImageProvenance.ReadMetadataMark(data)` returns the recorded model name, or null.
- `ImageProvenance.MeasureInvisibleMark(data)` scores the pixel watermark. Scores are
  normal-deviates: an unmarked image scores |z| ≲ 3, a marked one in the tens to hundreds depending
  on size and recompression. At or above `ImageProvenance.DetectionThreshold` (12.0) the image
  carries Ikon's mark.
- `ImageProvenance.Apply(data, model, invisibleWatermark, visibleWatermark)` marks an image yourself,
  for a path the platform generators did not produce.

## Image Utilities

`Ikon.AI.ImageUtils` holds the pixel chores the AI calls keep needing. `GetImageDimensions` reads
width and height without decoding the whole image; `IsWebP` sniffs the format. `EncodeJpegCapped`
caps both dimensions (aspect preserved) and re-encodes as JPEG, returning the source bytes unchanged
when the image already fits — this is how an image is brought under a model's input limit before it
is sent. The mask helpers convert between the two conventions models disagree on:
`ConvertAlphaMaskToBlackWhiteMask`, `ConvertBlackWhiteMaskToAlphaMask` and `InvertMask`.


## ImageSegmentation

`Ikon.AI.ImageSegmentation.ImageSegmenter` segments objects from images using text, point, or box prompts (Segment Anything models). The result contains one mask image per detected object, along with confidence scores and normalized bounding boxes.

Both calls return an `ImageSegmenterResult`: `Segments` is a list of `ImageSegmenterResult.Segment`, each carrying a `Mask` image, a normalized `Box` and a `Score`, and `Preview` is an optional composite. `ImageSegmenter` implements `IImageSegmenter`, so a method that only needs "something that segments" can take the interface.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot from image bytes — defaults to `Sam31` (latest SAM revision):

Needs the `Ikon.AI.ImageSegmentation` using directive.

<!-- ikon-code: ai-imagesegmentation -->
```csharp
var result = await ImageSegmenter.SegmentAsync(imageBytes, "image/png", "person");
await File.WriteAllBytesAsync("mask.png", await result.Segments[0].Mask.GetDataAsync());
```

Use the constructor + config form for URL input, point/box prompts, or multiple masks:

Needs the `Ikon.AI.ImageSegmentation` using directive.

<!-- ikon-code: ai-imagesegmentation-2 -->
```csharp
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

## ImageUpscaling

`Ikon.AI.ImageUpscaling.ImageUpscaler` raises the resolution of a single input image (super-resolution). The result is one larger image. Useful for rescuing low-resolution source material, printing or presenting a generated image at a larger size, and recovering detail from compressed photos.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps). SeedVR2 is the default and scales up to 10x; Topaz is the premium option at up to 4x and is the only model that can restore faces; Recraft Crisp upscales by a fixed amount with no controls and returns WebP; Crystal is the one model that will invent detail.

Some models cap how large an output they will produce, reported as `MaxOutputMegapixels` in the capabilities (Topaz is capped at 48; the rest are uncapped). A request whose input size and scale factor would exceed the cap is refused before the provider is called, rather than running up a charge at a price tier above the one the platform bills.

**Faithful vs. creative:** upscalers differ in whether they invent detail, and every model's `Fidelity` says which it is. A `Faithful` model reconstructs only what the input supports, so its output can still be read as evidence of the original. A `Creative` model synthesizes plausible detail that was never there. A `Tunable` model moves between the two as `Creativity` rises (0 to 1) and sits at the faithful end when it is left at 0. Every model here defaults to faithful behaviour, and asking a faithful model for `Creativity` above 0 throws rather than being quietly ignored — so nothing hallucinates unless you ask it to. Check `ImageUpscaler.GetCapabilities(model)` when the distinction matters.

One-shot from image bytes — defaults to `SeedVr2`, and to the model's own scale factor:

Needs the `Ikon.AI.ImageUpscaling` using directive.

<!-- ikon-code: ai-imageupscaling -->
```csharp
var result = await ImageUpscaler.UpscaleAsync(imageBytes, "image/png", scaleFactor: 4);
await File.WriteAllBytesAsync("upscaled.png", await result.Image.GetDataAsync());
```

Use the constructor + config form for URL input, a target resolution, or creative upscaling:

Needs the `Ikon.AI.ImageUpscaling` using directive.

<!-- ikon-code: ai-imageupscaling-2 -->
```csharp
using var imageUpscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);

var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
{
    InputImage = new InputImage { Url = "https://example.com/photo.png" },
    TargetResolution = UpscaleTargetResolution.Uhd2160
});

await File.WriteAllBytesAsync("upscaled.png", await result.Image.GetDataAsync());
```

`ScaleFactor` and `TargetResolution` are mutually exclusive, and a model rejects either one it does not support rather than silently ignoring it. Output is PNG unless `OutputFormat` says otherwise, so a freshly recovered image is not immediately thrown away to JPEG.

## DepthEstimation

`Ikon.AI.DepthEstimation.DepthEstimator` produces a monocular depth map from a single input image. The result is a `DepthEstimatorResult` whose single `Depth` image is the map (closer surfaces brighter, farther surfaces darker). Useful for 3D reconstruction, parallax/relighting effects, compositing, and depth-conditioned image generation. The estimator implements `IDepthEstimator`.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps). Depth Anything V2 is the fast default; Marigold is diffusion-based and higher quality but slower; MiDaS is a lightweight classic.

One-shot from image bytes — defaults to `DepthAnythingV2` (cheap+fast):

Needs the `Ikon.AI.DepthEstimation` using directive.

<!-- ikon-code: ai-depthestimation -->
```csharp
var result = await DepthEstimator.EstimateAsync(imageBytes, "image/png");
await File.WriteAllBytesAsync("depth.png", await result.Depth.GetDataAsync());
```

Use the constructor + config form for URL input or the Marigold tuning fields:

Needs the `Ikon.AI.DepthEstimation` using directive.

<!-- ikon-code: ai-depthestimation-2 -->
```csharp
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

Needs the `Ikon.AI.MeshGeneration` using directive.

<!-- ikon-code: ai-meshgeneration -->
```csharp
var mesh = await MeshGenerator.GenerateAsync("A small wooden treasure chest with brass fittings");
Log.Instance.Info($"GLB URL: {mesh.GlbUrl}");
```

Use the constructor + config form for image-to-mesh, PBR textures, or polycount/topology control:

Needs the `Ikon.AI.MeshGeneration` using directive.

<!-- ikon-code: ai-meshgeneration-2 -->
```csharp
using var meshGenerator = new MeshGenerator(MeshGeneratorModel.Meshy6);

var result = await meshGenerator.GenerateMeshAsync(new MeshGeneratorConfig
{
    Prompt = "A small wooden treasure chest with brass fittings",
    TargetPolycount = 20000
});

Log.Instance.Info($"GLB URL: {result.GlbUrl}");
```

A `MeshGeneratorResult` carries `GlbUrl`, `FbxUrl`, `ObjUrl`, `MtlUrl`, `UsdzUrl` and `ThumbnailUrl` — whichever formats the model produced — plus the `ExpiresAt` the signed URLs stop working at. `MeshGeneratorConfig` takes a `MeshGeneratorMeshStyle` (`Standard` or `LowPoly`) and a `MeshGeneratorTopology` (`Triangle` or `Quad`). Models differ in what they accept, so check before you send: `MeshGenerator` implements `IMeshGenerator`, which extends `IMeshGeneratorInfo` with `SupportsTextToMesh`, `SupportsImageToMesh`, `SupportsPbr`, `SupportsLowPoly` and `MaxInputImages`. The same five read off a `MeshGeneratorCapabilities` without constructing a generator.

## VideoGeneration

`Ikon.AI.VideoGeneration.VideoGenerator` renders video clips with configurable length, resolution, and aspect ratio.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot text-to-video — defaults to `Veo31Fast` (cheap+fast):

Needs the `Ikon.AI.VideoGeneration` using directive.

<!-- ikon-code: ai-videogeneration -->
```csharp
var video = await VideoGenerator.GenerateAsync("A santa dancing in the snow");
Log.Instance.Info($"Video URL: {video.Url}");
```

Use the constructor + config form for input images (image-to-video), length, resolution, or aspect ratio:

Needs the `Ikon.AI.VideoGeneration` using directive.

<!-- ikon-code: ai-videogeneration-2 -->
```csharp
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

Needs the `Ikon.AI.VideoEnhancement` using directive.

<!-- ikon-code: ai-videoenhancement -->
```csharp
var enhanced = await VideoEnhancer.EnhanceAsync("https://example.com/input.mp4");
Log.Instance.Info($"Enhanced video URL: {enhanced.Url}");
```

Use the constructor + config form for raw video bytes, frame ranges, or a target FPS:

Needs the `Ikon.AI.VideoEnhancement` using directive.

<!-- ikon-code: ai-videoenhancement-2 -->
```csharp
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

Needs the `Ikon.AI.SpeechGeneration`, `Ikon.Resonance` using directives.

<!-- ikon-code: ai-speechgeneration -->
```csharp
var audio = await SpeechGenerator.GenerateAsync("There once was a ship that put to sea.");

using var wavFile = new WavFile(audio.SampleRate, audio.ChannelCount, WavFile.SampleFormat.Float);
wavFile.AddSamples(audio.Samples);
wavFile.SaveToFile("speech.wav");
```

Use the constructor + config form for chunk-by-chunk streaming, voice discovery, language, instructions, or speed:

Needs the `Ikon.AI.SpeechGeneration`, `Ikon.Resonance` using directives.

<!-- ikon-code: ai-speechgeneration-2 -->
```csharp
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

Needs the `Ikon.AI.SpeechRecognition` using directive.

<!-- ikon-code: ai-speechrecognition -->
```csharp
string text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
```

Use the constructor + config form for PCM16 byte input, language hints, prompts, timings, or
continuous recognition. `RecognizeBatchSpeechAsync` returns a `Transcript`, not a string:

Needs the `Ikon.AI.SpeechRecognition`, `Ikon.Resonance` using directives.

<!-- ikon-code: ai-speechrecognition-2 -->
```csharp
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
```

### Timestamps, speakers and confidence

`Transcript` carries `Text`, the detected `Language`, the audio `Duration`, a `Confidence` where the
provider reports one, and — when you ask for them — `Words` (a list of `SpeechWord`) and `Segments`
(a list of `TranscriptSegment`). Both record types carry `Text`, `Start`, `End`, `Confidence` and
`Speaker`. Timings are always `TimeSpan` relative to the start of the submitted audio, whatever units
the provider reported.

<!-- ikon-code: ai-timestamps-speakers-and-confidence -->
```csharp
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
```

`Timestamps` defaults to `None`, so an unchanged request costs exactly what it did before. **Asking
for a granularity the model does not support throws** rather than returning an empty list — an empty
`Words` would otherwise mean both "not supported" and "no speech". Check first with
`SpeechRecognizer.GetCapabilities(model)`, which reports `SupportsWordTimestamps`,
`SupportsSegmentTimestamps` and `SupportsDiarization` alongside the existing flags.

Two provider limits are worth knowing because they surface as exceptions rather than as quietly
missing fields:

- The GPT-4o transcribe models have no timings at all; the diarizing variant reports speaker
  segments but never words.
- Voxtral accepts **one** granularity per request, and rejects a language hint together with
  timestamps — ask for `Word` or `Segment`, and leave `Language` empty when you do.

Continuous recognition yields `TranscriptEvent` instead of a string. `IsFinal` separates a
provider's revisable interim hypothesis from text it will not change, and only final events carry
`Words` — no provider attaches word timings to an interim result. `Start`/`End` are relative to the
start of the stream, so they keep growing for the life of the recognition. Interim events are off
unless `InterimResults` asks for them:

<!-- ikon-code: ai-timestamps-speakers-and-confidence-2 -->
```csharp
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
```

## SoundEffectGeneration

`Ikon.AI.SoundEffectGeneration.SoundEffectGenerator` generates sound effects from text prompts.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — returns a buffered WAV file:

Needs the `Ikon.AI.SoundEffectGeneration` using directive.

<!-- ikon-code: ai-soundeffectgeneration -->
```csharp
var effect = await SoundEffectGenerator.GenerateAsync("A thunderstorm with heavy rain");
await File.WriteAllBytesAsync("thunder.wav", await effect.GetDataAsync());
```

Use the constructor + config form for duration, looping, prompt influence, or streaming PCM chunks:

Needs the `Ikon.AI.SoundEffectGeneration` using directive.

<!-- ikon-code: ai-soundeffectgeneration-2 -->
```csharp
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

Needs the `Ikon.AI.MusicGeneration` using directive.

<!-- ikon-code: ai-musicgeneration -->
```csharp
var music = await MusicGenerator.GenerateAsync("An upbeat 8-bit chiptune loop");
await File.WriteAllBytesAsync("music.mp3", await music.GetDataAsync());
```

Use the constructor + config form for duration control, input audio (editing), seeding, or streaming PCM chunks via `GenerateMusicAsync`:

Needs the `Ikon.AI.MusicGeneration` using directive.

<!-- ikon-code: ai-musicgeneration-2 -->
```csharp
using var musicGenerator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);

var result = await musicGenerator.GenerateMusicFileAsync(new MusicGeneratorConfig
{
    Prompt = "An upbeat 8-bit chiptune loop",
    DurationSeconds = 10
});

await File.WriteAllBytesAsync("music.mp3", await result.GetDataAsync());
```

The buffered call returns a `MusicGeneratorResult` — `Data` or `Url` depending on the `Kind`, plus `MimeType` and `DurationSeconds`. `MusicGenerator` implements `IMusicGenerator`, which extends `IMusicGeneratorInfo` with `SupportsStreaming`, `SupportsEditing` and `SupportsDurationControl`; the same three read off a `MusicGeneratorCapabilities`. Streaming is gated: `GenerateMusicAsync` throws `NonRetryableAIException` on a model whose `SupportsStreaming` is false, and a model without duration control silently ignores `DurationSeconds`.

## WebScraping

`Ikon.AI.WebScraping.WebScraper` fetches and normalizes website content, with options for Markdown extraction and screenshots.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

**Local browser-based models need `Ikon.AI.Scrape`:** the LocalPuppeteer and LocalPlaywright implementations ship in the optional `Ikon.AI.Scrape` package (and the Azure and Google speech implementations elsewhere in the library ship in `Ikon.AI.Speech.Azure` and `Ikon.AI.Speech.Google`) to keep the core library lean. Add a reference to the matching capability package when your app runs those models with its own API credentials or local browsers; without it they resolve through the Ikon AI service as usual.

One-shot single page scrape — defaults to `Jina` (cheap+fast hosted reader) and returns the page as Markdown:

Needs the `Ikon.AI.WebScraping` using directive.

<!-- ikon-code: ai-webscraping -->
```csharp
var page = await WebScraper.ScrapeAsync("https://example.com");
Log.Instance.Info($"{page.Title}: {page.Content}...");
```

Use the constructor + config form for output formats, cookies, custom JavaScript, multi-page crawling, screenshots, and file downloads:

Needs the `Ikon.AI.WebScraping` using directive.

<!-- ikon-code: ai-webscraping-2 -->
```csharp
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

Needs the `Ikon.AI.WebSearching` using directive.

<!-- ikon-code: ai-websearching -->
```csharp
var results = await WebSearcher.SearchAsync("Finnish ice hockey teams", maxResults: 5);

foreach (var result in results)
{
    Log.Instance.Info($"{result.Title}: {result.Url}");
}
```

Use the constructor + config form for site-restricted search, country/language targeting, or image search:

Needs the `Ikon.AI.WebSearching` using directive.

<!-- ikon-code: ai-websearching-2 -->
```csharp
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

Needs the `Ikon.AI.FileConversion` using directive.

<!-- ikon-code: ai-fileconversion -->
```csharp
var pdf = await FileConverter.ConvertToPdfAsync(await File.ReadAllBytesAsync("brochure.docx"), "brochure.docx");
await File.WriteAllBytesAsync("brochure.pdf", await pdf.GetDataAsync());
```

Use the constructor + config form when the source is a URL or `AssetUri`, or when you need a custom timeout:

Needs the `Ikon.AI.FileConversion` using directive.

<!-- ikon-code: ai-fileconversion-2 -->
```csharp
var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
var convertedFile = await fileConverter.ConvertToPdfAsync(new FileConverterConfig
{
    Url = "https://example.com/brochure.docx",
    FileName = "brochure.docx"
});
await File.WriteAllBytesAsync("brochure.pdf", await convertedFile.GetDataAsync());
```

## OCR

`Ikon.AI.OCR.OCR` extracts selectable text and structural metadata from images, PDFs and Office documents.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot from raw file bytes — defaults to `AzureDocumentIntelligence` (cheap+robust):

Needs the `Ikon.AI.OCR` using directive.

<!-- ikon-code: ai-ocr -->
```csharp
var result = await OCR.AnalyzeAsync(await File.ReadAllBytesAsync("invoice.pdf"));
Log.Instance.Info(result.Text);
```

Use the constructor + config form when the document is a URL or `AssetUri`, or when you need page selection or word-level bounding boxes:

Needs the `Ikon.AI.OCR` using directive.

<!-- ikon-code: ai-ocr-2 -->
```csharp
var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
var result = await ocr.AnalyzeDocumentAsync(new OCRConfig
{
    Url = "https://example.com/invoice.pdf",
    Pages = "1-5",
    IncludeWords = true
});

Log.Instance.Info(result.Text);
```

**Page selection** is 1-based and inclusive on every model — `"1-5"`, `"2"`, `"1-3,7"` all mean the same pages whichever model reads them, and `result.Pages[].PageNumber` numbers them the same way. A range whose syntax is wrong is rejected before the provider is called rather than being silently read as the whole document.

**Limits** come from `OCR.GetCapabilities(model)`, so a caller can size a request before making it:

| | What it says |
|---|---|
| `MaxPagesSupported` | Most pages the model reads in one request. Split a longer document across several requests with `Pages`. |
| `MaxDocumentSizeBytes` | Largest document the model accepts. A `Data` request over the limit is refused before the provider is called; the size behind a `Url` or `AssetUri` is not known up front, so it is not checked. |
| `SupportedMimeTypes` | What the provider documents as readable input. Advisory — a type outside the list is still passed through, because the provider is the authority on what it will read. |
| `SupportsWordLevelText` | Whether `IncludeWords` fills `result.Words`. Asking a model that reports `false` is refused rather than answered with an empty list, so check this before branching on word geometry. |

A `0` on either numeric limit means the provider publishes no limit — not a zero budget.

## Reranking

`Ikon.AI.Reranking.Reranker` orders candidate documents for relevance to a query to improve retrieval pipelines.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `CohereRerank4Fast` (cheap+fast):

Needs the `Ikon.AI.Reranking` using directive.

<!-- ikon-code: ai-reranking -->
```csharp
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

Needs the `Ikon.AI.Reranking` using directive.

<!-- ikon-code: ai-reranking-2 -->
```csharp
using var reranker = new Reranker(RerankModel.CohereRerank4Fast);
var items = await reranker.RerankAsync(new RerankerConfig { Documents = documents, Query = query, TopN = 5 });
```

## Classification

`Ikon.AI.Classification.Classifier` performs moderation and category detection with score-level transparency per safety label.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot text moderation — defaults to `OpenAIOmniModeration` (free to use):

Needs the `Ikon.AI.Classification` using directive.

<!-- ikon-code: ai-classification -->
```csharp
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

Needs the `Ikon.AI.Classification` using directive.

<!-- ikon-code: ai-classification-2 -->
```csharp
using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);

var result = await classifier.ClassifyAsync("What a nice weather!");
Log.Instance.Info($"Flagged: {result.IsFlagged}");
```

## Embeddings

`Ikon.AI.Embeddings.EmbeddingGenerator` creates vector representations for similarity search, clustering, or semantic scoring.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

One-shot — defaults to `OpenAI3Small` (cheap+fast) with `EmbeddingType.Generic`:

Needs the `Ikon.AI.Embeddings` using directive.

<!-- ikon-code: ai-embeddings -->
```csharp
var embeddings = await EmbeddingGenerator.EmbedAsync(
    ["Example sentence 1", "Example sentence 2", "Example sentence 3"]);

foreach (var embedding in embeddings)
{
    Log.Instance.Info($"Embedding length: {embedding.Length}");
}
```

Use the constructor + `GenerateEmbeddingsAsync` for batching control, a custom timeout, or the generator's `MaxInputCount` / `EmbeddingVectorSize` properties:

Needs the `Ikon.AI.Embeddings` using directive.

<!-- ikon-code: ai-embeddings-2 -->
```csharp
using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);

var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(new EmbeddingGeneratorConfig
{
    Inputs = ["Example sentence 1", "Example sentence 2", "Example sentence 3"],
    Type = EmbeddingType.Document
});
```

## Kernel

`Ikon.AI.Kernel` supplies shared primitives such as `KernelContext`, `MessageBlock`, and `Instruction` that underpin Emergence passes and direct LLM calls.

### Attaching media from the asset system

For large media that lives in the Ikon asset system, pass an `AssetUri` directly instead of reading the bytes yourself. `VideoAssetPart` lets you hand a video asset to a multimodal LLM without worrying about request-size limits:

Needs the `Ikon.AI.Kernel`, `Ikon.Common.Core.Assets` using directives.

<!-- ikon-code: ai-attaching-media-from-the-asset-system -->
```csharp
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
