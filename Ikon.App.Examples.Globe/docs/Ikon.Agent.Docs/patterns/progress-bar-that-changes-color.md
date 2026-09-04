<!-- mined-from: Ikon.App.Patterns -->
# Progress Bar That Changes Color — Animating The Fill, Not Just Setting It

`view.Progress` renders the bar but animates nothing: change `value` and the fill jumps. The glide
comes from a transition class on the **indicator**, and the colour comes from a fill variant — both
passed through `Progress.ComposeIndicator`, which lays down the base recipe, then the variant, then
the caller's overrides last so they win.

## When to use

Any bar that advances through steps, a countdown, an upload, or a quiz — anywhere the fill should
slide rather than snap, or should change colour as it fills.

Derive the variant from the value rather than storing it. A colour held in its own field is one
more thing that can disagree with the bar it is describing.

## Notes

- `Progress.Variant` carries `Default`, `Success`, `Warning` and `Error`. The `tone:` parameter on
  `view.Progress` maps `SemanticTone.Success`/`Warning`/`Error` to the same tokens — use `tone:` for
  a static colour, `ComposeIndicator` when the colour is computed or you also need overrides.
- `Progress.IndicatorTransform(value)` is the arbitrary-value class that fills to a percentage; only
  reach for it when driving the fill yourself instead of through `value:`.
- For work with no known end, pass `indeterminate: true` rather than animating a fake value.

## Snippet

```csharp
private const int TotalSteps = 10;

private readonly Reactive<int> _step = new(3);

// The fill colour is a function of progress, not a stored field -- one source of truth, and no
// way for the colour to disagree with the bar.
private static string VariantFor(double percent) => percent switch
{
    >= 100 => Progress.Variant.Success,
    >= 60 => Progress.Variant.Default,
    >= 30 => Progress.Variant.Warning,
    _ => Progress.Variant.Error,
};

private void Render(IView view)
{
    var percent = 100.0 * _step.Value / TotalSteps;

    view.Column(["gap-3"], content: col =>
    {
        // ComposeIndicator builds the fill class list: base recipe, then the variant, then
        // caller overrides LAST so they win. The transition is what makes the width glide
        // instead of jumping -- Progress animates nothing on its own.
        col.Progress(
            value: percent,
            max: 100,
            indicatorStyle: [Progress.ComposeIndicator(
                variant: VariantFor(percent),
                indeterminate: false,
                "transition-all duration-500 ease-out")]);

        col.Text(["text-muted-foreground text-sm"], text: $"Step {_step.Value} of {TotalSteps}");

        col.Row(["gap-2"], content: row =>
        {
            row.Button(
                disabled: _step.Value == 0,
                onClick: () => _step.Value--,
                content: v => v.Text(text: "Back"));

            row.Button(
                disabled: _step.Value == TotalSteps,
                onClick: () => _step.Value++,
                content: v => v.Text(text: "Next"));
        });
    });
}
```

## See also

- `busy-flag-loading` — the Reactive-bool + try/catch shape for the async work a bar reports on.
