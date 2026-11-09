namespace Ikon.App.Platform.Patterns.Examples;

// The accessibility guide, as code that compiles.

file sealed class AccessibilityGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public void PageTitle()
    {
        #region example:accessibility-page-title
        app.OnClientJoined(ctx =>
        {
            _ = ClientFunctions.SetPageMetadataAsync(title: "Home – City Grants");
            return Task.CompletedTask;
        });
        #endregion
    }
}
