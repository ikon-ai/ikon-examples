namespace Ikon.App.Patterns.Patterns;

// Pattern: form-state-submit — see docs/patterns/form-state-submit.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class FormStateSubmit : IPatternDemo
{
    public string Slug => "form-state-submit";
    public string Title => "Form draft, validation and submit";
    public string Category => "Forms & input";
    public void RenderDemo(IView view) => Render(view);

    private static Task SaveAsync(FormStateSubmit.Invite invite) => throw new NotImplementedException();

    internal sealed record Invite(string Email = "", string Role = "member");

    #region docsnippet:pattern-form-state-submit
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
    #endregion
}
