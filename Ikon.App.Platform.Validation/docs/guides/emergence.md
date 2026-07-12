# Emergence

## Emergence

Use `Emerge.Run<T>()` for all LLM text generation — structured JSON output, chatbot conversations, agentic tool loops. Supports records and sealed classes as result types. Conversation history is maintained by reusing `KernelContext` across calls.

> **No setup required.** The platform handles all AI connections automatically. Do NOT add provider setup code,
> Azure/OpenAI configuration, or connection strings. Just call `Emerge.Run<T>()` with a model enum and it works.

### Emerge.Run<T> - Basic Pattern

```csharp
// Both sealed class and record work as result types
public sealed class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
}

// Records also work:
// public record AnalysisResult(string Summary, List<string> KeyPoints);

// Streaming (observe each event)
await foreach (var ev in Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
{
    pass.SystemPrompt = "You are a helpful analyst.";
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
    pass.Temperature = 0.7;
    pass.MaxOutputTokens = 32000;
    pass.MaxIterations = 5;
}))
{
    if (ev is Completed<AnalysisResult> completed)
    {
        var result = completed.Result;
    }
}

// Direct result (no streaming) — ResultAsync returns non-null T or throws EmergenceStoppedException
var result = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
{
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
    pass.Temperature = 0.3;
}).ResultAsync();
```

### Conversation History (Chatbots)

To build a chatbot that remembers context, reuse the `KernelContext` returned from previous `Emerge.Run` calls:

```csharp
// First user message — start with a fresh KernelContext
var (result1, context) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
{
    pass.SystemPrompt = "You are a friendly assistant.";
    pass.Command = userMessage;
}).FinalAsync();

// Second message — pass the returned context so it carries the full conversation history automatically
var (result2, context2) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = nextUserMessage;
}).FinalAsync();
```

> **Always use `Emerge.Run<T>()` for chatbots** — do not use lower-level LLM classes directly. Emerge handles conversation context, retries, and structured output automatically.

### Cancellation & Timeouts

All Emerge methods accept a `CancellationToken` as the last parameter. Use `CancellationTokenSource` with a timeout:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var result = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
{
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
}, cts.Token).ResultAsync(cts.Token);
```

> **Do NOT use `Task.WhenAny` for timeouts.** Pass the `CancellationToken` directly — the Emerge system handles
> cancellation internally and cleans up properly. Use `try/catch (OperationCanceledException)` to handle timeouts.
>
> If you get `FinishReason=max_tokens` errors, increase `pass.MaxOutputTokens` (default is 4000).

### Tools

```csharp
pass.AddTool(Tool.Of("search", "Search the web", (string query) => SearchWeb(query)))
    .AddTool(Tool.Of("get_data", "Get statistics", (string topic) => GetData(topic)));
pass.MaxToolCalls = 10;
```

`Tool.Of` takes up to 4 lambda parameters; annotate them with `[Description("...")]` to document them to the LLM. A tool that needs more parameters takes a single request record with `[property: Description]` on its fields. For MCP servers, wrap a connected `McpClient` in an `McpSkill`, or build schema-first tools with `Tool.FromSchema`.

### Emerge.BestOf<T> - Generate Multiple Candidates

```csharp
await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude46Sonnet, new KernelContext(), bo =>
{
    bo.Command = $"Write a tagline for: {prompt}\n\nReturn JSON:\n{bo.JsonSchema}";
    bo.Count = 3;
    bo.Score = (response, trace) => ScoreResponse(response);
    bo.Candidate(c => { c.Temperature = 0.5 + c.Index * 0.2; });
}))
{
    if (ev is Completed<CreativeResponse> completed) { /* best candidate */ }
}
```

Refer to the emergence-patterns guide for advanced patterns (MapReduce, TaskGraph, TreeSearch, etc.) and the ai-models guide for LLM model listings.

### Available LLM Models

`LLMModel.Claude46Sonnet`, `LLMModel.Claude45Sonnet`, `LLMModel.Claude45Haiku`, `LLMModel.Gemini25Flash`, `LLMModel.Gemini25Pro`, `LLMModel.Gpt5Mini`, `LLMModel.Gpt5`, `LLMModel.Grok420Reasoning`, and many more. All models use the same `Emerge.Run<T>` API — just change the model enum.

---

# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    string? Role { get; set; }
    int? Seed { get; set; }
  sealed class BestOfOptions<T> : EmergeScope<T>
    ctor()
    Func<T, ScoreBreakdown?, string>? BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>>? CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get; set; }
    Func<T, EmergenceTrace, double>? Score { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    int? Seed { get; set; }
  sealed class Completed<T> : EmergeEvent<T>, IEquatable<Completed<T>>
    ctor(T Result, KernelContext Context, EmergenceTrace Trace)
    KernelContext Context { get; init; }
    T Result { get; init; }
    EmergenceTrace Trace { get; init; }
  static class Emerge
    // One-shot LLM completion that returns the result string. The verbose form
    // var (reply, _) = await Emerge.Run<string>(
    //     LLMModel.Claude45Haiku, new KernelContext(),
    //     pass => pass.Command = command).FinalAsync(ct);
    // becomes
    // var reply = await Emerge.AskAsync(command, ct);
    // Uses Claude45Haiku by default — cheap+fast, the right choice for short transformations (chatbot replies, reformat-as-X, classify, summarize). Override the model via the other overload when the task warrants a stronger tier. Reach for the full Run when you need tools, multi-iteration agentic loops, a populated KernelContext , or fine pass tuning.
    static Task<string> AskAsync(string command, CancellationToken ct = default)
    // Like AskAsync but with an explicit model override.
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = default)
    // One-shot structured-output completion. Same shape as the string overload, but the model is asked for a JSON object matching T 's schema. Throws if the model returns nothing or invalid JSON.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = default) where T : class
    // Like AskAsync but with an explicit model override.
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = default) where T : class
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    // Like Run but creates a fresh KernelContext internally — the common case where the call carries no prior conversation. Paired with ResultAsync , the verbose form
    // var (result, _) = await Emerge.Run<Recipe>(
    //     LLMModel.Claude45Sonnet, new KernelContext(),
    //     pass => pass.Command = command, ct).FinalAsync(ct);
    // becomes
    // var result = await Emerge.Run<Recipe>(
    //     LLMModel.Claude45Sonnet,
    //     pass => pass.Command = command, ct).ResultAsync(ct);
    // Pass an explicit KernelContext via the other overloads when you seed the call with input (images, prior turns) or carry conversation history across calls.
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    // Like Run but with an explicit ILLM (e.g. a mock for testing).
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = default)
  static class EmergeEventExtensions
    // Drains the stream and returns the completed result together with the updated KernelContext . Reach for this over ResultAsync when you need the context back (conversation continuity) or want to handle a null result yourself.
    static Task<(T Result, KernelContext Context)> FinalAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Like FinalAsync but also returns the run's EmergenceTrace . Reach for this when you need telemetry (duration, token usage, tool-call history) alongside the result.
    static Task<(T Result, KernelContext Context, EmergenceTrace Trace)> FinalWithTraceAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Drains the stream and returns the completed result without the tuple ceremony. The verbose form
    // var (result, _) = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).FinalAsync(ct);
    // becomes
    // var result = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).ResultAsync(ct);
    // Never returns null — if the run completes without producing a result (where FinalAsync would hand back a null result), an EmergenceStoppedException is thrown. Reach for FinalAsync instead when you need the updated KernelContext back (conversation continuity) or want to handle a missing result yourself via a nullable result.
    static Task<T> ResultAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
  abstract class EmergeEvent<T> : IEquatable<EmergeEvent<T>>
  sealed class EmergePass<T>
    ctor()
    bool CaseInsensitiveJson { get; set; }
    string? Command { get; set; }
    KernelContext Context { get; }
    bool HasFunctionResults { get; }
    bool HasNewFunctionResults { get; }
    bool? IncludeJsonExample { get; set; }
    bool IsStopped { get; }
    int Iteration { get; }
    string JsonExample { get; }
    string JsonSchema { get; }
    int? MaxIterations { get; set; }
    int? MaxOutputTokens { get; set; }
    int? MaxRetries { get; set; }
    int? MaxToolCalls { get; set; }
    TimeSpan? MaxWallTime { get; set; }
    // Concrete model for this pass. Callers that sit above the agent layer (Ikon.Agent) usually don't set this directly — there a persona declares an abstract Reasoning (Capability × ModelFamily) and the agent runtime resolves it to the LLMModel placed here.
    LLMModel? Model { get; set; }
    bool? OptimizeContext { get; set; }
    // Names of tools the caller declares SIDE-EFFECT-FREE (pure read/lookup). The executor runs consecutive calls to these from one model turn CONCURRENTLY — measured on codegen, sequential guide/read batches dominated pass latency. Results are still recorded in the model's original order. Mutating tools stay out of this set and act as barriers.
    ISet<string> ReadOnlyToolNames { get; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? StopReason { get; }
    string? SystemPrompt { get; set; }
    double? Temperature { get; set; }
    TimeSpan? Timeout { get; set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get; set; }
    bool UseJson { get; set; }
    int? UseLastNMessages { get; set; }
    void Stop(string? reason = null)
    void UseLastMessages(int count, int skipLast = 0)
  // Optional wrapper a tool body can return to control how Emerge feeds the result back to the model. The executor unwraps Result into the tool-result message (so the model never sees the wrapper), and SkipReprocessing = true completes the run right after the current batch of tool calls instead of sending the results back for another model turn — for tools whose side effect IS the answer (e.g. a UI action the model triggered on the user's behalf); the run then completes with a default result. Plain return values behave as if wrapped with SkipReprocessing = false.
  sealed class EmergeResult
    ctor(object? result = null)
    object? Result { get; }
    bool SkipReprocessing { get; init; }
  abstract class EmergeScopeBase
    string? Command { get; set; }
    bool? IncludeJsonExample { get; set; }
    int? MaxIterations { get; set; }
    int? MaxOutputTokens { get; set; }
    int? MaxRetries { get; set; }
    int? MaxToolCalls { get; set; }
    TimeSpan? MaxWallTime { get; set; }
    LLMModel? Model { get; set; }
    bool? OptimizeContext { get; set; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? SystemPrompt { get; set; }
    double? Temperature { get; set; }
    TimeSpan? Timeout { get; set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get; set; }
    int? UseLastNMessages { get; set; }
    void UseLastMessages(int count, int skipLast = 0)
  class EmergeScope<T> : EmergeScopeBase
    ctor()
    bool CaseInsensitiveJson { get; set; }
    string JsonExample { get; }
    string JsonSchema { get; }
    bool UseJson { get; set; }
  enum EmergenceStatus
    Completed
    Stopped
    Failed
  class EmergenceStoppedException : Exception
    ctor(EmergenceStatus status, string? stopReason)
    ctor(EmergenceStatus status, string? stopReason, Exception innerException)
    EmergenceStatus Status { get; }
    string? StopReason { get; }
  sealed class EmergenceTrace : IEquatable<EmergenceTrace>
    ctor()
    ctor(int iterations, int toolCalls, TimeSpan duration, IReadOnlyList<FunctionCall>? toolCallHistory = null, string? finishReason = null, Exception? error = null, long inputTokens = 0, long cachedInputTokens = 0, long cacheCreationInputTokens = 0, long outputTokens = 0)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    TimeSpan Duration { get; init; }
    Exception? Error { get; init; }
    string? FinishReason { get; init; }
    long InputTokens { get; init; }
    bool IsTruncated { get; }
    int Iterations { get; init; }
    long OutputTokens { get; init; }
    IReadOnlyList<FunctionCall> ToolCallHistory { get; init; }
    int ToolCalls { get; init; }
  sealed class EnsembleMergeOptions<T> : EmergeScope<T>
    ctor()
    int MaxParallel { get; set; }
    EmergeScope<T> MergerScope { get; }
    Action<AgentScope<T>>? SolverConfig { get; set; }
    int SolverCount { get; set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  // One tree section the navigator marked relevant, with the reason it gave.
  sealed class FoundSection : IEquatable<FoundSection>
    ctor(string NodeId, string Path, string Content, string Relevance, int? Page = null)
    string Content { get; init; }
    string NodeId { get; init; }
    int? Page { get; init; }
    string Path { get; init; }
    string Relevance { get; init; }
  static class KernelContextExtensions
    static IReadOnlyList<FunctionCall> GetFunctionCalls(this KernelContext ctx, int take = 10)
    static IReadOnlyList<FunctionResultPart> GetFunctionResults(this KernelContext ctx, int take = 10)
    static bool HasFunctionResults(this KernelContext ctx)
  sealed class MapReduceOptions<TChunk, TResult> : EmergeScope<TResult>
    ctor()
    IReadOnlyList<object>? Chunks { get; set; }
    object? Input { get; set; }
    EmergeScope<TChunk> MapScope { get; }
    int MaxParallel { get; set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<object, IEnumerable<object>>? Split { get; set; }
    void Map(Action<EmergeScope<TChunk>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  // MCP (Model Context Protocol) client using Streamable HTTP transport. Connects to an MCP server, discovers tools, and proxies tool calls.
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    // Calls an MCP tool by name with the given JSON arguments.
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    // Calls an MCP tool and returns both content and pagination cursor. Pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, CancellationToken ct = default, string? cursor = null)
    // Initializes the MCP session and discovers available tools.
    Task ConnectAsync(CancellationToken ct = default)
    void Dispose()
  class McpTool : IEquatable<McpTool>
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string? Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  class McpToolResult : IEquatable<McpToolResult>
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string? NextCursor { get; init; }
  sealed class ModelText<T> : EmergeEvent<T>, IEquatable<ModelText<T>>
    ctor(string Text)
    string Text { get; init; }
  // The navigator's structured verdict at the end of a TreeSearch run.
  sealed class NavigationDecision : IEquatable<NavigationDecision>
    ctor(string Reasoning = "", bool Complete = false)
    bool Complete { get; init; }
    string Reasoning { get; init; }
  sealed class Progress<T> : EmergeEvent<T>, IEquatable<Progress<T>>
    ctor(string Message)
    string Message { get; init; }
  sealed class RefineOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope<T> InitialScope { get; }
    int MaxRefinements { get; set; }
    EmergeScope<T> RefinementScope { get; }
    Func<T, EmergenceTrace, Task<bool>>? ShouldContinue { get; set; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed class Retry<T> : EmergeEvent<T>, IEquatable<Retry<T>>
    ctor(string Reason, int AttemptNumber, int MaxAttempts)
    int AttemptNumber { get; init; }
    int MaxAttempts { get; init; }
    string Reason { get; init; }
  sealed class ScoreBreakdown
    ctor()
    IReadOnlyList<ScoreMetric> Metrics { get; init; }
    double TotalScore { get; init; }
    ScoreMetric? Weakest { get; init; }
    string FormatBreakdown()
  sealed class ScoreBreakdownBuilder<T>
    ctor()
    ScoreBreakdownBuilder<T> Metric(string name, double weight, Func<T, double> evaluate)
    ScoreBreakdown Score(T value)
  sealed class ScoreMetric
    ctor()
    string Name { get; init; }
    double Score { get; init; }
    double Weight { get; init; }
    double WeightedScore { get; }
  sealed class Stage<T> : EmergeEvent<T>, IEquatable<Stage<T>>
    ctor(string Name)
    string Name { get; init; }
  sealed class Stopped<T> : EmergeEvent<T>, IEquatable<Stopped<T>>
    ctor(KernelContext Context, string? Reason)
    KernelContext Context { get; init; }
    string? Reason { get; init; }
  sealed class TokenUpdate<T> : EmergeEvent<T>, IEquatable<TokenUpdate<T>>
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed class ToolCallPlanned<T> : EmergeEvent<T>, IEquatable<ToolCallPlanned<T>>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  sealed class ToolCallResult<T> : EmergeEvent<T>, IEquatable<ToolCallResult<T>>
    ctor(FunctionCall Call, LLMEvent[] Events, object Result)
    FunctionCall Call { get; init; }
    LLMEvent[] Events { get; init; }
    object Result { get; init; }
  sealed class TreeSearchOptions<T> : EmergeScope<T>
    ctor()
    TreeIndex? Index { get; set; }
    int MaxResults { get; set; }
    int MaxSteps { get; set; }
    EmergeScope<NavigationDecision> NavigatorScope { get; }
    string Query { get; set; }
    void Navigator(Action<EmergeScope<NavigationDecision>> configure)
  // Result of a TreeSearch run: the sections the navigator marked relevant, plus its final reasoning.
  sealed class TreeSearchResult : IEquatable<TreeSearchResult>
    ctor(List<FoundSection> Sections, string ReasoningTrace = "")
    string ReasoningTrace { get; init; }
    List<FoundSection> Sections { get; init; }

namespace Ikon.AI.Emergence.Structured
  // A parsed block from the content
  sealed class StructuredTagParser.ParsedBlock : IEquatable<StructuredTagParser.ParsedBlock>
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  // Complete parsed response with plain text and extracted blocks
  sealed class StructuredTagParser.ParsedResponse : IEquatable<StructuredTagParser.ParsedResponse>
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get; init; }
    string PlainText { get; init; }
  // Generic parser for structured XML-style tags in LLM responses. Handles case mismatches, partial tags, and various formatting variations.
  static class StructuredTagParser
    // Extract the content of a specific tag (first occurrence)
    static string? GetTagContent(string content, string tagName)
    // Check if content contains a specific tag
    static bool HasTag(string content, string tagName)
    // Parse content and extract structured blocks for the specified tag names
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)

namespace Ikon.AI.Emergence.Tree
  class ContentSection : IEquatable<ContentSection>
    ctor(string Title, string Content, int? Page = null)
    string Content { get; init; }
    int? Page { get; init; }
    string Title { get; init; }
  interface IContentReader
    abstract IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get; set; }
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
    TreeNode? FindById(string id)
    void RebuildIndex()
    string ToTableOfContents(int maxDepth = -1)
    IEnumerable<TreeNode> Traverse()
  class TreeIndexOptions
    ctor()
    bool GenerateSummaries { get; set; }
    int MaxDepth { get; set; }
    int MaxSummaryTokens { get; set; }
  class TreeNode
    ctor()
    ctor(string id, string title, string content = "")
    List<TreeNode> Children { get; }
    string Content { get; set; }
    int Depth { get; }
    string Id { get; set; }
    int? Page { get; set; }
    TreeNode? Parent { get; }
    string Summary { get; set; }
    string Title { get; set; }
    void AddChild(TreeNode child)
    string GetPath()
    IEnumerable<TreeNode> Traverse()
