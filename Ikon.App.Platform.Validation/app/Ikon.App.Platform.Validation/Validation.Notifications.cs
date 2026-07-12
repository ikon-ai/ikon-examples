public partial class Validation
{
    private readonly ClientReactive<string> _notificationPermission = new("(unknown — click Check)");
    private readonly ClientReactive<string> _notificationStatus = new("(nothing sent yet)");
    private readonly Reactive<bool> _notificationToastOpen = new(false);
    private readonly Reactive<string> _offlinePushLog = new("(no offline push scheduled yet)");

    private void RenderNotificationsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Overview
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Notifications");
                view.Text([Text.Caption, "mb-2"], "Server-initiated notifications via app.Notifications. Browser notifications on the web, OS notifications on Flutter native apps.");
                view.Text([Text.Caption], "Permission is requested lazily on the FIRST send — not when the app opens. On Safari/iOS the web SDK queues the notification and requests permission on your next interaction.");
            });

            // Permission state
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Permission");
                view.Text([Text.Caption, "mb-4"], $"Current: {_notificationPermission.Value}");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd], text: "Check permission", icon: "bell",
                        onClick: async () =>
                        {
                            var permission = await app.Notifications.GetPermissionAsync(ReactiveScope.ClientId);
                            _notificationPermission.Value = permission.ToString();
                        });
                });
            });

            // Foreground (implemented)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Foreground (implemented)");
                view.Text([Text.Caption, "mb-4"], "Shown while the client is connected. The first send triggers the permission prompt.");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd], text: "Notify this session", icon: "bell",
                        onClick: async () => await NotifyOneAsync(() => app.Notifications.SendToSessionAsync(
                            ReactiveScope.ClientId,
                            new NotificationContent("Validation", "Foreground notification to this session."))));

                    view.Button([Button.NeutralMd], text: "Broadcast to all sessions", icon: "bell-ring",
                        onClick: async () => await NotifyManyAsync(() => app.Notifications.BroadcastAsync(
                            new NotificationContent("Validation", "Broadcast to every connected session."))));

                    view.Button([Button.NeutralMd], text: "Notify my user (all my devices)", icon: "user",
                        onClick: async () =>
                        {
                            var userId = app.GlobalState.Clients.TryGetValue(ReactiveScope.ClientId, out var ctx) ? ctx.UserId : "";
                            if (string.IsNullOrEmpty(userId))
                            {
                                _notificationStatus.Value = "No user id — this session is anonymous.";
                                _notificationToastOpen.Value = true;
                                return;
                            }

                            await NotifyManyAsync(() => app.Notifications.SendToUserAsync(
                                userId, new NotificationContent("Validation", "Sent to every session of your user.")));
                        });
                });

                view.Row([Layout.Row.Md, "flex-wrap mt-3"], content: view =>
                {
                    view.Button([Button.OutlineMd], text: "Collapsing tag (send twice)", icon: "layers",
                        onClick: async () =>
                        {
                            await app.Notifications.SendToSessionAsync(ReactiveScope.ClientId,
                                new NotificationContent("Validation", "First — should be replaced.", Tag: "validation-demo"));
                            await NotifyOneAsync(() => app.Notifications.SendToSessionAsync(ReactiveScope.ClientId,
                                new NotificationContent("Validation", "Second — replaces the first (same tag).", Tag: "validation-demo")));
                        });

                    view.Button([Button.OutlineMd], text: "With launch URL + data (tap test)", icon: "external-link",
                        onClick: async () => await NotifyOneAsync(() => app.Notifications.SendToSessionAsync(
                            ReactiveScope.ClientId,
                            new NotificationContent(
                                "Validation",
                                "Tap me — the web SDK fires an 'ikon.notification-click' event.",
                                LaunchUrl: "/buttons",
                                Data: "{\"source\":\"validation\"}"))));
                });
            });

            // Offline push
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Offline push");
                view.Text([Text.Caption, "mb-2"], "Delivered by the OS through the backend push hub when you have NO connected session. SendToUserAsync fans out to your connected sessions and falls back to offline push when you're disconnected — the same API covers web push (live) and mobile FCM (once a Firebase project is configured).");
                view.Text([Text.Caption, "mb-4"], "Test it in two steps: grant permission so this device's push subscription registers (Step 1), then schedule a push and immediately close this tab (Step 2). The OS notification should arrive while the app is closed. Reopen to see the outcome below.");

                view.Text([Text.Label, "mb-2"], "Step 1 — Enable push on this device");
                view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                {
                    view.Button([Button.PrimaryMd], text: "Grant & register", icon: "bell-plus",
                        onClick: async () => await NotifyOneAsync(() => app.Notifications.SendToSessionAsync(
                            ReactiveScope.ClientId,
                            new NotificationContent("Validation", "Push enabled — this device is now registered for offline push."))));
                });

                view.Text([Text.Label, "mb-2"], "Step 2 — Schedule a push, then close this tab");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.OutlineMd], text: "Push in 10s", icon: "clock",
                        onClick: async () => await ScheduleOfflinePushAsync(10));

                    view.Button([Button.OutlineMd], text: "Push in 30s", icon: "clock",
                        onClick: async () => await ScheduleOfflinePushAsync(30));

                    view.Button([Button.OutlineMd], text: "Push in 60s", icon: "clock",
                        onClick: async () => await ScheduleOfflinePushAsync(60));
                });

                view.Text([Text.Caption, "mt-4"], $"Last offline push: {_offlinePushLog.Value}");
            });

            // Last result
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Last result");
                view.Text([Text.Caption], _notificationStatus.Value);
            });

            // Result toast
            view.Toast(
                viewportStyle: [Toast.ViewportBottomCenter],
                open: _notificationToastOpen.Value,
                onOpenChange: async open => _notificationToastOpen.Value = open,
                durationMs: 3000,
                toastStyle: [Toast.Base],
                title: "Notification",
                titleStyle: [Toast.Title],
                description: _notificationStatus.Value,
                descriptionStyle: [Toast.Description],
                showClose: true,
                closeStyle: [Toast.Close]);
        });
    }

    private Task ScheduleOfflinePushAsync(int delaySeconds)
    {
        var userId = app.GlobalState.Clients.TryGetValue(ReactiveScope.ClientId, out var ctx) ? ctx.UserId : "";

        if (string.IsNullOrEmpty(userId))
        {
            _notificationStatus.Value = "No user id — this session is anonymous; offline push needs a signed-in user.";
            _notificationToastOpen.Value = true;
            return Task.CompletedTask;
        }

        _offlinePushLog.Value = $"Scheduled in {delaySeconds}s — close this tab now to test the offline path.";
        _notificationStatus.Value = $"Push scheduled in {delaySeconds}s. Close this tab to receive it as an OS push.";
        _notificationToastOpen.Value = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                var results = await app.Notifications.SendToUserAsync(
                    userId,
                    new NotificationContent("Validation", $"Offline push test — scheduled {delaySeconds}s ago.", LaunchUrl: "/notifications"));

                _offlinePushLog.Value = results.Count == 0
                    ? "Fired with no connected session — delivered via OFFLINE push through the backend."
                    : $"Fired while {results.Count} session(s) still connected — delivered FOREGROUND (close the tab before it fires to test the offline path).";
            }
            catch (Exception ex)
            {
                _offlinePushLog.Value = $"Failed: {ex.Message}";
            }
        });

        return Task.CompletedTask;
    }

    private async Task NotifyOneAsync(Func<Task<NotificationSendResult>> send)
    {
        var result = await send();
        _notificationStatus.Value = $"session {result.SessionId}: {(result.Delivered ? "delivered" : "not delivered")} (permission: {result.Permission})";
        _notificationPermission.Value = result.Permission.ToString();
        _notificationToastOpen.Value = true;
    }

    private async Task NotifyManyAsync(Func<Task<IReadOnlyList<NotificationSendResult>>> send)
    {
        var results = await send();

        if (results.Count == 0)
        {
            _notificationStatus.Value = "No target sessions connected.";
        }
        else
        {
            var delivered = results.Count(r => r.Delivered);
            var permissions = string.Join(", ", results.Select(r => r.Permission.ToString()).Distinct());
            _notificationStatus.Value = $"{results.Count} session(s): {delivered} delivered (permissions: {permissions})";
        }

        _notificationToastOpen.Value = true;
    }
}
