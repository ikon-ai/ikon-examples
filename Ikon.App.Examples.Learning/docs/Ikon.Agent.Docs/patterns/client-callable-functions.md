<!-- mined-from: Ikon.App.Patterns -->
# Client-Callable Functions — Registering What a Client May Call

A method an app writes is invisible to its frontend until the function registry advertises it.
`[Function]` marks the method, `[RegisterAll]` sets the default for a whole class, and
`FunctionRegistry.Instance` does the registering. The registry is also readable, so the list of what
a client may call is derivable rather than hand-maintained.

## When to use

Any time the frontend, an LLM tool call, or a webhook has to reach app code by name: a lookup the UI
performs, an action a tool exposes, an endpoint a partner posts to.

## The two things that go wrong

**Visibility defaults to `Local`, and `Local` means invisible to a client.** A bare `[Function]`
outside a `[RegisterAll]` class registers in-process only — callable from your own code and from
nowhere else. Nothing fails: the method exists, the registration succeeds, and the client's call
finds no such function. Set `Visibility = FunctionVisibility.External` on the class, or on each
method that needs it.

**An external function must declare its auth posture.** `[RequireLogin]` or `[AllowAnonymous]` — a
startup audit warns when an `External` function has neither, because the default otherwise depends
on reading the registration code to find out. Put it on the class and every method inherits it.

## Two more contracts worth knowing

- **The default name embeds the namespace.** Without `Name`, a function is registered as the full
  type name plus the method name, so moving or renaming the class renames the endpoint and every
  client calling it breaks. Naming it explicitly decouples the wire name from the code layout.
- **`FunctionRegistry.Instance.Functions` is keyed by NAME, and each entry holds every registered
  version** — `IReadOnlyDictionary<string, IReadOnlyList<Function>>`, not a flat list. Flatten the
  values to enumerate what is registered.

`RegisterFromInstance` is for instance methods and keeps whatever the instance closes over;
`RegisterFromType(typeof(X))` is the static-only equivalent and needs no instance.

## Snippet

```csharp
// Every method here is advertised over the protocol, because [RegisterAll] sets the default and
// each [Function] inherits it. Without the class attribute a bare [Function] is Local: callable
// in-process and invisible to a client, which is the quiet version of "my endpoint 404s".
[RegisterAll(Visibility = FunctionVisibility.External)]
[RequireLogin]
private sealed class CatalogFunctions(Reactive<int> viewCount)
{
    [Function("Look a product up by its SKU")]
    public string Describe(string sku) => $"Product {sku}";

    // The name a client calls is the full type name plus the method name unless Name is set.
    // Set it: the default embeds the namespace, so moving the class renames the endpoint.
    [Function(Name = "catalog.views", Description = "How many times the catalog was opened")]
    public int Views() => viewCount.Value;
}

private readonly Reactive<int> _viewCount = new(0);
private readonly Reactive<string?> _registered = new(null);

private void Register()
{
    // RegisterFromInstance for instance methods — the closure over _viewCount is the point;
    // RegisterFromType(typeof(X)) is the static-only equivalent and needs no instance.
    FunctionRegistry.Instance.RegisterFromInstance(new CatalogFunctions(_viewCount));

    // The registry is readable, which is how an app shows a client what it may call rather than
    // maintaining a second hand-written list that drifts.
    var external = FunctionRegistry.Instance.Functions
        // Keyed by NAME, and each entry holds every registered version of it.
        .SelectMany(entry => entry.Value)
        .Where(f => f.Visibility == FunctionVisibility.External)
        .Select(f => $"{f.Name}({string.Join(", ", f.Parameters.Select(p => p.Name))})")
        .Order(StringComparer.Ordinal)
        .ToList();

    _registered.Value = string.Join("  ·  ", external);
}
```
