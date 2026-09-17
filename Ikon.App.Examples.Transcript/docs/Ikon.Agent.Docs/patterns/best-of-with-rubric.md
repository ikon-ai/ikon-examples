<!-- mined-from: Ikon.App.Patterns -->
# Best-Of With A Rubric — Scoring That Actually Discriminates
<!-- checked-against: 1f3b996992733c0b -->
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
- **Set a scorer.** With none, every candidate scores 0.0 and the FIRST one always wins — after
  paying for all `Count` runs. `ScoreDetailedAsync` and `ScoreDetailed` take precedence over
  `ScoreAsync` and `Score`, and are what hand a `ScoreBreakdown` to the critic.
- **Judge prose, do not measure it.** A rubric over the text itself (its length, its word count)
  reads a Chinese or Japanese candidate as a third of its English twin. Have a quick model read
  the candidate into the rubric's axes, each in `[0, 1]`, and score that reading — the
  `ScoreDetailedAsync` shape exists for exactly this call.
- **Configuring the critic enables it.** Setting `BuildCriticFeedback` turns the critic on; an
  explicitly configured critic that silently never ran was the trap. Set `EnableCritic = false`
  afterwards for the rare case of pre-configuring one to toggle later.
- `CriticMustImprove` keeps the critic's rewrite only when it scores better than the winner it was
  given.
- `CandidateScope.Seed` is not a sampler seed and does not make a run reproducible — the chat
  models expose none. It only nudges sibling candidates to diverge.
- `ScoreBreakdown.Weakest` names the lowest weighted metric, and `FormatBreakdown()` renders the
  whole thing — useful in a debug panel when a ranking surprises you.
- The breakdown passed to `BuildCriticFeedback` is non-null exactly when `ScoreDetailedAsync` or
  `ScoreDetailed` produced it, and null when ranking with `ScoreAsync` or `Score`.

## Snippet

```csharp
private sealed record Tagline(string Text, IReadOnlyList<string> Keywords);

// What a reader makes of a tagline, each axis in [0, 1]. A quick judge model fills it, so the
// rubric never measures the text itself -- a character count would call a Chinese tagline
// three times shorter than its English twin.
private sealed class TaglineReading
{
    [Description("How quickly it lands: 1 for a line said in one breath, 0 for one that needs a second read.")]
    public double Brevity { get; set; }

    [Description("How much of the brief it carries: 1 when every key idea is felt, 0 when none is.")]
    public double BriefFit { get; set; }
}

private readonly Reactive<string?> _winner = new(null);
private readonly Reactive<string?> _breakdown = new(null);

// A rubric of named, weighted metrics beats one opaque number: the critic can be told which
// metric was weakest, and the breakdown is readable when a choice looks wrong.
private static readonly ScoreBreakdownBuilder<TaglineReading> Rubric = new ScoreBreakdownBuilder<TaglineReading>()
    // EVERY metric must return [0, 1]. A rubric left on a 0..10 scale clamps to 1.0 for every
    // candidate and the ranking silently stops discriminating -- the judge is asked for the
    // range and clamped here.
    .Metric("brevity", 0.4, reading => Math.Clamp(reading.Brevity, 0, 1))
    .Metric("brief fit", 0.6, reading => Math.Clamp(reading.BriefFit, 0, 1));

private static async Task<ScoreBreakdown> JudgeAsync(Tagline candidate, string brief)
{
    var reading = await Emerge.Run<TaglineReading>(LLMModel.Claude45Haiku, pass =>
    {
        pass.SystemPrompt = "You judge taglines against a brief. Read the tagline in whatever language it is written and rate each axis from 0 to 1.";
        pass.Command = $"Brief: {brief}\nTagline: {candidate.Text}\nKeywords it claims to carry: {string.Join(", ", candidate.Keywords)}";
        pass.Temperature = 0;
    });

    return Rubric.Score(reading);
}

private async Task GenerateAsync(string brief)
{
    var judged = new Dictionary<Tagline, ScoreBreakdown>();

    var best = await Emerge.BestOf<Tagline>(LLMModel.Claude46Sonnet, new KernelContext(), options =>
    {
        options.Command = brief;
        options.Count = 4;

        // ScoreDetailedAsync is the judge's rubric: it ranks the candidates and hands the
        // breakdown to the critic. Set no scorer and every candidate scores 0.0 -- the FIRST
        // one then always wins, after paying for all Count runs.
        options.ScoreDetailedAsync = async (candidate, _) => judged[candidate] = await JudgeAsync(candidate, brief);

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
    _breakdown.Value = judged[best].FormatBreakdown();
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
