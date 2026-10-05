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
        app.Consent.Require(userId, ConsentPurposes.AiProviderPersonalData);
        #endregion
    }
}
