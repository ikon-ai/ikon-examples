# Notify Across Channels — One Route Decides Where It Goes

Every app that tells someone something eventually needs the same four decisions: does it go in an in-app inbox, does a device buzz, does it also reach them by email or SMS, and does it respect the fact that it is 3am. `NotificationInbox` makes those one object — a `NotificationRoute` — instead of four scattered call sites.

`app.Notifications` (a `NotificationService`) is the push half on its own: `SendToSessionAsync`, `SendToUserAsync`, `BroadcastAsync`, each returning a `NotificationSendResult`. The inbox wraps it and adds persistence, per-user preferences and the extra channels.

## When to use

Anything asynchronous the user should learn about: an approval, a finished job, a mention, a threshold crossed. Not for transient in-app confirmation — that is a `toast-notifications` job and needs no delivery decision at all.

## Snippet

```csharp
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
```

## Notes

- **The route is the decision, not the content.** `NotificationRoute.Silent` records without buzzing, `Default` is inbox plus connected devices, `AllDevices` reaches every registered one, and `Everywhere("email", "sms")` adds channels. Writing the same `NotificationContent` and varying the route keeps "what it says" and "how hard it knocks" separate.
- `NotificationReach.ConnectedFirst` deliberately does **not** buzz a phone when the person is reading on a laptop. `AllDevices` does — set `NotificationContent.Tag` so the connected device collapses its foreground and push copies into one.
- **`NotificationPriority.High` bypasses quiet hours and the frequency cap**, which is exactly why it must stay rare. `Normal` respects both; `Low` is ambient — inbox only, nothing buzzes. An explicit channel mute wins over all three.
- Register channels once in `OnStarting`. The platform does not hand apps its users' email addresses or phone numbers, so `EmailNotificationChannel` and `SmsNotificationChannel` each take a resolver into the app's own profile state. `TelegramNotificationChannel` and `WhatsAppNotificationChannel` ship too, and `INotificationChannel` is one interface with two members if you need your own.
- **Read `NotificationOutcome` carefully.** `Skipped` is not failure — no address on file, channel unconfigured, or the user muted it. `Failed` is. The inbox item stands either way, which is the point: the record survives a delivery that didn't.
- Preferences belong to the user, not the app: `SetQuietHoursFor` stores a `QuietHours` per user, `MuteFor` a per-user channel mute. The un-suffixed `SetQuietHours` / `Mute` act on the current user scope.
- `MaxPushPerWindow` and `PushWindow` are the frequency cap and are **init-only** — set them in the object initializer on the field, not later.
- Permission is requested lazily on the first real send, not at app open. Don't build a "enable notifications?" screen; send something worth receiving and let the browser ask.
- Every `InboxItem` should carry a `LaunchUrl` into the thing that happened. A notification you cannot act on is noise with a timestamp.

## See also

- `toast-notifications` — the transient in-app confirmation that needs no delivery decision.
- `zero-results-state` — what the bell shows before anything has happened.
- `persistent-user-preferences` — where the per-user addresses these channels resolve should live.
