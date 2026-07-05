<!-- mined-from: recurring visual-gate residuals ("missing signature moments", "typographic flatness", "weakest: completion celebration") -->
# Signature Moment — one hero, one celebration, real display type

Apps that clear every correctness bar still grade as forgettable when everything is the same
`text-sm` and the core success action just mutates a list row. The visual gate's recurring residual
verdicts name it directly: "missing signature moments", "typographic flatness". The fix is cheap
and structural: every app commits ONE hero treatment and ONE celebration state, both carried by
real display typography.

## When to use

Every UI app. Decide at plan time (DESIGN → CONCEPT/EVOKES) which moment is the app's signature:
the header identity, the "goal reached" state, the big number. One or two per app — a page where
everything celebrates has no signature.

## Snippet

```csharp
/// 1. HERO HEADER — the app's identity moment. A real display jump (5xl vs base is a statement;
///    2xl vs base is timid), tight tracking, and ONE accent word in the brand colour.
view.Column(["gap-1 pt-10 pb-8"], content: v =>
{
    v.Row(["items-baseline gap-2"], content: r =>
    {
        r.Text(["text-5xl font-bold tracking-tight text-foreground"], text: "Morning");
        r.Text(["text-5xl font-bold tracking-tight text-brand"], text: "Flow");
    });
    v.Text(["text-sm text-muted-foreground"], text: "Your routine, one sunrise at a time");
});

/// 2. BIG NUMBER — the one metric that matters, rendered like it matters. Display size + tabular
///    numerals + a small muted label; never a same-size stat row.
view.Column(["items-center gap-1"], content: v =>
{
    v.Text(["text-6xl font-bold tracking-tight tabular-nums text-brand"], text: $"{streak}");
    v.Text(["text-xs font-medium uppercase tracking-widest text-muted-foreground"], text: "day streak");
});

/// 3. CELEBRATION STATE — the core success action deserves a DESIGNED state change, not a toast.
///    Swap the working surface for a celebratory one (brand gradient or brand-tinted fill, an icon
///    with presence, display-type headline) when the goal completes. Reactive makes this a plain
///    conditional — no animation framework needed for the moment to land.
if (_completedToday.Value == _steps.Value.Count && _steps.Value.Count > 0)
{
    view.Column(["items-center gap-3 rounded-2xl p-8 bg-gradient-to-br from-amber-400 to-orange-500 shadow-lg shadow-orange-500/25"], content: v =>
    {
        v.Icon(["size-10 text-white"], icon: "sunrise");
        v.Text(["text-3xl font-bold tracking-tight text-white"], text: "Morning complete");
        v.Text(["text-sm text-white/85"], text: $"{_streak.Value} days in a row — see you tomorrow");
    });
}
```

## Notes

- Display type is a SCALE JUMP: pair a 4xl-6xl headline with base body and xs labels — three
  distinct sizes minimum. Weight and colour step WITH size (bold+foreground → medium+muted).
- The celebration's palette comes from the committed brand tokens/palette (see the CTA rule and
  `depth-and-atmosphere`); the moment must feel like the SAME app at full volume, not a party
  sticker on top.
- `tabular-nums` on any number that changes — layout shift on digit change reads as jank.
- Emoji are not a celebration. An icon with size and a committed gradient surface is.

## See also

- `depth-and-atmosphere` — the gradient/shadow recipes the celebration surface draws from.
- `per-letter-glow-pulse` — animated text treatment when the DESIGN brief is vivid.
- `score-bar-meter` — progress that feeds the moment (bar fills → celebration replaces it).
