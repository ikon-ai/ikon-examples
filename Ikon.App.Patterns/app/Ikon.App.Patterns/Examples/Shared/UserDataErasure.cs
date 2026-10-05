using Dapper;
using Ikon.Common.Core.Protocol;
using System.Data.Common;

namespace Ikon.App.Patterns.Examples;

// Abstract so the bundle scan passes it over: these are two declarations of the one listener an app
// may have, and DiscoverTriggers skips abstract types, so the examples compile without registering
// either.
file abstract class UserDataErasureExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:user-data-erasure-database
    [Trigger(TriggerEventType.UserErased)]
    internal async Task EraseUserDataAsync(UserDataErasureEventArgs args)
    {
        await using var connection = await OpenAppDatabaseAsync();
        await connection.ExecuteAsync("DELETE FROM orders WHERE customer_id = @userId", new { userId = args.UserId });
    }
    #endregion

    #region example:user-data-erasure
    [Trigger(TriggerEventType.UserErased)]
    internal Task EraseAsync(UserDataErasureEventArgs args)
    {
        // Delete app-owned data for args.UserId: rows in your own tables,
        // personal data embedded in Session/Global scoped values.
        return Task.CompletedTask;
    }
    #endregion

    private async Task<DbConnection> OpenAppDatabaseAsync() =>
        await app.DatabaseAsync(app.Databases.First().Name);
}
