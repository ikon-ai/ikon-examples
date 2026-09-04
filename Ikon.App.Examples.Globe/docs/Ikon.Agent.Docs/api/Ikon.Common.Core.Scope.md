namespace Ikon.Common.Core.Scope
  // Each time a client connects to the server, it gets a new ClientScope with a unique Id (session ID). This scope is used by ClientReactive<T> to partition state per client. Relationship to UserScope: Multiple ClientScopes can belong to the same user. For example, a user connected from two clients has two different ClientScope IDs but the same UserScope ID. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework for each client iteration.
  readonly struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  readonly struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Pushed by the framework alongside UserScope / ClientScope during the per-(client, mount) render iteration in ReactiveRoot.RunAsync.
  readonly struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream; apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Machine-triggered work has no ClientScope and no UserScope, so without this its cost lands in the space's totals attached to nothing and a schedule quietly burning credits is indistinguishable from the app's ordinary use. Every log event carries the active scopes, so the cost of an AI call made inside a trigger handler is attributed by the ambient scope alone — call sites need no change. Scoped to the invocation rather than the session on purpose: a session woken by cron goes on to serve clients, and their spend is theirs, not the schedule's. The values match the backend's AppSessionSource spelling, so the trigger a cost row carries reads the same as the source stamped on the session that ran it.
  readonly struct TriggerScope : IScopeKey
    ctor(string kind)
    string Id { get; }
    string Name { get; }
    const string Cron
    const string Endpoint
  // Identifies a logical user across their multiple client sessions. Used by UserReactive<T> to share state across a user's multiple connected clients. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework alongside ClientScope.
  readonly struct UserScope : IScopeKey
    ctor(string userId)
    ctor(Context context)
    string Id { get; }
    string Name { get; }
