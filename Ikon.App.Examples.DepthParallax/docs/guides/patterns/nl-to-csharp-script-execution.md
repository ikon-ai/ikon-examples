<!-- mined-from: Ikon.App.Examples.DynamicUI -->
# NL To C# Script Execution — Roslyn-Compiled UI From Chat

A two-pane app where the left side is a chat asking "what UI do you want?" and the right side renders the answer live. The LLM produces a snippet of C# Parallax UI code which is compiled with `Microsoft.CodeAnalysis.CSharp.Scripting`, executed *during render* against a `UIView` global, and validated with a refinement loop on syntax errors.

## When to use

You're building an LLM-driven UI generator, a "show me a chart of X" agent, or any tool where the user describes a component and gets a live, interactive instance back. The shared-state dictionary lets generated code keep state across renders without recompilation.

## Snippet

```csharp
public class UIScriptGlobals
{
    public UIView view { get; }
    public SharedState state { get; }
    public UIScriptGlobals(UIView view, SharedState sharedState) { this.view = view; state = sharedState; }
}

public class SharedState
{
    private readonly Dictionary<string, object> _state = new();
    public T GetOrCreate<T>(string key, Func<T> factory) where T : class
    {
        if (_state.TryGetValue(key, out var existing) && existing is T typed) return typed;
        var newValue = factory();
        _state[key] = newValue!;
        return newValue;
    }
}

private static ScriptOptions CreateScriptOptions() => ScriptOptions.Default
    .AddReferences(/* DynamicUI, Reactive<>, UIView, etc. */)
    .AddImports("System", "System.Linq", "System.Threading.Tasks",
        "Ikon.Common.Core.Reactive", "Ikon.Parallax",
        "Ikon.Parallax.Components.Standard", "Ikon.Parallax.Theming");

private (bool Success, string? Error) ExecuteCodeSync(string code, UIView uiView)
{
    try
    {
        var options = CreateScriptOptions();
        var globals = new UIScriptGlobals(uiView, _sharedState);
        CSharpScript.RunAsync(code, options, globals).GetAwaiter().GetResult();
        return (true, null);
    }
    catch (CompilationErrorException ex)
    {
        var errors = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
        return (false, $"Compilation error:\n{errors}");
    }
}

// In RenderUIPanel:
view.Box(["flex-1 p-4 overflow-auto"], content: uiView =>
{
    _ = _executionVersion.Value;       // reactive dep so a Clear State bumps re-run
    var (success, error) = ExecuteCodeSync(_lastGeneratedCode.Value, uiView);
    if (!success && error != null) _lastError.Value = error;
});
```

## Notes

- Execute *synchronously during render* — UI action callbacks (`onClick`) require the script body to register them before the render method returns.
- Persist state in `SharedState` keyed by string ("counter", "todoList") so the same recompiled code finds its `Reactive<int>` between renders.
- Pair with `Emerge.Refine<UICodeResponse>` and `opt.ShouldContinue = (r,_) => ValidateSyntaxAsync(r.Code) != null` to auto-fix compile errors without bothering the user.
- Cache `ScriptOptions` — building the metadata reference list is slow.

## See also

- `plan-then-code-iteration`
- `expandable-detail-card`
- `ai-prefill-form-from-description`
