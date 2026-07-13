<!-- mined-from: NeonArcade -->
# Plan-Then-Code — Persistent Plan Drives Code Generation

Two-stage LLM pipeline for generating large artifacts (HTML games, full-page UIs, long documents). First call produces a structured plan ("GAME TITLE / CORE MECHANICS / VISUAL DESIGN / ..."), saved alongside the artifact. Subsequent edits update only the named sections that changed (`PlanAdjustSystemPrompt` outputs *only* changed sections; `MergePlanSections` splices them in), then a second call regenerates code from the merged plan. Avoids context blow-up and keeps human-readable intent persistent.

## When to use

Generating artifacts > ~300 lines that go through many edit rounds. Without a plan, every edit re-derives the design from the code, drifts, and forgets earlier decisions. The plan is the canonical "what we agreed" — code is the regeneration target.

## Snippet

```csharp
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
```

## Notes

- Plan adjust prompt explicitly says "Output ONLY the sections that need to change" — without this the LLM repeats the whole plan and the diff is meaningless.
- `MergePlanSections` is a header-aware string splice (regex on `^# SECTION` lines), not JSON — keeps the plan human-readable.
- The "focused context" pass (cheap Gemini Flash) extracts relevant plan sections + a code-structure summary so the expensive code-gen pass doesn't get the full HTML twice.
- Persist the plan with the artifact (`GameEntry.Plan`) — every reopen gets the same context.

## See also

- `ai-prefill-form-from-description`
- `web-research`
