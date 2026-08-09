# AI Image Gallery

Prompt input + Generate button + grid of generated thumbnails. Uses ImageGenerator (not Emerge.Run).

## When to use

Any app where the user generates / iterates / collects images: studio, mood board, character designer, hero-image picker, asset producer.

## Snippet

```csharp
public sealed record GalleryItem(string Id, string Prompt, byte[] Data, string MimeType);

private readonly ReactiveList<GalleryItem> _gallery = new();
private readonly Reactive<string> _prompt = new("");
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _error = new(null);

private async Task GenerateAsync()
{
    var prompt = _prompt.Value.Trim();
    if (string.IsNullOrEmpty(prompt) || _busy.Value) return;

    _error.Value = null;
    using var _ = _busy.AsToken();
    try
    {
        var image = await ImageGenerator.GenerateAsync(prompt);
        _gallery.Add(new GalleryItem(Guid.NewGuid().ToString("N"), prompt, image.Data, image.MimeType));
        _prompt.Value = "";
    }
    catch (Exception ex)
    {
        _error.Value = ex.Message;
    }
}

private void Remove(string id) =>
    _gallery.RemoveAll(g => g.Id == id);

// UI:
view.Row(["gap-2 p-4"], content: v =>
{
    v.TextField(["flex-1"], value: _prompt.Value, placeholder: "Describe the image…",
        onValueChange: async x => _prompt.Value = x,
        onSubmit: async _ => await GenerateAsync());
    v.Button(style: [Button.Default, "transition-colors duration-150 hover:opacity-90", _busy.Value ? "opacity-50" : ""],
        disabled: _busy.Value, onClick: GenerateAsync,
        content: c => c.Text(text: _busy.Value ? "Generating…" : "Generate"));
});

if (_error.Value is string err)
{
    view.Box(["bg-destructive/10 text-destructive border border-destructive/30 rounded-lg p-3 mx-4"], content: v =>
        v.Text(text: $"Generation failed: {err}"));
}

if (_gallery.Count == 0 && !_busy.Value)
{
    view.Box(["text-center text-muted-foreground p-12"], content: v =>
        v.Text(text: "No images yet. Try a prompt above."));
}
else
{
    view.Grid(["grid-cols-3 gap-3 p-4"], content: view =>
    {
        if (_busy.Value)
        {
            // Skeleton tile while generating
            view.Box(["aspect-square bg-surface rounded-lg animate-pulse"], content: _ => { });
        }
        foreach (var item in _gallery)
        {
            view.Box(["relative group rounded-lg overflow-hidden bg-surface aspect-square hover:ring-2 hover:ring-primary transition-all duration-150"], content: v =>
            {
                v.Image(["w-full h-full object-cover"], data: item.Data, mimeType: item.MimeType, alt: item.Prompt);
                v.Button(
                    style: ["absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-150", Button.ErrorMd],
                    onClick: () => Remove(item.Id),
                    content: c => c.Text(text: "Remove"));
            });
        }
    });
}
```

## Notes

- Use `ImageGenerator`, not `Emerge.Run<string>`. The LLM can describe images; only `ImageGenerator` actually creates them.
- Render bytes inline as a data URI; for production scale, persist via `IkonAssetBackend` (an `IAssetBackend`) and reference by URL.
- Skeleton tile during generation gives immediate feedback before the image arrives.
- Empty state: helpful prompt, not blank.
- `group-hover:` pattern reveals the Remove button on hover; cleaner than always-visible.

## See also

- `busy-flag-loading` — the underlying async pattern.
- `ai-image` (top-level guide) — full ImageGenerator API including model choice, size, count.
- `chatbot-streaming` — different async flavor (single LLM, transcript).
