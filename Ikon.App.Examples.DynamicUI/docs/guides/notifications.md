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

`NotificationContent` is a record — `new NotificationContent(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null)`. `Title` is required; `Tag` is a collapse key (a later notification with the same tag replaces the earlier one); `LaunchUrl` is the in-app path to open on tap; `Data` is opaque JSON returned to the app on tap.

### Online and offline delivery

You address by **session** (`int`) or **user** (`string`); offline push is transparent — the same call covers both.

- **Foreground (connected):** the notification shows immediately over the live connection.
- **Offline push:** `SendToUserAsync` fans out to the user's connected sessions; only when the user has **zero** connected sessions does it fall back to an OS push notification through the Ikon backend push hub — so it lands even if the app is closed (but a user connected on one device gets nothing on their other, offline devices). You write the same `SendToUserAsync` call either way.

### Permission is lazy (important)

Permission is requested **on the first actual send**, never when the app opens — a deliberate product rule. So just send when you have something to say; the platform handles the prompt. Inspect the result rather than gating on permission yourself:

- `NotificationSendResult.Delivered` — `true` only when the notification was actually shown.
- `NotificationSendResult.Permission` / `GetPermissionAsync` → `NotificationPermission.Granted` / `Denied` / `Default` (asked, still pending — e.g. Safari/iOS waits for the next user gesture) / `Unsupported` (the client can't show notifications).

### Tap handling

Set `LaunchUrl` (an in-app path) and/or `Data` (opaque JSON) on the content. When the user taps, the client focuses the app and surfaces them so the host shell can route to `LaunchUrl` / act on `Data`.
