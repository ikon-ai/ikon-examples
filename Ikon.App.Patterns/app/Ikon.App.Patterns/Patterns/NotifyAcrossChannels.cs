namespace Ikon.App.Patterns.Patterns;

// Pattern: notify-across-channels — see docs/patterns/notify-across-channels.md.
// The docsnippet region is the whole delivery decision: one inbox, one route per notification, and
// the render for the bell. The inbox itself is a null! placeholder here — constructing one reaches
// app.Notifications, which a host with no notification surface does not have.
internal sealed class NotifyAcrossChannels(IAppBase app) : IPatternDemo
{
    public string Slug => "notify-across-channels";
    public string Title => "Notify across channels";
    public string Category => "Feedback";

    public void RenderDemo(IView view) => RenderBell(view, DemoItems, 2);

    private readonly NotificationInbox _inbox = null!;

    private static readonly IReadOnlyList<InboxItem> DemoItems =
    [
        new InboxItem("1", "Marcus approved your invoice", "INV-2291 · €1,240", "approval",
            "/invoices/2291", null, "invoice", DateTime.UtcNow.AddMinutes(-4), false),
        new InboxItem("2", "Two receipts need a category", null, "review",
            "/receipts", null, "receipts", DateTime.UtcNow.AddHours(-3), false),
        new InboxItem("3", "Weekly summary is ready", "8 approvals, 2 rejections", "digest",
            "/summary", null, "digest", DateTime.UtcNow.AddDays(-1), true),
    ];

    #region docsnippet:pattern-notify-across-channels
    /// Register channels ONCE, in OnStarting. The platform does not hand apps its users' email
    /// addresses or phone numbers, so each channel takes a resolver into the app's own profile state.
    private void Wire()
    {
        _inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => AddressOf(userId)));
        _inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => PhoneOf(userId)));

        // A channel is just INotificationChannel — TelegramNotificationChannel and
        // WhatsAppNotificationChannel ship, and your own is one interface with two members.
        //
        // The frequency cap is init-only, so it goes on the field itself:
        //   private readonly NotificationInbox _inbox = new(app) { MaxPushPerWindow = 3 };
    }

    /// One call decides everything: what it says, how loud it is, and where it goes.
    private async Task NotifyAsync(string userId, string title, string body, bool urgent)
    {
        var content = new NotificationContent(
            title,
            body,
            LaunchUrl: "/approvals",
            Tag: "approvals",
            Priority: urgent ? NotificationPriority.High : NotificationPriority.Normal,
            Actions: [new NotificationAction("approve", "Approve", "/approvals?act=approve")]);

        // The ROUTE is the decision, not the content. Silent records without buzzing; Default is
        // inbox plus the connected devices; Everywhere adds every registered device and the named
        // channels — the "they must see this" route, and the one to use sparingly.
        var route = urgent
            ? NotificationRoute.Everywhere("email", "sms")
            : NotificationRoute.Default;

        NotificationOutcome outcome = await _inbox.NotifyAsync(userId, content, kind: "approval", route: route);

        // Skipped is not failure: no address on file, channel unconfigured, or the user muted it.
        // Failed is. The item still stands in the inbox either way.
        if (outcome.Failed.Count > 0) { LogFailures(outcome.Failed); }
    }

    /// Preferences are per user and belong to the user, not the app. Quiet hours and mutes are
    /// honoured for Normal and Low; High bypasses both, which is why urgent must stay rare.
    private void SetPreferences(string userId, bool wantsEmail)
    {
        _inbox.SetQuietHoursFor(userId, new TimeOnly(22, 0), new TimeOnly(7, 0));
        _inbox.MuteFor(userId, "email", muted: !wantsEmail);
    }

    /// The bell. Unread count with sensible overflow, newest first, and every row a way into the
    /// thing that happened — a notification you cannot act on is just noise with a timestamp.
    private void RenderBell(IView view, IReadOnlyList<InboxItem> items, int unread)
    {
        view.Column([Card.Default, "w-80 p-2", Layout.Column.Xs], content: view =>
        {
            view.Row([Layout.Row.SpaceBetween, "px-2 py-1"], content: v =>
            {
                v.Row([Layout.Row.Xs], content: h =>
                {
                    h.Icon([Icon.Sm], name: "bell");
                    h.Text([Text.Caption], text: "Notifications");
                });

                if (unread > 0)
                {
                    v.Text([Badge.BrandSm], text: unread > 99 ? "99+" : $"{unread}");
                }
            });

            foreach (var item in items)
            {
                view.Box([item.Read ? Card.Ghost : Card.Subtle, "p-2 w-full text-left"],
                    onClick: async () => await OpenAsync(item.LaunchUrl),
                    content: v =>
                    {
                        v.Text([Text.Body, "text-sm", item.Read ? "text-tertiary" : "font-semibold"], text: item.Title);

                        if (item.Body is { } body) { v.Text([Text.Caption], text: body); }
                    });
            }
        });
    }
    #endregion

    private static string? AddressOf(string userId) => null;
    private static string? PhoneOf(string userId) => null;
    private static void LogFailures(IReadOnlyList<string> failed) { }
    private static Task OpenAsync(string? launchUrl) => Task.CompletedTask;
}
