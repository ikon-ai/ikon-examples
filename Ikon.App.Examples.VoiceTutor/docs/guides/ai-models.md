# AI Models & LLM

## AI Models & LLM Connection

LLM model selection, connection configuration, and core AI infrastructure.

---

# Ikon.AI Public API
namespace Ikon.AI
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = null)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
    // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
    ctor(GovernanceAction Action, string DecisionId, string RuleId, string PolicyId, string Reason, string? Target = null)
    GovernanceAction Action { get; init; }
    string DecisionId { get; init; }
    string PolicyId { get; init; }
    string Reason { get; init; }
    string RuleId { get; init; }
    string? Target { get; init; }
  // AsyncLocal scope carrying the active IGovernanceHook for the duration of an AI-touched operation. Host code wraps work in using var _ = GovernanceScope.Use(hook);; downstream Ikon AI primitives read Current and apply the hook if present. The scope crosses await boundaries naturally; it does NOT cross Task.Run or manually-started threads. Capture the hook into a local before any fork if you need to.
  static class GovernanceScope
    static IGovernanceHook? Current { get; }
    static IDisposable Use(IGovernanceHook hook)
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent2), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Central configuration for SDK connection to the Ikon.AI function host. Uses BackendConfig mode (IkonBackend.Instance token) for authentication. Inherits from AsyncLocalInstance to support proper async local flow in tests and apps.
  class IkonAIConnection : AsyncLocalInstance<IkonAIConnection>
    ctor()
    IkonClientConfig? ConfigOverride { get; set; }
    Task ForceReconnectAsync(CancellationToken ct = null)
    // Gets or creates an IkonClient connected to the Ikon.AI function host. The client is cached per instance to avoid connection overhead on each call. If the client is reconnecting, waits for reconnection to complete.
    Task<IkonClient> GetOrCreateClientAsync(CancellationToken ct = null)
    // Pre-establishes the connection to the host app so that subsequent function calls do not incur connection setup latency.
    Task WarmupAsync(CancellationToken ct = null)
    static string ChannelKey
    static string DevelopmentSpaceId
    static string ExternalUserId
    static string ProductionSpaceId
  class ImplementationSelector : AsyncLocalInstance<ImplementationSelector>
    ctor()
    bool ForceLocal { get; set; }
    bool ForceRemote { get; set; }
  enum ModelCategory
    Classifier
    DepthEstimator
    Embeddings
    FileConverter
    ImageGenerator
    ImageSegmenter
    LLM
    MeshGenerator
    MusicGenerator
    OCR
    Reranker
    SoundEffectGenerator
    SpeechGenerator
    SpeechRecognizer
    VideoEnhancer
    VideoGenerator
    WebScraper
    WebSearcher
  // JSON converter factory that handles deserialization of legacy model enum formats. Supports both the current enum names (e.g., "OpenAI3Small") and legacy canonical names (e.g., "OpenAI_3Small").
  class ModelEnumConverterFactory : JsonConverterFactory
    ctor()
    override bool CanConvert(Type typeToConvert)
    override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  enum ModelRegion
    Global
    Eu
    EuNorth
    EuWest
    EuCentral
    EuSouth
    Us
    UsEast
    UsWest
  struct ModelRegionPriorityKey : IEquatable<ModelRegionPriorityKey>
    ctor(ModelCategory category, Organization organization, string modelFamilyName)
    ModelCategory Category { get; }
    string ModelFamilyName { get; }
    Organization Organization { get; }
  static class ModelRegionSelector
    static void SetPriorityList(ModelRegionPriorityKey key, IReadOnlyList<ModelRegion> priorities)
    static bool TryGetPriorityList(ModelRegionPriorityKey key, out IReadOnlyList<ModelRegion> priorities)
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  enum Organization
    None
    AI21
    Anthropic
    AssemblyAI
    Aws
    Azure
    BlackForestLabs
    Cerebras
    Cohere
    ConvertApi
    DeepInfra
    Deepgram
    ElevenLabs
    Fal
    Fireworks
    Google
    Groq
    Hyperbolic
    Ikon
    Jina
    Meshy
    Mistral
    OpenAI
    OpenRouter
    Pollo
    SerpApi
    Spider
    Stability
    TensorPix
    Together
    Voyage
    XAI

namespace Ikon.AI.Kernel
  sealed class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049
    Task<T1[]> AsArrayAsync<T1>()
    Task<T1> AsFirstAsync<T1>()
    Task<string> AsStringAsync()
    IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  static class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049.<M>$7325656A85ACD35A95DB91A9468B406C
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<T1> AsFirstAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<string> AsStringAsync(IAsyncEnumerable<StreamingResult> source)
    static IAsyncEnumerable<StreamingResult> WithCitationsAsync(IAsyncEnumerable<StreamingResult> source, IdMapper idMapper)
    static IAsyncEnumerable<StreamingResult> WithParsedTagsAsync(IAsyncEnumerable<StreamingResult> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<StreamingResult> WithReasoningFromTagAsync(IAsyncEnumerable<StreamingResult> source, string reasoningTagName)
    static IAsyncEnumerable<StreamingResult> WithThrottlingAsync(IAsyncEnumerable<StreamingResult> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = null)
    static IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(IAsyncEnumerable<StreamingResult> source, Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class Citation
    ctor(string originalId, string mappedId, int referStartIndex, int referEndIndex, int positionIndex)
    string MappedId { get; }
    string OriginalId { get; }
    int PositionIndex { get; }
    int ReferEndIndex { get; }
    int ReferStartIndex { get; }
  class FinalModelMessage
    ctor(string text)
    string Text { get; }
  class FinalTextResponse
    ctor(string text)
    string Text { get; }
  class FinishReason
    ctor(string reason)
    string Reason { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string? ModelMessagePrefix { get; set; }
    string? ModelMessageSuffix { get; set; }
    object? Result { get; set; }
  struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, StreamingResult[] streamingResults, object result)
    FunctionCall FunctionCall { get; }
    object Result { get; }
    StreamingResult[] StreamingResults { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  class JsonExampleGenerator
    ctor()
    static JsonNode DeepSerialize(object? obj)
    static T GenerateExampleInstance<T>()
    static string GenerateExampleJson<T>()
  // Generates JSON Schema definitions from .NET types. To satisfy the OpenAI spec, every object schema’s "required" array must exactly equal the keys in "properties", and every object schema must have a "type": "object" key. Properties that are allowed to be null are marked according to the target dialect: the 2020-12 dialect expands "type" into a ["X", "null"] union, while the OpenAPI 3.0 dialect adds a sibling "nullable": true.
  static class JsonSchemaGenerator
    static ExpandoObject GenerateJsonSchemaExpandoObject<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Generate the schema as a JsonNode tree rather than a serialised string. Handles primitives (string, int, bool, ...), enums, arrays, dictionaries, and complex types — i.e. valid as a root for any callable shape, not just records. Useful when the caller wants to embed the schema into a larger JSON structure without the round-trip of string→parse.
    static JsonNode GenerateSchemaNode(Type type, string? description = null, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static string GenerateSchemaString<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Non-generic overload for callers that have a Type at runtime (reflection, dynamic dispatch, MCP tool-schema generation). Same semantics as the generic version.
    static string GenerateSchemaString(Type type, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Alias for Empty . Some generated code reaches for `Default` first (common shadcn / .NET pattern).
    static KernelContext Default { get; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()` or `default`. Provided as a named constant for code generated against frameworks that expect an `.Empty` / `.Default` affordance on context-like types.
    static KernelContext Empty { get; }
    ImmutableDictionary<string, Function> Functions { get; init; }
    string GbnfGrammar { get; init; }
    ImmutableList<Instruction> Instructions { get; init; }
    object? JsonSchema { get; init; }
    bool LogFullRequest { get; init; }
    bool LogFullResponse { get; init; }
    int MaxOutputTokens { get; init; }
    ImmutableList<MessageBlock> Messages { get; init; }
    ReasoningEffort ReasoningEffort { get; init; }
    int ReasoningTokenBudget { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    string ToolPlan { get; init; }
    bool UseAudioOutput { get; init; }
    bool UseCaching { get; init; }
    bool UseCitations { get; init; }
    bool UseJson { get; init; }
    bool UseStreaming { get; init; }
    bool UseUserNames { get; init; }
    KernelContext Add(Instruction instruction)
    KernelContext Add(MessageBlock message)
    static KernelContext Create(IEnumerable<Instruction>? instructions = null, IEnumerable<MessageBlock>? messages = null, IEnumerable<Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    IAsyncEnumerable<StreamingResult> GenerateAsync(ILLM llm, CancellationToken cancellationToken = null)
    KernelContext KeepMessagesMax(int count)
    IAsyncEnumerable<StreamingResult> RecurseAsync(IAsyncEnumerable<StreamingResult> generator, HashSet<string> alreadyCalledFunctions, CancellationToken cancellationToken = null)
    IAsyncEnumerable<StreamingResult> ReturnFunctionCallAsync(string name, string parametersJson, string callId, string thoughtSignature = "", string reasoningContent = "")
    IAsyncEnumerable<StreamingResult> RunFunctionAsync(string functionName, object?[] parameters, CancellationToken cancellationToken = null)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
    static MessageBlock? CreateFromObjects(IReadOnlyList<object?> inputs, MessageBlockRole role)
    override string ToString()
  enum MessageBlockRole
    User
    Model
    FunctionResult
  enum MessagePartType
    Text
    Image
    ImageUrl
    Audio
    AudioId
    Video
    VideoUrl
    VideoAsset
    Pdf
    PdfUrl
    FunctionResult
  class OutputAudioId
    ctor(string id)
    string Id { get; }
  class OutputAudioTranscript
    ctor(string transcript)
    string Transcript { get; }
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  class ReasoningBlock
    ctor(string text)
    string Text { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    JsonSchema202012
    OpenApi30
  struct StreamingResult
    ctor(object value, string sourceName, string? valueTypeName = null)
    string SourceName { get; }
    object Value { get; }
    string? ValueTypeName { get; }
  class Tag
    ctor(string name, string content, Dictionary<string, string>? attributes = null)
    Dictionary<string, string>? Attributes { get; }
    string Content { get; }
    string Name { get; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  class TokenUsage
    ctor(int inputTokens, int cachedInputTokens, int cacheCreationInputTokens, int outputTokens)
    int CacheCreationInputTokens { get; }
    // Subset of InputTokens served from the provider's prompt cache (Anthropic cache_read_input_tokens, OpenAI cached_tokens, Bedrock CacheReadInputTokens). Always included in InputTokens; this is the cache-attributable portion.
    int CachedInputTokens { get; }
    int InputTokens { get; }
    int OutputTokens { get; }
  class ToolPlan
    ctor(string text)
    string Text { get; }
  struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    SchemaDialect SchemaDialect { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsInputPdf { get; }
    bool SupportsInputVideo { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
  sealed class LLM : IDisposable, ILLM, ILLMInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(LLMModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    SchemaDialect SchemaDialect { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsInputPdf { get; }
    bool SupportsInputVideo { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsSingleToolCalling { get; }
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
    void Dispose()
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsZeroDataRetention { get; init; }
    bool UsesInlineReasoning { get; init; }
  enum LLMModel
    Gpt4OmniMini
    Gpt41
    Gpt41Mini
    Gpt5
    Gpt5Mini
    Gpt5Nano
    Gpt51
    Gpt52
    Gpt5Pro
    Gpt52Pro
    Gpt53Codex
    Gpt54
    Gpt54Mini
    Gpt54Nano
    Gpt54Pro
    Gpt55
    Gpt55Pro
    O3
    O3Pro
    Claude41Opus
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Grok43
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    Qwen36
    Qwen37
    Qwen37Max
    GptOss120B
    Glm5
    Glm51
    Glm52
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // Maximum input-context window for the model, in tokens (e.g. 200_000 for Claude 4.x base, 1_000_000 for the 1M-context tier). Returns 0 when the model can't be resolved — callers should treat 0 as "unknown" and skip utilization computation rather than dividing by zero.
    static int ContextWindowSize(LLMModel model)
    static string DisplayName(LLMModel model)
