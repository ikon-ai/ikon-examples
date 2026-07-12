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
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Thrown by AI primitives when an active IGovernanceHook returns Deny . Carries the decision id so callers can correlate the failure to the audit record.
  sealed class GovernanceDeniedException : Exception
    ctor(string decisionId, string ruleId, string policyId, string reason)
    string DecisionId { get; }
    string PolicyId { get; }
    string Reason { get; }
    string RuleId { get; }
  // Thrown by AI primitives when an active hook returns Escalate . The host runtime is expected to catch this and route to the escalation target rather than retry — the operation is paused, not failed.
  sealed class GovernanceEscalatedException : Exception
    ctor(string decisionId, string target, string reason)
    string DecisionId { get; }
    string Reason { get; }
    string Target { get; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = default)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
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
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Connecting to the Ikon server timed out or failed. TRANSIENT by nature — a network blip, a server restart, a flaky link — so it is retryable: the RPC layer retries with a forced reconnect, and one that exhausts those attempts still lands as retryable so Emerge's bounded retry (and a host's re-drive) get their shot. A single 15s blip killing a 40-minute codegen run (observed repeatedly on a flaky uplink) is exactly what this classification prevents.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
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
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  class RegionNotSupportedException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(this IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(this IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(this IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(this IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(this IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<(bool, List<LLMEvent>)>> processAsync, int windowSize = 0, int windowOverlap = 0)
  // An incremental chunk of generated output audio.
  sealed class LLMEvent.AudioDelta : LLMEvent, IEquatable<LLMEvent.AudioDelta>
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // The provider-side id of the generated output audio, replayable as an AudioIdPart in a follow-up context.
  sealed class LLMEvent.AudioId : LLMEvent, IEquatable<LLMEvent.AudioId>
    ctor(string Id)
    string Id { get; init; }
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  // The transcript of generated output audio.
  sealed class LLMEvent.AudioTranscript : LLMEvent, IEquatable<LLMEvent.AudioTranscript>
    ctor(string Transcript)
    string Transcript { get; init; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  // A citation reference detected in the generated text. The refer indices bound the text span that refers to the citation; PositionIndex is the character index of the citation marker itself.
  sealed class LLMEvent.Citation : LLMEvent, IEquatable<LLMEvent.Citation>
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  // Generation was stopped by a content-safety classifier.
  sealed class LLMEvent.ContentFiltered : LLMEvent, IEquatable<LLMEvent.ContentFiltered>
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // The complete model message of a shader run (may differ from the text response), emitted once at the end.
  sealed class LLMEvent.FinalModelMessage : LLMEvent, IEquatable<LLMEvent.FinalModelMessage>
    ctor(string Text)
    string Text { get; init; }
  // The complete text response of a shader run, emitted once at the end.
  sealed class LLMEvent.FinalText : LLMEvent, IEquatable<LLMEvent.FinalText>
    ctor(string Text)
    string Text { get; init; }
  // The provider's finish reason for the generation (e.g. "stop", "max_tokens").
  sealed class LLMEvent.Finished : LLMEvent, IEquatable<LLMEvent.Finished>
    ctor(string Reason)
    string Reason { get; init; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Function/tool result carrying media alongside text. Providers that support media inside tool results (Anthropic tool_result image blocks) inline the media so the model actually SEES it; every other consumer degrades to ToString , which summarizes the media without dumping bytes.
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
  struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
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
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // When set, providers that support server-side context editing (Anthropic context-management beta) clear OLD tool results once the request's input exceeds this many tokens — after prompt-cache lookup, so cached prefixes survive. The single biggest context sink in long tool-using loops is superseded tool results being re-sent every round; server-side clearing removes them without the cache-busting a client-side history rewrite causes. Null = off. Providers without support ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names whose results are NEVER cleared by ClearToolResultsAfterInputTokens (semantic anchors like verdicts).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()` or `default`. Provided as a named constant for code generated against frameworks that expect an `.Empty` affordance on context-like types.
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
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // One event in the typed stream produced by GenerateAsync and its combinators. Consume the stream by switching on the concrete case: TextDelta for incremental text, ToolCallRequested when the model asks for a tool, ToolResult for a tool's output, Usage and Finished for end-of-generation accounting, and so on. Events not relevant to a consumer should be passed through unchanged so downstream consumers still see them.
  abstract class LLMEvent : IEquatable<LLMEvent>
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
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
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  // The model's reasoning trace for this generation.
  sealed class LLMEvent.Reasoning : LLMEvent, IEquatable<LLMEvent.Reasoning>
    ctor(string Text)
    string Text { get; init; }
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
  // A parsed XML-style tag extracted from the text stream by WithParsedTagsAsync .
  sealed class LLMEvent.Tag : LLMEvent, IEquatable<LLMEvent.Tag>
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  // An incremental chunk of generated text.
  sealed class LLMEvent.TextDelta : LLMEvent, IEquatable<LLMEvent.TextDelta>
    ctor(string Text)
    string Text { get; init; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  // The model requested a tool invocation.
  sealed class LLMEvent.ToolCallRequested : LLMEvent, IEquatable<LLMEvent.ToolCallRequested>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // The model's plan for upcoming tool calls (Cohere).
  sealed class LLMEvent.ToolPlan : LLMEvent, IEquatable<LLMEvent.ToolPlan>
    ctor(string Text)
    string Text { get; init; }
  // The output of an executed tool. Value holds the tool's return value; ValueType records its runtime type so the value can be rehydrated to the original type after a JSON round-trip (e.g. over RPC).
  sealed class LLMEvent.ToolResult : LLMEvent, IEquatable<LLMEvent.ToolResult>
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  // Token accounting for one generation. CachedInputTokens is the subset of InputTokens served from the provider's prompt cache (Anthropic cache_read_input_tokens, OpenAI cached_tokens, Bedrock CacheReadInputTokens).
  sealed class LLMEvent.Usage : LLMEvent, IEquatable<LLMEvent.Usage>
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
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
  // Public seam over the provider-facing JSON schema generator. This is the exact projection every LLM provider applies when it ships a Function to the model (Anthropic input_schema, OpenAI parameters, …). Callers that need to display, persist, or compare "the schema the LLM will see" should use this instead of re-deriving their own — any drift between a home-grown projection and the wire is a bug this seam exists to prevent.
  static class FunctionSchema
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
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
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // True when the provider binding can inline images INSIDE tool results (Anthropic tool_result image blocks). Distinct from SupportsInputImages : a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) sees images in messages but not in tool results.
    bool SupportsImagesInToolResults { get; init; }
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
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
  class NonRetryableLLMException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableLLMException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
