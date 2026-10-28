namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private void DocConsentGate(string userId)
    {
        #region example:consent-gate
        // A withdrawal has to reach what is already running, not just the next call.
        app.Consent.OnChanged(change =>
        {
            if (change.Record.Purpose == ConsentPurposes.UsageMeasurement && !change.Record.Granted)
            {
                _measuring.SetFor(change.UserId, false);
            }
        });

        // Gate the work itself, not only the button that starts it. A purpose nobody has answered
        // reads as NotAsked, which is never a yes.
        if (!app.Consent.IsGranted(userId, ConsentPurposes.UsageMeasurement))
        {
            // Say the feature is off. A measured feature that quietly records nothing is worse than
            // one that admits it is not measuring.
            _measurementState.Value = "Usage measurement is off until you allow it";
            return;
        }

        _measuring.SetFor(userId, true);

        // Or let it throw: ConsentRequiredException stops the path rather than letting it proceed
        // with less than it promised.
        app.Consent.Require(userId, ConsentPurposes.AIProviderPersonalData);
        #endregion
    }

    private void DocConsentPrompt(IView view)
    {
        #region example:consent-prompt
        // A guest has a user id of its own and can answer. A client with no user id, or a visitor on the
        // space's one global guest id, sees no prompt, because there is no one to record the answer for.
        if (ReactiveScope.UserIdOrNull is not { Length: > 0 } userId || app.CurrentClientContext is { IsGlobal: true })
        {
            return;
        }

        // StateOf is a tracked read: once the user answers, this render runs again and the card goes.
        if (app.Consent.StateOf(userId, ConsentPurposes.AIProviderPersonalData).State != ConsentAnswer.NotAsked)
        {
            return;
        }

        view.Card([Card.Default], title: "Summaries use an AI provider",
            description: "Your notes are sent to an AI model provider to write the summary. You can change this later in Settings.",
            footer: footer =>
            {
                footer.Button([Button.PrimarySm], text: "Allow", onClick: () => app.Consent.Grant(userId, ConsentPurposes.AIProviderPersonalData));
                footer.Button([Button.OutlineSm], text: "Don't allow", onClick: () => app.Consent.Revoke(userId, ConsentPurposes.AIProviderPersonalData));
            });
        #endregion
    }
}
