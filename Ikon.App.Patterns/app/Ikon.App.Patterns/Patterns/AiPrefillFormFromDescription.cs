namespace Ikon.App.Patterns.Patterns;

// Pattern: ai-prefill-form-from-description — see docs/patterns/ai-prefill-form-from-description.md.
// The stubs outside the region stand in for the wizard state and the extraction shape the app owns;
// the docsnippet region is the canonical body the doc extracts.
internal sealed class AiPrefillFormFromDescription : IPatternDemo
{
    public string Slug => "ai-prefill-form-from-description";
    public string Title => "AI prefill form from description";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: runs an LLM extraction over a free-text description and fills only the empty wizard fields. See the source and docs/patterns/ai-prefill-form-from-description.md.");

    private readonly Reactive<string> _newCaseDescription = new("");
    private readonly Reactive<string> _newCaseName = new("");
    private readonly Reactive<string> _wizardClientName = new("");
    private readonly Reactive<bool> _wizardAiPrefillApplied = new(false);
    private readonly Reactive<bool> _wizardIsAnalyzing = new(false);
    private readonly ReactiveList<WizardContactPerson> _wizardContactPersons = new();

    private sealed record CaseDescriptionAnalysis(
        string SuggestedCaseName, string ClientName, List<string> ContactPersons);

    private sealed class WizardContactPerson
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    #region docsnippet:pattern-ai-prefill-form-from-description
    private async Task RunWizardAiPrefillAsync()
    {
        if (string.IsNullOrWhiteSpace(_newCaseDescription.Value) || _wizardAiPrefillApplied.Value)
        {
            return;
        }

        _wizardIsAnalyzing.Value = true;

        try
        {
            var result = await Emerge.Run<CaseDescriptionAnalysis>(
                LLMModel.Claude46Sonnet, pass =>
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
                });

            // Only fill empty fields — never overwrite what the user has typed.
            if (string.IsNullOrWhiteSpace(_newCaseName.Value) && !string.IsNullOrEmpty(result.SuggestedCaseName))
            {
                _newCaseName.Value = result.SuggestedCaseName;
            }

            if (string.IsNullOrWhiteSpace(_wizardClientName.Value) && !string.IsNullOrEmpty(result.ClientName))
            {
                _wizardClientName.Value = result.ClientName;
            }

            if (result.ContactPersons.Count > 0 && _wizardContactPersons.Count == 0)
            {
                _wizardContactPersons.AddRange(result.ContactPersons.Select(_ => new WizardContactPerson { /* ... */ }));
            }

            _wizardAiPrefillApplied.Value = true;
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
    #endregion
}
