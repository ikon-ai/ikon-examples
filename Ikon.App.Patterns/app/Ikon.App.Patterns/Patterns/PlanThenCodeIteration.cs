namespace Ikon.App.Patterns.Patterns;

// Pattern: plan-then-code-iteration — see docs/patterns/plan-then-code-iteration.md.
// The records, model/prompt constants, reactives and helpers outside the region stand in for the app's
// persisted plan+artifact state and the context-narrowing / merge helpers the two-stage pipeline calls.
internal sealed class PlanThenCodeIteration : IPatternDemo
{
    public string Slug => "plan-then-code-iteration";
    public string Title => "Plan-then-code iteration";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend AI pattern with no standalone UI: a two-stage plan-adjust then code-regenerate pipeline over persisted plan and artifact state. See the source and docs/patterns/plan-then-code-iteration.md.");

    private sealed record GamePlanAdjustResponse(string UpdatedSections);
    private sealed record GameResponse(string Code);
    private sealed record FocusedContext(string RelevantPlanSections, string CodeSummary, string FocusAreas);

    private static readonly LLMModel CodeGenModel = LLMModel.Claude45Sonnet;
    private const string PlanAdjustSystemPrompt = "Output ONLY the plan sections that need to change.";
    private const string PlanGenerateSystemPrompt = "Regenerate the full HTML game from the plan.";

    private readonly Reactive<string> _statusText = new("");
    private readonly Reactive<string?> _currentPlan = new(null);
    private readonly Reactive<string> _currentGameHtml = new("");

    private Task<string> CreatePlanFromHtmlAsync(string html) => throw new NotImplementedException();
    private Task<FocusedContext?> PrepareContextAsync(string? plan, string html, string prompt) => throw new NotImplementedException();
    private static string MergePlanSections(string plan, string updatedSections) => throw new NotImplementedException();
    private static string InjectBridgeScript(string code) => throw new NotImplementedException();

    #region docsnippet:pattern-plan-then-code-iteration
    private async Task ModifyGameAsync(int clientId, string prompt)
    {
        if (_currentPlan.Value == null)
        {
            _statusText.Value = "CREATING PLAN...";
            _currentPlan.Value = await CreatePlanFromHtmlAsync(_currentGameHtml.Value);
        }

        _statusText.Value = "UPDATING PLAN...";
        var existingPlan = _currentPlan.Value!;
        var planResult = await Emerge.Run<GamePlanAdjustResponse>(
            CodeGenModel,
            pass =>
            {
                pass.SystemPrompt = PlanAdjustSystemPrompt;
                pass.Command = $"Current plan:\n{existingPlan}\n\nRequested changes: {prompt}";
                pass.Temperature = 0.7;
                pass.MaxOutputTokens = 4000;
                pass.UseJson = true;
            });

        if (!string.IsNullOrWhiteSpace(planResult.UpdatedSections))
        {
            _currentPlan.Value = MergePlanSections(existingPlan, planResult.UpdatedSections);
        }

        _statusText.Value = "GENERATING...";
        var focused = await PrepareContextAsync(_currentPlan.Value, _currentGameHtml.Value, prompt);
        var command = focused != null
            ? $"Relevant plan sections:\n{focused.RelevantPlanSections}\n\n"
                + $"Code structure summary:\n{focused.CodeSummary}\n\n"
                + $"Focus areas:\n{focused.FocusAreas}\n\n"
                + $"Current HTML:\n{_currentGameHtml.Value}\n\n"
                + "Apply the changes."
            : $"Plan:\n{_currentPlan.Value}\n\nCurrent HTML:\n{_currentGameHtml.Value}\n\nChange: {prompt}";

        var result = await Emerge.Run<GameResponse>(CodeGenModel,
            pass =>
            {
                pass.SystemPrompt = PlanGenerateSystemPrompt;
                pass.Command = command;
                pass.MaxOutputTokens = 32000;
                pass.UseJson = true;
            });

        _currentGameHtml.Value = InjectBridgeScript(result.Code);
    }
    #endregion
}
