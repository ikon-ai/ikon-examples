# Web Research — Multi-Stage AI

WebSearcher fetches results, then Emerge.Run synthesizes a concise answer with citation links. Two-phase loading state, sources displayed alongside each answer.

## When to use

Research assistant, news summarizer, fact-checker, "ask the internet" tools, citation-aware Q&A. Anywhere the answer needs to be grounded in web data, not just LLM memory.

## Snippet

```csharp
public sealed record Answer(string Question, string Synthesis, List<Source> Sources);
public sealed record Source(string Title, string Url, string Snippet);

private readonly Reactive<List<Answer>> _answers = new([]);
private readonly Reactive<string> _question = new("");
private readonly Reactive<string?> _phase = new(null); // null | "Searching" | "Synthesizing"
private readonly Reactive<string?> _error = new(null);

private async Task ResearchAsync()
{
    var q = _question.Value.Trim();
    if (string.IsNullOrEmpty(q) || _phase.Value != null) return;

    _phase.Value = "Searching";
    _error.Value = null;
    try
    {
        using var searcher = new WebSearcher(WebSearcherModel.Google);
        var results = await searcher.SearchPagesAsync(new SearchConfig { Query = q, MaxResults = 5 });

        _phase.Value = "Synthesizing";
        var sources = results.Select(r => new Source(r.Title, r.Url, r.Content)).ToList();
        var context = string.Join("\n", sources.Select((s, i) => $"[{i + 1}] {s.Title}\n{s.Snippet}"));

        var (synthesisRaw, _) = await Emerge.Run<string>(LLMModel.Claude46Sonnet, new KernelContext(),
            pass => { pass.Command = $"Question: {q}\n\nSearch results:\n{context}\n\nWrite a concise answer citing sources by number [1], [2]…"; })
            .FinalAsync();
        var synthesis = string.IsNullOrEmpty(synthesisRaw) ? "(no synthesis)" : synthesisRaw;

        _answers.Value = [new Answer(q, synthesis, sources), .. _answers.Value]; // newest first
        _question.Value = "";
    }
    catch (Exception ex)
    {
        _error.Value = ex.Message;
    }
    finally
    {
        _phase.Value = null;
    }
}

// UI:
view.Row(["gap-2 p-4"], content: v =>
{
    v.TextField(["flex-1"], value: _question.Value, placeholder: "What do you want to know?",
        onValueChange: async x => _question.Value = x,
        onSubmit: async _ => await ResearchAsync());
    v.Button(style: [Button.Default, "transition-colors duration-150 hover:opacity-90", _phase.Value != null ? "opacity-50" : ""],
        disabled: _phase.Value != null, onClick: ResearchAsync,
        content: c => c.Text(text: _phase.Value ?? "Research"));
});

if (_phase.Value is string phase)
{
    view.Box(["bg-surface rounded-lg p-4 mx-4 animate-pulse"], content: v =>
        v.Text(["text-sm text-muted-foreground"], text: $"{phase}…"));
}

if (_error.Value is string err)
{
    view.Box(["bg-destructive/10 text-destructive border border-destructive/30 rounded-lg p-3 mx-4"], content: v =>
        v.Text(text: err));
}

if (_answers.Value.Count == 0 && _phase.Value == null)
{
    view.Box(["text-center text-muted-foreground p-12"], content: v =>
        v.Text(text: "Ask a question to get an answer with sources."));
}

view.Column(["gap-4 p-4"], content: view =>
{
    foreach (var ans in _answers.Value)
    {
        view.Box(["bg-surface rounded-lg p-4 gap-3"], content: v =>
        {
            v.Text(["text-base font-semibold"], text: ans.Question);
            v.Text(["text-sm whitespace-pre-wrap"], text: ans.Synthesis);
            v.Column(["gap-1 pt-2 border-t"], content: vv =>
            {
                vv.Text(["text-xs uppercase tracking-wider text-muted-foreground"], text: "Sources");
                for (int i = 0; i < ans.Sources.Count; i++)
                {
                    var src = ans.Sources[i];
                    vv.Link(href: src.Url, style: ["text-xs text-primary hover:underline"],
                        content: c => c.Text(text: $"[{i + 1}] {src.Title}"));
                }
            });
        });
    }
});
```

## Notes

- Two-phase loading via a single nullable `Reactive<string?>` — `null` (idle) / `"Searching"` / `"Synthesizing"`. Button label and shimmer panel both react.
- WebSearcher does the actual fetch; Emerge.Run synthesizes. **Don't shortcut by asking the LLM directly** — the answers go stale.
- The fetch call is `searcher.SearchPagesAsync(new SearchConfig { Query = ..., MaxResults = ... })`. There is no positional `SearchAsync(string, int)` overload — use the config object. `SearchImagesAsync` is the image-search counterpart.
- `SearchResult` exposes `Url`, `Title`, `Content`, `Mimetype`, `Keywords`. There is no `Snippet` property — use `Content` for the body text.
- Sources displayed alongside the synthesis with clickable links. Citation numbers in the prose match the list.
- Newest-first prepend (`[new, ..old]`).

## See also

- `busy-flag-loading` — single-phase async pattern this generalises.
- `ai-web-and-data` (top-level) — full WebSearcher API + alternative search models.
- `chatbot-streaming` — single-LLM-call pattern without web grounding.
