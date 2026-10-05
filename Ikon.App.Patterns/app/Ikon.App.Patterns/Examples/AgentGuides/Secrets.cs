namespace Ikon.App.Patterns.Examples;

file sealed class SecretsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:secrets-runtime
    public async Task Main()
    {
        string token = app.Secrets["GITHUB_TOKEN"];

        if (app.Secrets.TryGet("SENTRY_DSN", out var dsn))
        {
            // wire up optional integration
        }
    }
    #endregion
}
