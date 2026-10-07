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
            // Busy or failing service: the call may succeed later, but a write that timed out (504) may already have taken effect.
        }
        catch (ConnectorException)
        {
            // The request itself was refused (a missing channel, an unknown id): retrying will not help.
        }
        #endregion
    }
}
