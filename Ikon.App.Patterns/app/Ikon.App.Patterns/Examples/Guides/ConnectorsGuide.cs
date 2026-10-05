using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class ConnectorsGuideExamples
{
    public async Task ErrorsAsync(Func<Task> connectorCall)
    {
        #region example:connectors-errors
        try
        {
            await connectorCall();   // any connector's call
        }
        catch (ConnectorException ex) when (ex.IsReconnectRequired)
        {
            // Permanent: the token is invalid, revoked or lacks access. Ask the user to reconnect.
        }
        catch (ConnectorException ex) when (ex.IsTransient)
        {
            // Busy or failing service: safe to retry later.
        }
        catch (ConnectorException)
        {
            // The request itself was refused (a missing channel, an unknown id): retrying will not help.
        }
        #endregion
    }
}
