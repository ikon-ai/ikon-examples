# Ikon.AI.Emergence Guide

Ikon.AI.Emergence is a streaming-first, C#-idiomatic library for building AI workflows with typed JSON outputs. It provides a collection of patterns for common AI tasks, from simple single-shot generation to parallel candidate search and document-tree navigation.

## Core Concepts

### Streaming-First Design

All APIs return `IAsyncEnumerable<EmergeEvent<T>>`. Non-streaming usage is achieved via the `.ResultAsync()` extension method — or `.FinalAsync()` when you also need the updated `KernelContext` back.

```csharp
// Streaming - observe progress
await foreach (var ev in Emerge.Run<MyType>(model, ctx, pass => { ... }))
{
    switch (ev)
    {
        case ModelText<MyType> t: Console.Write(t.Text); break;
        case ToolCallPlanned<MyType> tc: Console.WriteLine($"Calling {tc.Call.Function.Name}"); break;
        case Completed<MyType> done: Console.WriteLine($"Result: {done.Result}"); break;
    }
}

// Non-streaming - just get the result (never null; throws EmergenceStoppedException
// if the run stops or completes without one)
var result = await Emerge.Run<MyType>(model, pass => { ... }).ResultAsync();

// Non-streaming - get the (nullable) result plus the updated KernelContext
var (result, context) = await Emerge.Run<MyType>(model, ctx, pass => { ... }).FinalAsync();

// Non-streaming - get the result with trace info
var (result, context, trace) = await Emerge.Run<MyType>(model, ctx, pass => { ... }).FinalWithTraceAsync();
```

### Event Types

| Event | Description |
|-------|-------------|
| `ModelText<T>` | Streaming text chunk from the model |
| `ToolCallPlanned<T>` | Tool call detected (contains `FunctionCall`) |
| `ToolCallResult<T>` | Tool execution completed (contains `Call`, `Events`, `Result`) |
| `Stage<T>` | Pattern stage boundary (e.g., "Candidate:0", "Critic") |
| `Progress<T>` | Progress message |
| `Retry<T>` | Retry attempt (contains `Reason`, `AttemptNumber`, `MaxAttempts`) |
| `TokenUpdate<T>` | Token usage update (contains `InputTokens`, `CachedInputTokens`, `CacheCreationInputTokens`, `OutputTokens`) |
| `Completed<T>` | Final result with `Result`, `Context`, and `Trace` |
| `Stopped<T>` | Execution stopped (budget exceeded, user stop, etc.) with `Context` and optional `Reason` |

### Typed JSON Output

All patterns produce typed results. The library automatically generates JSON schemas and examples for your types:

```csharp
public class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
    public float Confidence { get; set; }
}

var result = await Emerge.Run<AnalysisResult>(model, pass =>
{
    pass.Command = "Analyze the following text and provide structured output.";
}).ResultAsync();

// result.Summary, result.KeyPoints, result.Confidence are typed
```

### Configuration Inheritance

Pattern options inherit from `EmergeScopeBase`. Child scopes (like `InitialScope`, `RefinementScope`) inherit settings from the parent unless overridden:

```csharp
await Emerge.Refine<T>(model, ctx, opt =>
{
    // Parent settings - inherited by all scopes
    opt.Temperature = 0.3f;
    opt.SystemPrompt = "You are an expert...";

    opt.Initial(s =>
    {
        // Only set what's different
        s.Command = "Generate initial draft.";
    });

    opt.Refinement(s =>
    {
        s.Temperature = 0.2f;  // Override for refinement
        s.Command = "Improve the draft.";
    });
}).ResultAsync();
```

### Context Behavior

Patterns handle context in two ways:

- **Shared context**: Sequential stages (Refine iterations, EnsembleMerge merger) share context. Each stage's output is automatically added to context before the next stage runs.
- **Isolated context**: Parallel runs (BestOf candidates, MapReduce chunks, EnsembleMerge solvers) use isolated derived contexts to ensure deterministic parallel execution.

---

## Patterns

### AskAsync — One-Shot Shortcut

The simplest entry point: a one-shot LLM call with no `KernelContext`, no tools, no streaming. Defaults to `LLMModel.Claude45Haiku` — cheap and fast for short transformations (chatbot replies, reformat-as-X, classify, summarize). Reach for `Run<T>` when you need tools, multi-iteration loops, a populated context, or pass tuning.

```csharp
// String response
string reply = await Emerge.AskAsync("Summarize this in one sentence: ...");

// Structured response (T must be a reference type)
public class Classification
{
    public string Category { get; set; } = "";
    public float Confidence { get; set; }
}

Classification result = await Emerge.AskAsync<Classification>(
    "Classify this support ticket: \"My laptop won't turn on\"");

// Explicit model override
string reply = await Emerge.AskAsync("Hard reasoning question", LLMModel.Claude45Sonnet);
```

The structured overload throws `InvalidOperationException` if the model returns nothing or invalid JSON.

---

### Run — Single Agent Loop

The core pattern. Generates a typed JSON result with optional tool use.

```csharp
var result = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, pass =>
{
    pass.SystemPrompt = "You are a helpful assistant.";
    pass.Command = "Answer the user's question.";
    pass.Temperature = 0.7;
    pass.MaxIterations = 5;
    pass.AddTool(Tool.Of("search_web", "Search the web for information",
        (string query) => SearchWeb(query)));
}).ResultAsync();
```

A fresh `KernelContext` is created internally. Pass your own when you seed the call with input (images, prior turns), and use `.FinalAsync()` when you need the updated context back for conversation continuity or want a nullable result instead of a throw:

```csharp
var (result, ctx) = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, context, pass =>
{
    pass.Command = "Answer the user's question.";
}).FinalAsync();
```

The `EmergePass<T>` configure callback is invoked on every iteration, giving access to runtime state:

- `pass.Iteration` — current iteration number
- `pass.HasFunctionResults` / `pass.HasNewFunctionResults` — whether tool results exist in context
- `pass.Stop(reason?)` — early termination from within the callback

**Options:**
- `SystemPrompt` - System instruction
- `Command` - User command/prompt
- `Temperature`, `MaxOutputTokens`, `ReasoningEffort`, `ReasoningTokenBudget` - Model parameters
- `MaxIterations`, `MaxToolCalls`, `MaxWallTime` - Budget limits
- `MaxRetries`, `RetryDelay` - Automatic retry on transient failures
- `Tools` - Available tools (see [Inline Tool Registration](#inline-tool-registration))

---

### BestOf — Score and Select Best

Run N independent attempts and select the best result based on a scoring function.

```csharp
var best = await Emerge.BestOf<Answer>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Count = 5;
    opt.Command = "Solve this problem step by step.";
    opt.Score = (answer, trace) => answer.Confidence * (1f / trace.Duration.TotalSeconds);

    opt.Candidate(c =>
    {
        c.Temperature = 0.7 + 0.1 * c.Index;  // Vary temperature per candidate
        c.Seed = 1000 + c.Index;
    });
}).ResultAsync();
```

**Options:**
- `Count` - Number of candidates (default: 3)
- `Score` - Scoring function `Func<T, EmergenceTrace, double>`
- `Candidate(Action<CandidateScope<T>>)` - Configure each candidate (has `Index`, `Seed`)
- `EnableCritic` - Enable critic-guided refinement of candidates (default: false)
- `Critic(Action<EmergeScope<T>>)` - Configure the critic scope
- `BuildCriticFeedback` - Custom function `Func<T, ScoreBreakdown?, string>` to build critic feedback
- `CriticMustImprove` - Require critic to improve on the current best (default: true)

**Context flow:** Each candidate runs with an isolated derived context.

---

### MapReduce — Chunk Processing

Split input into chunks, process each in parallel, then reduce to a final result.

```csharp
var report = await Emerge.MapReduce<ChunkSummary, FinalReport>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Chunks = documents.Select(d => (object)d).ToList();
    opt.MaxParallel = 8;

    opt.Map(m =>
    {
        m.Temperature = 0.5;
        m.Command = "Summarize the key points from this document chunk.";
    });

    opt.Reduce(r =>
    {
        r.Temperature = 0.3;
        r.Command = "Combine all chunk summaries into a comprehensive final report.";
    });
}).ResultAsync();
```

**Options:**
- `Chunks` - Pre-split input chunks (takes precedence if set)
- `Input` + `Split` - Or provide input with a split function (used only if `Chunks` is null)
- `MaxParallel` - Concurrency for map phase (default: 4)
- `Map(Action<EmergeScope<TChunk>>)` - Configure chunk processing
- `Reduce(Action<EmergeScope<TResult>>)` - Configure reduction

**Context flow:** Map runs use isolated contexts. All map outputs are collected and provided to Reduce in context.

---

### Refine — Iterative Improvement

Generate an initial result, then iteratively improve it based on feedback.

```csharp
var final = await Emerge.Refine<Code>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxRefinements = 3;

    opt.Initial(s =>
    {
        s.Command = "Write initial implementation of the feature.";
    });

    opt.Refinement(s =>
    {
        s.Command = "Improve the code based on the issues found.";
    });

    // Async validation - continue refining while there are errors
    opt.ShouldContinue = async (result, trace) =>
    {
        var error = await ValidateCodeAsync(result.Code);
        return error != null;
    };
}).ResultAsync();
```

**Options:**
- `MaxRefinements` - Maximum improvement iterations (default: 3)
- `ShouldContinue` - Async callback `Func<T, EmergenceTrace, Task<bool>>` to control refinement
- `Initial(Action<EmergeScope<T>>)` - Configure initial generation
- `Refinement(Action<EmergeScope<T>>)` - Configure refinement passes

**Context flow:** Each refinement automatically receives the previous attempt's JSON output in context.

---

### EnsembleMerge — Diverse Solutions Merged

Run multiple diverse solvers in parallel, then merge their outputs into a coherent result.

```csharp
var merged = await Emerge.EnsembleMerge<Analysis>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.SolverCount = 4;
    opt.MaxParallel = 4;

    opt.Solver(s =>
    {
        s.Temperature = 0.6 + 0.15 * s.Index;  // Varying temperatures
        s.Command = "Analyze this data from your unique perspective.";
    });

    opt.Merger(m =>
    {
        m.Temperature = 0.3;
        m.Command = "Synthesize all analyses into a comprehensive unified result.";
    });
}).ResultAsync();
```

**Options:**
- `SolverCount` - Number of parallel solvers (default: 3)
- `MaxParallel` - Concurrency limit (default: 3)
- `Solver(Action<AgentScope<T>>)` - Configure each solver (has `Index`, `Role`, `Seed`)
- `Merger(Action<EmergeScope<T>>)` - Configure the merger

**Context flow:** Solvers run with isolated contexts for deterministic parallel execution. Merger receives all solver outputs in context.

---

### TreeSearch — Document Tree Navigation

Navigate a hierarchical document index to find relevant sections without vector embeddings.

```csharp
// Step 1: Build a tree index from content
TreeIndex index = null;
await foreach (var ev in TreeIndex.BuildAsync(LLMModel.Claude45Sonnet, documentContent,
    new TreeIndexOptions { MaxDepth = 4, GenerateSummaries = true }))
{
    if (ev is Completed<TreeIndex> done)
    {
        index = done.Result;
    }
}

// Step 2: Search the tree
var result = await Emerge.TreeSearch<TreeSearchResult>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Index = index;
    opt.Query = "How does authentication work?";
    opt.MaxSteps = 10;
    opt.MaxResults = 3;

    opt.Navigator(n =>
    {
        n.Command = "Navigate the document tree to find sections relevant to the query.";
    });
}).ResultAsync();

// result.Sections contains found sections with NodeId, Path, Content, Relevance, Page
```

**Options:**
- `Index` - The `TreeIndex` to search
- `Query` - Search query
- `MaxSteps` - Maximum navigation steps (default: 10)
- `MaxResults` - Maximum sections to return (default: 5)
- `Navigator(Action<EmergeScope<NavigationDecision>>)` - Configure navigator

**Tree indexing types:**

`TreeIndex` builds a hierarchical document structure:
- `BuildAsync(model, string content, options?)` - Build from raw text
- `BuildAsync(model, IContentReader reader, options?)` - Build from custom reader
- `ToTableOfContents(maxDepth)` - Generate table of contents
- `FindById(id)` - Look up a node by ID

`TreeIndexOptions`: `MaxDepth` (default: 4), `MaxSummaryTokens` (default: 100), `GenerateSummaries` (default: true)

`TreeNode`: `Id`, `Title`, `Summary`, `Content`, `Page`, `Children`, `Parent`, `Depth`

`IContentReader` / `ContentSection`: Interface for custom content sources. `StringContentReader` wraps a plain string.

---

## Tool Registration

Tools are authored with the `Tool` vocabulary from `Ikon.Agent` and registered on the pass via `AddTool` / `AddTools`. `Tool.Of` infers the parameter schema from the lambda signature — parameter names carry through to the model, and `[Description]` attributes (from `System.ComponentModel`) document individual parameters. Tools are deduplicated by name.

```csharp
await foreach (var ev in Emerge.Run<CoderResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
{
    pass.AddTool(Tool.Of("write_file", "Write content to a file",
            ([Description("Workspace-relative path")] string path, string content) => WriteFile(path, content)))
        .AddTool(Tool.Of("read_file", "Read file contents",
            (string path) => ReadFile(path)))
        .AddTool(Tool.Of("list_files", "List all files",
            () => ListFiles()));

    pass.Command = "Complete this coding task.";
    pass.MaxIterations = 10;
    pass.MaxToolCalls = 50;
}))
{ ... }
```

**Methods:**
- `Tool.Of(name, description, lambda)` — sync or async lambda, up to 4 parameters; the schema the LLM sees is derived from the lambda's parameter names and `[Description]` attributes
- `Tool.OfContext(name, description, (ToolContext toolCtx, ...) => ...)` — like `Tool.Of` but the impl receives the live `ToolContext`; requires an `AgentRunner` scope when invoked
- `Tool.FromSchema(name, description, parameterSchemaJson, invoke)` — schema-first, for shapes a typed delegate cannot express (MCP-discovered tools, hand-authored schemas); `invoke` receives the raw `JsonElement` arguments
- `AddTool(Tool)` / `AddTools(params Tool[])` — register on the pass, skipping tools whose name is already present; both return `EmergePass<T>` for chaining
- `tool.WithParamDescription(paramName, description)` / `tool.WithAllowedValues(paramName, values)` — per-pass dynamic parameter docs and enums on a copy of the tool
- Pre-built `Function` objects go directly onto the pass via `pass.Tools.Add(function)`

**Many-parameter tools — request record.** `Tool.Of` tops out at 4 parameters by design. A tool that needs more takes a single request record; `[property: Description]` documents each field:

```csharp
public sealed record CreateEventRequest(
    [property: Description("Event title shown in the calendar")] string Title,
    [property: Description("ISO-8601 start time")] string Start,
    [property: Description("ISO-8601 end time")] string End,
    [property: Description("Optional location")] string? Location,
    [property: Description("Attendee emails")] string[]? Attendees);

pass.AddTool(Tool.Of("create_event", "Create a calendar event",
    (CreateEventRequest request) => CreateEvent(request)));
```

**MCP tools.** Wrap a connected `McpClient` in an `McpSkill` — it yields one `Tool.FromSchema` per tool the server advertises, proxying calls back through the client:

```csharp
var mcpClient = new McpClient("https://example.com/mcp");
await mcpClient.ConnectAsync();
var skill = new McpSkill(mcpClient);

// As part of a Persona's skill set:
var persona = new Persona("Assistant", systemPrompt,
    Skills: [Built.Messaging, skill],
    Reasoning: new Reasoning());

// Or directly on a pass (requires an AgentRunner scope):
pass.AddTools(skill.Tools().ToArray());
```

---

## Structured Tag Parser

`StructuredTagParser` extracts XML-style tags from LLM responses, useful for structured output outside of JSON mode.

```csharp
using Ikon.AI.Emergence.Structured;

var parsed = StructuredTagParser.Parse(content, "reasoning", "answer");

// parsed.PlainText — text outside tags
// parsed.Blocks — list of ParsedBlock (TagName, Content, StartIndex, EndIndex)

// Utility methods
bool has = StructuredTagParser.HasTag(content, "reasoning");
string? text = StructuredTagParser.GetTagContent(content, "answer");
```

---

## KernelContext Extensions

Extension methods for inspecting tool call history in a `KernelContext`:

```csharp
bool hasFn = ctx.HasFunctionResults();
var results = ctx.GetFunctionResults(take: 10);  // IReadOnlyList<FunctionResultPart>
var calls = ctx.GetFunctionCalls(take: 10);       // IReadOnlyList<FunctionCall>
```

---

## Common Options Reference

All pattern options inherit these from `EmergeScopeBase`:

| Option | Type | Description |
|--------|------|-------------|
| `Model` | `LLMModel?` | Override the model |
| `Temperature` | `double?` | Sampling temperature |
| `MaxOutputTokens` | `int?` | Maximum output tokens |
| `ReasoningEffort` | `ReasoningEffort?` | Reasoning effort level |
| `ReasoningTokenBudget` | `int?` | Token budget for reasoning |
| `Timeout` | `TimeSpan?` | Request timeout |
| `Regions` | `IReadOnlyList<ModelRegion>?` | Model region preferences |
| `MaxIterations` | `int?` | Max agentic iterations |
| `MaxToolCalls` | `int?` | Max tool calls |
| `MaxWallTime` | `TimeSpan?` | Max wall clock time |
| `MaxRetries` | `int?` | Max retries on transient failures |
| `RetryDelay` | `TimeSpan?` | Delay between retries |
| `SystemPrompt` | `string?` | System instruction |
| `Command` | `string?` | User command |
| `Tools` | `IList<Function>` | Available tools |
| `UseLastNMessages` | `int?` | Context window limit |
| `SkipLastNMessages` | `int?` | Skip N most recent messages |
| `OptimizeContext` | `bool?` | Enable context optimization |
| `UseCitations` | `bool?` | Enable citations |
| `IncludeJsonExample` | `bool?` | Include JSON example in prompt (default: true) |

`UseLastMessages(count, skipLast)` is a convenience method for setting both `UseLastNMessages` and `SkipLastNMessages`.

`EmergeScope<T>` adds `UseJson` (default: true), `CaseInsensitiveJson` (default: true), `JsonSchema`, and `JsonExample` (both read-only, auto-generated from `T`).

### EmergenceTrace

Returned with `Completed<T>` events:

| Property | Type | Description |
|----------|------|-------------|
| `Iterations` | `int` | Number of LLM iterations |
| `ToolCalls` | `int` | Number of tool calls made |
| `InputTokens` | `long` | Total input tokens consumed |
| `CachedInputTokens` | `long` | Input tokens served from prompt cache |
| `CacheCreationInputTokens` | `long` | Input tokens written into prompt cache |
| `OutputTokens` | `long` | Total output tokens generated |
| `Duration` | `TimeSpan` | Total wall time |
| `ToolCallHistory` | `IReadOnlyList<FunctionCall>` | Full tool call history |
| `FinishReason` | `string?` | Model finish reason (e.g., "length", "max_tokens") |
| `Error` | `Exception?` | Error if one occurred |
| `IsTruncated` | `bool` | True when `FinishReason` indicates output was cut short |

## Testing with Mock LLM

All pattern methods have an overload accepting `ILLM` for testing:

```csharp
var mockLlm = new MockLLM(responses);

var result = await Emerge.Run<MyType>(
    LLMModel.Claude45Sonnet,
    pass => { ... },
    mockLlm  // Injected for testing
).ResultAsync();
```
