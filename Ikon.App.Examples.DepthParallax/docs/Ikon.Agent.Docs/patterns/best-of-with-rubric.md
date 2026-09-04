<!-- mined-from: Ikon.App.Patterns -->
# Best-Of With A Rubric — Scoring That Actually Discriminates

`Emerge.BestOf` runs N candidates and keeps the highest scorer, but it only ranks as well as the
score it is given. A `ScoreBreakdownBuilder` of named, weighted metrics beats one opaque number
twice over: the critic can be told which metric was weakest, and a choice that looks wrong is
readable afterwards.

## When to use

Generation where quality varies run to run and "best of four" is worth paying for — taglines,
titles, summaries, layout choices, anything with a rubric you can write down. Candidates run
sequentially, so budget wall time for `Count` full calls.

## Notes

- **Every metric must return `[0, 1]`.** Values outside are clamped, so a rubric left on a 0..10
  or 0..100 scale collapses to 1.0 for every candidate and the ranking silently stops
  discriminating. Divide by the scale's maximum inside the callback.
- **Set `Score` or `ScoreDetailed`.** With neither, every candidate scores 0.0 and the FIRST one
  always wins — after paying for all `Count` runs. `ScoreDetailed` takes precedence over `Score`
  and is what hands a `ScoreBreakdown` to the critic.
- **Configuring the critic enables it.** Setting `BuildCriticFeedback` turns the critic on; an
  explicitly configured critic that silently never ran was the trap. Set `EnableCritic = false`
  afterwards for the rare case of pre-configuring one to toggle later.
- `CriticMustImprove` keeps the critic's rewrite only when it scores better than the winner it was
  given.
- `CandidateScope.Seed` is not a sampler seed and does not make a run reproducible — the chat
  models expose none. It only nudges sibling candidates to diverge.
- `ScoreBreakdown.Weakest` names the lowest weighted metric, and `FormatBreakdown()` renders the
  whole thing — useful in a debug panel when a ranking surprises you.
- The breakdown passed to `BuildCriticFeedback` is non-null exactly when `ScoreDetailed` produced
  it, and null when ranking with the plain `Score` delegate.

## Snippet

```csharp
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
```

## See also

- `refine-with-validation-loop` — self-correction against a hard check rather than a rubric.
- `emergence-event-feed` — watching a multi-candidate run as it happens.
