<!-- mined-from: Ikon.App.Patterns -->
# Form State And Submit — One Object Instead Of Four Reactives

`FormState<T>` owns the draft, the busy flag, the dirty flag, the field errors and the form-level
error. Hand-rolling that is four or five reactives that can disagree with each other — a submit
that leaves `IsBusy` stuck, an error that outlives the field it described, a draft cleared on a
failure so the user retypes everything.

`SubmitAsync` refuses while one is already in flight and when the validator returns anything, so the
button needs no guard of its own, and **errors survive the failed submit** — the draft is untouched.

## When to use

Any form with validation or a submit that can fail: an invite, a settings dialog, a create-record
flow. For a single always-saved field, a bound `TextField` is enough.

## Notes

- **Field names in `FormFieldError` are the same strings `ErrorFor` reads back.** A typo shows as
  an error that renders nowhere, which looks like validation silently passing. `nameof(...)` keeps
  the two ends together.
- **`Edit` runs under the reactive's per-client lock**, so two field handlers racing cannot lose
  each other's edits. With a record draft it is a `with` expression:
  `form.Edit(d => d with { Name = value })`.
- `Error` (form-level) and `ErrorFor(field)` are separate. A server rejection belonging to no single
  field lands in the former — render both or one class of failure is invisible.
- **A `FormException` thrown from the handler becomes a field or form error**, depending on which
  constructor was used. Any other exception becomes a form-level error carrying its message and is
  logged at warning — so an unexpected failure still reaches the user rather than vanishing.
- The form closes only when the handler completes and `closeOnSuccess` is true; pass `false` for a
  form that stays open to add another.
- `Show()` uses the `newDraft` factory; `Show(draft)` opens with an existing record, which is the
  edit case.
- `IsDirty` is what a "discard changes?" confirmation branches on.

## Snippet

```csharp
// FormState owns the draft, the busy flag, the dirty flag and the errors, so none of those
// become four separate reactives that can disagree with each other.
private readonly FormState<Invite> _form = new(() => new Invite());

private static IEnumerable<FormFieldError> Validate(Invite invite)
{
    // Field names here are the SAME strings ErrorFor reads back, so a typo shows as an error
    // that renders nowhere.
    if (!invite.Email.Contains('@'))
    {
        yield return new FormFieldError(nameof(Invite.Email), "Enter a valid email address");
    }
}

private async Task SubmitAsync()
{
    // SubmitAsync refuses while one is already in flight and when validate returns anything,
    // so the button needs no guard of its own. Errors SURVIVE the failed submit -- the draft
    // is untouched and the user is not retyping.
    await _form.SubmitAsync(
        onSubmit: async invite => await SaveAsync(invite),
        validate: Validate);
}

private void Render(IView view)
{
    if (!_form.IsOpen)
    {
        view.Button(onClick: () => _form.Show(), content: v => v.Text(text: "Invite someone"));
        return;
    }

    view.Column(["gap-3"], content: col =>
    {
        col.TextField(
            label: "Email",
            value: _form.Draft.Email,
            disabled: _form.IsBusy,
            // Edit runs under the reactive's per-client lock, so two field handlers racing
            // cannot lose each other's edits. With a record draft it is a `with` expression.
            onValueChange: async email => _form.Edit(d => d with { Email = email }));

        if (_form.ErrorFor(nameof(Invite.Email)) is { } fieldError)
        {
            col.Text(["text-destructive text-sm"], text: fieldError);
        }

        // A form-level error is separate from the field ones: a server rejection that belongs
        // to no single field lands here.
        if (_form.Error is { } formError)
        {
            col.Text(["text-destructive text-sm"], text: formError);
        }

        col.Row(["gap-2"], content: row =>
        {
            row.Button([Button.PrimaryMd],
                disabled: _form.IsBusy,
                onClick: SubmitAsync,
                content: v => v.Text(text: _form.IsBusy ? "Sending…" : "Send invite"));

            row.Button(
                onClick: () => _form.Close(),
                content: v => v.Text(text: "Cancel"));
        });
    });
}
```

## See also

- `form-field-discipline` — labels, error placement and review before a consequential action.
- `choice-controls-bound` — which controls take `bind:` for the fields themselves.
