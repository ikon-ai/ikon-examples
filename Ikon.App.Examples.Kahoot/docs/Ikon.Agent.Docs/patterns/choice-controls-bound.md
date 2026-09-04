<!-- mined-from: Ikon.App.Patterns -->
# Choice Controls Bound To State — Which Control Takes `bind:` And Which Does Not

`bind:` is the two-way form: it reads the reactive's `.Value` and writes back on every change,
`value:` is ignored when it is set, and `onValueChange` still fires after the write-back. But the
controls do not all accept it, and the ones that look most alike differ the most.

| Control | Two-way form |
|---|---|
| `TextField`, `TextArea`, `Select`, `RadioGroup` | `bind:` a `Reactive<string>` |
| `Checkbox`, `Switch` | `bind:` a `Reactive<bool>` |
| `Slider` | `bind:` a `Reactive<double>` — on the style-first overload only |
| **`Toggle`** | **none** — `value:` + `onValueChange:` |
| `DatePicker`, `TimePicker`, `ColorPicker` | none — `value:` (a string) + `onValueChange:` |

## When to use

Any settings panel, filter bar or form built from choice controls rather than free text. Bind
user-scoped state (`UserReactive`) rather than client-scoped: a reload mints a new client session,
so a `ClientReactive` selection vanishes with it.

## Notes

- **`Slider` has two overloads and mixing their shapes is the classic CS1503.** On the canonical
  style-first overload `value:` is a **list** of thumb positions (it is multi-thumb capable), so
  `value: 0.5` there does not compile — and `bind:` lives on that overload. The scalar convenience
  overload is **value-first**: `view.Slider(0.5, style: [...], onValueChange: ...)` — the one
  exception to the style-array-first rule.
- **`Toggle` takes neither `bind:` nor `formValue:`** (CS1739). `Checkbox` and `Switch` take both.
  The three look interchangeable and are not.
- `DatePicker` and `TimePicker` speak **ISO strings**, not `DateTime`.
- `label:` renders a visible label above the control — and on `Checkbox`/`Switch`/`Toggle` renders
  as clickable trailing text that becomes the accessible name. Omit it when the design has none;
  passing it anyway paints stray text.
- A `value:` with **no** `onValueChange` and no `bind:` renders a genuinely read-only control
  (`aria-readonly`) rather than one that silently ignores edits — use that shape only when you mean
  a display field.
- `RadioGroup` children are `RadioGroupItem`s with their own `value:`; the group carries the
  selection.

## Snippet

```csharp
private readonly UserReactive<string> _plan = new("standard");
private readonly UserReactive<bool> _notify = new(true);
private readonly UserReactive<double> _volume = new(0.5);
private readonly UserReactive<string> _startDate = new("");
private readonly UserReactive<string> _startTime = new("09:00");

private void Render(IView view)
{
    view.Column(["gap-4"], content: col =>
    {
        // bind: is the two-way form -- it reads .Value and writes back on change. With bind:
        // set, value: is ignored; onValueChange still fires after the write-back.
        col.RadioGroup(label: "Plan", bind: _plan, content: group =>
        {
            group.RadioGroupItem(value: "standard", content: v => v.Text(text: "Standard"));
            group.RadioGroupItem(value: "pro", content: v => v.Text(text: "Pro"));
        });

        // Toggle takes NEITHER bind: nor formValue: -- it is value: + onValueChange only.
        // (Checkbox and Switch take both; the three are not interchangeable.)
        col.Toggle(
            label: "Email me",
            value: _notify.Value,
            onValueChange: async on => _notify.Value = on);

        // Slider has TWO overloads and mixing their shapes is the classic CS1503. On this,
        // the canonical style-first one, value: is a LIST of thumb positions -- so `value: 0.5`
        // does not compile. bind: lives here and takes a Reactive<double>.
        col.Slider(["w-full"], label: "Volume", min: 0, max: 1, step: 0.05, bind: _volume);

        // The scalar convenience overload is value-FIRST: the one exception to the
        // style-array-first rule.
        col.Slider(_volume.Value, style: ["w-full"], min: 0, max: 1, step: 0.05,
            onValueChange: async v => _volume.Value = v);

        // Date and time are ISO strings, not DateTime, and carry no bind: -- pair value: with
        // onValueChange.
        col.DatePicker(
            label: "Starts",
            value: _startDate.Value,
            onValueChange: async iso => _startDate.Value = iso);

        col.TimePicker(
            label: "At",
            value: _startTime.Value,
            granularity: TimeGranularity.Minute,
            onValueChange: async time => _startTime.Value = time);
    });
}
```

## See also

- `form-field-discipline` — labels, errors that survive a failed submit, and review before commit.
- `persistent-user-preferences` — when a setting should outlive the session.
