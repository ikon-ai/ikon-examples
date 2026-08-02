# Ikon Platform Events

Structured events emitted by IkonServer and the core Ikon libraries via `Log.Instance.Event(name, payload)`. They are routed through `LogEventSender` to the Ikon backend and intended for consumption by external monitoring/analytics tooling. Events from the legacy `AIAgentPlugin` and from vertical extensions (Velory, Learning, TalkToData, etc.) are out of scope here.

## Server lifecycle

| Event | When | Payload |
|---|---|---|
| `server_initializing` | Core server starts initialising | `isPrestart`, `isHosted` |
| `server_warmup_complete` | Prestart JIT warmup finished | `elapsedTimeMs` |
| `server_initialized` | `CORE_SERVER_INIT` message processing finished — plugins + extensions loaded | `durationMs` (block duration), `pluginsInitMs`, `extensionsInitMs` |
| `server_started` | Server is fully started and accepting traffic | `elapsedTimeMs` |
| `server_stopped` | Server has shut down cleanly | `stopDurationMs` |
| `server_failed` | Uncaught exception in the server entry point | `type`, `message`, `stackTrace` |

## Client / session lifecycle

| Event | When | Payload |
|---|---|---|
| `client_rejected_limit` | An external client connect was rejected (HTTP 429) because the server is at its client limit | `count`, `limit` |
| `client_authentication_failed` | Connect token rejected | `user`, `clientSessionId` |
| `client_joined` | A client/session joins the app session | `user`, `clientSessionId`, `firstSessionOfUser` (no other live session of this user existed), `clientContext` (PascalCase projection: `AuthSessionId`, `Description`, `ProductId`, `VersionId`, `InstallId`, `Locale`, `ContextType`, `UserType`, `PayloadType`) |
| `client_reconnected` | A soft-disconnected session rejoins (reconnect, not a fresh join) | `user`, `clientSessionId` |
| `client_soft_disconnected` | A session is soft-disconnected on transport loss (kept in GlobalState, may reconnect) | `user`, `clientSessionId` |
| `client_left` | A client/session departs definitively — every `client_joined` eventually pairs with exactly one `client_left` | `user`, `clientSessionId`, `lastSessionOfUser` (no other live or soft-disconnected session of this user remains), `reason` (`explicit` / `beacon` / `graceTimeout` / `serverStopped`) |

Successful authentication is not a separate event — it is implied by `client_joined`, which follows it within milliseconds. User-level lifecycle is derived from the client events: `firstSessionOfUser` on `client_joined` marks a user arriving, `lastSessionOfUser` on `client_left` marks a user leaving.

## App lifecycle

| Event | When | Payload |
|---|---|---|
| `app_initialized` | Ikon AI App has finished initialising (functions registered, `Main()` done, persistent storage loaded) — fires immediately before `SignalReadyAsync`. Memory metrics are intentionally omitted: gathering them cost ~15ms on the cold-start path. | `appType`, `initDurationMs`, `functionCount`, `endpointCount` |
| `app_failed` | App initialisation failed unrecoverably (`Main()` threw, or a cell-host spawn failed) — distinct from the process-level `server_failed` | `appType`, `errorType`, `errorMessage`, `stackTrace` |

## Connect latency timeline

`*_connect_timeline` events break a single client-triggered server provision/boot into per-tier timings, stitched together on `connectTraceId`. Each tier is emitted only when the client's connect supplied a trace id.

| Event | Source | Payload |
|---|---|---|
| `hostagent_connect_timeline` | `HostAgent` — one row per client-triggered provision (cold start or warm prestart swap) | `connectTraceId`, `serverSessionId`, `spaceId`, `appBundleId`, `ikonServerReleaseId`, `path` (`cold` / `warm`), `bundleResolveMs`, `bundleCacheHit`, `bundleDownloaded`, `containerOrSwapMs`, `mountsOrStageMs`, `totalMs` |
| `server_connect_timeline` | `IkonServer` — warm `CORE_SERVER_INIT` boot cost attributed to the connect that triggered the prestart swap | `connectTraceId`, `serverSessionId`, `bootPath` (`warm`), `serverInitBlockMs`, `pluginInitMs` |
| `app_connect_timeline` | `Ikon.App` — app-init cost broken down by internal task | `connectTraceId`, `appType`, `initDurationMs`, `ctorMs`, `secretsMs`, `appCreateMs`, `bridgeMs`, `endpointsMs`, `storageLoadMs`, `mainMs` |

## RPC failures

`rpc_*_call_failed` events are emitted only when something goes wrong — a healthy platform produces zero of them. Successful calls are not tracked individually.

| Event | When | Payload |
|---|---|---|
| `rpc_server_call_failed` | A function call was rejected at validation or threw during execution in `FunctionRegistry.Handler` | `callId`, `functionName`, `version` (requested), `versionResolution` (`None` / `Exact` / `Floor` / `Greatest` / `Current` / `Unversioned` / `Other`), `callerSessionId`, `errorKind`, `errorMessage`, `elapsedMs` |
| `rpc_client_call_failed` | An SDK call from `IkonAIConnection.ExecuteWithRetryAsync` succeeded only after a retry, or exhausted all retries and threw. Not emitted on first-attempt success. | `functionName`, `attemptsMade`, `finalOutcome` (`succeeded_after_retry` / `failed`), `lastErrorKind`, `lastErrorMessage`, `totalElapsedMs` |

`errorKind` taxonomy (server side): `Timeout`, `NotFound`, `VersionMismatch`, `PolicyDenied`, `ArgumentBinding`, `Execution`, `InvalidArgument`.

`lastErrorKind` taxonomy (client side): `Timeout`, `ConnectionFailed`, `InstanceNotFound`, `RemoteError`, `IOError`, `Other`.

## Tool errors

Top-level crash markers for the platform's CLI / utility processes. All share the same payload shape: `type`, `message`, `stackTrace`.

| Event | Source |
|---|---|
| `tool_failed` | `IkonTool` (the `ikon` CLI) |
| `build_util_failed` | `BuildUtil` |
| `codegen_docs_gen_failed` | `CodegenDocsGen` |
| `stress_tester_failed` | `StressTester` |
| `proxy_server_failed` | `IkonProxyServer` |

## Wrapped async work — `EventLogHelper`

`Ikon.AI.Utils.EventLogHelper.RunAsync` and `RunAsyncEnumerable` instrument an async operation with a fixed set of events, derived from the `eventName` argument the caller supplies:

- `{eventName}_succeeded`
- `{eventName}_cancelled`
- `{eventName}_failed`

Payload (same for all three): `modelName`, `elapsedSeconds`, plus a caller-defined `additionalFields` object, the completion details, the exception (`errorType`, `errorMessage`, `stackTrace` — on `_failed` only), `isRemote`, `isUserCredential`. Used primarily to wrap LLM and other model-backed calls.

## User data erasure

When a user's data is erased (GDPR erasure — see [User Data Erasure](ikon-user-data-erasure.md)), the backend delivers a durable erasure request to every space the user touched, and the app host raises the `OnUserDataErasure` hook:

```csharp
app.OnUserDataErasure(async userId =>
{
    // Delete app-owned data for this user: rows in your own tables,
    // personal data embedded in Session/Global scoped values.
});
```

Semantics:

- **When it fires** — after the platform has re-erased the user's platform-managed state on the app side (`EraseUserStateAsync` — persistent user-scoped reactives and stored user-scope rows), once per id in the erased user's identity closure (merged accounts included). The user is not connected when it fires and no client/user reactive scope is active.
- **At-least-once delivery** — the request is stored per space on the backend and redelivered on every session start until a run completes without throwing, so a cold or stopped app processes it whenever it next runs. A crash between completing the handler and acknowledging also results in one extra delivery.
- **Idempotency is required** — because delivery is at-least-once, the handler must tolerate running again over already-deleted data (`DELETE ... WHERE user_id = @userId` is naturally idempotent).
- **Failure handling** — let exceptions propagate. A throwing handler leaves the request unacknowledged and it is redelivered on the next session start; swallowing the exception would acknowledge an incomplete erasure.
- **No handler registered** — the platform-managed erasure still runs and the request is acknowledged; the host logs at info that no handler was registered. App-owned data is the app's documented responsibility either way.
