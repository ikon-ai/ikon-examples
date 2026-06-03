# Endpoints & Webhooks

## Endpoints & Webhooks

Two separate mechanisms for exposing your AI app to the public internet. They solve different problems — pick the one that fits.

### Endpoints

Endpoints are public host:port tunnels to the running AI app instance. Create them at runtime via `new AppEndpointHost(app)` (for HTTP/HTTPS) or `app.RequestEndpointAsync` (for raw TCP/TLS/UDP). The platform allocates a public hostname and forwards traffic to a local port inside your app. Your app code is then responsible for binding to that local port and serving whatever it wants — HTTP, HTTPS, WebSockets, raw TCP, TLS, UDP, etc.

Key properties:
- **Protocol-agnostic.** Endpoints carry any TCP/UDP-based protocol, not just HTTP. You write the listener.
- **Only active while the app is running.** When the app stops, the endpoint disappears. There is no traffic queue and no way for an inbound request to start your app.
- **Public URL changes on every restart.** In both local dev and cloud, restarting the app gets you a fresh public hostname. Endpoints are not suitable for use cases where the URL must be stable (e.g. registering it once in a third-party dashboard).
- **No config file entry needed.** Endpoints are allocated dynamically at runtime — no `ikon-config.toml` declaration and no deploy-time registration.
- **Convenience class for HTTP:** `AppEndpointHost` wraps Kestrel and gives you `MapGet`/`MapPost`/`MapWebSocket` so you don't have to set up a listener manually.

### Webhooks

Webhooks are static, name-based HTTP entry points that route incoming requests to a `[Function(Webhook = true)]` method on your App class. The platform owns the URL, the routing, and the AI app lifecycle.

Key properties:
- **Stable public URL in cloud deployments.** The cloud URL is `https://{space}.ikonai.app/ikon/webhook/{functionName}` and does not change across restarts or redeploys, so you can register it once with Stripe/GitHub/Slack/etc.
- **In local dev the URL is NOT stable** — it uses the same public tunnel as endpoints, so it changes every time you restart the app with `--public-access`. Useful for testing real webhook traffic against your local app.
- **Webhooks will start the app if it is not running.** A webhook hit causes the platform to provision an AI app instance on demand (~2-3 s with warm pool), wait for it to become ready, and then deliver the function call. Endpoints cannot do this.
- **Always HTTP.** No TCP/UDP/WebSocket — webhooks are specifically for "external service POSTs JSON, function processes it, returns response".

### When to use which

| Use case | Use |
|---|---|
| Stripe / GitHub / Slack webhooks (need stable URL, app may be idle) | **Webhook function** |
| Custom REST API with multiple routes, auth, file uploads | **AppEndpointHost** |
| WebSocket server for a custom client | **AppEndpointHost** (`MapWebSocket`) |
| Game server / TCP / UDP / DTLS | **AppEndpointHost** + raw listener on the local port |
| Single "process this event" function | **Webhook function** |

---

## Endpoints

Endpoints are requested at runtime from your app code. No `ikon-config.toml` entry is needed.

### Creating and Starting an HTTP/HTTPS Endpoint

```csharp
var endpoint = new AppEndpointHost(app);

// Write the response via ctx.Response.Body (a Stream). NOT ctx.Response.WriteAsync(string)
// — that ASP.NET Core extension (Microsoft.AspNetCore.Http) is not in scope in a
// generated app and produces CS1061. Write UTF-8 bytes to the body stream.
endpoint.MapGet("/health", async ctx =>
{
    ctx.Response.ContentType = "text/plain";
    await ctx.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("OK"));
});

endpoint.MapPost("/data", async ctx =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    ctx.Response.ContentType = "application/json";
    await ctx.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("{\"received\": true}"));
});

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

## Webhooks

Mark any method with `[Function(Webhook = true)]` to expose it as a public HTTP endpoint. No `ikon-config.toml` entry is needed — webhook functions are discovered at deploy time and the deploy step prints the public URL for each one.

### Defining a webhook function

The method signature must be:
- Exactly three parameters: `(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)`.
- Return `string`, `Task<string>`, `void`, or `Task`. String returns become the HTTP response body; void/Task returns produce an empty 200.

Webhook functions defined directly on the **App class** (the class with the `[App]` attribute) are registered automatically. Webhook functions defined on **other classes** still get discovered at deploy time and get a public URL, but they will only respond to incoming calls once they are actually registered with the FunctionRegistry through the normal function registration mechanism. The deploy console marks these with `(needs manual registration)`.

```csharp
[App]
public class MyApp(IApp<SessionIdentity, ClientParams> app)
{
    [Function(Webhook = true, Name = "stripe", Description = "Stripe payment events")]
    public async Task<string> HandleStripe(
        Dictionary<string, string> queryParams,
        Dictionary<string, string> headers,
        string body)
    {
        var signature = headers["Stripe-Signature"];
        // Verify the signature against the body using your Stripe webhook secret...

        var stripeEvent = JsonSerializer.Deserialize<StripeEvent>(body);
        // Process the event...

        return "ok"; // becomes the HTTP response body
    }

    public async Task Main()
    {
        // app.Webhooks contains the live webhook URLs - log them or display in your UI
        foreach (var webhook in app.Webhooks)
        {
            Log.Instance.Info($"Webhook: POST {webhook.PublicUrl}");
        }
    }
}
```

### URL format

- **Cloud (stable):** `POST https://{space}.ikonai.app/ikon/webhook/{functionName}?userid=...&orgId=...`
- **Local dev (changes per restart):** `POST {server-public-tunnel}/webhook/{functionName}` — uses the same public tunnel the IkonServer exposes when run with `--public-access`. You can configure this URL in Stripe/GitHub/etc. dashboards while testing locally; just remember to update it when you restart the app.

The path segment `{functionName}` is the function's registered name — either the explicit `[Function(Name = "...")]` override or the auto-generated `TypeFullName.MethodName`. Use a `Name` override to keep URLs short and clean.

After deployment, webhook URLs are printed by `ikon app deploy`. At runtime they are available via `app.Webhooks`.

### Query params, session identity, and per-user routing

If your `SessionIdentity` includes `UserId` (or any other custom field), webhook callers must include those values as query params on the URL so the platform can route to the correct app instance. Example for `SessionIdentity { UserId, OrganizationId }`:

```
https://myapp.ikonai.app/ikon/webhook/stripe?userid=u1&organizationid=org5
```

The query params are also passed verbatim to the function via the `queryParams` argument, so the function can read them too. URLs surfaced via `app.Webhooks` already have the current session identity prefilled — give that URL to the external service when you register the webhook for that user.

### Authentication

There is no API key. Webhook endpoints are unauthenticated at the platform level — best practice for webhooks is for the **function itself** to verify the request, e.g. validating Stripe's `Stripe-Signature` HMAC against the request body using a shared signing secret. Read the relevant header from the `headers` argument inside your function.

### Cold start and provisioning

If no AI app instance is running for the matching session identity when a webhook arrives, the platform provisions one and waits for it to become ready (~2-3 s with warm pool, longer on cold start) before delivering the function call. Plan your function to be idempotent in case the external service retries on timeouts.

---

# Ikon.Sdk Public API

namespace Ikon.Sdk
  // Configuration for API key authentication mode. Use this for programmatic access to cloud channels.
  class ApiKeyConfig
    ctor()
    // API key for the space (from portal, format: 'ikon-xxxxx').
    string ApiKey { get; set; }
    // Backend environment. Defaults to Production.
    BackendType BackendType { get; set; }
    // Optional channel key (slug) for spaces with multiple channels. If not provided, connects to the first available channel.
    string? ChannelKey { get; set; }
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; set; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; set; }
    // Optional session ID for targeting precomputed sessions.
    string? SessionId { get; set; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; set; }
    // User type for this connection. Default: Human
    UserType UserType { get; set; }
  // Async event handler delegate.
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(object sender, TEventArgs e)
  // Event arguments raised when an incoming audio frame is received
  class AudioInputFrameEventArgs : EventArgs
    // Event arguments raised when an incoming audio frame is received
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    // Whether this is the first frame in a sequence
    bool IsFirst { get; }
    // Whether this is the last frame in a sequence
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; set; }
  // Event arguments raised when an incoming audio stream begins
  class AudioInputStreamBeginEventArgs : EventArgs
    // Event arguments raised when an incoming audio stream begins
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    // Number of audio channels
    int ChannelCount { get; }
    // Audio codec used for encoding/decoding
    AudioCodec Codec { get; }
    // Codec-specific details
    string CodecDetails { get; }
    // Description of the audio stream
    string Description { get; }
    // Sample rate in Hz (can be modified by event handler)
    int SampleRate { get; set; }
    // Source type of the audio stream (e.g., "microphone")
    string SourceType { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Controls when frames are output (can be modified by event handler)
    AudioInputStreamingMode StreamingMode { get; set; }
  // Event arguments raised when an incoming audio stream ends
  class AudioInputStreamEndEventArgs : EventArgs
    // Event arguments raised when an incoming audio stream ends
    ctor(string streamId)
    // Unique identifier for the audio stream
    string StreamId { get; }
  // Controls when incoming audio frames are output to listeners
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  // Configuration for backend authentication mode. Uses existing IkonBackend login credentials (from login.json or environment variables). This is the preferred mode for internal Ikon C# applications.
  class BackendConfig
    ctor()
    // Optional channel key (slug) for spaces with multiple channels. If not provided, connects to the first available channel.
    string? ChannelKey { get; set; }
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; set; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; set; }
    // Optional session ID for targeting precomputed sessions.
    string? SessionId { get; set; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; set; }
    // User type for this connection. Default: Human
    UserType UserType { get; set; }
  // Backend environment type.
  enum BackendType
    Production
    Development
  // Connection state of the IkonClient.
  enum ConnectionState
    Idle
    Connecting
    Connected
    Reconnecting
    Offline
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    ConnectionState State { get; }
  // Helper methods for ConnectionState.
  static class ConnectionStateExtensions
    // Returns true if the state represents a successful connection.
    static bool IsConnected(ConnectionState state)
    // Returns true if the state represents an active connection attempt.
    static bool IsConnecting(ConnectionState state)
    // Returns true if the state represents a disconnected state.
    static bool IsOffline(ConnectionState state)
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    Exception Error { get; }
  // Main client for connecting to Ikon servers. Features: - Single connection per client instance - Three authentication modes: Local, ApiKey, Backend - Automatic reconnection with exponential backoff - Audio encoding/decoding helpers - Function registration via FunctionRegistry
  sealed class IkonClient : IAsyncDisposable
    // Creates a new IkonClient with the specified configuration. Each IkonClient instance gets its own FunctionRegistry, enabling multiple SDK connections to run independently without conflicts (e.g., when running SDK inside an Ikon app).
    ctor(IkonClientConfig config)
    // Client context from the server. Available after connection is established.
    Context? ClientContext { get; }
    // Configuration used to create this client.
    IkonClientConfig Config { get; }
    // Default encoder options for audio output
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Function registry for this client instance. Each IkonClient has its own isolated FunctionRegistry, allowing multiple SDK connections to run independently (e.g., when running SDK inside an Ikon app, or multiple SDK clients).
    FunctionRegistry FunctionRegistry { get; }
    // Global state from the server. Available after connection is established.
    GlobalState? GlobalState { get; }
    // Current connection state.
    ConnectionState State { get; }
    // Connect to the Ikon server.
    Task ConnectAsync(CancellationToken ct = null)
    // Disconnect from the server and release connection-specific resources.
    Task DisconnectAsync()
    // Disposes the client and releases all resources.
    ValueTask DisposeAsync()
    // Sends audio data to the server.
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Send a protocol message to the server.
    ValueTask SendMessageAsync(ProtocolMessage message)
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Signal that the client is ready. Should be called after initialization in the ReadyAsync event handler.
    Task SignalReadyAsync()
    // Wait for a specific client to connect and become ready.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan timeout = null)
    // Event raised when an incoming audio frame is received and decoded
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Event raised when an incoming audio stream begins
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event triggered after disconnection.
    event AsyncEventHandler<EventArgs>? DisconnectedAsync
    // Event triggered when an error occurs.
    event AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    // Event triggered when a protocol message is received.
    event AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    // Event triggered when connection is fully established and ready. Called before SignalReadyAsync() should be called.
    event AsyncEventHandler<EventArgs>? ReadyAsync
    // Event triggered when connection state changes.
    event AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    // Event triggered when server is stopping. Messages can still be sent in this handler.
    event AsyncEventHandler<EventArgs>? StoppingAsync
  // Configuration for IkonClient. Exactly one of Local, ApiKey, or Backend must be provided.
  class IkonClientConfig
    ctor()
    // API key authentication for programmatic access. Use this for libraries, scripts, plugins that need to connect to cloud channels.
    ApiKeyConfig? ApiKey { get; set; }
    // Backend authentication using existing IkonBackend login. Use this for internal Ikon C# applications that have already logged in via CLI.
    BackendConfig? Backend { get; set; }
    // Description for this client. Default: "Ikon SDK C#"
    string Description { get; set; }
    // Device ID for the connection. If not provided, a random one will be generated.
    string? DeviceId { get; set; }
    // Installation ID.
    string? InstallId { get; set; }
    // Local server configuration for development mode. Use this when connecting to a local Ikon server.
    LocalConfig? Local { get; set; }
    // User locale (e.g., "en-US"). Default: "en-US"
    string Locale { get; set; }
    // Opcode groups to receive from server. Default: All groups
    Opcode OpcodeGroupsFromServer { get; set; }
    // Opcode groups to send to server. Default: All groups
    Opcode OpcodeGroupsToServer { get; set; }
    // Client parameters passed to the server.
    Dictionary<string, string>? Parameters { get; set; }
    // Payload type for protocol messages. Default: Teleport
    PayloadType PayloadType { get; set; }
    // Product identifier.
    string? ProductId { get; set; }
    // Timeout configuration.
    TimeoutConfig Timeouts { get; set; }
    // User agent string.
    string? UserAgent { get; set; }
    // Version identifier.
    string? VersionId { get; set; }
    // Validates the configuration.
    void Validate()
  // Configuration for local development mode. Connects directly to a local Ikon server.
  class LocalConfig
    ctor()
    // Host of the local Ikon server. Example: "localhost"
    string Host { get; set; }
    // HTTPS port of the local Ikon server. Example: 8443
    int HttpsPort { get; set; }
    // User ID for the connection. Falls back to "local" if not provided (with a warning).
    string? UserId { get; set; }
  // Event arguments for protocol messages.
  class MessageEventArgs : EventArgs
    // Event arguments for protocol messages.
    ctor(ProtocolMessage message)
    // The protocol message.
    ProtocolMessage Message { get; }
  // Subscribes local callbacks to a server-side Reactive`1 over the existing function-call wire. The current value is fetched on first subscribe and pushed by the server on every change — no polling.
  sealed class ReactiveRegistry
    // Create a registry over an IkonClient 's function registry. Registers the reactive-update handler immediately; call Detach on teardown.
    ctor(FunctionRegistry functionRegistry)
    // Drop all subscriptions and unregister the update handler. Intended for client teardown — does not notify the server per key (the server's per-session subscription map is cleaned up when the session disconnects).
    void Detach()
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = null)
  // Timeout configuration for the SDK.
  class TimeoutConfig
    ctor()
    // Initial delay before the first reconnection attempt. Each subsequent attempt doubles the delay (e.g. 500ms, 1s, 2s, 4s). Default: 500 milliseconds
    TimeSpan InitialReconnectDelay { get; set; }
    // Maximum number of reconnection attempts. Default: 4
    int MaxReconnectAttempts { get; set; }
  // Version class
  static class Version
    // Version string for the library
    static string VersionString


---

# Ikon AI C# SDK

The Ikon AI C# SDK provides a simple way to connect to Ikon AI App from any .NET application. It supports .NET 10 and .NET Standard 2.1 (including Unity).

## Features

- Three authentication modes: API Key, Local Development, Backend
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

client.ReadyAsync += async (sender, e) =>
{
    Console.WriteLine("Connected!");
    await client.SignalReadyAsync();
};

client.MessageReceivedAsync += async (sender, e) =>
{
    Console.WriteLine($"Received: {e.Message.Opcode}");
};

await client.ConnectAsync();
```

## Authentication Modes

The SDK supports three authentication modes. Exactly one must be configured.

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
        ChannelKey = "main",              // Optional: specific channel
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
        ChannelKey = "main",             // Optional
        SessionId = "session-xyz",       // Optional: target a precomputed session
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
- `state.IsOffline()` - True if `Idle` or `Offline`

### Events

```csharp
// Connection state changes
client.StateChangedAsync += async (sender, e) =>
{
    Console.WriteLine($"State: {e.State}");
};

// Connection established and ready
client.ReadyAsync += async (sender, e) =>
{
    // Perform initialization here
    await client.SignalReadyAsync();  // Signal that this client is ready (mandatory)
};

// Server is stopping (can still send messages)
client.StoppingAsync += async (sender, e) =>
{
    Console.WriteLine("Server stopping...");
};

// Disconnected from server
client.DisconnectedAsync += async (sender, e) =>
{
    Console.WriteLine("Disconnected");
};

// Error occurred
client.ErrorOccurredAsync += async (sender, e) =>
{
    Console.WriteLine($"Error: {e.Error.Message}");
};

// Protocol message received
client.MessageReceivedAsync += async (sender, e) =>
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
        MaxReconnectAttempts = 4                                  // Max attempts (default)
    }
};
```

Reconnection uses exponential backoff starting from `InitialReconnectDelay` (500ms, 1s, 2s, 4s by default).

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
    encoderOptions: new AudioEncoderOptions   // Custom encoder settings
    {
        Bitrate = 64000,
        Complexity = 10
    },
    targetIds: new[] { 123, 456 }             // Target specific session IDs
);

// Set default encoder options for all audio
client.DefaultEncoderOptions = new AudioEncoderOptions
{
    Bitrate = 48000,
    Complexity = 8
};
```

### Receiving Audio

Subscribe to audio events to receive incoming audio streams:

```csharp
client.AudioInputStreamBeginAsync += async (sender, e) =>
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

client.AudioInputFrameAsync += async (sender, e) =>
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

client.AudioInputStreamEndAsync += async (sender, e) =>
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
client.AudioInputStreamBeginAsync += async (sender, e) =>
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
        MaxReconnectAttempts = 4                                  // Max reconnect attempts (default)
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
    PayloadType = PayloadType.Teleport  // Default
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
| `AudioInputStreamingMode` | Enum: `Streaming`, `DelayUntilTotalDurationKnown`, `DelayUntilIsLast` |
| `AudioInputStreamBeginEventArgs` | Event args for audio stream start |
| `AudioInputFrameEventArgs` | Event args for audio frame |
| `AudioInputStreamEndEventArgs` | Event args for audio stream end |
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
| `ConnectionStateEventArgs` | Event args containing the new `ConnectionState` |
| `ErrorEventArgs` | Event args containing an `Exception` |

## License

This SDK is licensed under the Ikon AI SDK License. See `LICENSE` for details.

## Support

For issues and feature requests, contact Ikon support or open an issue on GitHub.
