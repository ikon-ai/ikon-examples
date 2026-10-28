using System.Reflection;
using System.Runtime.CompilerServices;

public partial class Validation
{
    // The space secret the own-key check runs on. Groq because its GPT-OSS 120B route is the cheapest
    // own-key LLM route, and Groq has no EU route that a region policy could switch the call to.
    private const string OwnKeySecret = "IKON_GROQ_API_KEY";
    private const LLMModel OwnKeyModel = LLMModel.GptOss120B;
    private const string OwnKeyApiModelName = "gpt-oss-120b";

    private const string ProviderToolName = "validation_forecast";
    private const string ProviderToolCity = "Oulu";
    private const string ProviderReply = "Sunny in Oulu";
    private static readonly TimeSpan AILogDrainTimeout = TimeSpan.FromSeconds(30);

    private readonly ClientReactive<bool> _aiCatalogRunning = new(false);
    private readonly ClientReactive<string?> _aiCatalogResult = new(null);
    private readonly ClientReactive<bool> _aiProviderClassRunning = new(false);
    private readonly ClientReactive<string?> _aiProviderClassResult = new(null);
    private readonly ClientReactive<bool> _aiOwnKeyRunning = new(false);
    private readonly ClientReactive<string?> _aiOwnKeyResult = new(null);

    private void RenderAIProvidersCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-providers-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "AI providers");

            RenderAIProviderCheck(view, "Read the model catalog", "Free · GET /ai/catalog for this organisation",
                "ai-catalog-run", "ai-catalog-result", _aiCatalogRunning, _aiCatalogResult, CheckAICatalogAsync);
            RenderAIProviderCheck(view, "Run an app provider class", "Free · an in-process LLM and embedding provider, called by name",
                "ai-provider-class-run", "ai-provider-class-result", _aiProviderClassRunning, _aiProviderClassResult, CheckProviderClassAsync);
            RenderAIProviderCheck(view, "Run on the space's own key", $"One {OwnKeyModel} call on the space secret {OwnKeySecret}",
                "ai-own-key-run", "ai-own-key-result", _aiOwnKeyRunning, _aiOwnKeyResult, CheckOwnKeyRouteAsync);
        });
    }

    private static void RenderAIProviderCheck(UIView view, string label, string caption, string runTestId, string resultTestId,
        ClientReactive<bool> running, ClientReactive<string?> result, Func<Task<string>> check)
    {
        view.Column([Layout.Column.Xs, "mb-4"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.OutlineMd],
                    text: label,
                    props: TestId(runTestId),
                    disabled: running.Value,
                    onClick: async () =>
                    {
                        if (running.Value)
                        {
                            return;
                        }

                        running.Value = true;
                        result.Value = null;

                        try
                        {
                            result.Value = await check();
                        }
                        catch (Exception ex)
                        {
                            var (kind, message) = DescribeAIFailure(ex);
                            result.Value = $"FAIL {kind}: {ex.GetType().Name}: {message}";
                        }
                        finally
                        {
                            running.Value = false;
                        }
                    });

                if (running.Value)
                {
                    view.Spinner();
                }
            });

            view.Text([Text.Caption], caption);

            if (result.Value is { } text)
            {
                var tone = text.StartsWith("PASS", StringComparison.Ordinal) ? "text-success-primary"
                    : text.StartsWith("SKIP", StringComparison.Ordinal) ? "text-warning-primary"
                    : "text-error-primary";
                view.Text([Text.Body, "whitespace-pre-wrap", tone], text, props: TestId(resultTestId));
            }
        });
    }

    private static async Task<string> CheckAICatalogAsync()
    {
        var fetch = await IkonBackend.Instance.GetAICatalogAsync(ifNoneMatch: null);

        if (fetch.Body is null)
        {
            return "FAIL catalog: the backend answered 304 to a request that sent no ETag";
        }

        using var document = JsonDocument.Parse(fetch.Body);
        var root = document.RootElement;
        var tier = StringProperty(root, "tier") ?? "(none)";
        var hostVersion = StringProperty(root, "ikonAIVersion");
        var appVersion = AppIkonAIVersion();
        var kinds = new SortedSet<string>(StringComparer.Ordinal);
        var models = 0;
        var pricedRoutes = 0;
        var ownKeyPricedRoutes = 0;

        if (root.TryGetProperty("models", out var modelList) && modelList.ValueKind == JsonValueKind.Array)
        {
            foreach (var model in modelList.EnumerateArray())
            {
                models++;

                if (StringProperty(model, "kind") is { } kind)
                {
                    kinds.Add(kind);
                }

                if (!model.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var route in routes.EnumerateArray())
                {
                    if (!route.TryGetProperty("usages", out var usages) || usages.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var usageList = usages.EnumerateArray().ToList();

                    if (usageList.Any(usage => PositiveNumber(usage, "credits")))
                    {
                        pricedRoutes++;
                    }

                    if (usageList.Any(usage => PositiveNumber(usage, "ownKeyCredits")))
                    {
                        ownKeyPricedRoutes++;
                    }
                }
            }
        }

        var failures = new List<string>();

        if (kinds.Count < 3)
        {
            failures.Add($"models of {kinds.Count} kinds ({string.Join(", ", kinds)}), expected several");
        }

        if (pricedRoutes == 0)
        {
            failures.Add("no route has a usage priced above 0 credits");
        }

        if (ownKeyPricedRoutes == 0)
        {
            failures.Add("no route has an own-key price above 0 credits");
        }

        if (string.IsNullOrEmpty(fetch.ETag))
        {
            failures.Add("no ETag");
        }

        if (hostVersion is null)
        {
            failures.Add("no ikonAIVersion: the AI host has posted no manifest");
        }
        else if (!AreNearVersions(hostVersion, appVersion))
        {
            failures.Add($"ikonAIVersion {hostVersion} is not near this app's Ikon.AI {appVersion}");
        }

        var summary = $"{models} models of {kinds.Count} kinds, {pricedRoutes} priced routes, {ownKeyPricedRoutes} with own-key prices, tier {tier}, AI host Ikon.AI {hostVersion ?? "?"} (app {appVersion})";

        if (failures.Count > 0)
        {
            return $"FAIL catalog: {string.Join("; ", failures)}\n{summary}";
        }

        // The ETag covers the prices, which only change with the catalog, so a read right after must be a 304
        var again = await IkonBackend.Instance.GetAICatalogAsync(fetch.ETag);

        if (!again.NotModified)
        {
            return $"FAIL catalog: a second read with ETag {fetch.ETag} was not answered 304\n{summary}";
        }

        return $"PASS catalog: {summary}; a read with its ETag answered 304";
    }

    private static async Task<string> CheckProviderClassAsync()
    {
        if (AIMocksSwitchedOn())
        {
            return "SKIP provider class: this run is on the AI mocks (ikon run --mock-ai), which answer before any provider";
        }

        var name = $"validation-provider-{Guid.NewGuid():N}"[..28];
        var provider = new ValidationModelProvider();
        CustomModels.Instance.Register(name, provider, ModelRegion.Eu);

        try
        {
            string? toolCity = null;
            var streamedDeltas = 0;
            var streamedText = "";
            string reply = "";
            List<float[]> vectors = [];

            using var capture = new AICallCapture(fragment: $".{name}.");

            reply = await Emerge.Run<string>(name, pass =>
            {
                pass.Command = $"What is the weather in {ProviderToolCity}? Use the tool.";
                pass.MaxIterations = 3;
                pass.Tools.Clear();
                pass.AddTool(Tool.Of(ProviderToolName, "The weather forecast for a city", (string city) =>
                {
                    toolCity = city;
                    return $"Sunny in {city}";
                }));
            });

            var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, "Say the forecast"));

            await foreach (var llmEvent in Emerge.Generate(name, context))
            {
                if (llmEvent is LLMEvent.TextDelta delta)
                {
                    streamedDeltas++;
                    streamedText += delta.Text;
                }
            }

            using (var embeddings = new EmbeddingGenerator(name))
            {
                vectors = await embeddings.EmbedAsync(["one", "two", "three"]);
            }

            await capture.DrainAsync();

            var failures = new List<string>();
            var llmRequests = capture.UsageMatching($"llm.custom.{name}.", ".requests.user");
            var embeddingRequests = capture.UsageMatching($"embeddings.custom.{name}.", ".requests.user");
            var succeeded = capture.EventsNamed("llm_succeeded");
            var usagePrefixes = succeeded.Select(parameters => ParameterText(parameters, "usagePrefix")).Distinct().ToList();

            if (toolCity != ProviderToolCity)
            {
                failures.Add($"the tool ran with city {toolCity ?? "(never)"}, expected {ProviderToolCity}");
            }

            if (reply.Trim() != ProviderReply)
            {
                failures.Add($"Emerge.Run replied \"{Truncate(reply.Trim(), 60)}\", expected \"{ProviderReply}\"");
            }

            if (streamedDeltas < 2 || streamedText != ProviderReply)
            {
                failures.Add($"Emerge.Generate streamed {streamedDeltas} deltas \"{streamedText}\", expected the provider's 3 deltas of \"{ProviderReply}\"");
            }

            if (vectors.Count != 3 || vectors.Any(vector => vector.Length != ValidationModelProvider.VectorSize))
            {
                failures.Add($"embeddings returned {vectors.Count} vectors of lengths {string.Join("/", vectors.Select(vector => vector.Length))}");
            }

            if (provider.EmbedBatches != 2)
            {
                failures.Add($"3 inputs reached the provider in {provider.EmbedBatches} batches, expected 2 of at most {ValidationModelProvider.MaxInputs}");
            }

            if (llmRequests != provider.Turns || provider.Turns != 3)
            {
                failures.Add($"billed llm.custom.{name}.*.requests.user {llmRequests} for {provider.Turns} provider turns, expected 3 for 3");
            }

            if (embeddingRequests != 2)
            {
                failures.Add($"billed embeddings.custom.{name}.*.requests.user {embeddingRequests}, expected 2");
            }

            if (succeeded.Count == 0 || usagePrefixes.Any(prefix => prefix?.StartsWith($"llm.custom.{name}.", StringComparison.Ordinal) != true))
            {
                failures.Add($"llm_succeeded events: {succeeded.Count}, usagePrefix {string.Join(", ", usagePrefixes)}, expected llm.custom.{name}.*");
            }

            if (succeeded.Any(parameters => ParameterText(parameters, "isRemote") == "true"))
            {
                failures.Add("an llm_succeeded event says isRemote, but an app provider runs in the app");
            }

            var summary = $"tool bound ({toolCity}), {streamedDeltas} deltas streamed, {provider.Turns} LLM turns billed {llmRequests} under {usagePrefixes.FirstOrDefault()}, {provider.EmbedBatches} embedding batches billed {embeddingRequests}";

            return failures.Count > 0
                ? $"FAIL provider class: {string.Join("; ", failures)}\n{summary}"
                : $"PASS provider class: {summary}";
        }
        finally
        {
            CustomModels.Instance.Unregister(name);
        }
    }

    private async Task<string> CheckOwnKeyRouteAsync()
    {
        if (!app.Secrets.TryGet(OwnKeySecret, out var key) || string.IsNullOrWhiteSpace(key))
        {
            return $"SKIP own key: the space has no secret {OwnKeySecret} (a person stores a Groq key with `ikon secret set {OwnKeySecret} --stdin`, see the app's AGENTS.md)";
        }

        if (AIMocksSwitchedOn())
        {
            return "SKIP own key: this run is on the AI mocks (ikon run --mock-ai), which reach no provider";
        }

        using var capture = new AICallCapture(fragment: OwnKeyApiModelName);
        string reply;

        try
        {
            reply = await Emerge.AskAsync($"Reply with the single word OK. ({Guid.NewGuid():N})", OwnKeyModel);
        }
        catch (Exception ex) when (FindInChain<AIRegionPolicyViolationException>(ex) is { } policy)
        {
            return $"SKIP own key: the organisation's {policy.Policy} policy refuses {OwnKeyModel}, whose only routes are global";
        }

        await capture.DrainAsync();

        var outcome = capture.EventsNamed("llm_succeeded").LastOrDefault();

        if (outcome is null)
        {
            return $"FAIL own key: the call answered \"{Truncate(reply.Trim(), 40)}\" but logged no llm_succeeded event for {OwnKeyApiModelName}";
        }

        var usagePrefix = ParameterText(outcome, "usagePrefix") ?? "";
        var isRemote = ParameterText(outcome, "isRemote");
        var isUserCredential = ParameterText(outcome, "isUserCredential");
        var ownKeyUsage = capture.UsageMatching("llm.groq.", ".user");
        var platformUsage = capture.UsageMatching("llm.", "", excludeSuffix: ".user");
        var summary = $"modelName {ParameterText(outcome, "modelName")}, usagePrefix {usagePrefix}, isRemote {isRemote}, isUserCredential {isUserCredential}, own-key usage {ownKeyUsage:0.##}";
        var failures = new List<string>();

        if (isRemote != "false")
        {
            failures.Add("the call ran on the AI host, not in the app");
        }

        if (isUserCredential != "true")
        {
            failures.Add("the call did not run on the space's own key");
        }

        if (!usagePrefix.StartsWith("llm.groq.", StringComparison.Ordinal))
        {
            failures.Add("the call was not served by the Groq route");
        }

        if (ownKeyUsage <= 0)
        {
            failures.Add("no llm.groq.*.user usage was logged");
        }

        if (platformUsage > 0)
        {
            failures.Add($"{platformUsage:0.##} usage was logged at the platform's price (no .user suffix)");
        }

        return failures.Count > 0
            ? $"FAIL own key: {string.Join("; ", failures)}\n{summary}"
            : $"PASS own key: {summary}";
    }

    private static bool AIMocksSwitchedOn()
    {
        var value = Environment.GetEnvironmentVariable("IKON_MOCK_AI")?.Trim().ToLowerInvariant();
        return value is "on" or "true" or "1";
    }

    private static T? FindInChain<T>(Exception exception) where T : Exception
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    private static string AppIkonAIVersion()
    {
        var version = typeof(CustomModels).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plusIndex = version.IndexOf('+');
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }

    // The AI host is redeployed nightly and this app on every battery run, so across a release the two
    // can be a minor version apart for up to a day
    private static bool AreNearVersions(string hostVersion, string appVersion)
    {
        if (hostVersion == appVersion)
        {
            return true;
        }

        if (!TryParseMajorMinor(hostVersion, out var host) || !TryParseMajorMinor(appVersion, out var local))
        {
            return false;
        }

        return host.Major == local.Major && Math.Abs(host.Minor - local.Minor) <= 1;
    }

    private static bool TryParseMajorMinor(string version, out (int Major, int Minor) parsed)
    {
        parsed = default;
        var parts = version.Split('-', 2)[0].Split('.');

        if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        parsed = (major, minor);
        return true;
    }

    private static string? StringProperty(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool PositiveNumber(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.GetDouble() > 0;

    private static string? ParameterText(IReadOnlyDictionary<string, object?> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            bool flag => flag ? "true" : "false",
            JsonElement { ValueKind: JsonValueKind.True } => "true",
            JsonElement { ValueKind: JsonValueKind.False } => "false",
            _ => value.ToString(),
        };
    }

    /// <summary>Collects the usage and call events that this session's log sends to its handlers while the
    /// capture is open. It keeps only those whose usage name or model name contains the fragment, so the
    /// session's other calls are left out.</summary>
    private sealed class AICallCapture : IDisposable
    {
        private readonly Log _log;
        private readonly string _fragment;
        private readonly ConcurrentDictionary<string, double> _usages = new(StringComparer.Ordinal);
        private readonly ConcurrentQueue<(string Name, IReadOnlyDictionary<string, object?> Parameters)> _events = new();

        public AICallCapture(string fragment)
        {
            _log = Log.Instance;
            _fragment = fragment;
            _log.LogEvent += OnLogEvent;
        }

        public void Dispose()
        {
            _log.LogEvent -= OnLogEvent;
        }

        public double UsageMatching(string prefix, string suffix, string? excludeSuffix = null)
            => _usages
                .Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal) && entry.Key.EndsWith(suffix, StringComparison.Ordinal)
                                && (excludeSuffix is null || !entry.Key.EndsWith(excludeSuffix, StringComparison.Ordinal)))
                .Sum(entry => entry.Value);

        public List<IReadOnlyDictionary<string, object?>> EventsNamed(string name)
            => _events.Where(entry => entry.Name == name).Select(entry => entry.Parameters).ToList();

        // The log sends queued lines to its handlers one at a time, in order, so when a marker added now
        // arrives, every usage and event logged before it has been handled
        public async Task DrainAsync()
        {
            var marker = $"validation-ai-capture-{Guid.NewGuid():N}";
            var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnMarker(object sender, LogEvent logEvent)
            {
                if (logEvent.Message == marker)
                {
                    arrived.TrySetResult();
                }
            }

            _log.LogEvent += OnMarker;

            try
            {
                _log.AddLogEvent(new LogEvent
                {
                    Time = DateTime.UtcNow,
                    Type = LogType.Trace,
                    Message = marker,
                    Path = nameof(AICallCapture),
                    MemberName = nameof(DrainAsync),
                });

                await arrived.Task.WaitAsync(AILogDrainTimeout);
            }
            finally
            {
                _log.LogEvent -= OnMarker;
            }
        }

        private void OnLogEvent(object sender, LogEvent logEvent)
        {
            if (logEvent.Type == LogType.Usage)
            {
                if (logEvent.UsageName.Contains(_fragment, StringComparison.Ordinal))
                {
                    _usages.AddOrUpdate(logEvent.UsageName, logEvent.Usage, (_, existing) => existing + logEvent.Usage);
                }

                return;
            }

            if (string.IsNullOrEmpty(logEvent.EventName))
            {
                return;
            }

            var parameters = logEvent.GetParameters();

            if (parameters.TryGetValue("modelName", out var modelName) && modelName?.ToString()?.Contains(_fragment.Trim('.'), StringComparison.Ordinal) == true)
            {
                _events.Enqueue((logEvent.EventName, parameters));
            }
        }
    }

    /// <summary>An app's own model: a scripted LLM that calls the forecast tool once and then answers
    /// from its result, and an embedding model whose vectors hold their input's position.</summary>
    private sealed class ValidationModelProvider : ILLMProvider, IEmbeddingProvider
    {
        public const int VectorSize = 4;
        public const int MaxInputs = 2;

        private int _turns;
        private int _embedBatches;
        private int _embeddedInputs;

        public LLMCapabilities Capabilities { get; } = new()
        {
            ContextWindowSize = 8192,
            SupportsStreaming = true,
            SupportsSingleToolCalling = true,
        };

        public int Turns => Volatile.Read(ref _turns);

        public int EmbedBatches => Volatile.Read(ref _embedBatches);

        int IEmbeddingProvider.EmbeddingVectorSize => VectorSize;

        int IEmbeddingProvider.MaxInputCount => MaxInputs;

        public async IAsyncEnumerable<LLMProviderOutput> GenerateAsync(KernelContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _turns);
            await Task.Yield();

            var toolAnswered = context.Messages.Any(message => message.Role == MessageBlockRole.FunctionResult);

            if (!toolAnswered && context.Functions.ContainsKey(ProviderToolName))
            {
                yield return new LLMProviderOutput.ToolCall(ProviderToolName, $$"""{"city":"{{ProviderToolCity}}"}""");
                yield return new LLMProviderOutput.Usage(InputTokens: 24, OutputTokens: 6);
                yield break;
            }

            yield return new LLMProviderOutput.Text("Sunny ");
            yield return new LLMProviderOutput.Text("in ");
            yield return new LLMProviderOutput.Text(ProviderToolCity);
            yield return new LLMProviderOutput.Usage(InputTokens: 30, OutputTokens: 3);
        }

        public Task<IReadOnlyList<float[]>> EmbedAsync(EmbeddingGeneratorConfig batch, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _embedBatches);

            IReadOnlyList<float[]> vectors = batch.Inputs.Select(_ =>
            {
                var vector = new float[VectorSize];
                vector[0] = Interlocked.Increment(ref _embeddedInputs);
                return vector;
            }).ToList();

            return Task.FromResult(vectors);
        }
    }
}
