namespace Ikon.App.Cells
  // A cell is always shared by its SessionIdentity: every caller that Cells.Connects with the same identity reaches the same instance and its Reactive<T> state — the identity IS the sharing scope (parameterless = one global; keyed = one per key). The runtime picks the transport: a local run hosts every cell in-process (a direct object); in the cloud the cell lives in its own cell-host and callers reach it through a proxy ([HttpGet]/[HttpPost] over HTTP, [Function] methods and Reactive<T> members over an SDK connection). App authors never choose or think about placement — they declare [Cell] and a SessionIdentity, and get exactly what those mean.
  sealed class CellAttribute : Attribute
    ctor()
    // Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them: globals (parameterless SessionIdentity) eager-spawn at host construction, keyed cells spawn together on first access. Sharded keyed cells must tolerate eventual consistency between shards — hold no per-instance state, or persist shared state externally.
    int Capacity { get; init; }
    // Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework installs the host at startup and swaps it on every hot reload and session-identity change (InstallHost, which keeps the app's registered wiring); Initialize is the clean-slate form for tests and the no-host fallback. Apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    ValueTask DisposeAsync()
    const string CellTypeParam
  // Injected into a cell's primary constructor by the framework.
  interface ICell<out TSessionIdentity>
    TSessionIdentity Identity { get; }
  // Pattern-match the proxy's Reactive<T> member to this type to read Status inside a render (a tracked read, so the UI re-renders when the subscription lands or gives up) and show "loading" or "unavailable" instead of an empty list that looks complete. Error carries the last subscribe failure once Status is MirrorStatus.Failed.
  sealed class MirrorReactive<T> : Reactive<T>
    // Null until a subscribe attempt has failed for good.
    Exception? Error { get; }
    // A tracked read: a render that reads it re-renders when the subscription lands or fails.
    MirrorStatus Status { get; }
  enum MirrorStatus
    // The subscription has not landed yet; the mirror holds its seed value (empty, 0, null).
    Connecting
    // The subscription is live; the value is the cell's.
    Live
    // Every subscribe attempt failed; the value is still the seed and will not update. Reading the member again from the cell proxy re-subscribes.
    Failed
