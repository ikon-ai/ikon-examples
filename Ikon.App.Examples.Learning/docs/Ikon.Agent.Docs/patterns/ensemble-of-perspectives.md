<!-- mined-from: Ikon.App.Patterns -->
# Ensemble Of Perspectives — Merging Reviewers Instead Of Picking One

`BestOf` runs candidates and keeps the winner. `EnsembleMerge` runs them and **merges** the
results. Reach for this one when the answer should contain every perspective rather than the best
single one — a review, a risk list, a checklist, a set of options.

What makes it work is that the solvers actually differ. `AgentScope.Role` is prepended to that
solver's system prompt, so naming them ("the security reviewer", "the accessibility reviewer") is
the difference between three genuine perspectives and three attempts at the same one.

## When to use

Review, critique, brainstorming, risk analysis — anywhere completeness beats picking a winner. When
one answer is wanted and quality varies run to run, `best-of-with-rubric` is the right shape.

## Notes

- **Left unset, roles default to `Solver0`, `Solver1`, …** and members diverge only by `Seed`,
  which is a far weaker signal. Naming the roles is most of the value.
- **`MaxParallel` must be at least 1** — there is no "unbounded" sentinel, so `0` is a
  configuration error rather than a request for no limit.
- **The merger is its own scope with its own instructions.** Without steering it will largely
  concatenate; telling it how to combine (keep every distinct risk, drop duplicates, order by
  severity) is what turns N answers into one.
- `AgentScope.Index` is the member's position — useful for assigning roles from a list, as here.
- `Seed` is not a sampler seed and does not make a run reproducible; it only nudges siblings apart.
- Every solver runs the same `Command`; per-member steering goes through `SolverConfig`, not
  through the shared prompt.

## Snippet

```csharp
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
```

## See also

- `best-of-with-rubric` — when one answer should win rather than all of them merging.
- `mapreduce-long-document-summary` — fan out over *inputs* rather than over perspectives.
