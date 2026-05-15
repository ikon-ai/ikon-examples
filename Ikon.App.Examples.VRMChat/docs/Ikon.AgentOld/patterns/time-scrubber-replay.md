<!-- mined-from: Veldra.OS -->
# Time Scrubber — Live / Replay With Window Chips

A bottom-anchored bar with a LIVE/REPLAY pill, a horizontal slider that drags the view back through time, a clock readout, and chips to choose how far back the slider reaches (5m / 30m / 1h / 6h). Dragging the slider enters playback at a specific timestamp; releasing on the right edge snaps back to live. The slider's range is computed each render from `nowNs - windowNs`.

## When to use

Any live-data dashboard where the operator might want to scrub recent history without leaving the view — incident review, sensor traces, log timelines, event streams. Avoids a separate "history" page.

## Snippet

```csharp
private readonly Reactive<long> _scrubWindowNs = new(10L * 60 * 1_000_000_000L);
private static readonly (string Label, long WindowNs)[] s_windows =
{
    ("5m", 5L * 60 * 1_000_000_000L),
    ("30m", 30L * 60 * 1_000_000_000L),
    ("1h", 60L * 60 * 1_000_000_000L),
    ("6h", 6L * 60 * 60 * 1_000_000_000L),
};

private void RenderTimeScrubber(UIView view)
{
    bool active = _playbackActive.Value;
    long nowNs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L;
    long windowNs = _scrubWindowNs.Value;
    long windowStart = nowNs - windowNs;
    long cursor = active ? _playbackTimeNs.Value : nowNs;
    cursor = Math.Clamp(cursor, windowStart, nowNs);
    double pct = (cursor - windowStart) / (double)windowNs;

    view.Row(["absolute bottom-3 left-3 right-3 items-center gap-3 px-3 py-2"], content: view =>
    {
        view.Box(["px-2 py-0.5 cursor-pointer border", active ? "border-amber-500" : "border-emerald-500"],
            onClick: async () => { if (active) ExitPlayback(); await Task.CompletedTask; },
            content: view => view.Text(["text-[10px] font-semibold"], active ? "REPLAY" : "LIVE"));

        view.Slider(["w-full flex-1"], min: 0, max: 1, step: 0.001, value: [pct],
            onValueChange: async vs =>
            {
                double p = Math.Clamp(vs[0], 0, 1);
                long t = windowStart + (long)(p * windowNs);
                if (p > 0.995) ExitPlayback(); else EnterPlayback(t);
                await Task.CompletedTask;
            });

        view.Row(["gap-1"], content: view =>
        {
            foreach (var (label, w) in s_windows)
            {
                bool sel = windowNs == w;
                view.Box([sel ? "px-1.5 py-0.5 bg-amber-500/20" : "px-1.5 py-0.5 hover:bg-slate-700"],
                    onClick: async () => { _scrubWindowNs.Value = w; await Task.CompletedTask; },
                    content: view => view.Text(["text-[9px] font-semibold"], label));
            }
        });

        view.Text(["text-[10px] font-mono w-[80px] text-right"],
            DateTimeOffset.FromUnixTimeMilliseconds(cursor / 1_000_000L).UtcDateTime.ToString("HH:mm:ss") + "Z");
    });
}
```

## Notes

- Snap-to-live on the rightmost ~1% so the operator can drag back to live without hunting the edge.
- Slider value is recomputed each render from the cursor — never store `pct` in state, derive it.
- `Reactive<long>` for `_playbackTimeNs` lets a separate timer advance the cursor while paused so playback animates.
- Window chips and the LIVE/REPLAY pill share the same accent palette so the active mode reads at a glance.

## See also

- `multi-step-wizard`
- `streaming-agent-status`
