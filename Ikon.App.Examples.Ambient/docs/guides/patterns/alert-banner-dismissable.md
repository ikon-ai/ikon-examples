<!-- mined-from: Sentinel -->
# Alert Banner — Pulsing strip with action buttons

A horizontal strip across the top of the page that appears when there's something the user *needs* to act on (an unacknowledged alert, a quota cap exceeded, an offline integration). Unlike toast notifications it persists until acted upon, pulses to draw the eye, includes an age counter, and offers Acknowledge / Dismiss buttons inline. Auto-hides as soon as the source condition clears.

## When to use

For a single high-priority transient state that requires user action — open security alert, payment failure, license expiring. Show *one* of these at a time, anchored just below the top strip and above the page content. Pair with `toast-notifications` for confirmation feedback after the user acknowledges.

## Snippet

```csharp
private void RenderAlertBanner(UIView view)
{
    var openAlert = _events.Value.FirstOrDefault(e => e.Severity == Severity.Alert && e.Status == EventStatus.Open);
    if (openAlert == null) return;

    view.Row([
        "bg-rose-500/10 border-y border-rose-500/30 px-5 py-2.5 items-center gap-4 flex-shrink-0",
        "motion-[0:bg-rose-500/10,50:bg-rose-500/20,100:bg-rose-500/10] motion-duration-1500ms motion-loop"
    ], content: view =>
    {
        view.Icon(["w-5 h-5 text-rose-400 flex-shrink-0"], name: "alert-triangle");

        view.Column(["flex-1 min-w-0 gap-0.5"], content: view =>
        {
            var ageSeconds = (int)(DateTime.UtcNow - openAlert.FirstSeen).TotalSeconds;
            var ageStr = ageSeconds switch
            {
                < 60 => $"{ageSeconds}s",
                < 3600 => $"{ageSeconds / 60}:{(ageSeconds % 60):D2}",
                _ => $"{ageSeconds / 3600}h{(ageSeconds % 3600) / 60:D2}m"
            };
            var ageColor = ageSeconds < 30 ? "text-rose-300" : ageSeconds < 120 ? "text-rose-200" : "text-white";

            view.Row(["items-center gap-2"], content: view =>
            {
                view.Text(["text-sm font-semibold text-rose-100"], "Unacknowledged alert");
                var openCount = _events.Count(e => e.Severity == Severity.Alert && e.Status == EventStatus.Open);
                if (openCount > 1)
                {
                    view.Text(["text-xs text-rose-300/80"], $"× {openCount}");
                }
                view.Box(["px-1.5 py-0.5 rounded bg-rose-500/15 ring-1 ring-rose-500/30"], content: v =>
                    v.Text([$"text-xs font-mono {ageColor}"], $"open {ageStr}"));
            });
            view.Text(["text-sm text-rose-100 truncate"], openAlert.Narration);
        });

        view.Row(["items-center gap-2 flex-shrink-0"], content: view =>
        {
            view.Button(
                ["px-2.5 py-1 rounded-md bg-emerald-500/15 ring-1 ring-emerald-500/40 text-emerald-200 hover:bg-emerald-500/25 text-xs font-medium"],
                "Acknowledge",
                onClick: async () => UpdateEventStatus(openAlert.Id, EventStatus.Acknowledged));
            view.Button(
                ["px-2.5 py-1 rounded-md ring-1 ring-zinc-700 text-zinc-300 hover:ring-zinc-500 text-xs font-medium"],
                "Dismiss",
                onClick: async () => UpdateEventStatus(openAlert.Id, EventStatus.Dismissed));
        });
    });
}
```

## Notes

- The pulsing motion class loops between `bg-rose-500/10` and `bg-rose-500/20` over 1.5s — visually present without seizure-inducing strobe. Tune `motion-duration` rather than the keyframe brightness.
- The age counter ramps the text color through three intensities (rose-300 → rose-200 → white) the longer the alert sits unacknowledged. A 2-minute-old alert *should* read more urgent than a 5-second-old one.
- The banner shows the *first* open alert and a `× N` chip when there are more — clicking through to a triage queue is the natural next step. Avoid showing N banners at once; users start ignoring them.
- Render order matters: place this after the top strip and before the main content, both as siblings of the same flex column. `flex-shrink-0` keeps it from getting eaten when content overflows.
- The banner disappears the instant the source condition (`openAlert == null`) clears — no manual hide call needed.

## See also

- `toast-notifications` — for confirmation feedback after acknowledging
- `command-palette-jump` — to give power users a keyboard-driven way to triage
