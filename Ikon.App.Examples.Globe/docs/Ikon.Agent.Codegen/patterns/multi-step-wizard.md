<!-- mined-from: Sentrix -->
# Multi-Step Wizard — Sidebar Steps + Back/Next Footer

A modal wizard with a fixed step list down the left side, the active step's form on the right, and a Back / Next / Submit footer. A `_wizardStep` Reactive int drives both the highlighted sidebar item and the `switch` that picks which step body to render.

## When to use

Any flow that gathers a long structured form across visible stages — case creation, onboarding, AI agent setup, multi-document submission. Use over a single tall form when steps have intra-step validation or the user needs a sense of progress.

## Snippet

```csharp
private static readonly string[] WizardStepLabels =
    ["Information", "Client", "Other parties", "Team", "Integrations", "Compliance", "Upload files"];

private readonly Reactive<int> _wizardStep = new(0);
private readonly Reactive<int> _wizardHighestStep = new(0);

private void RenderCreateCaseDialog(UIView view)
{
    view.Dialog(
        open: _showCreateCaseDialog.Value,
        modal: true,
        onOpenChange: async o => { _showCreateCaseDialog.Value = o ?? false; if (!(o ?? false)) ResetWizard(); },
        contentStyle: ["w-[860px] min-h-[85vh] max-h-[85vh] p-0 flex flex-col"],
        content: view =>
        {
            view.Row(["flex flex-1 min-h-0 overflow-hidden"], content: view =>
            {
                RenderWizardSidebar(view);
                view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
                {
                    view.Column(["px-6 py-5"], content: view =>
                    {
                        view.Text(["text-base font-semibold"],
                            TF("Step {0}  {1}", _wizardStep.Value + 1, T(WizardStepLabels[_wizardStep.Value])));

                        view.Box(key: $"wizard-step-{_wizardStep.Value}", content: view =>
                        {
                            switch (_wizardStep.Value)
                            {
                                case 0: RenderWizardStepInformation(view); break;
                                case 1: RenderWizardStepClient(view); break;
                                case 2: RenderWizardStepParties(view); break;
                                // ...
                            }
                        });
                    });
                });
            });
            RenderWizardFooter(view);
        });
}

private void RenderWizardFooter(UIView view)
{
    view.Row(["flex items-center justify-end gap-2 px-6 py-6 border-t"], content: view =>
    {
        if (_wizardStep.Value > 0)
            view.Button([Button.OutlineMd], text: T("Back"),
                onClick: async () => _wizardStep.Value -= 1);

        if (_wizardStep.Value < WizardStepLabels.Length - 1)
            view.Button([Button.PrimaryMd], text: T("Next"),
                onClick: async () =>
                {
                    _wizardStep.Value += 1;
                    if (_wizardStep.Value > _wizardHighestStep.Value)
                        _wizardHighestStep.Value = _wizardStep.Value;
                });
        else
            view.Button([Button.PrimaryMd], text: T("Create"),
                disabled: _isLoading.Value, onClick: async () => await ProcessWizardSubmitAsync());
    });
}
```

## Notes

- One `Reactive<int>` for the active step, a second for the highest step the user has reached. The "highest" gate makes earlier steps clickable in the sidebar but locks future steps until the user has advanced.
- The `key: $"wizard-step-{_wizardStep.Value}"` on the body box forces the form to re-mount per step — text fields keep their state by index, not by stale identity.
- Step labels are a `static readonly string[]`, indexed by the step int. Adding a step is one line in the array plus one `case` in the `switch`.
- The footer button morphs on the last step ("Next" → "Create") rather than rendering a separate submit button.
- Reset wizard state in `onOpenChange` when the dialog closes — don't carry abandoned drafts into the next open.

## See also

- `wizard-sidebar-stepper` — the visual sidebar that highlights done/active/locked steps.
- `ai-prefill-from-description` — prefill wizard fields from a free-form description in step 0.
