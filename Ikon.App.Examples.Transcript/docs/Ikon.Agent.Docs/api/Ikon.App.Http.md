namespace Ikon.App.Http
  // Exposes the request's resolved identity to handler code on endpoint/MCP-dispatched calls, where the connection-level context carries none. Headers and RawBody are untrusted request inputs — read them for handler logic such as inline webhook-signature verification, but never to derive identity; the target instance is already chosen from trusted sources before the handler runs.
  sealed record HttpCallContext
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // Null when no HttpCallContext is current or the identity carries no userid (e.g. an anonymous endpoint).
    string? UserId { get; }
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)
