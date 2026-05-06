<!-- mined-from: PolymarketMirror -->
# Slider With Live Label — Numeric Filter In A FormField Column

A `Slider` whose current value is interpolated into the label *above* the track, so the user sees `Max Wallets: 350` update as they drag — no separate readout cell, no tooltip, no popover. The label is the readout. Lives inside a `FormField.Root` column with a `min-w` so it sits cleanly in a wrapping filter row alongside `Select` and `TextField` filters.

## When to use

A bounded numeric parameter (max items, threshold, page size, time-window length) inside a multi-control filter bar. Better than a `TextField` of `int` because users immediately understand the range without typing; better than a `Select` of canned values when the user wants smooth scrubbing through a continuum.

## Snippet

```csharp
private readonly Reactive<int> _maxWallets = new(200);

view.Row(["flex flex-row flex-wrap gap-4 mt-4"], content: view =>
{
    view.Column([FormField.Root, "min-w-[160px]"], content: view =>
    {
        view.Text([FormField.Label], $"Max Wallets: {_maxWallets.Value}");
        view.Slider(
            [Slider.Default],
            min: 50,
            max: 500,
            step: 50,
            value: [_maxWallets.Value],
            onValueChange: v =>
            {
                if (v.Count > 0)
                {
                    _maxWallets.Value = (int)v[0];
                }
                return Task.CompletedTask;
            },
            content: view =>
            {
                view.SliderTrack([Slider.Track], content: view =>
                {
                    view.SliderRange([Slider.Range]);
                });
                view.SliderThumb([Slider.Thumb]);
            });
    });

    view.Column([FormField.Root, "min-w-[140px]"], content: view =>
    {
        view.Text([FormField.Label], "Time Period");
        view.Select(
            triggerStyle: ["w-full"],
            options:
            [
                new SelectOption("ALL", "All Time"),
                new SelectOption("MONTH", "Month"),
                new SelectOption("WEEK", "Week"),
                new SelectOption("DAY", "Day")
            ],
            value: _timePeriod.Value,
            onValueChange: async value => _timePeriod.Value = value);
    });
});
```

## Notes

- `Slider` returns a `List<double>` even for single-thumb sliders — guard with `v.Count > 0` and cast to your target type once.
- Keep the live readout on the *label*, not below the track: it's already in tab order and screen-reader output, and saves a row of vertical space.
- Pick a `step` that makes the increments meaningful (50 wallets, 0.05 weight, 5 minutes). Continuous sliders without a step send a flood of updates and produce nonsensical labels like `Max Wallets: 347`.
- Wrap each control in `[FormField.Root, "min-w-[160px]"]` and the row in `flex-wrap gap-4` — the filter bar reflows naturally on narrow viewports.

## See also

- `lora-stack-with-weight-sliders` — slider per row with a toggle gate, for a list of weighted ingredients
- `filter-button-group` — discrete-pick alternative for short option sets
