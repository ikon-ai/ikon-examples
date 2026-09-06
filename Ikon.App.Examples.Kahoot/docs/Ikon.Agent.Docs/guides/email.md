# Email

## Email

Send transactional email and read the mail delivered to the app's space via `app.Email` — through the platform mailer, with no SMTP credentials or DNS setup in the app. The platform owns the From domain (always one the space has verified for sending); the request chooses the local part, display name, and, for a space with several verified sending domains, which domain to send from.

---

# Ikon.App.Email Guide

Send transactional email from your app and read the mail delivered to your app's space — through the
platform mailer, with no SMTP credentials, provider account, or DNS setup in the app itself.
`app.Email` (an `EmailService`) is the entry point; the space's organisation must have the **Email** feature enabled
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
designated as the space's **email sender** — chosen when the domain is set up — wins; without a
designation, a domain of your own beats the platform-provided one, then the earliest verified one.

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

## Sending from your own domain

Out of the box a space sends from a platform-provided domain, so the examples above work with no DNS
setup at all. Sending from a domain of your own — so the From address reads `reports@yourfirm.com` —
is arranged with Ikon rather than configured in the app: ask your Ikon contact to add the domain to
your space, and you will be given the records to publish.

They come in two rounds:

| Round | Record | What it does |
| --- | --- | --- |
| Ownership | one `TXT` at `_ikon-verify.<your-domain>` | proves the domain is yours; nothing is provisioned before it resolves |
| Sending | `DKIM`, `SPF` and a return-path record | let receiving servers accept mail the platform sends as you |

Publish the ownership record first — the sending records are only issued once it has been confirmed.
The domain becomes usable when every sending record resolves publicly and passes verification. Both
rounds wait on your DNS provider to propagate, so expect the setup to span hours rather than minutes,
and keep the records in place afterwards: they are re-checked periodically, and a domain whose
records disappear stops being a valid sender.

If your domain publishes no `DMARC` policy, the platform supplies `v=DMARC1; p=none` so mail is not
treated as unauthenticated by the mailbox providers that now require a record. Tightening it to
`quarantine` or `reject` is yours to do once your reports are clean — publish a stricter record of
your own and it takes precedence.

A verified domain is what the `SenderDomain`, `SenderLocalPart` and `SenderDisplayName` fields above
resolve against. A domain is verified for one environment at a time, so development keeps using the
platform default sender.

## Read the inbox

Inbound email delivered to the app's space is available as pages or as a lazy stream:

```csharp
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
```

`InboxQuery` filters by recipient, sender, and time window. Deleting a message frees its attachment
storage; deleting an unknown id throws rather than succeeding silently.

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Email
  sealed record EmailAddress
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Bytes is the raw binary content; the platform encodes it as base64 on the wire.
  sealed record EmailAttachment
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  // The caller owns the Content stream; dispose this object (e.g. await using) to release it.
  sealed class EmailAttachmentDownload : IAsyncDisposable
    Stream Content { get; }
    // Sender-supplied, sanitized by the platform.
    string Filename { get; }
    string MimeType { get; }
    long Size { get; }
    ValueTask DisposeAsync()
  sealed record EmailHeader
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // The platform enqueues the send and returns once accepted; transient delivery failures are retried server-side.
  sealed record EmailSendRequest
    // Attachments: Up to 10 per email.
    // Metadata: Forwarded to the mail provider for tracking.
    // SenderLocalPart: The From-address part before the @: lowercase letters, digits, dot, underscore, hyphen; alphanumeric at both ends; max 64 chars; mail-infrastructure names (postmaster, abuse, …) rejected. Needs a verified sending domain, else the send fails with EmailSenderNotAvailableException.
    // SenderDisplayName: Defaults to the space's name. Max 64 characters; header-unsafe characters rejected. Needs a verified sending domain.
    // SenderDomain: Must be one of the space's verified sending domains, else EmailSenderNotAvailableException. Null lets the platform pick the designated or best verified domain.
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null, string? SenderLocalPart = null, string? SenderDisplayName = null, string? SenderDomain = null)
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    string HtmlBody { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? ReplyTo { get; init; }
    string? SenderDisplayName { get; init; }
    string? SenderDomain { get; init; }
    string? SenderLocalPart { get; init; }
    string Subject { get; init; }
    string? TextBody { get; init; }
    string To { get; init; }
  // Checking against these before sending turns a rejection from the platform into an immediate, local error.
  static class EmailSenderIdentity
    static bool IsReservedLocalPart(string localPart)
    static bool IsValidLocalPart(string localPart)
    // Trims and lowercases the way the backend does before validating; returns null when nothing remains.
    static string? NormalizeLocalPart(string? localPart)
    const int MaxDisplayNameCodePoints = 64
    const int MaxLocalPartLength = 64
  // Metadata only — no body bytes; fetch the body via the email service's DownloadAttachmentAsync.
  sealed record InboundAttachmentInfo
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
  sealed record InboundEmailDetail
    ctor(string Id, string Recipient, string From, string Subject, string? BodyText, string? BodyHtml, IReadOnlyList<EmailAddress> To, IReadOnlyList<EmailAddress> Cc, string? ReplyTo, IReadOnlyList<EmailHeader> Headers, IReadOnlyList<InboundAttachmentInfo> Attachments, DateTimeOffset ReceivedAt, double? SpamScore, string? Tag)
    IReadOnlyList<InboundAttachmentInfo> Attachments { get; init; }
    string? BodyHtml { get; init; }
    string? BodyText { get; init; }
    IReadOnlyList<EmailAddress> Cc { get; init; }
    string From { get; init; }
    IReadOnlyList<EmailHeader> Headers { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    string? ReplyTo { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
    IReadOnlyList<EmailAddress> To { get; init; }
  // Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
  sealed record InboundEmailSummary
    ctor(string Id, string Recipient, string From, string Subject, DateTimeOffset ReceivedAt, int AttachmentCount, double? SpamScore, string? Tag)
    int AttachmentCount { get; init; }
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
  // NextCursor is null when there are no more pages.
  sealed record InboxPage
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  sealed record InboxQuery
    ctor()
    // Opaque cursor from a previous InboxPage.NextCursor; null requests the first page.
    string? Cursor { get; init; }
    // Case-insensitive.
    string? From { get; init; }
    // The platform clamps to [1, 100]; values outside that range are silently adjusted. Defaults to 25.
    int Limit { get; init; }
    // Case-insensitive.
    string? Recipient { get; init; }
    // Inclusive lower bound on the SMTP receive timestamp.
    DateTimeOffset? Since { get; init; }
    // Inclusive upper bound on the SMTP receive timestamp.
    DateTimeOffset? Until { get; init; }
