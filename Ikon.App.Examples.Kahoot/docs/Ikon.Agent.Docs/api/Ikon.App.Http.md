namespace Ikon.App.Http
  // Exposes the request's resolved identity to handler code on endpoint/MCP-dispatched calls, where the connection-level context carries none. Headers and RawBody are untrusted request inputs — read them for handler logic such as inline webhook-signature verification, but never to derive identity; the target instance is already chosen from trusted sources before the handler runs.
  sealed record HttpCallContext
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // The user the gateway proved for the call in flight (ProvenUser.Current) when there is one, regardless of the identity dict; otherwise the identity's userid (case-insensitive key). Null only when no user was proven and the identity carries no userid (e.g. an anonymous endpoint).
    string? UserId { get; }
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)
