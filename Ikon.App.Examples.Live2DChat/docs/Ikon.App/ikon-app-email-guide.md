# Ikon.App.Email Guide

Send transactional email from your app and read the mail delivered to your app's space — through the
platform mailer, with no SMTP credentials, provider account, or DNS setup in the app itself.
`app.Email` is the entry point; the space's organisation must have the **Email** feature enabled
(calls without it throw `FeatureNotEnabledException`).

## Send an email

```csharp
await app.Email.SendAsync(new EmailSendRequest(
    To: "customer@example.com",
    Subject: "Your report is ready",
    HtmlBody: "<p>Find the report attached.</p>",
    TextBody: "Find the report attached.",           // optional plain-text fallback
    ReplyTo: "reports@yourfirm.com",                 // optional; replies go here, not to the From address
    Attachments: [new EmailAttachment("report.pdf", "application/pdf", pdfBytes)],
    Metadata: new Dictionary<string, string> { ["kind"] = "report" }));
```

The send is **accepted, not delivered**: a successful return means the platform queued the message,
and transient delivery failures are retried server-side. Invalid input throws `ArgumentException`
locally, before anything is sent.

Limits, enforced identically in the client and the backend:

| What | Limit |
| --- | --- |
| Total payload (subject, bodies, metadata, attachments as base64) | 10 MB |
| Subject | 200 characters |
| Attachments | 10 per email; filename ≤ 255, MIME type ≤ 100 characters |
| Metadata | string values only, at most 20 keys |

Attachments count at their base64-encoded size (~4/3 of the raw bytes), so the practical budget for
raw attachment bytes is roughly 7.5 MB.

## The From address and sender identity

The platform owns the From **domain** — it only ever sends from a domain the space has verified for
sending, so an app cannot impersonate an address it does not control. Within that, the request
chooses the identity:

- **`SenderLocalPart`** — the part before the `@`. Lowercase letters, digits, dot, underscore and
  hyphen, starting and ending alphanumeric, at most 64 characters. Names that belong to the mail
  infrastructure (`postmaster`, `abuse`, `security`, `mailer-daemon`, …) are rejected.
- **`SenderDisplayName`** — the name shown beside the address, at most 64 characters (measured in
  code points). Defaults to the space's own name.
- **`SenderDomain`** — for a space with more than one verified sending domain: name the one to send
  from. It must be a verified sending domain of the space.

With no `SenderDomain`, the platform picks the space's sending domain deterministically: the domain
designated as the space's **email sender** (set on the hostname's page in the portal) wins; without
a designation, a customer-owned domain beats the platform-provided hostname, then the earliest
verified one.

### When the sender identity cannot be honoured

A request that names any sender identity needs a verified sending domain behind it. When the space
has none — or the requested `SenderDomain` is not a verified sending domain of the space — the send
fails with `EmailSenderNotAvailableException`. Nothing is sent in that case, so decide what matters
more, the identity or the delivery:

```csharp
try
{
    await app.Email.SendAsync(request);
}
catch (EmailSenderNotAvailableException)
{
    // Deliver anyway, from the platform's own address.
    await app.Email.SendAsync(request with { SenderLocalPart = null, SenderDisplayName = null, SenderDomain = null });
}
```

A request with **no** sender identity fields never hits this: it sends from the space's verified
domain when one exists and from the platform's default address otherwise.

## Read the inbox

Inbound email delivered to the app's space is available as pages or as a lazy stream:

```csharp
// One page at a time
var page = await app.Email.GetInboxPageAsync(new InboxQuery(Limit: 50));

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
```

`InboxQuery` filters by recipient, sender, and time window. Deleting a message frees its attachment
storage; deleting an unknown id throws rather than succeeding silently.
