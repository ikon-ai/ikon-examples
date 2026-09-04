namespace Ikon.App.Patterns.Patterns;

// Pattern: best-of-with-rubric — see docs/patterns/best-of-with-rubric.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class BestOfWithRubric : IPatternDemo
{
    public string Slug => "best-of-with-rubric";
    public string Title => "Best-of with a weighted rubric";
    public string Category => "Conversational AI";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-best-of-with-rubric
    private sealed record Tagline(string Text, IReadOnlyList<string> Keywords);

    private readonly Reactive<string?> _winner = new(null);
    private readonly Reactive<string?> _breakdown = new(null);

    // A rubric of named, weighted metrics beats one opaque number: the critic can be told which
    // metric was weakest, and the breakdown is readable when a choice looks wrong.
    private static readonly ScoreBreakdownBuilder<Tagline> Rubric = new ScoreBreakdownBuilder<Tagline>()
        // EVERY metric must return [0, 1]. A rubric left on a 0..10 scale clamps to 1.0 for every
        // candidate and the ranking silently stops discriminating -- divide by the max here.
        .Metric("brevity", 0.4, t => Math.Clamp(1.0 - t.Text.Length / 80.0, 0, 1))
        .Metric("keywords", 0.6, t => Math.Min(t.Keywords.Count, 3) / 3.0);

    private async Task GenerateAsync(string brief)
    {
        var best = await Emerge.BestOf<Tagline>(LLMModel.Claude46Sonnet, new KernelContext(), options =>
        {
            options.Command = brief;
            options.Count = 4;

            // ScoreDetailed takes precedence over Score and is what hands the breakdown to the
            // critic. Set neither and every candidate scores 0.0 -- the FIRST one then always
            // wins, after paying for all Count runs.
            options.ScoreDetailed = (candidate, _) => Rubric.Score(candidate);

            // Configuring the critic also ENABLES it; an explicitly configured critic that never
            // ran was the trap this shape avoids.
            options.BuildCriticFeedback = (candidate, breakdown) =>
                $"Weakest: {breakdown?.Weakest?.Name}. Improve it without losing the rest.";
            options.CriticMustImprove = true;

            // Candidates diverge by Seed -- not a sampler seed and not reproducible, just a
            // nudge so four runs do not return four copies.
            options.CandidateConfig = candidate => candidate.Seed = candidate.Index;
        });

        _winner.Value = best.Text;
        _breakdown.Value = Rubric.Score(best).FormatBreakdown();
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            col.Button(
                onClick: async () => await GenerateAsync("a tagline for a calm budgeting app"),
                content: v => v.Text(text: "Generate"));

            if (_winner.Value is { } winner)
            {
                col.Text([Text.H3], text: winner);
                col.Text(["text-muted-foreground text-xs whitespace-pre"], text: _breakdown.Value);
            }
        });
    }
    #endregion
}
