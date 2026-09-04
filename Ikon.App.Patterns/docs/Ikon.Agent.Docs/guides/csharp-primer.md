# C# Language Primer

## C# Language Primer

Ikon AI Apps target the **latest C#** (C# 14 on .NET 10). Use modern idioms; avoid enterprise patterns and unnecessary abstractions. The Coder agent should write code that looks like a 2025 senior engineer's natural style, not 2010 layered-architecture C#.

### Use these modern constructs

- **Records** for value types: `public record TodoItem(string Id, string Text, bool Done = false);` — never plain DTOs with mutable properties unless mutability is the point.
- **Primary constructors** on classes: `public class MyApp(IApp<...> app) { ... }` — inject dependencies through the constructor parameter list, not separate fields + assignments.
- **Collection expressions**: `string[] arr = ["a", "b", "c"];`, `List<int> nums = [1, 2, 3];`. Inline into the call site when assigning conditionally — see "Common syntax mistakes" below.
- **Raw string literals**: `var prompt = """..."""` (triple-quote) for multiline strings; `var json = $$"""...{{x}}..."""` for templated multiline. NO `\n` escape soup.
- **Pattern matching**: `if (msg is ChatMessage cm) { ... }`, `var label = state switch { Loading => "...", Error e => $"!{e.Message}", _ => "ok" };` — prefer this over chains of `if/else if (x is …)`.
- **File-scoped namespaces**: `namespace Foo;` at the top, no nested braces.
- **Top-level statements**: `return await App.Run(args);` is the first *statement* in the app file — after any `using` directives (usings must precede it, or you get CS1529). No `class Program { static void Main() { ... } }`.
- **Target-typed `new()`**: `Dictionary<string, int> map = new();` — drop the right-hand `Dictionary<string,int>` repetition.
- **`required` properties** instead of constructor parameters when there are many: `public required string Name { get; init; }`.

### Async / await

- **`async Task` / `async Task<T>` everywhere** for I/O, LLM calls, file I/O, network, database. Never block on `.Result` / `.Wait()` / `GetAwaiter().GetResult()` (those deadlock under sync contexts and starve thread pools).
- **`await foreach`** for streaming: `await foreach (var ev in Emerge.Run<T>(...)) { ... }`.
- **`ValueTask` / `ValueTask<T>`** for hot paths that often complete synchronously (caches, state lookups). Default to `Task` otherwise — `ValueTask` has more rules.
- **`IAsyncEnumerable<T>`** for streams of values — pair with `await foreach` on the consumer side.
- **Cancellation tokens** flow through every awaitable: `Task DoX(CancellationToken ct = default)`. Pass them down — don't drop them into `default` mid-call.
- **No `Thread.Sleep`** in async code — use `await Task.Delay(ms, ct)`.

### Common syntax mistakes (these all appear as compile errors)

- **Dictionary literals**: C# uses `[key] = value`, NOT JSON's `key: value`.
  - Wrong: `new Dictionary<string,string> { "k": "v" }` → a `CS1002` / `CS1513` syntax error at the colon.
  - Right: `new Dictionary<string,string> { ["k"] = "v" }`.
  - Same applies to any `IDictionary<,>` initializer (Reactive<Dictionary<...>>, route maps, etc.).

- **Collection expression target typing**: both branches of a `?:` must have a target type.
  - Wrong: `var x = cond ? ["a"] : ["b"];` → `CS0173` (no common type).
  - Right (inline): `view.Box(cond ? ["a"] : ["b"], ...)` — the parameter type supplies inference.
  - Right (typed local): `string[] x = cond ? ["a"] : ["b"];`.

- **Lambda shape per callback**: `view.Button(onClick: () => ...)` is parameterless (sync or async both fine); `view.TextField(onSubmit: async value => ...)` takes the submitted value and returns a Task. Giving `onSubmit` a parameterless lambda (or `onClick` a parameterized one) fails — `CS8917` ("delegate type could not be inferred") or an argument error.

- **Explicit `using` for Ikon namespaces**: GlobalUsings already imports them, so a per-file `using` is redundant (harmless but unnecessary) — just write the type name. Only a made-up namespace that doesn't exist (e.g. `using Ikon.NotReal;`) produces `CS0234`.

- **Null-forgiving on framework calls**: don't `!` your way past `CS8602` (possibly null reference) on `.Value` of `Reactive<T>` — those are non-nullable by contract. If you see this warning on Ikon types, you're holding it wrong.

### Avoid these enterprise anti-patterns

The codebase is intentionally NOT layered, NOT DDD-onion, NOT IUnitOfWork-around-EF. Don't introduce them.

- **No factory factories.** A `Func<IFoo>` parameter beats `IFooFactory.Create()`.
- **No "I" prefix on every type.** Interfaces only when there is a real second implementation today, not "for testing" speculation.
- **No abstract base classes for one concrete class.** Just write the class.
- **No `IUnitOfWork`, `IRepository<T>`, `IService` ceremony.** Talk to the platform's storage APIs directly (`Asset.Instance`, `await app.DatabaseAsync("name")`, `PersistentReactive<T>`).
- **No DI container.** The app is wired via primary constructor parameters. Don't pull in Microsoft.Extensions.DependencyInjection.
- **No "Manager / Helper / Service / Provider" naming when a verb works.** `RoomScheduler` not `RoomManagementService`.
- **No mock-heavy testing.** Tests run against real implementations or the platform's in-memory variants. Mocks are a smell, not a strategy.
- **No `try { ... } catch (Exception) { }` swallow blocks.** Either handle the specific exception, log it, or let it bubble.
- **No defensive `if (x == null) return;` walls.** `Reactive<T>` is non-null. Method parameters annotated non-nullable are non-null. Trust the type system.
- **No "ProcessXAsync" / "HandleYAsync" wrapping a single LLM call.** Inline it. The verb is already in the lambda.

### When in doubt

Pick the option a reader of *new* C# code would write today. If the option you're considering would have looked normal in C# 7, but feels heavy in C# 14 — the C# 14 form is correct.
