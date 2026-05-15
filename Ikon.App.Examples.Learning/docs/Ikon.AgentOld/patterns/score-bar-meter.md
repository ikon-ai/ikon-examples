<!-- mined-from: Threads -->
# Score Bar Meter — Threshold-colored progress bar

A horizontal labeled progress bar that fills from 0 to 100% (driven by a 0-10 score scaled ×10) with a color that flips based on a threshold: red below threshold-2, amber within 2 of threshold, emerald at-or-above. The score number is shown to the right with the same threshold-aware coloring. Three or four of these stacked makes an at-a-glance quality scorecard.

## When to use

Showing a small set of related scores that each have a "passing" threshold — Craft / Beauty / Magic, Confidence / Coverage / Clarity, Strength / Endurance / Skill. Pair with a popover that surfaces them on a hover-revealed pill so the bars only render when the user wants to see them. Use this rather than a chart when the values are bounded, the count is fixed, and "passing or not" is the dominant question.

## Snippet

```csharp
private static void RenderScoreBar(UIView view, string label, int score, int threshold)
{
    var color = score == 0 ? "bg-muted-foreground/30"
        : score >= threshold ? "bg-emerald-500"
        : score >= threshold - 2 ? "bg-amber-500"
        : "bg-red-500";
    var widthPercent = score == 0 ? 0 : score * 10;

    view.Row(["items-center gap-2"], content: row =>
    {
        row.Text(["text-[10px] text-muted-foreground w-10 text-right"], text: label);
        row.Box(["flex-1 h-1.5 bg-muted rounded-full overflow-hidden"], content: bar =>
        {
            bar.Box([$"h-full {color} rounded-full", $"w-[{widthPercent}%]"]);
        });
        row.Text(["text-[10px] font-medium w-5 text-right",
            score == 0 ? "text-muted-foreground" : score >= threshold ? "text-emerald-400" : "text-foreground"],
            text: score == 0 ? "—" : score.ToString());
    });
}

// Consumed in a popover that opens on the phase pill:
view.Popover(trigger: triggerView =>
{
    triggerView.Button(["bg-primary/15 text-primary text-[10px] px-1.5 py-0.5 rounded"],
        onClick: async () => CycleAppPhase(),
        content: v => v.Text(text: phaseLabel));
}, content: popView =>
{
    var scores = capturedThread.GetAppScores();
    if (scores?.HasScores == true)
    {
        popView.Column(["p-3 gap-2 min-w-[180px]"], content: col =>
        {
            col.Text(["text-[10px] text-muted-foreground font-medium uppercase tracking-wider"], text: "Quality Scores");
            RenderScoreBar(col, "Craft", scores.Craft, scores.Threshold);
            RenderScoreBar(col, "Beauty", scores.Beauty, scores.Threshold);
            RenderScoreBar(col, "Magic", scores.Magic, scores.Threshold);
            col.Row(["justify-between text-[10px] text-muted-foreground mt-1"], content: row =>
            {
                row.Text(text: $"Target: {scores.Threshold}+");
                row.Text(text: $"Critiques: {scores.CritiqueCount}");
            });
        });
    }
});
```

## Notes

- The width is computed as a `w-[{percent}%]` arbitrary value — works because the surrounding box has a fixed flex height (`h-1.5`) and the inner box gets its width from this string.
- The threshold-2 amber band is the "almost passing" warning state — a useful affordance that tells the user "not yet, but close." Choose your band size based on the noise floor of the score.
- A score of `0` is rendered as an em-dash `—` and a fully empty bar — distinguishes "no data yet" from "actually scored zero." Pair with `HasScores` on the score record so the parent decides whether to render the bars at all.
- For >5 metrics, use a vertical sparkline list instead — at that count, three bars with labels become a wall of text.

## See also

- `bar-chart-from-list` — for time-series counts where many bars are needed
- `kpi-card-grid` — for a small number of headline numbers without thresholds
