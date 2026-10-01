public partial class Validation
{
    private readonly ClientReactive<string> _notificationPermission = new("unknown");
    private readonly Reactive<string> _offlinePushLog = new("(none scheduled)");

    private void RenderNotificationsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Notifications");

            RenderNotificationSendCard(view);
            RenderNotificationInboxCard(view);
            RenderNotificationPolicyCard(view);
            RenderNotificationChannelsCard(view);

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Offline push");
                view.Row([Layout.Row.Md, "flex-wrap mb-3"], content: view =>
                {
                    view.Button([Button.PrimaryMd], text: "Push to my user in 10s", icon: "clock",
                        onClick: async () => ScheduleOfflinePush(10));
                    view.Button([Button.OutlineMd], text: "In 30s", onClick: async () => ScheduleOfflinePush(30));
                    view.Button([Button.OutlineMd], text: "In 60s", onClick: async () => ScheduleOfflinePush(60));
                });
                view.Text([Text.Body], _offlinePushLog.Value, props: TestId("notif-offline-result"));
            });
        });
    }

    private void ScheduleOfflinePush(int delaySeconds)
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _offlinePushLog.Value = "Error: no user id in this session";
            return;
        }

        _offlinePushLog.Value = $"Fires in {delaySeconds}s — close this tab to receive it as an OS push";

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                var results = await app.Notifications.SendToUserAsync(
                    userId,
                    new NotificationContent("Validation", $"Offline push, scheduled {delaySeconds}s ago", LaunchUrl: "/notifications"));

                _offlinePushLog.Value = results.Count == 0
                    ? "Fired with no session connected: sent as offline push"
                    : $"Fired with {results.Count} session(s) connected: shown in the foreground";
            }
            catch (Exception ex)
            {
                _offlinePushLog.Value = $"Error: {ex.Message}";
            }
        });
    }
}
