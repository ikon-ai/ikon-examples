# Email

## Email

Send transactional email and read the mail delivered to the app's space via `app.Email` — through the platform mailer, with no SMTP credentials or DNS setup in the app. The platform owns the From domain (always one the space has verified for sending); the request chooses the local part, display name, and, for a space with several verified sending domains, which domain to send from.

---

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


---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Email
  // Sender or recipient entry parsed from an inbound email envelope.
  sealed record EmailAddress
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Represents a single attachment on an outgoing app email. Bytes is the raw binary content; the platform encodes it as base64 before sending it on the wire.
  sealed record EmailAttachment
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  // A streaming attachment download. The caller owns the Content stream; dispose this object (e.g. await using) to release it.
  sealed class EmailAttachmentDownload : IAsyncDisposable
    // Decrypted attachment bytes streamed from the platform.
    Stream Content { get; }
    // The sender-supplied filename, sanitized by the platform.
    string Filename { get; }
    // The attachment's MIME type, as recorded at ingest time.
    string MimeType { get; }
    // The decrypted (plaintext) attachment size in bytes.
    long Size { get; }
    ValueTask DisposeAsync()
  // A single SMTP header preserved on an inbound email.
  sealed record EmailHeader
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // Specification for a custom email sent by an app through the platform mailer. The platform enqueues the send for asynchronous delivery and returns once the request has been accepted; transient delivery failures are retried server-side.
  sealed record EmailSendRequest
    // To: Recipient email address.
    // Subject: Email subject line.
    // HtmlBody: Pre-rendered HTML body of the email.
    // TextBody: Optional plain-text fallback for clients that do not render HTML.
    // ReplyTo: Optional Reply-To address, for directing replies away from the From address.
    // Attachments: Optional list of binary attachments. Up to 10 per email.
    // Metadata: Optional string key/value pairs forwarded to the mail provider for tracking.
    // SenderLocalPart: Optional local part of the From address — the part before the @. The platform owns the domain and only ever uses one the space has verified for sending, so this cannot send from somewhere else. Lowercase letters, digits, dot, underscore and hyphen only, starting and ending alphanumeric, at most 64 characters; names belonging to the mail infrastructure (postmaster, abuse, mailer-daemon, …) are rejected. When the space has no verified sending domain the send fails with EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address.
    // SenderDisplayName: Optional display name shown beside the From address. Defaults to the space's own name. At most 64 characters, with line breaks and other header-unsafe characters rejected. Like SenderLocalPart, requires a verified sending domain.
    // SenderDomain: Optional sending domain for the From address, for a space with more than one verified sending domain. Must be one of the space's own verified sending domains; anything else fails the send with EmailSenderNotAvailableException. Left null, the platform picks the space's designated or best verified sending domain.
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null, string? SenderLocalPart = null, string? SenderDisplayName = null, string? SenderDomain = null)
    // Optional list of binary attachments. Up to 10 per email.
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    // Pre-rendered HTML body of the email.
    string HtmlBody { get; init; }
    // Optional string key/value pairs forwarded to the mail provider for tracking.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional Reply-To address, for directing replies away from the From address.
    string? ReplyTo { get; init; }
    // Optional display name shown beside the From address. Defaults to the space's own name. At most 64 characters, with line breaks and other header-unsafe characters rejected. Like SenderLocalPart, requires a verified sending domain.
    string? SenderDisplayName { get; init; }
    // Optional sending domain for the From address, for a space with more than one verified sending domain. Must be one of the space's own verified sending domains; anything else fails the send with EmailSenderNotAvailableException. Left null, the platform picks the space's designated or best verified sending domain.
    string? SenderDomain { get; init; }
    // Optional local part of the From address — the part before the @. The platform owns the domain and only ever uses one the space has verified for sending, so this cannot send from somewhere else. Lowercase letters, digits, dot, underscore and hyphen only, starting and ending alphanumeric, at most 64 characters; names belonging to the mail infrastructure (postmaster, abuse, mailer-daemon, …) are rejected. When the space has no verified sending domain the send fails with EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address.
    string? SenderLocalPart { get; init; }
    // Email subject line.
    string Subject { get; init; }
    // Optional plain-text fallback for clients that do not render HTML.
    string? TextBody { get; init; }
    // Recipient email address.
    string To { get; init; }
  // Rules for the sender identity fields on an outgoing app email — the local part alphabet and the names the mail infrastructure keeps for itself. Checking against these before sending turns a rejection from the platform into an immediate, local error.
  static class EmailSenderIdentity
    // Whether a normalized local part is one of the names reserved for the mail infrastructure.
    static bool IsReservedLocalPart(string localPart)
    // Whether a normalized local part matches the alphabet the platform accepts.
    static bool IsValidLocalPart(string localPart)
    // Trims and lowercases a local part the way the backend does before validating. Returns null when nothing remains.
    static string? NormalizeLocalPart(string? localPart)
    const int MaxDisplayNameCodePoints = 64
    const int MaxLocalPartLength = 64
  // Lightweight metadata for an inbound email's attachment — does not include the body bytes. Fetch the body via the email service's DownloadAttachmentAsync.
  sealed record InboundAttachmentInfo
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Full inbound email with decrypted body and parsed envelope. Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
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
  // Inbox-listing entry. Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
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
  // One page of inbox results. NextCursor is null when there are no more pages.
  sealed record InboxPage
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  // Filter and pagination parameters for an inbox listing.
  sealed record InboxQuery
    ctor()
    // Opaque cursor returned by a previous InboxPage.NextCursor. null requests the first page.
    string? Cursor { get; init; }
    // Filter to messages sent from this address. Case-insensitive.
    string? From { get; init; }
    // Maximum number of messages to return for this page. The platform clamps to [1, 100]; values outside that range are silently adjusted. Defaults to 25.
    int Limit { get; init; }
    // Filter to messages delivered to this recipient address. Case-insensitive.
    string? Recipient { get; init; }
    // Inclusive lower bound on the SMTP receive timestamp.
    DateTimeOffset? Since { get; init; }
    // Inclusive upper bound on the SMTP receive timestamp.
    DateTimeOffset? Until { get; init; }
