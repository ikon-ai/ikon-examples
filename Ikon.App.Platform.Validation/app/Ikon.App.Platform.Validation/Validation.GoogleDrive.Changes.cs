using Ikon.Connectors;
using Ikon.Connectors.Google;

public partial class Validation
{
    private const int GdChangePages = 5;
    private const int GdNotificationLogSize = 50;

    private static readonly TimeSpan GdChannelLifetime = TimeSpan.FromHours(1);

    // Google sends it back on every notification, so a request without it did not come from these
    // channels. A fresh one per app instance is enough for a manual test.
    private readonly string _gdChannelToken = Guid.NewGuid().ToString("N");

    private readonly ReactiveList<DriveFile> _gdChanges = new();
    private readonly Reactive<string?> _gdChangesSummary = new(null);
    private readonly ReactiveList<GoogleChannel> _gdChannels = new();
    private readonly ReactiveList<string> _gdNotificationLog = new();

    private string? _gdDeltaToken;

    private void RenderGoogleDriveChangesCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Changes");
            view.Text([Text.Caption, "mb-3"], _gdDeltaToken == null
                ? "The first check reads the whole Drive and remembers where it stopped; Start from now skips that"
                : "Shows what changed in My Drive and what is shared with you since the last check");

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Check for changes", disabled: _gdBusy.Value, props: TestId("gd-changes"),
                    onClick: async () => await GdRunAsync("check for changes", () => GdReadChangesAsync(fromNow: false)));
                view.Button([Button.GhostMd], text: "Start from now", disabled: _gdBusy.Value, props: TestId("gd-changes-now"),
                    onClick: async () => await GdRunAsync("start changes from now", () => GdReadChangesAsync(fromNow: true)));
            });

            if (_gdChangesSummary.Value is { } summary)
            {
                view.Text([Text.Body, "mt-3"], summary, props: TestId("gd-changes-summary"));
            }

            foreach (var change in _gdChanges.Take(50))
            {
                view.Text([Text.Caption], $"{GdChangeKind(change)} · {(change.Deleted ? change.Id : change.Name)}{(change.ModifiedTime is { } at ? $" · {at:yyyy-MM-dd HH:mm}" : "")}",
                    key: change.Id, props: TestId("gd-change"));
            }
        });
    }

    private void RenderGoogleDrivePushCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Push notifications");

            if (GdNotificationUrl() is not { } url)
            {
                view.Text([Text.Body, "text-warning-primary"], "SKIP: Google delivers only to a public https URL; deploy the app, or run it with a public tunnel",
                    props: TestId("gd-push-skip"));
                return;
            }

            view.Text([Text.Caption, "mb-3 break-all"], $"Google posts to {url}; change something in Drive and watch the log. A channel lives at most {GdChannelLifetime.TotalHours:0} hour here.");

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Watch Drive changes", disabled: _gdBusy.Value, props: TestId("gd-watch-changes"),
                    onClick: async () => await GdRunAsync("watch Drive changes", GdWatchChangesAsync));
                view.Button([Button.OutlineSm], text: _gdActiveItem.Value is { } active ? $"Watch {active.Name}" : "Watch a file (press More on one)",
                    disabled: _gdBusy.Value || _gdActiveItem.Value == null, props: TestId("gd-watch-file"),
                    onClick: async () => await GdRunAsync("watch the file", GdWatchFileAsync));
                view.Button([Button.GhostErrorSm], text: "Stop all", disabled: _gdBusy.Value || _gdChannels.Count == 0, props: TestId("gd-watch-stop"),
                    onClick: async () => await GdRunAsync("stop the channels", GdStopChannelsAsync));
            });

            foreach (var channel in _gdChannels)
            {
                var what = channel.Target.Kind == "driveChanges" ? "Drive changes" : $"file {channel.Target.Id}";
                view.Text([Text.Caption], $"{what} · until {channel.ExpiresAt:HH:mm} UTC · {channel.Id}", key: channel.Id, props: TestId("gd-channel"));
            }

            if (_gdNotificationLog.Count > 0)
            {
                view.Text([Text.Label, "mt-3 mb-1"], "Received");

                foreach (var line in _gdNotificationLog)
                {
                    view.Text([Text.Caption, "font-mono"], line, props: TestId("gd-notification"));
                }
            }
        });
    }

    [HttpPost("/google/notifications", Auth = EndpointAuth.Public)]
    public HttpResult GoogleDriveNotification(HttpRequest request)
    {
        if (GoogleNotifications.ParseChannel(request.Headers, _gdChannelToken) is not { } notification)
        {
            GdLogNotification("ignored a request that did not come from this tab's channels");
            return new HttpResult(200, "", "text/plain");
        }

        var channel = _gdChannels.FirstOrDefault(c => c.Id == notification.ChannelId);
        var what = channel?.Target.Kind == "driveFile" ? $"file {channel.Target.Id}" : "Drive changes";

        if (notification.ResourceState == "sync")
        {
            GdLogNotification($"{what}: channel open (sync)");
            return new HttpResult(200, "", "text/plain");
        }

        GdLogNotification($"{what}: {notification.ResourceState} #{notification.MessageNumber}{(notification.Changed.Count > 0 ? $" ({string.Join(", ", notification.Changed)})" : "")}");

        if (channel?.Target.Kind == "driveChanges")
        {
            // Google wants a quick answer and the notification says only that something changed, so
            // reading what changed runs after it.
            _ = Task.Run(GdReadAfterNotificationAsync);
        }

        return new HttpResult(200, "", "text/plain");
    }

    private string? GdNotificationUrl()
    {
        return app.Endpoints.FirstOrDefault(e => e.FunctionName.EndsWith("_" + nameof(GoogleDriveNotification), StringComparison.Ordinal))?.PublicUrl is { } url
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : null;
    }

    private async Task<string> GdReadChangesAsync(bool fromNow)
    {
        var drive = GdClients().Drive;

        if (fromNow)
        {
            _gdDeltaToken = (await drive.DeltaAsync(fromNow: true)).DeltaToken;
            _gdChanges.Clear();
            _gdChangesSummary.Value = "Watching from now on: change something in Drive, then Check for changes";
            return "PASS started from now";
        }

        var first = _gdDeltaToken == null;
        DriveFileDelta delta;
        bool capped;
        var more = "";

        try
        {
            (delta, capped) = await GdReadDeltaAsync(drive, _gdDeltaToken);
        }
        catch (ConnectorException ex) when (ex.IsResyncRequired)
        {
            (delta, capped) = await GdReadDeltaAsync(drive, null);
            first = true;
            more = " — the stored token had expired, so this read the whole Drive again";
        }

        if (capped)
        {
            more += " — more pages to read; check again to continue";
        }

        _gdDeltaToken = delta.DeltaToken;
        _gdChanges.ReplaceAll(delta.Items);
        _gdChangesSummary.Value = first
            ? $"Read the whole Drive: {delta.Items.Count} files{more}. Change something and check again."
            : $"{delta.Items.Count} changed since the last check ({delta.Items.Count(f => f.Deleted)} gone, {delta.Items.Count(f => f.Trashed)} in the bin){more}";

        return $"PASS {(first ? "read the whole Drive" : "read the changes")}: {delta.Items.Count} files";
    }

    // A large Drive takes more pages than one check reads; the capped read's ResumeFrom is a token
    // like any other, so the next check continues from it instead of starting over.
    private static async Task<(DriveFileDelta Delta, bool Capped)> GdReadDeltaAsync(Drive drive, string? deltaToken)
    {
        try
        {
            return (await drive.DeltaAsync(deltaToken, maxPages: GdChangePages), false);
        }
        catch (ConnectorPageCapException<DriveFile> cap) when (cap.ResumeFrom is not null)
        {
            return (new DriveFileDelta(cap.Items, cap.ResumeFrom), true);
        }
    }

    private async Task<string> GdWatchChangesAsync()
    {
        var (_, drive, watches) = GdClients();

        // A channel on the changes feed starts from a change token, not from a whole-Drive read.
        if (_gdDeltaToken == null || _gdDeltaToken.StartsWith("list:", StringComparison.Ordinal))
        {
            _gdDeltaToken = (await drive.DeltaAsync(fromNow: true)).DeltaToken;
        }

        var channel = await watches.CreateAsync(WatchTarget.DriveChanges(_gdDeltaToken), GdNotificationUrl()!, _gdChannelToken, GdChannelLifetime);
        _gdChannels.Add(channel);
        return $"PASS watching Drive changes until {channel.ExpiresAt:HH:mm} UTC; Google sends a sync notification first";
    }

    private async Task<string> GdWatchFileAsync()
    {
        var file = _gdActiveItem.Value!;
        var channel = await GdClients().Watches.CreateAsync(WatchTarget.DriveFile(file.Id), GdNotificationUrl()!, _gdChannelToken, GdChannelLifetime);
        _gdChannels.Add(channel);
        return $"PASS watching {file.Name} until {channel.ExpiresAt:HH:mm} UTC";
    }

    private async Task<string> GdStopChannelsAsync()
    {
        var watches = GdClients().Watches;
        var count = _gdChannels.Count;

        foreach (var channel in _gdChannels.ToList())
        {
            try
            {
                await watches.StopAsync(channel);
            }
            catch (ConnectorException ex) when (ex.StatusCode == 404)
            {
                // Already expired on Google's side; there is nothing left to stop.
            }
        }

        _gdChannels.Clear();
        return $"PASS stopped {count} channels";
    }

    private async Task GdReadAfterNotificationAsync()
    {
        try
        {
            if (_gdClients is not { } clients || _gdDeltaToken == null)
            {
                return;
            }

            var (delta, _) = await GdReadDeltaAsync(clients.Drive, _gdDeltaToken);
            _gdDeltaToken = delta.DeltaToken;
            GdLogNotification($"Drive changes: read {delta.Items.Count} — {string.Join(", ", delta.Items.Take(5).Select(f => $"{GdChangeKind(f)} {(f.Deleted ? f.Id : f.Name)}"))}");
        }
        catch (Exception ex)
        {
            GdLogNotification($"Drive changes: read failed: {ex.Message}");
        }
    }

    private void GdLogNotification(string line)
    {
        _gdNotificationLog.Insert(0, $"{DateTime.UtcNow:HH:mm:ss} {line}");

        while (_gdNotificationLog.Count > GdNotificationLogSize)
        {
            _gdNotificationLog.RemoveAt(_gdNotificationLog.Count - 1);
        }
    }

    private static string GdChangeKind(DriveFile file)
    {
        return file.Deleted ? "gone" : file.Trashed ? "in the bin" : file.IsFolder ? "folder" : "changed";
    }
}
