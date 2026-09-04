namespace Ikon.Connectors
  sealed class ConnectorException : Exception
    ctor(string provider, string message, int? statusCode = null)
    string Provider { get; }
    // HTTP status of the failed response, when the failure was an HTTP error. Lets a caller distinguish a permanent 401/403 (reconnect required) from a transient failure.
    int? StatusCode { get; }
  // Repositories are addressed as "owner/name".
  sealed class GitHub
    ctor(string token, HttpClient? http = null)
    // Works on both issues and pull requests; returns the created comment's html_url.
    Task<string> CommentAsync(string repo, int number, string body, CancellationToken ct = default)
    Task<GitHubIssue> CreateIssueAsync(string repo, string title, string body, CancellationToken ct = default)
    Task<GitHubIssue> GetIssueAsync(string repo, int number, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately. A GitHub 403 may itself indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential.
    Task<string> GetPullRequestDiffAsync(string repo, int number, CancellationToken ct = default)
    // Ordered by update time ascending and paged to completion (bounded by maxPages). See the ListIssuesSinceAsync overload for the paging, truncation and inclusivity caveats.
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, DateTimeOffset since, int maxPages = 50, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal. Detect this by comparing the result length against the page cap (maxPages × 100): if it reaches the cap, resume by calling again with since raised to the newest GitHubIssue.UpdatedAt returned. A GitHub 403 may indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential. since is INCLUSIVE (returns issues updated at-or-after it) while results are ordered by update time ascending, so resuming with since set to the last item's GitHubIssue.UpdatedAt re-returns every item updated in that same second. When resuming, dedupe on GitHubIssue.Number (unlike Slack's exclusive oldest).
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
    // Only Slack-owned hosts (slack.com and subdomains) are fetched. A URL pointing anywhere else — e.g. one parsed out of untrusted message text — is rejected with an ArgumentException rather than fetched, so this cannot be turned into a server-side request against an internal host, and the workspace token can never leak to an attacker-controlled server.
    Task<byte[]> DownloadFileAsync(string url, CancellationToken ct = default)
    Task<SlackConversation> GetConversationAsync(string channelId, CancellationToken ct = default)
    // Returns only the most recent limit messages (default 20) as a single bounded peek — it does not paginate. For a complete range use HistorySinceAsync.
    Task<IReadOnlyList<SlackMessage>> HistoryAsync(string channel, int limit = 20, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal. Because pages go backward in time, the OLDEST messages are the ones dropped, leaving a gap at the start of the range. Comparing the result length against the page cap (maxPages × pageLimit) under-counts and is NOT a reliable truncation signal: conversations.history routinely returns fewer than pageLimit per page even when more pages remain, so a genuinely truncated backfill rarely reaches the product. The certain approach is to raise maxPages until a call returns a short (unfilled) final page; on truncation, resume by calling again with oldestTs raised to the oldest ts returned.
    Task<IReadOnlyList<SlackMessage>> HistorySinceAsync(string channel, string oldestTs, int pageLimit = 200, int maxPages = 50, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal, so a caller cannot trust "completion" for a workspace with more conversations than the cap admits.
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
