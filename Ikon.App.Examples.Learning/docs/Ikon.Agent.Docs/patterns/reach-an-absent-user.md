<!-- mined-from: Ikon.App.Patterns -->
# Reaching A User Who Has Left — Permission States And The Email Fallback

Two failure modes look like success here. `SendToUserAsync` returns an **empty list** when the user
had no connected session — that is not an error, it means only offline push was attempted, and
treating it as failure double-sends. And a notification whose permission was denied delivers
nothing while the call itself succeeds.

So the branch that matters is on `NotificationSendResult.Permission`, and the fallback is another
channel rather than a retry.

## When to use

Anything that must reach someone who is not looking at the app: a finished long job, an approval
request, a receipt, an alert.

## Notes

- **Permission is requested lazily on the first actual send**, not when the app opens. So
  `NotificationPermission.Default` means *never asked yet*, and the send you just made is what
  asked.
- `Denied` is a choice the user can change; `Unsupported` is a browser with no such feature.
  Neither is worth retrying, and both mean a different channel.
- **Give the content a `Tag`.** A device that is both connected and pushed otherwise shows the same
  thing twice; a later notification with the same tag replaces the earlier one instead of stacking.
- `NotificationContent`'s `Title` is a **required positional** argument; everything else is
  optional and named.
- **`using Ikon.Common.Core.Email;` is required for `EmailSendRequest`.** `GlobalUsings.cs` does not
  cover it, and the API reference documents `EmailService.SendAsync(EmailSendRequest)` without
  declaring that type anywhere — so its fields are not discoverable from the reference alone.
- **A named sender needs a verified sending domain.** Without one the send throws
  `EmailSenderNotAvailableException` rather than quietly rewriting the from-address; the fallback
  is to resend with the sender fields cleared and deliver from the platform's own address.
- Set `TextBody` as well as `HtmlBody`. Some clients show nothing without it.
- `NotificationPriority.Low` is recorded in the inbox with no device push or channel send — the
  right level for something worth having a record of but not worth interrupting for.

## Snippet

```csharp
/// <summary>
/// SendToUserAsync already falls back to offline OS push when the user has no connected
/// session, so an EMPTY result list is not an error -- it means nobody was connected and only
/// push was attempted. Treating it as failure double-sends.
/// </summary>
private async Task NotifyAsync(string userId, string title, string body)
{
    // Title is a REQUIRED positional argument; the rest are optional named ones.
    // A Tag is what stops a device that is BOTH connected and pushed showing the same thing
    // twice -- a later notification with the same tag replaces the earlier one.
    var results = await App.Notifications.SendToUserAsync(
        userId, new NotificationContent(title, Body: body, Tag: "invoice-ready"));

    // Permission is requested lazily on the first actual SEND, not when the app opens -- so
    // Default here means "never asked yet", and this send is what asked.
    foreach (var result in results)
    {
        if (result.Permission is NotificationPermission.Denied or NotificationPermission.Unsupported)
        {
            // Denied is a choice the user can change; Unsupported is a browser that has no
            // such feature. Neither is worth retrying, and both mean another channel.
            await EmailFallbackAsync(userId, title, body);
            return;
        }
    }
}

/// <summary>
/// A named sender needs a VERIFIED sending domain. Without one the send throws rather than
/// silently rewriting the from-address, so the fallback is to resend with no sender fields
/// and deliver from the platform's own address.
/// </summary>
private async Task EmailFallbackAsync(string userId, string subject, string body)
{
    EmailService email = App.Email;
    var request = new EmailSendRequest(
        To: userId,
        Subject: subject,
        HtmlBody: $"<p>{body}</p>",
        // A text body is not optional in practice: some clients show nothing without it.
        TextBody: body,
        SenderDisplayName: "Acme Billing",
        SenderDomain: "acme.example");

    try
    {
        await email.SendAsync(request);
    }
    catch (EmailSenderNotAvailableException)
    {
        await email.SendAsync(request with { SenderDisplayName = null, SenderDomain = null });
    }
}
```

## See also

- `notify-across-channels` — inbox, push, email and SMS from one route, with quiet hours.
