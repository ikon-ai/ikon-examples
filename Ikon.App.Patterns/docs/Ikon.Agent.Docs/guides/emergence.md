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
```

Then run against it:

```csharp
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

// Direct result (no streaming) — awaiting the run returns non-null T or throws EmergenceStoppedException
var analysis = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
{
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
    pass.Temperature = 0.3;
});
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
}, cts.Token);
```

> **Do NOT use `Task.WhenAny` for timeouts.** Pass the `CancellationToken` directly — the Emerge system handles
> cancellation internally and cleans up properly. Use `try/catch (OperationCanceledException)` to handle timeouts.
>
> If you get `FinishReason=max_tokens` errors, increase `pass.MaxOutputTokens` (default is 16000; on reasoning models the thinking tokens count against this cap).

### Tools

```csharp
pass.AddTool(Tool.Of("search", "Search the web", (string query) => SearchWeb(query)))
    .AddTool(Tool.Of("get_data", "Get statistics", (string topic) => GetData(topic)));
pass.MaxToolCalls = 10;
```

`Tool.Of` takes up to 4 lambda parameters; annotate them with `[Description("...")]` to document them to the LLM. `[Description]` also takes an `example:` value and an `isRequired:` of `RequiredStatus.Required` or `RequiredStatus.Optional`, overriding the `Default` the parameter's own nullability implies. A tool that needs more parameters takes a single request record with `[property: Description]` on its fields. For MCP servers, wrap a connected `McpClient` in an `McpSkill`, or build schema-first tools with `Tool.FromSchema`.

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

Refer to the emergence-patterns guide for advanced patterns (MapReduce, Refine, EnsembleMerge, TreeSearch, etc.) and the ai-models guide for LLM model listings.

### Canonical typed run

The canonical structured `Emerge.Run<T>` call, extracted verbatim from the platform validation app so it is always real, compiling code:
```csharp
var brief = await Emerge.Run<TopicBrief>(
    LLMModel.Claude45Sonnet,
    pass =>
    {
        pass.SystemPrompt = """
            Research the given topic. Return JSON matching the output schema.
            Be concrete — named entities, dates, numbers. Confidence reflects
            how grounded your facts are; lower it if you're guessing.
            """;
        pass.Command = $"Topic: {topic}\nDepth: {depth}";
        pass.Temperature = 0.2;
        pass.MaxIterations = depth;
    }).ResultAsync();
```

### Available LLM Models

All models use the same `Emerge.Run<T>` API — just change the model enum. The complete, authoritative set of `LLMModel` values is generated from the enum itself below, so it can never drift from the code.

---

# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    // Prepended to the solver's system prompt so ensemble members differentiate. Defaults to Solver{Index}; set a meaningful value (e.g. "the security reviewer") to steer each member.
    string? Role { get; set; }
    // Same as CandidateScope<T>.Seed: drives divergence between solvers, not a sampler seed and not reproducible.
    int? Seed { get; set; }
  sealed class BestOfOptions<T> : EmergeScope<T>
    ctor()
    // The ScoreBreakdown is non-null exactly when ScoreDetailed produced one, and null when ranking with the plain Score delegate.
    Func<T, ScoreBreakdown?, string>? BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>>? CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    // Runs a critic pass over the winning candidate and keeps its result when it scores better (see CriticMustImprove). The prompt comes from BuildCriticFeedback; without one, the best candidate and its score are appended to CriticScope's Command.
    bool EnableCritic { get; set; }
    // Set this or ScoreDetailed — with neither, every candidate scores 0.0 and the FIRST candidate always wins after paying for all Count runs. Ignored when ScoreDetailed is set. Candidates run sequentially, so budget wall time for Count full calls.
    Func<T, EmergenceTrace, double>? Score { get; set; }
    // Ranks candidates by ScoreBreakdown.TotalScore and passes the breakdown to BuildCriticFeedback. Takes precedence over Score.
    Func<T, EmergenceTrace, ScoreBreakdown>? ScoreDetailed { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    // Configuring the critic also enables it — an explicitly configured critic that silently never ran was the trap; set EnableCritic back to false afterward for the rare case of pre-configuring a critic to toggle later.
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    // Not a sampler seed (the chat models expose none), so it does not make a run reproducible — it only drives sibling candidates to diverge.
    int? Seed { get; set; }
  sealed record Completed<T> : EmergeEvent<T>
    ctor(T? Result, KernelContext Context, EmergenceTrace Trace)
    KernelContext Context { get; init; }
    T? Result { get; init; }
    EmergenceTrace Trace { get; init; }
  static class Emerge
    // Defaults to LLMModel.Claude45Haiku (cheap and fast — right for short transformations); use the model overload for a stronger tier. Never returns null; throws EmergenceStoppedException if the run stops or completes without a reply.
    static Task<string> AskAsync(string command, CancellationToken ct = default)
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = default)
    static Task<string> AskAsync(string command, string model, CancellationToken ct = default)
    // Asks the model for JSON matching T's schema; defaults to LLMModel.Claude45Haiku. Throws EmergenceStoppedException when the run stops, completes without a result, or returns invalid JSON.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = default) where T : class
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = default) where T : class
    static Task<T> AskAsync<T>(string command, string model, CancellationToken ct = default) where T : class
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    // Return this from a tool body to end the run right after the current tool batch, with toolResult fed to the transcript as the tool result. The value also becomes the run result when it is assignable to the run's T; otherwise the run completes with default(T).
    static EndRun<TValue> EndRun<TValue>(TValue toolResult)
    // Return from a tool body to end the run after the current tool batch; the completion is recorded as a plain marker with no value and the run completes with default(T).
    static EndRun EndRun()
    static EmergeRun<T> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(string model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(string model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    // The unmediated model stream: no pass, no tool loop, no structured output — reach for Run<T> for those. Compose the result with the EmergeEventExtensions helpers (AsStringAsync and friends).
    // regions: Restricts which regions may serve the call; null lets the platform choose.
    static IAsyncEnumerable<LLMEvent> Generate(LLMModel model, KernelContext context, IReadOnlyList<ModelRegion>? regions = null, CancellationToken ct = default)
    static IAsyncEnumerable<LLMEvent> Generate(string model, KernelContext context, IReadOnlyList<ModelRegion>? regions = null, CancellationToken ct = default)
    // regions: Restricts which regions may serve the call; null lets the platform choose.
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions = null)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(string model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(string model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(string model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(string model, KernelContext context, Action<RefineOptions<T>> configure, ModelStream stream, CancellationToken ct = default)
    // Awaiting returns a non-null T and throws EmergenceStoppedException if the run stops without a result. This overload creates a fresh KernelContext; pass an explicit one via the other overloads to seed input (images, prior turns) or carry conversation history across calls.
    static EmergeRun<T> Run<T>(LLMModel model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, Action<EmergePass<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, ModelStream stream, CancellationToken ct = default)
    // Every overload here that takes a substitute model takes a ModelStream, so a test can pass a lambda of its own instead. This exists for the common case of a fixed script, and because the replay cursor has to live somewhere the delegate can advance.
    // responses: Replayed in order, then from the start again; an empty list yields empty text.
    static ModelStream Scripted(IReadOnlyList<string> responses)
    static EmergeRun<TreeSearchResult> TreeSearch(LLMModel model, KernelContext context, Action<TreeSearchOptions> configure, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(string model, KernelContext context, Action<TreeSearchOptions> configure, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(LLMModel model, KernelContext context, Action<TreeSearchOptions> configure, ModelStream stream, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(string model, KernelContext context, Action<TreeSearchOptions> configure, ModelStream stream, CancellationToken ct = default)
  abstract record EmergeEvent<T>
  static class EmergeEventExtensions
    // Returns the result together with the updated KernelContext (for conversation continuity). The result stays nullable — a run can complete without producing one — so guard it before use.
    static Task<(T? Result, KernelContext Context)> FinalAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Like FinalAsync<T> but also returns the EmergenceTrace (duration, token usage, tool-call history). The result stays nullable.
    static Task<(T? Result, KernelContext Context, EmergenceTrace Trace)> FinalWithTraceAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Drains the stream and returns the completed result — the same thing awaiting an EmergeRun<T> does. Never returns null; throws EmergenceStoppedException if the run stops or completes without a result. Use FinalAsync<T> when you need the updated KernelContext back or want to handle a missing result yourself.
    static Task<T> ResultAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
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
    // Null inherits the run's model; set it to override the model for this pass only.
    LLMModel? Model { get; set; }
    // Wins over Model when both are set; null inherits the run's model.
    string? ModelName { get; set; }
    // Tools named here are treated as side-effect-free: the executor runs consecutive calls to them from one model turn concurrently, while results are still recorded in the model's original order. Any tool not listed acts as a barrier and runs alone.
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
  // Both awaitable (one-shot non-null result, throws EmergenceStoppedException on failure) and enumerable (event stream). Single-shot: consumed exactly once — awaiting twice hands back the same result, but mixing the two shapes (enumerate then await, or the reverse) throws.
  sealed class EmergeRun<T> : IAsyncEnumerable<EmergeEvent<T>>
    IAsyncEnumerator<EmergeEvent<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    // Awaiting drains the stream and returns the completed result: never null, and throws EmergenceStoppedException if the run stops without producing one.
    TaskAwaiter<T> GetAwaiter()
  class EmergeScope<T> : EmergeScopeBase
    ctor()
    // Defaults to true.
    bool CaseInsensitiveJson { get; set; }
    string JsonExample { get; }
    string JsonSchema { get; }
    // Defaults to true for every T except string.
    bool UseJson { get; set; }
  abstract class EmergeScopeBase
    string? Command { get; set; }
    bool? IncludeJsonExample { get; set; }
    // Null does NOT mean unbounded — the executor caps at 10 iterations and stops the run with "MaxIterationsExceeded", which an awaited run surfaces as EmergenceStoppedException. Raise this explicitly for long tool loops.
    int? MaxIterations { get; set; }
    // Default when null: 16000.
    int? MaxOutputTokens { get; set; }
    // Default when null: 3 retries.
    int? MaxRetries { get; set; }
    // Default when null: 50 tool calls, then the run stops with "MaxToolCallsExceeded".
    int? MaxToolCalls { get; set; }
    // Default when null: 5 minutes of wall time, then the run stops with "MaxWallTimeExceeded".
    TimeSpan? MaxWallTime { get; set; }
    LLMModel? Model { get; set; }
    // Wins over Model when both are set.
    string? ModelName { get; set; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? SystemPrompt { get; set; }
    // Default when null: 0.7.
    double? Temperature { get; set; }
    // Default when null: 15 minutes.
    TimeSpan? Timeout { get; set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get; set; }
    int? UseLastNMessages { get; set; }
    void UseLastMessages(int count, int skipLast = 0)
  enum EmergenceStatus
    Completed
    Stopped
    Failed
  class EmergenceStoppedException : Exception
    ctor(EmergenceStatus status, string? stopReason)
    ctor(EmergenceStatus status, string? stopReason, Exception innerException)
    EmergenceStatus Status { get; }
    string? StopReason { get; }
  sealed record EmergenceTrace
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
  // Return this from a tool body to end the run immediately after the current tool batch instead of looping back to the model. The value (if any) is fed to the model transcript as this tool's result AND becomes the run's result when it is assignable to T; EndRun() with no value, or a value of an unrelated type, completes with default(T). Both await Emerge.Run<T>(...) and enumerating for the Completed<T> event observe that result. Create via Emerge.EndRun<TValue> or Emerge.EndRun.
  class EndRun
  // ToolResult is written to the model transcript as the tool result and becomes the run's result when assignable to the run's result type.
  sealed class EndRun<TValue> : EndRun
    TValue ToolResult { get; }
  sealed class EnsembleMergeOptions<T> : EmergeScope<T>
    ctor()
    // Must be at least 1 — there is no "unbounded" sentinel.
    int MaxParallel { get; set; }
    EmergeScope<T> MergerScope { get; }
    Action<AgentScope<T>>? SolverConfig { get; set; }
    int SolverCount { get; set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  sealed record FoundSection
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
    // Keeps the last take message blocks (after ignoring the last skipLast), then advances the start to the next User block so the result never begins on an orphan Model or FunctionResult turn (which providers reject). Instructions and all other fields are preserved.
    static KernelContext TrimToLastMessages(this KernelContext ctx, int take, int skipLast = 0)
  // Each TInput chunk is mapped by its own LLM call into a TMapped, then all mapped results are reduced by one final call into the TResult. Chunks are passed to the map prompt as JSON, so any serializable type works (a string per chunk is the common case).
  sealed class MapReduceOptions<TInput, TMapped, TResult> : EmergeScope<TResult>
    ctor()
    // Set this or Input; each chunk is one map call.
    IReadOnlyList<TInput>? Chunks { get; set; }
    // Split into chunks by Split; without a Split it is mapped as a single chunk. Alternative to Chunks.
    TInput? Input { get; set; }
    EmergeScope<TMapped> MapScope { get; }
    // Must be at least 1 — there is no "unbounded" sentinel.
    int MaxParallel { get; set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<TInput, IEnumerable<TInput>>? Split { get; set; }
    void Map(Action<EmergeScope<TMapped>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  // Call ConnectAsync before reading Tools or calling a tool — it performs the MCP handshake and populates the tool list. Uses Streamable HTTP transport.
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    // Returns the content plus a pagination cursor; pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, string? cursor = null, CancellationToken ct = default)
    Task ConnectAsync(CancellationToken ct = default)
    void Dispose()
  record McpTool
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string? Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  record McpToolResult
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string? NextCursor { get; init; }
  // The whole seam for substituting a model: a lambda satisfies it. It carries no capabilities because those describe the MODEL, which every overload taking one of these is passed separately — a substitute reporting its own would be either a copy of the real ones or a fiction the run then acted on.
  delegate ModelStream
    IAsyncEnumerable<LLMEvent> ModelStream(KernelContext context, CancellationToken ct = default)
  sealed record ModelText<T> : EmergeEvent<T>
    ctor(string Text)
    string Text { get; init; }
  sealed record NavigationDecision
    ctor(string Reasoning = "", bool Complete = false)
    bool Complete { get; init; }
    string Reasoning { get; init; }
  sealed record Progress<T> : EmergeEvent<T>
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
  sealed record Retry<T> : EmergeEvent<T>
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
    // evaluate must return a score in [0, 1]: values outside that range are clamped, so a rubric on a 0..10 or 0..100 scale collapses to 1.0 for every candidate and the ranking stops discriminating. Divide by the scale's maximum in the callback.
    ScoreBreakdownBuilder<T> Metric(string name, double weight, Func<T, double> evaluate)
    // Each metric score is clamped to [0, 1] and the total is the weight-normalized sum.
    ScoreBreakdown Score(T value)
  sealed class ScoreMetric
    ctor()
    string Name { get; init; }
    double Score { get; init; }
    double Weight { get; init; }
    double WeightedScore { get; }
  sealed record Stage<T> : EmergeEvent<T>
    ctor(string Name)
    string Name { get; init; }
  sealed record Stopped<T> : EmergeEvent<T>
    ctor(KernelContext Context, string? Reason)
    KernelContext Context { get; init; }
    string? Reason { get; init; }
  // Counts are cumulative running totals across all iterations, not per-iteration deltas — take the last event's values, never sum them.
  sealed record TokenUpdate<T> : EmergeEvent<T>
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed record ToolCallPlanned<T> : EmergeEvent<T>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  sealed record ToolCallResult<T> : EmergeEvent<T>
    ctor(FunctionCall Call, LLMEvent[] Events, object Result)
    FunctionCall Call { get; init; }
    LLMEvent[] Events { get; init; }
    object Result { get; init; }
  sealed class TreeSearchOptions : EmergeScope<TreeSearchResult>
    ctor()
    TreeIndex? Index { get; set; }
    int MaxResults { get; set; }
    int MaxSteps { get; set; }
    EmergeScope<NavigationDecision> NavigatorScope { get; }
    string Query { get; set; }
    void Navigator(Action<EmergeScope<NavigationDecision>> configure)
  sealed record TreeSearchResult
    ctor(List<FoundSection> Sections, string ReasoningTrace = "")
    string ReasoningTrace { get; init; }
    List<FoundSection> Sections { get; init; }

namespace Ikon.AI.Emergence.Structured
  // Tag matching is case-insensitive and tolerates attributes and formatting variations.
  static class StructuredTagParser
    // Returns the first occurrence's inner content, or null if the tag is absent.
    static string? GetTagContent(string content, string tagName)
    static bool HasTag(string content, string tagName)
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)
  sealed record StructuredTagParser.ParsedBlock
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  sealed record StructuredTagParser.ParsedResponse
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get; init; }
    string PlainText { get; init; }

namespace Ikon.AI.Emergence.Tree
  record ContentSection
    ctor(string Title, string Content, int? Page = null)
    string Content { get; init; }
    int? Page { get; init; }
    string Title { get; init; }
  interface IContentReader
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get; set; }
    static EmergeRun<TreeIndex> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(string model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(string model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
    TreeNode? FindById(string id)
    // Also repairs the TreeNode.Parent and TreeNode.Depth links of nodes that were added to TreeNode.Children directly rather than through TreeNode.AddChild.
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
    // Prefer AddChild, which also sets the child's Parent and Depth; a node added to this list directly gets those links when the tree is put into a TreeIndex (or on TreeIndex.RebuildIndex), not before.
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


---

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
    Claude51Fable
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Gemini35FlashLite
    Gemini36Flash
    Gemini37Flash
    Gemini38Flash
    Grok43
    Grok45
    Grok46
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
    MuseSpark13
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
    Glm53Flash
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
