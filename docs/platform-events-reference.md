<!-- checked-against: 8cf841c2219035384fb1f942 -->

# IKON Platform Events

Structured analytics events the platform records as your app runs — servers starting, clients
joining and leaving, apps initialising, calls failing, models being invoked. Your app can add its
own with `Log.Instance.Event(name, payload)`, and they appear alongside these.

Events from the legacy `AIAgentPlugin`, from first-party apps (Studio, Velory, Learning,
TalkToData, …) and from vertical extensions are out of scope.

## Reading your events

Events are scoped to an app, and you can read your own three ways:

| | |
|---|---|
| `ikon` CLI | `ikon events` — defaults to the app in the current directory and the last 7 days. `--days` (1-90) or `--start`/`--end`, `--event-name` substring filter, `--limit` page size (1-10000, default 1000), `--all` to page through everything, `--format text\|json\|csv`, `--output <file>` |
| Portal | the app's **Events** page — a range picker (24 hours to 90 days), a name filter, per-event detail, and CSV export of the whole filtered range rather than just the page on screen, capped at the first 10000 events |
| REST | `GET /spaces/:id/events`, the id being the app's — `days` (1-90, default 7) or `start`/`end`, `eventName` substring filter, `limit`, cursor-paginated |

Each event is deleted once its retention period has passed, so a range reaching further back returns
only what is still kept:

| Events | Kept for |
|---|---|
| Your app's own events (`Log.Instance.Event`), and any name this page does not list | 90 days |
| Visits and requests: `client_joined`, `client_left`, `ikon_session_started`, `screen_opened`, `endpoint_calls`, `mcp_tool_called`, `cron_ran`, `message_received`, `call_received` | 90 days. Insights in Studio, the Portal's traffic view and `ikon traffic` keep anonymous daily totals of them for longer |
| Where clients connected from: `client_geo`, `app_page_served` | 90 days |
| Sign-ins, accounts and profiles | 13 months |
| Billing | Kept with the billing records |
| The platform's diagnostics: every other event on this page | 30 days |

Every event carries a timestamp, its name, the app and session it belongs to, and a `parameters`
object. Every payload field named below is a key inside `parameters`.

The app and session are taken from your auth token, never from the request body, so an event
cannot claim to belong to an app that did not emit it.

**Parameters that look like personal data are withheld.** A top-level parameter is stripped from the
event you read back, and kept separately under restricted access for a short period, when its key
has a `.`/`_`/`-`-delimited segment naming an email, phone number (`phone`, `msisdn`), national id
(`ssn`, `nationalid`, `personalid`), date of birth, password, secret, token, API key, card number
(`cardnumber`, `creditcard`/`credit_card`), IBAN or postal/street address, or when you explicitly
tag it `{ sensitive: true, value }`. The segment must be one of these spellings whole, so
`card_number`, `national_id` or `bank_account` is not matched. Tagging a value can only add
protection, never remove it: a key matching the pattern is withheld whether you tag it or not. Keep
personal data out of event parameters anyway, and do not rely on this filter to catch it.

No event records a client IP address. Where a client connected from is reported as a country, region
and city by `client_geo` below, resolved at the network edge without the platform reading the
address. The platform does read client addresses elsewhere — audit records and security logs keep
them — but never into an event.

## Reading a failure event

Most events that report something going wrong carry a `class` parameter, so you can separate the
ones that need someone's attention from the ones that are the platform working as designed. The name
of an event tells you what happened; `class` tells you whose problem it is.

| `class` | Meaning | What to do |
|---|---|---|
| `user_error` | The caller asked for something impossible or malformed, and was told so | Nothing — this is the platform refusing bad input |
| `expected` | Not a failure: a confirmation the caller has to give, a deliberate stop, a retry that then succeeded, a limit enforced on purpose | Nothing |
| `dependency` | Something we call refused or did not answer — a model provider, the backend, a spawned toolchain | Watch the rate; act when it moves |
| `provider_credit` | A provider took the credential and refused for lack of credit — the account behind the key has run out | Top the account up. Waiting does not clear it |
| `defect` | An unhandled exception reached the top of a process | Fix it. This bucket should be near zero |

**Count `defect`, `dependency` and `provider_credit`; ignore the rest.** A dashboard built on event
*names* alone counts a missing `--yes` flag the same as a crash.

A few failure events carry no `class` — `client_rejected_limit` and
`client_authentication_failed` — so count those by name.

`provider_credit` is separate from `dependency` because the two need different people: a dependency
failure is watched until it clears on its own, and a credit failure clears only when somebody pays.
`provider_credit` is about an account the platform holds with a model provider. Your own IKON credit
limit is a limit enforced on purpose, and reports as `expected`.

A model-operation failure event also carries `failureKind`, which says the same thing one level finer
(`Transient`, `Unavailable`, `AccessDenied`, `CreditExhausted`, `NotAllowed`, `Quality`, `Unknown`) and matches
`Ikon.AI.ModelFailureKind`.

Your own events can join the same taxonomy — `Ikon.Common.Core.EventFailureClass` holds the
values as constants, so a failure your app records reads the same way as one the platform did:

<!-- ikon-example: platform-events-own-failure -->
```csharp
Log.Instance.Event("invoice_export_failed", new
{
    @class = EventFailureClass.Dependency,
    invoiceId,
    errorMessage = ex.Message,
});
```

## Server lifecycle

Kept 30 days.

| Event | When | Payload |
|---|---|---|
| `server_initializing` | Core server starts initialising | `isPrestart`, `isHosted` |
| `server_warmup_complete` | Prestart JIT warmup finished | `elapsedTimeMs` |
| `server_initialized` | `CORE_SERVER_INIT` message processing finished — plugins + extensions loaded | `durationMs` (block duration), `pluginsInitMs`, `extensionsInitMs` |
| `server_started` | Server is fully started and accepting traffic | `elapsedTimeMs` |
| `server_stopped` | Server has shut down cleanly | `stopDurationMs` |
| `server_failed` | Uncaught exception in the server entry point | `class` (`dependency` when the backend, the network or a database failed under it, else `defect`), `type`, `message`, `stackTrace` |

## Client / session lifecycle

`client_joined` and `client_left` are kept 90 days, the others 30 days.

| Event | When | Payload |
|---|---|---|
| `client_rejected_limit` | An external client connect was rejected (HTTP 429) because the server is at its client limit | `count`, `pending`, `limit` |
| `client_authentication_failed` | The connect token was valid but the server's `OnAuthenticateAsync` refused the client (a rejected token gets a 401, or a 503 while the server is still starting, and no event) | `user`, `clientSessionId` |
| `client_joined` | A client/session joins the app session | `user`, `clientSessionId`, `firstSessionOfUser` (no other live session of this user existed), `clientContext` (see below) |
| `client_reconnected` | A soft-disconnected session rejoins (reconnect, not a fresh join) | `user`, `clientSessionId` |
| `client_soft_disconnected` | A session is soft-disconnected on transport loss (kept in GlobalState, may reconnect) | `user`, `clientSessionId` |
| `client_left` | A client/session departs definitively — every `client_joined` eventually pairs with exactly one `client_left` | `user`, `clientSessionId`, `lastSessionOfUser` (no other live or soft-disconnected session of this user remains), `reason` (`explicit` / `beacon` / `graceTimeout` / `serverStopped` / `replaced`, when a local run replaces its app instance on a hot reload or a session-identity change / `sessionReplaced`, when the client started a new session in place of this one) |

`clientContext` is a PascalCase projection of the connecting client's `Context`: `AuthSessionId`,
`Description`, `ProductId`, `VersionId`, `InstallId`, `Locale`, `Timezone`, `ContextType`,
`UserType`, `PayloadType`.

It is a deliberate subset — the `Context` also carries `UserAgent`, `ClientType`, `DeviceId`,
viewport dimensions and more, none of which are projected. Three of its values come from the signed
connect token the backend minted — `AuthSessionId`, `ContextType` and `UserType` — and are attested.
The rest (`Description`, `ProductId`, `VersionId`, `InstallId`, `Locale`, `Timezone`,
`PayloadType`) originate from `ClientEnvironment`, which the client sends unsigned and which the
server copies verbatim: they are what the client *says about itself*, never an attested fact.
`Timezone` (an IANA zone name such as `Europe/Helsinki`) is therefore a coarse geography hint, not
a location — a VPN, a misconfigured device or a client that simply lies all report the wrong one.

Successful authentication is not a separate event — it is implied by `client_joined`, which follows
it within milliseconds. User-level lifecycle is derived from the client events: `firstSessionOfUser`
on `client_joined` marks a user arriving, `lastSessionOfUser` on `client_left` marks a user leaving.

## App lifecycle

Kept 30 days.

| Event | When | Payload |
|---|---|---|
| `app_initialized` | The IKON app has finished initialising (functions registered, `Main()` done, persistent storage loaded) — fires immediately before `SignalReadyAsync`. Memory metrics are intentionally omitted: gathering them is synchronous work on the cold-start path, which delays readiness on a single-core container. | `appType`, `initDurationMs`, `functionCount`, `endpointCount` |
| `app_failed` | App initialisation failed unrecoverably (`Main()` threw, a cell-host spawn failed, the app's seed run threw, or an `app.OnStarting` handler threw) — distinct from the process-level `server_failed` | `class` (as on `server_failed`), `appType`, `errorType`, `errorMessage`, `stackTrace` |

## Page arrivals

Kept 90 days.

| Event | When | Payload |
|---|---|---|
| `app_page_served` | Periodic count of pages the frontend router served, one row per country | `tenant`, `country`, `served`, `doNotTrack`, `rttMsecSum`, `rttSamples`, `windowSeconds` |

Your app's `index.html` is served `no-store`, so **every** page load reaches the platform even when
the CDN holds every other file. That makes this a count of everyone who arrived — including the
visitors who gave up before your app started and so never emitted `client_joined` or anything else.
The gap between `app_page_served` and `client_joined` is how many you are losing during the load.

The event is aggregated over a window rather than emitted per page, so `served` is a count and not a row per
visitor. `rttMsecSum` and `rttSamples` are summed rather than averaged so they stay correct when you
re-aggregate across countries or days: divide the two yourself, at whatever grouping you want. Not
every request can be measured, which is why the sample count is separate from `served`.

Only page loads are counted: a `GET` the browser marks as loading a page in the window or in a
frame (`Sec-Fetch-Dest: document` or `iframe`), and not a prefetch or prerender. A browser too old
to send that header (Safari before 16.4, Chrome before 76, Firefox before 90) is not counted.
Crawlers are excluded too. A large share of what reaches the router is vulnerability scanners and
scripted clients, and counting them would inflate the denominator of any completion rate built on
this.

`doNotTrack` is how many of the `served` page loads sent `DNT: 1` or `Sec-GPC: 1`. They are counted
in `served` like any other, but their pages send no boot report, so subtract them before comparing
`served` with `app_page_booted`.

`country` is resolved at the network edge from the connection, never from an IP address, and is
`unknown` when the edge could not place the client. `tenant` is your app's tenant name, the label of
its platform subdomain, even when the page was served on a custom domain; the app's id is on the row
itself.

## Page boots

Kept 30 days.

| Event | When | Payload |
|---|---|---|
| `app_page_booted` | Periodic count of how far the pages the frontend router served got, one row per outcome, browser and country | `tenant`, `country`, `browser`, `stage`, `ended`, `error`, `unsupported`, `pageCounted`, `count`, `windowSeconds` |

The router adds a small inline script to every `index.html` it serves. The script sends one report
per page load: when the app's UI shows, when the page is hidden or closed first, or after 30 seconds
if neither happened. `app_page_served` counts the pages that arrived. `app_page_booted` counts what
happened to them, including the pages where your bundle never ran, so nothing else could report.

| Field | Values |
|---|---|
| `stage` | How far the page got: `html` (the bundle never ran), `bundle` (it ran but asked the backend for no session), `sign-in` (it showed the sign-in screen and did not connect), `connect` (it asked for a session, but no UI showed), `shown` |
| `ended` | `shown`, `hidden` (the visitor hid or left the page first), `stuck` (still visible after 30 seconds without the UI) |
| `error` | The first failure the page saw: `exception`, `rejection` (an unhandled promise rejection), `script-load`, `stylesheet-load`, or empty |
| `unsupported` | The browser lacks `WebSocket`, `fetch` or `BigInt`, which the SDK needs |
| `browser` | `chrome`, `safari`, `firefox`, `edge`, `samsung`, `opera` or `other`, from the user agent |
| `pageCounted` | The browser sends the headers `app_page_served` needs, so the page load is in `served`. It is `false` for the old browsers that `app_page_served` leaves out |

`shown` means the first UI update from your app's server arrived. An older SDK does not report that,
so for its pages `shown` means the page drew something in `#root`. `sign-in` needs an
`@ikonai/sdk-react-ui` that reports the sign-in screen; with an older one, a visitor who stays on the
sign-in screen is reported at `bundle`.

Visitors who send `DNT: 1` or `Sec-GPC: 1` are not reported, and neither is a page the browser
prerendered or one whose browser lacks `navigator.sendBeacon`. A report the browser never delivers is
lost too, so the reports with `pageCounted` true add up to a little less than `served` minus
`doNotTrack`. Only pages the frontend router serves carry the script, so an app embedded elsewhere, a
local `ikon run` and a Flutter app send no boot reports.

## Screens and entries

Kept 90 days, `web_vitals` 30 days. The daily totals Insights shows are kept after that.

What Insights › Traffic in Studio counts: which screens the app's clients open and which ways
into the app are used. None of these rows names a person, and the `user` field that
`client_joined` carries is deliberately absent here. A visit is one browser tab. The web client
names its tab with `tabId`, a random id that stays the same across reloads in that tab and is
different in another tab, so a reload or a reconnect is not a new visit. A client with no tab (the
phone app) is counted by the session id its server gave it, which is new every session.

A visitor who asked not to be tracked (the browser's Do Not Track setting or the Global Privacy
Control signal, which the SDK sends as `ClientEnvironment.DoNotTrack`) produces no `screen_opened`
or `web_vitals` at all, and sends no tab or referrer. Their `client_connect_timeline` is still
sent, because it is how the platform finds connects that fail. It carries `doNotTrack: true` in
place of `tabId` and none of the hardware or network details, and no figure an app owner sees
includes it.

| Event | When | Payload |
|---|---|---|
| `screen_opened` | A client's recorded path changed: it followed a link or a `path:` control, used back or forward, or the app moved it with `Navigation.SetPathAsync` and the client acknowledged the move. The first screen of a session counts too, once. Emitted by the app's server for every kind of client, so the web and the phone app count the same way | `clientSessionId`, `tabId` (the web client's tab; absent for other clients), `path` (query string removed), `from` (the previous path, or null for the first screen), `referrer` (first screen only: the origin of the site that linked to the page, never its page; absent when the browser named none or the link came from the app itself), `clientType` (`DesktopWeb`, `MobileWeb`, `MobileApp`, `DesktopApp`, `Unknown`), `contextType`, `sdk`, `surface`, `country` (as `client_geo` places it; empty when not placed) |
| `web_vitals` | The web visitor's Core Web Vitals, as the browser measured them with no script of ours on the frame. Sent once per page load that showed the app, at the earlier of ten seconds after the first paint or the page being hidden | `tabId`, `lcpMs` (largest contentful paint), `clsScore` (cumulative layout shift, unexpected shifts only), `inpMs` (the slowest interaction to next paint); a vital the browser could not measure is left out |
| `endpoint_calls` | The app's HTTP endpoints answered calls. One row per method, route and status class every five minutes in which there were calls, and one more when the app stops | `method`, `route` (the declared route, so an id in the path groups with its siblings), `statusClass` (`1xx` … `5xx`, or `other` for a status outside them), `count`, `durationMsSum`, `durationMsMax`, `windowSeconds` (how long the row covers) |
| `mcp_tool_called` | An MCP tool of the app ran for an agent | `tool`, `outcome` (`ok` / `failed` / `cancelled`), `durationMs`; never the arguments |
| `cron_ran` | A `[Cron]` function ran on its schedule | `function`, `outcome` (`ok` / `failed`), `durationMs` |
| `message_received` | A text message reached the app's number, as a `TriggerEventType.SmsReceived` event | `handled` (always true: the event reaches only an app that declares a handler); never the number or the text |
| `call_received` | A phone call reached the app's number | `handled` (whether the app answers calls); never the numbers |

The platform's own snapshot capture and internal connections open no screens. A path is recorded
as the client sent it, so a screen whose path carries an id (`/orders/8731`) is one row per id; an
app that wants them grouped puts the id in the query string, which is removed, or reads
`Navigation.PathChangedAsync` and counts its own `Log.Instance.Event`.

`web_vitals` is sent through the same authenticated channel as the SDK's logs, so it carries the
space and the session like every other row. The Flutter SDK sends no `web_vitals`, tab or referrer,
because a phone app has no browser to measure and no page that links to it. Its screens reach
`screen_opened` through the same path report the web client makes.

## Client placement

Kept 90 days.

| Event | When | Payload |
|---|---|---|
| `client_geo` | A client opened an app session, and the network edge was able to place it | `appSession`, `authSession`, `country` (ISO 3166-1 alpha-2), `subdivision`, `city`, `rttMsec` |

Resolved by the load balancer from the connection itself, so **the client's IP address is not read
to produce it** — only the placement it implies is recorded. Nothing is emitted when the edge could
not place the client, or for a request that did not reach us through it (local development, for
instance), so absence of a row is normal and is not a failure.

`rttMsec` is the round-trip time the edge measured to the client. It describes the network, not your
app, and is the one field here that says anything about connection quality.

Placement is approximate. A VPN, a corporate egress or a mobile carrier gateway all report the
network's location rather than the person's, and city is materially less reliable than country. It
describes where a client *appears* to be and must never be used to decide what anyone is allowed to
do.

There is one row each time an app session is opened, keyed by `appSession`, which is what the
session views join on. A client who moves between sessions therefore gets a placement for each
session instead of one fixed at sign-in.

## Server session provisioning

Kept 90 days.

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
key, so those spellings stay in the raw events permanently. The session views translate both, so nothing built
on them shows the old names. If you query the raw events directly, you have to handle both yourself.

`pipeline-run` also still emits the superseded `toolkitRelease` / `toolkitReleaseVersion` spellings
alongside the `ikonTool*` pair. Prefer the `ikonTool*` keys in anything new.

## Connect latency timeline

Kept 30 days.

`*_connect_timeline` events break a single client-triggered server provision/boot into per-tier
timings, joined on `connectTraceId`. Each tier is emitted only when the client's connect supplied a
trace id, so a connect without a trace id produces no rows at all.

| Event | Source | Payload |
|---|---|---|
| `client_connect_timeline` | TypeScript SDK in the browser, once per connect: when it has connected and its media has settled, when it fails, or when the page closes first | `connectTraceId`, `tabId` (or `doNotTrack: true` in its place, see [Screens and entries](#screens-and-entries)), `outcome`, `firstPaintMs` (the first live UI arrived, from navigation start), `firstPaintOfPage` (`true` only for the page's first live UI; a later client in the same page measures from the same start), `firstFrameMs` (the start of the frame that shows it; sent by apps on the React SDK), `cdnHtmlMs`, `domContentLoadedMs`, `authMs`, `initMs`, `connectMs`, plus the asset fields below |
| `backend_connect_timeline` | Backend `/init`, one row per request, a failed one included, except a join-only request that finds no live session | `connectTraceId`, `status` (`success`, `abandoned`, `not_running`, `startup_failed`, `server_full`, `insufficient_credits`, `failure`), `prestarted`, `pollCount`, `resolveMs`, `profileMs`, `startMs`, `waitMs`, `configMs`, `totalMs`, `serverSessionId`; `httpStatus` on a request that was answered with an error. A request that failed before it had a session has no `prestarted` or `serverSessionId` |
| `provision_connect_timeline` | Backend, one row per cold start (a server started for the connect because no prestarted one was free) that reached running | `connectTraceId`, `serverSessionId`, `attempt` (`2` when the first attempt failed), and milliseconds since the attempt that succeeded began: `placedMs`, `hostAgentCalledMs`, `readsDoneMs`, `hostAgentReturnedMs`, `portsWrittenMs`, `databaseConnectionsMs`, `entrypointReadyMs`, `serverInitSentMs`, `runningWrittenMs`, `readyHandledMs`, `totalMs` |
| `hostagent_connect_timeline` | `HostAgent` — one row per client-triggered provision (cold start or warm prestart swap) | `connectTraceId`, `serverSessionId`, `spaceId`, `appBundleId`, `ikonServerReleaseId`, `path` (`cold` / `warm`), `bundleResolveMs`, `bundleCacheHit`, `bundleDownloaded`, `containerOrSwapMs`, `totalMs`; a cold start also `releaseResolveMs`, `prepareMs`, `ikonToolMs`, `imageCheckMs`, `listenWaitMs` |
| `server_connect_timeline` | `IkonServer` — the cost of starting the app, attributed to the connect it started for | `connectTraceId`, `serverSessionId`, `bootPath` (`warm` / `cold`), `serverInitBlockMs`, `backendReInitMs`, `bundleWriteMs`, `pluginInitMs`, `extensionsInitMs`, `jitMs`, `bundleOptimized` (`true` when the app was compiled ReadyToRun by `ikon deploy --optimize` or `ikon bundle --optimize`; absent for a bundle whose manifest does not record it); a cold start also `runtimeStartMs`, `listeningMs`, `listeningToServerInitMs` |
| `app_connect_timeline` | `Ikon.App` — app-init cost broken down by internal task | `connectTraceId`, `appType`, `initDurationMs`, `ctorMs`, `protocolMs`, `secretsMs`, `appCreateMs`, `bridgeMs`, `endpointsMs`, `storageLoadMs`, `saverStartMs`, `portsMs`, `onStartingMs`, `mainMs`, `seedMs`, `jitMs`. An app built on older libraries sends fewer of them |

These measure the platform's own startup path. `cdnHtmlMs` covers fetching the **HTML document**
only.

`client_connect_timeline` also summarises everything else the page fetched — your app's own files,
as the browser experienced them:

| Field | Meaning |
|---|---|
| `assetCount` | Assets fetched during the load |
| `assetBytes` | Bytes actually transferred, so a served-from-cache asset adds none |
| `assetSlowestMs` | The single slowest asset |
| `assetP95Ms` | 95th percentile duration, nearest-rank |
| `assetCachedCount` | Served from the browser's own cache |
| `assetOpaqueCount` | Cross-origin assets whose timing the browser withheld |

A non-zero `assetOpaqueCount` means an asset host did not send `Timing-Allow-Origin`, so those
assets are missing from the byte and cache counts. It is reported separately rather than assumed to
be cached, because a withheld timing and a cache hit look identical (both report zero bytes), and
counting them together would show a perfect cache rate for assets that were downloaded in full.

This describes only visitors whose page got as far as asking the backend for a session. A visitor
who closes the page after that is reported with `outcome` `abandoned`, and one who leaves before it
never runs the code that reports it. See `app_page_served` for the count that includes them.

A warm connect has no `provision_connect_timeline`. `jitMs` is the JIT compile time of the whole
server process during the step, so it also counts JIT compilation for other sessions' servers in
the same process.

## RPC / function calls

Kept 30 days.

`rpc_*_call_failed` events are emitted only when something goes wrong — a healthy platform produces
zero of them. Successful calls are not tracked individually.

| Event | When | Payload |
|---|---|---|
| `rpc_server_call_failed` | A function call was rejected at validation, or threw while executing | `class`, `callId`, `functionName`, `version` (requested), `versionResolution`, `callerSessionId`, `errorKind`, `errorMessage`, `elapsedMs` |
| `rpc_client_call_failed` | An SDK call succeeded only after a retry, or threw — after its retries ran out, or on its first attempt when the error was not one to retry. Not emitted on first-attempt success. | `class`, `callId`, `functionName`, `attemptsMade`, `finalOutcome` (`succeeded_after_retry` / `failed`), `lastErrorKind`, `lastErrorMessage`, `totalElapsedMs` |
| `rpc_server_version_resolved` | A hosted function version served a call for the first time, once per version and calling app. Hosts that register no versions stay silent, so an ordinary app emits none. | `functionName`, `version` (requested), `hostedVersion`, `versionResolution`, `callerSpaceId`, `callerSessionId` |

`versionResolution` taxonomy: `None`, `Exact`, `Floor`, `Greatest`, `Current`, `Unversioned`,
`Other`. `Floor` means the caller asked for something older than anything hosted.

`errorKind` taxonomy (server side): `Timeout`, `Cancelled`, `NotFound`, `LocalVisibility`,
`VersionMismatch`, `PolicyDenied`, `PolicyError`, `ArgumentBinding`, `Execution`, `InvalidArgument`.
`class` follows from it. `NotFound`, `VersionMismatch`, `ArgumentBinding` and `InvalidArgument` mean
the caller asked wrongly and are `user_error`. `ArgumentBinding` is only an argument that could not be
converted to its parameter's type; an `ArgumentException` the function itself throws is `Execution`.
`PolicyDenied` (a limit enforced on purpose) and `Cancelled` (the caller cancelled its own call) are
`expected`. The rest are `dependency`, except an `Execution` failure that an AI operation inside the
function already recorded: it takes the class of that operation's `{eventName}_failed` row, so a
request the model rejected stays `user_error`. A platform function that names the caller's mistake
itself, such as subscribing to a reactive the server does not have, is `user_error` too.

`lastErrorKind` taxonomy (client side): `Timeout`, `ConnectionFailed`, `InstanceNotFound`,
`RemoteError`, `IOError`, `Other`. On the client, `class` is `expected` for `succeeded_after_retry`,
for a call the caller cancelled itself, and for a spend limit or policy denial the far side enforced
on purpose. A denial whose limit check itself failed stays `dependency`, as on the server. Any other
call that `failed` is classed by the error the far side threw, using the same rule as the AI
operation's own `{eventName}_failed` row (below): a refused prompt, and a plan, credit or spend limit that denied the call on purpose, are `expected`, a request the model rejects is `user_error`, an empty provider account
is `provider_credit`, and everything else is `dependency`.

## Resource and health

Kept 30 days.

| Event | When | Payload |
|---|---|---|
| `ikon_server_oom` | A server ran out of memory and was killed. Reported from outside the dying process, so it survives even a hard kill. | `class` (`expected` when the app outgrew its own memory limit, `dependency` when the host ran short of memory, which is always the case for a server with no limit of its own), `ikonServerId`, `spaceId`, `appBundleId`, `ikonServerReleaseId`, `memoryLimitMb`, `peakMemoryUsageBytes` (the highest working set the host's 5-second polls read from the server's cgroup: `memory.current` less inactive page cache, shared memory included), `cgroupMemoryPeakBytes` (the cgroup's own high-water mark, page cache included: what the kernel counted against the limit, or 0 when it could not be read), `exitCode`, `failureCategory`, `failureSubCategory`, `cgroupMemoryLimited`, `isStartupFailure`, `uptimeMs`, and from the kernel's counters of the dead container, null when they could not be read: `memoryLimitReached` (how often usage reached the limit), `oomKillCount` (the processes killed for memory; more than 1 means a child process was killed before the server), `shmemAtExitBytes` (shared memory still charged to the container after its processes ended, such as files in a tmpfs or `/dev/shm`) and `fileAtExitBytes` (the page cache still charged, shared memory included) |
| `oom_recovered` | The in-process memory guard recovered from memory pressure and the process survived. Once per process lifetime, so a thrashing spike cannot flood analytics. | `heapSizeBytes`, `totalAvailableMemoryBytes`, `memoryLoadBytes`, `highMemoryLoadThresholdBytes`, `recoveriesInWindow`, `maxRecoveriesPerWindow`, `recoveryWindowSeconds`, `processMemoryBytes`, `containerMemoryLimitBytes`, `memoryInfo` |
| `host_server_needs_recycle` | A host server decided it must be recycled | `hostServerSessionId`, `reason` |
| `legacy_usage_observed` | A deprecated code path was reached, reported by the server, the host agent (a started bundle's platform version) or the backend (an old client and the space that sent it). Deduplicated per process on every field but `sessionId`, so it reports first contact, not call volume. | `feature`, `detail`, `sessionId`, `callerSpaceId`, `spaceId`, `contextType`, `userType`, `sdkType`, `productId` |

## AI and model operations — `EventLogHelper`

Kept 30 days.

`Ikon.AI.Utils.EventLogHelper.RunAsync` and `RunAsyncEnumerable` instrument an async operation with
a fixed set of events, derived from the `eventName` argument the caller supplies:

- `{eventName}_succeeded`
- `{eventName}_cancelled`
- `{eventName}_failed`

A call the AI mocks answer (`AIMocks.Use()`, `ikon run --ai mock`) emits none of them; only its debug log lines record it.

Payload (same for all three): `modelName`, `elapsedSeconds`, `callId` when the operation ran inside a
function call (it joins the `rpc_*_call_failed` rows), plus caller-defined additional fields as
top-level keys, the completion details, the exception (`class`, `failureKind`,
`errorType`, `errorMessage`, `stackTrace` — on `_failed` only), `isRemote`, `isUserCredential`. For
LLM calls the completion details carry the token counts (`InputTokens`, `OutputTokens`,
`InputCachedTokens`, `OutputReasoningTokens`, `FinishReason`, …).

`usagePrefix` names the route that served the call the way its usage events are named
(`{kind}.{provider}.{model}.{region}`, such as `llm.groq.gpt-oss-120b.global`), so a call and what it
cost can be matched. A call runs on one route and is never moved to another, so a `_failed` event's
`usagePrefix` is the route that failed. A request with an option its route does not take is refused
before anything is sent, and reported as a `_failed` event with no `statusCode`. A `_failed` event
otherwise carries `statusCode`, the provider's HTTP status as an integer, when there was one.

`failureKind` says what the failure means for the model — `Transient`, `Unavailable`,
`AccessDenied`, `CreditExhausted`, `NotAllowed`, `Quality` or `Unknown`. It is finer than `class`, and
it tells a provider that is down from one whose account has no credit left. Both stop the call, but
only the first is fixed by waiting. `NotAllowed` is a model your organisation may not call: its plan
does not include the model, its admin blocked the model, or the model reached the monthly credit cap
the admin set for it. Another model may be allowed. A `NotAllowed` failure has the class `expected`.

**A persisting failure is logged once, not once per call.** Every failed call still writes its `_failed`
row, so event counts stay correct. The log line that goes with them drops to debug after the first
for that model and failure kind, and becomes a warning again an hour later. Count the events, not
the log lines.

**One failed operation is one `_failed` event, recorded where it happened.** When the operation runs
in another process, the failure is recorded there, by the process that saw it, with the exception it
actually threw. The calling side records nothing, so there is no second row saying
`FunctionCallException`. What the caller experienced is in `rpc_client_call_failed`, which reports
once per call with `attemptsMade` and `finalOutcome`, so a call that succeeded on its second attempt
is one `expected` row and not a failure at all.

`errorType` is the type the operation threw. Where that reached us over the wire it is the *remote*
type, unwrapped from the transport exception that carried it, and `stackTrace` is the remote stack —
the wrapper's own type and stack name the RPC layer, never the cause.

The prefixes in use are `llm`, `decision`, `classification`, `embedding_generation`, `reranking`,
`image_generation`, `image_segmentation`, `image_upscaling`, `depth_estimation`, `mesh_generation`,
`mesh_texturing`, `mesh_splitting`, `mesh_rigging`, `mesh_animation`, `music_generation`,
`sound_effect_generation`, `speech_generation`, `speech_recognition`, `ocr`, `video_generation`,
`video_enhancement`, `video_segmentation`, `file_conversion`, `web_scraping` and `web_searching` —
so 75 concrete event names.

## Users and authentication

Kept 13 months.

Emitted by the backend, not by the server.

| Event | When | Payload |
|---|---|---|
| `user_created` | A new user account was created | `user` |
| `user_login` | A user authenticated | `authSession`, `user`, `provider` |
| `user_logout` | A user signed out | `user` |
| `user_removed` | A user account was removed | `user` |
| `user_token_delegated` | A user token was delegated to another app | `authSession`, `user`, `role`, `sourceSpace` |
| `login_email_request` | A magic-link sign-in email was requested. No payload — the address would be personal data. | — |
| `anonymous_user_created` | An anonymous user was created | `user` |
| `anonymous_user_login` | An anonymous user authenticated | `authSession`, `user` |
| `anonymous_user_removed` | An anonymous user was removed | `user` |

`provider` on `user_login`: `username`, `google`, `apple`, `microsoft`, `facebook`, `signicat`,
`sso`, `email`, `passkey`, `api-key`, `space-token`, `ticket`. OAuth and Signicat sign-ins also carry `space` when
one is known, `sso` sign-ins carry `space` and `ssoConnection`, and `ticket` sign-ins (a sign-in ticket an app's own server issued) carry `space`, the issuing space.

`authSession` is what ties a sign-in to the sessions that follow it — it is also on `client_joined`'s
`clientContext.AuthSessionId`.

## Profiles

Kept 13 months.

| Event | When | Payload |
|---|---|---|
| `profile_created` | A profile was created in an app | `user`, `role` |
| `profile_updated` | A profile changed | `user`, `role` |
| `profile_removed` | A profile was removed | `user` |
| `lead_created` | A profile was forwarded to a CRM integration | `user`, `integration` |

## Session issue analysis

Kept 30 days.

App-scoped, emitted by the backend for apps with session analysis enabled.

| Event | When | Payload |
|---|---|---|
| `session_issue_opened` | The analysis opened a new issue from this app's session logs | `issue`, `fingerprint`, `severity`; `title` and `category` when the issue was classified |

`category` is the classifier's guess at the cause: `app-defect`, `platform-defect`,
`configuration`, `integration`, `capacity` or `noise`. The event carries no `class`, because a new
issue is something the analysis found in the logs. It does not mean an operation failed.

## Billing

Kept with the billing records.

Organisation-scoped, emitted by the backend. All carry `organisation`.

| Event | When | Payload |
|---|---|---|
| `billing_checkout_created` | A checkout session was opened | `paymentProvider`, `session`, `customer`, `planType`, `mode` |
| `billing_checkout_completed` | Checkout completed | `paymentProvider`, `session`, `customer`, `paymentIntent`, `amountPaid`, `currency`, `product`, `price`, `planType`, `tier`, `paymentStatus` |
| `billing_checkout_failed` | A checkout session expired, or its payment failed after the customer submitted it | `class`, `paymentProvider`, and the failure details |
| `billing_invoice_payment_failed` | A subscription invoice payment failed | `class`, `paymentProvider`, `subscription`, `invoice`, `amountDue`, `currency`, `reason` (`renewal`, or `upgrade` for a plan upgrade waiting on its invoice) |
| `billing_async_payment_completed` | A deferred payment method settled later | `paymentProvider`, and the settlement details |
| `billing_invoice_paid` | An invoice was paid | the invoice and period details |
| `billing_credits_added` | Credits were added to an organisation | `source`, `credits`, `subscriptionRemaining`, `purchasedRemaining` |
| `billing_credits_reset` | The monthly credit allowance was reset | the subscription and period details |
| `billing_subscription_updated` | A subscription changed | `subscription`, and the change details |
| `billing_subscription_cancelled` | A subscription was cancelled | `subscription`, and the cancellation details |
| `billing_subscription_reconciled` | A subscription was reconciled against the payment provider | `subscription`, and the reconciliation details |

On `billing_checkout_failed`, `class` is `expected` when the checkout session expired (the customer
left without paying) and `user_error` when the payment method was declined after the customer
submitted it. `billing_invoice_payment_failed` is always `user_error`, because the customer's payment
method was declined.

## Tool errors

Kept 30 days, deploys included.

Top-level crash markers for the platform's CLI / utility processes. All carry `class` (`defect` — an
unhandled exception reached the entry point; `tool_failed` also uses `dependency` and `expected`, below), `type`,
`message`, `stackTrace`.

| Event | Source |
|---|---|
| `tool_failed` | `IkonTool` (the `ikon` CLI) |
| `build_util_failed` | `BuildUtil` |
| `codegen_docs_gen_failed` | `CodegenDocsGen` |
| `proxy_server_failed` | `IkonProxyServer` |
| `host_server_failed` | `IkonHostServer` |

### The `ikon` CLI's own outcomes

The CLI reports a command that rejected your input separately from one that broke:

| Event | When | `class` |
|---|---|---|
| `tool_failed` | An unhandled exception, a backend request that failed, or a recognised environment failure (missing file, refused connection, timeout, a program such as `npm` that exited non-zero, a file Windows refused to open) | `defect`, or `dependency` for the backend and environment cases, or `expected` when the backend rate-limited the request (429) |
| `tool_rejected` | The command refused the input and said why (an app that does not compile among them), the backend refused the request as the caller's fault (400, 401, 403, 404, 409 or 422), or the database refused a statement as written (SQLSTATE class 42: a syntax error, an undefined table, column or function) | `user_error` |
| *(nothing)* | The command asked for `--yes` and did not get it | — |
| `tool_command_finished` | Every command, when it ends: `outcome` (`succeeded`, `failed`, `rejected`, `cancelled` or `confirmation`), `exitCode`, `durationMs` and `outputFormat` (`default`, `auto`, `text`, `json`, `csv` or `other`; never the raw argument) | — |
| `tool_installed` | The first run after an installer finished, with the installer's result: `ok`, `code`, `signedIn`, `components` (each one's `component`, `status` and `version`), `installer` and `finishedAt` | — |
| `tool_updated` | After `ikon self update`, an automatic update or `ikon self reset` replaced the tool, with the result of the script that replaced it. It is sent by the first run of the tool that ends after the script: the command that started the script, or the command that an automatic update ran next. When the script ran in a window of its own, as `ikon self reset` and a switch of channel do on Windows, the next command you run sends it. It carries `ok`, `step` (where a failed one stopped: `remove`, `install`, `finish` or `after-install`), `kind` (`update` or `reset`), `automatic`, `fromVersion`, `toVersion`, `channel` (`stable` or `dev`), `switched` (it moved between the two), `fallback` (`dotnet tool update` failed and a fresh install was tried) and `finishedAt` | — |
| `app_run_stopped` | `ikon run`'s app ended because `ikon stop` asked it to (`graceful` says whether it shut down on its own or was killed because it did not stop in time), or it was killed by a signal | `expected` |

**These rows are sent only with consent.** The CLI asks once in a terminal, after you sign in.
`ikon telemetry status|enable|disable` shows and sets the answer, and `DO_NOT_TRACK=1` or
`IKON_TELEMETRY=0|1` overrides it. Until someone answers, nothing is sent. They are kept for 30 days,
in a table of their own with no space or session column.

The rows of a command carry `verb` (the verb that was running, or `(unresolved)` when it failed before
one was picked; `self update`, with `automatic` true, for an automatic update that ran before the
command; `--finish-install` for the run an update makes of the new install) and
`phase` (`startup`, `sign-in` or `verb`: where the run was when it ended), so a command's failures can be found without matching on message text. After an automatic update the command runs in the new
version, and its own rows name the command. On macOS and Linux the new version takes over the
process, so the rows of the update itself are not sent. Every `tool_*` row carries the run
context: `toolVersion`, `os`, `isCi`, `isInteractive`, `isLocalBuild` (a tool built from the
platform's own source) and `isHosted` (the tool ran on an app's token, as the platform's hosted runner
starts it). When another program ran the command, its rows also carry `caller`: the name and version
that program set in the `IKON_CALLER` environment variable (`ikon-desktop/1.2.0` for IKON Desktop).
The CLI sends `caller` only for a program it knows, with an optional version made of numbers and
dots. A program the CLI starts itself, such as a coding agent, does not inherit `IKON_CALLER`.

Before a row is sent, the home folder becomes `<home>`, the app project's folder `<project>`, a
person's account name `<user>` and an email address `<email>`. A person's row names no app: it has no
`spaceId` or `bundleId`, and an id inside a message becomes `<id>`. Rows from a hosted run or from the
platform's own CI keep `spaceId`, `bundleId` and the ids.

When the backend is what failed or refused, `tool_failed` or `tool_rejected` also carries `statusCode` and `route` —
the path with its ids replaced by `:id`, so every caller of one broken endpoint groups into one row
rather than into as many rows as there were ids. `requestId` is kept only where the backend failed
(`class` `dependency` or `defect`), which is how that request is found in the backend's logs.

When a program the command ran exited non-zero (such as `npm`), `message` is only which program
failed and with what code, because its output holds local paths. A build of the app that failed is a
`tool_rejected` row whose `message` is the tool's own line naming the first errors. The distinct compiler, NuGet,
MSBuild, TypeScript and npm error codes in that output are kept as `errorCodes` (for example
`["CS0103"]`), which is enough to tell a broken app from a broken package. When Windows refused to
open a file the command needed, `tool_failed` carries `blockedBy`: `app-control` (Smart App Control),
`antivirus` (Microsoft Defender), `policy` (an App Control or group policy an administrator set) or
`controlled-folder` (a write in a folder Controlled Folder Access protects, such as Documents).

### Deploys

Every deploy, from `ikon deploy` or from Studio, writes exactly one `deploy_finished` row when it ends,
whatever the outcome, so a failure rate is a count over one event. A deploy from the command line that
fails also writes a `tool_failed` or `tool_rejected` row; count deploys from `deploy_finished` alone.
Only a process killed outright (closing the terminal, running out of memory) writes nothing. A
command-line deploy's row follows the CLI's rules above: sent only with consent, masked, and kept 30
days.

| Field | Meaning |
|---|---|
| `outcome` | `succeeded`, `failed` or `cancelled` (Ctrl+C, or Studio giving up on a deploy that ran too long) |
| `step` | How far it got: `prepare` (config, the credit check, assets), `build` (app, boot snapshot capture, frontend), `package`, `upload`, `activate`, `finish` (after activation, such as restarting the person's own sessions) |
| `durationMs` | From the start of the deploy to its end |
| `spaceId` | The app deployed to, once it is known. Left out of a person's command-line deploy, like every app id in the CLI's rows |
| `bundleId`, `sizeMb` | The deployment, once the backend registered it (left out with `spaceId`), and the package size |
| `fromPackage`, `noActivate` | The deploy uploaded a package `ikon bundle` made (`--bundle`), or did not activate it (`--no-activate`) |
| `wsl` | The package was built in WSL (`--wsl`, Windows only) |
| `warnings` | The codes of every warning the deploy printed, such as `BOOT_SNAPSHOT_SKIPPED` (`--no-boot-snapshots` deployed without capturing), `APP_START_FAILED` (the app did not start in the boot snapshot capture and `--ignore-start-failure` deployed it anyway), `TOOLS_DANGLING_LINK`, `HASH_UNCHANGED` (the build matched the active deployment), `NOT_SAVED` (saving to version control failed) and `LOW_CREDITS` (the organisation had under 10 credits left once the deployment was active) |
| `activationErrors` | The backend's codes when activation refused the deployment, such as `INSUFFICIENT_CREDITS` |
| `snapshotSkipReason`, `snapshotAppError` | When the boot snapshot capture fell short: what it missed, and the first error the app logged during the capture |
| `failureType`, `failureMessage`, `errorCodes` | On a failed deploy: the exception, its message (for a failed program only which program and its exit code), and the program's error codes. A deploy refused because the app did not start in the boot snapshot capture has `errorCodes` `["APP_START_FAILED"]`, and one refused because the capture or the prerender fell short of what the surfaces declare has `["BOOT_SNAPSHOT_FAILED"]` |
| `toolVersion`, `isCi`, `isInteractive` | The run context. A deploy Studio ran is not interactive, and its row's own space column is Studio's app |

## Metered usage is not an event

Consumption — tokens spent, images generated, database seconds, egress — is metered separately and
is what billing and cost reporting are based on. It does not appear in your events, so an event
query is the wrong place to look for it. Use the cost and usage views in the Portal instead. They
show the credits each usage was charged at the time. Each use is kept for 25 months. After that,
only daily totals per app and usage name are kept, as billing records, until six full years have
passed after the end of the year they belong to. For those older days the Portal shows the daily
totals, which cannot be broken down by scope or by hour.

The AI events above are the nearest equivalent: `llm_succeeded` and its siblings record that a model
call happened and how long it took, and carry token counts in their completion details. Treat those
as diagnostics, not as a billing record.

## User data erasure

When a user's data is erased (GDPR erasure — see [User Data Erasure](user-data-erasure-guide.md)), the backend delivers a durable erasure event once to every affected app that declares a listener for it:

<!-- ikon-example: user-data-erasure -->
```csharp
[Trigger(TriggerEventType.UserErased)]
internal Task EraseAsync(UserDataErasureEventArgs args)
{
    // Delete app-owned data for args.UserId: rows in your own tables,
    // personal data embedded in Session/Global scoped values.
    return Task.CompletedTask;
}
```

Semantics:

- **When it fires** — after the platform has re-erased the user's platform-managed state on the app side (`EraseUserStateAsync` — persistent user-scoped reactives and stored user-scope rows), once per id in the erased user's identity closure (merged accounts included). The user is not connected when it fires and no client/user reactive scope is active.
- **It is a platform event** — erasure is delivered as the `ikon.user.erased` event, carrying a `UserErasurePayload` (the platform's erasure id and the account's whole identity closure), on the same machinery as every event above. It is delivered only when your deployment declares a listener, once per app, to the app's shared userless instance (the one `[Cron]` ticks in). That instance is cold-started if none is running, and delivery is retried with a growing backoff until your handler returns.
- **But it is dispatched differently** — the handler takes `UserDataErasureEventArgs`, not a `TriggerContext`, because it runs once per user id rather than once per delivery, and `MaxParallelism` must stay 1: any other value fails registration at startup and fails bundling. `app.OnUserDataErasure(userId => ...)` attaches a second handler at runtime and runs alongside the declared one; it does not make the platform deliver, because no bundle scan can see it.
- **Once per app, in one instance** — however many instances of your app are live, the handler runs once per erasure. The others are only told to drop the erased user's in-memory state, which runs no handler of yours.
- **At-least-once delivery** — the event is stored per app on the backend and redelivered until a run completes without throwing, so a cold or stopped app processes it whenever it next runs. A crash between completing the handler and reporting also results in one extra delivery.
- **Idempotency is required** — because delivery is at-least-once, the handler must tolerate running again over already-deleted data (`DELETE ... WHERE user_id = @userId` is naturally idempotent).
- **Failure handling** — let exceptions propagate. A throwing handler leaves the event unanswered and it is redelivered after its backoff; swallowing the exception would report an incomplete erasure as done.
- **However long it takes** — the session is held open for the whole run, so a handler slower than the idle limit is not stopped mid-erasure.
- **No listener declared** — nothing is delivered, and the erasure report records the app as skipped because it has no listener. The platform erases the user's platform-managed state centrally whether or not your app ever runs; app-owned data is the app's documented responsibility either way.
