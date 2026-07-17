# Ikon Platform Events

Structured events emitted by IkonServer and the core Ikon libraries via `Log.Instance.Event(name, payload)`. They are routed through `LogEventSender` to the Ikon backend and intended for consumption by external monitoring/analytics tooling. Events from the legacy `AIAgentPlugin` and from vertical extensions (Velory, Learning, TalkToData, etc.) are out of scope here.

## Server lifecycle

| Event | When | Payload |
|---|---|---|
| `server_initializing` | Core server starts initialising | `isPrestart`, `isHosted` |
| `server_warmup_complete` | Prestart JIT warmup finished | `elapsedTimeMs` |
| `server_initialized` | `CORE_SERVER_INIT` message processing finished — plugins + extensions loaded | `durationMs` (block duration), `elapsedTimeMs` (since CoreServer.InitTime) |
| `server_plugins_initialized` | All plugins initialised | `elapsedTimeMs` |
| `server_extensions_initialized` | All server extensions initialised | `elapsedTimeMs` |
| `server_started` | Server is fully started and accepting traffic | `elapsedTimeMs` |
| `server_stopped` | Server has shut down cleanly | `stopDurationMs` |
| `server_failed` | Uncaught exception in the server entry point | `type`, `message`, `stackTrace` |

## Client / session lifecycle

| Event | When | Payload |
|---|---|---|
| `client_rejected_limit` | An external client connect was rejected (HTTP 429) because the server is at its client limit | `count`, `limit` |
| `client_authenticated` | Connect token authenticated successfully | `user`, `clientSessionId` |
| `client_authentication_failed` | Connect token rejected | `user`, `clientSessionId` |
| `user_joined` | A user joins the channel (first session) | `user`, `clientSessionId` |
| `client_joined` | A client/session joins the channel | `user`, `clientSessionId`, `clientContext` (PascalCase projection: `AuthSessionId`, `Description`, `ProductId`, `VersionId`, `InstallId`, `Locale`, `ContextType`, `UserType`, `PayloadType`) |
| `client_reconnected` | A soft-disconnected session rejoins (reconnect, not a fresh join) | `user`, `clientSessionId` |
| `client_soft_disconnected` | A session is soft-disconnected (kept in GlobalState, may reconnect) | `user`, `clientSessionId` |
| `client_left` | A client/session leaves the channel | `user`, `clientSessionId` |
| `user_left` | A user leaves the channel (last session) | `user`, `clientSessionId` |

## App lifecycle

| Event | When | Payload |
|---|---|---|
| `app_initialized` | Ikon AI App has finished initialising (functions registered, `Main()` done, persistent storage loaded) — fires immediately before `SignalReadyAsync`. Memory metrics are intentionally omitted: gathering them cost ~15ms on the cold-start path. | `appType`, `initDurationMs`, `functionCount`, `endpointCount` |

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
