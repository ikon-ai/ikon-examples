# Endpoints & Webhooks

## Endpoints & Webhooks

Your app's inbound HTTP lives under one public surface: `https://{space}.ikonai.app/api/...`. There are two ways to put something there — pick by what you need.

### Declarative endpoints — `[HttpGet]`/`[HttpPost]` and `[Mcp]` (the common case)

Mark a method on your `[App]` class (or any `[Cell]` type) and the platform routes a **stable** public URL to it. This is how you build a REST API, expose tools to an LLM/agent, and **receive third-party webhooks** (Stripe/GitHub/Slack) — those are just `[HttpGet]`/`[HttpPost]`s; there is no separate "webhook" attribute.

- **Stable URL** — `https://{space}.ikonai.app/api/{path}`, unchanged across restarts and redeploys, so you register it once with an external service.
- **Can cold-start the app** — a request to an idle app provisions an instance on demand (~2-3 s with a warm pool), waits for it, then delivers the call. Make handlers idempotent (services retry on timeouts).
- **No config entry** — discovered at deploy time; `app.Endpoints` exposes them at runtime. Their `PublicUrl` is a *bare* address; call `app.MintUrlAsync` to get a working, identity-bound URL to hand out (see below).

### Manual endpoint hosts — `AppEndpointHost` / `RequestEndpointAsync` (custom servers)

When `[HttpGet]`/`[HttpPost]` can't express it — a WebSocket server, a raw TCP/TLS/UDP listener, or an HTTP server you wire yourself — open a host at runtime and own the listener. These get a public relay URL that **changes on every restart** and **cannot cold-start** the app (traffic flows only while it's running).

### When to use which

| Use case | Use |
|---|---|
| REST API, third-party webhook (Stripe/GitHub/Slack), needs a stable URL / app may be idle | **`[HttpGet]`/`[HttpPost]`** |
| Expose a tool to an LLM / agent | **`[Mcp]`** |
| WebSocket server for a custom client | **`AppEndpointHost`** (`MapWebSocket`) |
| Game server / raw TCP / UDP / DTLS, or a custom HTTP server you wire yourself | **`AppEndpointHost` / `RequestEndpointAsync`** |

---

## Manual endpoint hosts (raw / WebSocket / custom servers)

Requested at runtime from your app code — no `ikon-config.toml` entry needed. The public URL changes on every restart and **cannot** cold-start the app; for a stable, cold-startable URL use `[HttpGet]`/`[HttpPost]` (see below). Use these only when you need a protocol `[HttpGet]`/`[HttpPost]` can't express.

### Creating and Starting an HTTP/HTTPS Endpoint

```csharp
var endpoint = new AppEndpointHost(app);

endpoint.MapWebSocket("/ws", async (ctx, webSocket) =>
{
    var buffer = new byte[4096];
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close) { break; }
        await webSocket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, true, CancellationToken.None);
    }
});

await endpoint.StartAsync();
```

`MapGet`/`MapPost`/`MapPut`/`MapDelete`/`MapPatch` exist for an HTTP server you wire yourself (streaming proxies, catch-all static serving, custom redirects). A plain GET/POST JSON or text handler does NOT belong here — declare it as an `[HttpGet]`/`[HttpPost]` method instead for a stable, cold-startable URL; the build nudges with IKON005 when a verb route lands on the raw host. When you do own the server:

```csharp
// Write the response via ctx.Response.Body (a Stream). NOT ctx.Response.WriteAsync(string)
// — that ASP.NET Core extension (Microsoft.AspNetCore.Http) is not in scope in a
// generated app and produces CS1061. Write UTF-8 bytes to the body stream.
endpoint.MapGet("/stream/{**path}", async ctx =>
{
    ctx.Response.ContentType = "text/plain";
    await ctx.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("OK"));
});
```

Pass `secure: false` for plain HTTP (the default is HTTPS with TLS terminated at the relay).

### Raw TCP/TLS/UDP Endpoints

`AppEndpointHost` only supports HTTP/HTTPS. For raw TCP/TLS/UDP, request an endpoint directly and bind your own listener:

```csharp
await using var endpoint = await app.RequestEndpointAsync(EndpointProtocol.Udp);
var udp = new UdpClient(endpoint.LocalPort);
Log.Instance.Info($"Game server listening at udp://{endpoint.PublicHost}:{endpoint.PublicPort}");
// `await using` above releases the endpoint when it goes out of scope.
```

The `RelayEndpoint` you get back carries the `LocalPort` to bind to, the `PublicHost`/`PublicPort` clients connect to, and the `Protocol`; it is `IAsyncDisposable`, and disposing releases the tunnel. Valid raw protocols: `EndpointProtocol.Tcp`, `Tls`, `Udp`. `Tls` enables TLS termination at the relay (your listener sees plaintext on the local port). For HTTP/HTTPS use `AppEndpointHost` with `secure: true` (default) or `secure: false`.

### Properties

- `endpoint.PublicUrl` - The public URL assigned by the platform (use this to share with external services). **Changes on every app restart.**
- `endpoint.LocalPort` - The local port the endpoint is listening on.

### Cleanup

Dispose endpoints when the app stops:

```csharp
app.OnStopping(async () =>
{
    await endpoint.DisposeAsync();
});
```

---

## HTTP endpoints & MCP tools

Declare these on your `[App]` class (or any `[Cell]` type). They're discovered at deploy time and routed under `https://{space}.ikonai.app/api/...` — no `ikon-config.toml` entry needed.

On the app class:

```csharp
// The JSON body binds to your typed parameter. The binder is lenient — missing
// fields default, unknown fields are ignored, and bad input returns a 4xx (it never throws a 500).
[HttpPost("/sum")]
public HttpResult Sum(SumRequest req) => HttpResult.Ok(new { sum = req.A + req.B });

// Explicit verb, no body. Return a value (→ JSON), a string (→ text/plain), or an HttpResult.
[HttpGet("/health")]
public string Health() => "ok";

// A third-party webhook is a normal [HttpPost]. It must be Auth = Public: the default (Grant)
// makes the gateway reject the bare URL with 401 before the handler runs, and a provider like
// Stripe calls a fixed URL it cannot carry a grant on. Read the signature header + raw body from
// the injected Ikon.App.HttpRequest and verify it yourself — the signature IS the authorization.
[HttpPost("/stripe", Auth = EndpointAuth.Public)]
public async Task<HttpResult> Stripe(Ikon.App.HttpRequest req)
{
    if (!VerifyStripe(req.Headers["Stripe-Signature"], req.Body)) return HttpResult.Unauthorized();
    // ... process req.Body ...
    return HttpResult.Ok();   // return 200 even on a skip to avoid the provider's retry storm
}

// An MCP tool, callable by an LLM / agent. Its JSON Schema is reflected from the signature.
[Mcp(Name = "add_numbers", Description = "Adds two integers")]
public int AddNumbers(int a, int b) => a + b;
```

and the request body as a record beside it:

```csharp
public record SumRequest(int A, int B);
```

### `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpDelete]` / `[HttpPatch]`

- The verb is the **attribute name** (there is no `Verb` enum); the single constructor arg is the path: `[HttpPost("/p")]`, `[HttpGet("/p")]`.
- The handler binds **one optional typed body** (a JSON record/object, or a raw `string` for the unparsed body) plus any **host-injected context params** — `Ikon.App.HttpRequest` (method/path/query/headers/body), `HttpCallContext`, `CancellationToken` — in any order. Zero non-injected params = no body.
- **Authorization** is declared on the attribute and evaluated at the gateway *before* the handler runs (a denial returns 401/403). Two ways: `Auth` takes the `EndpointAuth` enum — `Grant` (**the default**: the URL must carry a valid signed grant from `MintUrl`), `Public` (anonymous), or `Deny` (always rejected); and `AuthPolicy = "name"` names a custom `/router/` edge policy — an `apiKey` / `hmac` / `ipAllow` helper you define in `router/index.ts` (`AuthPolicy` wins when both are set). On a policy endpoint a grant in the URL is **address-only** (it picks the instance); the policy is what authorizes. Return a value (→ JSON), a `string` (→ text), or an `HttpResult` (`Ok`/`BadRequest`/`Unauthorized`/`NotFound`/…).

### `[Mcp]`

Exposes the method as an MCP tool for LLM/agent tool-use, reachable two ways:
- **The JSON-RPC multiplexer** — all of an owner's tools share **one** endpoint (`tools/list`/`tools/call`): `https://{space}.ikonai.app/api/mcp` when the `[Mcp]` methods are on the app class, or `/api/{kebab-cased-cell-type}/mcp` for a cell (e.g. `LabCell` → `/api/lab-cell/mcp`); the input/output JSON Schema is reflected from the C# signature.
- **A per-tool POST endpoint** — each tool is ALSO at its own URL (derived from the method name, or `[Mcp("/custom")]` to override), with the request body bound the same way `tools/call` binds its arguments. So `[Mcp] int Add(int a, int b)` is callable as `POST /api/add {"a":1,"b":2}` → `3`, not just via JSON-RPC.

Pair with `[McpResource]` for resources. (A method with both a verb-named REST attribute (`[HttpGet]`/`[HttpPost]` etc.) and `[Mcp]` uses the REST route for its direct HTTP surface; the tool still appears in the multiplexer.)

Inside a tool, `McpCallContext.Current` exposes what the connection-level context cannot carry on an
endpoint-dispatched call: the request's `CancellationToken`, its resolved `UserId` (null when no
context is current or the claims carry none), the `SessionIdentityFields` it routed on, and an
`OnProgress` callback taking a `ProgressUpdate` (`Progress`, an optional `Total`, an optional
`Message`). Progress is a monotonic counter — keep `Total` constant across one call's updates so a
client can render a stable percentage.

### Scheduled Methods — `[Cron]`

`[Cron("0 * * * *")]` runs a method on a schedule. It registers the method through the
`FunctionRegistry` by name the way `[Function]` does, and applying it is enough — you do not also
need `[Function]`, though combining them is fine. The handler takes no caller-supplied arguments; it
may optionally accept a host-injected `CronContext` (the `FireTimeUtc` and the `Schedule` string)
and/or a `CancellationToken` that signals app shutdown, in either order, mirroring how an
`[HttpPost]` handler may accept an `HttpRequest`. Any other parameter fails registration at startup,
because the scheduler has nothing to bind it to. **Overlap is allowed**: a tick fires even if the
previous invocation is still running, so guard re-entrancy yourself when it matters.

### Public URLs, identity & minting

`app.Endpoints` lists every endpoint, but each `PublicUrl` is a **bare** address (`{space}.ikonai.app/api/...`) with no grant. A `Public` endpoint is callable as-is; for a `grant` (default) or policy endpoint, **mint** a working, identity-bound URL — minting is the single way to get a callable URL, and it's required for local-dev too:

```csharp
// Pin a resource identity into a signed grant in the URL:
MintedUrl minted = await app.MintUrlAsync(nameof(GetDocument), new { DocumentId = "doc-42" });
string url = minted.Url;   // https://{space}.ikonai.app/api/...?ikon-grant=...

// Omit the identity to pin THIS instance's own identity (the URL routes back here):
MintedUrl self = await app.MintUrlAsync(nameof(Sum));

// Batch several endpoints under one identity in a single backend round-trip:
IReadOnlyDictionary<string, MintedUrl> urls = await app.MintUrlsAsync(
    new[] { nameof(GetDoc), nameof(UpdateDoc) }, new { DocumentId = "doc-42" });
```

You identify the endpoint to mint by its **handler method** — `nameof(GetDocument)` (or the full `{Owner}_{Method}` registry name when the bare name is ambiguous), **not** by its URL path. Use `nameof` so a rename stays in sync. You never pass the path: an endpoint's path is often *derived* from the method name (and may be templated), so the path is what minting **returns** to you, built from that handler's `PublicUrl`.

Hand the minted URL (not the bare `PublicUrl`, and never a hand-built one) to the external service. A pinned field that matches a `{placeholder}` in the path is substituted into the URL; fields you omit stay open for the caller to fill. The grant is **non-expiring by default** — retire it with `app.RevokeUrlAsync(minted.GrantId)`, or pass `expiresIn:` for a self-destructing link. Minting is **idempotent** for stable (non-expiring) URLs, so re-minting on every restart returns the same URL — a registered webhook keeps working across restarts. On a `grant` endpoint the grant authorizes the request and the cold-start; on an `apiKey`/`hmac`/`ipAllow` policy endpoint it only addresses the instance and the caller authenticates with their own credential.

---

# Ikon.Sdk Public API

namespace Ikon.Sdk
  sealed record ApiKeyConfig
    ctor()
    // From the portal; format 'ikon-xxxxx'.
    string ApiKey { get; init; }
    // Defaults to Production.
    BackendType BackendType { get; init; }
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // An arbitrary string, not an internal Ikon user ID — the backend creates/maps an internal user for it.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  // Uses the existing IkonBackend login credentials (login.json or environment variables); the preferred mode for internal Ikon C# applications.
  sealed record BackendConfig
    ctor()
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // An arbitrary string, not an internal Ikon user ID — the backend creates/maps an internal user for it.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  enum BackendType
    Production
    Development
  enum ConnectionState
    // Intentionally not connected: the initial state before ConnectAsync, and the state after a user-requested DisconnectAsync. Ready to connect; nothing went wrong.
    Idle
    Connecting
    Connected
    Reconnecting
    // Unexpectedly disconnected and not retrying: automatic reconnection was exhausted, or the server signalled an intentional shutdown. (A user-requested disconnect goes to Idle.)
    Offline
  static class ConnectionStateExtensions
    static bool IsConnected(this ConnectionState state)
    static bool IsConnecting(this ConnectionState state)
    // Returns true if the client is not connected and not connecting — this covers BOTH the pristine ConnectionState.Idle state (nothing went wrong) and the failure ConnectionState.Offline state. The name deliberately reads as "no live connection", not "failed": use IsFaulted to detect a failure specifically.
    static bool IsDisconnected(this ConnectionState state)
    // Returns true only for ConnectionState.Offline — the connection failed (auto-reconnect exhausted or the server shut down), as opposed to the intentional ConnectionState.Idle state before connect or after a requested disconnect.
    static bool IsFaulted(this ConnectionState state)
  sealed class IkonClient : IAsyncDisposable
    // config: Client configuration. Exactly one of ExternalConnectUrl, Local, ApiKey, Backend, UserLogin, or ResumeAuthResponse must be specified.
    // throws ArgumentException: Thrown when configuration is invalid.
    ctor(IkonClientConfig config)
    // Null until the connection is established.
    Context? ClientContext { get; }
    IkonClientConfig Config { get; }
    // Default encoder options for audio output, used when a SendAudioAsync call passes no explicit encoderOptions. Captured when a stream's encoder is first created (the first SendAudioAsync for a given streamId, or for the shared fallback stream when none is given); changing it afterwards has no effect on already-active streams.
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Each IkonClient has its own isolated FunctionRegistry, so multiple SDK connections run independently (e.g. when running the SDK inside an Ikon app, or several clients in one process).
    FunctionRegistry FunctionRegistry { get; }
    // Null until the connection is established.
    GlobalState? GlobalState { get; }
    // Null until connected. Hand it to another client's IkonClientConfig.ResumeAuthResponse to have that client join this one's session. The auth ticket inside is a bearer credential for the session: give it only to a client you started yourself.
    AuthResponse? LastAuthResponse { get; }
    ConnectionState State { get; }
    // Valid only from ConnectionState.Idle or ConnectionState.Offline; throws InvalidOperationException if already connecting or connected. Calling it from ConnectionState.Offline while the background reconnect loop is still running stops that loop first, so the connection this call makes is the one the client keeps (if the loop happened to finish connecting meanwhile, the call returns with that connection). On failure the client returns to ConnectionState.Offline and the exception is rethrown. The same failure is also delivered to the ErrorOccurredAsync event before it is rethrown, so a caller that both handles that event and catches this call sees the failure twice — guard against double handling if both paths are wired.
    // throws InvalidOperationException: Thrown if already connected or connecting.
    // throws Exception: Thrown on connection failure.
    Task ConnectAsync(CancellationToken ct = default)
    Task DisconnectAsync()
    ValueTask DisposeAsync()
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired; audio is never silently dropped. Safe to call concurrently: sends are serialized, so frames of one stream never interleave. A reconnect that lands on a new server session re-announces every active stream before its next frame.
    // samples: PCM samples in range [-1.0, 1.0]
    // sampleRate: Fixed per stream: the first call for a streamId configures its encoder and announces the format, so every later call must pass the same rate — a different one throws ArgumentException; use a new streamId for another format
    // channelCount: Fixed per stream like sampleRate
    // encoderOptions: Falls back to DefaultEncoderOptions; applied only when the stream's encoder is first created — later changes do not reconfigure an active stream
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the message.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the payload.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Call once your setup completes, typically from the ReadyAsync handler. Throws if not connected.
    Task SignalReadyAsync()
    // Waits up to timeout (30 seconds when null) for a client matching productId/userId. An explicit TimeSpan.Zero is honored as a single poll rather than promoted to the default. Throws if not connected.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan? timeout = null)
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputFrameEventArgs> AudioInputFrameAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event IkonClient.AsyncEventHandler<EventArgs>? DisconnectedAsync
    event IkonClient.AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    event IkonClient.AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? ReadyAsync
    // Unlike the other events this one is not awaited by the transition that raised it: the client moves on as soon as the state is set, so a handler that awaits may observe State already past ConnectionStateEventArgs.State — read the state from the args. A handler exception is delivered to ErrorOccurredAsync rather than lost as an unobserved task.
    event IkonClient.AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    // Messages can still be sent from this handler.
    event IkonClient.AsyncEventHandler<EventArgs>? StoppingAsync
  delegate IkonClient.AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  class IkonClient.AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    bool IsFirst { get; }
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; }
  class IkonClient.AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    int ChannelCount { get; }
    AudioCodec Codec { get; }
    string CodecDetails { get; }
    string Description { get; }
    // A begin-event handler may set it to choose the rate the stream is decoded at.
    int SampleRate { get; set; }
    string SourceType { get; }
    string StreamId { get; }
    // Default Streaming; a begin-event handler may set it to delay frame delivery.
    AudioInputStreamingMode StreamingMode { get; set; }
  class IkonClient.AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId)
    string StreamId { get; }
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    ConnectionState State { get; }
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    Exception Error { get; }
  // Exactly one authentication mode — ExternalConnectUrl, Local, ApiKey, Backend, UserLogin, or ResumeAuthResponse — must be set; the constructor rejects zero or multiple.
  sealed record IkonClientConfig
    ctor()
    ApiKeyConfig? ApiKey { get; init; }
    BackendConfig? Backend { get; init; }
    // Default ContextType.Plugin connects as a backend component: no UI, no per-connection ClientScope. Set ContextType.Native (or ContextType.Browser) to connect as a first-class PLAYER client that receives a ClientScope and streamed UI, like the web client.
    ContextType ContextType { get; init; }
    // Default: "Ikon SDK C#"
    string Description { get; init; }
    // If not provided, a random one is generated.
    string? DeviceId { get; init; }
    // Whether to establish the unreliable UDP side channel alongside the TCP connection when the server advertises one. Default true. Set false to run over TCP only — unreliable-flagged messages then fall back to the reliable channel.
    bool EnableUdpChannel { get; init; }
    // When set, authentication is skipped and the client connects straight through this URL — the same mechanism the TypeScript SDK reads from its query parameter. Mutually exclusive with Local, ApiKey, Backend, and UserLogin.
    string? ExternalConnectUrl { get; init; }
    // Delivered to the app as Context.InitialPath at join, like a web client opening a deep link. Empty means the app's root.
    string InitialPath { get; init; }
    string? InstallId { get; init; }
    // Sets Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant. Only the build-time boot-snapshot capture client sets this; leave false otherwise.
    bool IsSnapshot { get; init; }
    LocalConfig? Local { get; init; }
    // Default: "en-US"
    string Locale { get; init; }
    // Default: All groups
    Opcode OpcodeGroupsFromServer { get; init; }
    // Default: All groups
    Opcode OpcodeGroupsToServer { get; init; }
    Dictionary<string, string>? Parameters { get; init; }
    // Default: Teleport
    PayloadType PayloadType { get; init; }
    string? ProductId { get; init; }
    // No authentication request is made; the transport opens straight from the response's entrypoints and auth ticket, so the server resumes that connection's client session — same session id and per-client state. Take it from IkonClient.LastAuthResponse. On a session minted with ikon-shared-session the original connection stays up beside this one; otherwise the server treats this as its replacement. There is nothing to re-authenticate with, so a lost transport is only recovered within the server's soft-disconnect grace.
    AuthResponse? ResumeAuthResponse { get; init; }
    // Boot-snapshot variant id this capture client asks the app to render, carried into Context.SnapshotVariant. Empty for route captures and all live clients; only variant captures set this (together with IsSnapshot).
    string SnapshotVariant { get; init; }
    TimeoutConfig Timeouts { get; init; }
    string? UserAgent { get; init; }
    UserLoginConfig? UserLogin { get; init; }
    // Version identifier, as a whole number in string form ("3"). Sent to the backend verbatim, but the ikon-server sees it as an integer: a value that does not parse as one is reported as version 1 and logged as a warning at connect time.
    string? VersionId { get; init; }
  sealed record LocalConfig
    ctor()
    string Host { get; init; }
    int HttpsPort { get; init; }
    // Falls back to "local" if not provided (with a warning).
    string? UserId { get; init; }
  class MessageEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // One registry per IkonClient: construct it over client.FunctionRegistry after the connection is established, and call Detach on teardown. The current value is fetched on first subscribe and pushed by the server on every change — no polling. Subscriptions live in the server session. A reconnect that resumes the same session keeps them, but one that creates a new session (ClientContext.SessionId changed since the subscribe) drops them server-side while the local callbacks stay registered and silently stop firing. Call ResubscribeAsync from the client's ReadyAsync handler to re-establish them.
  sealed class ReactiveRegistry
    ctor(FunctionRegistry functionRegistry)
    // Drop all subscriptions and unregister the update handler. Intended for client teardown — does not notify the server per key (the server's per-session subscription map is cleaned up when the session disconnects).
    void Detach()
    // Re-send Subscribe for every key that still has local subscribers and hand each of them the value the server returns. Call it after a reconnect that produced a new server session, where the server-side subscriptions are gone but the local callbacks remain. Safe to call after a reconnect that resumed the session — the server treats the repeat subscribe as a no-op and the callbacks simply receive the current value once more.
    // throws AggregateException: One or more keys could not be re-subscribed; the rest were.
    Task<int> ResubscribeAsync(CancellationToken cancellationToken = default)
    // Dispose the returned handle to unsubscribe — the last unsubscribe for a key notifies the server.
    // stableId: The reactive's IReactiveWithState.StableId.
    // callback: Invoked with each value. JSON is deserialized to T.
    // mountId: Mount id when subscribing to a server-side MountReactive<T>; empty (the default) works for unscoped Reactive<T>, ClientReactive<T>, and UserReactive<T>.
    // cancellationToken: Cancels the initial Subscribe call.
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  sealed record TimeoutConfig
    ctor()
    // When true, the client keeps retrying with capped exponential backoff after the fast reconnection ladder is exhausted, instead of staying Offline until the next explicit call. Default: true
    bool BackgroundReconnect { get; init; }
    // Each subsequent attempt doubles the delay (500ms, 1s, 2s, 4s). Default: 500 milliseconds
    TimeSpan InitialReconnectDelay { get; init; }
    // Maximum number of attempts for the initial connect. Retries are spaced by the same capped exponential backoff as the reconnection ladder, and only a transport-level failure is retried — a rejection the backend actually answered fails on the first attempt. Default: 3
    int MaxConnectAttempts { get; init; }
    // Default: 4
    int MaxReconnectAttempts { get; init; }
    // Upper bound for the delay between background reconnection attempts. Default: 30 seconds
    TimeSpan MaxReconnectDelay { get; init; }
    // Time budget for a single reconnection attempt (per tier), bounding an attempt against a half-open connection that would otherwise hang the recovery ladder. Default: 30 seconds
    TimeSpan ReconnectAttemptTimeout { get; init; }
  // Authenticate as the developer logged in on this machine (the ikon CLI's stored login). Connects through the cloud gateway exactly like a browser client, so gateway features — cell routing via Parameters above all — apply. Intended for dev tooling, spikes, and headless tests; production clients use ApiKeyConfig or BackendConfig.
  sealed record UserLoginConfig
    ctor()
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  // Build stamp for this component: the version of the build it was compiled from, exposed as a compile-time constant. Generated on every build from versions.json and git state, so never edit it by hand. Note that this type shadows System.Version in any file that imports this namespace — write System.Version explicitly there when you mean the BCL type.
  static class Version
    // The version this build was produced from, in the shape git describe uses: the release version, the number of commits since that release, the short commit hash, a -dirty suffix when the working tree had uncommitted changes, and the branch name on any branch other than main.
    const string VersionString


---

# Ikon AI C# SDK

The Ikon AI C# SDK provides a simple way to connect to Ikon AI App from any .NET application. It supports .NET 10 and .NET Standard 2.1 (including Unity).

## Features

- Five authentication modes: API Key, Local Development, Backend, External Connect URL, and UserLogin (developer CLI login, for dev tooling)
- Automatic reconnection with exponential backoff
- Audio streaming with Opus encoding/decoding
- Flexible audio streaming modes
- Function registration and remote invocation
- Low-level protocol message access

## Installation

Install the NuGet package:

```bash
dotnet add package Ikon.Sdk
```

## Quick Start

Add `using Ikon.Sdk;`, then:

```csharp
// Create configuration with API key authentication
var config = new IkonClientConfig
{
    ApiKey = new ApiKeyConfig
    {
        ApiKey = Environment.GetEnvironmentVariable("IKON_API_KEY")!,
        SpaceId = "your-space-id",
        ExternalUserId = "user-123"
    },
    Description = "My App"
};

// Create and connect the client
await using var client = new IkonClient(config);

client.ReadyAsync += async e =>
{
    Console.WriteLine("Connected!");
    await client.SignalReadyAsync();
};

client.MessageReceivedAsync += async e =>
{
    Console.WriteLine($"Received: {e.Message.Opcode}");
};

await client.ConnectAsync();
```

`ContextType` defaults to `ContextType.Plugin` — a headless backend component that
receives no UI and gets no per-connection `ClientScope`. That is correct for a bot or
service, but an interactive player client that expects to render UI or hold client state
must set `ContextType = ContextType.Native` (or `Browser`); otherwise it connects with no
error yet never receives a `ClientScope`.

## Transports

The SDK opens one reliable connection to the server — TCP with TLS against a hosted app, plain TCP
against a local one — and, alongside it, an unreliable UDP side channel used for messages the app
flags unreliable.

Set `EnableUdpChannel = false` to run over the reliable connection alone. Unreliable-flagged messages
then fall back to it, so nothing is lost; they just stop being able to overtake a queued reliable
message. Turn it off when the network between the client and the server drops or blocks UDP outright
(a corporate egress filter is the usual case, where the SDK would otherwise spend the DTLS handshake
timeout on every connect before giving up), or when you are connecting many clients from one process
and want each to cost as little server memory as possible — the side channel is a second socket, a
DTLS session and a second pair of send queues per client on both ends.

```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    EnableUdpChannel = false,
};
```

## Authentication Modes

The SDK supports five authentication modes. Exactly one must be configured:
`ApiKey`, `Local`, `Backend`, `ExternalConnectUrl`, or `UserLogin`. `UserLogin` authenticates as the
developer logged in on this machine (the ikon CLI's stored login) and is intended for dev tooling and
headless tests; production clients use `ApiKey` or `Backend`.

### API Key Authentication

Use this for programmatic access to Ikon AI App. Get your API key from the Ikon portal.

```csharp
var config = new IkonClientConfig
{
    ApiKey = new ApiKeyConfig
    {
        ApiKey = "ikon-xxxxx",           // API key from portal
        SpaceId = "...",                  // Space ID
        ExternalUserId = "user-123",      // Your user identifier
        SessionIdentityHash = "...",      // Optional: attach to a specific live session (connect fails if none owns this hash)
        BackendType = BackendType.Production,
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

### Local Development

Connect directly to a local Ikon server during development.

```csharp
var config = new IkonClientConfig
{
    Local = new LocalConfig
    {
        Host = "localhost",
        HttpsPort = 8443,
        UserId = "dev-user"
    }
};
```

### Backend Authentication

Use existing Ikon backend login credentials. This is for applications that have already authenticated to the backend.

```csharp
var config = new IkonClientConfig
{
    Backend = new BackendConfig
    {
        SpaceId = "...",
        ExternalUserId = "user-123",     // Your user identifier
        SessionIdentityHash = "...",     // Optional: attach to a specific live session (connect fails if none owns this hash)
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

### External Connect URL

Connect through a pre-minted connect URL (`{serverUrl}/connect?token=...`) issued by a trusted
host — for example an embedded in-process app server minting URLs for its own clients. The
authentication step is skipped entirely and the client connects straight through the URL. This
mode is mutually exclusive with the other four; a config that combines them is rejected.

```csharp
var config = new IkonClientConfig
{
    ExternalConnectUrl = connectUrl
};
```

### UserLogin

Authenticate as the developer logged in on this machine (the ikon CLI's stored login), connecting
through the cloud gateway like a browser client. Intended for dev tooling, spikes, and headless
tests — production clients use `ApiKey` or `Backend`. Mutually exclusive with the other four modes.

```csharp
var config = new IkonClientConfig
{
    UserLogin = new UserLoginConfig
    {
        SpaceId = "...",              // required
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

## Connection Lifecycle

### Connection States

The client tracks its connection state via the `State` property:

| State | Description |
|-------|-------------|
| `Idle` | Initial state, not connected |
| `Connecting` | Authentication and connection in progress |
| `Connected` | Fully connected and ready |
| `Reconnecting` | Lost connection, attempting automatic reconnect |
| `Offline` | Disconnected (connection failed or max retries exceeded) |

Helper extension methods are available:
- `state.IsConnecting()` - True if `Connecting` or `Reconnecting`
- `state.IsConnected()` - True if `Connected`
- `state.IsOffline()` - True if `Idle` or `Offline` (covers the pristine initial state too, not just failures)
- `state.IsFaulted()` - True only for `Offline` (a genuine failure) — use this, not `IsOffline`, to detect a dropped/failed connection

### Events

```csharp
// Connection state changes
client.StateChangedAsync += async e =>
{
    Console.WriteLine($"State: {e.State}");
};

// Connection established and ready
client.ReadyAsync += async e =>
{
    // Perform initialization here
    await client.SignalReadyAsync();  // Signal that this client is ready (mandatory)
};

// Server is stopping (can still send messages)
client.StoppingAsync += async e =>
{
    Console.WriteLine("Server stopping...");
};

// Disconnected from server
client.DisconnectedAsync += async e =>
{
    Console.WriteLine("Disconnected");
};

// Error occurred
client.ErrorOccurredAsync += async e =>
{
    Console.WriteLine($"Error: {e.Error.Message}");
};

// Protocol message received
client.MessageReceivedAsync += async e =>
{
    Console.WriteLine($"Message: {e.Message.Opcode}");
};
```

### Connecting and Disconnecting

```csharp
// Connect (will throw on failure)
await client.ConnectAsync();

// Wait for a specific client to connect
bool found = await client.WaitForClientAsync(
    productId: "my-product",
    userId: "user-123",
    timeout: TimeSpan.FromSeconds(30)
);

// Disconnect
await client.DisconnectAsync();

// Or dispose (also disconnects)
await client.DisposeAsync();
```

### Automatic Reconnection

The SDK automatically attempts to reconnect when the connection is lost unexpectedly. Configure reconnection behavior:

```csharp
var config = new IkonClientConfig
{
    // ... authentication config ...
    Timeouts = new TimeoutConfig
    {
        InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
        MaxReconnectAttempts = 4,                                 // Max attempts (default)
        MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
        ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per attempt (default)
        BackgroundReconnect = true                                // Keep retrying after max attempts (default)
    }
};
```

Reconnection uses exponential backoff starting from `InitialReconnectDelay` (500ms, 1s, 2s, 4s by default), capped at `MaxReconnectDelay` and jittered. When `BackgroundReconnect` is enabled (the default), the client keeps retrying with capped exponential backoff from the `Offline` state after the fast reconnection attempts are exhausted.

## Sending Messages

### Raw Protocol Messages

`ClientContext` is `null` until the client is connected, so read it only after
`ReadyAsync` has fired (or guard it). The `!` below is safe because a raw send happens
on a connected client:

```csharp
// Send a raw protocol message (on a connected client)
var message = ProtocolMessage.Create(client.ClientContext!.SessionId, payload);
await client.SendMessageAsync(message);
```

### Typed Payloads

```csharp
// Send a typed payload (creates ProtocolMessage automatically)
await client.SendMessageAsync(new MyCustomPayload { /* ... */ });
```

## Audio

The SDK provides comprehensive audio support with automatic Opus encoding/decoding.

### Sending Audio

Send audio to the server:

```csharp
// Get audio samples (float PCM, range [-1.0, 1.0])
ReadOnlyMemory<float> samples = GetAudioSamples();

// Send audio
await client.SendAudioAsync(
    samples: samples,
    sampleRate: 48000,
    channelCount: 1,
    isFirst: true,      // First chunk of this stream
    isLast: false       // More chunks coming
);

// Send final chunk
await client.SendAudioAsync(samples, 48000, 1, isFirst: false, isLast: true);

// Optional: specify stream ID, total duration, encoder options, and target clients
await client.SendAudioAsync(
    samples: samples,
    sampleRate: 48000,
    channelCount: 1,
    isFirst: true,
    isLast: true,
    streamId: "my-audio-stream",              // Unique stream identifier
    totalDuration: TimeSpan.FromSeconds(5),
    encoderOptions: new AudioEncoderOptions(  // Custom encoder settings
        bitrate: 64000,
        complexity: 10
    ),
    targetIds: new[] { 123, 456 }             // Target specific session IDs
);

// Set default encoder options for all audio
client.DefaultEncoderOptions = new AudioEncoderOptions(bitrate: 48000, complexity: 8);
```

### Receiving Audio

Subscribe to audio events to receive incoming audio streams:

```csharp
client.AudioInputStreamBeginAsync += async e =>
{
    Console.WriteLine($"Audio stream started: {e.StreamId}");
    Console.WriteLine($"  Codec: {e.Codec}");
    Console.WriteLine($"  Sample rate: {e.SampleRate}");
    Console.WriteLine($"  Channel count: {e.ChannelCount}");

    // Optional: override sample rate (SDK will resample)
    // e.SampleRate = 44100;

    // Optional: change streaming mode
    // e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
};

client.AudioInputFrameAsync += async e =>
{
    // e.Samples contains decoded PCM float samples
    float[] samples = e.Samples;

    Console.WriteLine($"Frame: {e.StreamId}");
    Console.WriteLine($"  Samples: {samples.Length}");
    Console.WriteLine($"  IsFirst: {e.IsFirst}");
    Console.WriteLine($"  IsLast: {e.IsLast}");
    Console.WriteLine($"  Total duration: {e.TotalDuration}");  // Zero if unknown

    // Process or play the audio samples...
};

client.AudioInputStreamEndAsync += async e =>
{
    Console.WriteLine($"Audio stream ended: {e.StreamId}");
};
```

### Audio Streaming Modes

Control how audio frames are delivered:

| Mode | Behavior |
|------|----------|
| `Streaming` | Forward frames immediately (lowest latency) |
| `DelayUntilTotalDurationKnown` | Buffer until the total duration is known, then stream |
| `DelayUntilIsLast` | Buffer everything, emit all frames when stream ends |

Set the streaming mode in the `AudioInputStreamBeginAsync` event handler:

```csharp
client.AudioInputStreamBeginAsync += async e =>
{
    // Buffer audio for UI timeline display
    e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
};
```

## Functions

The SDK provides a per-client function registry system that allows you to register callable functions that can be invoked locally or shared with other connected clients via the server. Each `IkonClient` has its own isolated `FunctionRegistry` accessible via `client.FunctionRegistry`.

### Registering Functions

**Attribute-Based Registration (Recommended)**

Mark methods with the `[Function]` attribute and register the containing class:

Add `using Ikon.Common.Core.Functions;`, then:

```csharp
public class MyFunctions
{
    [Function(Description = "Greets a user by name")]
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    [Function(Description = "Calculates sum", Visibility = FunctionVisibility.External)]
    public async Task<int> AddAsync(int a, int b)
    {
        return a + b;
    }

    [Function(Description = "Streams numbers")]
    public async IAsyncEnumerable<int> CountAsync(int max)
    {
        for (int i = 0; i < max; i++)
            yield return i;
    }
}
```

```csharp
// Register all [Function] methods from an instance
var myFuncs = new MyFunctions();
client.FunctionRegistry.RegisterFromInstance(myFuncs);

// Or register from a type (static methods only)
client.FunctionRegistry.RegisterFromType<MyStaticFunctions>();

// Or scan entire assembly
client.FunctionRegistry.RegisterFromAssembly(typeof(MyFunctions).Assembly);
```

**Manual Registration (Lambda/Delegate)**

Register functions directly using lambdas:

```csharp
// Simple synchronous function
client.FunctionRegistry.AddFunction(
    Function.Register((string name) => $"Hello, {name}!", "Greet")
);

// Async function
client.FunctionRegistry.AddFunction(
    Function.Register(async (int a, int b) =>
    {
        await Task.Delay(10);
        return a + b;
    }, "AddAsync")
);

// With attributes (description, visibility, etc.)
client.FunctionRegistry.AddFunction(
    Function.Register(
        (string query) => SearchDatabase(query),
        "Search",
        new FunctionAttribute { Description = "Searches the database", Visibility = FunctionVisibility.External }
    )
);
```

### Function Visibility

Functions can be either local or external:

- **Local** (default): Function is not advertised. Only callable within this process.
- **External**: Function is advertised over the protocol; remote clients can call it.

```csharp
// Local - only available in this process (default)
[Function(Visibility = FunctionVisibility.Local)]
public string LocalOnly() => "local";

// External - advertised over the protocol and callable by other clients
[Function(Visibility = FunctionVisibility.External)]
public string SharedWithAll() => "shared";
```

Visibility can also be overridden where the instance is registered:

```csharp
client.FunctionRegistry.RegisterFromInstance(myFuncs, FunctionVisibility.External);
```

### Discovering Functions

Query the registry to find available functions:

```csharp
// Check if a function exists
if (client.FunctionRegistry.HasFunction("MyFunc"))
{
    var func = client.FunctionRegistry.GetFunction("MyFunc");
    Console.WriteLine($"Found: {func?.Name}, Params: {func?.Parameters.Length}");
}

// Get all functions grouped by name (including remote)
var allFuncs = client.FunctionRegistry.Functions;

// Find which client sessions have a specific function
var clientIds = client.FunctionRegistry.GetClientSessionsWithFunction("SharedFunc");

// Wait for a function to become available (useful for coordination between clients)
bool available = await client.FunctionRegistry.WaitForFunctionAsync(
    "RemoteFunc",
    timeout: TimeSpan.FromSeconds(30)
);
```

### Calling Functions

Call registered functions locally or remotely:

```csharp
// Synchronous call
string result = client.FunctionRegistry.Call<string>("Greet", args: new object?[] { "World" });

// Async call
int sum = await client.FunctionRegistry.CallAsync<int>("AddAsync", args: new object?[] { 1, 2 });

// Void async call
await client.FunctionRegistry.CallAsync("LogMessage", args: new object?[] { "Hello" });

// Call a function on a specific remote client (uses targetId parameter)
int remoteSum = await client.FunctionRegistry.CallAsync<int>("Calculate", targetId: 123, args: new object?[] { 5, 10 });

// Streaming results (async enumerable)
await foreach (var item in client.FunctionRegistry.CallAsyncEnumerable<int>("CountAsync", args: new object?[] { 10 }))
{
    Console.WriteLine(item);
}
```

### Removing Functions

```csharp
// Remove a specific function by name (local functions only)
client.FunctionRegistry.RemoveFunction("MyFunc");

// Remove a function with specific visibility
client.FunctionRegistry.RemoveFunction("MyFunc", FunctionVisibility.External);

// Clear all local functions
client.FunctionRegistry.ClearLocalFunctions();
```

### Function Events

Subscribe to function registration events:

```csharp
client.FunctionRegistry.FunctionRegistered += func =>
{
    Console.WriteLine($"Registered: {func.Name} ({func.Visibility})");
};

client.FunctionRegistry.FunctionUnregistered += name =>
{
    Console.WriteLine($"Unregistered: {name}");
};
```

## Advanced Configuration

### Timeouts

```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    Timeouts = new TimeoutConfig
    {
        InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
        MaxReconnectAttempts = 4,                                 // Max reconnect attempts (default)
        MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
        ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per attempt (default)
        BackgroundReconnect = true                                // Keep retrying after max attempts (default)
    }
};
```

### Protocol Options

```csharp
var config = new IkonClientConfig
{
    // ... authentication ...

    // Filter which message types to receive/send
    OpcodeGroupsFromServer = Opcode.GROUP_ALL,
    OpcodeGroupsToServer = Opcode.GROUP_ALL,

    // Payload serialization format
    PayloadType = PayloadType.Teleport,  // Default

    // How this connection identifies to the server.
    // Default Plugin connects as a backend component (no UI).
    // Native or Browser connects as a first-class player client that receives streamed UI.
    ContextType = ContextType.Plugin
};
```

### Client Identification

```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    DeviceId = "unique-device-id",
    ProductId = "my-app",
    VersionId = "1.0.0",
    InstallId = "install-xyz",
    Locale = "en-US",
    Description = "My Application",
    UserAgent = "my-app/1.0.0",
    Parameters = new Dictionary<string, string>
    {
        ["custom_param"] = "value"
    }
};
```

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `IkonClient` | Main client class for connecting to Ikon servers |
| `IkonClientConfig` | Configuration for the client |
| `ConnectionState` | Enum: `Idle`, `Connecting`, `Connected`, `Reconnecting`, `Offline` |

### Configuration Types

| Type | Description |
|------|-------------|
| `LocalConfig` | Configuration for local server development |
| `ApiKeyConfig` | Configuration for API key authentication |
| `BackendConfig` | Configuration for backend authentication |
| `TimeoutConfig` | Timeout settings |
| `BackendType` | Enum: `Production`, `Development` |

### Audio Types

| Type | Description |
|------|-------------|
| `AudioInputStreamingMode` | Enum (from `Ikon.Resonance.Core`): `Streaming`, `DelayUntilTotalDurationKnown`, `DelayUntilIsLast` |
| `IkonClient.AudioInputStreamBeginEventArgs` | Event args for audio stream start |
| `IkonClient.AudioInputFrameEventArgs` | Event args for audio frame |
| `IkonClient.AudioInputStreamEndEventArgs` | Event args for audio stream end |
| `AudioEncoderOptions` | Options for configuring the Opus encoder |

### Function Types

| Type | Description |
|------|-------------|
| `FunctionRegistry` | Per-client registry for function registration and invocation |
| `Function` | Immutable function metadata and callback |
| `FunctionAttribute` | Attribute for marking methods as registerable functions |
| `RegisterAllAttribute` | Class-level attribute to auto-register all public members as functions |
| `FunctionVisibility` | Enum: `Local`, `External` |
| `FunctionParameter` | Parameter metadata (name, type, default value) |

### Event Types

| Type | Description |
|------|-------------|
| `MessageEventArgs` | Event args containing a `ProtocolMessage` |
| `IkonClient.ConnectionStateEventArgs` | Event args containing the new `ConnectionState` |
| `IkonClient.ErrorEventArgs` | Event args containing an `Exception` |

## License

This SDK is licensed under the Ikon AI SDK License. See `LICENSE` for details.

## Support

For issues and feature requests, contact Ikon support or open an issue on GitHub.
