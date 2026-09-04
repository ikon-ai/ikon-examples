namespace Ikon.App.Patterns.Patterns;

// Pattern: busy-flag-loading — see docs/patterns/busy-flag-loading.md.
// The docsnippet region below is the canonical body the doc extracts; the stub outside it stands in
// for the caller's real async work.
internal sealed class BusyFlagLoading : IPatternDemo
{
    public string Slug => "busy-flag-loading";
    public string Title => "Busy-flag loading";
    public string Category => "State";
    public void RenderDemo(IView view) => Render(view);

    private Task SlowOperationAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-busy-flag-loading
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
    #endregion
}
