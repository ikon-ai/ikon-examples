# Ikon.Connectors Public API

namespace Ikon.Connectors
  sealed class ConnectorException : Exception
    ctor(string provider, string message, int? statusCode = null)
    string Provider { get; }
    // HTTP status of the failed response, when the failure was an HTTP error. Lets a caller distinguish a permanent 401/403 (reconnect required) from a transient failure.
    int? StatusCode { get; }
  // Catch ConnectorPageCapException<T> to keep the partial Items; this base only identifies the cap. ResumeFrom is where the unread remainder starts, in the paging method's own cursor form.
  abstract class ConnectorPageCapException : Exception
    int MaxPages { get; }
    string Provider { get; }
    // The paging method's own cursor form. GitHub issues: the newest UpdatedAt returned — pass it as the next since. Slack history: the OLDEST Ts returned — Slack pages backward, so the unread gap lies BELOW it; a caller must not advance its cursor past it, and can only close the gap by raising maxPages. Slack conversations: the next page cursor.
    string ResumeFrom { get; }
  // Same ordering as the completed result would have had (GitHub: update time ascending; Slack history: oldest-first). A caller that stays under the cap never sees this exception.
  sealed class ConnectorPageCapException<T> : ConnectorPageCapException
    ctor(string provider, string operation, int maxPages, IReadOnlyList<T> items, string resumeFrom)
    IReadOnlyList<T> Items { get; }
  // Repositories are addressed as "owner/name".
  sealed class GitHub
    ctor(string token, HttpClient? http = null)
    // Works on both issues and pull requests; returns the created comment's html_url.
    Task<string> CommentAsync(string repo, int number, string body, CancellationToken ct = default)
    Task<GitHubIssue> CreateIssueAsync(string repo, string title, string body, CancellationToken ct = default)
    Task<GitHubIssue> GetIssueAsync(string repo, int number, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately. A GitHub 403 may itself indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential.
    Task<string> GetPullRequestDiffAsync(string repo, int number, CancellationToken ct = default)
    // Ordered by update time ascending and paged to completion; throws ConnectorPageCapException<T> at maxPages when more remain. See the ListIssuesSinceAsync overload for the paging and inclusivity caveats.
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, DateTimeOffset since, int maxPages = 50, CancellationToken ct = default)
    // Never silently truncated: when maxPages pages are read and the last one was full, throws ConnectorPageCapException<T> of GitHubIssue carrying the pages fetched (Items, ascending, gap-free) and ResumeFrom, the newest GitHubIssue.UpdatedAt in them — pass it as the next since. A GitHub 403 may indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential. since is INCLUSIVE (returns issues updated at-or-after it), so resuming from an item's GitHubIssue.UpdatedAt re-returns every item updated in that same second: dedupe on GitHubIssue.Number (unlike Slack's exclusive oldest).
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, string since, int maxPages = 50, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately.
    Task<GitHubMergeResult> MergePullRequestAsync(string repo, int number, string? commitTitle = null, CancellationToken ct = default)
  sealed record GitHubIssue
    // UpdatedAt: The raw ISO-8601 timestamp exactly as GitHub returns it — callers that page by updated use it as an opaque ordered cursor, so reformatting it would break resume-from-cursor round-trips.
    ctor(int Number, string Title, string Body, string State, string Author, string? HtmlUrl, bool IsPullRequest, IReadOnlyList<string> Labels, string UpdatedAt)
    string Author { get; init; }
    string Body { get; init; }
    string? HtmlUrl { get; init; }
    bool IsPullRequest { get; init; }
    IReadOnlyList<string> Labels { get; init; }
    int Number { get; init; }
    string State { get; init; }
    string Title { get; init; }
    string UpdatedAt { get; init; }
  sealed record GitHubMergeResult
    ctor(bool Merged, string Message)
    bool Merged { get; init; }
    string Message { get; init; }
  sealed class Slack
    ctor(string botToken, HttpClient? http = null)
    // Only Slack-owned hosts (slack.com and subdomains) are fetched. A URL pointing anywhere else — e.g. one parsed out of untrusted message text — is rejected with an ArgumentException rather than fetched, so this cannot be turned into a server-side request against an internal host, and the workspace token can never leak to an attacker-controlled server. Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately.
    Task<byte[]> DownloadFileAsync(string url, CancellationToken ct = default)
    Task<SlackConversation> GetConversationAsync(string channelId, CancellationToken ct = default)
    // Returns only the most recent limit messages (default 20) as a single bounded peek — it does not paginate, and a page of exactly limit messages does not say whether older ones exist. For a complete range use HistorySinceAsync.
    Task<IReadOnlyList<SlackMessage>> HistoryAsync(string channel, int limit = 20, CancellationToken ct = default)
    // Top-level messages only: conversations.history omits in-thread replies (a reply is fetched by conversations.replies on its parent's ThreadTs, which this connector does not call). Never silently truncated: at maxPages with a next_cursor still pending, throws ConnectorPageCapException<T> of SlackMessage carrying the messages fetched (Items, oldest-first) and ResumeFrom, the oldest ts among them. Pages go BACKWARD in time, so the unread gap lies below ResumeFrom: ingest Items if useful, but never move a cursor past oldestTs — only a larger maxPages closes the gap. Page counts are no substitute; Slack routinely returns short pages with more remaining.
    Task<IReadOnlyList<SlackMessage>> HistorySinceAsync(string channel, string oldestTs, int pageLimit = 200, int maxPages = 50, CancellationToken ct = default)
    // Never silently truncated: when maxPages pages are read and Slack still reports a next_cursor, throws ConnectorPageCapException<T> of SlackConversation carrying the conversations fetched (Items) and, as ResumeFrom, that next cursor. A caller that stays under the cap never sees it.
    Task<IReadOnlyList<SlackConversation>> ListConversationsAsync(int maxPages = 50, CancellationToken ct = default)
    // appToken: An app-level token (xapp-...), not the bot token.
    Task<string> OpenSocketUrlAsync(string appToken, CancellationToken ct = default)
    // Accepts a message object from a history page or a Socket Mode event; returns null when the object has no ts (not a message).
    static SlackMessage? ParseMessage(JsonElement message, string channel)
    // The returned SlackMessage is synthesized from the request, not fetched back: only SlackMessage.Ts and SlackMessage.Channel are populated from the server response. SlackMessage.User is always empty, SlackMessage.Subtype is always null, SlackMessage.Files is always empty, and SlackMessage.ThreadTs merely echoes the argument — callers must not read those back.
    Task<SlackMessage> PostAsync(string channel, string text, string? threadTs = null, CancellationToken ct = default)
  sealed record SlackConversation
    ctor(string Id, string Name, bool IsMember, bool IsPrivate, bool IsIm, bool IsMpim)
    string Id { get; init; }
    bool IsIm { get; init; }
    bool IsMember { get; init; }
    bool IsMpim { get; init; }
    bool IsPrivate { get; init; }
    string Name { get; init; }
  sealed record SlackFile
    ctor(string Id, string MimeType, string? DownloadUrl)
    string? DownloadUrl { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
  sealed record SlackMessage
    ctor(string Channel, string User, string Text, string Ts, string? ThreadTs = null, string? Subtype = null, IReadOnlyList<SlackFile>? Files = null)
    string Channel { get; init; }
    // Empty, never null, when the message has none.
    IReadOnlyList<SlackFile> Files { get; init; }
    string? Subtype { get; init; }
    string Text { get; init; }
    string? ThreadTs { get; init; }
    string Ts { get; init; }
    string User { get; init; }
