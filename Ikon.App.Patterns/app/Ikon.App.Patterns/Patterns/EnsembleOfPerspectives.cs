namespace Ikon.App.Patterns.Patterns;

// Pattern: ensemble-of-perspectives — see docs/patterns/ensemble-of-perspectives.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class EnsembleOfPerspectives : IPatternDemo
{
    public string Slug => "ensemble-of-perspectives";
    public string Title => "Ensemble of named perspectives";
    public string Category => "Conversational AI";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Review(string Summary, IReadOnlyList<string> Risks);

    #region docsnippet:pattern-ensemble-of-perspectives
    private readonly Reactive<Review?> _review = new(null);

    // BestOf picks one candidate; EnsembleMerge MERGES several. Reach for this when the answer
    // should contain every perspective rather than the best single one.
    private static readonly string[] Perspectives =
    [
        "the security reviewer",
        "the accessibility reviewer",
        "the performance reviewer",
    ];

    private async Task ReviewAsync(string proposal)
    {
        var merged = await Emerge.EnsembleMerge<Review>(LLMModel.Claude46Sonnet, new KernelContext(), options =>
        {
            options.Command = proposal;
            options.SolverCount = Perspectives.Length;

            // MaxParallel must be at least 1 -- there is no "unbounded" sentinel, and 0 is a
            // configuration error rather than a request for no limit.
            options.MaxParallel = 3;

            // AgentScope.Role is prepended to that solver's system prompt, which is what makes the
            // members differ. Left unset they default to Solver0, Solver1 … and differ only by
            // Seed, which is a much weaker form of divergence.
            options.SolverConfig = solver => solver.Role = Perspectives[solver.Index % Perspectives.Length];

            // The merger is its own scope with its own instructions -- it decides how the
            // perspectives combine, and without steering it will simply concatenate them.
            options.Merger(merger =>
                merger.Command = "Combine the reviews. Keep every distinct risk; drop duplicates.");
        });

        _review.Value = merged;
    }

    private void Render(IView view)
    {
        if (_review.Value is not { } review)
        {
            return;
        }

        view.Column(["gap-2"], content: col =>
        {
            col.Text(text: review.Summary);

            foreach (var risk in review.Risks)
            {
                col.Text(["text-muted-foreground text-sm"], key: risk, text: risk);
            }
        });
    }
    #endregion
}
