// EmailSendRequest lives in Ikon.Common.Core.Email — a nested namespace GlobalUsings does not
// cover, and one the API reference never declares the type under either.
using Ikon.Common.Core.Email;

namespace Ikon.App.Patterns.Patterns;

// Pattern: reach-an-absent-user — see docs/patterns/reach-an-absent-user.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ReachAnAbsentUser : IPatternDemo
{
    public string Slug => "reach-an-absent-user";
    public string Title => "Reaching a user who has left";
    public string Category => "Status & feedback";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Server-side pattern with no UI: notification permission states and the email fallback "
        + "for a user who is not there. See the source and docs/patterns/reach-an-absent-user.md.");

    private IAppBase App => throw new NotImplementedException();

    #region docsnippet:pattern-reach-an-absent-user
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
    #endregion
}
