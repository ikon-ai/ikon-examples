# Form Field Discipline — Labels, Errors That Survive, and a Review Step

Forms are where a generated app most often looks finished and behaves badly. The recurring defects are always the same three: the placeholder is the only label, so the field is nameless the moment someone types; a failed submit clears the fields that were fine; and a consequential action fires straight off the button with nothing stated back.

One `Validate()` that checks everything and keeps every value, per-field errors under the field they belong to, and a review step before anything irreversible.

## When to use

Any structured entry beyond a single search box — sign-up, settings, a create/edit sheet, a booking, a multi-field filter. Not for a one-field composer, where a placeholder is genuinely enough.

## Snippet

```csharp
private readonly Reactive<string> _name = new("");
private readonly Reactive<string> _email = new("");
private readonly Reactive<string> _plan = new("");
private readonly ReactiveDictionary<string, string> _errors = new();
private readonly Reactive<bool> _confirming = new(false);

/// Validate everything at once and KEEP every entered value. A form that clears the other
/// fields because one failed is the fastest way to lose a user.
private bool Validate()
{
    _errors.Clear();

    if (_name.Value.Trim().Length == 0)
    {
        _errors["name"] = "Enter the name this account appears under.";
    }

    if (!_email.Value.Contains('@', StringComparison.Ordinal))
    {
        _errors["email"] = "That does not look like an email address.";
    }

    if (_plan.Value.Length == 0)
    {
        _errors["plan"] = "Pick a plan to continue.";
    }

    return _errors.Count == 0;
}

private void RenderForm(IView view)
{
    view.Column([Layout.Column.Md, "max-w-md w-full"], content: view =>
    {
        // label: is a PERSISTENT label above the field. A placeholder disappears the moment
        // someone types, so it can never be the only name a required field has.
        RenderField(view, "name", v => v.TextField([Input.Default],
            label: "Full name", placeholder: "Aino Virtanen", bind: _name));

        RenderField(view, "email", v => v.TextField([Input.Default],
            label: "Work email", placeholder: "aino@example.com", bind: _email));

        RenderField(view, "plan", v => v.Select([Input.Default],
            label: "Plan", placeholder: "Choose a plan", bind: _plan, options:
            [
                new SelectOption("free", "Free"),
                new SelectOption("team", "Team — 12 €/user/month"),
            ]));

        view.Text([Text.Caption], text: "All fields required.");

        view.Button([Button.PrimaryMd, "self-start"], text: "Create account",
            onClick: async () => { if (Validate()) { _confirming.Value = true; } });
    });

    // A consequential action gets a review step: what is about to happen, stated back, with
    // the way out beside it. Reversible in-app actions do NOT get one.
    view.AlertDialog(
        open: _confirming.Value,
        onOpenChange: async open => _confirming.Value = open,
        overlayStyle: [AlertDialog.Overlay], contentStyle: [AlertDialog.Content],
        title: "Create this account?",
        titleStyle: [AlertDialog.Title],
        description: $"{_name.Value} · {_email.Value} · {_plan.Value} plan. We will email a confirmation.",
        descriptionStyle: [AlertDialog.Description],
        footerStyle: [AlertDialog.Footer],
        cancelLabel: "Back", cancelStyle: [AlertDialog.Cancel],
        actionLabel: "Create account", actionStyle: [Button.PrimaryMd],
        onAction: async () => CreateAccount());
}

/// One field, its error slot always reserved so nothing jumps when a message appears.
private void RenderField(IView view, string key, Action<IView> field)
{
    view.Column([Layout.Column.Xs], content: v =>
    {
        field(v);

        if (_errors.ContainsKey(key))
        {
            v.Row([Layout.Row.Xs, "text-error-primary"], content: row =>
            {
                row.Icon([Icon.Xs], name: "circle-alert");
                row.Text(["text-xs"], text: _errors[key]);
            });
        }
    });
}
```

## Notes

- **`label:` renders a persistent visible label above the field; `placeholder:` is an example, not a name.** Use both, and never let the placeholder carry the only identification a required field has — it vanishes on first keystroke, and it is invisible to anyone reviewing what they typed.
- Validate everything in one pass and **keep every entered value**. Rebuilding a form because one field failed is the single most common reason people abandon one. Never clear siblings.
- Put the message under the field it belongs to, not in a summary banner at the top — the user has to be able to see what to fix without hunting.
- Say what to do, not what is wrong: "That does not look like an email address" beats "Invalid input".
- Mark required vs optional wherever the distinction matters. One line ("All fields required") beats an asterisk on every label when everything is required.
- Use the right control for the data — a `Select` for a closed set, a date picker for dates, a `Switch` for a binary. A text field asking someone to type a plan name is a validation problem you created.
- **A consequential submit gets a review step**, stating back what is about to happen with the way out beside it: creating an account, sending, charging, publishing, deleting. Reversible in-app actions must NOT get one — a confirm on everyday work trains people to click through dialogs without reading, which is what breaks them when a real one appears.
- Reserve the error slot in the layout, or the form jumps as messages appear and disappear under the pointer.
- For a long or consequential flow, break it into steps with visible progress, and preserve entered data when the user moves backward.
- **The rhythm is a token, not a judgement call.** `FormField.Root` binds a field's label, control and supporting text at 8px; `FormField.Group` separates whole fields at 20px. That 2.5:1 ratio is what makes a field group read as one thing — close it up and the form becomes one undifferentiated column, which is the most common way a generated form looks wrong while every individual control is fine.
- **Reserve the supporting-text line from the start** with `FormField.Support`. Help and error share that one line, so a validation message replaces the hint instead of pushing the rest of the form down while someone is fixing it. Error state changes colour, never border thickness — geometry that moves under a mistake reads as a second mistake.

## See also

- `ai-prefill-form-from-description` — letting an LLM fill this form from a sentence, before the user corrects it.
- `overlay-selection` — choosing the review step's shape, and why it is `AlertDialog` here.
- `zero-results-state` — the same three-way discipline applied to a collection.
