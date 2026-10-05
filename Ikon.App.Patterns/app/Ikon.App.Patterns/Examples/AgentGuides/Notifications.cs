namespace Ikon.App.Patterns.Examples;

// The inbox is a field on the app class and its channels are wired from Main, so the holder is that
// app class — and it declares the same field twice under two names because the throttle example
// shows the same line with initializers on it.
file sealed class NotificationsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record Profile(string Email = "", string Phone = "", string TelegramChatId = "");

    private sealed record Order(string Id, string CustomerUserId);

    private readonly PersistentUserReactive<Profile> _profiles = new(new Profile());

    #region example:notification-inbox-field
    private readonly NotificationInbox _inbox = new(app);
    #endregion

    private async Task WireAsync(string botToken, string accessToken, string phoneNumberId, Order order)
    {
        #region example:notification-inbox-channels
        // In Main(): the platform does not know users' addresses, so each channel takes a resolver.
        _inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => _profiles.ValueFor(userId).Email));
        _inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => _profiles.ValueFor(userId).Phone));
        _inbox.Channels.Add(new TelegramNotificationChannel(botToken, userId => _profiles.ValueFor(userId).TelegramChatId));
        // WhatsApp delivers free text only within 24 h of the user's last message; with templateName set,
        // every notification goes as that approved Meta template (one body parameter), which reaches everyone.
        _inbox.Channels.Add(new WhatsAppNotificationChannel(accessToken, phoneNumberId, userId => _profiles.ValueFor(userId).Phone, templateName: "order_update"));

        // One call. The route says where it goes.
        var outcome = await _inbox.NotifyAsync(order.CustomerUserId,
            new NotificationContent("Order delivered", "Enjoy your meal", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
            kind: "order",
            route: NotificationRoute.Everywhere("email"));          // inbox + every device + email
        #endregion

        Log.Instance.Debug($"{outcome}");
    }
}

file sealed class NotificationThrottleExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:notification-inbox-throttle
    private readonly NotificationInbox _inbox = new(app) { MaxPushPerWindow = 5, PushWindow = TimeSpan.FromMinutes(10) };
    #endregion

    public void Use() => Log.Instance.Debug($"{_inbox}");
}

file sealed class InboxUiExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly NotificationInbox _inbox = new(app);

    private static Task NavigateAsync(string? launchUrl) => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region example:notification-inbox-ui
        view.Badge($"{_inbox.UnreadCount}");              // signed-in user
        foreach (var item in _inbox.Items)                // newest first
        {
            view.Box([Card.Default, "p-3 mb-2"], onClick: async () => { _inbox.MarkRead(item.Id); await NavigateAsync(item.LaunchUrl); });
        }
        #endregion
    }
}

internal sealed partial class AgentGuideExamples
{

    private async Task DocNotifyPriorityAsync(string userId)
    {
        #region example:notify-priority
        await _inbox.NotifyAsync(userId, new NotificationContent("Payment failed", "Tap to fix", Priority: NotificationPriority.High), kind: "payment");
        #endregion
    }

    private void DocNotifyQuietHours(string userId)
    {
        #region example:notify-quiet-hours
        _inbox.SetQuietHoursFor(userId, new TimeOnly(21, 0), new TimeOnly(6, 0));   // 21:00–06:00 UTC
        // signed-in form: SetQuietHours(...) / QuietHours; read QuietHoursFor(userId); clear with ClearQuietHoursFor(userId)
        #endregion
    }

    private async Task DocNotifyAllDevicesAsync(string userId, DocOrder order)
    {
        #region example:notify-all-devices
        await app.Notifications.SendToUserAsync(userId,
            new NotificationContent("Order delivered", "Enjoy!", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
            NotificationReach.AllDevices);
        #endregion
    }

    private async Task DocNotifyActionsAsync(string userId)
    {
        #region example:notify-actions
        await app.Notifications.SendToUserAsync(userId, new NotificationContent(
            "Ride arriving", "Petri is 2 min away",
            LaunchUrl: "/trip/847",
            Actions: [new NotificationAction("track", "Track", "/trip/847"),
                      new NotificationAction("cancel", "Cancel ride", "/trip/847/cancel")]));
        #endregion
    }

    private async Task DocNotificationSendingAsync(int sessionId, string userId)
    {
        #region example:notification-sending
        // One connected session — sessionId is an int (e.g. ReactiveScope.ClientId inside a UI / onClick handler).
        NotificationSendResult r = await app.Notifications.SendToSessionAsync(
            sessionId, new NotificationContent("Build finished", "Your app deployed successfully."));

        // A user's connected sessions — userId is a string. Falls back to offline push when the user has NO connected session (see below).
        await app.Notifications.SendToUserAsync(userId, new NotificationContent("New message", "Alice replied"));

        // Everyone currently connected.
        await app.Notifications.BroadcastAsync(new NotificationContent("Maintenance in 5 min"));

        // Read permission state without sending.
        NotificationPermission p = await app.Notifications.GetPermissionAsync(sessionId);
        #endregion

        Log.Instance.Debug($"{r} {p}");
    }
}
