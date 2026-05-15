<!-- mined-from: Sentrix -->
# AI Prefill — Free-Form Description → Structured Form Fields

User pastes a free-form description into a textarea. The app calls `Emerge.Run<T>` with a JSON schema, then writes the returned fields back into the existing wizard state — but only into fields the user hasn't already filled. A `_aiPrefillApplied` flag prevents re-running on every keystroke.

## When to use

Any form where the user can describe the goal in prose faster than they can fill the fields one by one — case intake, project setup, character creation, recipe entry, agent definition. Especially valuable as the first step of a wizard.

## Snippet

```csharp
private async Task RunWizardAiPrefillAsync()
{
    if (string.IsNullOrWhiteSpace(_newCaseDescription.Value) || _wizardAiPrefillApplied.Value)
    {
        return;
    }

    _wizardIsAnalyzing.Value = true;

    try
    {
        var model = await ResolveModelAsync(ModelTier.Large);
        var (result, _) = await Emerge.Run<CaseDescriptionAnalysis>(
            model, new KernelContext(), pass =>
            {
                pass.SystemPrompt = "You are a legal case intake assistant. Analyze the case description and extract structured information. " +
                    "Extract every detail that is stated or strongly implied — names, identifiers, contact details, addresses. " +
                    "When ambiguous, pick the most likely option rather than leaving the field blank — the user will verify before submitting. " +
                    "Do not invent details that are not present in the description.";
                pass.Command = $"Analyze this legal case description and extract structured information.\n\n" +
                    $"Description:\n{_newCaseDescription.Value}\n\n" +
                    $"Return JSON:\n{pass.JsonSchema}";
                pass.Temperature = 0.3;
                pass.MaxOutputTokens = 6000;
            }).FinalAsync();

        if (result != null)
        {
            // Only fill empty fields — never overwrite what the user has typed.
            if (string.IsNullOrWhiteSpace(_newCaseName.Value) && !string.IsNullOrEmpty(result.SuggestedCaseName))
                _newCaseName.Value = result.SuggestedCaseName;

            if (string.IsNullOrWhiteSpace(_wizardClientName.Value) && !string.IsNullOrEmpty(result.ClientName))
                _wizardClientName.Value = result.ClientName;

            if (result.ContactPersons.Count > 0 && _wizardContactPersons.Value.Count == 0)
            {
                _wizardContactPersons.AddRange(result.ContactPersons.Select(_ => new WizardContactPerson { /* ... */ }));
            }

            _wizardAiPrefillApplied.Value = true;
        }
    }
    catch (Exception ex)
    {
        Log.Instance.Warning($"AI prefill failed: {ex.Message}");
    }
    finally
    {
        _wizardIsAnalyzing.Value = false;
    }
}
```

## Notes

- Guard at the top: bail if description is empty or `_aiPrefillApplied` is already true. Otherwise the user re-triggers the call every time they advance/retreat in the wizard.
- Only fill fields the user hasn't touched (`string.IsNullOrWhiteSpace(_field.Value)` checks). Overwriting typed values is the fastest way to lose user trust.
- Surface progress with a separate `_wizardIsAnalyzing.Value = true/false` flag — show "Analyzing…" in the header, don't block the form.
- `pass.SystemPrompt` is explicit about ambiguity policy: "pick the most likely option rather than leaving blank — the user will verify". This shifts the LLM's natural caution toward completeness.
- Catch and log; never bubble — a failed prefill should silently fall back to manual entry.

## See also

- `multi-step-wizard` — the wizard host this prefill plugs into at step 0.
- `emergence` (top-level guide) — full `Emerge.Run<T>` signatures and structured-output patterns.
