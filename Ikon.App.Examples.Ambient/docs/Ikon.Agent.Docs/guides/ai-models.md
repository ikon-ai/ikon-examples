# AI Models & LLM

## AI Models & LLM Connection

LLM model selection, connection configuration, and core AI infrastructure.

---

# Ikon.AI Public API
namespace Ikon.AI
  class AIException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class AITimeoutException : RetryableAIException
    ctor(string message)
    ctor(TimeSpan configuredTimeout, string targetName)
    TimeSpan ConfiguredTimeout { get; }
    string TargetName { get; }
  // Downloads URL-delivered generation results. When a result's ResultKind is ResultKind.Url the payload lives behind a signed download link valid for roughly one hour; GetDataAsync returns the bytes either way, downloading transparently when needed.
  static class AssetOutputs
    static Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    // Returns the result's payload bytes: Data when delivered inline, otherwise downloaded from Url.
    static Task<byte[]> GetDataAsync(this IResultPayload result, CancellationToken cancellationToken = default)
  // The name selects the model in the category's string-based APIs (e.g. new LLM("my-model")). An empty ApiKey means the endpoint needs no authentication header.
  abstract class CustomModel
    // API key for the endpoint. Empty means no authentication header is sent (e.g. a local Ollama instance).
    string ApiKey { get; init; }
    // Model name sent to the endpoint in the request payload. Defaults to Name when left unset.
    string ApiModelName { get; init; }
    // Full URL of the endpoint, e.g. http://localhost:8000/v1/chat/completions.
    required string EndpointUrl { get; init; }
    // Name the model is registered and selected by. Must not collide with a built-in model name and must not contain dots or whitespace.
    required string Name { get; init; }
  // Register a model at app startup, then select it by name anywhere a model name string is accepted:
  // CustomModels.Instance.Register(new CustomLLMModel
  // {
  //     Name = "my-model",
  //     EndpointUrl = "http://localhost:8000/v1/chat/completions",
  //     Api = CustomLLMApi.OpenAICompletions,
  //     ApiKey = "sk-...",
  //     ContextWindowSize = 32768,
  // });
  //
  // var reply = await Emerge.AskAsync("Hello", "my-model");
  // Custom models always execute in the local process — calls never go through the Ikon RPC mechanism. Usage is reported with a .user suffix and billed as a flat per-request fee instead of per-token provider pricing. The registry is async-local (like CredentialStorage): register models on the main flow at startup, before spawning parallel work, so every flow sees them. Registering the same name again replaces the previous registration; instances constructed before the replacement keep the configuration they were created with.
  sealed class CustomModels : AsyncLocalInstance<CustomModels>
    ctor()
    // True when the name is registered in any category.
    bool IsRegistered(string name)
    // Registers a custom LLM endpoint, selectable with new LLM(name) and the string-model overloads of Emerge.
    void Register(CustomLLMModel model)
    // Registers a custom embedding endpoint, selectable with new EmbeddingGenerator(name).
    void Register(CustomEmbeddingModel model)
    // Registers a custom rerank endpoint, selectable with new Reranker(name).
    void Register(CustomRerankModel model)
    // Registers a custom classification endpoint, selectable with new Classifier(name).
    void Register(CustomClassificationModel model)
    // Removes the named model from every category it is registered in. Returns true when at least one registration was removed.
    bool Unregister(string name)
  // A generation result payload delivered either as inline bytes or as a short-lived download URL, as told by Kind.
  interface IResultPayload
    byte[]? Data { get; }
    ResultKind Kind { get; }
    string? Url { get; }
  // Transient (network blip, server restart, flaky link) and therefore retryable — the RPC layer retries with a forced reconnect, and exhausted attempts still surface as retryable.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
  // A reference clip for prompt-driven audio editing: the model preserves this clip's timing and structure while the prompt re-styles it. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record InputAudio
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string? MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output adheres to this reference, in [0, 1]; higher keeps the original melody/timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
    string? Url { get; init; }
  // Supply the image exactly one way: inline via Data (with MimeType), by Url, or by AssetUri — all consumers resolve the asset to a URL. Type, Strength, and MaskDilution apply only to image-editing/inpainting models; depth, segmentation, mesh, and video generation ignore them.
  sealed record InputImage
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    double? MaskDilution { get; init; }
    string? MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
  enum InputImageType
    Normal
    Mask
  // A reference clip for video generation: footage the model is shown rather than asked to invent, addressed from the prompt the way reference images are. What it is *for* is the prompt's business — carrying a subject's appearance across a cut, holding a camera move, or regenerating a stretch of an existing film with one thing changed. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically). Providers impose their own length and size limits.
  sealed record InputVideo
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  // Attributes a failed model call to a ModelFailureKind.
  static class ModelFailure
    static ModelFailureKind Classify(Exception exception)
  // Why a model call failed, independent of the modality that raised it. The retryable/non-retryable split answers "should this call be tried again"; it deliberately says nothing about the cause, so a removed model, an unpaid account and a model that merely answered badly all land in the same bucket. This answers "what does the failure say about the model", which is what decides whether a human has to act.
  enum ModelFailureKind
    // The failure could not be attributed. Callers that gate on this should treat it as a real failure: an unrecognised error is far more likely to be a genuine defect than a benign one.
    Unknown
    // The call never reached a verdict about the model: transport error, timeout, throttling or a provider-side fault. Says nothing about whether the model is healthy.
    Transient
    // The provider no longer serves this model id. The model has been removed, renamed or retired and the configuration has to be updated.
    Unavailable
    // The model exists but this account cannot call it: missing or rejected credentials, exhausted credits, or a quota that is not a transient rate limit. An operator has to act, but nothing is wrong with the model or the code.
    AccessDenied
    // The model answered and the answer did not meet the contract: no content, an unusable tool call, or output that failed validation. Non-deterministic by nature and often not reproducible on the next call.
    Quality
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
  class NonRetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // An image produced by an analysis model (depth map, segmentation mask, preview). Kind tells how it was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record OutputImage : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  class RegionNotSupportedException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // With Auto the payload stays inline in-process; only when the result is returned from a remotely hosted AI function is it uploaded to a short-lived asset URL, and then only if it exceeds an internal size threshold (a few MB), keeping the protocol message small. Url always uploads, in any context. Check the result's ResultKind field to see which delivery was used.
  enum ResultDelivery
    Auto
    Url
  // Data guarantees the result's Data is non-null; Url guarantees its Url is non-null. Call result.GetDataAsync() (AssetOutputs.GetDataAsync) to get the bytes either way.
  enum ResultKind
    Data
    Url
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // The URLs these clients fetch come from app code, from LLM tool arguments and from provider responses — none of which the platform controls. Checking the URL string is not enough: the name is resolved later, so a host that resolves to 169.254.169.254 passes any check made up front, and a redirect or a second DNS answer moves the target after the check. The decision therefore happens at SocketsHttpHandler.ConnectCallback, on the address actually being connected to. That covers every redirect hop, because each one connects again, and it closes DNS rebinding, because the address checked is the address used.
  static class SsrfGuard
    // A handler that resolves the host itself and connects only to a public address.
    static SocketsHttpHandler CreateHandler()
    static bool IsAllowedScheme(Uri uri)

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(this IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(this IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(this IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(this IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(this IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<(bool, List<LLMEvent>)>> processAsync, int windowSize = 0, int windowOverlap = 0)
  readonly struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  readonly struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Only providers that support media in tool results inline the media for the model to see; all other consumers fall back to ToString, which summarizes the media without emitting the bytes.
  sealed class FunctionMediaResult
    ctor(string text, params BinaryDataContainer[] media)
    IReadOnlyList<BinaryDataContainer> Media { get; }
    string Text { get; }
    override string ToString()
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string? ModelMessagePrefix { get; set; }
    string? ModelMessageSuffix { get; set; }
    object? Result { get; set; }
  readonly struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  readonly struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  readonly struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  readonly record struct KernelContext
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Threshold is measured in input tokens; clearing runs after prompt-cache lookup so cached prefixes survive. Null disables it. Only providers with server-side context editing (Anthropic context-management) act on it — others ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names exempt from ClearToolResultsAfterInputTokens clearing — use for results that stay semantically load-bearing all run (verdicts, anchors).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    bool DisableFunctionCalling { get; init; }
    // When true (the DEFAULT — set in the constructor), any assistant text the model emits on the same turn as a tool call is DROPPED — only the tool call flows on. Set false to keep that interleaved text (e.g. a model that narrates before calling a tool). A direct Kernel/LLM consumer who does not set this loses same-turn text with no signal.
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()`. Prefer this or `new KernelContext()` over `default(KernelContext)`: `default` leaves the collections unset, though the mutation helpers below tolerate it. Provided as a named constant for code generated against frameworks that expect an `.Empty` affordance.
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
    // Travels with the context over RPC, so the process that actually talks to the provider honours it — which is the only way a remote generation can report progress at all.
    bool StreamProgress { get; init; }
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
    // Runs the model over this context AND executes registered functions in-loop: it wraps ILLM.GenerateAsync and, whenever the model calls a function whose result is produced inline, runs it and feeds the output back into the event stream (recursing until the model stops calling functions). Prefer this when the context has functions registered. Use ILLM.GenerateAsync directly for the RAW provider stream that never runs a tool.
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // Consume by switching on the concrete record case; forward any case you do not handle unchanged so downstream consumers still receive it.
  abstract record LLMEvent
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
  // An incremental chunk of generated output audio.
  sealed record LLMEvent.AudioDelta : LLMEvent
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // The provider-side id of the generated output audio, replayable as an AudioIdPart in a follow-up context.
  sealed record LLMEvent.AudioId : LLMEvent
    ctor(string Id)
    string Id { get; init; }
  // The transcript of generated output audio.
  sealed record LLMEvent.AudioTranscript : LLMEvent
    ctor(string Transcript)
    string Transcript { get; init; }
  // ReferStartIndex/ReferEndIndex bound the citing text span; PositionIndex is the character index of the citation marker itself.
  sealed record LLMEvent.Citation : LLMEvent
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  // Generation was stopped by a content-safety classifier.
  sealed record LLMEvent.ContentFiltered : LLMEvent
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // The complete model message of a shader run (may differ from the text response), emitted once at the end.
  sealed record LLMEvent.FinalModelMessage : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The complete text response of a shader run, emitted once at the end.
  sealed record LLMEvent.FinalText : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The provider's finish reason for the generation (e.g. "stop", "max_tokens").
  sealed record LLMEvent.Finished : LLMEvent
    ctor(string Reason)
    string Reason { get; init; }
  // How much content of a given kind arrived in one chunk, emitted as the chunk is decoded. Opt-in via KernelContext.StreamProgress — off by default, because it changes the event stream every consumer sees. It exists because nothing else can answer "is the model working right now" over RPC: usage is reported once a turn has ended, Reasoning and tool arguments are only emitted after the stream drains, and text may be suppressed entirely on a tool-calling turn. Carries the SIZE and not the content, so it costs a few bytes and never puts the same text on the wire twice — the content still arrives in its own event.
  sealed record LLMEvent.GenerationProgress : LLMEvent
    ctor(LlmStreamKind Kind, int Characters)
    int Characters { get; init; }
    LlmStreamKind Kind { get; init; }
  // The model's reasoning trace for this generation.
  sealed record LLMEvent.Reasoning : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // A parsed XML-style tag extracted from the text stream by AsyncEnumerableExtensions.WithParsedTagsAsync.
  sealed record LLMEvent.Tag : LLMEvent
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  // An incremental chunk of generated text.
  sealed record LLMEvent.TextDelta : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The model requested a tool invocation.
  sealed record LLMEvent.ToolCallRequested : LLMEvent
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // The model's plan for upcoming tool calls (Cohere).
  sealed record LLMEvent.ToolPlan : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // ValueType is the value's runtime type name, used to rehydrate Value to its original type after a JSON round-trip (e.g. over RPC).
  sealed record LLMEvent.ToolResult : LLMEvent
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  // The buckets are disjoint: total input = InputTokens + CachedInputTokens + CacheCreationInputTokens. A fully cached prompt reports InputTokens=0 with all input in CachedInputTokens.
  sealed record LLMEvent.Usage : LLMEvent
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  readonly struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
    // Builds a block from a heterogeneous input list. Each input must be a string or a BinaryDataContainer whose MIME type is an image, audio, video, or PDF; any other input type or MIME type is rejected rather than silently dropped. Returns null only when inputs is empty.
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
  readonly struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    // JSON Schema 2020-12 / OpenAPI 3.1. Nullable primitives expand their "type" into a ["X", "null"] union. Accepted by OpenAI strict structured outputs and Anthropic tool-use schemas.
    JsonSchema202012
    // OpenAPI 3.0 Schema Object. "type" is always a single string and nullability is carried on a separate "nullable": true flag. Accepted by Google's Gemini response_schema validator, which rejects the 2020-12 union-type form outright.
    OpenApi30
  readonly struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  readonly struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  readonly struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  readonly struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  // Request/response format a custom LLM endpoint speaks.
  enum CustomLLMApi
    // OpenAI chat completions format (/v1/chat/completions) — spoken by most self-hosted stacks: vLLM, Ollama, llama.cpp, LM Studio, TGI, SGLang.
    OpenAICompletions
    // OpenAI responses format (/v1/responses).
    OpenAIResponses
    // Anthropic messages format.
    Anthropic
    // Google Gemini format (Vertex-style streamGenerateContent).
    Google
    // Cohere chat format.
    Cohere
  // Capability flags default to what a typical self-hosted OpenAI-compatible model supports; enable more (e.g. SupportsJsonSchema) when the endpoint provides them.
  sealed class CustomLLMModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomLLMApi Api { get; init; }
    // Maximum input-context window of the model, in tokens.
    required int ContextWindowSize { get; init; }
    // Largest number of tokens the endpoint will generate in one response. Leave at 0 when the endpoint has no such cap: a request asking for more than the model can produce is capped at this value instead of being sent as-is, and 0 means "send the caller's value".
    int MaxOutputTokens { get; init; }
    bool SupportsCaching { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsStrictJsonSchema { get; init; }
    bool SupportsSystemMessages { get; init; }
    bool SupportsTemperature { get; init; }
  // Returns the exact JSON schema each provider ships to the model for a Function; use it rather than re-deriving your own projection.
  static class FunctionSchema
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    // In tokens. 0 means "no published limit", not "no output allowed".
    int MaxOutputTokens { get; }
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
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    int MaxOutputTokens { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // Distinct from SupportsInputImages: a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) accepts images in messages but not inside tool_result blocks.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsZeroDataRetention { get; init; }
    bool UsesInlineReasoning { get; init; }
  class LLMMaxOutputTokensException : NonRetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
    Gpt56Sol
    Gpt56Terra
    Gpt56Luna
    O3
    O3Pro
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Claude5Opus
    Claude5Fable
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Gemini35FlashLite
    Gemini36Flash
    Grok43
    Grok45
    GrokBuild01
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    Ministral14B
    Ministral8B
    Ministral3B
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandRPlus
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    KimiK3
    Qwen36
    Qwen37
    Qwen37Max
    Qwen38Max
    Qwen37Flash
    Qwen3827B
    GptOss120B
    Glm5
    Glm51
    Glm52
    Glm53
    Glm5VTurbo
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    DeepSeekV4FlashVision
    Seed21Turbo
    Seed20Code
    Seed20Lite
    Seed20Mini
    MiMoV25
    MiMoV25Pro
    Step37Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // In tokens. Returns 0 when the model can't be resolved — treat 0 as "unknown" and skip utilization math rather than dividing by zero.
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
    // In tokens. Returns 0 when the limit is unknown (unresolvable model, or a provider that publishes none) — treat 0 as "no cap known", not as a zero budget.
    static int MaxOutputTokens(this LLMModel model)
  class ModelOutputException : RetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class NonRetryableLLMException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableLLMException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
