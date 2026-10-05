namespace Ikon.App.Patterns.Examples;

// The email guide, as code that compiles. The holder is the one file the guide reads as: its
// shared names are the holder's fields.

file sealed class EmailGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task SendAsync(byte[] pdfBytes)
    {
        #region example:email-send
        await app.Email.SendAsync(new EmailSendRequest(
            To: "customer@example.com",
            Subject: "Your report is ready",
            HtmlBody: "<p>Find the report attached.</p>",
            TextBody: "Find the report attached.",           // optional plain-text fallback
            ReplyTo: "reports@yourfirm.com",                 // optional; replies go here, not to the From address
            Attachments: [new EmailAttachment("report.pdf", "application/pdf", pdfBytes)],
            Metadata: new Dictionary<string, string> { ["kind"] = "report" }));
        #endregion
    }

    public async Task FallbackAsync(EmailSendRequest request)
    {
        #region example:email-sender-fallback
        try
        {
            await app.Email.SendAsync(request);
        }
        catch (EmailSenderNotAvailableException)
        {
            // Deliver anyway, from the platform's own address.
            await app.Email.SendAsync(request with { SenderLocalPart = null, SenderDisplayName = null, SenderDomain = null });
        }
        #endregion
    }

    public async Task InboxAsync()
    {
        #region example:email-inbox
        // One page at a time
        var page = await app.Email.GetInboxPageAsync(new InboxQuery { Limit = 50 });

        // Or enumerate across pages; breaking out stops fetching
        await foreach (var summary in app.Email.EnumerateInboxAsync(new InboxQuery()))
        {
            var detail = await app.Email.GetMessageAsync(summary.Id);

            foreach (var attachment in detail.Attachments)
            {
                await using var download = await app.Email.DownloadAttachmentAsync(detail.Id, attachment.Id);
                // download.Content is the decrypted stream
            }

            await app.Email.DeleteAsync(summary.Id);
        }
        #endregion

        Log.Instance.Debug($"{page}");
    }
}
