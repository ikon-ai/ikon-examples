# Ikon.Connectors.Google Public API

namespace Ikon.Connectors.Google
  sealed class Drive
    ctor(GoogleCredentials credentials)
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    IAsyncEnumerable<DriveFile> ListAllAsync(string? folderId = null, string? extraQuery = null, CancellationToken ct = default)
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
  sealed record DriveListRequest
    ctor(string? FolderId = null, int Limit = 50)
    string? FolderId { get; init; }
    int Limit { get; init; }
  sealed class DriveSkill : Skill
    ctor(Drive drive)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed record EmailSummary
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Authenticates with Google OAuth2 (refresh-token) credentials. Raw connector — no agent logic.
  sealed class Gmail
    ctor(GoogleCredentials credentials)
    // Returns the text/plain part when present, else the raw HTML of the text/html part, else an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Pages through the entire result set, unlike ListAsync which is capped by its limit. Bound a historical backfill with query date operators, e.g. "after:2024/01/01".
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, CancellationToken ct = default)
  sealed record GmailListRequest
    ctor(string? Query = null, int Limit = 20)
    int Limit { get; init; }
    string? Query { get; init; }
  sealed record GmailSendRequest
    ctor(string To, string Subject, string Body, string? Cc = null)
    string Body { get; init; }
    string? Cc { get; init; }
    string Subject { get; init; }
    string To { get; init; }
  sealed class GmailSkill : Skill
    ctor(Gmail gmail)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  static class GoogleAuth
    // The returned UserCredential is a third-party type from the Google.Apis.Auth NuGet package (namespace Google.Apis.Auth.OAuth2), which ships transitively with this library. Assign it as the HttpClientInitializer in any Google API service initializer (Drive, Sheets, Gmail, Calendar, ...) from the corresponding Google.Apis.* package.
    static UserCredential CredentialFor(GoogleCredentials credentials, IEnumerable<string> scopes)
    // Branch on this to stop retrying and surface a "reconnect required" state: it is true only for permanent auth failures (revoked/expired refresh token, bad client), never for transient or network errors.
    static bool IsAuthFailure(Exception ex)
  sealed record GoogleCredentials
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }
