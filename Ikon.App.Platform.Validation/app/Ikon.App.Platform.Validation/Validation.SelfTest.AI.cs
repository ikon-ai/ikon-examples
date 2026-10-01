using Ikon.AI.VideoSegmentation;
using CoreFunction = Ikon.Common.Core.Functions.Function;
using ResonanceAudioChunk = Ikon.Resonance.Core.AudioChunk;

public partial class Validation
{
    public sealed record SelfTestPerson(string Name, int Age);

    public sealed record SelfTestScore(int Value);

    public sealed record SelfTestSum(int Sum);

    public sealed record SelfTestDraft(int Version);

    public sealed record SelfTestCount(int Count);

    private async Task<string> SelfTestEmergeScriptedRunAsync()
    {
        var person = await Emerge.Run<SelfTestPerson>(
            LLMModel.Default,
            pass => { pass.Command = "Describe the first programmer"; },
            Emerge.Scripted(["{\"name\":\"Ada\",\"age\":36}"]));
        Expect(person == new SelfTestPerson("Ada", 36), $"the scripted JSON parsed to {person}");

        var cycling = Emerge.Scripted(["one", "two"]);
        var replies = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            replies.Add((await Emerge.Run<string>(LLMModel.Default, pass => { pass.Command = "next"; }, cycling)).Trim());
        }

        Expect(replies.SequenceEqual(["one", "two", "one"]), $"Scripted replayed [{string.Join(",", replies)}] instead of cycling one,two,one");

        var stopped = await ExpectThrowsAsync<EmergenceStoppedException>(async () => await Emerge.Run<SelfTestPerson>(
            LLMModel.Default,
            pass =>
            {
                pass.Command = "Describe someone";
                pass.MaxIterations = 2;
            },
            Emerge.Scripted(["this is not json"])), "a run whose every reply is invalid JSON");

        return $"Run<T> parsed scripted JSON to a record; Scripted cycled one,two,one; invalid JSON stopped the run ({stopped.Status})";
    }

    private async Task<string> SelfTestEmergeBestOfAsync()
    {
        var scored = 0;

        var best = await Emerge.BestOf<SelfTestScore>(
            LLMModel.Default,
            new KernelContext(),
            options =>
            {
                options.Command = "Pick a number";
                options.Count = 3;
                options.ScoreAsync = async (candidate, trace) =>
                {
                    Interlocked.Increment(ref scored);
                    await Task.Yield();
                    return candidate.Value;
                };
            },
            Emerge.Scripted(["{\"value\":1}", "{\"value\":7}", "{\"value\":3}"]));

        Expect(best.Value == 7, $"BestOf picked {best.Value} instead of the highest-scoring 7");
        Expect(scored == 3, $"ScoreAsync ran {scored} times for 3 candidates");

        return "three scripted candidates scored by ScoreAsync; the 7 won";
    }

    private async Task<string> SelfTestEmergeToolEndRunAsync()
    {
        var modelCalls = 0;
        var toolCalls = 0;

        async IAsyncEnumerable<LLMEvent> Reply(KernelContext context)
        {
            await Task.Yield();

            if (Interlocked.Increment(ref modelCalls) == 1)
            {
                var add = context.Functions["add"];
                yield return new LLMEvent.ToolCallRequested(new FunctionCall(add, [2, 3], "{\"a\":2,\"b\":3}", Guid.NewGuid().ToString("N"), "validation-selftest"));
            }
            else
            {
                yield return new LLMEvent.TextDelta("{\"sum\":-1}");
            }
        }

        var result = await Emerge.Run<SelfTestSum>(
            LLMModel.Default,
            pass =>
            {
                pass.Command = "Add 2 and 3 with the tool";
                pass.MaxIterations = 3;
                pass.AddTool(Tool.Of("add", "Adds two integers", (int a, int b) =>
                {
                    Interlocked.Increment(ref toolCalls);
                    return Emerge.EndRun(new SelfTestSum(a + b));
                }));
            },
            (context, ct) => Reply(context));

        Expect(result.Sum == 5, $"the run ended with Sum={result.Sum} instead of the tool's 5");
        Expect(toolCalls == 1 && modelCalls == 1, $"the tool ran {toolCalls}x and the model was asked {modelCalls}x (EndRun must end the run after the tool)");

        return "a ModelStream requested the add tool (Tool.Of + AddTool); its Emerge.EndRun value became the run result with no second model turn";
    }

    private async Task<string> SelfTestEmergeRefineAsync()
    {
        var checkedVersions = new List<int>();

        var result = await Emerge.Refine<SelfTestDraft>(
            LLMModel.Default,
            new KernelContext(),
            options =>
            {
                options.MaxRefinements = 3;
                options.Initial(scope => { scope.Command = "Write a first draft"; });
                options.Refinement(scope => { scope.Command = "Improve the draft"; });
                options.ShouldContinue = async (draft, trace) =>
                {
                    lock (checkedVersions)
                    {
                        checkedVersions.Add(draft.Version);
                    }

                    await Task.Yield();
                    return draft.Version < 2;
                };
            },
            Emerge.Scripted(["{\"version\":1}", "{\"version\":2}", "{\"version\":3}"]));

        Expect(result.Version == 2, $"Refine returned version {result.Version} instead of stopping at 2");
        Expect(checkedVersions.SequenceEqual([1, 2]), $"ShouldContinue saw [{string.Join(",", checkedVersions)}] instead of [1,2]");

        return "initial draft v1 refined once to v2, then ShouldContinue stopped it before v3";
    }

    private async Task<string> SelfTestEmergeMapReduceAsync()
    {
        string? reducePrompt = null;

        async IAsyncEnumerable<LLMEvent> Reply(KernelContext context)
        {
            await Task.Yield();
            var text = SelfTestContextText(context);

            if (text.Contains("Mapped results", StringComparison.Ordinal))
            {
                reducePrompt = text;
                yield return new LLMEvent.TextDelta("{\"count\":3}");
            }
            else
            {
                var chunk = text.Contains("Chunk 1/3", StringComparison.Ordinal) ? 1 : text.Contains("Chunk 2/3", StringComparison.Ordinal) ? 2 : 3;
                yield return new LLMEvent.TextDelta($"{{\"value\":{chunk * 10}}}");
            }
        }

        var result = await Emerge.MapReduce<string, SelfTestScore, SelfTestCount>(
            LLMModel.Default,
            new KernelContext(),
            options =>
            {
                options.Chunks = ["alpha", "beta", "gamma"];
                options.MaxParallel = 2;
                options.Map(scope => { scope.Command = "Score the chunk"; });
                options.Reduce(scope => { scope.Command = "Count the scores"; });
            },
            (context, ct) => Reply(context));

        Expect(result.Count == 3, $"the reduce result was {result.Count}");
        Expect(reducePrompt != null, "the reduce step never reached the model");

        var ten = reducePrompt!.IndexOf("10", StringComparison.Ordinal);
        var twenty = reducePrompt.IndexOf("20", StringComparison.Ordinal);
        var thirty = reducePrompt.IndexOf("30", StringComparison.Ordinal);
        Expect(ten >= 0 && ten < twenty && twenty < thirty, "the reduce prompt did not list the mapped values in input order");

        return "three chunks mapped (two in parallel) and reduced; the reduce prompt carried the mapped values in input order";
    }

    private async Task<string> SelfTestLlmCapabilitiesAsync()
    {
        var models = Enum.GetValues<LLMModel>();
        var unresolved = new List<string>();
        var capabilities = new List<(LLMModel Model, LLMCapabilities Caps)>();

        foreach (var model in models)
        {
            try
            {
                capabilities.Add((model, Emerge.GetCapabilities(model)));
            }
            catch (Exception ex)
            {
                unresolved.Add($"{model} ({ex.GetType().Name})");
            }
        }

        Expect(unresolved.Count == 0, $"no capabilities for {string.Join(", ", unresolved)}");
        // A zero output cap means the provider publishes none, which is a valid answer; a zero
        // context window means the model's entry is incomplete.
        var noContextWindow = capabilities.Where(entry => entry.Caps.ContextWindowSize <= 0).Select(entry => entry.Model).ToList();
        Expect(noContextWindow.Count == 0, $"no context window on {string.Join(",", noContextWindow)}");
        var uncappedOutput = capabilities.Count(entry => entry.Caps.MaxOutputTokens <= 0);

        var effort = capabilities.Count(entry => entry.Caps.AcceptedReasoningDial == ReasoningDial.Effort);
        var budget = capabilities.Count(entry => entry.Caps.AcceptedReasoningDial == ReasoningDial.TokenBudget);
        var contextEditing = capabilities.Count(entry => entry.Caps.SupportsServerSideContextEditing);
        Expect(effort > 0 && budget > 0, $"reasoning dials: {effort} Effort, {budget} TokenBudget — both kinds should exist");
        Expect(contextEditing > 0, "no model supports server-side context editing");

        var dialWithoutReasoning = capabilities.Where(entry => !entry.Caps.SupportsReasoning && entry.Caps.AcceptedReasoningDial != ReasoningDial.None).Select(entry => entry.Model).ToList();

        return $"{models.Length} models resolve ({uncappedOutput} publish no output cap); dial Effort {effort}, TokenBudget {budget}; server-side context editing {contextEditing}; non-reasoning models with a dial: {dialWithoutReasoning.Count}";
    }

    private async Task<string> SelfTestSpeechCapabilitiesAsync()
    {
        var models = Enum.GetValues<SpeechRecognizerModel>();
        var controls = Enum.GetValues<SpeechEndpointing>().Where(control => control != SpeechEndpointing.None).ToList();
        int wordTimestamps = 0, diarization = 0, semantic = 0, knownRanges = 0, languageLists = 0;

        foreach (var model in models)
        {
            var caps = SpeechRecognizer.GetCapabilities(model);

            Expect(caps.SupportsBatchRecognition || caps.SupportsContinuousRecognition, $"{model} recognizes neither batch nor continuous");
            Expect(caps.SupportsLanguage(""), $"{model} refused an empty language hint");

            foreach (var control in controls)
            {
                var range = caps.RangeOf(control);

                if (!range.IsKnown)
                {
                    continue;
                }

                knownRanges++;
                Expect(caps.Endpointing.HasFlag(control), $"{model} publishes a range for {control} it does not take");
                Expect(range.Minimum <= range.Maximum && range.Accepts(0), $"{model} {control} range {range.Minimum}..{range.Maximum} is inverted or refuses 0");
            }

            if (!caps.Endpointing.HasFlag(SpeechEndpointing.Sensitivity))
            {
                Expect(caps.SensitivityLevels.Count == 0, $"{model} lists sensitivity levels without the Sensitivity control");
            }

            if (caps.Languages.Count > 0)
            {
                languageLists++;
                Expect(caps.SupportsLanguage(caps.Languages[0]), $"{model} does not support its own first language {caps.Languages[0]}");
                Expect(caps.Languages.Any(language => language.StartsWith("zz", StringComparison.OrdinalIgnoreCase)) || !caps.SupportsLanguage("zz-ZZ"), $"{model} claims an unlisted language");
            }

            wordTimestamps += caps.SupportsWordTimestamps ? 1 : 0;
            diarization += caps.SupportsDiarization ? 1 : 0;
            semantic += caps.TurnDetection == SpeechTurnDetection.Semantic ? 1 : 0;
        }

        Expect(wordTimestamps > 0, "no recognizer supports word timestamps");
        Expect(diarization > 0, "no recognizer supports diarization");
        Expect(knownRanges > 0, "no recognizer publishes an endpointing range");

        return $"{models.Length} recognizers: word timestamps {wordTimestamps}, diarization {diarization}, semantic turn detection {semantic}, published ranges {knownRanges}, language lists {languageLists}";
    }

    private async Task<string> SelfTestOcrCapabilitiesAsync()
    {
        var models = Enum.GetValues<OCRModel>();
        var pdf = 0;
        var words = 0;
        var sizes = new List<string>();

        foreach (var model in models)
        {
            var caps = OCR.GetCapabilities(model);
            Expect(caps.SupportedMimeTypes.Count > 0, $"{model} lists no mime types");
            Expect(caps.MaxDocumentSizeBytes >= 0 && caps.MaxPagesSupported >= 0, $"{model} has a negative limit");
            Expect(!string.IsNullOrWhiteSpace(model.DisplayName()), $"{model} has no display name");

            pdf += caps.SupportedMimeTypes.Contains(MimeTypes.ApplicationPdf) ? 1 : 0;
            words += caps.SupportsWordLevelText ? 1 : 0;
            sizes.Add($"{model} {caps.MaxDocumentSizeBytes / (1024 * 1024)} MB");
        }

        Expect(pdf > 0, "no OCR model accepts PDF");
        Expect(words > 0, "no OCR model returns word-level text");

        return $"{models.Length} OCR models: PDF {pdf}, word-level {words}; {string.Join(", ", sizes)}";
    }

    private async Task<string> SelfTestVideoCapabilitiesAsync()
    {
        var models = Enum.GetValues<VideoGeneratorModel>();
        int textToVideo = 0, imageToVideo = 0, takesVideos = 0, takesAudios = 0, promptLimited = 0;

        foreach (var model in models)
        {
            var caps = VideoGenerator.GetCapabilities(model);
            Expect(caps.MaxPromptLength >= 0 && caps.MaxInputVideos >= 0 && caps.MaxInputAudios >= 0 && caps.MaxInputImages >= 0, $"{model} has a negative limit");
            Expect(caps.SupportsTextToVideo || caps.SupportsImageToVideo || caps.MaxInputVideos > 0, $"{model} takes no input of any kind");

            textToVideo += caps.SupportsTextToVideo ? 1 : 0;
            imageToVideo += caps.SupportsImageToVideo ? 1 : 0;
            takesVideos += caps.MaxInputVideos > 0 ? 1 : 0;
            takesAudios += caps.MaxInputAudios > 0 ? 1 : 0;
            promptLimited += caps.MaxPromptLength > 0 ? 1 : 0;
        }

        Expect(textToVideo > 0 && imageToVideo > 0, $"text-to-video {textToVideo}, image-to-video {imageToVideo}");

        return $"{models.Length} video models: text {textToVideo}, image {imageToVideo}, input videos {takesVideos}, input audios {takesAudios}, prompt-length limit {promptLimited}";
    }

    private async Task<string> SelfTestMeshCapabilitiesAsync()
    {
        var models = Enum.GetValues<MeshGeneratorModel>();

        foreach (var model in models)
        {
            var caps = MeshGenerator.GetCapabilities(model);
            Expect(caps.SupportsTextToMesh || caps.SupportsImageToMesh, $"{model} generates from neither text nor image");
            Expect(caps.MinPolycount <= 0 || caps.MaxPolycount <= 0 || caps.MinPolycount <= caps.MaxPolycount, $"{model} polycount {caps.MinPolycount}..{caps.MaxPolycount} is inverted");
            Expect(!string.IsNullOrWhiteSpace(model.DisplayName()), $"{model} has no display name");
        }

        var names = models.Select(model => model.DisplayName()).ToList();
        Expect(names.Distinct().Count() == names.Count, "two mesh models share a display name");

        var rodin = MeshGenerator.GetCapabilities(MeshGeneratorModel.Rodin25Medium);
        Expect(rodin.MaxPolycount > 0, $"Rodin25Medium publishes no polycount ceiling ({rodin.MaxPolycount})");

        return $"{models.Length} mesh models consistent; Rodin25Medium polycount {rodin.MinPolycount}..{rodin.MaxPolycount}, text {rodin.SupportsTextToMesh}, image {rodin.SupportsImageToMesh}, PBR {rodin.SupportsPbr}";
    }

    private async Task<string> SelfTestDeciderCapabilitiesAsync()
    {
        var caps = Decider.GetCapabilities(DecisionModel.Jev);
        Expect(caps.MaxInputTokens > 0, $"Jev MaxInputTokens is {caps.MaxInputTokens}");

        var regions = Decider.GetSupportedRegions(DecisionModel.Jev);
        Expect(regions.Count > 0, "Jev lists no regions");

        return $"Jev takes {caps.MaxInputTokens} input tokens, served in {string.Join(",", regions)}";
    }

    private async Task<string> SelfTestVideoSegmenterModelsAsync()
    {
        var models = Enum.GetValues<VideoSegmenterModel>();
        var described = new List<string>();

        foreach (var model in models)
        {
            var name = model.DisplayName();
            Expect(!string.IsNullOrWhiteSpace(name), $"{model} has no display name");
            Expect(VideoSegmenter.GetSupportedRegions(model).Count > 0, $"{model} lists no regions");
            described.Add($"{model}=\"{name}\"");
        }

        return $"{string.Join(", ", described)} with regions";
    }

    private async Task<string> SelfTestRegionRefusalAsync()
    {
        var candidates = new List<(string Label, IReadOnlyList<ModelRegion> Regions, Action<IReadOnlyList<ModelRegion>> Construct)>();

        foreach (var model in Enum.GetValues<MeshGeneratorModel>())
        {
            candidates.Add(($"MeshGenerator {model}", MeshGenerator.GetSupportedRegions(model), regions => new MeshGenerator(model, regions).Dispose()));
        }

        foreach (var model in Enum.GetValues<VideoGeneratorModel>())
        {
            candidates.Add(($"VideoGenerator {model}", VideoGenerator.GetSupportedRegions(model), regions => new VideoGenerator(model, regions).Dispose()));
        }

        foreach (var model in Enum.GetValues<SpeechRecognizerModel>())
        {
            candidates.Add(($"SpeechRecognizer {model}", SpeechRecognizer.GetSupportedRegions(model), regions => new SpeechRecognizer(model, regions).Dispose()));
        }

        var target = candidates.FirstOrDefault(candidate => candidate.Regions.Count > 0 && candidate.Regions.All(region => !region.ToString().StartsWith("Eu", StringComparison.Ordinal)));
        Expect(target.Label != null, "every mesh, video and speech model has an EU route, so nothing is left to refuse");

        // Constructing resolves the route before any implementation exists, so the refusal is
        // proven without a request leaving the process.
        var refusal = ExpectThrows<NonRetryableAIException>(() => target.Construct([ModelRegion.Eu]), $"{target.Label} asked for EU only");
        Expect(refusal is RegionNotSupportedException or AIRegionPolicyViolationException, $"{target.Label} was refused with {refusal.GetType().Name}");

        var policy = new AIRegionPolicyViolationException("refused by the validation self-test", "validation-model", AIRegionPolicy.EuOnly);
        Expect(policy is NonRetryableAIException && policy.ModelName == "validation-model" && policy.Policy == AIRegionPolicy.EuOnly, "AIRegionPolicyViolationException lost its model or policy");
        Expect(!string.IsNullOrEmpty(AIRegionPolicyViolationException.ErrorCode), "AIRegionPolicyViolationException.ErrorCode is empty");

        return $"{target.Label} (routes {string.Join(",", target.Regions)}) refused an EU-only request with {refusal.GetType().Name} at construction; the organisation-wide AIRegionPolicy is backend-set and not app-settable";
    }

    private async Task<string> SelfTestKernelContextAsync()
    {
        var lookup = CoreFunction.Register<string, string>(query => query, name: "lookup");
        var verdict = CoreFunction.Register<string, string>(query => query, name: "verdict");
        var bulky = new string('x', 4000);

        MessageBlock Result(CoreFunction function) =>
            new(MessageBlockRole.FunctionResult,
                [new FunctionResultPart(new FunctionCall(function, ["q"], "{\"query\":\"q\"}", Guid.NewGuid().ToString("N"), "validation-selftest"), [], bulky)]);

        var context = new KernelContext()
            .Add(new MessageBlock(MessageBlockRole.User, new string('u', 400)))
            .Add(Result(lookup))
            .Add(Result(verdict))
            .Add(Result(lookup))
            .Add(Result(lookup));

        var before = context.EstimateInputTokens();
        Expect(before >= 100 + 4 * 1000, $"EstimateInputTokens gave {before} for 400 chars of text and four 4000-char results");

        var cleared = context.ClearOldFunctionResults(targetTokens: 500, keep: 1, excludedTools: ["verdict"], out var clearedCount);
        var after = cleared.EstimateInputTokens();
        var results = cleared.Messages.SelectMany(message => message.Parts).OfType<FunctionResultPart>().ToList();

        Expect(clearedCount == 2, $"cleared {clearedCount} results instead of the two older lookups");
        Expect(KernelContext.IsClearedResult(results[0].Result) && KernelContext.IsClearedResult(results[2].Result), "the older lookup results were not replaced by cleared stubs");
        Expect(!KernelContext.IsClearedResult(results[1].Result), "an excluded tool's result was cleared");
        Expect(!KernelContext.IsClearedResult(results[3].Result), "the newest kept result was cleared");
        Expect(after < before, $"clearing did not lower the estimate ({before} → {after})");
        Expect(!KernelContext.IsClearedResult(bulky) && !KernelContext.IsClearedResult(42), "IsClearedResult flagged ordinary results");

        var again = cleared.ClearOldFunctionResults(targetTokens: 0, keep: 1, excludedTools: ["verdict"], out var clearedAgain);
        Expect(clearedAgain == 0 && again.EstimateInputTokens() == after, "stubs were cleared a second time");

        return $"estimate {before} → {after} tokens; two old lookups cleared, the excluded verdict and the kept newest intact; stubs never re-cleared";
    }

    private async Task<string> SelfTestImageProvenanceAsync()
    {
        const int size = 192;
        var rgba = new byte[size * size * 4];
        var random = new Random(1234);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = (y * size + x) * 4;
                rgba[index] = (byte)(60 + x * 120 / size);
                rgba[index + 1] = (byte)(80 + y * 100 / size);
                rgba[index + 2] = (byte)(100 + (x + y) * 30 / size + random.Next(-3, 4));
                rgba[index + 3] = 255;
            }
        }

        byte[] png;

        using (var stream = new MemoryStream())
        {
            new StbImageWriteSharp.ImageWriter().WritePng(rgba, size, size, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
            png = stream.ToArray();
        }

        Expect(ImageProvenance.GetMarkingSupport(png) == ProvenanceMarking.Full, $"a PNG reports {ImageProvenance.GetMarkingSupport(png)} marking support");
        Expect(ImageProvenance.GetMarkingSupport([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]) == ProvenanceMarking.None, "unrecognised bytes report marking support");
        Expect(ImageProvenance.ReadMetadataMark(png) == null, "an unmarked PNG carries a metadata mark");

        var unmarkedScore = ImageProvenance.MeasureInvisibleMark(png);
        var marked = ImageProvenance.Apply(png, "validation-selftest-model", invisibleWatermark: true, visibleWatermark: "", out var marking);
        var markedScore = ImageProvenance.MeasureInvisibleMark(marked);
        var metadata = ImageProvenance.ReadMetadataMark(marked);

        Expect(marking == ProvenanceMarking.Full, $"Apply reported {marking}");
        Expect(metadata != null && metadata.Contains("validation-selftest-model", StringComparison.Ordinal) && metadata.Contains("trainedAlgorithmicMedia", StringComparison.Ordinal), "the XMP packet does not name the model and the IPTC source type");
        Expect(markedScore >= ImageProvenance.DetectionThreshold, $"the marked image scores {markedScore:F1}, under the {ImageProvenance.DetectionThreshold} threshold");
        Expect(Math.Abs(unmarkedScore) < ImageProvenance.DetectionThreshold, $"the unmarked image scores {unmarkedScore:F1}");

        return $"{size}x{size} PNG marked Full: watermark score {unmarkedScore:F1} → {markedScore:F1} (threshold {ImageProvenance.DetectionThreshold}), XMP names the model";
    }

    private async Task<string> SelfTestMediaProvenanceAsync()
    {
        var samples = SelfTestTone(8000, 0.1, 440, 0.4f);
        var wav = SelfTestWav(samples, 8000);

        Expect(MediaProvenance.GetMarkingSupport(wav) == ProvenanceMarking.MetadataOnly, $"a WAV reports {MediaProvenance.GetMarkingSupport(wav)}");
        Expect(MediaProvenance.GetMarkingSupport(new byte[64]) == ProvenanceMarking.None, "raw bytes report a markable container");
        Expect(MediaProvenance.ReadMetadataMark(wav) == null, "an unmarked WAV carries a mark");

        var marked = MediaProvenance.Apply(wav, "validation-selftest-model", out var marking);
        Expect(marking == ProvenanceMarking.MetadataOnly, $"Apply reported {marking}");
        Expect(marked.Length > wav.Length && Encoding.ASCII.GetString(marked, 0, 4) == "RIFF" && Encoding.ASCII.GetString(marked, 8, 4) == "WAVE", "the marked file is not a larger RIFF/WAVE");
        Expect(MediaProvenance.ReadMetadataMark(marked)?.Contains("validation-selftest-model", StringComparison.Ordinal) == true, "the _PMX packet does not name the model");

        var raw = new byte[256];
        var passThrough = MediaProvenance.Apply(raw, SelfTestDeliberate, out var rawMarking);
        Expect(rawMarking == ProvenanceMarking.None && passThrough.Length == raw.Length, "raw PCM was reported as marked");

        return $"WAV {wav.Length} → {marked.Length} bytes with a _PMX XMP chunk naming the model; raw bytes pass through as None";
    }

    private async Task<string> SelfTestMuLawAsync()
    {
        var samples = SelfTestTone(8000, 0.05, 300, 0.5f);
        var encoded = MuLawCodec.Encode(samples);
        var decoded = MuLawCodec.Decode(encoded);

        Expect(encoded.Length == samples.Length && decoded.Length == samples.Length, $"lengths {samples.Length} → {encoded.Length} → {decoded.Length}");

        var maxError = samples.Zip(decoded, (original, roundTripped) => Math.Abs(original - roundTripped)).Max();
        Expect(maxError < 0.03f, $"round-trip error {maxError:F4} exceeds 0.03");

        var clipped = MuLawCodec.Decode(MuLawCodec.Encode([2f, -2f, 0f]));
        Expect(clipped[0] > 0.95f && clipped[1] < -0.95f && Math.Abs(clipped[2]) < 0.001f, $"clamping gave [{string.Join(",", clipped.Select(value => value.ToString("F3")))}]");
        Expect(decoded.All(value => value >= -1f && value <= 1f), "a decoded sample left [-1, 1]");

        return $"{samples.Length} samples round-tripped with max error {maxError:F4}; ±2 clamps to ±1, 0 stays silent";
    }

    private async Task<string> SelfTestBargeInAsync()
    {
        var detector = new BargeInDetector(sustainedFrames: 3, graceMs: 300);
        var speech = SelfTestTone(16000, 0.02, 220, 0.3f);
        var silence = new float[speech.Length];

        bool IsSpeech(float[] frame) => Math.Sqrt(frame.Average(sample => sample * sample)) > 0.02;

        Expect(IsSpeech(speech) && !IsSpeech(silence), "the synthetic energy gate cannot tell tone from silence");

        Expect(!detector.ShouldInterrupt(IsSpeech(speech), agentSpeaking: true, msSinceSpeakStart: 100), "speech inside the grace window interrupted");
        Expect(!detector.ShouldInterrupt(IsSpeech(speech), agentSpeaking: false, msSinceSpeakStart: 1000), "speech while the agent is silent interrupted");

        Expect(!detector.ShouldInterrupt(IsSpeech(speech), true, 400) && !detector.ShouldInterrupt(IsSpeech(speech), true, 420), "fewer than three sustained frames interrupted");
        Expect(!detector.ShouldInterrupt(IsSpeech(silence), true, 440), "a silent frame interrupted");
        Expect(!detector.ShouldInterrupt(IsSpeech(speech), true, 460) && !detector.ShouldInterrupt(IsSpeech(speech), true, 480), "the silent frame did not reset the count");
        Expect(detector.ShouldInterrupt(IsSpeech(speech), true, 500), "three sustained speech frames past the grace window did not interrupt");
        Expect(!detector.ShouldInterrupt(IsSpeech(speech), true, 520), "the count did not reset after an interrupt");

        detector.ShouldInterrupt(IsSpeech(speech), true, 540);
        detector.Reset();
        Expect(!detector.ShouldInterrupt(IsSpeech(speech), true, 560) && !detector.ShouldInterrupt(IsSpeech(speech), true, 580), "Reset did not clear the count");

        return "grace window and a silent agent never interrupt; three sustained 20 ms tone frames do; silence and Reset clear the count";
    }

    private async Task<string> SelfTestSpeechMixerAsync()
    {
        const int sampleRate = 48000;
        await using var mixer = new SpeechMixer();
        using var streaming = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var frames = 0;
        var frameRate = 0;

        var consumer = Task.Run(async () =>
        {
            try
            {
                await foreach (var frame in mixer.StreamAsync(streaming.Token))
                {
                    frames++;
                    frameRate = frame.SampleRate;
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelling is how this consumer is told to stop; the frames counted so far stand.
            }
        });

        ExpectThrows<ArgumentException>(() => mixer.AddSamples(new ResonanceAudioChunk("validation-selftest-bad", new float[480], 0, 1, true, true)), "AddSamples with a zero sample rate");
        Expect(mixer.GetBufferedDuration("validation-selftest-unknown") == TimeSpan.Zero, "an unknown event reports buffered audio");

        mixer.AddSamples(new ResonanceAudioChunk("validation-selftest-long", SelfTestTone(sampleRate, 2.0, 330, 0.2f), sampleRate, 1, true, false));
        var buffered = mixer.GetBufferedDuration("validation-selftest-long");
        Expect(buffered > TimeSpan.Zero, "a two-second event reports nothing buffered");

        await Task.Delay(100);
        mixer.AddSamples(new ResonanceAudioChunk("validation-selftest-short", SelfTestTone(sampleRate, 0.1, 440, 0.2f), sampleRate, 1, true, true));

        var longOutcome = await mixer.WaitForCompletionAsync("validation-selftest-long").WaitAsync(TimeSpan.FromSeconds(3));
        var shortOutcome = await mixer.WaitForCompletionAsync("validation-selftest-short").WaitAsync(TimeSpan.FromSeconds(4));

        streaming.Cancel();
        await consumer.WaitAsync(TimeSpan.FromSeconds(2));

        Expect(longOutcome == SpeechEventOutcome.Interrupted, $"the long event ended {longOutcome} after a new event id arrived");
        Expect(shortOutcome == SpeechEventOutcome.Completed, $"the short event ended {shortOutcome}");
        Expect(frames > 0 && frameRate > 0, $"the mixer streamed {frames} frames at {frameRate} Hz");

        return $"buffered {buffered.TotalMilliseconds:0} ms; a new event id interrupted the long one and the short one completed; {frames} frames at {frameRate} Hz; zero sample rate refused";
    }

    private static float[] SelfTestTone(int sampleRate, double seconds, double frequency, float amplitude)
    {
        var samples = new float[(int)(sampleRate * seconds)];

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = amplitude * (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }

        return samples;
    }

    private static byte[] SelfTestWav(float[] samples, int sampleRate)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Length * 2;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            writer.Write((short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static string SelfTestContextText(KernelContext context)
    {
        var text = new StringBuilder();

        foreach (var instruction in context.Instructions)
        {
            text.AppendLine(instruction.Content);
        }

        foreach (var message in context.Messages)
        {
            foreach (var part in message.Parts)
            {
                if (part is TextPart textPart)
                {
                    text.AppendLine(textPart.Content);
                }
            }
        }

        return text.ToString();
    }
}
