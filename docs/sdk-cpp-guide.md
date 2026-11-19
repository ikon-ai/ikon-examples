<!-- checked-against: 1f1d0b72e68590e6e3b0c442 -->

# IKON C++ SDK

The IKON C++ SDK connects C++ applications to an IKON app. It is a header-only library requiring C++17, and every example below compiles as C++17.

## Features

- Five authentication modes: API Key, Local Development, Guest, User Token, Connect URL
- Connection state management with callbacks
- The app's own schema messages, sent and received as generated types
- Function registry for registering and calling shared cross-client functions, including streams in both directions, cancellation and binary values
- Client events: other clients joining, changing their context and leaving
- Reactive subscriptions to values the app holds
- A headless UI layer: the app's UI trees, kept current, with their controls' actions to call
- File upload through the app's upload controls, resumed across reconnects
- Client context updates (viewport, theme, timezone, visibility) and path changes
- Audio and video sent and received as encoded frames (Opus, H.264, VP8), and playback reports of the app's audio
- Configurable timeouts and reconnection
- Header-only library

## Requirements

- C++17 compatible compiler
- Implementations of required interfaces:
  - `ILogInterface` - Logging
  - `IHttpInterface` - HTTP client
  - `INetworkInterface` - TLS client over TCP

## Building a Program

The SDK is headers only, with no library of its own to build. Each release is a zip on the [IKON SDK releases page](https://github.com/ikon-ai/ikon-sdk-releases/releases), under the tag `v-cpp-sdk-<version>`, and the zip is a CMake package with two targets:

- `ikon::sdk`: the SDK's headers, C++17 and threads. A program that brings its own logger, HTTP client and network client needs only this.
- `ikon::sdk_examples`: the SDK and the example implementations in its folder (`ExampleLogger`, `ExampleHttpClient`, `ExampleNetworkClient`), linked with OpenSSL, which `ExampleNetworkClient` needs. `ExampleHttpClient` uses WinHTTP on Windows, except for [post-quantum key exchange](#post-quantum-key-exchange); elsewhere it uses cpp-httplib over OpenSSL. The target exists only when CMake finds OpenSSL.

Compiling the SDK is slow: a translation unit that uses `IkonClient` can take several minutes and over 1 GB of compiler memory in a Release build, and about a minute and a half in Debug, while one that only includes `ikon_sdk.h` builds in seconds. Keep the code that uses the SDK in one `.cpp` file, and give the rest of the program a header of your own that does not include `ikon_sdk.h`, so that a change elsewhere does not compile the SDK again. Allow for it in a build that has a time limit.

Both targets add `/bigobj` on MSVC, because a Debug build of the SDK's headers has more sections than an object file holds by default. A minimal `CMakeLists.txt` that downloads a release with `FetchContent`, with the zip's address and SHA-256 copied from the release:

```cmake
cmake_minimum_required(VERSION 3.18)
project(my_client CXX)

include(FetchContent)
FetchContent_Declare(ikon_sdk
  URL https://github.com/ikon-ai/ikon-sdk-releases/releases/download/v-cpp-sdk-<version>/<zip name>
  URL_HASH SHA256=<the zip's SHA-256>
)
FetchContent_MakeAvailable(ikon_sdk)

add_executable(my_client main.cpp)
target_link_libraries(my_client PRIVATE ikon::sdk_examples)
```

`add_subdirectory` on the unpacked zip works the same way. To install it instead, run `cmake -S <unpacked zip> -B build` and `cmake --install build --prefix <prefix>`, and find it with `find_package(ikon-sdk <major>.<minor> CONFIG REQUIRED)` and `-DCMAKE_PREFIX_PATH=<prefix>`. A minor release can change the API, so `find_package` accepts only the minor version it asks for.

On Windows the example headers link WinHTTP, Winsock and Crypt32 themselves, and OpenSSL can come from vcpkg (`vcpkg install openssl`, or `openssl` in a `vcpkg.json`).

`find_package` finds a package from vcpkg, such as libopus for audio, only when CMake is configured with vcpkg's toolchain file. With the packages listed in a `vcpkg.json` next to `CMakeLists.txt` (`{ "dependencies": ["opus"] }`), the first configure also builds them:

```bash
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE="$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake"
cmake --build build --config Release
```

`windows.h`, which the example headers include, defines `SendMessage` as a macro for the Win32 function, so on Windows, after that header, `client.SendMessage(...)` turns into a call to a member that does not exist. In a file that calls it, include `ikon_sdk.h` before the example headers, as the Quick Start does, and add `#undef SendMessage` after them.

## Quick Start

<!-- ikon-example: cpp-sdk-quick-start -->
```cpp
#include "ikon_sdk.h"
#include "example_logger.h"
#include "example_http_client.h"
#include "example_network_client.h"

#include <iostream>

using namespace ikon;

int main()
{
    // Create interface implementations
    auto log = std::make_shared<ExampleLogger>();
    auto httpClient = std::make_shared<ExampleHttpClient>();
    auto networkClient = std::make_shared<ExampleNetworkClient>();

    // Create configuration with API key authentication
    ApiKeyConfig apiKey;
    apiKey.apiKey = "ikon-xxxxx";             // API key from portal
    apiKey.spaceId = "your-app-id";
    apiKey.externalUserId = "user-123";

    IkonClientConfig config;
    config.apiKey = apiKey;
    config.description = "My App";

    // Create and connect the client
    IkonClient client(config, log, httpClient, networkClient);

    client.Ready = []()
    {
        std::cout << "Connected!" << std::endl;
    };

    client.MessageReceived = [](const ProtocolMessage& message)
    {
        std::cout << "Received message with opcode: " << static_cast<int>(message.GetOpcode()) << std::endl;
    };

    client.Connect();

    return 0;
}
```

## Authentication Modes

The SDK supports five authentication modes: an API key, local development, a guest, a user token and a connect URL. Exactly one must be configured.

`config.parameters` are the app's client parameters, and a program has no URL to carry them, so it picks the app instance here: `config.parameters["room"] = "harbour";` joins the instance a browser joins at `?room=harbour`, for an app whose `SessionIdentity` has a `Room` field (the name's case does not matter). Without it the client joins the instance with that field empty.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once. `ikon api-key create --app-id <id> --format json --output key.json` works from any folder and writes the key to `key.json` as its `apiKey` field, beside `spaceId`. The app's surface needs no `guest` or `global` entry in its `[Surface.Auth] Methods` for a key.

<!-- ikon-example: cpp-sdk-auth-api-key -->
```cpp
ApiKeyConfig apiKey;
apiKey.apiKey = "ikon-xxxxx";             // API key from portal
apiKey.spaceId = "...";                   // The app's id (ikon app list)
apiKey.externalUserId = "user-123";       // Your user identifier
apiKey.sessionId = "session-xyz";         // Optional: target a precomputed session
apiKey.backendType = BackendType::Production;
apiKey.userType = UserType::Human;
apiKey.clientType = ClientType::DesktopApp;

IkonClientConfig config;
config.apiKey = apiKey;
```

Set `joinOnly = true` on the `ApiKeyConfig` for a client that should join the app only while someone
else has it running, and never start it: a device or agent that stays connected around the clock
would otherwise keep a server running, billed to the app's organisation. When nothing is running,
`Connect()` throws `NoLiveSessionException`, whose `retryAfter` (a `std::chrono::seconds`, empty when
the backend named none) is how long the backend asks you to wait before trying again. If the server
shuts down, the next reconnect reports that through `ErrorOccurred` and goes offline instead of
retrying. Pair `joinOnly` with `UserType::Machine`, so the connection does not count as a person and
does not keep the session open either.

While the app's server is starting, `Connect()` keeps retrying for up to `connectionTimeoutSec`. When
the server refuses the connect token because it was issued for a server that has since been replaced,
or because it has expired, `Connect()` stops retrying and asks `/init` for a new token once. If that
one is refused too, it throws `ServerChangedException`. The API key is still valid, so call `Connect()`
again later. When a server answers that it is full, `Connect()` asks `/init` again up to three times,
naming the full servers so a sharded app places the person on another shard, and then throws
`ServerFullException`.

When the platform no longer accepts the client, `Connect()` throws at once, and a reconnect reports it
through `ErrorOccurred` and goes offline, because only a new build fixes it:

| Exception | What is too old | The fix |
|---|---|---|
| `SdkUpgradeRequiredException` | This version of the C++ SDK | Update the SDK and rebuild |
| `AppUpgradeRequiredException` | The IKON libraries the app's deployed server was built on | The app's developer runs `ikon update` and deploys again |

Both derive from `ClientUpgradeRequiredException`, whose `reason` is the platform's message and
`minimum` the oldest version it accepts.

A deleted API key (`ikon api-key delete <id> --app-id <app-id> --yes`, or without `--app-id` in the app's folder) is refused from the next sign-in on: a new connect, or a reconnect that has to sign in again, fails with the key refused. `Connect()` throws `AuthRefusedException`, whose `what()` is a short reason with the backend's request id (`The API key was refused (403 Invalid key; request <id>)`) and whose `response` holds the request and the backend's whole answer. `IkonClient` logs that answer at Debug. Deleting it does not end any session: a client the key already connected keeps going, and a reconnect that resumes the same session with the tickets the client holds needs no sign-in.

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: cpp-sdk-auth-local -->
```cpp
LocalConfig local;
local.host = "localhost";
local.httpsPort = 8443;              // The port of the server address the app's ikon run printed
local.spaceId = "your-app-id";       // Optional: refuse a server that runs another app
local.userId = "dev-user";

IkonClientConfig config;
config.local = local;
```

The port can change between runs. A run takes the port the project's last run used while it is free, else 8443 or the next free port, so two apps run at once listen on different ports, and a run can stay on 8444 after 8443 frees up. Use the port of the server address in the line `ikon run` prints when the app is ready: `Ready: https://localhost:8443`, or the `server` address in brackets after the frontend's. It is also the `serverUrl` in the app's `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`.

A connect to a port that another app's run holds joins that other app, and that app gets everything the program sends. Set `spaceId` to the app's id (`[Target] SpaceId` in the app's `ikon-config.development.toml`) to prevent that: when the server runs another app, `Connect()` throws `std::runtime_error` naming both app ids, and every reconnect fails the same way. Without `spaceId` the client joins whatever app the port serves. A client keeps the port it was made with, so after the app's next `ikon run` on another port, make a new `IkonClient` with the new port.

A program on the same machine as the run can read the port and the app id from the app's folder with `LocalConfigFromAppFolder`, from `ikon_local_run.h`, which `ikon_sdk.h` does not include because it needs `std::filesystem`:

<!-- ikon-example: cpp-sdk-local-run -->
```cpp
// The app's folder, where ikon run runs it. Read it again before each new client: the port can change
LocalConfig local = LocalConfigFromAppFolder("../my-app");
local.userId = "dev-user";

IkonClientConfig config;
config.local = local;
```

It takes the host and port from `serverUrl` in the `ikon-server-info.json` the run writes when it starts (the newest one under `build/app/artifacts/bin` when there are several) and `spaceId` from `[Target] SpaceId`. It throws `std::runtime_error` when the folder has no `ikon-config.development.toml` or the app has not been run there.

`localClientProof` matters only for a local connect that does not look local to the run, such as one through `ikon run --host-public`. Without it, such a connect joins as a guest, or is refused where the app admits no guests. With the run's `localClientProof` from its `ikon-server-info.json`, which only a program on the run's machine can read, the run keeps `userId`. `LocalConfigFromAppFolder` fills it in. A program on the same machine connecting to `localhost` needs none.

### Guests

A client can join a deployed app as a signed-out visitor, as a browser that has not signed in does. It needs no key.

<!-- ikon-example: cpp-sdk-auth-guest -->
```cpp
GuestConfig guest;
guest.spaceId = "...";                    // The app's id (ikon app list)
guest.backendType = BackendType::Production;

IkonClientConfig config;
config.guest = guest;
config.deviceId = "device-1234";          // Keep it to come back as the same guest
```

Each device id gets a guest user of its own, unless the app's guest sign-in puts every guest in one shared user. Without `deviceId` the client picks a random one, which it keeps until it is destroyed. The app sees a guest as `UserType::Human`. When the app lets no guests in, `Connect()` throws `AuthRefusedException` with `statusCode` 403.

### User Tokens

A program that already holds a person's session token for the app, issued by another service, connects with it instead of an API key. `backendUrl` is the backend that issued the token: `https://api.prod.ikon.live` for an app on `*.ikonai.app`, `https://api.dev.ikon.live` for one on `*.dev.ikonai.app`.

An API key's `externalUserId` names a user of the program's own: the same id is the same user on every connect, but it is never one of the app's existing people and has no email, so a role the app gives by email does not reach it. Acting as an existing person needs that person's session token in this mode, and the IKON tool hands out none; only the .NET SDK's `UserLogin` connects as the person signed in with `ikon login`.

<!-- ikon-example: cpp-sdk-auth-user-token -->
```cpp
UserTokenConfig userToken;
userToken.spaceId = "...";                // The app's id (ikon app list)
userToken.token = "...";                  // The person's session token, from the service that issued it
userToken.backendUrl = "https://api.prod.ikon.live";
userToken.userType = UserType::Human;

IkonClientConfig config;
config.userToken = userToken;
```

The client cannot renew the token. When the backend refuses it (`statusCode` 401) or the app refuses the user (403), `Connect()` throws `AuthRefusedException`. During a reconnect, the client stops reconnecting at once, reports the refusal through `ErrorOccurred` and goes offline. Get a new token from the service that issued the old one, pass it to `IkonClient::UpdateAuthToken`, and call `Connect()` again. Every later connect and reconnect uses the new token. `UpdateAuthToken` throws `std::logic_error` on a client configured without `userToken`.

### Connect URLs

A client can also join through a server's connect URL (`https://<server>/connect?token=...`) that a service it trusts created for it. It does not sign in at all.

<!-- ikon-example: cpp-sdk-auth-connect-url -->
```cpp
IkonClientConfig config;
config.externalConnectUrl = "https://server.example/connect?token=...";
```

When the server refuses the URL's token, `Connect()` throws at once: `ServerChangedException` for an expired token or one signed for another server, `AuthRefusedException` for any other 401 or 403. A reconnect into a new session uses the same URL, so once its token has expired the server refuses it, and the client reports that through `ErrorOccurred` and goes offline. Make a new `IkonClient` with a new URL.

## Connection Lifecycle

### Connection States

The client tracks its connection state via `GetState()`:

| State | Description |
|-------|-------------|
| `Idle` | Not connected: the initial state, and the state after `Disconnect()` |
| `Connecting` | Authentication and connection in progress |
| `Connected` | Joined and ready; `Ready` runs right after |
| `Reconnecting` | Lost connection, attempting automatic reconnect |
| `Offline` | Disconnected (connection failed or max retries exceeded) |

### Callbacks

<!-- ikon-example: cpp-sdk-callbacks -->
```cpp
// Connection state changes
client.StateChanged = [](ConnectionState state)
{
    std::cout << "State: " << ToString(state) << std::endl;
};

// Joined and ready, after Connect() and after every reconnect: the app now knows this client's
// user, and client.Functions() already holds the app's functions
client.Ready = []()
{
    std::cout << "Ready" << std::endl;
};

// This client is stopping: on Disconnect(), destruction, a dropped connection, a join that
// timed out or before a reconnect attempt. A message sent from here is not guaranteed to be delivered
client.Stopping = []()
{
    std::cout << "Client stopping..." << std::endl;
};

// Disconnected from server
client.Disconnected = []()
{
    std::cout << "Disconnected" << std::endl;
};

// Error occurred
client.ErrorOccurred = [](const std::string& error)
{
    std::cerr << "Error: " << error << std::endl;
};

// Protocol message received
client.MessageReceived = [](const ProtocolMessage& message)
{
    std::cout << "Message opcode: " << static_cast<int>(message.GetOpcode()) << std::endl;
};
```

### Connecting and Disconnecting

<!-- ikon-example: cpp-sdk-connect-disconnect -->
```cpp
// Connect (throws on failure)
client.Connect();

// Wait for a specific client to connect
bool found = client.WaitForClient(
    "my-product",                      // productId (optional)
    std::nullopt,                      // userId (optional)
    std::chrono::seconds(30)           // timeout
);

// Disconnect
client.Disconnect();

// Or let destructor handle it
```

A failed `Connect()` reports its error twice, with the same message: first through `ErrorOccurred`, then as the
exception it throws. Show the exception for a connect your code called, and keep `ErrorOccurred` for failures no
call returns, such as a reconnect that gives up.

Once the connection is open, `Connect()` waits for the server to send the session state and the app's functions, up to `timeouts.joinTimeoutSec` (30 seconds). If they do not arrive, it closes the connection and throws `JoinTimeoutException`, and the client goes `Offline` without running `Ready`; call `Connect()` again to retry. A reconnect attempt that runs out of this time counts as a failed attempt.

`Disconnect()` and the destructor first tell the server that the client is leaving, so the server ends the session at once instead of holding it for a reconnect. They wait up to a second for that message to be written. `Disconnect()` during an automatic reconnect stops it, and the client goes from `Reconnecting` to `Idle`.

### Accessing Client State

<!-- ikon-example: cpp-sdk-client-state -->
```cpp
// Access the client configuration
const IkonClientConfig& config = client.GetConfig();

// Access the global state (available after connection).
// The returned pointer aliases the live GlobalState, which the network
// thread can replace at any time and a reconnect frees. Only safe to read
// from the receive/network thread; otherwise use SnapshotGlobalState() for a copy.
const GlobalState* state = client.GetGlobalState();
if (state)
{
    // Use global state...
}

// Thread-safe snapshot of the current GlobalState, also while a reconnect
// replaces the connection. Returns std::nullopt
// before Connect(), after Disconnect(), and when a Connect() from Idle or a
// reconnect attempt fails at its auth/token HTTP request. A Connect() retried
// from Offline keeps the earlier attempt's state, and after a later failure or
// a dropped connection it returns an empty or stale state.
std::optional<GlobalState> snapshot = client.SnapshotGlobalState();
```

## Sending Messages

`SendMessage` queues the message for a send thread and returns. `Disconnect()` queues its leaving message behind what is already queued and waits up to a second for the queue to be written up to that message. Anything still queued after that wait is dropped.

The app learns which user a client belongs to only when the client reports ready. `Connect()` and every reconnect report ready as soon as the client has joined, before `Ready` runs. A message sent while the client is joining, from another thread or during a reconnect, waits and goes out right after the client reports ready, in the order it was sent. Audio and video sent then are dropped, and if the connection drops before the client is ready, the waiting messages are dropped too. At most 4096 messages wait for one join; any sent after that are dropped, with one warning in the log.

### The App's Own Messages

An app declares its own messages in `schema/*.tp`. `ikon schema` compiles them into one header per message, in namespace `ikon::app`: into `frontend-cpp/` when the app has one, and into every folder `schema/outputs.toml` names for `cpp`, such as a program in another repository (see `teleport-schema-spec.md`). A header includes `ikon_protocol.h`, so put the SDK's folder on the include path.

`SendMessage` sends one, and `OnMessage<T>` calls a handler with every message of type `T` the client receives, decoded, with the sender's session id. The opcode, payload version and flags come from the type, so the program never names them. The handler lasts across reconnects until `Unsubscribe()`; register it before `Connect()` to see the first message the app sends once the client is ready. It runs on the receive thread, so it should return quickly and must not wait for a remote call's result. A handler that throws, or a message that does not decode, is logged as an error; the other handlers still get the message.

<!-- ikon-example: cpp-sdk-app-messages -->
```cpp
// SdkTypedPing and SdkTypedPong are the app's own messages, from its schema/*.tp,
// generated into headers in namespace ikon::app. Subscribe before Connect() so the
// handler sees the first message the app sends.
MessageSubscription subscription = client.OnMessage<app::SdkTypedPong>(
    [](const app::SdkTypedPong& pong, int32_t senderId)
    {
        std::cout << "Pong " << pong.Sequence << " from " << senderId << ": " << pong.Text << std::endl;
    });

client.Connect();

app::SdkTypedPing ping;
ping.Sequence = 1;
ping.Text = "hello";
client.SendMessage(ping);

// subscription.Unsubscribe() stops the handler; dropping the handle does not
```

### Platform Messages

`SendMessage` takes the platform's own generated messages from `ikon_protocol.h` the same way.

<!-- ikon-example: cpp-sdk-send-typed -->
```cpp
// Send a generated protocol class from ikon_protocol.h
// (creates a Teleport-encoded ProtocolMessage automatically)
ActionCustomUserMessage payload;
payload.TypeName = "ChatMessage";
payload.JsonPayload = R"({"text":"Hello"})";
client.SendMessage(payload);
```

## Shared Functions

`FunctionRegistry` lets a client register functions for the app to call and call the functions the app registers; the server offers a client's functions to the app only, not to other clients. The client owns one and attaches it itself on connect; get it with `client.Functions()`. Each join waits until the registry holds the app's functions, so they can be called from `Ready` and after `Connect()` returns; a join that does not get them fails with `JoinTimeoutException`. Register `Shared` functions before `Connect()`, as below. The client announces them on every join, reconnects included, before it reports ready, so the app has them when it first sees the client. One registered while connected is announced at once. It handles function-related protocol messages through its own message handler, independent of `MessageReceived`.

<!-- ikon-example: cpp-sdk-shared-functions -->
```cpp
auto& registry = client.Functions();

// Register a function the app can call. Registered before Connect(), it is announced on
// every join, before the client reports ready
registry.RegisterFunction(
    "Echo",                                       // name
    "Echoes back the first argument",             // description
    [](const nlohmann::json& args) -> nlohmann::json
    {
        return args.empty() ? nullptr : args[0];
    },
    {{"value", "string", "Value to echo back"}},  // parameters
    "string",                                     // result type name
    FunctionVisibility::Shared);

client.Connect();

// Wait for a remote function to become available
if (registry.WaitForFunction("Add", std::chrono::seconds(15)))
{
    // Call it, blocking until the result is received or timeout expires
    nlohmann::json result = registry.CallRemote("Add", nlohmann::json::array({5, 3}));

    // Or asynchronously via a future
    std::future<nlohmann::json> future = registry.CallRemoteAsync("Add", nlohmann::json::array({5, 3}));
}

// Disconnect detaches the registry
client.Disconnect();
```

A call made while the client is connecting or reconnecting waits until the client is ready, then goes out after the messages that waited with it, in the order the calls were made. `CallRemote` counts that wait against its timeout and, if the client is not ready in time, throws `std::runtime_error` saying the call timed out and the client was not connected; the call is never sent. A future from `CallRemoteAsync` has no time limit. When the client is not trying to connect (before `Connect()`, after `Disconnect()`, or offline after its reconnects ran out), both throw `std::runtime_error` at once saying the client is not connected, and a call that was waiting fails the same way when the client stops trying. Once the client is ready, the function is looked up in the registry as it is at that moment, so a function the app no longer has throws "Function not found". At most 4096 calls wait at a time; more throw at once, with one warning in the log. A call already sent when the connection drops, or when the program calls `Disconnect()`, gets no reply, so it fails then with a `std::runtime_error` saying it was disconnected: `CallRemote` throws at once, and the future from `CallRemoteAsync` carries the error.

The app's functions belong to the app's own session, and a local run replaces that session each time the app hot reloads. When a session leaves, its functions leave the registry, and a call it has not answered fails at once with a `std::runtime_error` saying the session left. The new session registers the functions again moments later, and calls by name go to it. A call made between the two throws "Function not found", so after a hot reload wait with `WaitForFunction` before calling again.

When the app calls a function this client registered, the handler runs on one of the registry's own threads, not on the thread that receives messages, so a handler may itself call the app's functions and wait for their results. Up to 16 calls run at once, and they start in the order they arrived. What a handler throws reaches the app as the call's error. A call that times out in `CallRemote` after it was sent is cancelled on the app.

Additional members: `Call` (invoke a local function), `HasFunction`, `GetFunctionNames`, `RemoveFunction`, and the `FunctionRegistered` / `FunctionUnregistered` callbacks.

### Streams, Cancellation and Binary Values

<!-- ikon-example: cpp-sdk-function-streams -->
```cpp
auto& registry = client.Functions();

// A function that returns a stream (IAsyncEnumerable<int> in a C# app): read its items as
// the app produces them. Next() returns std::nullopt once the app ends the stream
RemoteStream stream = registry.CallRemoteStream("CountTo", nlohmann::json::array({3}));

while (std::optional<nlohmann::json> item = stream.Next(std::chrono::seconds(10)))
{
    std::cout << "Item: " << item->dump() << std::endl;
}

// Stop a stream early: the app's function is cancelled. Destroying a stream that has not
// ended does the same
RemoteStream ticks = registry.CallRemoteStream("Ticks");
ticks.Next();
ticks.Cancel();

// Cancel a call made with CallRemoteAsync; its future then throws FunctionCallCancelledException
TeleportGuid callId;
std::future<nlohmann::json> slow = registry.CallRemoteAsync("SlowReport", nlohmann::json::array(), "", callId);
registry.CancelRemoteCall(callId);

// A byte[] argument or result is a JSON binary value
std::vector<uint8_t> bytes = {1, 2, 3};
nlohmann::json result = registry.CallRemote("Checksum", nlohmann::json::array({nlohmann::json::binary(bytes)}));

if (result.is_binary())
{
    const std::vector<uint8_t>& resultBytes = result.get_binary();
    std::cout << "Got " << resultBytes.size() << " bytes" << std::endl;
}
```

`Next()` throws `std::runtime_error` when the app's function throws, when the connection drops or the client disconnects, and when no item arrives within its timeout (30 seconds by default); a timeout leaves the stream open. Items that arrive faster than they are read wait in memory, and past 65536 unread items the stream fails and the call is cancelled on the app. A `RemoteStream` refers to the registry, so it must not outlive the `IkonClient`. Calling a function that returns a single value with `CallRemoteStream` ends the stream without an item. `CancelRemoteCall` returns false when the call had already completed. A handler this client registers takes a `byte[]` argument as a JSON binary value too, and a binary value it returns reaches the app as `byte[]`.

### Stream Functions on the Client

A client can register a function that returns a stream too, which a C# app reads as an `IAsyncEnumerable`:

<!-- ikon-example: cpp-sdk-client-streams -->
```cpp
// A function the app reads as IAsyncEnumerable<int>: each Write() is one item, and returning
// ends the stream
client.Functions().RegisterStreamFunction(
    "Countdown",
    "Counts down from a number, one a second",
    [](const nlohmann::json& args, FunctionStreamWriter& writer)
    {
        for (int i = args.at(0).get<int>(); i >= 0; --i)
        {
            // False once the app cancelled the call or stopped reading
            if (!writer.Write(i))
            {
                return;
            }

            std::this_thread::sleep_for(std::chrono::seconds(1));
        }
    },
    {{"from", "System.Int32", "Where to start"}},  // parameters
    "System.Int32",                                // item type name
    FunctionVisibility::Shared);

client.Connect();
```

The handler runs on the registry's own threads, as a `RegisterFunction` handler does. Each `Write()` sends one item, the stream ends when the handler returns, and what the handler throws reaches the app as the stream's error. `itemTypeName` is the C# type (`"System.Int32"`, `"System.String"`) that the app reads each item's JSON as. When the app cancels the call or stops reading, `Write()` returns false and sends nothing, and the handler should return; `IsCancelled()` gives the same answer without writing. A dropped connection or a new session cancels every running stream the same way. The `FunctionStreamWriter` is valid only while the handler runs.

## Client Context and Path

The app sees each client's viewport size, theme, timezone and visibility in the client's context. Tell it when one of them changes, and when the client shows another path:

<!-- ikon-example: cpp-sdk-client-context -->
```cpp
// Tell the app the window was resized. Fields left empty keep the value last sent
ClientContextUpdate update;
update.viewportWidth = 1280;
update.viewportHeight = 720;
client.UpdateClientContext(update);

// Later, only the theme changes; the viewport stays 1280x720
ClientContextUpdate themeUpdate;
themeUpdate.theme = "dark";
client.UpdateClientContext(themeUpdate);

// Tell the app this client now shows another path, as a browser does when it navigates
client.SetPath("/room/7");
```

Both throw `std::runtime_error` when the client is not connected. The server replaces all five context values on each update, so the SDK fills each empty field with the value last sent, or else the one the client connected with. A reconnect into a new session, and the next `Connect()` after `Disconnect()`, send the last values and the last path again. The path is relative to the app's root; a query string reaches the app's path handlers but is not kept as the path.

## Audio and Video

The SDK sends and receives media as encoded frames. It does not encode or decode them: the program brings its own encoders and decoders ("Encoding and Decoding Audio" below shows Opus). Audio is Opus; video is H.264 (Annex B), VP8 or VP9. A client's audio and video reach the app only. In the example, `opusPackets` holds your encoder's packets and `playingStreamId` names a stream your player is playing.

<!-- ikon-example: cpp-sdk-media -->
```cpp
// Media the app sends this client, as encoded frames. The callbacks name each stream
// "{senderId}_{trackId}"; begin.StreamId is the app's own id for it.
// These run on the receive thread: hand the frames to a decoder, do not call the app from here
client.AudioStreamBegan = [](const std::string& streamId, int32_t senderId, const AudioStreamBegin& begin)
{
    // begin.Codec holds an AudioCodec. Make an Opus stream's decoder with begin.SampleRate and begin.Channels
    bool isOpus = static_cast<AudioCodec>(begin.Codec) == AudioCodec::Opus;
    std::cout << "Audio " << streamId << " (" << begin.StreamId << "): " << (isOpus ? "Opus, " : "not Opus, ")
              << begin.SampleRate << " Hz, " << begin.Channels << " ch" << std::endl;
};
client.AudioFrameReceived = [](const std::string& streamId, const AudioFrame& frame)
{
    // frame.Samples is one Opus packet
};
client.VideoStreamBegan = [](const std::string& streamId, int32_t senderId, const VideoStreamBegin& begin)
{
    std::cout << "Video " << streamId << ": " << begin.Width << "x" << begin.Height << std::endl;
};
client.VideoFrameReceived = [](const std::string& streamId, const VideoFrame& frame)
{
    // frame.Data is one encoded frame in the codec the begin named
};

// Tell the app how far your player has played a stream, from the start of the current segment,
// and how much it holds that it has not played yet. Either id of the stream works
client.SendAudioPlaybackReport(playingStreamId, std::chrono::milliseconds(1500), std::chrono::milliseconds(120));

// Send the packets of your Opus encoder: 20 ms each at 48 kHz mono. The correlation id ties the
// audio to what the app asked for, such as a press of a talk button
for (size_t i = 0; i < opusPackets.size(); i++)
{
    client.SendAudioFrame("mic", opusPackets[i], 48000, 1, 960, i == 0, i == opusPackets.size() - 1, "talk-1");
}

client.EndAudioStream("mic");

// The app asks for a keyframe when it starts showing the stream or loses frames
auto keyFrameWanted = std::make_shared<std::atomic<bool>>(true);
client.VideoKeyFrameRequested = [keyFrameWanted](const std::string& streamId, int32_t requesterId)
{
    *keyFrameWanted = true;
};

// Send H.264 (Annex B) frames at 30 fps; EncodeNextH264Frame stands for your encoder. A change
// of size, codec or correlation id announces the stream again
for (int i = 0; i < 90; i++)
{
    bool isKey = keyFrameWanted->exchange(false);
    client.SendVideoFrame("camera", EncodeNextH264Frame(isKey), isKey, std::chrono::microseconds(i * 33333),
                          VideoCodec::H264, 640, 480);
}

client.EndVideoStream("camera");
```

`SendAudioFrame` takes a sample rate of 8000, 12000, 16000, 24000 or 48000 Hz and 1 or 2 channels, fixed by the stream's first packet; another format throws `std::invalid_argument`, so start a new stream id for it. `isFirst` and `isLast` mark a segment such as one utterance. The app's speech recognition ignores audio without a correlation id, and a new correlation id on a first packet announces the stream again. `SendVideoFrame`'s `sourceType` (`"camera"` by default, or `"screen"`) sets the kind of video input the app sees. Send a keyframe first, because a receiver shows nothing until one arrives. Both throw `std::runtime_error` when the client is not connected, and media sent before the client is ready is dropped. A reconnect announces every open stream again.

Each incoming stream has two ids. The callbacks, `RequestVideoKeyFrame` and `SendAudioPlaybackReport` use `"{senderId}_{trackId}"`, such as `"1_3"`. `begin.StreamId` is the id the app gave the stream, such as the `streamId` it passed to `Audio.Raw.SendFrameAsync`; use it to tell the app which of its streams you mean. `begin.Description` names the kind of source, not the stream.

Incoming streams end with `AudioStreamEnded` and `VideoStreamEnded`, which also run for every open stream on `Disconnect()` and on a reconnect into a new session. `RequestVideoKeyFrame(streamId)` asks the sender of an incoming video stream for a keyframe, and returns false when that stream is not open or the client is not connected.

A program that plays the app's audio reports how far it has played each stream with `SendAudioPlaybackReport(streamId, played, buffered, state)`: every 500 ms of playback, and at once when the state changes, as the browser does. `played` is the position from the start of the current segment (`std::nullopt` when unknown), `buffered` is what the player holds that it has not played, and `state` is `AudioPlaybackState::Playing` (the default), `Blocked` or `Hidden`. The app reads the latest report with `Audio.Raw.GetPlaybackStatus` and `Audio.Raw.PlaybackReportReceivedAsync`, to pace what it says to what was heard. Only the streams the app sends with `Audio.Raw.SendFrameAsync` ask for reports: `WantsPlaybackReports(begin)` is true for them, and false for every other stream, such as the one that carries what `Audio.Play` plays, except that every stream of an app built before the flag existed asks. The report names the segment of the stream's last received frame. `SendAudioPlaybackReport` sends nothing and returns false for a stream that is not open or wants no reports, and throws `std::runtime_error` when the client is not connected.

### Encoding and Decoding Audio

Any Opus library works. libopus comes from vcpkg (`opus` in a `vcpkg.json`, or `vcpkg install opus`). Find it with `find_package(Opus CONFIG REQUIRED)` in a build configured with vcpkg's toolchain file (see "Building a Program"), and link it as `Opus::opus`. On Linux the system package works without vcpkg: `apt install libopus-dev` (`dnf install opus-devel` on Fedora), then `find_package(PkgConfig REQUIRED)` and `pkg_check_modules(opus REQUIRED IMPORTED_TARGET opus)`, and link `PkgConfig::opus`. Either way, include it as `#include <opus.h>`. Encode 20 ms frames, 960 samples per channel at 48 kHz, as the .NET SDK does, and pass the same sample rate, channel count and frame size to `SendAudioFrame`. Make a decoder for each incoming stream in `AudioStreamBegan` from `begin.SampleRate` and `begin.Channels`, never from what the app sent: audio the app sends as mono can arrive as 48 kHz stereo.

<!-- example-skip: needs libopus, which the SDK does not include -->
```cpp
int error = 0;
OpusEncoder* encoder = opus_encoder_create(48000, 1, OPUS_APPLICATION_VOIP, &error);
std::vector<uint8_t> packet(4000);
int bytes = opus_encode(encoder, pcm, 960, packet.data(), static_cast<opus_int32>(packet.size()));  // pcm: 960 int16 samples
packet.resize(bytes);

OpusDecoder* decoder = opus_decoder_create(begin.SampleRate, begin.Channels, &error);  // in AudioStreamBegan
std::vector<opus_int16> out(5760 * begin.Channels);  // room for 120 ms, the longest Opus packet
int samplesPerChannel = opus_decode(decoder, frame.Samples.data(), static_cast<opus_int32>(frame.Samples.size()), out.data(), 5760, 0);
```

## Client Events

Client events tell this client about the other clients in the session and about the app itself. Each handler gets the other client's `Context`: its `SessionId`, `UserId`, `ContextType`, and what it reports of its viewport, theme, timezone and visibility.

<!-- ikon-example: cpp-sdk-client-events -->
```cpp
// Set before Connect(): on joining, ClientJoined runs for each client already there
client.ClientJoined = [](const Context& other)
{
    std::cout << "Joined: " << other.SessionId << " (" << other.UserId << ")" << std::endl;
};
client.ClientUpdated = [](const Context& other)
{
    std::cout << "Updated: " << other.SessionId << " now uses the " << other.Theme << " theme" << std::endl;
};
client.ClientLeft = [](const Context& other)
{
    std::cout << "Left: " << other.SessionId << std::endl;
};

client.Connect();
```

`ClientJoined` runs once a client has finished joining, when the server has sent it the session's state, so the app can answer it from then on. On joining, it runs for each client already there. A client that leaves before it has finished joining is never reported, and `ClientUpdated` and `ClientLeft` run only for a client `ClientJoined` reported. Each client is reported once per session. A reconnect into the same session reports only what changed while this client was away: `ClientJoined` for a client that joined meanwhile, and `ClientUpdated` for one whose viewport, theme, timezone or visibility changed. A client that left meanwhile raises `ClientLeft` once the server has sent the clients still there. A reconnect into a new session reports its clients as a first join does. `ClientUpdated` runs when a client reports a new viewport, theme, timezone or visibility, and is read from the client's context in each `GlobalState` the server sends. The app is one of the clients: a `ContextType::Plugin` client with `IsInternal` true, which gets a new `SessionId` each time the app restarts or hot reloads. The events are never about this client, and a lifecycle batch the server sends is reported event by event. They run on the receive thread, so a handler should return quickly and must not wait for a remote call's result. Set them before `Connect()`.

## Reactive Subscriptions

`client.Reactives()` follows values the app holds in a `Reactive<T>`, `ClientReactive<T>`, `UserReactive<T>` or `MountReactive<T>` without rendering its UI. It follows a value by its stable id. The app's built-in function `Ikon.Reactive.GetStableIdByName` returns the stable id of a reactive field or auto-property of the app's `[App]` class, by its name in the source (`"_score"` for `private readonly Reactive<int> _score`), so the app needs no code of its own for it. For a reactive kept anywhere else, the app returns its `StableId` from an `External` function.

<!-- ikon-example: cpp-sdk-reactive -->
```cpp
// The app looks up a reactive's stable id by the member's name in its [App] class
std::string stableId = client.Functions()
                           .CallRemote("Ikon.Reactive.GetStableIdByName", nlohmann::json::array({"_score"}))
                           .get<std::string>();

// The callback gets the current value before Subscribe returns, then every change in order
ReactiveSubscription subscription = client.Reactives().Subscribe(stableId, [](const nlohmann::json& value)
{
    std::cout << "Score: " << value.get<int>() << std::endl;
});

// ...

// Stops the callback; the last subscriber of a value also stops the app pushing it
subscription.Unsubscribe();
```

`Subscribe` blocks for the app's reply and throws `std::runtime_error` when the app refuses the subscription or the client is not connected. The callback gets the current value before `Subscribe` returns, on the calling thread, then every change in the order the app made it, on one of the registry's threads; a reactive with no value yet is not delivered. What the callback throws for the current value is rethrown by `Subscribe`, which then leaves the value unsubscribed; what it throws for a later change is logged as a warning. Pass `mountId` for a `MountReactive<T>`. Destroying a `ReactiveSubscription` keeps its callback; `Unsubscribe()` removes it, blocking for the app's reply. A reconnect can end up in a new session that has none of the subscriptions, so each time the client is ready, the SDK subscribes every followed value again and delivers its current value; `Resubscribe()` does the same on demand. A `ReactiveSubscription` must not outlive the `IkonClient`.

## UI

`client.UI()` holds the app's UI as this client receives it: one tree per stream, the styles the trees use, and the actions their controls call. The SDK applies the app's diffs, asks for a whole tree again when a diff cannot be applied, and acknowledges each update, as the web client does. It draws nothing. Render a stream's `root` with a UI of your own, and call the actions of its controls.

<!-- ikon-example: cpp-sdk-ui -->
```cpp
IkonUI& ui = client.UI();

// Runs on the receive thread for every change: hand the tree to your renderer, call no action here
ui.StreamUpdated = [](const UIStream& stream)
{
    std::cout << "Stream " << stream.streamId << " (" << stream.category << ") is at version " << stream.version << std::endl;
};

// Find a button by its label, waiting up to 30 s for the app to send it. A button with an icon
// names itself in "aria-label" instead
std::shared_ptr<const UINode> save = ui.WaitForNode([](const UINode& node)
{
    return node.type == "std.button" && node.GetText("label").value_or("") == "Save";
}, std::chrono::seconds(30));

if (save)
{
    // Press it, as a person would; true when the app's handler completed
    bool isDone = ui.CallAction(*save, "onInvokeId");
    std::cout << "Saved: " << isDone << std::endl;
}

// A text field takes its new value as the text itself, not as JSON
std::shared_ptr<const UINode> name = ui.WaitForNode([](const UINode& node)
{
    return node.type == "std.text-field" && node.GetActionId("onValueChangeId");
}, std::chrono::seconds(30));

if (name)
{
    ui.CallAction(*name, "onValueChangeId", "Ada");
}

// The app's page, and every node's styles by id
if (std::shared_ptr<const UIStream> page = ui.Page())
{
    std::cout << "Page root: " << (page->root ? page->root->type : "none") << ", " << ui.Styles().size() << " styles" << std::endl;
}
```

A `UIStream` is one mount at one moment: `category` is `"ikon-ui"` for the app's page (also `Page()`), `"ikon.platform"` for the platform's layer above it, or another mount the app declares. `Streams()` lists them all. A `UINode` has an `id`, a `type` naming the component (`"std.button"`, `"std.text-field"`), its `props` as JSON, its `children` and its `styleIds` into `Styles()`. Nodes are immutable: an update replaces the nodes on the changed path and keeps the rest, so a node read earlier stays as it was. `UINode::Find` and `UIStream::FindNode` search a tree depth first.

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
| `std.button` with an `href` (a link) | `href`, `target`, and the text as on a button | None unless the app also handles the press (`onInvokeId`, `{}`). A client follows the `href` itself |
| `std.action-button` | `label`, `actionType` (`CopyToClipboard`, `DownloadFile`, `Share` and the others) and `actionOptionsJson`, the action's options as JSON | The action runs in the browser, so a client does it itself. `onActionCompleteId` reports it, `{"ActionType":"CopyToClipboard","Success":true}` |
| `std.feed-scroller` | `activeIndex` when the app controls it, `slideCount`, `muted`. Each slide is a `std.feed-slide` with its `index` | `onActiveChangeId` when a slide becomes the active one and `onScrollNearEndId` near the end, the slide's index as a number (`3`); `onMuteChangeId`, `true` or `false` |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

A payload the app cannot read, such as `0.8` for a slider instead of `[0.8]`, fails the call like a handler that throws: `CallAction` returns false, and the reason is only in the app's log (`ikon logs --level warn`).

`Actions()` lists a node's actions by prop: `"onInvokeId"` for a button's press, `"onValueChangeId"` for a text field's new value, `"onCheckedChangeId"` for a checkbox. `CallAction` sends `"{}"` by default; a text field's payload is the text itself, a checkbox's `"true"` or `"false"`. It blocks until the app answers and returns whether the handler completed, and throws `std::runtime_error` when not connected, on a timeout (30 s by default), and when the client moves to a new server session before the answer arrives. `StreamUpdated` and `StreamEnded` run on the receive thread, which delivers `CallAction`'s answer, so never call `CallAction` from them or from `MessageReceived`. A reconnect into the same session asks for every tree again; one into a new session ends every stream. `RequestResync(stream)` asks for a whole tree on demand.

A client that leaves `Opcode::GROUP_UI` out of `IkonClientConfig::opcodeGroupsFromServer` gets no UI: `IsEnabled()` is false and the trees stay empty.

## File Upload

`UploadFile` sends a file through one of the app's `FileUpload` or `FileUploadZone` controls, which names the upload in its `uploadActionId` prop. The app's `onUploadComplete` gets the file.

<!-- ikon-example: cpp-sdk-upload -->
```cpp
// The app's FileUpload control names the upload in its uploadActionId prop
std::shared_ptr<const UINode> control = client.UI().WaitForNode([](const UINode& node)
{
    return node.GetActionId("uploadActionId").has_value();
}, std::chrono::seconds(30));

if (!control)
{
    return;
}

std::ifstream file("report.pdf", std::ios::binary);
UploadOptions options;
options.mimeType = "application/pdf";
options.progress = [](const UploadProgress& progress)
{
    std::cout << progress.acknowledgedBytes << " of " << progress.totalBytes << " bytes" << std::endl;
};

try
{
    // Blocks until the app's onUploadComplete has run, resuming across reconnects
    UploadResult result = client.UploadFile(*control->GetText("uploadActionId"), file, "report.pdf", options);
    std::cout << "Uploaded " << result.size << " bytes, SHA-256 " << result.hash << std::endl;
}
catch (const UploadFailedException& ex)
{
    std::cout << "Upload failed: " << ex.what() << (ex.IsRetriable() ? " (worth retrying)" : "") << std::endl;
}
```

`UploadFile` blocks until the app's `onUploadComplete` has run and returns the upload's id, size and the lowercase hex SHA-256 of the bytes, which the app verified before storing them. It reads the stream from its position to its end, and again from the last acknowledged byte after a resume, so the stream must be seekable and outlive the call. If the connection drops after the upload started and the client reconnects within 45 s, the upload resumes on the new connection. It throws `std::runtime_error` when not connected, `std::invalid_argument` for a missing id or name or an unseekable stream, and `UploadFailedException` when the app refuses the upload or it fails; `IsRetriable()` says whether sending the same bytes again is likely to work. `UploadOptions` also takes the `uploadId` the app sees in every upload hook (empty generates one) and, for a file from a folder, its `relativePath`. `progress` runs on the receive thread, so it should return quickly. At most 8 uploads per client run at once; later calls wait. Never call `UploadFile` on the receive thread.

## Interface Implementations

The SDK requires you to provide implementations of three interfaces. Example implementations are included in the SDK: `ExampleLogger`, `ExampleHttpClient` and `ExampleNetworkClient` in `example_logger.h`, `example_http_client.h` and `example_network_client.h`. `ExampleNetworkClient` needs OpenSSL; `ExampleHttpClient` uses WinHTTP on Windows, except for [post-quantum key exchange](#post-quantum-key-exchange), and elsewhere needs OpenSSL and `CPPHTTPLIB_OPENSSL_SUPPORT` defined.

### ILogInterface

Implement every logging method:

<!-- ikon-example: cpp-sdk-log-interface -->
```cpp
class ConsoleLogger : public ILogInterface
{
public:
    void Initialize() override {}
    void Trace(const std::string& message) override { Write("TRACE", message); }
    void Debug(const std::string& message) override { Write("DEBUG", message); }
    void Info(const std::string& message) override { Write("INFO", message); }
    void Warning(const std::string& message) override { Write("WARN", message); }
    void Error(const std::string& message) override { Write("ERROR", message); }
    void Critical(const std::string& message) override { Write("CRITICAL", message); }

private:
    static void Write(const char* level, const std::string& message)
    {
        std::cout << "[" << level << "] " << message << std::endl;
    }
};
```

`ExampleLogger` prints every line; `ExampleLogger(ExampleLogger::Level::Info)` drops the trace and debug lines.

### IHttpInterface

Implement `Send`, which makes one HTTP request and returns its response:

<!-- ikon-example: cpp-sdk-http-interface -->
```cpp
class MyHttpClient : public IHttpInterface
{
public:
    HttpResponse Send(const HttpRequest& request) override
    {
        // Send request.content to request.url with request.headers, as GET or POST
        // (request.method), giving up after request.timeoutMs. disableCertificateValidation
        // is true when connecting to a local IKON server, whose certificate is self-signed, and
        // when fetching the entrypoints with an API key on BackendType::Development.
        const char* method = request.method == HttpMethod::Post ? "POST" : "GET";
        std::cout << method << " " << request.url << std::endl;

        HttpResponse response;
        response.response_code = 200;  // the HTTP status code
        response.headers = {};         // the response headers
        response.content = "";         // the response body
        return response;
    }
};
```

### INetworkInterface

Implement a TLS connection over TCP. The SDK connects to the server's TLS entrypoint, so a plain TCP socket is not enough:

<!-- ikon-example: cpp-sdk-network-interface -->
```cpp
class MyNetworkClient : public INetworkInterface
{
public:
    // Open a TLS connection to host:port. Verify the server's certificate and host name unless
    // GetVerifyServerCertificate() is false, which the SDK sets only for a local server.
    bool Connect(const std::string& host, int port) override { return false; }

    // Close the connection, then call the connection-closed callback.
    void Disconnect() override {}

    void Write(const uint8_t* data, size_t size) override {}

    // Block until data arrives, and return 0 once the connection has closed.
    size_t Read(uint8_t* buffer, size_t maxSize) override { return 0; }

    bool IsConnected() const override { return false; }

    void SetConnectionClosedCallback(std::function<void()> callback) override
    {
        _connectionClosed = std::move(callback);
    }

private:
    std::function<void()> _connectionClosed;
};
```

On Linux and macOS, a write to a connection the peer has reset raises `SIGPIPE`, which ends the program by default. A network client must turn it into a failed write: `ExampleNetworkClient` sets `SO_NOSIGPIPE` on the socket where it exists (macOS) and on Linux gives OpenSSL a socket BIO of its own (`ExampleSocketBio`) that writes with `send(..., MSG_NOSIGNAL)`. A client that writes with `send` itself passes `MSG_NOSIGNAL` the same way.

The SDK calls `SetVerifyServerCertificate` before each `Connect`: `true` (the default) for the cloud, and `false` only when `IkonClientConfig::local` is set, because a local IKON server's certificate is self-signed. `ExampleNetworkClient` verifies the chain against the system's trusted roots (the Windows root store on Windows, OpenSSL's default paths elsewhere) and checks the host name or IP address; when verification fails, `Connect` throws `std::runtime_error` naming the host and the reason.

## Advanced Configuration

### Timeouts

<!-- ikon-example: cpp-sdk-timeouts -->
```cpp
config.timeouts.connectionTimeoutSec = 30;    // Connection timeout
config.timeouts.provisioningTimeoutSec = 60;  // Server startup timeout
config.timeouts.joinTimeoutSec = 30;          // Session state and the app's functions, once connected
config.timeouts.maxReconnectAttempts = 6;     // Max reconnection attempts
config.timeouts.reconnectBackoffMs = 500;     // Initial backoff (ms), doubled on each attempt
```

In API-key mode the `/init` request that waits up to `provisioningTimeoutSec` is itself sent with an HTTP timeout of `connectionTimeoutSec`, so raise `connectionTimeoutSec` to at least `provisioningTimeoutSec` for a longer provisioning wait to take effect.

### Reconnection

When a connection the client joined drops, the client reconnects by itself. It waits `reconnectBackoffMs` before the first attempt and twice as long before each next one, each wait shortened at random by up to a quarter so that the clients one server dropped do not all come back at once. When the backend answers an attempt with `RateLimitedException` (a 429 that is not a full server), the next attempt waits at least its `retryAfter`, capped at 5 minutes, plus up to half as long again. After `maxReconnectAttempts` failed attempts it goes `Offline`, and only a new `Connect()` connects it again. The client stops reconnecting at once and goes `Offline` when retrying cannot help: a `joinOnly` client finds no live session, the platform refuses the client's build (`ClientUpgradeRequiredException`), the backend says the app is not available (`SpaceNotFoundException`, a 404 or 410 from the sign-in or `/init`, as for a suspended app), the backend refuses the credential or the app refuses the user (`AuthRefusedException`), or a server refuses the token in an `externalConnectUrl`.

Each attempt first tries to resume the session the client had. It opens a new connection to the same server address with that session's auth ticket and does not ask for a new session, so the app still has the client's session and what it keeps for it. The server holds a dropped session for its soft-retry window, which it sends in `AuthResponse.SoftRetryWindowMs` and which is 5 minutes by default. The attempt starts a new session instead, as `Connect()` does, when the window has passed, or when the server closes the connection before the client has joined, which is how it refuses a session it no longer holds. The server then ends the old session. Once the server has answered a request for a new session, the later attempts start new sessions too.

An attempt that cannot open a connection at all, as while the network is down, costs the server nothing. During the first half of the attempts, the next attempt tries to resume the session again. From the second half on, such an attempt also asks for a new session, so a server that has gone away is replaced. A request for a new session that reaches nothing leaves the old session held, and the next attempt tries to resume it again. With the default 6 attempts and 500 ms backoff, the attempts run at most about 0.5, 1.5, 3.5, 7.5, 15.5 and 31.5 seconds after the drop, so a client whose network comes back within about 15 seconds returns to the session it had.

A resumed join ends the same way as a new one: the client announces its shared functions, reports ready, and `Ready` runs.

### Post-Quantum Key Exchange

<!-- ikon-example: cpp-sdk-require-post-quantum -->
```cpp
config.requirePostQuantum = true;
```

With `requirePostQuantum` set, every TLS connection the client makes (the sign-in, `/init`, `/connect` and the connection to the server) offers only the hybrid post-quantum key exchange group X25519MLKEM768, and only TLS 1.3. A server that cannot negotiate it fails the handshake, and the client never falls back to a classical key exchange. The deployed IKON servers and the load balancers negotiate it. A server on Windows (a local `ikon run` reached at a non-loopback address) has no ML-KEM in its TLS, so the handshake fails there. A connection to a loopback address (`localhost`, `127.0.0.0/8`, `::1`), such as a local `ikon run` on the same machine, crosses no network and is exempt. The client reaches a server only over TLS, so no other transport needs to be turned off. An app that sets `RequirePostQuantum` in its `ikon-config.toml` makes its server offer only transports that can use post-quantum key exchange, but only this flag makes the client refuse a classical key exchange.

The SDK passes the flag to your interface implementations: `HttpRequest::requirePostQuantum` on each request, and `GetRequirePostQuantum()` on the network interface before each `Connect`. Both are `false` for a loopback host. An implementation says it honours them by overriding `SupportsPostQuantumOnly()` to return `true`. With the flag set, `Connect()` throws `std::runtime_error` before sending anything when either interface returns `false`, the default, so an implementation written before the flag existed cannot connect with a classical key exchange. With OpenSSL, the TLS setup is two calls:

<!-- ikon-example: cpp-sdk-post-quantum-tls -->
```cpp
// Call it on the SSL_CTX before the handshake when HttpRequest::requirePostQuantum or
// GetRequirePostQuantum() is true, and fail the request or the connect when it returns false.
// OpenSSL before 3.5 has no X25519MLKEM768, and the second call fails there.
bool OfferOnlyPostQuantum(SSL_CTX* context)
{
    return SSL_CTX_set_min_proto_version(context, TLS1_3_VERSION) == 1 &&
           SSL_CTX_set1_groups_list(context, "X25519MLKEM768") == 1;
}
```

`ExampleHttpClient` and `ExampleNetworkClient` honour the flag and need OpenSSL 3.5 or later for it. With an older OpenSSL their `SupportsPostQuantumOnly()` returns `false`, so `Connect()` refuses. WinHTTP has no setting for the key exchange group, so on Windows `ExampleHttpClient` sends a request that requires post-quantum key exchange through cpp-httplib over OpenSSL instead. That needs `CPPHTTPLIB_OPENSSL_SUPPORT`, which `ikon::sdk_examples` defines; without it, `SupportsPostQuantumOnly()` returns `false`.

### Protocol Options

<!-- ikon-example: cpp-sdk-protocol-options -->
```cpp
// Filter which message types to receive/send. These values are the default: every group,
// plus GROUP_APP_LOCAL for the app's own schema messages. Without it those messages are dropped.
const auto groups = static_cast<int32_t>(Opcode::GROUP_ALL) | static_cast<int32_t>(Opcode::GROUP_APP_LOCAL);
config.opcodeGroupsFromServer = static_cast<Opcode>(groups);
config.opcodeGroupsToServer = static_cast<Opcode>(groups);

// Payload serialization format
config.payloadType = PayloadType::Teleport;  // Default
```

### Client Identification

<!-- ikon-example: cpp-sdk-client-identification -->
```cpp
config.deviceId = "unique-device-id";
config.productId = "my-app";
config.versionId = "1.0.0";
config.installId = "install-xyz";
config.locale = "en-US";
config.description = "My Application";
config.parameters = {
    {"custom_param", "value"}
};
```

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `IkonClient` | Main client class for connecting to IKON servers |
| `IkonClientConfig` | Configuration struct for the client |
| `ConnectionState` | Enum: `Idle`, `Connecting`, `Connected`, `Reconnecting`, `Offline`; `ToString(state)` gives the name |
| `FunctionRegistry` | Registry for local and shared (cross-client) functions |
| `FunctionVisibility` | Enum: `Local`, `Shared` |
| `RemoteStream` | The items of a remote function that returns a stream, from `CallRemoteStream` |
| `FunctionStreamWriter` | What a handler registered with `RegisterStreamFunction` writes its items to |
| `FunctionCallCancelledException` | Thrown by the future of a call cancelled with `CancelRemoteCall` |
| `JoinTimeoutException` | Thrown by `Connect()` when the server does not finish the join within `joinTimeoutSec` |
| `ClientContextUpdate` | The context values `UpdateClientContext` changes; empty fields keep their value |
| `ReactiveRegistry` | Follows the app's reactive values, from `client.Reactives()` |
| `ReactiveSubscription` | One callback following one reactive value, from `Subscribe` |
| `MessageSubscription` | One handler of one message type, from `OnMessage` |
| `IkonUI` | The app's UI trees, styles and actions, from `client.UI()` |
| `UIStream` | One UI stream: the tree of one mount at one moment |
| `UINode` | One immutable node of a UI tree |
| `UploadOptions` | The MIME type, upload id, relative path and progress callback of an upload |
| `UploadResult` | What `UploadFile` returns: the upload id, size and SHA-256 |
| `UploadProgress` | Bytes acknowledged so far, passed to `UploadOptions::progress` |
| `UploadFailedException` | An upload that did not finish, and whether a retry is likely to work |

### Configuration Types

| Type | Description |
|------|-------------|
| `LocalConfig` | Configuration for local server development; `LocalConfigFromAppFolder` (in `ikon_local_run.h`) reads it from the app's folder |
| `ApiKeyConfig` | Configuration for API key authentication |
| `TimeoutConfig` | Timeout settings |
| `BackendType` | Enum: `Production`, `Development` |

### Interface Types

| Type | Description |
|------|-------------|
| `ILogInterface` | Logging interface to implement |
| `IHttpInterface` | HTTP client interface to implement |
| `INetworkInterface` | TLS-over-TCP client interface to implement |

### Protocol Types

| Type | Description |
|------|-------------|
| `ProtocolMessage` | Protocol message for sending/receiving |
| `Context` | Client context from server |
| `GlobalState` | Global state from server |

## License

This SDK is licensed under the Apache License 2.0. See `LICENSE` for details.

## Support

For issues and feature requests, contact IKON support or open an issue on GitHub.
