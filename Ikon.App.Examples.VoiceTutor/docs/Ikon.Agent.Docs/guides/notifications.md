# Notifications

## Notifications

Show a user-facing notification — a browser notification on the web, an OS notification in a Flutter native app — through **`app.Notifications`** (type `NotificationService`). Notifications are **server-initiated**: the app decides when to send. There is no UI component or `ActionButton` for it.

### Sending

```csharp
// One connected session — sessionId is an int (e.g. ReactiveScope.ClientId inside a UI / onClick handler).
NotificationSendResult r = await app.Notifications.SendToSessionAsync(
    sessionId, new NotificationContent("Build finished", "Your app deployed successfully."));

// A user's connected sessions — userId is a string. Falls back to offline push when the user has NO connected session (see below).
await app.Notifications.SendToUserAsync(userId, new NotificationContent("New message", "Alice replied"));

// Everyone currently connected.
await app.Notifications.BroadcastAsync(new NotificationContent("Maintenance in 5 min"));

// Read permission state without sending.
NotificationPermission p = await app.Notifications.GetPermissionAsync(sessionId);
```

`NotificationContent` is a record — `new NotificationContent(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null, NotificationPriority Priority = NotificationPriority.Normal, IReadOnlyList<NotificationAction>? Actions = null)`. `Title` is required; `Tag` is a collapse key (a later notification with the same tag replaces the earlier one); `LaunchUrl` is the in-app path to open on tap; `Data` is opaque JSON returned to the app on tap; `Priority` (`Low`/`Normal`/`High`) is honoured by `NotificationInbox` (below).

Every field is yours to build at runtime — there is no template system or fixed catalogue of messages. Compose `Title`/`Body` per user (`$"Hi {name}, order {id} is on its way"`), attach app-specific `Data` (opaque JSON your app reads back on tap), and point `LaunchUrl` at the exact screen to open. Target one person with `SendToUserAsync(userId, …)` (or `NotificationInbox.NotifyAsync(userId, …)`), one device with `SendToSessionAsync`, or everyone connected with `BroadcastAsync`.

### Online and offline delivery

You address by **session** (`int`) or **user** (`string`); offline push is transparent — the same call covers both.

- **Foreground (connected):** the notification shows immediately over the live connection.
- **Offline push:** `SendToUserAsync` fans out to the user's connected sessions; only when the user has **zero** connected sessions does it fall back to an OS push notification through the Ikon backend push hub — so it lands even if the app is closed (but a user connected on one device gets nothing on their other, offline devices). You write the same `SendToUserAsync` call either way.

### Permission is lazy (important)

Permission is requested **on the first actual send**, never when the app opens — a deliberate product rule. So just send when you have something to say; the platform handles the prompt. Inspect the result rather than gating on permission yourself:

- `NotificationSendResult.Delivered` — `true` only when the notification was actually shown.
- `NotificationSendResult.Permission` / `GetPermissionAsync` → `NotificationPermission.Granted` / `Denied` / `Default` (asked, still pending — e.g. Safari/iOS waits for the next user gesture) / `Unsupported` (the client can't show notifications).

### Registering devices — the platform does it for you

You never handle push tokens, FCM keys, web-push subscriptions or device ids, and there is no "register for push" call to make. Device registration is a side effect of a granted send: the first time you notify a *connected* client and the user grants permission, the platform captures that device's push subscription and registers it with the backend push hub under the user's id, in the background — best-effort, so it never blocks or fails your send. From then on that device is reachable by offline push even with the app closed.

What that means in practice:

- **Address people by `userId`, never by device.** One user has many devices (a laptop, a phone, a second browser); the platform tracks which are registered and fans out. You keep a `userId`, not a token list — `SendToUserAsync(..., NotificationReach.AllDevices)` and `NotificationInbox` routes with `AllDevices`/`Everywhere` reach them all.
- **Registration follows permission, per device.** A device registers only once the user grants permission on it, which happens on the first send to that connected session. Denied → nothing registers there; that is expected, not an error.
- **An "enable notifications" toggle is just a first send.** There is no separate subscribe API. To let a user opt in explicitly, send a first notification when they switch it on — `await app.Notifications.SendToSessionAsync(sessionId, new NotificationContent("Notifications on", "You'll hear from us here."))` — which prompts permission and, on grant, registers the device. Reflect `NotificationSendResult.Permission` back in the toggle.
- **Stale devices drop themselves.** A subscription the browser or OS has invalidated is removed by the push hub on its next failed delivery; you never prune device lists.

### Where registrations and the inbox live

Two different things, two different homes — and only one is in your app:

- **Device registrations (push subscriptions) live in the platform backend, not your app.** On grant, the client's subscription — a web-push endpoint + keys, or the FCM token, plus platform and device id — is sent to the Ikon backend keyed by `userId`, and offline delivery is the backend's job. Your app stores **no** tokens and keeps **no** device list, which is why there is nothing to register, migrate or prune.
- **The in-app inbox lives in your app's persistent state.** `NotificationInbox` keeps its items in a `PersistentUserReactiveList<InboxItem>` (and mutes in another), per user, in the app's configured persistence backend — so the message history, unread count and per-channel mutes are yours, durable across restarts, and read/render like any other reactive state.

### Timing and scheduling

The send itself is always immediate — `SendToUserAsync` / `NotifyAsync` fire now. *When* to fire is ordinary app code:

- **Soon, while the app is running** — a reminder a few minutes out, a follow-up after an action: `await Task.Delay(…)` in a background task (or a `PeriodicTimer` loop), then send. This lives only as long as the app instance; an app with no connected clients idles out, so do not lean on a long `Task.Delay` for something that must fire hours later.
- **Recurring or at a set time, durably** — a morning digest, a weekly nudge, "notify everyone with an unpaid order at 18:00": use a **Scheduled pipeline** (`[Pipeline(executionMode: PipelineExecutionMode.Scheduled, schedule: "0 8 * * *")]`), which the platform runs on cron even when nobody is connected (minimum interval 5 minutes). The pipeline decides who needs notifying and calls `SendToUserAsync` / `NotifyAsync` for each — see the Pipelines guide.

Give a scheduled or repeated send a stable `Tag` so a re-run updates one notification (and one inbox item) instead of stacking duplicates.

### Tap handling

Set `LaunchUrl` (an in-app path) and/or `Data` (opaque JSON) on the content. When the user taps, the client focuses the app and surfaces them so the host shell can route to `LaunchUrl` / act on `Data`.

### Every device of one user

`SendToUserAsync` stops at the connected sessions by default — a user reading the app on a laptop gets no buzz on their phone. Pass `NotificationReach.AllDevices` to reach every registered device in one call: connected sessions get the foreground notification and the push hub delivers to the others. Give the content a `Tag` so the connected device collapses its foreground and push copies into one.

```csharp
await app.Notifications.SendToUserAsync(userId,
    new NotificationContent("Order delivered", "Enjoy!", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
    NotificationReach.AllDevices);
```

### One system for inbox, push, email, SMS, Telegram and WhatsApp

`NotificationInbox` is the one call that reaches a user everywhere: it keeps a **persistent per-user in-app inbox** (a `PersistentUserReactiveList<InboxItem>` — unread count, mark read, survives restarts), shows the notification on the user's devices through `app.Notifications` (web push, iOS and Android from the Flutter app), and fans out to any registered `INotificationChannel`. Declare it as a field and register the channels the app can address:

```csharp
private readonly NotificationInbox _inbox = new(app);
```

Declare the field, and then in Main():

```csharp
// In Main(): the platform does not know users' addresses, so each channel takes a resolver.
_inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => _profiles.ValueFor(userId).Email));
_inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => _profiles.ValueFor(userId).Phone));
_inbox.Channels.Add(new TelegramNotificationChannel(botToken, userId => _profiles.ValueFor(userId).TelegramChatId));
_inbox.Channels.Add(new WhatsAppNotificationChannel(accessToken, phoneNumberId, userId => _profiles.ValueFor(userId).Phone));

// One call. The route says where it goes.
var outcome = await _inbox.NotifyAsync(order.CustomerUserId,
    new NotificationContent("Order delivered", "Enjoy your meal", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
    kind: "order",
    route: NotificationRoute.Everywhere("email"));          // inbox + every device + email
```

Routes: `NotificationRoute.Default` (inbox + push on connected devices), `.AllDevices` (inbox + push everywhere), `.Silent` (inbox only), `.Everywhere("email", "sms")` (inbox + every device + the named channels), or `Default.With("email")`. A channel that is unconfigured (empty token) or has no address for the user is *skipped*, one that throws is *failed* and logged — the inbox item is the durable record either way, and `NotificationOutcome` lists `Delivered`, `Skipped` and `Failed` by channel name. Pass `Tag` to collapse updates of the same thing (an order's status line) into one inbox item as well as one OS notification.

The inbox in the UI — reads are tracked, so the badge and list re-render on change:

```csharp
view.Badge($"{_inbox.UnreadCount}");              // signed-in user
foreach (var item in _inbox.Items)                // newest first
{
    view.Box([Card.Default, "p-3 mb-2"], onClick: async () => { _inbox.MarkRead(item.Id); await NavigateAsync(item.LaunchUrl); });
}
```

Users opt out per channel with `_inbox.Mute("email")` / `Mute("push")` (`IsMuted`, `Muted`, `MuteFor(userId, …)` from a background task); muted channels show up as skipped. `ClearFor(userId)` empties a user's inbox and mutes — call it from the account-deletion path. Implement `INotificationChannel` (a name and `SendAsync` returning whether it had somewhere to send) for anything else — a webhook, Slack, a second app.

### Priority, quiet hours and frequency

`NotificationInbox` applies a small delivery policy so you don't hand-roll notification etiquette:

- **Priority** — `NotificationContent.Priority`: `Low` is *ambient* (inbox only, nothing buzzes); `Normal` (default) pushes subject to quiet hours and the frequency cap; `High` is *urgent* and bypasses both (an explicit channel mute still wins). On the device the priority also shapes presentation: `High` pops a heads-up and stays prominent (Android importance + iOS interruption level, `requireInteraction` on web), `Low` arrives silently.
```csharp
await _inbox.NotifyAsync(userId, new NotificationContent("Payment failed", "Tap to fix", Priority: NotificationPriority.High), kind: "payment");
```
- **Quiet hours (do-not-disturb)** — a per-user UTC window in which `Normal`/`Low` stay in the inbox but do not push; `High` still gets through. Convert from the user's local time when you set it (the window may wrap past midnight):
```csharp
_inbox.SetQuietHoursFor(userId, new TimeOnly(21, 0), new TimeOnly(6, 0));   // 21:00–06:00 UTC
// signed-in form: SetQuietHours(...) / QuietHours; read QuietHoursFor(userId); clear with ClearQuietHoursFor(userId)
```
- **Frequency cap** — set `MaxPushPerWindow` (over `PushWindow`, 10 min by default) so a burst can't spam a user's devices; the excess is still recorded in the inbox, only the buzz is dropped, and `High` ignores the cap:
```csharp
private readonly NotificationInbox _inbox = new(app) { MaxPushPerWindow = 5, PushWindow = TimeSpan.FromMinutes(10) };
```

A push held back by priority, quiet hours, a mute or the cap shows in `NotificationOutcome.Skipped` (not `Delivered`); the inbox item is the durable record either way. **Push is a nudge, the inbox is the record** — buzz sparingly and let the inbox hold the scrollable history. Localization is still app-side (build the content with your own i18n).

### Action buttons

Attach `Actions` to give a notification tappable buttons — an id, a label, and the in-app path to open when that button is tapped:

```csharp
await app.Notifications.SendToUserAsync(userId, new NotificationContent(
    "Ride arriving", "Petri is 2 min away",
    LaunchUrl: "/trip/847",
    Actions: [new NotificationAction("track", "Track", "/trip/847"),
              new NotificationAction("cancel", "Cancel ride", "/trip/847/cancel")]));
```

A tap on a button (or the body) routes exactly like `LaunchUrl`: the client opens the action's path, falling back to the notification's own `LaunchUrl` for a body tap. On web, buttons render only through the bundled `push-service-worker.js` (already wired in the frontend template); a body tap still works without it. On Flutter the tap surfaces through the SDK's `ikonNotificationTaps` stream carrying the launch path and the tapped `action` id; iOS groups a given set of buttons into a category the plugin registers on first use. Keep the button set small and stable — two or three actions, reused across sends.
