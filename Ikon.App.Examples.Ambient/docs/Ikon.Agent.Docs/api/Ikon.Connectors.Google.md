namespace Ikon.Connectors.Google
  sealed class Drive : IDisposable
    ctor(GoogleCredentials credentials)
    // Disposes the underlying Drive service and its HttpClient; construct one Drive per credential and reuse it rather than constructing per call.
    void Dispose()
    // Only files with binary content can be downloaded. Google-native Docs, Sheets and Slides (mime types application/vnd.google-apps.document, .spreadsheet, .presentation) have no binary content and Google rejects this call with HTTP 403 "Only files with binary content can be downloaded" — those require an Export, which this connector does not provide. Buffers the entire file into memory before returning, rather than streaming it — the returned stream is a fully-populated MemoryStream. Do not use it for very large files.
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    // Trashed files are included by default — the folder clause is only "<folder> in parents". Pass extraQuery "trashed = false" to exclude them.
    IAsyncEnumerable<DriveFile> ListAllAsync(string? folderId = null, string? extraQuery = null, CancellationToken ct = default)
    // Fetches a single page — limit caps that page, it is not a total across the folder. The query is only "<folder> in parents", so trashed files are included. For a complete listing that also filters them out, use ListAllAsync with extraQuery "trashed = false".
    Task<IReadOnlyList<DriveFile>> ListAsync(string? folderId = null, int limit = 50, CancellationToken ct = default)
    Task<DriveFile> UploadAsync(string name, string mimeType, Stream content, string? folderId = null, CancellationToken ct = default)
  sealed record DriveFile
    ctor(string Id, string Name, string MimeType, long? Size, string? WebViewLink, DateTimeOffset? ModifiedTime = null)
    string Id { get; init; }
    string MimeType { get; init; }
    DateTimeOffset? ModifiedTime { get; init; }
    string Name { get; init; }
    long? Size { get; init; }
    string? WebViewLink { get; init; }
  // ReceivedAt is DateTimeOffset.MinValue (year 0001) when Gmail supplies no internal date for the message, so guard for it before sorting or displaying.
  sealed record EmailSummary
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Authenticates with Google OAuth2 (refresh-token) credentials. Raw connector — no agent logic.
  sealed class Gmail : IDisposable
    ctor(GoogleCredentials credentials)
    // Disposes the underlying Gmail service and its HttpClient; construct one Gmail per credential and reuse it rather than constructing per call.
    void Dispose()
    // Returns the text/plain part when present, else the raw HTML of the text/html part, else an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Pages through the entire result set, unlike ListAsync which is capped by its limit. Bound a historical backfill with query date operators, e.g. "after:2024/01/01".
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    // to: One or more recipient addresses, comma- or semicolon-separated.
    // cc: Optional CC addresses, comma- or semicolon-separated.
    // isHtml: When true, body is sent as an HTML part; otherwise plain text.
    // throws ArgumentException: No recipient address remains after trimming empty entries from to.
    // throws ConnectorException: A recipient or CC address is malformed and cannot be parsed.
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, bool isHtml = false, CancellationToken ct = default)
  static class GoogleAuth
    // The returned UserCredential is a third-party type from the Google.Apis.Auth NuGet package (namespace Google.Apis.Auth.OAuth2), which ships transitively with this library. Assign it as the HttpClientInitializer in any Google API service initializer (Drive, Sheets, Gmail, Calendar, ...) from the corresponding Google.Apis.* package.
    // credentials: The stored OAuth2 client and refresh-token credentials.
    // scopes: Informational only. The credential refreshes via a refresh-token grant, which never sends a scope, so this argument has no runtime effect — it does not restrict or broaden the credential. The effective scopes are whatever the refresh token was granted at consent.
    static UserCredential CredentialFor(GoogleCredentials credentials, IEnumerable<string> scopes)
    // Branch on this to stop retrying and surface a "reconnect required" state: it is true only for permanent auth failures (revoked/expired refresh token, bad client), never for transient or network errors.
    static bool IsAuthFailure(Exception ex)
  sealed record GoogleCredentials
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }
