// EmailSendRequest lives in Ikon.Common.Core.Email — a nested namespace GlobalUsings does not
// cover, and one the API reference never declares the type under either.
using Ikon.Common.Core.Email;

namespace Ikon.App.Patterns.Patterns;

// Pattern: reach-an-absent-user — see docs/patterns/reach-an-absent-user.md.
// The example region below is the canonical body the doc extracts.
internal sealed class ReachAnAbsentUser : IPatternDemo
{
    public string Slug => "reach-an-absent-user";
    public string Title => "Reaching a user who has left";
    public string Category => "Status & feedback";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Server-side pattern with no UI: notification permission states and the email fallback "
        + "for a user who is not there. See the source and docs/patterns/reach-an-absent-user.md.");

    private IAppBase App => throw new NotImplementedException();

    #region example:pattern-reach-an-absent-user
    /// <summary>
    /// SendToUserAsync already falls back to offline OS push when the user has no connected
    /// session, so the list is never empty: it holds one row per connected session, or a single
    /// OfflinePush row when nobody was connected. Which channel a row came from decides which of
    /// its fields carries the outcome.
    /// </summary>
    private async Task NotifyAsync(string userId, string email, string title, string body)
    {
        // Title is a REQUIRED positional argument; the rest are optional named ones.
        // A Tag is what stops a device that is BOTH connected and pushed showing the same thing
        // twice -- a later notification with the same tag replaces the earlier one.
        var results = await App.Notifications.SendToUserAsync(
            userId, new NotificationContent(title, Body: body, Tag: "invoice-ready"));

        foreach (var result in results)
        {
            if (result.Channel == NotificationSendChannel.OfflinePush)
            {
                // The push row has no client to ask, so its Permission is always Default and says
                // nothing. Delivered is whether the push hub took it, and Error is why not.
                if (!result.Delivered)
                {
                    await EmailFallbackAsync(email, title, body);
                    return;
                }

                continue;
            }

            // On a session row, permission is requested lazily on the first actual SEND, not when
            // the app opens -- so Default means this send asked and got no answer (dismissed, or
            // queued until the user's next gesture), and nothing was shown.
            if (result.Permission is NotificationPermission.Default or NotificationPermission.Denied or NotificationPermission.Unsupported)
            {
                // Denied is a choice the user can change; Unsupported is a client with no such
                // feature, or a session whose show call threw or failed. All mean another channel.
                await EmailFallbackAsync(email, title, body);
                return;
            }
        }
    }

    /// <summary>
    /// A named sender needs a VERIFIED sending domain. Without one the send throws rather than
    /// silently rewriting the from-address, so the fallback is to resend with no sender fields
    /// and deliver from the platform's own address.
    /// </summary>
    private async Task EmailFallbackAsync(string email, string subject, string body)
    {
        EmailService emailService = App.Email;
        // To is an email address -- the backend rejects anything else, so an Ikon user id here
        // fails the send. Keep the address from the user's profile alongside their id.
        var request = new EmailSendRequest(
            To: email,
            Subject: subject,
            HtmlBody: $"<p>{body}</p>",
            // A text body is not optional in practice: some clients show nothing without it.
            TextBody: body,
            SenderDisplayName: "Acme Billing",
            SenderDomain: "acme.example");

        try
        {
            await emailService.SendAsync(request);
        }
        catch (EmailSenderNotAvailableException)
        {
            await emailService.SendAsync(request with { SenderDisplayName = null, SenderDomain = null });
        }
    }
    #endregion
}
