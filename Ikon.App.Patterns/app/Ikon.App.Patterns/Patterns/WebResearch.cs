namespace Ikon.App.Patterns.Patterns;

// Pattern: web-research — see docs/patterns/web-research.md.
// Fully self-contained: the example region is the canonical two-stage WebSearcher → Emerge.Run
// research flow, its two-phase loading reactive, and the answers-with-sources render.
internal sealed class WebResearch : IPatternDemo
{
    public string Slug => "web-research";
    public string Title => "Web research";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-web-research
    public sealed record Answer(string Question, string Synthesis, List<Source> Sources);
    public sealed record Source(string Title, string Url, string Snippet);

    private readonly ReactiveList<Answer> _answers = new();
    // Per user, so a reload keeps a half-typed question
    private readonly UserReactive<string> _question = new("");
    private readonly Reactive<string?> _phase = new(null); // null | "Searching" | "Synthesizing"
    private readonly Reactive<string?> _error = new(null);

    private async Task ResearchAsync()
    {
        var q = _question.Value.Trim();

        if (string.IsNullOrEmpty(q) || _phase.Value != null)
        {
            return;
        }

        _phase.Value = "Searching";
        _error.Value = null;
        try
        {
            using var searcher = new WebSearcher(WebSearcherModel.Google);
            var results = await searcher.SearchAsync(q, maxResults: 5);

            _phase.Value = "Synthesizing";
            var sources = results.Select(r => new Source(r.Title, r.Url, r.Content)).ToList();
            var context = string.Join("\n", sources.Select((s, i) => $"[{i + 1}] {s.Title}\n{s.Snippet}"));

            var synthesisRaw = await Emerge.Run<string>(LLMModel.Claude55Sonnet,
                pass => { pass.Command = $"Question: {q}\n\nSearch results:\n{context}\n\nWrite a concise answer citing sources by number [1], [2]…"; });
            var synthesis = string.IsNullOrEmpty(synthesisRaw) ? "(no synthesis)" : synthesisRaw;

            _answers.Insert(0, new Answer(q, synthesis, sources)); // newest first
            _question.Value = "";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Web research for '{q}' failed, user can ask again: {ex.Message}");
            _error.Value = "Couldn't find an answer. Try again.";
        }
        finally
        {
            _phase.Value = null;
        }
    }

    private void Render(IView view)
    {
        view.Row([Layout.Row.Form, "p-4"], content: v =>
        {
            v.TextField(["default", "sm:flex-1"], value: _question.Value, placeholder: "What do you want to know?",
                onValueChange: async x => _question.Value = x,
                onSubmit: async _ => await ResearchAsync());
            v.Button([Button.Default, "transition-colors duration-150 hover:opacity-90", _phase.Value != null ? "opacity-50" : ""],
                disabled: _phase.Value != null, onClick: ResearchAsync,
                content: c => c.Text(text: _phase.Value ?? "Research"));
        });

        if (_phase.Value is string phase)
        {
            view.Box(["bg-card rounded-lg p-4 mx-4 animate-pulse"], content: v =>
                v.Text(["text-sm text-muted-foreground"], text: $"{phase}…"));
        }

        if (_error.Value is string err)
        {
            view.Alert(["default", "mx-4"], err, tone: SemanticTone.Error);
        }

        if (_answers.Count == 0 && _phase.Value == null)
        {
            view.Box(["text-center text-muted-foreground p-12"], content: v =>
                v.Text(text: "Ask a question to get an answer with sources."));
        }

        view.Column(["gap-4 p-4"], content: view =>
        {
            foreach (var ans in _answers)
            {
                view.Box(["bg-card rounded-lg p-4 gap-3"], content: v =>
                {
                    v.Text(["text-base font-semibold"], text: ans.Question);
                    v.Markdown(ans.Synthesis);
                    v.Column(["gap-1 pt-2 border-t"], content: vv =>
                    {
                        vv.Text(["text-xs uppercase tracking-wider text-muted-foreground"], text: "Sources");
                        for (int i = 0; i < ans.Sources.Count; i++)
                        {
                            var src = ans.Sources[i];
                            vv.Link(href: src.Url, style: ["text-xs text-brand-secondary hover:underline"],
                                content: c => c.Text(text: $"[{i + 1}] {src.Title}"));
                        }
                    });
                });
            }
        });
    }
    #endregion
}
