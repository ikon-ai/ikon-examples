<!-- checked-against: b6db20b24554531bf25ad001 -->

# IKON C# SDK

The IKON C# SDK connects any .NET application to an IKON app. It supports .NET 10 and .NET Standard 2.1 (including Unity).

## Features

- Eight authentication modes: API Key, Local Development, Backend, External Connect URL, UserLogin (developer CLI sign-in, for dev tooling), UserToken (a person's session token for one space, issued elsewhere), Guest (a user who has not signed in), and ResumeAuthResponse (joining another connection's client session)
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

Add `using Ikon.Sdk;` and `using Ikon.Common.Core.Protocol;` (the `UserType` and `ClientType` enums live there), then:

<!-- ikon-example: sdk-quickstart -->
```csharp
// Create configuration with API key authentication
var config = new IkonClientConfig
{
    ApiKey = new ApiKeyConfig
    {
        ApiKey = Environment.GetEnvironmentVariable("IKON_API_KEY")!,
        SpaceId = "your-space-id",
        ExternalUserId = "user-123",
        BackendType = BackendType.Production // Development for an app on *.dev.ikonai.app
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

The SDK opens one reliable connection to the server — TCP with TLS first (a local server offers it
too), then plain TCP, then a WebSocket when the TCP ports do not answer — and, alongside it, an
unreliable UDP side channel used for messages the app flags unreliable. A client that ends up on a
WebSocket opens no side channel. The side channel is encrypted with DTLS, which the .NET Standard 2.1 build lacks, so that
build runs over the reliable connection alone.

Set `EnableUdpChannel = false` to run over the reliable connection alone. Messages flagged unreliable
are then sent over it, though one is still dropped rather than waited on when the send queue is full,
and they can no longer overtake a queued reliable message.
Turn it off in two cases. One is when the network between the client and the server drops or blocks
UDP, usually because of a corporate egress filter. The connect does not wait for UDP, but every
connect would otherwise spend up to the 15-second DTLS handshake timeout on a background attempt that
cannot succeed. The other is when you connect many clients from one process and want each to cost as
little server memory as possible: the side channel is a second socket, a DTLS session and a second
pair of send queues per client on both ends.

<!-- ikon-example: sdk-disable-udp -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    EnableUdpChannel = false,
};
```

## Authentication Modes

The SDK supports eight authentication modes. Exactly one must be configured, or the `IkonClient` constructor throws `ArgumentException`:
`ApiKey`, `Local`, `Backend`, `ExternalConnectUrl`, `UserLogin`, `UserToken`, `Guest`, or `ResumeAuthResponse` (another connection's `IkonClient.LastAuthResponse`, to join its client session). `UserLogin` authenticates as the
developer signed in on this machine (the ikon CLI's stored sign-in) and is intended for dev tooling and
headless tests; production clients use `ApiKey` or `Backend`. `UserToken` connects as a person with a session
token for the space that was issued elsewhere. The constructor also throws `ArgumentException`
when the chosen mode lacks a required field: `ApiKey`'s `ApiKey`, `SpaceId` and `ExternalUserId`, `Backend`'s
`SpaceId` and `ExternalUserId`, `UserLogin`'s or `Guest`'s `SpaceId`, `UserToken`'s `SpaceId`, `Token` and `BackendUrl`, or `Local`'s `Host` and a positive `HttpsPort`.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once.

<!-- ikon-example: sdk-api-key-config -->
```csharp
var config = new IkonClientConfig
{
    ApiKey = new ApiKeyConfig
    {
        ApiKey = "ikon-xxxxx",           // From `ikon api-key create` in the app's folder
        SpaceId = "...",                  // The app's id
        ExternalUserId = "user-123",      // Your user identifier
        SessionIdentityHash = "...",      // Optional: attach to a specific live session (connect fails if none owns this hash)
        BackendType = BackendType.Production, // Development for an app on *.dev.ikonai.app
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: sdk-local-config -->
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

A local connection is a machine user by default. A client acting for a signed-in person sets `UserType = UserType.Human` and `AuthSessionId` to the `authSession` claim of that person's backend token, as the browser SDK's local connect does, and `ClientType` to the kind of client it is.

### Backend Authentication

Use existing IKON backend sign-in credentials. This is for applications that have already authenticated to the backend.

<!-- ikon-example: sdk-backend-config -->
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
authentication step is skipped entirely and the client connects straight through the URL. The
client still describes itself on the request: its `InitialPath`, `IsSnapshot`, `StyleFormat`,
viewport and description come from the config. The client type always comes from the token the
URL carries, and so do the initial path, its URL, the snapshot flag and the snapshot variant when
the config leaves them unset. A URL whose minter already put a client description on it keeps that
description, and none of the config's values are sent. This mode is mutually exclusive with the other seven; a config that
combines them is rejected.

<!-- ikon-example: sdk-external-connect-url -->
```csharp
var config = new IkonClientConfig
{
    ExternalConnectUrl = connectUrl
};
```

### UserLogin

Authenticate as the developer signed in on this machine (the ikon CLI's stored sign-in), connecting
through the cloud gateway like a browser client. Intended for dev tooling, spikes, and headless
tests — production clients use `ApiKey` or `Backend`. Mutually exclusive with the other seven modes.
`Environment` names the platform the app is on. If it names a platform other than the process's own,
the client signs in with that platform's saved sign-in. Otherwise, or when it is null, the process's
own sign-in is used.

<!-- ikon-example: sdk-user-login-config -->
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

### UserToken

`UserToken = new UserTokenConfig { SpaceId = "...", Token = "...", BackendUrl = "..." }` connects as a
person with a session token for the space that was issued elsewhere, through the cloud gateway like
a browser client. It is how an app's server shows another app inside itself (`view.Portal` in the
Parallax guide): the backend hands the host a session in the guest app for one of its users, and the
host connects with it. The token's own lifetime limits the connection: a reconnect after it expires
fails, and the program that holds the token has to get a new one. `BackendUrl` is the backend that
issued the token, which `/init` is posted to; `UserType`, `ClientType` and `JoinOnly` have the same
meaning as in `UserLogin`.

### Guest

`Guest = new GuestConfig { SpaceId = "..." }` connects to a deployed app as a guest, the same way the
browser SDK connects a user who has not signed in. It needs no sign-in. The platform picks the guest
user by `IkonClientConfig.DeviceId`: two clients with the same device id are the same guest, and a new
device id is a new guest. If the app's guest entries include "global", the device id is ignored and
all guests share one user. If the app lets no guests in, or the surface the program enters
(`IkonClientConfig.Surface`, else, leaving out sign-in pages, the only surface left or the one with
neither `Routes` nor `Host`, else the first declared) does not, the connect
fails with a 403. `Environment` (`IkonBackend.EnvironmentType`, in `Ikon.Common.Core`) names the
platform the app is on, Development or Production. Null uses the platform this process signed in to.
A standalone program has not signed in, so it must set `Environment`; otherwise `ConnectAsync` throws
`ArgumentException` ("Guest authentication needs the Development or Production platform, not
Unknown"). For a local server, `LocalConfig.IsAnonymous = true` connects as a guest in the same way,
under the `UserId` you give it.

## A Console Program That Joins An App

A kiosk, a bot or a script that shows an app's live state or calls its functions is a console
program with this SDK. Kept with an app, it goes in the app's `devices/<name>/` folder, which is
kept in the app's repository (the save before a deploy commits it) but is not built or run by the
deploy or by `ikon run`. Create it with `dotnet new console -n <Name>`, which puts the project in a `<Name>/` subfolder of `devices/<name>/`, and add the package from inside that subfolder with
`dotnet add package Ikon.Sdk --version <version>`, taking the `Ikon.Sdk` version the app's csproj
lists in its `PackageReference`. The SDK is released on its own version line, apart from the app's
other `Ikon.*` libraries, and is on nuget.org. That published package also works against an app
that this machine builds from platform sources, locally and deployed, so the program needs no
source build of its own. When `ikon update` raises the versions in the app's csproj, raise the
program's `Ikon.Sdk` to match. `OnMessage<T>`, `IkonClientConfig.LogFilter`, the console log at
`Info` on stderr and `LocalConfig.SpaceId` need `Ikon.Sdk` 2.0.54 or later; `ikon update` raises an
app whose csproj lists an earlier one. An earlier package logs
every connect step to stdout; `Log.Instance.Filter = LogFilter.Info;` before creating the client
keeps the steps out.

`Program.cs` starts with:

<!-- ikon-example: sdk-console-usings -->
```csharp
using Ikon.Common.Core;
using Ikon.Common.Core.Protocol;
using Ikon.Sdk;
```

Which authentication mode to use depends on where the app runs:

| The app | Mode | What it needs |
|---|---|---|
| A local `ikon run` | `Local` | `HttpsPort`: the port of the Server URL the run prints (8443 unless another run held it), also in `serverUrl` of `build/app/artifacts/bin/<project>/debug/ikon-server-info.json`. `SpaceId`: the app id, so a connect to a server that runs another app throws instead of joining it. Without it the program joins whatever app holds the port |
| Deployed | `Guest` | `SpaceId`: the app id (`[Target] SpaceId` in `ikon-config.<environment>.toml`). `Environment`: `Development` for an app on the development platform (`*.dev.ikonai.app`), otherwise `Production`. The surface the program enters (`IkonClientConfig.Surface`, else, leaving out sign-in pages, the only surface left, else the one with neither `Routes` nor `Host`, else the first declared) must list `guest` or `global` in its `Methods` |
| Deployed, for users of your own service | `ApiKey` | A space API key, made with `ikon api-key create` (or in the Portal under the app's API keys); `ExternalUserId` names the user it connects as, and `BackendType` the platform as for `Guest`. The surface needs no `guest` or `global` entry in `Methods`. Any holder of the key can join as any `ExternalUserId`, so it stays in the program's secret store. `ikon auth token create` makes a service token for CI, not an API key |

<!-- ikon-example: sdk-console-client -->
```csharp
// The app id: [Target] SpaceId in the app's ikon-config.<environment>.toml
const string appId = "your-app-id";

// For a local `ikon run`, use Local with the port of the Server URL that the run prints (pass it as
// `--local 8443`); with SpaceId set, a port that serves another app makes the connect throw. For the
// deployed app, join as a guest, which needs no key; Environment is required.
var localIndex = Array.IndexOf(args, "--local");
var local = localIndex >= 0;
var localPort = local && localIndex + 1 < args.Length ? int.Parse(args[localIndex + 1]) : 8443;
var config = local
    ? new IkonClientConfig { Local = new LocalConfig { Host = "localhost", HttpsPort = localPort, UserId = "door-kiosk", SpaceId = appId } }
    : new IkonClientConfig
    {
        Guest = new GuestConfig { SpaceId = appId, Environment = IkonBackend.EnvironmentType.Development },
        DeviceId = "door-kiosk",
    };

await using var client = new IkonClient(config);
client.ReadyAsync += async e => await client.SignalReadyAsync();
await client.ConnectAsync();

// The app looks up a reactive's stable id by the member's name in its [App] class
var stableId = await client.FunctionRegistry.CallAsync<string>("Ikon.Reactive.GetStableIdByName", args: ["_visitorCount"]);

// The callback runs once with the current value, then on every change
var reactives = new ReactiveRegistry(client);
await using var visitors = await reactives.SubscribeAsync<int>(stableId, count => Console.WriteLine($"Visitors today: {count}"));

// Call an External [Function] of the app, passing the key it checks
var answer = await client.FunctionRegistry.CallAsync<string>("RecordVisit",
    args: [Environment.GetEnvironmentVariable("KIOSK_KEY"), "Front door"]);
```

The app's side, on its `[App]` class:

<!-- ikon-example: sdk-console-app-side -->
```csharp
// On the [App] class: a shared reactive the program reads by its name
private readonly Reactive<int> _visitorCount = new(0);

// Any program can join as a guest, so a call that changes data sends a key the app checks
[Function(Name = "RecordVisit", Visibility = FunctionVisibility.External)]
[AllowAnonymous]
public string RecordVisit(string key, string door)
{
    if (!app.Secrets.TryGet("KIOSK_KEY", out var expected)
        || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(expected)))
    {
        return "denied";
    }

    _visitorCount.Value++;
    return $"ok, {door}";
}
```

- **Which reactive.** `Ikon.Reactive.GetStableIdByName` finds a reactive field or auto-property of
  the app's `[App]` class by its name in the source. A reactive kept anywhere else has no name there:
  the app hands out its `StableId` from an `External` function instead. A stable id is made from
  where the reactive is declared, so it stays the same across restarts and deploys until that code
  moves.
- **Which instance.** The program joins the instance its identity maps to. With the scaffold's
  per-user `SessionIdentity(string? UserId)` that is an instance of its own, for its own guest or
  local user, so it does not see what other people's instances hold. To share state with everyone,
  keep it in an app with one instance for everyone (`public record SessionIdentity();`), in a
  `GlobalReactive` or `PersistentGlobalReactive` field, which every instance shares (a program
  subscribed to it usually sees another instance's write within seconds), or in the database. Count
  in a global value with `Update(v => v + 1)`, which runs again over a newer value another instance
  wrote. An assignment such as `Value++` is last-writer-wins, so two instances counting at once lose
  a visit.
- **Values.** `SubscribeAsync<T>` reads the value as JSON into `T`: `int` for a `Reactive<int>`, a
  `List<T>` for a `ReactiveList<T>`, `JsonElement` to look at any of them. A `ClientReactive` gives
  the program its own copy, not a browser's.
- **Who can read a reactive.** Any client that joins the instance, a guest included, can subscribe
  to every reactive of the `[App]` class by its name, private fields too, and to any other reactive
  whose stable id it learns. Nothing marks a reactive as server-only. A `ClientReactive` or
  `UserReactive` gives each caller only its own value; keep anything a guest must not see out of
  shared reactives, in a plain field or the database, and hand it out from a function that checks
  the caller.
- **A function that returns an app type.** The result arrives as JSON with the C# property names
  as they are (`Name`, `SinceUtc`) and enums as their names. Ask for `JsonElement` and read it with
  `GetProperty("Name")`, or declare a record with the same property names in the program and ask
  for it (`CallAsync<RosterEntry[]>`); the names are matched with their case. A `schema/*.tp` type
  arrives in its binary form instead, and needs the app's generated class.
- **Keys.** Anyone can join an app that lets guests in, so a function that changes data checks a
  key it is given against an app secret (`ikon secret set KIOSK_KEY`), and the program reads the
  key from its environment.
- `ContextType.Plugin`, the default, is the right one here, because the program draws no UI.
- **Output.** In a program that has not used `Log.Instance` before it creates the client, the SDK
  logs at `LogFilter.Info` to stderr, so stdout holds only what the program prints. Set
  `LogFilter = LogFilter.Debug` in `IkonClientConfig` to see each step of a connect that fails.
- **Typed messages.** A device that sends readings as the app's `schema/*.tp` types, and waits for
  an ack, is the `device-typed-messages` pattern; see "Receiving Typed Messages" below.

A program that should keep running while the app restarts (`ikon stop` and a new `ikon run`, a
deploy, `ikon session stop`) connects again itself. The client reconnects by itself after a dropped
connection, but when the server stops, the client goes `Offline` and stays there. A loop at the end
of the program connects it again:

<!-- ikon-example: sdk-console-stay-connected -->
```csharp
// Connecting again joins the app's next server, and the ReactiveRegistry subscribes again by itself
while (true)
{
    await Task.Delay(TimeSpan.FromSeconds(15));

    if (client.State != ConnectionState.Offline)
    {
        continue;
    }

    try
    {
        await client.ConnectAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Not connected: {ex.Message}");
    }
}
```

Against a deployed app, each connect starts the app's server if none is running.

## Connection Lifecycle

### Connection States

The client tracks its connection state via the `State` property:

| State | Description |
|-------|-------------|
| `Idle` | Initial state, not connected |
| `Connecting` | Authentication and connection in progress |
| `Connected` | Fully connected and ready |
| `Reconnecting` | Lost connection, attempting automatic reconnect |
| `Offline` | Disconnected: the connection failed, the reconnect ladder was exhausted, or the server shut down. Not terminal after an exhausted ladder: with `BackgroundReconnect` on (the default) the client keeps retrying and can return to `Connected` |

Helper extension methods are available:
- `state.IsConnecting()` - True if `Connecting` or `Reconnecting`
- `state.IsConnected()` - True if `Connected`
- `state.IsDisconnected()` - True if `Idle` or `Offline` (covers the pristine initial state too, not just failures)
- `state.IsFaulted()` - True only for `Offline` (a genuine failure) — use this, not `IsDisconnected`, to detect a dropped/failed connection; it is not final while the background reconnect runs, since that can still bring the client back

### Events

<!-- ikon-example: sdkx-events -->
```csharp
// Connection state changes
client.StateChangedAsync += async e =>
{
    Console.WriteLine($"State: {e.State}");
};

// Connection established and ready, raised again on every reconnect
client.ReadyAsync += async e =>
{
    // Perform initialization here. e.IsReconnect is false only for the first connect;
    // e.IsNewSession says the server session changed and its server-side state is gone
    await client.SignalReadyAsync();  // Signal that this client is ready (mandatory)
};

// Server is stopping (can still send messages)
client.StoppingAsync += async e =>
{
    Console.WriteLine("Server stopping...");
};

// Disconnected: the client gave up and went Offline — every reconnect attempt failed, or
// the server is stopping. With BackgroundReconnect on (the default) a failed reconnect keeps
// retrying from Offline, so this is final only when the server is stopping,
// BackgroundReconnect is off, or a JoinOnly client found the session ended. Not raised by a failed IkonClient.ConnectAsync (that is the
// call's exception), by IkonClient.DisconnectAsync or IkonClient.DisposeAsync, nor by a
// drop that reconnects
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

<!-- ikon-example: sdk-lifecycle -->
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

<!-- ikon-example: sdk-timeouts -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication config ...
    Timeouts = new TimeoutConfig
    {
        InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
        MaxReconnectAttempts = 4,                                 // Max attempts (default)
        MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
        ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
        BackgroundReconnect = true                                // Keep retrying after max attempts (default)
    }
};
```

Reconnection uses exponential backoff starting from `InitialReconnectDelay` (500ms, 1s, 2s, 4s by default), capped at `MaxReconnectDelay` and jittered. When `BackgroundReconnect` is enabled (the default), the client keeps retrying with capped exponential backoff from the `Offline` state after the fast reconnection attempts are exhausted. A `JoinOnly` client whose reconnect finds the session ended does not: it stays `Offline`.

## Sending Messages

### Raw Protocol Messages

`ClientContext` is `null` before the first connect and after a disconnect, and while
connecting it holds a placeholder until the server's context arrives, so read it only after
`ReadyAsync` has fired; a null check does not prove the client is connected. The `!` below is safe because a raw send happens
on a connected client:

<!-- ikon-example: sdk-send-raw -->
```csharp
// Send a raw protocol message (on a connected client)
var message = ProtocolMessage.Create(client.ClientContext!.SessionId, payload);
await client.SendMessageAsync(message);
```

### Typed Payloads

Like the raw overload, this throws `InvalidOperationException` instead of dropping the
payload when the client is not connected, so send only after `ReadyAsync` has fired:

<!-- ikon-example: sdkx-typed-payloads -->
```csharp
// Send a typed payload (creates ProtocolMessage automatically)
await client.SendMessageAsync(new MyCustomPayload { /* ... */ });
```

### Receiving Typed Messages

`OnMessage<T>` is the client's side of the app's `app.OnMessage<T>`. It can be called before
`ConnectAsync` and lasts across reconnects until the handle it returns is disposed. The handler runs
on the client's receive loop, so the next message waits until it returns:

<!-- ikon-example: sdk-receive-typed -->
```csharp
// The handler gets only messages of this type, decoded, with the sender's session id
var subscription = client.OnMessage<MyCustomPayload>(async (message, senderId) =>
{
    Console.WriteLine($"{senderId}: {message.Text}");
});

// Later, to stop receiving them
subscription.Dispose();
```

In the app, the `senderId` that `app.OnMessage<T>` gets for a message from this program is the
program's `client.ClientContext.SessionId`, the same number as `ReactiveScope.ClientId` and
`Context.SessionId` for the program's client, and `app.SendMessageAsync(reply, senderId)` answers
only the program.

The types an app sends and receives come from its `schema/*.tp`, and the app's build writes their C#
to `app/<App>/Generated/Protocol/` only. A standalone program compiles those same files, linked from
its csproj, never copied. A type's opcode comes from its schema file's place in the sorted list of
schema files, so a copy can end up with the wrong opcodes when a schema is added. The `device-typed-messages` pattern
(`docs/Ikon.Agent.Docs/patterns/device-typed-messages.md` in an app) has the csproj lines and a
program that sends a reading and waits for the app's ack.

## Audio

The SDK encodes and decodes audio with Opus automatically.

### Sending Audio

Send audio to the server. Like every send, it throws `InvalidOperationException` when the client is
not connected — call it after `ReadyAsync` has fired, never from setup code:

<!-- ikon-example: sdk-send-audio -->
```csharp
// Default encoder options for every stream that sends no encoderOptions of its own. A
// stream's encoder is created on its first SendAudioAsync and keeps the options in force
// then, so set this before the first send — not after.
client.DefaultEncoderOptions = new AudioEncoderOptions(bitrate: 48000, complexity: 8);

// Get audio samples (float PCM, range [-1.0, 1.0]) at 8, 12, 16, 24 or 48 kHz in 1 or 2 channels;
// the SDK does not resample, and a stream's first send throws ArgumentOutOfRangeException otherwise
ReadOnlyMemory<float> samples = GetAudioSamples();

// Send audio
await client.SendAudioAsync(
    MediaTargets.Everyone,
    samples: samples,
    sampleRate: 48000,
    channelCount: 1,
    isFirst: true,      // First chunk of this stream
    isLast: false       // More chunks coming
);

// Send final chunk
await client.SendAudioAsync(MediaTargets.Everyone, samples, 48000, 1, isFirst: false, isLast: true);

// Optional: specify stream ID, total duration, encoder options, and target clients.
// A stream's sample rate and channel count are fixed by its first send, and every call
// without a streamId shares one stream, so audio in another format needs its own streamId
await client.SendAudioAsync(
    MediaTargets.To(123, 456),                // Target specific session IDs
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
    ));
```

### Receiving Audio

Subscribe to audio events to receive incoming audio streams:

<!-- ikon-example: sdk-receive-audio -->
```csharp
client.AudioInputStreamBeginAsync += async e =>
{
    Console.WriteLine($"Audio stream started: {e.StreamId}");
    Console.WriteLine($"  Codec: {e.Codec}");
    Console.WriteLine($"  Sample rate: {e.SampleRate}");
    Console.WriteLine($"  Channel count: {e.ChannelCount}");

    // Optional: choose the decode rate (Opus accepts 8, 12, 16, 24 or 48 kHz)
    // e.SampleRate = 24000;

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
| `DelayUntilIsLast` | Buffer each segment, emit its frames when its last frame arrives |

The two `Delay*` modes are for clips. A live microphone sends no last frame until it closes, so a segment that buffers past `e.MaxBufferedDuration` (5 minutes by default) is delivered with one warning and the rest of it streams.

Set the streaming mode in the `AudioInputStreamBeginAsync` event handler:

<!-- ikon-example: sdk-streaming-mode -->
```csharp
client.AudioInputStreamBeginAsync += async e =>
{
    // Buffer audio for UI timeline display
    e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
};
```

## Functions

Each `IkonClient` has its own isolated `FunctionRegistry`, accessible via `client.FunctionRegistry`. A function registered in it can be called locally or offered to the app through the server (never to other clients).

### Registering Functions

**Attribute-Based Registration (Recommended)**

Mark methods with the `[Function]` attribute and register the containing class. A method is registered under its
type's full name plus the method name (`MyNamespace.MyFunctions.Greet`) unless `[Function(Name = "...")]` sets one,
and calls and lookups must use that name:

Add `using Ikon.Common.Core.Functions;`, then:

<!-- ikon-example: sdk-functions-class -->
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

<!-- ikon-example: sdk-register-functions -->
```csharp
// Register all [Function] methods from an instance
var myFuncs = new MyFunctions();
client.FunctionRegistry.RegisterFromInstance(myFuncs);

// Or register from a type: only its static methods (and a [RegisterAll] class's constructors) become callable
client.FunctionRegistry.RegisterFromType<MyStaticFunctions>();

// Or scan entire assembly (same rule as RegisterFromType)
client.FunctionRegistry.RegisterFromAssembly(typeof(MyFunctions).Assembly);
```

Without an instance, `RegisterFromType` and `RegisterFromAssembly` still register every instance `[Function]` method,
but as metadata only (`RequiresInstance` is true and there is no callback): calling one locally has nothing to run,
and a remote call that names no instance is rejected. For `MyFunctions`, whose methods are all instance methods, the
assembly scan therefore lists `Greet`, `AddAsync` and `CountAsync` without making them callable; use
`RegisterFromInstance` for those. On a `[RegisterAll]` class the same holds for every public instance method, with or
without `[Function]`.

**Manual Registration (Lambda/Delegate)**

Register functions directly using lambdas:

<!-- ikon-example: sdk-register-lambdas -->
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

- **Local** (default): Function is not advertised. In a standalone SDK client a remote caller that names it can still call it; only an IKON app restricts it to its own process.
- **External**: Function is advertised over the protocol; the app can call it. The server offers a client's functions to the app only, not to other clients.

<!-- ikon-example: sdk-function-visibility -->
```csharp
// Local - not advertised (default); only an IKON app keeps remote callers out
[Function(Visibility = FunctionVisibility.Local)]
public string LocalOnly() => "local";

// External - advertised over the protocol; the app can call it
[Function(Visibility = FunctionVisibility.External)]
public string SharedWithAll() => "shared";
```

Registered inside an **IKON app** rather than a standalone client, an `External` function is also
expected to declare who may call it: `[RequireLogin]`, `[RequireRole(...)]` or `[AllowAnonymous]`
on the method, or a policy attached at registration time. The app's startup audit, run once before `Main()`
over the functions registered by then (the app class's own `[Function]` methods), logs a warning for
every `External` function with none of them, and throws instead when the app sets
`<ExternalFunctionsRequireAuth>true</ExternalFunctionsRequireAuth>`. A function registered in
`Main()` or later is never audited. A standalone SDK client runs no
such audit.

Visibility can also be overridden where the instance is registered:

<!-- ikon-example: sdkx-function-visibility -->
```csharp
client.FunctionRegistry.RegisterFromInstance(myFuncs, FunctionVisibility.External);
```

### Discovering Functions

Query the registry to find available functions:

<!-- ikon-example: sdk-inspect-registry -->
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

// Wait for a function the app registers to become available (other clients' functions never reach this registry)
bool available = await client.FunctionRegistry.WaitForFunctionAsync(
    "RemoteFunc",
    timeout: TimeSpan.FromSeconds(30)
);
```

### Calling Functions

Call registered functions locally or remotely:

<!-- ikon-example: sdk-call-functions -->
```csharp
// Synchronous call
string result = client.FunctionRegistry.Call<string>("Greet", args: new object?[] { "World" });

// Async call
int sum = await client.FunctionRegistry.CallAsync<int>("AddAsync", args: new object?[] { 1, 2 });

// Void async call
await client.FunctionRegistry.CallAsync("LogMessage", args: new object?[] { "Hello" });

// Call a function on a specific remote client (uses targetId parameter)
int remoteSum = await client.FunctionRegistry.CallAsync<int>("Calculate", targetId: 123, args: new object?[] { 5, 10 });

// Streaming results (async enumerable). A [Function] with no Name is registered under its type's full name
await foreach (var item in client.FunctionRegistry.CallAsyncEnumerable<int>($"{typeof(MyFunctions).FullName}.CountAsync", args: new object?[] { 10 }))
{
    Console.WriteLine(item);
}
```

### Removing Functions

<!-- ikon-example: sdkx-removing-functions -->
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

<!-- ikon-example: sdkx-function-events -->
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

<!-- ikon-example: sdkx-timeouts -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    Timeouts = new TimeoutConfig
    {
        InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
        MaxReconnectAttempts = 4,                                 // Max reconnect attempts (default)
        MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
        ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
        BackgroundReconnect = true                                // Keep retrying after max attempts (default)
    }
};
```

### Protocol Options

<!-- ikon-example: sdkx-protocol-options -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication ...

    // Filter which message types to receive/send (default; leaving out GROUP_APP_LOCAL drops the app's own schema messages)
    OpcodeGroupsFromServer = Opcode.GROUP_ALL | Opcode.GROUP_APP_LOCAL,
    OpcodeGroupsToServer = Opcode.GROUP_ALL | Opcode.GROUP_APP_LOCAL,

    // Payload serialization format
    PayloadType = PayloadType.Teleport,  // Default

    // How this connection identifies to the server.
    // Default Plugin connects as a backend component (no UI).
    // Native or Browser connects as a first-class player client that receives streamed UI.
    ContextType = ContextType.Plugin
};
```

### Client Identification

<!-- ikon-example: sdkx-client-identification -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    DeviceId = "unique-device-id",
    ProductId = "my-app",
    VersionId = "3",                  // a whole number
    InstallId = "install-xyz",
    Locale = "en-US",
    Description = "My Application",
    UserAgent = "my-app/1.0.0",       // replaces the default, which names this machine's OS and architecture
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
| `IkonClient` | Main client class for connecting to IKON servers |
| `IkonClientConfig` | Configuration for the client |
| `ConnectionState` | Enum: `Idle`, `Connecting`, `Connected`, `Reconnecting`, `Offline` |
| `ReactiveRegistry` | Subscribes to an app's reactive values over one client, and subscribes again after every reconnect |

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

For issues and feature requests, contact IKON support or open an issue on GitHub.
