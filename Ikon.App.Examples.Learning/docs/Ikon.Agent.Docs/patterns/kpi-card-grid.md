<!-- mined-from: Sentinel -->
# KPI Card Grid — Headline metrics with subtitles

A responsive grid (2 cols on mobile, 4 on desktop) of small cards that each show a label, big number, and an optional sub-line. Each card is a one-line method call (`RenderKpiCard(grid, label, value, sub: …, accent: …)`) so the dashboard composes from data rather than markup. The accent color flips based on whether the metric is in a good or bad state.

## When to use

The top of an analytics / insights / overview page where you want 3-6 headline numbers to anchor the eye before the user scrolls into charts. Cards are inert (no click) — they're a status display, not navigation. For clickable summary tiles that drive a tab or filter, use a different shape.

## Snippet

```csharp
private void RenderKpiCard(UIView view, string label, string value, string sub = "", string accent = "text-zinc-100")
{
    view.Column(["rounded-lg ring-1 ring-zinc-800 bg-zinc-925 p-4 gap-1"], content: c =>
    {
        c.Text(["text-xs text-zinc-500 font-medium"], label);
        c.Text([$"text-2xl font-semibold {accent}"], value);
        if (!string.IsNullOrEmpty(sub))
        {
            c.Text(["text-xs text-zinc-500 truncate"], sub);
        }
    });
}

private void RenderInsightsSection(UIView view)
{
    var todayLocal = DateTime.Now.Date;
    var allEvents = _events.Value;
    var todays = allEvents.Where(e => e.LastSeen.ToLocalTime().Date == todayLocal).ToList();
    var openCount = allEvents.Count(e => e.Status == EventStatus.Open);
    var todayCost = EstimateTodayCostUsd();

    view.Column(["w-full px-6 py-6 gap-5 max-w-5xl mx-auto"], content: col =>
    {
        col.Column(["gap-1"], content: head =>
        {
            head.Text(["text-2xl font-semibold text-zinc-100"], "Insights");
            head.Text(["text-sm text-zinc-500"], "Operational health, alert trends, and triage performance.");
        });

        col.Box(["grid grid-cols-2 md:grid-cols-4 gap-3"], content: kpi =>
        {
            RenderKpiCard(kpi, "Today's events", todays.Count.ToString(),
                sub: $"{todays.Count(e => e.Severity == Severity.Alert)} alerts · {todays.Count(e => e.Severity == Severity.Watch)} watches");
            RenderKpiCard(kpi, "Open right now", openCount.ToString(),
                sub: openCount == 0 ? "All clear" : "Needs triage",
                accent: openCount > 0 ? "text-rose-300" : "text-emerald-300");
            RenderKpiCard(kpi, "AI cost today", $"${todayCost:0.00}",
                sub: _dailyCostCapUsd.Value > 0 ? $"of ${_dailyCostCapUsd.Value:0.00} cap" : "—");
        });
    });
}
```

## Notes

- The accent color is the *only* signal of "is this number good or bad" — keep the label and number neutral and let the accent carry the verdict. This works because the cards are visually small and you'd otherwise need a colored chip to convey state.
- The sub-line is essential — a number alone is a quiz, with `sub: "of $5.00 cap"` it's a story. Compose sub-lines from the same data source as the number, not from a separate query.
- `grid grid-cols-2 md:grid-cols-4` is the canonical responsive setup — phones see 2 wide, desktops see 4. For 3 cards, prefer `md:grid-cols-3`; for 5+, use `md:grid-cols-4` and let one wrap.
- `max-w-5xl mx-auto` on the section wrapper keeps cards readable on ultra-wide monitors.

## See also

- `bar-chart-from-list` — the chart that usually sits below a KPI grid
- `score-bar-meter` — for bounded 0-10 scores where threshold matters more than magnitude
