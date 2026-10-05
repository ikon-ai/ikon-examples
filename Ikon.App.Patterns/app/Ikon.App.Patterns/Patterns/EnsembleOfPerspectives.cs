namespace Ikon.App.Patterns.Patterns;

// Pattern: ensemble-of-perspectives — see docs/patterns/ensemble-of-perspectives.md.
// The example region below is the canonical body the doc extracts.
internal sealed class EnsembleOfPerspectives : IPatternDemo
{
    public string Slug => "ensemble-of-perspectives";
    public string Title => "Ensemble of named perspectives";
    public string Category => "Conversational AI";

    private sealed record Review(string Summary, IReadOnlyList<string> Risks);

    // The gallery never calls the model: the demo starts with the kind of merged review three
    // reviewers would produce for one proposal.
    private static readonly Review SampleReview = new(
        "Magic-link sign-in is sound if links are single-use and short-lived, the email is readable without images, and sending is kept off the request path.",
        [
            "Security: a link that stays valid after use or for more than 15 minutes can be replayed from a forwarded email",
            "Security: the sign-in endpoint needs per-address rate limiting, or it becomes a way to flood an inbox",
            "Accessibility: the email's call to action must be a real link with descriptive text, not an image button",
            "Performance: sending the email inline adds the mail provider's latency to every sign-in request; queue it",
        ]);

    public EnsembleOfPerspectives() => _review.Value = SampleReview;

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Text(["text-xs text-zinc-400"],
                "A sample merged review of \"let users sign in with a magic link\" from a security, an accessibility and a performance reviewer; no model is called");
            Render(col);
        });
    }

    #region example:pattern-ensemble-of-perspectives
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
        // All or nothing: if any solver produces no result the run stops without merging, and
        // the await throws EmergenceStoppedException.
        var merged = await Emerge.EnsembleMerge<Review>(LLMModel.Claude46Sonnet, new KernelContext(), options =>
        {
            options.Command = proposal;
            options.SolverCount = Perspectives.Length;

            // MaxParallel must be at least 1 -- there is no "unbounded" sentinel, and 0 is a
            // configuration error rather than a request for no limit.
            options.MaxParallel = 3;

            // AgentScope.Role is added to that solver's system prompt, which is what makes the
            // members differ. Left unset they default to Solver0, Solver1 … and differ only by
            // that generic name, which is a much weaker form of divergence.
            options.SolverConfig = solver => solver.Role = Perspectives[solver.Index % Perspectives.Length];

            // The merger inherits these options, its own settings on top -- unset, its Command is
            // the proposal. It decides how the perspectives combine, and without steering it will
            // simply concatenate them.
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
