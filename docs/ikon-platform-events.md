# Ikon Platform Events

Structured analytics events the platform records as your app runs — servers starting, clients
joining and leaving, apps initialising, calls failing, models being invoked. Your app can add its
own with `Log.Instance.Event(name, payload)`, and they appear alongside these.

Events from the legacy `AIAgentPlugin`, from first-party apps (Studio, Velory, Learning,
TalkToData, …) and from vertical extensions are out of scope.

## Reading your events

Events are scoped to a space, and you can read your own three ways:

| | |
|---|---|
| `ikon` CLI | `ikon app events` — defaults to the current app project's space and the last 7 days. `--days` or `--start`/`--end`, `--all` to page through everything, `--format table\|json\|csv`, `--output <file>` |
| Portal | the space's **Events** page — a range picker (24 hours to 90 days), a name filter, per-event detail, and CSV export of the whole filtered range rather than just the page on screen |
| REST | `GET /spaces/:id/events` — `days` (default 7) or `start`/`end`, `eventName` substring filter, `limit`, cursor-paginated |

Every event carries a timestamp, its name, the space and session it belongs to, and a `parameters`
object. Every payload field named below is a key inside `parameters`.

The space and session are taken from your auth token, never from the request body, so an event
cannot claim to belong to a space that did not emit it.

**Parameters that look like personal data are withheld.** Any parameter whose key resembles an
email, phone number, national id, date of birth, password, token, API key, card number, bank account
or postal address — or that you explicitly tag `{ sensitive: true, value }` — is stripped from the
event you read back and retained separately under restricted access for a short period. Tagging a
value can only add protection, never remove it: a key matching the pattern is withheld whether you
tag it or not. Keep personal data out of event parameters regardless; this is a backstop, not a
feature to rely on.

No event records a client IP address. Where a client connected from is reported as a country, region
and city by `client_geo` below, resolved at the network edge — the address itself is never read into
the platform, so it cannot appear in an event or be retained anywhere.

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
| `client_joined` | A client/session joins the app session | `user`, `clientSessionId`, `firstSessionOfUser` (no other live session of this user existed), `clientContext` (see below) |
| `client_reconnected` | A soft-disconnected session rejoins (reconnect, not a fresh join) | `user`, `clientSessionId` |
| `client_soft_disconnected` | A session is soft-disconnected on transport loss (kept in GlobalState, may reconnect) | `user`, `clientSessionId` |
| `client_left` | A client/session departs definitively — every `client_joined` eventually pairs with exactly one `client_left` | `user`, `clientSessionId`, `lastSessionOfUser` (no other live or soft-disconnected session of this user remains), `reason` (`explicit` / `beacon` / `graceTimeout` / `serverStopped`) |

`clientContext` is a PascalCase projection of the connecting client's `Context`: `AuthSessionId`,
`Description`, `ProductId`, `VersionId`, `InstallId`, `Locale`, `Timezone`, `ContextType`,
`UserType`, `PayloadType`.

It is a deliberate subset — the `Context` also carries `UserAgent`, `ClientType`, `DeviceId`,
viewport dimensions and more, none of which are projected. Every value in it originates from
`ClientEnvironment`, which the client sends unsigned and which the server copies verbatim: it is
what the client *says about itself*, never an attested fact. `Timezone` (an IANA zone name such as
`Europe/Helsinki`) is therefore a coarse geography hint, not a location — a VPN, a misconfigured
device or a client that simply lies all report the wrong one.

Successful authentication is not a separate event — it is implied by `client_joined`, which follows
it within milliseconds. User-level lifecycle is derived from the client events: `firstSessionOfUser`
on `client_joined` marks a user arriving, `lastSessionOfUser` on `client_left` marks a user leaving.

## App lifecycle

| Event | When | Payload |
|---|---|---|
| `app_initialized` | Ikon AI App has finished initialising (functions registered, `Main()` done, persistent storage loaded) — fires immediately before `SignalReadyAsync`. Memory metrics are intentionally omitted: gathering them cost ~15ms on the cold-start path. | `appType`, `initDurationMs`, `functionCount`, `endpointCount` |
| `app_failed` | App initialisation failed unrecoverably (`Main()` threw, or a cell-host spawn failed) — distinct from the process-level `server_failed` | `appType`, `errorType`, `errorMessage`, `stackTrace` |

## Client placement

| Event | When | Payload |
|---|---|---|
| `client_geo` | A client opened an app session, and the network edge was able to place it | `appSession`, `authSession`, `country` (ISO 3166-1 alpha-2), `subdivision`, `city`, `rttMsec` |

Resolved by the load balancer from the connection itself, so **the client's IP address is never read
by the platform** — only the placement it implies is recorded. Nothing is emitted when the edge could
not place the client, or for a request that did not reach us through it (local development, for
instance), so absence of a row is normal and is not a failure.

`rttMsec` is the round-trip time the edge measured to the client. It describes the network, not your
app, and is the one field here that says anything about connection quality.

Placement is approximate. A VPN, a corporate egress or a mobile carrier gateway all report the
network's location rather than the person's, and city is materially less reliable than country. It
describes where a client *appears* to be and must never be used to decide what anyone is allowed to
do.

One row per app-session open, keyed by `appSession` — that is what the session views join on, and
why the placement follows a client who moves between sessions instead of being fixed at login.

## Server session provisioning

| Event | When | Payload |
|---|---|---|
| `ikon_session_started` | The backend has provisioned a server session | `session`, `type`, plus per-type fields |

`type` selects the shape of the rest:

| `type` | When | Extra fields |
|---|---|---|
| `app-session` | A server was provisioned for an app session | `appSession`, `ikonServerInstance`, `ikonServerRelease`, `containerImage` (absent when a prestarted server was claimed rather than a container started) |
| `app-session-hosted` | The same, on a host server | `appSession`, `ikonServerInstance`, `ikonServerRelease`, `hostServer`, `containerImage` |
| `ikon-server-prestart-host-agent` | A server was prestarted into the pool, ahead of any app session | `ikonServerInstance`, `ikonServerRelease`, `containerImage` |
| `pipeline-run` | A pipeline run started | `ikonToolRelease`, `ikonToolReleaseVersion`, `serverRuntime` |

`session` is the **ikon server** session; `appSession` is the **app session** it serves. They are
different ids and both appear on the same row.

**Reading rows written before the rename.** "Room" was the old word for an app session. Older rows
carry the id under `room` instead of `appSession`, and `type` values of `room` and `hosted-room`
rather than `app-session` and `app-session-hosted`. A row that has been written can never gain a new
key, so those spellings stay in the raw events forever — the session views translate both, and
nothing built on them shows the old vocabulary. Query the raw events directly and you have to handle
both yourself.

`pipeline-run` also still emits the superseded `toolkitRelease` / `toolkitReleaseVersion` spellings
alongside the `ikonTool*` pair. Prefer the `ikonTool*` keys in anything new.

## Connect latency timeline

`*_connect_timeline` events break a single client-triggered server provision/boot into per-tier
timings, stitched together on `connectTraceId`. Each tier is emitted only when the client's connect
supplied a trace id, so an event with no trace id produces no rows at all.

| Event | Source | Payload |
|---|---|---|
| `client_connect_timeline` | TypeScript SDK in the browser, flushed at the first live UI update | `connectTraceId`, `firstPaintMs`, `cdnHtmlMs`, `domContentLoadedMs`, `authMs`, `initMs`, `connectMs`, `snapshotSeeded` |
| `backend_connect_timeline` | Backend `/init` | `connectTraceId`, `status`, `prestarted`, `pollCount`, `resolveMs`, `profileMs`, `startMs`, `waitMs`, `configMs`, `totalMs`, `serverSessionId` |
| `hostagent_connect_timeline` | `HostAgent` — one row per client-triggered provision (cold start or warm prestart swap) | `connectTraceId`, `serverSessionId`, `spaceId`, `appBundleId`, `ikonServerReleaseId`, `path` (`cold` / `warm`), `bundleResolveMs`, `bundleCacheHit`, `bundleDownloaded`, `containerOrSwapMs`, `mountsOrStageMs`, `totalMs` |
| `server_connect_timeline` | `IkonServer` — warm `CORE_SERVER_INIT` boot cost attributed to the connect that triggered the prestart swap | `connectTraceId`, `serverSessionId`, `bootPath` (`warm`), `serverInitBlockMs`, `pluginInitMs` |
| `app_connect_timeline` | `Ikon.App` — app-init cost broken down by internal task | `connectTraceId`, `appType`, `initDurationMs`, `ctorMs`, `secretsMs`, `appCreateMs`, `bridgeMs`, `endpointsMs`, `storageLoadMs`, `mainMs` |

These measure the platform's own startup path. `cdnHtmlMs` covers fetching the **HTML document**
only; no event measures how long an app's images, media or other assets take to reach the browser
from the CDN.

`server_connect_timeline` and `app_connect_timeline` exist only on the warm prestart-swap path, so a
cold connect stitches at most three tiers.

## RPC / function calls

`rpc_*_call_failed` events are emitted only when something goes wrong — a healthy platform produces
zero of them. Successful calls are not tracked individually.

| Event | When | Payload |
|---|---|---|
| `rpc_server_call_failed` | A function call was rejected at validation, or threw while executing | `callId`, `functionName`, `version` (requested), `versionResolution`, `callerSessionId`, `errorKind`, `errorMessage`, `elapsedMs` |
| `rpc_client_call_failed` | An SDK call succeeded only after a retry, or exhausted its retries and threw. Not emitted on first-attempt success. | `functionName`, `attemptsMade`, `finalOutcome` (`succeeded_after_retry` / `failed`), `lastErrorKind`, `lastErrorMessage`, `totalElapsedMs` |
| `rpc_server_version_resolved` | A hosted function version served a call for the first time, once per version and calling space. Hosts that register no versions stay silent, so an ordinary app emits none. | `functionName`, `version` (requested), `hostedVersion`, `versionResolution`, `callerSpaceId`, `callerSessionId` |

`versionResolution` taxonomy: `None`, `Exact`, `Floor`, `Greatest`, `Current`, `Unversioned`,
`Other`. `Floor` means the caller asked for something older than anything hosted.

`errorKind` taxonomy (server side): `Timeout`, `NotFound`, `VersionMismatch`, `PolicyDenied`,
`ArgumentBinding`, `Execution`, `InvalidArgument`.

`lastErrorKind` taxonomy (client side): `Timeout`, `ConnectionFailed`, `InstanceNotFound`,
`RemoteError`, `IOError`, `Other`.

## Resource and health

| Event | When | Payload |
|---|---|---|
| `ikon_server_oom` | A server ran out of memory and was killed. Reported from outside the dying process, so it survives even a hard kill. | `ikonServerId`, `spaceId`, `appBundleId`, `ikonServerReleaseId`, `memoryLimitMb`, `peakMemoryUsageBytes`, `exitCode`, `failureCategory`, `failureSubCategory`, `cgroupMemoryLimited`, `isStartupFailure`, `uptimeMs` |
| `oom_recovered` | The in-process memory guard recovered from memory pressure and the process survived. Once per process lifetime, so a thrashing spike cannot flood analytics. | `heapSizeBytes`, `totalAvailableMemoryBytes`, `memoryLoadBytes`, `highMemoryLoadThresholdBytes`, `recoveriesInWindow`, `maxRecoveriesPerWindow`, `recoveryWindowSeconds`, `processMemoryBytes`, `containerMemoryLimitBytes`, `memoryInfo` |
| `host_server_needs_recycle` | A host server decided it must be recycled | `hostServerSessionId`, `reason` |
| `legacy_usage_observed` | A deprecated code path was reached. Deduplicated per feature, detail and calling space, so it reports first contact, not call volume. | `feature`, `detail`, `sessionId`, `callerSpaceId` |

## AI and model operations — `EventLogHelper`

`Ikon.AI.Utils.EventLogHelper.RunAsync` and `RunAsyncEnumerable` instrument an async operation with
a fixed set of events, derived from the `eventName` argument the caller supplies:

- `{eventName}_succeeded`
- `{eventName}_cancelled`
- `{eventName}_failed`

Payload (same for all three): `modelName`, `elapsedSeconds`, plus a caller-defined
`additionalFields` object, the completion details, the exception (`errorType`, `errorMessage`,
`stackTrace` — on `_failed` only), `isRemote`, `isUserCredential`. For LLM calls the completion
details carry the token counts (`InputTokens`, `OutputTokens`, `InputCachedTokens`,
`OutputReasoningTokens`, `FinishReason`, …).

The prefixes in use are `llm`, `classification`, `embedding_generation`, `reranking`,
`image_generation`, `image_segmentation`, `image_upscaling`, `depth_estimation`, `mesh_generation`,
`music_generation`, `sound_effect_generation`, `speech_generation`, `speech_recognition`, `ocr`,
`video_generation`, `video_enhancement`, `file_conversion`, `web_scraping` and `web_searching` — so
57 concrete event names.

## Users and authentication

Emitted by the backend, not by the server.

| Event | When | Payload |
|---|---|---|
| `user_created` | A new user account was created | `user` |
| `user_login` | A user authenticated | `authSession`, `user`, `provider` |
| `user_logout` | A user logged out | `user` |
| `user_removed` | A user account was removed | `user` |
| `user_token_delegated` | A user token was delegated to another space | `authSession`, `user`, `role`, `sourceSpace` |
| `login_email_request` | A magic-link login email was requested. No payload — the address would be personal data. | — |
| `anonymous_user_created` | An anonymous user was created | `user` |
| `anonymous_user_login` | An anonymous user authenticated | `authSession`, `user` |
| `anonymous_user_removed` | An anonymous user was removed | `user` |

`provider` on `user_login`: `username`, `google`, `email`, `passkey`, `api-key`, `space-token`.

`authSession` is what ties a login to the sessions that follow it — it is also on `client_joined`'s
`clientContext.AuthSessionId`.

## Profiles

| Event | When | Payload |
|---|---|---|
| `profile_created` | A profile was created in a space | `user`, `role` |
| `profile_updated` | A profile changed | `user`, `role` |
| `profile_removed` | A profile was removed | `user` |
| `lead_created` | A profile was forwarded to a CRM integration | `user`, `integration` |

## Billing

Organisation-scoped, emitted by the backend. All carry `organisation`.

| Event | When | Payload |
|---|---|---|
| `billing_checkout_created` | A checkout session was opened | `paymentProvider`, `session`, `customer`, `planType`, `mode` |
| `billing_checkout_completed` | Checkout completed | `paymentProvider`, `session`, `customer`, `paymentIntent`, `amountPaid`, `currency`, `product`, `price`, `planType`, `tier`, `paymentStatus` |
| `billing_checkout_failed` | Payment failed | `paymentProvider`, and the failure details |
| `billing_async_payment_completed` | A deferred payment method settled later | `paymentProvider`, and the settlement details |
| `billing_invoice_paid` | An invoice was paid | the invoice and period details |
| `billing_credits_added` | Credits were added to an organisation | `source`, `credits`, `subscriptionRemaining`, `purchasedRemaining` |
| `billing_credits_reset` | The monthly credit allowance was reset | the subscription and period details |
| `billing_subscription_updated` | A subscription changed | `subscription`, and the change details |
| `billing_subscription_cancelled` | A subscription was cancelled | `subscription`, and the cancellation details |
| `billing_subscription_reconciled` | A subscription was reconciled against the payment provider | `subscription`, and the reconciliation details |

## Tool errors

Top-level crash markers for the platform's CLI / utility processes. All share the same payload
shape: `type`, `message`, `stackTrace`.

| Event | Source |
|---|---|
| `tool_failed` | `IkonTool` (the `ikon` CLI) |
| `build_util_failed` | `BuildUtil` |
| `codegen_docs_gen_failed` | `CodegenDocsGen` |
| `stress_tester_failed` | `StressTester` |
| `proxy_server_failed` | `IkonProxyServer` |
| `host_server_failed` | `IkonHostServer` |

## Metered usage is not an event

Consumption — tokens spent, images generated, database seconds, egress — is metered separately and
is what billing and cost reporting are based on. It does not appear in your events, so an event
query is the wrong place to look for it. Use the cost and usage views in the Portal instead.

The AI events below are the nearest equivalent: `llm_succeeded` and its siblings record that a model
call happened and how long it took, and carry token counts in their completion details. Treat those
as diagnostics, not as a billing record.

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
