<!-- mined-from: recurring visual-gate flags (emoji-as-icon in instruction text and celebration banners) -->
# Inline Icon in Text — the emoji-free way to decorate a sentence

The most stubborn visual defect in generated apps: emoji used as functional icons inside text
("Tap 💧 to log", "🎉 Goal complete"). Coders reach for emoji because `view.Text` takes a plain
string and an icon can't live inside it — the emoji is the path of least resistance. The correct
shape is a baseline-aligned Row composing text fragments and `view.Icon`, which takes ~one more
line and reads as designed instead of AI-demo.

## When to use

Any sentence, hint, badge, or banner where a small pictograph belongs: instruction hints, empty
states, celebration lines, list-row prefixes.

## Snippet

```csharp
/// Instruction hint with an inline icon — instead of "Tap 💧 to log · ↩ to undo".
view.Row(["items-center gap-1.5 text-sm text-muted-foreground"], content: r =>
{
    r.Text(["text-sm"], text: "Tap");
    r.Icon([Icon.Xs, "text-brand"], name: "droplet");
    r.Text(["text-sm"], text: "to log · press");
    r.Icon([Icon.Xs], name: "undo-2");
    r.Text(["text-sm"], text: "to undo");
});

/// Celebration/status line with a leading icon — instead of "🎉 Goal complete".
view.Row(["items-center gap-2"], content: r =>
{
    r.Icon([Icon.Sm, "text-brand"], name: "party-popper");
    r.Text(["text-sm font-semibold text-foreground"], text: "Goal complete");
});

/// List-row prefix (status/type marker) — instead of a "✅ " string prefix.
view.Row(["items-center gap-2"], content: r =>
{
    r.Icon([Icon.Xs, "text-emerald-500"], name: "check-circle-2");
    r.Text(["text-sm text-foreground"], text: item.Title);
});
```

## Notes

- `items-center` on the Row keeps icons optically aligned with the text baseline at these sizes;
  `gap-1.5`/`gap-2` gives the natural word-space rhythm.
- Icon names are lucide (`droplet`, `undo-2`, `party-popper`, `check-circle-2`, `flame`,
  `trophy`, `sparkles` — pick the literal object, not a metaphor).
- Colour the icon with a theme token (`text-brand`, `text-muted-foreground`) or the app's
  committed accent — an icon inherits no colour by default.
- Emoji remain fine as CONTENT the user typed, or when the brief's theme genuinely calls for
  them — never as UI iconography.

## See also

- `signature-moment` — the celebration surface these lines often live in.
- `status-pill` — chips that pair an icon with a short label.
