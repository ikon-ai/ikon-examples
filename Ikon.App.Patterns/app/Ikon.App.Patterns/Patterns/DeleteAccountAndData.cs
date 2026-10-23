using Dapper;
using ServerRunType = Ikon.Common.Core.Protocol.ServerRunType;
using TriggerEventType = Ikon.Common.Core.Protocol.TriggerEventType;

namespace Ikon.App.Patterns.Patterns;

// Pattern: delete-account-and-data — see docs/patterns/delete-account-and-data.md.
// `app` is the App's primary-constructor handle. The demo draws the settings card in its default
// state; pressing through it needs a deployed app and a signed-in client.
internal sealed class DeleteAccountAndData(IAppBase app) : IPatternDemo
{
    public string Slug => "delete-account-and-data";
    public string Title => "Delete account and data";
    public string Category => "Accounts";
    public void RenderDemo(IView view) => RenderAccountSettings((UIView)view);

    #region example:pattern-delete-account-and-data
    private readonly ClientReactive<DateTimeOffset?> _removalDate = new((DateTimeOffset?)null);
    private readonly ClientReactive<bool> _confirmingRemoval = new(false);
    private readonly ClientReactive<string?> _removalError = new((string?)null);

    // Call from app.OnClientJoined(async ctx => await LoadRemovalAsync()), so a removal already scheduled shows with its date.
    private async Task LoadRemovalAsync()
    {
        var removal = await ClientFunctions.GetAccountRemovalAsync();
        _removalDate.Value = removal?.ScheduledFor;
    }

    private void RenderAccountSettings(UIView view)
    {
        view.Column([Card.Default, "p-6 gap-3 max-w-md"], content: view =>
        {
            view.Text([Text.H3], "Delete account");

            if (_removalDate.Value is { } date)
            {
                view.Text([Text.Body], $"Your account and everything in it will be deleted on {date:d MMMM yyyy}. You can change your mind until then.");
                view.Button([Button.OutlineMd, "self-start"], text: "Keep my account", onClick: CancelRemovalAsync);
                return;
            }

            view.Text([Text.Caption], "Deletes your profile, your lessons and your recordings after a grace period.");
            view.Button([Button.ErrorMd, "self-start"], text: "Delete my account…", onClick: async () => _confirmingRemoval.Value = true);

            if (_removalError.Value is { } error)
            {
                view.Text([Text.Caption, "text-error-primary"], error);
            }
        });

        view.AlertDialog(
            open: _confirmingRemoval.Value,
            onOpenChange: async open => _confirmingRemoval.Value = open,
            overlayStyle: [AlertDialog.Overlay], contentStyle: [AlertDialog.Content],
            title: "Delete your account?", titleStyle: [AlertDialog.Title],
            description: "Your account and all your data will be deleted after a grace period, and you can cancel until then.",
            descriptionStyle: [AlertDialog.Description],
            footerStyle: [AlertDialog.Footer],
            cancelLabel: "Keep it", cancelStyle: [AlertDialog.Cancel],
            actionLabel: "Delete my account", actionStyle: [Button.ErrorMd],
            onAction: RequestRemovalAsync);
    }

    private async Task RequestRemovalAsync()
    {
        // A local `ikon run` signs every browser in as YOU, so the platform refuses the request there
        // (Succeeded = false) and this says why instead of asking to sign in. Try it on a deployed app,
        // signed in with a throwaway account.
        if (app.GlobalState.ServerRunType == ServerRunType.Local)
        {
            _removalError.Value = "Account deletion is off in a local run";
            return;
        }

        var removal = await ClientFunctions.RequestAccountRemovalAsync();

        // Null: this client cannot remove accounts. Not Succeeded: a guest, or the request failed.
        if (removal is not { Succeeded: true })
        {
            _removalError.Value = "Couldn't schedule the deletion. Sign in and try again.";
            return;
        }

        _removalError.Value = null;
        _removalDate.Value = removal.ScheduledFor;
    }

    private async Task CancelRemovalAsync()
    {
        if (await ClientFunctions.CancelAccountRemovalAsync() is { Succeeded: true })
        {
            _removalDate.Value = null;
        }
    }
    #endregion
}

// Abstract so the bundle scan passes it over: an app has one UserErased listener, and the shared
// examples already declare theirs the same way.
file abstract class DeleteAccountAndDataTrigger(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:pattern-delete-account-and-data-trigger
    // Runs when the removal goes through (after the grace period, or at once when an admin deletes the
    // user), in the app's shared userless instance rather than the user's. The platform has already
    // erased the user's PersistentUserReactive values. The app deletes what it stores by user id here.
    [Trigger(TriggerEventType.UserErased)]
    internal async Task EraseUserDataAsync(UserDataErasureEventArgs args)
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();

        var recordings = await connection.QueryAsync<string>(
            "SELECT asset_uri FROM recordings WHERE user_id = @userId", new { userId = args.UserId });

        foreach (var uri in recordings)
        {
            try
            {
                await Asset.Instance.DeleteAsync(new AssetUri(uri));
            }
            catch (FileNotFoundException)
            {
                // Deleted by an earlier delivery of this event that failed further on.
            }
        }

        await connection.ExecuteAsync(
            "DELETE FROM recordings WHERE user_id = @userId; DELETE FROM lessons WHERE user_id = @userId",
            new { userId = args.UserId });

        // Every step is safe to repeat, so a redelivered event is harmless. Any other failure throws,
        // which leaves the event pending so that it is delivered again.
    }
    #endregion
}
