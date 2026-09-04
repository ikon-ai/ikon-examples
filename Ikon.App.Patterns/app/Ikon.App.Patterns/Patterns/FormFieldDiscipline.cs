namespace Ikon.App.Patterns.Patterns;

// Pattern: form-field-discipline — see docs/patterns/form-field-discipline.md.
// The docsnippet region is a whole form: persistent labels, per-field errors that survive a failed
// submit, and a review step before the consequential action. The stub outside it stands in for the
// thing the form actually does once confirmed.
internal sealed class FormFieldDiscipline : IPatternDemo
{
    public string Slug => "form-field-discipline";
    public string Title => "Form field discipline";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => RenderForm(view);

    private void CreateAccount() => _confirming.Value = false;

    #region docsnippet:pattern-form-field-discipline
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
    #endregion
}
