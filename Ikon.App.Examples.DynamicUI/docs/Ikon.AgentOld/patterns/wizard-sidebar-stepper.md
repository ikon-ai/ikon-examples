<!-- mined-from: Sentrix -->
# Wizard Sidebar Stepper — Numbered Circles With Done/Active/Locked States

A vertical list of numbered step circles connected by dashed lines. Done steps show a checkmark, the active step shows its number on a brand background, locked future steps render in muted grey and ignore clicks. Clicking a previously-completed step jumps back to it.

## When to use

The progress affordance for any multi-step form. Pair with `multi-step-wizard` (or any state-driven flow with linear steps). Use when the user benefits from seeing how far they've come and being able to revisit earlier steps without losing entered data.

## Snippet

```csharp
private void RenderWizardSidebar(UIView view)
{
    view.Column(["w-48 shrink-0 py-5 px-6 flex flex-col"], content: view =>
    {
        for (int i = 0; i < WizardStepLabels.Length; i++)
        {
            var step = i;
            var isActive = _wizardStep.Value == step;
            var isDone = _wizardHighestStep.Value > step && !isActive;
            var isClickable = _wizardHighestStep.Value >= step && !isActive;
            var isLast = i == WizardStepLabels.Length - 1;

            var rowStyle = isActive
                ? "flex items-center gap-3 px-3 py-2 bg-secondary rounded-[6px]"
                : isClickable
                    ? "flex items-center gap-3 px-3 py-2 cursor-pointer hover:bg-secondary/50 rounded-[6px]"
                    : "flex items-center gap-3 px-3 py-2";

            view.Button([rowStyle, "w-full text-left"],
                disabled: !isClickable,
                onClick: async () => _wizardStep.Value = step,
                content: view =>
                {
                    if (isDone)
                    {
                        view.Box(["w-5 h-5 rounded-full bg-success-primary flex items-center justify-center shrink-0"], content: view =>
                            view.Text(["text-[10px] text-success-primary font-bold leading-none"], "✓"));
                    }
                    else if (isActive)
                    {
                        view.Box(["w-5 h-5 rounded-full bg-brand-button flex items-center justify-center shrink-0"], content: view =>
                            view.Text(["text-[10px] text-white font-semibold leading-none"], $"{step + 1}"));
                    }
                    else
                    {
                        view.Box(["w-5 h-5 rounded-full border border-secondary flex items-center justify-center shrink-0"], content: view =>
                            view.Text(["text-[10px] text-muted-foreground leading-none"], $"{step + 1}"));
                    }

                    var labelStyle = isActive ? "text-xs font-semibold text-foreground"
                        : isDone ? "text-xs text-foreground"
                        : "text-xs text-muted-foreground";
                    view.Text([labelStyle], T(WizardStepLabels[step]));
                });

            if (!isLast)
            {
                view.Box(["h-3 pl-[22px]"], content: view =>
                    view.Box(["w-0 h-full border-l border-dashed border-secondary"]));
            }
        }
    });
}
```

## Notes

- Three states: `isActive` (current), `isDone` (highest > step), neither (locked). `isClickable = !isActive && (highest >= step)` — the active step is intentionally not clickable.
- The connector line is a separate `Box` between rows with `pl-[22px]` to align under the circle's centre (`px-3` = 12 + half-circle = 10 → 22).
- `disabled: !isClickable` is what blocks future steps; the styled `cursor-pointer` only changes hover affordance.
- Captured `var step = i;` inside the loop is required — closing over `i` directly would all jump to the last step.

## See also

- `multi-step-wizard` — the state machine and footer this stepper attaches to.
