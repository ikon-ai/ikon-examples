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
