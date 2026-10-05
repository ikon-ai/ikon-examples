using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    private const int SpNotificationLogSize = 50;

    // Graph sends it back with every notification, so anything without it did not come from these
    // subscriptions. A fresh one per app instance is enough for a manual test.
    private readonly string _spClientState = Guid.NewGuid().ToString("N");

    private readonly ReactiveList<SpSubscription> _spSubscriptions = new();
    private readonly ReactiveList<string> _spNotificationLog = new();

    private sealed record SpSubscription(string Id, string Name, string? DriveId, string? SiteId, string? ListId, DateTimeOffset? ExpiresAt);

    private void RenderSharePointNotificationsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Change notifications");

            if (SpNotificationUrl() is not { } url)
            {
                view.Text([Text.Body, "text-warning-primary"], "SKIP: Graph delivers only to a public https URL; deploy the app, or run it with a public tunnel",
                    props: TestId("sp-notifications-skip"));
                return;
            }

            view.Text([Text.Caption, "mb-3 break-all"], $"Graph posts to {url}; change something in SharePoint and watch the log");

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: _spLibrary.Value is { } library ? $"Subscribe {library.Name}" : "Subscribe a library",
                    disabled: _spBusy.Value || _spLibrary.Value == null, props: TestId("sp-subscribe-library"),
                    onClick: async () => await SpRunAsync("subscribe the library", SubscribeSharePointLibraryAsync));
                view.Button([Button.OutlineSm], text: _spList.Value is { } list ? $"Subscribe {list.DisplayName}" : "Subscribe a list",
                    disabled: _spBusy.Value || _spList.Value == null, props: TestId("sp-subscribe-list"),
                    onClick: async () => await SpRunAsync("subscribe the list", SubscribeSharePointListAsync));
                view.Button([Button.GhostSm], text: "Renew all", disabled: _spBusy.Value || _spSubscriptions.Count == 0, props: TestId("sp-subscriptions-renew"),
                    onClick: async () => await SpRunAsync("renew the subscriptions", RenewSharePointSubscriptionsAsync));
                view.Button([Button.GhostErrorSm], text: "Unsubscribe all", disabled: _spBusy.Value || _spSubscriptions.Count == 0, props: TestId("sp-subscriptions-delete"),
                    onClick: async () => await SpRunAsync("delete the subscriptions", DeleteSharePointSubscriptionsAsync));
            });

            foreach (var subscription in _spSubscriptions)
            {
                view.Text([Text.Caption], $"{subscription.Name} · until {subscription.ExpiresAt:yyyy-MM-dd HH:mm} UTC · {subscription.Id}", key: subscription.Id, props: TestId("sp-subscription"));
            }

            if (_spNotificationLog.Count > 0)
            {
                view.Text([Text.Label, "mt-3 mb-1"], "Received");

                foreach (var line in _spNotificationLog)
                {
                    view.Text([Text.Caption, "font-mono"], line, props: TestId("sp-notification"));
                }
            }
        });
    }

    [HttpPost("/sharepoint/notifications", Auth = EndpointAuth.Public)]
    public HttpResult SharePointNotification(HttpRequest request)
    {
        if (GraphNotifications.ValidationToken(request.Query) is { } token)
        {
            SpLogNotification("Graph checked the notification URL");
            return new HttpResult(200, token, "text/plain");
        }

        foreach (var notification in GraphNotifications.Parse(request.Body, _spClientState))
        {
            var subscription = _spSubscriptions.FirstOrDefault(s => s.Id == notification.SubscriptionId);
            var name = subscription?.Name ?? notification.SubscriptionId;

            if (notification.LifecycleEvent is { } lifecycle)
            {
                SpLogNotification($"{name}: {lifecycle}");

                if (lifecycle == "reauthorizationRequired")
                {
                    _ = Task.Run(() => SpReauthorizeAsync(notification.SubscriptionId, name));
                }

                continue;
            }

            SpLogNotification($"{name}: {notification.ChangeType}");

            if (subscription is not null)
            {
                // Graph wants an answer within three seconds, so the delta read runs after it.
                _ = Task.Run(() => SpReadAfterNotificationAsync(subscription));
            }
        }

        return new HttpResult(202, "", "text/plain");
    }

    private string? SpNotificationUrl()
    {
        return app.Endpoints.FirstOrDefault(e => e.FunctionName.EndsWith("_" + nameof(SharePointNotification), StringComparison.Ordinal))?.PublicUrl is { } url
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : null;
    }

    private GraphSubscriptions SpSubscriptionClient()
    {
        return new GraphSubscriptions(SpConnection().Tokens);
    }

    private async Task<string> SubscribeSharePointLibraryAsync()
    {
        var library = _spLibrary.Value!;
        var created = await SpSubscriptionClient().CreateAsync(SubscriptionResource.DriveRoot(library.Id), SpNotificationUrl()!, _spClientState,
            TimeSpan.FromDays(1), lifecycleNotificationUrl: SpNotificationUrl());

        _spSubscriptions.Add(new SpSubscription(created.Id, library.Name, library.Id, null, null, created.ExpiresAt));

        // A delta link from now means the first notification reads only what changed after it.
        var (_, oneDrive) = SpClients();
        _spDeltaLinks[library.Id] = (await oneDrive.DeltaAsync(library.Id, null, new DriveDeltaOptions(FromNow: true))).DeltaLink;
        return $"PASS subscribed to {library.Name} until {created.ExpiresAt:yyyy-MM-dd HH:mm} UTC; Graph accepted the URL";
    }

    private async Task<string> SubscribeSharePointListAsync()
    {
        var list = _spList.Value!;
        var siteId = _spSite.Value!.Id;
        var created = await SpSubscriptionClient().CreateAsync(SubscriptionResource.List(siteId, list.Id), SpNotificationUrl()!, _spClientState,
            TimeSpan.FromDays(1), lifecycleNotificationUrl: SpNotificationUrl());

        _spSubscriptions.Add(new SpSubscription(created.Id, list.DisplayName, null, siteId, list.Id, created.ExpiresAt));

        var (sharePoint, _) = SpClients();
        _spListDeltaLinks[list.Id] = (await sharePoint.ItemsDeltaAsync(siteId, list.Id, fromNow: true)).DeltaLink;
        return $"PASS subscribed to {list.DisplayName} until {created.ExpiresAt:yyyy-MM-dd HH:mm} UTC; Graph accepted the URL";
    }

    private async Task<string> RenewSharePointSubscriptionsAsync()
    {
        var client = SpSubscriptionClient();
        var renewed = new List<SpSubscription>();

        foreach (var subscription in _spSubscriptions)
        {
            var updated = await client.RenewAsync(subscription.Id, TimeSpan.FromDays(2));
            renewed.Add(subscription with { ExpiresAt = updated.ExpiresAt });
        }

        _spSubscriptions.ReplaceAll(renewed);
        return $"PASS renewed {renewed.Count} subscriptions";
    }

    private async Task<string> DeleteSharePointSubscriptionsAsync()
    {
        var client = SpSubscriptionClient();
        var count = _spSubscriptions.Count;

        foreach (var subscription in _spSubscriptions.ToList())
        {
            try
            {
                await client.DeleteAsync(subscription.Id);
            }
            catch (ConnectorException ex) when (ex.StatusCode == 404)
            {
                // Already ended on Graph's side; there is nothing left to delete.
            }
        }

        _spSubscriptions.Clear();
        return $"PASS deleted {count} subscriptions";
    }

    private async Task SpReadAfterNotificationAsync(SpSubscription subscription)
    {
        try
        {
            if (subscription.DriveId is { } driveId)
            {
                var (_, oneDrive) = SpClients();
                _spDeltaLinks.TryGetValue(driveId, out var link);
                var delta = await oneDrive.DeltaAsync(driveId, link, new DriveDeltaOptions(MaxPages: 10));
                _spDeltaLinks[driveId] = delta.DeltaLink;
                var changed = delta.Items.Where(i => i.Name != "root").ToList();
                SpLogNotification($"{subscription.Name}: delta read {changed.Count} changes — {string.Join(", ", changed.Take(5).Select(i => i.Deleted ? $"removed {i.Id}" : i.Name))}");
            }
            else if (subscription is { SiteId: { } siteId, ListId: { } listId })
            {
                var (sharePoint, _) = SpClients();
                _spListDeltaLinks.TryGetValue(listId, out var link);
                var delta = await sharePoint.ItemsDeltaAsync(siteId, listId, link, maxPages: 10);
                _spListDeltaLinks[listId] = delta.DeltaLink;
                SpLogNotification($"{subscription.Name}: delta read {delta.Items.Count} changed items — {string.Join(", ", delta.Items.Take(5).Select(i => i.Deleted ? $"removed #{i.Id}" : SpField(i, "Title") ?? $"#{i.Id}"))}");
            }
        }
        catch (Exception ex)
        {
            SpLogNotification($"{subscription.Name}: delta read failed: {ex.Message}");
        }
    }

    private async Task SpReauthorizeAsync(string subscriptionId, string name)
    {
        try
        {
            await SpSubscriptionClient().ReauthorizeAsync(subscriptionId);
            SpLogNotification($"{name}: reauthorized");
        }
        catch (Exception ex)
        {
            SpLogNotification($"{name}: reauthorize failed: {ex.Message}");
        }
    }

    private void SpLogNotification(string line)
    {
        _spNotificationLog.Insert(0, $"{DateTime.UtcNow:HH:mm:ss} {line}");

        while (_spNotificationLog.Count > SpNotificationLogSize)
        {
            _spNotificationLog.RemoveAt(_spNotificationLog.Count - 1);
        }
    }
}
