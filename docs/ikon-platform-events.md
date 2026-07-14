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
| `client_authenticated` | Connect token authenticated successfully | `user`, `clientSessionId` |
| `client_authentication_failed` | Connect token rejected | `user`, `clientSessionId` |
| `user_joined` | A user joins the channel (first session) | `user`, `clientSessionId` |
| `client_joined` | A client/session joins the channel | `user`, `clientSessionId` |
| `client_left` | A client/session leaves the channel | `user`, `clientSessionId` |
| `user_left` | A user leaves the channel (last session) | `user`, `clientSessionId` |

## App lifecycle

| Event | When | Payload |
|---|---|---|
| `app_initialized` | Ikon AI App has finished initialising (functions registered, `Main()` done, persistent storage loaded) — fires immediately before `SignalReadyAsync`. Memory snapshot reflects the post-init steady state. | `appType`, `processMemoryMb`, `managedMemoryMb`, `memoryDetails` (single-line GC breakdown from `DiagnosticUtils.BuildMemoryInfo`), `initDurationMs`, `functionCount`, `webhookCount` |

## RPC failures

`rpc_*_call_failed` events are emitted only when something goes wrong — a healthy platform produces zero of them. Successful calls are not tracked individually.

| Event | When | Payload |
|---|---|---|
| `rpc_server_call_failed` | A function call was rejected at validation or threw during execution in `FunctionRegistry.Handler` | `callId`, `functionName`, `version` (requested), `versionResolution` (`Exact` / `Greatest` / `Unversioned` / `Other` / `None`), `callerSessionId`, `errorKind`, `errorMessage`, `elapsedMs` |
| `rpc_client_call_failed` | An SDK call from `IkonAIConnection.ExecuteWithRetryAsync` succeeded only after a retry, or exhausted all retries and threw. Not emitted on first-attempt success. | `functionName`, `attemptsMade`, `finalOutcome` (`succeeded_after_retry` / `failed`), `lastErrorKind`, `lastErrorMessage`, `totalElapsedMs` |

`errorKind` taxonomy (server side): `Timeout`, `NotFound`, `VersionMismatch`, `PolicyDenied`, `ArgumentBinding`, `Execution`, `InvalidArgument`.

`lastErrorKind` taxonomy (client side): `Timeout`, `ConnectionFailed`, `InstanceNotFound`, `RemoteError`, `IOError`, `Other`.

## Tool errors

Top-level crash markers for the platform's CLI / utility processes. All share the same payload shape: `type`, `message`, `stackTrace`.

| Event | Source |
|---|---|
| `tool_failed` | `IkonTool` (the `ikon` CLI) |
| `build_util_failed` | `BuildUtil` |
| `stress_tester_failed` | `StressTester` |
| `proxy_server_failed` | `IkonProxyServer` |

## Wrapped async work — `EventLogHelper`

`Ikon.AI.Utils.EventLogHelper.RunAsync` and `RunAsyncEnumerable` instrument an async operation with a fixed set of events, derived from the `eventName` argument the caller supplies:

- `{eventName}_succeeded`
- `{eventName}_cancelled`
- `{eventName}_failed`

Payload (same for all three): `modelName`, `elapsedSeconds`, plus a caller-defined `additionalFields` object, the completion details, the exception (on `_failed` only), `isRemote`, `isUserCredential`. Used primarily to wrap LLM and other model-backed calls.
