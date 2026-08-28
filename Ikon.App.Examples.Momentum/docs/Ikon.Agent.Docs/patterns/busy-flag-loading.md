# Busy Flag with Visible Loading State

The standard pattern for any async work — LLM call, image generation, web search, file upload. Reactive flag + button label change + try/catch + error surface.

## When to use

Every async action triggered from the UI. If you find yourself writing `async () => { await DoSlowThing(); }` directly on a button's onClick, you're missing this.

## Snippet

```csharp
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _error = new(null);

private async Task DoWorkAsync()
{
    if (_busy.Value)
    {
        return;
    }

    _error.Value = null;
    using var _ = _busy.AsToken(); // flips _busy true here, false on dispose

    try
    {
        await SlowOperationAsync();
    }
    catch (Exception ex)
    {
        _error.Value = ex.Message;
    }
}

private void Render(IView view)
{
    view.Button(
        style: [Button.Default, "transition-colors duration-150 hover:opacity-90", _busy.Value ? "opacity-50 cursor-wait" : ""],
        disabled: _busy.Value,
        onClick: DoWorkAsync,
        content: v => v.Text(text: _busy.Value ? "Working…" : "Do thing"));

    if (_error.Value is string err)
    {
        view.Box(["bg-destructive/10 text-destructive border border-destructive/30 rounded-lg p-3"], content: v =>
            v.Text(text: $"Failed: {err}"));
    }
}
```

## Notes

- `_busy` is `Reactive<bool>` — drives both `disabled` and the label change. The label change is the loading state for sub-2s work; for longer (image generation, multi-stage), add a Skeleton in the result area.
- `_busy.AsToken()` returns an `IDisposable` that flips the flag to `true` on acquire and back to `false` on dispose, even if the wrapped block throws. Replaces the `try/finally` boilerplate; you only need a `try/catch` to surface the error message.
- `_error` is `Reactive<string?>` — `null` means no error. Surface via an Alert / inline box. Never silent catches.
- Re-entry guard: `if (_busy.Value) return` at the top.
- **Catch the right exception type.** The standalone AI services (`ImageGenerator`, `SpeechGenerator`, `WebSearcher`, …) throw `AIException`, with `AITimeoutException` for the deadline case; `Emerge.Run<T>` throws `EmergenceStoppedException`, which does NOT derive from `AIException`. A single `catch (AIException)` around an Emerge call silently misses every failure it was written for.

## See also

- `chatbot-streaming` — busy flag specialised for AI conversation.
- `shared-list-ai-cleanup` — busy flag specialised for AI list transformation.
