<!-- checked-against: 7c99a56359cd4240359b7e23 -->

# IKON C# SDK

The IKON C# SDK connects any .NET application to an IKON app. It supports .NET 10 and .NET Standard 2.1 (including Unity).

## Features

- Eight authentication modes: API Key, Local Development, Backend, External Connect URL, UserLogin (developer CLI sign-in, for dev tooling), UserToken (a person's session token for one space, issued elsewhere), Guest (a user who has not signed in), and ResumeAuthResponse (joining another connection's client session)
- Automatic reconnection with exponential backoff after a dropped connection (after the app's server stops, the program connects again itself)
- Audio streaming with Opus encoding/decoding
- Flexible audio streaming modes
- Video streaming of encoded frames (H.264, VP8, VP9) both ways, with keyframe requests
- Audio shape frames (visemes) and playback reports
- File upload through an app's upload control, resumed across reconnects
- Client context and path updates, as a browser reports them
- The app's UI as a tree of nodes, kept up to date, with the actions its controls call
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
        BackendType = BackendType.Production // The default; Development for an app `ikon status` lists under development (*.dev.ikonai.app)
    },
    Description = "My App"
};

// Create and connect the client
await using var client = new IkonClient(config);

client.ReadyAsync += async e =>
{
    Console.WriteLine("Connected!");
};

client.MessageReceivedAsync += async e =>
{
    Console.WriteLine($"Received: {e.Message.Opcode}");
};

await client.ConnectAsync();
```

`ContextType` defaults to `ContextType.Plugin`, for a program. A client a person uses sets
`ContextType.Native`. Either gets the app's UI and its own client state. Don't set `Browser`. The
SDK registers no setUrl function, so the app's `Navigation.SetPathAsync` returns false for the
client, and an app that switches the page on false only for clients that are not `Browser` never
moves it.

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

### Post-quantum key exchange

Set `RequirePostQuantum = true` to make every connection the client makes use TLS 1.3 with the
post-quantum X25519MLKEM768 key exchange and nothing else: the sign-in, the connect, and the channel.
A server that cannot do it fails the handshake, and the client does not fall back to a classical key
exchange. The client then opens no UDP side channel, because its DTLS is always classical, and uses
unencrypted TCP only to a server on the same machine. An app whose `ikon-config.toml` sets
`RequirePostQuantum` offers only connections that can use this key exchange.

<!-- ikon-example: sdk-require-post-quantum -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication ...
    RequirePostQuantum = true,
};
```

It works on Linux only, with OpenSSL 3.5 or later, in the SDK's .NET 10 build. Elsewhere
`ConnectAsync` throws a `UserException` and leaves the client unchanged, because Windows' TLS has no
ML-KEM, macOS's TLS cannot be limited to one key exchange, OpenSSL before 3.5 has no ML-KEM (Ubuntu
24.04 ships 3.0), and the .NET Standard 2.1 build cannot set the TLS options of its connections.

.NET has no API that chooses a TLS key exchange, so the first connect loads an OpenSSL configuration
for the whole process. Every TLS connection the process sets up after it, made or accepted, by the SDK
or anything else, offers only X25519MLKEM768 and TLS 1.3. The SDK's own connections are limited even
if the process used TLS before, but a connection the rest of the program makes can reuse a classical
TLS setup from before that first connect. The `Backend` and `UserLogin` modes sign in through such a
shared setup, so with them, connect before the program opens other TLS connections.

## Authentication Modes

The SDK supports eight authentication modes. Exactly one must be configured, or the `IkonClient` constructor throws `ArgumentException`:
`ApiKey`, `Local`, `Backend`, `ExternalConnectUrl`, `UserLogin`, `UserToken`, `Guest`, or `ResumeAuthResponse` (another connection's `IkonClient.LastAuthResponse`, to join its client session). `UserLogin` authenticates as the
developer signed in on this machine (the ikon CLI's stored sign-in) and is intended for dev tooling and
headless tests; production clients use `ApiKey` or `Backend`. `UserToken` connects as a person with a session
token for the space that was issued elsewhere. The constructor also throws `ArgumentException`
when the chosen mode lacks a required field: `ApiKey`'s `ApiKey`, `SpaceId` and `ExternalUserId`, `Backend`'s
`SpaceId` and `ExternalUserId`, `UserLogin`'s or `Guest`'s `SpaceId`, `UserToken`'s `SpaceId`, `Token` and `BackendUrl`, or `Local`'s `Host` and a positive `HttpsPort`.

`IkonClientConfig.Parameters` are the app's client parameters, and a program has no URL to carry them, so it picks the app instance there: `Parameters = new Dictionary<string, string> { ["room"] = "harbour" }` joins the instance a browser joins at `?room=harbour`, for an app whose `SessionIdentity` has a `Room` field (the name's case does not matter). Without it the client joins the instance with that field empty. A connect URL or a resumed auth response already names its instance.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once. `ikon api-key create --app-id <id> --format json --output key.json` works from any folder and writes the key to `key.json` as its `apiKey` field, beside `spaceId`.

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
        BackendType = BackendType.Production, // The default; Development for an app on *.dev.ikonai.app
        UserType = UserType.Human,
        ClientType = ClientType.DesktopApp
    }
};
```

A deleted API key (`ikon api-key delete <id> --app-id <app-id> --yes`, or without `--app-id` in the app's folder) is refused from the next sign-in on: a new connect, or a reconnect that has to sign in again, fails with 403 `Invalid key`, the backend's answer to a key it does not know. That 403 is about the key, not the app refusing the user; a key of another app gets 403 `Invalid key: this key belongs to app <id>`. Deleting it does not end any session: a client the key already connected keeps going, and a reconnect that resumes the same session with the tickets the client holds needs no sign-in.

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: sdk-local-config -->
```csharp
var config = new IkonClientConfig
{
    Local = new LocalConfig
    {
        Host = "localhost",
        // The port of the Server URL that `ikon run` prints, which is not always 8443
        HttpsPort = int.Parse(Environment.GetEnvironmentVariable("IKON_LOCAL_PORT") ?? "8443"),
        // The app id: a connect to a run of another app on that port throws instead of joining it
        SpaceId = "your-app-id",
        UserId = "dev-user"
    }
};
```

Several local runs on one machine take 8443, 8444 and so on. The run's port is also `serverUrl` in
`build/app/artifacts/bin/<project>/debug/ikon-server-info.json`. Without `SpaceId` the client joins
whichever app holds the port. "A Console Program That Joins An App" below has the full setup.

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
own sign-in is used. `JoinOnly = true` joins only a session already running and starts none; pair it
with `UserType = UserType.Machine` so the connection does not hold the session open ("A Console
Program That Joins An App").

<!-- ikon-example: sdk-user-login-config -->
```csharp
var config = new IkonClientConfig
{
    UserLogin = new UserLoginConfig
    {
        SpaceId = "...",              // required
        // An app on *.dev.ikonai.app, as `ikon status` shows its environment; null for the process's own platform
        Environment = IkonBackend.EnvironmentType.Development,
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
host connects with it. The token's own lifetime limits the connection. When a reconnect's token is
refused, because it expired or the person's sign-in was revoked, the client stops reconnecting and
raises `CredentialRejectedAsync` (see "When the backend refuses the sign-in" below); the program that
holds the token gets a new one and passes it to `client.UpdateAuthToken(token)`. `BackendUrl` is the backend that
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
deploy or by `ikon run`. Create it from the app's folder with `dotnet new console -n <Name> -o devices/<name>`,
which puts `<Name>.csproj` directly in `devices/<name>/` (the `device-typed-messages` pattern's
csproj lines expect it there), and add the package from inside `devices/<name>/` with
`dotnet add package Ikon.Sdk --version <version>`, taking the `Ikon.Sdk` version the app's csproj
lists in its `PackageReference`. The SDK is released on its own version line, apart from the app's
other `Ikon.*` libraries, and is on nuget.org. That published package also works against an app
that this machine builds from platform sources, locally and deployed, so the program needs no
source build of its own. When `ikon update` raises the versions in the app's csproj, raise the
program's `Ikon.Sdk` to match. `OnMessage<T>`, `IkonClientConfig.LogFilter`, the console log at
`Info` on stderr and `LocalConfig.SpaceId` need `Ikon.Sdk` 2.0.54 or later, which every 2.1 and later release is; `ikon update` raises an
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
| A local `ikon run` | `Local` | `HttpsPort`: the port of the Server URL the run prints (the project's port from its last run while that is free, else the first free one from 8443), also in `serverUrl` of `build/app/artifacts/bin/<project>/debug/ikon-server-info.json`. `SpaceId`: the app id, so a connect to a server that runs another app throws instead of joining it. Without it the program joins whatever app holds the port |
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
await client.ConnectAsync();

// A reactive of the app's [App] class, found by the member's name. The callback runs once with
// the current value, then after changes; changes close together arrive as one call with the latest value
var reactives = new ReactiveRegistry(client);
await using var visitors = await reactives.SubscribeByNameAsync<int>("_visitorCount", count => Console.WriteLine($"Visitors today: {count}"));

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

- **Which reactive.** `SubscribeByNameAsync` finds a reactive field or auto-property of the app's
  `[App]` class by its name in the source, through the app's `Ikon.Reactive.GetStableIdByName`
  function, and subscribes to its stable id. A name the app does not know fails the call. A reactive
  kept anywhere else has no name there: the app hands out its `StableId` from an `External` function,
  and the program passes it to `SubscribeAsync`. A stable id is made from where the reactive is
  declared, so it stays the same across restarts and deploys until that code moves.
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
  `List<T>` for a `ReactiveList<T>`, a `Dictionary<TKey, TValue>` for a `ReactiveDictionary<TKey, TValue>`,
  `JsonElement` to look at any of them. A record value keeps
  its C# property names (`Celsius`, not `celsius`): a typed `T` matches them in any case, but
  `JsonElement.GetProperty` needs the exact name. A `ClientReactive` gives the program its own copy,
  not a browser's.
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
- **A function that takes an app type.** The program's argument type must have the app type's full
  name, namespace and nesting included, so a record nested in the app class does not match a
  top-level record of the same name; or let the function take primitives.
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
deploy, `ikon session stop`) must connect again itself: the client does not reconnect after the
server stops. It reconnects by itself after a dropped connection, but when the server stops, it goes
`Offline` and stays there. A loop at the end of the program connects it again whenever it is
neither connected nor connecting (`IsDisconnected` is `Idle`, before the first connect, or `Offline`):

<!-- ikon-example: sdk-console-stay-connected -->
```csharp
// Connecting again joins the app's next server, and the ReactiveRegistry subscribes again by itself
while (true)
{
    await Task.Delay(TimeSpan.FromSeconds(15));

    if (!client.State.IsDisconnected())
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

Against a deployed app, each connect starts the app's server if none is running. A watcher that
should only join a session already running sets `JoinOnly = true` (on `ApiKey`, `UserLogin`,
`UserToken` or `Backend`) with `UserType = UserType.Machine`: the connect then throws
`NoLiveSessionException` when nothing runs and starts nothing, a reconnect after the server went away
goes offline instead of retrying, and the Machine user does not hold the session open.

A program killed without `DisposeAsync` stays in the session for the soft-retry window ("Client
Events" below), so the other clients see it leave only 5 minutes later. Dispose the client on Ctrl+C
and on a service manager's SIGTERM (`using System.Runtime.InteropServices;` for
`PosixSignalRegistration`). A shell starts a background job (`&`) with SIGINT ignored, so stop such a
program with SIGTERM:

<!-- ikon-example: sdk-console-shutdown -->
```csharp
// Ctrl+C (SIGINT) and a service manager's stop (SIGTERM) both end the wait below, and leaving the
// scope disposes the client, which tells the server the program is leaving: the others get
// ClientLeftAsync at once instead of after the 5-minute soft-retry window
using var stop = new CancellationTokenSource();
using var onTerminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;
    stop.Cancel();
});
using var onInterrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
{
    context.Cancel = true;
    stop.Cancel();
});

await using var client = new IkonClient(config);
await client.ConnectAsync();

try
{
    await Task.Delay(Timeout.Infinite, stop.Token);
}
catch (OperationCanceledException)
{
    // The stop signal: fall through to the disposal
}
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

// Raised once the SDK has told the server this client is ready, and again on every reconnect.
// The app knows the client from here on, so calls made here reach it with the client's identity.
client.ReadyAsync += async e =>
{
    // e.IsReconnect is false only for the first connect; e.IsNewSession says the server
    // session changed and its server-side state is gone
};

// The server is stopping: the app restarts, is deployed again or its session ends. Messages can
// still be sent here. Not raised by the program's own IkonClient.DisconnectAsync or by a dropped
// connection
client.StoppingAsync += async e =>
{
    Console.WriteLine("Server stopping...");
};

// The client went Offline. After a server stop, a refused sign-in or a required upgrade it stays
// there until the program calls IkonClient.ConnectAsync; after failed reconnects BackgroundReconnect
// (on by default) keeps trying. Not raised by the program's own IkonClient.DisconnectAsync, a failed
// IkonClient.ConnectAsync or a drop that reconnects
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

### Joining and Ready

A connect has two steps. The client joins the app's session and receives its state, then it signals
ready. Only at ready does the app hear about the client and learn its user, scopes and roles.
`ConnectAsync` signals ready itself on every connect and reconnect, before it returns and before
`ReadyAsync` is raised, so a `ReadyAsync` handler can call functions, subscribe to reactives and send
messages at once.

Register the program's own functions in `client.FunctionRegistry` before `ConnectAsync`. They are
sent while joining, so the app has them when it hears the client is ready. Anything else the client
sends to the app while joining, such as a send from a `MessageReceivedAsync` handler, is held and sent
right after ready, in order. At most 4096 messages are held per join; later ones are dropped with one
warning. Audio and video sent then are dropped, and held messages are discarded if the connection
drops before ready.

### Client Events

The client events say who else is in the session. `ClientJoinedAsync` is raised when another client
is ready: it has finished joining and the app knows who it is. On joining, it is raised once for each
ready client already there, which can be after `ConnectAsync` has returned. A client that leaves
before it is ready raises no event. `ClientUpdatedAsync` and `ClientLeftAsync` are raised only for a
client `ClientJoinedAsync` has reported, and none of the three is raised about this client itself.
Each event carries the other client's `Context` as `e.ClientContext`.

The app is one of the clients: a `ContextType.Plugin` with `IsInternal` set. After each restart or
hot reload it leaves and joins again under a new session, so `ClientLeftAsync` is raised for the old
session and `ClientJoinedAsync` for the new one.

A client is reported once per session. After a reconnect into the same session, a client that joined
meanwhile raises `ClientJoinedAsync`, and a client that changed its viewport, theme, timezone or
visibility meanwhile raises `ClientUpdatedAsync`, and a client that left meanwhile raises
`ClientLeftAsync`. A reconnect into a new session reports its clients as a first join does.

A client whose connection drops, a program that was killed included, is not reported as gone at
once: the server holds its session for the soft-retry window, 5 minutes by default, so it can resume.
Until then its `Context` has `IsSoftDisconnected` true and `SoftDisconnectAt` set, and
`ClientLeftAsync` is raised when the window has passed. A program that calls `DisconnectAsync` or `DisposeAsync` tells the
server it is leaving, so the others get `ClientLeftAsync` at once.

The client event handlers run on the client's receive loop. A handler that awaits an answer from the
app, such as a function call or `client.UI.CallActionAsync`, holds every later message, that answer
included, until the wait times out. Start such work without awaiting it in the handler.

<!-- ikon-example: sdk-client-events -->
```csharp
// The other clients in the session, and the app itself (IsInternal, a new session after each restart or hot reload)
client.ClientJoinedAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} joined as {e.ClientContext.ContextType}, user {e.ClientContext.UserId}");
client.ClientUpdatedAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} now uses the {e.ClientContext.Theme} theme");
client.ClientLeftAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} left");
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

A cloud connect that a full server refuses asks /init again up to three times, naming the servers that refused it, so a sharded app places the client on another shard; when none has room, `ConnectAsync` throws an `HttpRequestException` saying the app is full.

When the platform no longer accepts the client, `ConnectAsync` throws, and the SDK neither retries nor reconnects, because only a new build fixes it:

| Exception | What is too old | The fix |
|---|---|---|
| `SdkUpgradeRequiredException` | This version of the `Ikon.Sdk` package | Update the package and rebuild the program |
| `AppUpgradeRequiredException` | The IKON libraries the app's deployed bundle was built on | The app's developer runs `ikon update` and deploys again |

Both are `HttpRequestException`s carrying 426. `Reason` is the platform's message and `Minimum` the oldest version it accepts. A reconnect that meets either one stops and raises `ErrorOccurredAsync` with it.

When the app's server fails to start, `ConnectAsync` throws an `AppSessionNotRunningException`. Its `AppSessionId` names the session whose log says why.

### Automatic Reconnection

The SDK reconnects by itself when the connection drops. It does not reconnect when the app's server
stops: an app restart (`ikon stop` and a new `ikon run`), a deploy or `ikon session stop`. The server
says it is stopping, the client raises `StoppingAsync` and `DisconnectedAsync` and stays `Offline`,
whatever `BackgroundReconnect` says. A program that must keep running through an app restart calls
`ConnectAsync` again itself, as the loop at the end of "A Console Program That Joins An App" does. A
hot reload does not stop the server, so the client stays connected through it.

Configure reconnection behavior:

<!-- ikon-example: sdk-timeouts -->
```csharp
var config = new IkonClientConfig
{
    // ... authentication config ...
    Timeouts = new TimeoutConfig
    {
        InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
        MaxReconnectAttempts = 4,                                 // Max attempts (default)
        MaxReconnectDelay = TimeSpan.FromMinutes(5),              // Background backoff delay cap (default)
        ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
        BackgroundReconnect = true                                // Keep retrying after max attempts (default)
    }
};
```

Reconnection uses exponential backoff starting from `InitialReconnectDelay` (500ms, 1s, 2s, 4s by default), jittered. When `BackgroundReconnect` is enabled (the default), the client keeps retrying from the `Offline` state after the fast reconnection attempts are exhausted. It waits 10 seconds, doubled after each failed attempt up to `MaxReconnectDelay` (5 minutes), and at least as long as a 429's `Retry-After`. It stops and stays `Offline` when retrying cannot help: a refused sign-in (401, 403), an app that is not available (404, 410: suspended, unpublished or not deployed), an SDK or app build the platform no longer runs (426), or a `JoinOnly` reconnect that finds the session ended.

A function call made while the client is reconnecting waits for the reconnect and is sent once the
client is ready again; see [Calling Functions](#calling-functions).

### When the backend refuses the sign-in

A reconnect signs in again, and the backend can refuse it: with 401 when the token expired or the
person's sign-in was revoked (for example, their organisation's directory removed them), with 403 when the
app or surface does not admit them, or when an API key is unknown or deleted (`Invalid key`). The same credential would be refused on every retry, so the
client stops: it raises `ErrorOccurredAsync` and `DisconnectedAsync`, goes `Offline` and does not
retry in the background. A server that is down or busy (5xx, network errors) is still retried as
above. On a 401 it also raises `CredentialRejectedAsync`, so the program can act:

<!-- ikon-example: sdk-credential-rejected -->
```csharp
client.CredentialRejectedAsync += async e =>
{
    // A UserToken client resumes with a new token; any client can call ConnectAsync again
    client.UpdateAuthToken(await GetNewTokenAsync());
};
```

`UpdateAuthToken` works only for a `UserToken` client, and a client waiting after a refusal starts
reconnecting with the new token at once. Call it whenever the program renews the token, too; a
connected client uses it the next time it signs in. A first `ConnectAsync` that is refused throws
the `HttpRequestException` instead.

An `ApiKey` client posts its key at every sign-in, so it has no token to renew. A key that was
deleted is refused with 403: the client raises `ErrorOccurredAsync` and `DisconnectedAsync` and
stays `Offline`, and `CredentialRejectedAsync` is not raised. It connects again only through a new
client configured with a new key.

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
its csproj, never copied: a copy keeps the old classes when a schema changes. The `device-typed-messages` pattern
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

// An app's speech recognition skips a stream with no correlation id unless the app turned
// that off. Pass the id the app gave for the capture, or any id of your own. When a new
// segment uses another id, the SDK announces the stream again with that id
await client.SendAudioAsync(MediaTargets.Everyone, samples, 48000, 1, isFirst: true, isLast: true, correlationId: "question-1");

// Optional: specify stream ID, total duration and encoder options.
// A stream's sample rate and channel count are fixed by its first send, and every call
// without a streamId shares one stream, so audio in another format needs its own streamId
await client.SendAudioAsync(
    MediaTargets.Everyone,                    // A client's audio reaches the app
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

### Shape Frames and Playback Reports

An app that animates a speaking avatar sends shape values (visemes) with its audio, and reads how
far each client has played it. Both are tied to the audio stream's id:

<!-- ikon-example: sdk-audio-shapes -->
```csharp
client.AudioShapeFrameAsync += async e =>
{
    // Values the app analysed from its speech, such as visemes, for the audio of the same
    // stream; Timestamp is from the start of the segment
    foreach (var set in e.Sets)
    {
        for (int i = 0; i < set.Values.Count; i++)
        {
            Console.WriteLine($"{e.StreamId} at {e.Timestamp}: {set.Name}.{set.ShapeNames.ElementAtOrDefault(i)} = {set.Values[i]}");
        }
    }
};

// A program that plays the audio tells the app how far it got, about every 500 ms and at once
// when it stops being audible, so the app can pace what it says to what was heard
await client.SendAudioPlaybackReportAsync(streamId, played: TimeSpan.FromSeconds(1.5), buffered: TimeSpan.FromMilliseconds(120));
await client.SendAudioPlaybackReportAsync(streamId, played: null, buffered: TimeSpan.Zero, AudioPlaybackState.Blocked);
```

Only an app's `Audio.Raw` streams ask for reports, and every stream of an app built before the flag existed:
`WantsPlaybackReports` on the `AudioInputStreamBeginAsync` event's arguments is true for them, and
`SendAudioPlaybackReportAsync` sends nothing and returns false for any other stream.

## Video

The SDK sends and receives encoded video frames. It does not encode or decode them. A client's video
reaches the app, which gets it as a `VideoInput` (`Video.InputStartedAsync`). The app's own video sent
to this client arrives through the events below.

### Sending Video

Send each frame as you encode it. The first frame of a stream announces it, and a frame in another
size or codec announces it again. Send a keyframe first and whenever `VideoKeyFrameRequestedAsync`
fires, because the app shows nothing until it has one. For H.264 each frame is one access unit (every slice of one picture) in Annex B form with start codes, each keyframe with its SPS and PPS before the IDR slice, because a receiver decodes from the parameters in the stream. ffmpeg's libx264 writes such a stream with `-c:v libx264 -x264-params aud=1:repeat-headers=1 -f h264`: an access unit delimiter starts each picture, so a reader of its output splits the frames there, and every keyframe repeats the SPS and PPS.

<!-- ikon-example: sdk-send-video -->
```csharp
// The SDK sends frames you have already encoded (H.264 in Annex B form, VP8 or VP9); it
// does not encode. The app gets the stream as a VideoInput: "camera" makes its Kind Camera
var keyFrameRequested = true;
client.VideoKeyFrameRequestedAsync += async e => keyFrameRequested = true;

for (int frameNumber = 0; frameNumber < 300; frameNumber++)
{
    // Start with a keyframe, and send one whenever the app asks for it
    var (data, isKey) = EncodeNextFrame(forceKeyFrame: keyFrameRequested);
    keyFrameRequested = false;

    await client.SendVideoFrameAsync(
        MediaTargets.Everyone,                // A client's video reaches the app
        streamId: "camera-1",
        data: data,
        isKey: isKey,
        timestamp: TimeSpan.FromSeconds(frameNumber / 30.0),
        codec: VideoCodec.H264,
        width: 1280,
        height: 720,
        correlationId: "capture-1");          // Ties the stream to the app's capture
}

await client.EndVideoStreamAsync("camera-1");
```

### Receiving Video

<!-- ikon-example: sdk-receive-video -->
```csharp
client.VideoInputStreamBeginAsync += async e =>
{
    // Raised again when the sender changes the codec or size; reconfigure the decoder then
    Console.WriteLine($"Video stream {e.StreamId} ({e.SenderStreamId}): {e.Codec} {e.Width}x{e.Height}");
};

client.VideoInputFrameAsync += async e =>
{
    // e.Data is one encoded frame; decode it with a decoder of your own
    Console.WriteLine($"Frame {e.FrameNumber} of {e.StreamId}: {e.Data.Length} bytes, key: {e.IsKey}");
};

client.VideoInputStreamEndAsync += async e => Console.WriteLine($"Video stream ended: {e.StreamId}");

// A decoder that lost a frame or joined late asks the sender for a keyframe:
// await client.RequestVideoKeyFrameAsync(streamId);
```

## Client Context

A browser tells the app its viewport, theme, timezone and whether the page is shown, and which path
it is on. A program that stands in for a browser, or that drives an app's pages, reports the same:

<!-- ikon-example: sdk-client-context -->
```csharp
// What a browser reports about itself. A call names only what changed; the SDK
// sends the other values again, because the app's copy is replaced on each update
await client.UpdateClientContextAsync(viewportWidth: 390, viewportHeight: 844, theme: "dark", timezone: "Europe/Helsinki", visibility: "visible");
await client.UpdateClientContextAsync(theme: "light");

// Move to another page of the app, as a browser does when it navigates
await client.SetPathAsync("/room/7");
```

The app reads them from the client's context (`app.GlobalState.GetClientContext(sessionId)`) and its
`Navigation.CurrentPath`, and its client-updated and `Navigation.PathChangedAsync` handlers run.

## File Upload

A program uploads a file through an upload control of the app, as a person dropping a file on it
does. The SDK hashes the file, sends it in chunks sized to the link, and resumes on the next
connection when one drops mid-upload.

<!-- ikon-example: sdk-upload-file -->
```csharp
// The app's upload control (view.FileUpload or view.FileUploadZone) carries the id
// in the uploadActionId prop of its node in the UI tree
await using var file = File.OpenRead("report.pdf");

try
{
    var result = await client.UploadFileAsync(
        uploadActionId,
        file,                                 // Readable and seekable
        fileName: "report.pdf",
        mimeType: "application/pdf",
        progress: new System.Progress<IkonClient.UploadProgress>(p => Console.WriteLine($"{p.AcknowledgedBytes}/{p.TotalBytes}")));

    // Returns once the app's onUploadComplete has run
    Console.WriteLine($"Uploaded {result.Size} bytes, SHA-256 {result.Hash}");
}
catch (UploadFailedException ex)
{
    // The app refused it, or it failed; Retriable says whether sending it again can help
    Console.WriteLine($"Upload {ex.UploadId} failed: {ex.Message} (retriable: {ex.Retriable})");
}
```

The app's upload hooks run as for a browser upload (`onUploadPreStart`, `onUploadStart`,
`onUploadComplete`), with the program's client as the uploader. At most 8 uploads of one client run at
once; later calls wait for one to finish.

## UI

An app's UI reaches the SDK as it reaches a browser: one tree of nodes per stream, which the SDK
keeps up to date from the app's changes. The SDK draws nothing. A program reads the trees to show
the app with a UI of its own, or to drive the app as a person would, and calls the actions the
controls carry.

<!-- ikon-example: sdk-ui -->
```csharp
await using var client = new IkonClient(new IkonClientConfig
{
    Description = "My UI",
    // The port `ikon run` prints, and the app id, as in Local Development above
    Local = new LocalConfig { Host = "localhost", HttpsPort = int.Parse(Environment.GetEnvironmentVariable("IKON_LOCAL_PORT") ?? "8443"), SpaceId = "your-app-id" },
});

// Raised after every change to a stream's tree, with the whole tree as it now is.
// A node that did not change keeps its object, so a renderer can skip it by reference
client.UI.StreamUpdatedAsync += async e =>
{
    foreach (var node in e.Stream.Root?.DescendantsAndSelf() ?? [])
    {
        // node.Type is the component ("std.button"), node.Props its props as JSON,
        // node.StyleIds its styles (client.UI.Styles)
        Console.WriteLine($"{node.Type} {node.GetText("label") ?? node.GetText("content")}");
    }
};

await client.ConnectAsync();

// Use a control as a person would: find its node and call its action
var save = await client.UI.WaitForNodeAsync(node => node.Type == "std.button" && node.GetText("label") == "Save");
bool completed = await client.UI.CallActionAsync(save, "onInvokeId");

// A text field's action takes the new text as it is
var name = await client.UI.WaitForNodeAsync(node => node.Type == "std.text-field");
await client.UI.CallActionAsync(name, "onValueChangeId", "Ada");
```

`client.UI.Page` is the stream of the app's page; another stream is a mount the app declares or the
platform's own layer. The first tree can arrive just after `ConnectAsync` returns, so `Page` may still
be empty then; `WaitForNodeAsync` waits for it. A node's `Actions` lists its actions by prop name: a button's press is
`onInvokeId`, a text field's new value `onValueChangeId`, a checkbox's `onCheckedChangeId`. A press
sends `{}`, a text field the text itself (`Ada`, not JSON `"Ada"`), a checkbox `true` or `false`. A disabled control carries no
action, so waiting until `node.GetActionId("onInvokeId")` is not null waits until a button can be
pressed.

The standard components put their text and labels in these props:

| Node type | Text and labels | Action and its payload |
|---|---|---|
| `std.text` | `content` | |
| `std.heading` | `content`, and `level` 1 to 6 | |
| `std.button` | `label` on a button with text only. A button with an icon or its own content has no `label`: its text is the `content` of a `std.text` child, and also its `aria-label` | `onInvokeId`, `{}` |
| `std.text-field`, `std.text-area` | `value`, `placeholder`. A field label is a `std.text` node just before the field, and also the field's `aria-label` | `onValueChangeId`, the text itself |
| `std.checkbox`, `std.switch` | `checked`. The label is a `std.text` node just after it, and also its `aria-label` | `onCheckedChangeId`, `true` or `false` |
| `std.select` | `value`. The label is a `std.text` node just before it, and also its `aria-label` (also on its `std.select-trigger` child). Each option is a `std.select-item` with the option's `value`, its text in a `std.text` below it | `onValueChangeId`, the option's value itself |
| `std.slider` | `value`, a JSON array with one number per thumb, and `min`, `max`, `step` | `onValueChangeId` while it moves and `onValueCommitId` when it is let go, a JSON array with one number per thumb (`[0.8]`, `[0.2,0.8]`) |
| `std.color-picker` | `value`, and `format` (`hex` by default) | `onValueChangeId` and `onValueCommitId`, the color as text in that format (`#ff0000`) |
| `std.tabs` | `value` (`defaultValue` when the app does not control it). Each tab is a `std.tabs-trigger` with the tab's `value`, its label in a `std.text` below it. Every panel is in the tree as a `std.tabs-content`, and the open one's `value` equals the `std.tabs` node's `value` | `onValueChangeId`, the tab's `value` itself |
| `std.file-upload`, `std.file-upload-zone` | | `uploadActionId`, the id an upload names |
| A control declared with a `path`: `std.button`, `std.nav-item` and others | `actionPaths`, a JSON object from the control's action id to the app path its press opens (`/stable/{value}` for a control that sends a value) | The call moves the client to that path itself, as `SetPathAsync` does, and moves back when the app refuses it or the call fails |
| `std.button` with an `href` (a link) | `href`, `target`, and the text as on a button | None unless the app also handles the press (`onInvokeId`, `{}`). A client follows the `href` itself |
| `std.action-button` | `label`, `actionType` (`CopyToClipboard`, `DownloadFile`, `Share` and the others) and `actionOptionsJson`, the action's options as JSON | The action runs in the browser, so a client does it itself. `onActionCompleteId` reports it, `{"ActionType":"CopyToClipboard","Success":true}` |
| `std.feed-scroller` | `activeIndex` when the app controls it, `slideCount`, `muted`. Each slide is a `std.feed-slide` with its `index` | `onActiveChangeId` when a slide becomes the active one and `onScrollNearEndId` near the end, the slide's index as a number (`3`); `onMuteChangeId`, `true` or `false` |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

A payload the app cannot read, such as `0.8` for a slider instead of `[0.8]`, fails the call like a handler that throws: `CallActionAsync` returns false, and the reason is only in the app's log (`ikon logs --level warn`).

The SDK acknowledges each tree it applies, so the app can remove the actions no client can call any
longer. When a change cannot be applied, or after the connection drops and the client rejoins the same
session, the SDK asks the app for the whole tree again and keeps the old tree until the new one arrives.

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

### Built-In Client Functions

The app calls these on a client through `ClientFunctions` (`GetLocationAsync`, `LoginShowAsync`, `SetUrlAsync` and the rest). This SDK registers none of them: a program that wants to answer registers the ones it supports as shared functions, under these names. Arguments arrive in the order below.

| Function | Arguments | Reply |
|---|---|---|
| `ikon.client.getLanguage` | none | a locale string such as `"en-US"`, or null |
| `ikon.client.getTimezone` | none | an IANA name such as `"Europe/Helsinki"`, or null |
| `ikon.client.getUrl` | none | the path and query, such as `"/room/7?x=1"`, or null |
| `ikon.client.setUrl` | url, replace (bool), preserveQueryParams (bool) | bool |
| `ikon.client.goBack` | fallbackPath | bool |
| `ikon.client.openExternalUrl`, `ikon.client.navigateTo` | url | bool |
| `ikon.client.setTheme` | theme name (`"dark"`, `"light"`), persist (bool) | bool |
| `ikon.client.getVisibility` | none | `"visible"` or `"hidden"` |
| `ikon.client.getLocation` | none | `{"latitude": 60.17, "longitude": 24.94, "accuracy": 12.0}`, or null when unavailable |
| `ikon.client.getBatteryLevel` | none | 0 to 100, or null |
| `ikon.client.getNetworkType` | none | a string such as `"wifi"`, or null |
| `ikon.client.vibrate` | pattern as text, such as `"200"` or `"100,50,100"` | bool |
| `ikon.client.keepScreenAwake` | enabled (bool) | bool |
| `ikon.client.scrollTo` | x, y, smooth (bool) | bool |
| `ikon.client.setBadge` | count | bool |
| `ikon.client.loginShow` | reason (text, empty for none) | bool |
| `ikon.client.login` | provider | bool |
| `ikon.client.loginDismiss`, `ikon.client.logout` | none | bool |
| `ikon.client.openFeedback` | none | bool |

A reply of `true` means the client did what was asked, `false` that it declined or cannot. A bool function that answers `null` counts as `false`. If a program does not register a function, the app's `ClientFunctions` call for it returns `false`, `null`, an empty list or `Unknown` at once.

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

// Find which sessions (the app's and the server's) have a specific function
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

// Call a function on one session (targetId: the app's session id; another client's functions
// never reach this registry, so naming its session fails with "Function 'Calculate' not found")
int remoteSum = await client.FunctionRegistry.CallAsync<int>("Calculate", targetId: 123, args: new object?[] { 5, 10 });

// Streaming results (async enumerable). A [Function] with no Name is registered under its type's full name
await foreach (var item in client.FunctionRegistry.CallAsyncEnumerable<int>($"{typeof(MyFunctions).FullName}.CountAsync", args: new object?[] { 10 }))
{
    Console.WriteLine(item);
}
```

A call to the app waits while the client is connecting or reconnecting (soft, or into a new session)
and is sent right after the client is ready, after the messages held until then, in the order the calls
were made. The function is looked up then, so a function the app no longer has fails with
"Function 'x' not found". The wait counts against the call's 30-minute timeout; if the client is not
ready by then, the call fails with a `TimeoutException` saying the client was not connected, and it
is never sent. Pass a `CancellationToken` to wait less. At most 4096 calls wait; later ones fail at
once.

A call fails at once with a `FunctionCallException` saying the client is not connected when the
client is not trying to connect: before `ConnectAsync`, after `DisconnectAsync` or `DisposeAsync`,
when the server is stopping, or `Offline` with no reconnect left (`BackgroundReconnect` off, or a
failure no retry can fix). A waiting call fails the same way when the client moves into one of those
states. Calls to the program's own functions run at once whatever the connection state, and a call
already sent when the connection drops fails, since the server will not answer it.

Cancelling the token passed to `CallAsyncEnumerable` stops the app's stream: an app function that
takes an `[EnumeratorCancellation] CancellationToken` sees it cancelled. The token is not one of the
call's arguments. In the program, the `await foreach` then throws `OperationCanceledException`, so
catch it where cancelling is expected. Leaving the loop early with `break` or `return` ends the app's
stream too, and throws nothing. A long stream also fails with a `FunctionCallException` when the app restarts or
hot-reloads, because the app's session that serves it leaves. The program's own connection
stays up. Start the stream again then.

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
        MaxReconnectDelay = TimeSpan.FromMinutes(5),              // Background backoff delay cap (default)
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

    // How this connection identifies to the server: Plugin for a program,
    // Native for a client a person uses. Each gets the app's UI.
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

### Video Types

| Type | Description |
|------|-------------|
| `VideoCodec` | Enum (from `Ikon.Common.Core.Protocol`): `H264`, `Vp8`, `Vp9`, `Av1` |
| `IkonClient.VideoInputStreamBeginEventArgs` | Event args for video stream start or format change |
| `IkonClient.VideoInputFrameEventArgs` | Event args for one encoded video frame |
| `IkonClient.VideoInputStreamEndEventArgs` | Event args for video stream end |
| `IkonClient.VideoKeyFrameRequestedEventArgs` | Event args for a receiver's keyframe request |

### UI Types

| Type | Description |
|------|-------------|
| `IkonUI` | The app's UI as the client receives it (`client.UI`): streams, styles and action calls |
| `UIStream` | One stream's tree, with its mount, view and version |
| `UINode` | One node of a tree: type, props, actions, children and styles |
| `UIStreamEventArgs` | Event args carrying a stream after it changed or ended |

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
| `IkonClient.ClientEventArgs` | Event args naming another client in the session (`ClientContext`, a `Context`) |

## License

This SDK is licensed under the Apache License 2.0. See `LICENSE` for details.

## Support

For issues and feature requests, contact IKON support or send a report with `ikon report create`.
