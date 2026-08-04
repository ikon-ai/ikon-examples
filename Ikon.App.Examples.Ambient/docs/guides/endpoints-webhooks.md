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

Valid raw protocols: `EndpointProtocol.Tcp`, `Tls`, `Udp`. `Tls` enables TLS termination at the relay (your listener sees plaintext on the local port). For HTTP/HTTPS use `AppEndpointHost` with `secure: true` (default) or `secure: false`.

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

```csharp
[App]
public class MyApp(IApp<SessionIdentity, ClientParams> app)
{
    public record SessionIdentity(string? UserId);

    // The JSON body binds to your typed parameter. The binder is lenient — missing
    // fields default, unknown fields are ignored, and bad input returns a 4xx (it never throws a 500).
    [HttpPost("/sum")]
    public HttpResult Sum(SumRequest req) => HttpResult.Ok(new { sum = req.A + req.B });

    // Explicit verb, no body. Return a value (→ JSON), a string (→ text/plain), or an HttpResult.
    [HttpGet("/health")]
    public string Health() => "ok";

    // A third-party webhook is a normal [HttpPost]. Read the signature header + raw body from the
    // injected Ikon.App.HttpRequest and verify it yourself — the platform does NOT authenticate the URL.
    [HttpPost("/stripe")]
    public async Task<HttpResult> Stripe(Ikon.App.HttpRequest req)
    {
        if (!VerifyStripe(req.Headers["Stripe-Signature"], req.Body)) return HttpResult.Unauthorized();
        // ... process req.Body ...
        return HttpResult.Ok();   // return 200 even on a skip to avoid the provider's retry storm
    }

    // An MCP tool, callable by an LLM / agent. Its JSON Schema is reflected from the signature.
    [Mcp(Name = "add_numbers", Description = "Adds two integers")]
    public int AddNumbers(int a, int b) => a + b;
}

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
    string ApiKey { get; init; }
    BackendType BackendType { get; init; }
    ClientType ClientType { get; init; }
    string ExternalUserId { get; init; }
    string? SessionIdentityHash { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  sealed record BackendConfig
    ctor()
    ClientType ClientType { get; init; }
    string ExternalUserId { get; init; }
    string? SessionIdentityHash { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  enum BackendType
    Production
    Development
  enum ConnectionState
    Idle
    Connecting
    Connected
    Reconnecting
    Offline
  static class ConnectionStateExtensions
    static bool IsConnected(this ConnectionState state)
    static bool IsConnecting(this ConnectionState state)
    static bool IsOffline(this ConnectionState state)
  sealed class IkonClient : IAsyncDisposable
    ctor(IkonClientConfig config)
    Context? ClientContext { get; }
    IkonClientConfig Config { get; }
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    FunctionRegistry FunctionRegistry { get; }
    GlobalState? GlobalState { get; }
    ConnectionState State { get; }
    // Valid only from ConnectionState.Idle or ConnectionState.Offline; throws InvalidOperationException if already connecting or connected. On failure the client returns to ConnectionState.Offline and the exception is rethrown.
    Task ConnectAsync(CancellationToken ct = default)
    Task DisconnectAsync()
    ValueTask DisposeAsync()
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // No-op (logs a warning) when not connected; never throws.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // No-op (logs a warning) when not connected; never throws.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Call once your setup completes, typically from the ReadyAsync handler. Throws if not connected.
    Task SignalReadyAsync()
    // Waits up to timeout (30 seconds when left as default) for a client matching productId/userId. Throws if not connected.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan timeout = default)
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputFrameEventArgs> AudioInputFrameAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event IkonClient.AsyncEventHandler<EventArgs>? DisconnectedAsync
    event IkonClient.AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    event IkonClient.AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? ReadyAsync
    event IkonClient.AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? StoppingAsync
  delegate IkonClient.AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  class IkonClient.AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    bool IsFirst { get; }
    bool IsLast { get; }
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
  class IkonClient.AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    int ChannelCount { get; }
    AudioCodec Codec { get; }
    string CodecDetails { get; }
    string Description { get; }
    int SampleRate { get; set; }
    string SourceType { get; }
    string StreamId { get; }
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
  // Exactly one authentication mode — ExternalConnectUrl, Local, ApiKey, or Backend — must be set; the constructor rejects zero or multiple.
  sealed record IkonClientConfig
    ctor()
    ApiKeyConfig? ApiKey { get; init; }
    BackendConfig? Backend { get; init; }
    // Default ContextType.Plugin connects as a backend component: no UI, no per-connection ClientScope. Set ContextType.Native (or ContextType.Browser) to connect as a first-class PLAYER client that receives a ClientScope and streamed UI, like the web client.
    ContextType ContextType { get; init; }
    string Description { get; init; }
    string? DeviceId { get; init; }
    bool EnableUdpChannel { get; init; }
    // When set, authentication is skipped and the client connects straight through this URL — the same mechanism the TypeScript SDK reads from its query parameter. Mutually exclusive with Local, ApiKey, and Backend.
    string? ExternalConnectUrl { get; init; }
    // Delivered to the app as Context.InitialPath at join, like a web client opening a deep link. Empty means the app's root.
    string InitialPath { get; init; }
    string? InstallId { get; init; }
    // Sets Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant. Only the build-time boot-snapshot capture client sets this; leave false otherwise.
    bool IsSnapshot { get; init; }
    LocalConfig? Local { get; init; }
    string Locale { get; init; }
    Opcode OpcodeGroupsFromServer { get; init; }
    Opcode OpcodeGroupsToServer { get; init; }
    Dictionary<string, string>? Parameters { get; init; }
    PayloadType PayloadType { get; init; }
    string? ProductId { get; init; }
    string SnapshotVariant { get; init; }
    TimeoutConfig Timeouts { get; init; }
    string? UserAgent { get; init; }
    UserLoginConfig? UserLogin { get; init; }
    string? VersionId { get; init; }
  sealed record LocalConfig
    ctor()
    string Host { get; init; }
    int HttpsPort { get; init; }
    string? UserId { get; init; }
  class MessageEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // One registry per IkonClient: construct it over client.FunctionRegistry after the connection is established, and call Detach on teardown. The current value is fetched on first subscribe and pushed by the server on every change — no polling.
  sealed class ReactiveRegistry
    ctor(FunctionRegistry functionRegistry)
    void Detach()
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  sealed record TimeoutConfig
    ctor()
    bool BackgroundReconnect { get; init; }
    TimeSpan InitialReconnectDelay { get; init; }
    int MaxReconnectAttempts { get; init; }
    TimeSpan MaxReconnectDelay { get; init; }
    TimeSpan ReconnectAttemptTimeout { get; init; }
  sealed record UserLoginConfig
    ctor()
    ClientType ClientType { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  static class Version
    const string VersionString


---

# Ikon AI C# SDK

The Ikon AI C# SDK provides a simple way to connect to Ikon AI App from any .NET application. It supports .NET 10 and .NET Standard 2.1 (including Unity).

## Features

- Four authentication modes: API Key, Local Development, Backend, External Connect URL
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

```csharp
using Ikon.Sdk;

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

## Authentication Modes

The SDK supports four authentication modes. Exactly one must be configured:
`ApiKey`, `Local`, `Backend`, or `ExternalConnectUrl`.

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
        SessionId = "session-xyz",        // Optional: target a precomputed session
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
        SessionId = "session-xyz",       // Optional: target a precomputed session
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

### External Connect URL

Connect through a pre-minted connect URL (`{serverUrl}/connect?token=...`) issued by a trusted
host — for example an embedded in-process app server minting URLs for its own clients. The
authentication step is skipped entirely and the client connects straight through the URL. This
mode is mutually exclusive with the other three; a config that combines them is rejected.

```csharp
var config = new IkonClientConfig
{
    ExternalConnectUrl = connectUrl
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
- `state.IsOffline()` - True if `Idle` or `Offline`

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

```csharp
// Send a raw protocol message
var message = ProtocolMessage.Create(client.ClientContext.SessionId, payload);
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

```csharp
using Ikon.Common.Core.Functions;

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

// Override visibility at registration time
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
