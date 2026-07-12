<!-- mined-from: HabitPulse (live generated-app audit: cards 3x taller than content) -->
# Hover-reveal row actions (collapse the space, don't just hide the pixels)

Secondary actions (delete, archive, edit) revealed on card hover. The trap: hiding them with
`opacity-0 group-hover:opacity-100` makes them invisible but **still reserves their full
layout height** — every card carries a permanent empty band where the hidden row sits, and
the list reads as unfinished dead space (the defect this was mined from: habit cards 3x
taller than their content).

## When to use

List/card rows with destructive or secondary actions that would clutter the resting state.
For PRIMARY actions (complete, open), keep them always visible — hover-reveal is for the
long tail, and touch devices never hover (see Notes).

## Snippet

```csharp
view.Box(["group rounded-xl bg-card p-4"], key: item.Id, content: card =>
{
    card.Row(["items-center gap-3"], content: row =>
    {
        row.Text(["flex-1 font-medium"], text: item.Title);

        // Inline, right-aligned, and LAYOUT-COLLAPSED until hover: `hidden` removes the
        // buttons from layout entirely; `group-hover:flex` brings them back. The row's
        // height never changes because the visible content defines it.
        row.Row(["hidden group-hover:flex items-center gap-1"], content: actions =>
        {
            actions.Button([Button.GhostSm], text: "Archive", onClick: async () => { await ArchiveAsync(item); });
            actions.Button([Button.GhostSm, "text-error"], text: "Delete", onClick: async () => { await DeleteAsync(item); });
        });
    });
});
```

## Notes

- **`hidden group-hover:flex`, never `opacity-0`.** Opacity keeps the element in layout —
  reserved height, dead space. `hidden` collapses it. (If you want a fade, combine:
  `hidden group-hover:flex motion-[fade-in_150ms]` — the motion runs on reveal.)
- **Keep actions INLINE in the content row** (like this snippet), not in a second stacked
  row under the content — a second row changes card height on hover, which makes the whole
  list jump. Inline reveal is height-stable by construction.
- **Touch devices never hover.** Anything reachable ONLY via hover is unreachable on
  mobile — pair hover-reveal with another path (row click opens detail with the same
  actions, or a kebab menu). Never hover-gate the only way to do something.
