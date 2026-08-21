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

`ContextType` defaults to `ContextType.Plugin` — a headless backend component that
receives no UI and gets no per-connection `ClientScope`. That is correct for a bot or
service, but an interactive player client that expects to render UI or hold client state
must set `ContextType = ContextType.Native` (or `Browser`); otherwise it connects with no
error yet never receives a `ClientScope`.

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
