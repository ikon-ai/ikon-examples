namespace Ikon.Common.Core
  // A reactive version of the protocol GlobalState: each property is wrapped in a Reactive so a UI binding to it updates only when the value changes.
  class ReactiveGlobalState
    ctor()
    // Empty outside a cloud run.
    Reactive<string> AppSessionId { get; }
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    // Keyed by client session id; each Context carries that client's user id, device, viewport, and locale.
    Reactive<Dictionary<int, Context>> Clients { get; }
    Reactive<bool> DebugMode { get; }
    // The current first human user; reassigned when that user leaves. Contrast PrimaryUserId, which is fixed.
    Reactive<string> FirstUserId { get; }
    // Keyed by client session id.
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    Reactive<string> IkonServerId { get; }
    Reactive<string> OrganisationName { get; }
    // The session owner from server config, fixed for the session's lifetime; used for user-specific asset storage paths.
    Reactive<string> PrimaryUserId { get; }
    // True when the app is being run through publicly accessible endpoints in local development.
    Reactive<bool> PublicAccess { get; }
    Reactive<ServerRunType> ServerRunType { get; }
    Reactive<string> SessionIdentityHash { get; }
    Reactive<string> SessionUrl { get; }
    Reactive<string> SpaceId { get; }
    Reactive<string> SpaceName { get; }
    Reactive<string> SpaceUrl { get; }
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    Context? GetClientContext(int clientSessionId)
    // The first connected client context of this user, or null when the user has none.
    Context? GetClientContext(string userId)
    IEnumerable<Context> GetHumanClients()
    // One context per distinct AuthSessionId — a user with multiple clients contributes only the first by iteration order.
    IEnumerable<Context> GetUniqueAuthClientContexts()
    // One human context per distinct AuthSessionId — a user with multiple clients contributes only the first by iteration order.
    IEnumerable<Context> GetUniqueHumanAuthClientContexts()
    // Updates from a new GlobalState; only the reactive properties whose value actually changed trigger notifications.
    void UpdateFrom(GlobalState newState)
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static readonly ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  static class ReadOnlyListExtensions
    // -1 when nothing matches.
    static int FindIndex<T>(this IReadOnlyList<T> items, Predicate<T> match)
    // -1 when nothing matches.
    static int FindLastIndex<T>(this IReadOnlyList<T> items, Predicate<T> match)
    // Equality is EqualityComparer<T>.Default; -1 when absent.
    static int IndexOf<T>(this IReadOnlyList<T> items, T item)
  // Read-only view of the space-scoped secrets loaded from the Ikon backend. Apps receive one via app.Secrets; pipelines via host.Secrets. Manage values with ikon app secret set/list/delete. Rotating a secret while an app or pipeline is running only takes effect after a restart.
  sealed class Secrets
    // Throws InvalidOperationException when no secret with that key is set for this space; use TryGet for a non-throwing lookup.
    string this[string key] { get; }
    IReadOnlyCollection<string> Keys { get; }
    bool TryGet(string key, out string? value)
  class Sensitive<T>
    ctor(T value, SensitivityPolicy sensitivityPolicy = Default)
    bool IsSensitive { get; }
    SensitivityPolicy Policy { get; }
    T Value { get; }
  enum SensitivityPolicy
    Default
  // Turns the service token in IKON_SERVICE_TOKEN into a short-lived access token that lives in this process only; the exchange result is cached per process.
  static class ServiceTokenExchanger
    static string? GetServiceToken()
    // True when the service token's own expiry is within ExpiryWarningWindow of now, or already past. A missing or unparseable timestamp is never soon.
    static bool IsExpiringSoon(string? serviceTokenExpiresAt, DateTimeOffset now)
    // Null when no service token is set or the exchange failed. Cached per environment, because one token is only ever valid for the environment that minted it.
    static IkonBackend.LoginInfo? TryExchange(IkonBackend.EnvironmentType environment)
    static readonly TimeSpan ExpiryWarningWindow
    const string ServiceTokenVariable
  // The canonical routing-identity hash behind the wire SessionHash and the gateway's cell routing. Byte-compatible with the TypeScript backend's getHashFromObject: keys sorted by UTF-16 code units, values serialized with JSON.stringify escaping, SHA256, unpadded base64url. ROUTING only — persisted-reactive storage partitioning uses its own historical hash that must never change.
  static class SessionIdentityHash
    static string Compute(IReadOnlyDictionary<string, string> sessionIdentity)
    // Hashes the identity fields plus CellTypeKey. Identity KEYS must be the cell's SessionIdentity record parameter names VERBATIM (e.g. "ChannelId", not "channelId"): the gateway keys the hashed object by the manifest field name regardless of the query-param casing the caller used.
    static string ComputeForCell(string cellType, IReadOnlyDictionary<string, string> sessionIdentity)
    const string CellTypeKey
  // The env-var gate is the security boundary: the route-enumeration function must only exist in a capture process, never in production, because Context.IsSnapshot is client-controlled and cannot gate anything.
  static class SnapshotCapture
    static bool IsCaptureProcess { get; }
    // Total capture budget across all routes; routes not captured in time are skipped.
    const int CaptureBudgetMs = 600000
    const string EnabledEnvVar
    // Counted after unioning static and dynamic routes. Variant skeletons are bounded separately (MaxVariants) and do not consume route slots.
    const int MaxRoutes = 50
    const int MaxVariants = 16
    const string ReadyFunctionName
    const int RouteTimeoutMs = 10000
    const string RoutesEnvVar
    const string RoutesFunctionName
    // Quiescence window: a route is considered settled after this many ms without a UI update. The app's ready signal (ReadyFunctionName) always wins the race when it arrives first.
    const int SettleMs = 750
  sealed class TelephonyNumberNotAvailableException : UserException
    ctor(string friendlyMessage, string? number = null, string? hint = null)
    // What to do about it, as a command the developer can run.
    string? Hint { get; }
    // The number that was asked for; null when the caller named none.
    string? Number { get; }
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
  // User-facing errors displayed cleanly without stack traces. Use for expected error conditions like invalid input, missing files, or failed operations.
  class UserException : Exception
