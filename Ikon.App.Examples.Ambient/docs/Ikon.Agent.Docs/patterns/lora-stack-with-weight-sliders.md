<!-- mined-from: Sensei -->
# LoRA Stack — Toggleable Items With Per-Item Weight Slider

A library of stylistic ingredients grouped by `Kind`. Each row has a Switch on the right; when enabled, a Slider (0..1, step 0.05) and a tabular-nums weight readout appear underneath. Disabled rows render dimmed and collapse to a single line. The active count is shown in a small caption above the list.

## When to use

Stylistic blending UIs (LoRA stacks, prompt-mixin libraries, audio mix layers, lighting presets), where the user wants to enable several ingredients and dial in each one's contribution. The "header item with optional sub-control" structure also fits volume-mixers and EQ bands.

## Snippet

```csharp
private void RenderLibrary(UIView view)
{
    view.Row(["items-baseline justify-between"], content: view =>
    {
        view.Text([Text.Caption, "uppercase tracking-[0.18em]"], "library");
        view.Text([Text.Caption, "italic opacity-70"], $"{_library.Count(l => l.Enabled)} active");
    });

    view.ScrollArea(rootStyle: ["flex-1 min-h-0 -mx-2"], viewportStyle: ["px-2"], content: view =>
    {
        view.Column(["gap-4 pb-4"], content: view =>
        {
            foreach (var kindGroup in _library.Value.GroupBy(l => l.Kind))
            {
                view.Column(["gap-2"], content: v =>
                {
                    v.Text([Text.Caption, "uppercase tracking-wider opacity-70"], kindGroup.Key.ToString());
                    foreach (var lora in kindGroup) RenderLoraRow(v, lora);
                });
            }
        });
    });
}

private void RenderLoraRow(UIView view, Lora lora)
{
    view.Column([
        "p-3 gap-2 rounded-sm",
        lora.Enabled
            ? "bg-[#ece5d2]/80 ring-1 ring-[#1a1a18]/20"
            : "bg-transparent opacity-65 hover:opacity-90"
    ], content: view =>
    {
        view.Row(["items-start justify-between gap-3"], content: v =>
        {
            v.Column(["gap-0.5 flex-1 min-w-0"], content: c =>
            {
                c.Text(["text-sm tracking-wide"], lora.Name);
                c.Text([Text.Caption, "italic opacity-70"], lora.Notes);
            });

            v.Switch([Switch.Default],
                value: lora.Enabled,
                onValueChange: async v => UpdateLora(lora.Id, l => l.With(enabled: v)),
                content: s => s.SwitchThumb([Switch.Thumb]));
        });

        if (lora.Enabled)
        {
            view.Row(["items-center gap-3"], content: v =>
            {
                v.Slider([Slider.Default, "flex-1"],
                    value: [lora.Weight], min: 0, max: 1, step: 0.05,
                    onValueChange: async values =>
                    {
                        if (values.Count > 0) UpdateLora(lora.Id, l => l.With(weight: values[0]));
                    },
                    content: s =>
                    {
                        s.SliderTrack([Slider.Track], content: t => t.SliderRange([Slider.Range]));
                        s.SliderThumb([Slider.Thumb]);
                    });

                v.Text([Text.Caption, "tabular-nums w-9 text-right"],
                    lora.Weight.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            });
        }
    });
}

private void UpdateLora(string id, Func<Lora, Lora> mutate)
{
    var next = _library.Value.Select(l => l.Id == id ? mutate(l) : l).ToList();
    _library.Value = next;
}
```

## Notes

- Use `record` types with a `With(...)` instance method (or `with`-expressions) so updates are immutable, supporting clean `Select(...)` based mutators.
- `tabular-nums w-9 text-right` keeps the weight readout from jittering as digits change.
- Group by `Kind` with `GroupBy` and a small caption header per group; users browse by category.
- Disabled rows fall to `opacity-65` and the slider row simply isn't rendered — collapses neatly so a long list stays scannable.
- The whole library lives inside a `ScrollArea` with `-mx-2 / px-2` so the scrollbar sits flush with the panel edge while content has padding.

## See also

- `state-machine-cards-and-transitions` — same row pattern with status icons instead of sliders
- `inline-list-cell-edit` — cell-style editing with text instead of toggles+sliders
