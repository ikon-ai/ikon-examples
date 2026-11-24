<!-- checked-against: 7a42e82be4a61e33e3b0c442 -->

# IKON C++ SDK

How a C++ program, from a desktop tool to firmware on a microcontroller, connects to an IKON app with the C++ SDK. Every example below compiles as C++17.

The SDK is a header-only library. It needs no exceptions, no RTTI and no threads, and it never opens a socket, starts a thread or reads the clock by itself: the program gives it a TLS connection, an HTTPS client, a clock and a log. The same headers run on a desktop with the standard library and on a chip with a few hundred kilobytes of memory.

What it does:

- Connects in five ways: an API key, a local run, a guest, a user token or a connect URL
- Reports the connection's state, and reconnects by itself after a drop
- Sends and receives the platform's messages and the app's own schema messages, read in place without copying
- Through add-ons the program includes only when it uses them:
  - `Functions`: functions the app can call on the client, and calls of the app's functions, including streams in both directions, cancellation and binary values
  - `Reactives`: values the app holds in a `Reactive<T>`, followed as they change
  - `IkonUI`: the app's UI trees, kept current, and the actions of their controls
  - `Uploads`: files sent through the app's upload controls, resumed across reconnects
  - `Media`: audio and video sent and received as encoded frames (Opus, H.264, VP8, VP9)
- Tells the app about other clients joining, changing and leaving, and about this client's viewport, theme, time zone, visibility and path

## Requirements

- A C++17 compiler. Exceptions and RTTI may be off; the SDK reports every failure as a value.
- Threads are optional. Without them (`IKON_NO_THREADS`), the program calls `Poll` from its one thread.
- Four things from the machine, given to the client as interfaces (see [Interfaces a port implements](#interfaces-a-port-implements)):
  - `INetwork`: a TLS connection over TCP
  - `IHttp`: HTTPS requests
  - `ISystem`: a clock, random numbers and a way to wait
  - `Log`: where the SDK's log lines go (optional)

On a desktop or a server the SDK's own implementations cover all four: `StdSystem` and `StderrLogSink` in `ikon_platform_std.h`, the example clients in `example_network_client.h` and `example_http_client.h`, which need OpenSSL, and the console log in `example_logger.h`.

## Building a Program

The SDK is headers only, with nothing of its own to compile. It is a zip on the [C++ SDK release](https://github.com/ikon-ai/ikon-releases/releases/tag/cpp-sdk), which holds only the newest version, and the zip is a CMake package with two targets:

- `ikon::sdk`: the SDK's headers, C++17 and threads. A program that brings its own network and HTTP clients needs only this.
- `ikon::sdk_examples`: the SDK and the example implementations in its folder (`ExampleLog`, `ExampleHttpClient`, `ExampleNetworkClient`), linked with OpenSSL. On Windows `ExampleHttpClient` sends through WinHTTP, except for [post-quantum key exchange](#post-quantum-key-exchange); elsewhere it sends over OpenSSL. The target exists only when CMake finds OpenSSL.

A program that adds the SDK's source folder with `add_subdirectory` or `FetchContent` gets a generated `ikon_version.h` that names the version `v0.0.0-source`. The platform accepts it, so connects work; only a release zip carries a real version.

Both targets add `/bigobj` on MSVC, because a Debug build of the SDK's headers has more sections than an object file holds by default. The minimal `CMakeLists.txt` below downloads the SDK with `FetchContent`, from an address that always serves the newest version:

```cmake
cmake_minimum_required(VERSION 3.18)
project(my_client CXX)

include(FetchContent)
FetchContent_Declare(ikon_sdk
  URL https://github.com/ikon-ai/ikon-releases/releases/download/cpp-sdk/ikon-sdk-cpp.zip
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

Firmware adds the SDK's folder to its include path in whatever build system its toolchain uses; [Microcontrollers](#microcontrollers) says what else it needs.

## Quick Start

<!-- ikon-example: cpp-sdk-quick-start -->
```cpp
#include "ikon_sdk.h"
#include "example_http_client.h"
#include "example_logger.h"
#include "example_network_client.h"

#include <iostream>

using namespace ikon;

int main()
{
    // The machine's TLS connections, HTTPS requests, clock and log
    Log log = ExampleLog();
    ExampleNetworkClient network(log);
    ExampleHttpClient http;
    StdSystem system;

    ApiKeyConfig apiKey;
    apiKey.apiKey = "ikon-xxxxx";                  // From ikon api-key create
    apiKey.spaceId = "your-app-id";
    apiKey.externalUserId = "user-123";
    apiKey.backendType = BackendType::Production;  // The default; Development for an app on *.dev.ikonai.app

    ClientConfig config;
    config.apiKey = apiKey;
    config.description = "My App";

    IkonClient client(config, network, http, system, log);

    // Every message the client receives, read where it arrived
    client.OnAny([](const MessageView& message)
    {
        std::cout << "Received a message with opcode " << message.OpcodeValue() << std::endl;
    });

    if (Error error = client.Connect())
    {
        std::cerr << "Could not connect: " << error.message << std::endl;
        return 1;
    }

    std::cout << "Connected as session " << client.SessionId() << std::endl;

    // Messages are read only inside Poll, which also runs the handlers and reconnects after a drop
    uint64_t end = system.NowMs() + 10000;

    while (system.NowMs() < end)
    {
        client.Poll(100);
    }

    client.Disconnect();
    return 0;
}
```

`ikon_sdk.h` includes the client and every add-on. A program that wants less includes `ikon_client.h` and the add-on headers it uses.

## How the Client Runs

The client does nothing between calls. `Connect()` signs in and joins on the calling thread, and returns when the client is connected or has failed. After that, everything the client does happens inside `Poll(timeoutMs)`: it reads what the server sent, runs the message handlers, the callbacks and the add-ons, keeps the connection alive and reconnects after a drop. `Poll` waits up to `timeoutMs` for input, and an add-on can make it wait less, so a loop that calls it with 50 or 100 ms keeps the client current. A program that stops calling `Poll` stops receiving, and the server drops a connection it hears nothing from.

A device with one thread runs the whole connection in its own loop:

<!-- ikon-example: cpp-sdk-poll-loop -->
```cpp
// The device's whole connection, on the one thread it has. Returns once the client gives up: its
// reconnect attempts ran out, or the server refused it for good. The caller may wait and call it again
void RunDevice(const ClientConfig& config, INetwork& network, IHttp& http, ISystem& system, Log log)
{
    IkonClient client(config, network, http, system, log);

    // Add-ons are made before Connect(). Declared after the client, they are destroyed before it
    Functions functions(client);

    FunctionDefinition readTemperature;
    readTemperature.name = "ReadTemperature";
    readTemperature.description = "The room's temperature in degrees Celsius";
    readTemperature.resultTypeName = "System.Double";
    readTemperature.visibility = FunctionVisibility::Shared;

    // Runs inside Poll when the app calls it
    functions.Register(readTemperature, [](FunctionCall& call)
    {
        call.Return(ReadTemperatureSensor());
    });

    if (Error error = client.Connect())
    {
        IKON_ERROR(log, "Could not connect: ", error.message);
        return;
    }

    while (client.GetState() != ConnectionState::Offline)
    {
        // Reads what arrived, runs the handlers and the add-ons, and reconnects after a drop. Waits up to
        // 50 ms for input
        client.Poll(50);

        // The device's own work goes here, between polls
    }
}
```

`Offline` means the client gave up: its reconnect attempts ran out, or the server refused it in a way no retry changes. Another `Connect()` starts again.

A program with threads can let the client poll on a thread of its own instead. `StartPolling(timeoutMs)` starts it, with 100 ms by default, and `StopPolling()` or the client's destructor stops it:

<!-- ikon-example: cpp-sdk-start-polling -->
```cpp
if (Error error = client.Connect())
{
    std::cerr << "Could not connect: " << error.message << std::endl;
    return;
}

// From now on a thread of the client's own calls Poll, until StopPolling() or the client's destructor
client.StartPolling();

// So this thread may wait for an answer: the polling thread reads it
Functions::Value sum;

if (Error error = functions.CallAndWait("Add", FunctionArgs{5, 3}, &sum, 10000))
{
    std::cerr << "Add failed: " << error.message << std::endl;
}
else
{
    std::cout << "5 + 3 = " << sum.Parse().AsInt() << std::endl;
}

client.StopPolling();
```

### Threading Rules

- The thread inside `Connect()` or `Poll()` is the client's driving thread, and `IsDrivingThread()` tells code whether it is on it. Every handler, callback and add-on hook runs there, except:
  - `Disconnect()` runs `StateChanged`, the add-ons' `OnStateChanged` and the callbacks of what it ends (a function call, a UI action, an upload, an incoming stream's end) on its caller's thread.
  - A failure known at once, such as a `Functions::Call` made while not connected, runs its callback before the call returns, on the calling thread; `Functions::Cancel` runs the cancelled call's callback on the thread that cancels.
  - `Reactives::Subscribe` delivers a value the client already follows before it returns, on the calling thread.
- Only one thread drives the client: call `Poll` from one thread, or use `StartPolling`, never both.
- Never wait on the driving thread for something the server sends: only that thread reads it, so the wait would never end. There, `Functions::CallAndWait`, `IkonUI::CallActionAndWait` and `Uploads::UploadFile` fail at once, and `IkonUI::WaitForNode` and `Functions::WaitForFunction` only look, without waiting. Use the forms that take a callback instead (`Call`, `CallAction`, `StartUpload`), which work on any thread.
- `Send`, `Disconnect`, `Functions::Call`, `Uploads::StartUpload` and `Media`'s send methods are safe from any thread.
- A handler should return quickly: while it runs, nothing else of the client runs.
- With `IKON_NO_THREADS` defined, the waiting calls and `StartPolling` do not exist, and the SDK's locks compile to nothing.

## Errors

The SDK throws nothing. A call that can fail returns an `Error`, which converts to `true` when something went wrong:

<!-- example-skip: the error check every example uses -->
```cpp
if (Error error = client.Connect())
{
    std::cerr << ToString(error.code) << ": " << error.message << std::endl;
}
```

An `Error` has these fields:

- `code`: an `ErrorCode`.
- `message`: a short reason a person can read.
- `detail`: often empty. For a refused or failed HTTP request it holds the request, its request id and the backend's answer, for a log. For a few codes it holds a value: the refusal marker of `ServerChanged`, the oldest accepted version of the upgrade codes, the app of `WrongLocalApp`, the error type of `FunctionFailed`.
- `httpStatus`: the HTTP status behind the error, or 0.
- `retryAfterSeconds`: the wait the backend asked for, or 0.

`IsFinal()` is true for a failure that the same configuration will meet again, for `NoLiveSession` and for `Cancelled`, so a reconnect stops at it.

| Code | What happened | What to do |
|---|---|---|
| `InvalidConfig` | The configuration is incomplete or contradicts itself, or `Connect()` was called while the client was connecting, connected or reconnecting | Fix what `message` names |
| `ServerUnreachable` | No server answered: the network is down, the host does not resolve or the port refuses. It says nothing about the credentials | Try again later |
| `AuthRefused` | The backend or the guest sign-in refused the credential or the user (401 or 403), or a server refused the token of a connect URL | A new credential, or a change in the app |
| `ServerFull` | The app is at its client limit on every server the backend may place the client on. A local run or a connect URL has one server, so it fails at the first full answer | Try again later |
| `ServerChanged` | The server refused the connect token for good: it expired, or it was for a server that has since been replaced | Connect again |
| `JoinClosed` | The connection to the app's server closed during the join, at any point before the client reported ready | Connect again |
| `JoinTimeout` | The session state or the app's functions did not arrive within `timeouts.joinTimeoutMs` | Connect again |
| `NoLiveSession` | A `joinOnly` client found no running session | Wait `retryAfterSeconds`, then connect again |
| `SdkUpgradeRequired` | The platform no longer accepts this version of the C++ SDK | Update the SDK and rebuild |
| `AppUpgradeRequired` | The platform no longer runs the app's server, built on IKON libraries it no longer accepts | The app's developer runs `ikon update` and deploys again |
| `SpaceNotFound` | The backend answered 404 or 410: the app is suspended, unpublished, removed or not deployed, or it is on the other platform (`backendType`) | Check the app |
| `RateLimited` | The backend asked the client to slow down (429) | Wait `retryAfterSeconds` |
| `WrongLocalApp` | The local run on the configured port runs another app than `LocalConfig::spaceId` | Use the port of the right run |
| `PostQuantumUnsupported` | `requirePostQuantum` is set, but the HTTP or network interface cannot honour it | See [Post-Quantum Key Exchange](#post-quantum-key-exchange) |
| `ProtocolError` | The server's answer to the join cannot be read, or a function's result or stream item is not JSON. A message that is not one, or one larger than `maxMessageSize`, closes the connection instead: a join fails with `JoinClosed`, and a joined client reconnects as after any drop | |
| `Cancelled` | `Disconnect()` or the program ended the operation | |
| `NotConnected` | The client is not connected, so the operation was not sent: a function call or a subscription made while the client is idle or offline (one made while it connects waits), or a UI action called while it is not `Connected` | Connect first |
| `Disconnected` | The connection dropped before the call could be sent or was answered, or the session that runs the function left before it answered | Try again once the client is ready |
| `FunctionNotFound` | No function of that name (and version) is registered in the session | |
| `FunctionFailed` | The function ran and failed: `message` is its error, `detail` its error type | |
| `Timeout` | No answer came within the time the caller allowed | |
| `Failed` | Anything else; `message` says what | |

`ToString(ErrorCode)` gives a code's name.

## Authentication Modes

`ClientConfig` takes exactly one of five ways to connect: `apiKey`, `local`, `guest`, `userToken` or `externalConnectUrl`. `Connect()` fails with `InvalidConfig` when none or more than one is set.

`config.parameters` are the app's client parameters, which a browser carries in its URL. A program has no URL, so it picks the app instance here: `config.parameters = {{"room", "harbour"}};` joins the instance a browser joins at `?room=harbour`, for an app whose `SessionIdentity` has a `Room` field (the name's case does not matter). The name is the property name of the app's client-parameters record, as the URL query spells it: an app that reads `?guest=anna` takes `{"guest", "anna"}`. Without it the client joins the instance with that field empty.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once. `ikon api-key create --app-id <id> --format json --output key.json` works from any folder and writes the key to `key.json` as its `apiKey` field, beside `spaceId`. The app's surface needs no `guest` or `global` entry in its `[Surface.Auth] Methods` for a key. A key belongs to one platform: an app on `*.dev.ikonai.app` needs `BackendType::Development`, and the default `Production` backend refuses its key with a 403 `Invalid key` (or a 404) whose error names the other backend to try.

<!-- ikon-example: cpp-sdk-auth-api-key -->
```cpp
ApiKeyConfig apiKey;
apiKey.apiKey = "ikon-xxxxx";                  // From ikon api-key create
apiKey.spaceId = "...";                        // The app's id (ikon app list)
apiKey.externalUserId = "user-123";            // Your own id for the user
apiKey.sessionId = "session-xyz";              // Optional: join a precomputed session
apiKey.backendType = BackendType::Production;  // The default; Development for an app on *.dev.ikonai.app
apiKey.userType = UserType::Human;
apiKey.clientType = ClientType::DesktopApp;

ClientConfig config;
config.apiKey = apiKey;
```

`UserType` is `Human` (the default) or `Machine` (`Unknown` is the unset value); a program that no person sits behind sets `Machine`. `ClientConfig::productId` (default `"ikon-sdk-cpp"`) and `description` (default `"Ikon SDK C++"`) name the program to the platform; the defaults say only that an SDK client joined, so set them to something that says what the program is. `ClientType` is `MobileWeb`, `MobileApp`, `DesktopWeb` or `DesktopApp`, which the app reads from the client's context.

Set `joinOnly = true` on the `ApiKeyConfig` for a client that should join the app only while someone else has it running, and never start it: a device or agent that stays connected around the clock would otherwise keep a server running, billed to the app's organisation. When nothing is running, `Connect()` fails with `NoLiveSession`, and `retryAfterSeconds` is how long the backend asks you to wait before trying again (0 when it named no wait). If the server shuts down, the next reconnect reports that through `ErrorOccurred` and goes `Offline` instead of retrying. Pair `joinOnly` with `UserType::Machine`, so the connection does not count as a person and does not keep the session open either. A `Human` client, the default, holds the session open: a `joinOnly` client left on the default keeps the app's server running.

While the app's server is starting, `Connect()` keeps asking it for up to `timeouts.connectTimeoutMs`. When the server refuses the connect token because it was issued for a server that has since been replaced, or because it has expired, `Connect()` asks the backend for a new token once, and fails with `ServerChanged` if that one is refused too. The API key is still valid, so call `Connect()` again later. When a server answers that it is full, `Connect()` asks the backend up to three more times, naming the full servers so a sharded app places the client on another shard, and then fails with `ServerFull`. When no server answers at all, it fails with `ServerUnreachable`. A server that answers with an error status never causes `ServerUnreachable`. The example HTTP client reports an unreachable server as status 0, and an `IHttp` of your own does the same with a status of 0 or less. The example client also returns 0 for a request it could not finish, such as one whose certificate did not verify, that timed out or whose response was cut off, so those fail with `ServerUnreachable` too.

A deleted API key (`ikon api-key delete <id> --app-id <app-id> --yes`, or without `--app-id` in the app's folder) is refused from the next sign-in on: a new connect, or a reconnect that has to sign in again, fails with `AuthRefused`. Its `message` is a short reason with the backend's request id (`The API key was refused (403 Invalid key; request <id>)`), and its `detail` holds the request and the backend's whole answer. Deleting a key does not end any session: a client the key already connected keeps going, and a reconnect that resumes the same session needs no sign-in.

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: cpp-sdk-auth-local -->
```cpp
LocalConfig local;
local.host = "localhost";
local.httpsPort = 8443;         // The port of the server address the app's ikon run printed
local.spaceId = "your-app-id";  // Optional: refuse a server that runs another app
local.userId = "dev-user";

ClientConfig config;
config.local = local;
```

The port can change between runs. A run takes the port the project's last run used while it is free, else 8443 or the next free port, so two apps run at once listen on different ports, and a run can stay on 8444 after 8443 frees up. Use the port of the server address in the line `ikon run` prints when the app is ready: `Ready: https://localhost:8443`, or the `server` address in brackets after the frontend's. It is also the `serverUrl` in the app's `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`.

A connect to a port that another app's run holds joins that other app, and that app gets everything the program sends. Set `spaceId` to the app's id (`[Target] SpaceId` in the app's `ikon-config.development.toml`) to prevent that: when the server runs another app, `Connect()` fails with `WrongLocalApp` naming both app ids, and every reconnect fails the same way. Without `spaceId` the client joins whatever app the port serves. A client keeps the port it was made with, so after the app's next `ikon run` on another port, make a new `IkonClient` with the new port.

A program on the same machine as the run can read the port and the app id from the app's folder with `LocalConfigFromAppFolder`, from `ikon_local_run.h`:

<!-- ikon-example: cpp-sdk-local-run -->
```cpp
// The app's folder, where ikon run runs it. Read it again before each new client: the port can change
LocalConfig local;

if (Error error = LocalConfigFromAppFolder("../my-app", local))
{
    std::cerr << error.message << std::endl;
    return error;
}

local.userId = "dev-user";
config.local = local;
```

It takes the host and port from `serverUrl` in the `ikon-server-info.json` the run writes when it starts (the newest one under `build/app/artifacts/bin` when there are several) and `spaceId` from `[Target] SpaceId`. It fails with `InvalidConfig` when the folder has no `ikon-config.development.toml` or the app has not been run there. It needs `std::filesystem`; `LocalConfigFromRunFiles` takes the two files' text instead, for a build without it (`IKON_NO_FILESYSTEM`).

`localClientProof` matters only for a local connect that does not look local to the run, such as one through `ikon run --host-public`. Without it, such a connect joins as a guest, or is refused where the app admits no guests. With the run's `localClientProof` from its `ikon-server-info.json`, which only a program on the run's machine can read, the run keeps `userId`. `LocalConfigFromAppFolder` fills it in. A program on the same machine connecting to `localhost` needs none.

### Guests

A client can join a deployed app as a signed-out visitor, as a browser that has not signed in does. It needs no key.

<!-- ikon-example: cpp-sdk-auth-guest -->
```cpp
GuestConfig guest;
guest.spaceId = "...";  // The app's id (ikon app list)
guest.backendType = BackendType::Production;

ClientConfig config;
config.guest = guest;
config.deviceId = "device-1234";  // Keep it to come back as the same guest
```

Each device id gets a guest user of its own, unless the app's guest sign-in puts every guest in one shared user. Without `deviceId` the client picks a random one, which it keeps until it is destroyed. The app sees a guest as `UserType::Human`. When the app lets no guests in, `Connect()` fails with `AuthRefused` and `httpStatus` 403.

A client enters one of the app's surfaces. `ClientConfig::surface` names it, by its name in the app's `ikon-config.toml`; left empty, the client enters the only surface that is not a sign-in page, else the one with neither `Routes` nor `Host`, else the first declared. That surface must list `guest` or `global` in its `[Surface.Auth] Methods`, or `Connect()` fails with `AuthRefused` and `httpStatus` 403, even when another surface admits guests. A surface with no `[Surface.Auth]` keeps the default methods, which include `guest`; `ikon new --private` writes `email` and `passkey` only. `ClientConfig::initialPath` is the path the client opens, such as `/orders/42`, which the app reads from `Context.InitialPath`. Both apply in every mode:

<!-- example-skip: two fields of the config above -->
```cpp
config.surface = "kiosk";                 // A surface whose Methods list guest
config.initialPath = "/menu";
```

### User Tokens

A program that already holds a person's session token for the app, issued by another service, connects with it instead of an API key. `backendUrl` is the backend that issued the token: `ProductionBackendUrl` (`https://api.prod.ikon.live`) for an app on `*.ikonai.app`, `DevelopmentBackendUrl` (`https://api.dev.ikon.live`) for one on `*.dev.ikonai.app`.

An API key's `externalUserId` names a user of the program's own: the same id is the same user on every connect, but it is never one of the app's existing people and has no email, so a role the app gives by email does not reach it. `ikon profile role add <role> --external-user <id>`, run in the app's folder, gives it one, even before its first connect. A surface that lists `Roles`, as every surface of an `ikon new --private` app does, refuses it until then. Acting as an existing person needs that person's session token in this mode, and the IKON tool hands out none; only the .NET SDK's `UserLogin` connects as the person signed in with `ikon login`.

<!-- ikon-example: cpp-sdk-auth-user-token -->
```cpp
UserTokenConfig userToken;
userToken.spaceId = "...";  // The app's id (ikon app list)
userToken.token = "...";    // The person's session token, from the service that issued it
userToken.backendUrl = ProductionBackendUrl;
userToken.userType = UserType::Human;

ClientConfig config;
config.userToken = userToken;
```

The client cannot renew the token. When the backend refuses it (401) or the app refuses the user (403), `Connect()` fails with `AuthRefused`. During a reconnect, the client stops reconnecting at once, reports the refusal through `ErrorOccurred` and goes `Offline`. Get a new token from the service that issued the old one, pass it to `IkonClient::UpdateAuthToken`, and call `Connect()` again. Every later connect and reconnect uses the new token. `UpdateAuthToken` returns `InvalidConfig` on a client configured without `userToken`.

### Connect URLs

A client can also join through a server's connect URL (`https://<server>/connect?token=...`) that a service it trusts created for it. It does not sign in at all.

<!-- ikon-example: cpp-sdk-auth-connect-url -->
```cpp
ClientConfig config;
config.externalConnectUrl = "https://server.example/connect?token=...";
```

When the server refuses the URL's token, `Connect()` fails at once: with `ServerChanged` for an expired token or one signed for another server, and with `AuthRefused` for any other 401 or 403. A reconnect into a new session uses the same URL, so once its token has expired the server refuses it, and the client reports that through `ErrorOccurred` and goes `Offline`. Make a new `IkonClient` with a new URL.

## Connection Lifecycle

### Connection States

`GetState()` returns the client's state:

| State | Description |
|-------|-------------|
| `Idle` | Not connected: the initial state, and the state after `Disconnect()` |
| `Connecting` | Inside `Connect()`: signing in and joining |
| `Connected` | Joined and ready; `Ready` runs right after |
| `Reconnecting` | The connection dropped, and `Poll` is reconnecting |
| `Offline` | The connect failed, or the client gave up reconnecting |

### Callbacks

<!-- ikon-example: cpp-sdk-callbacks -->
```cpp
client.StateChanged = [](ConnectionState state)
{
    std::cout << "State: " << ToString(state) << std::endl;
};

// After Connect() and after every reconnect, once the client is Connected: the app now knows this
// client's user, and the add-ons hold the app's functions and UI
client.Ready = []()
{
    std::cout << "Ready" << std::endl;
};

// The connection dropped, and the client reconnects unless timeouts.maxReconnectAttempts is 0.
// Disconnect() does not call it
client.Disconnected = []()
{
    std::cout << "Disconnected" << std::endl;
};

// A connect or reconnect attempt failed
client.ErrorOccurred = [](const Error& error)
{
    std::cerr << "Error " << ToString(error.code) << ": " << error.message << std::endl;
};
```

The callbacks run on the driving thread: inside `Connect()` for a connect, inside `Poll()` for everything after, except that `Disconnect()` runs `StateChanged` on its caller's thread (see [Threading Rules](#threading-rules)). `StateChanged` runs after each change of state. `Ready` runs once the client is `Connected`, after each connect and each reconnect; the app knows the client's user from then on, and `Functions::Call` and `Reactives::Subscribe` go out at once. `Disconnected` runs when the connection drops without the program asking, never for `Disconnect()`.

A failed `Connect()` reports its error twice, with the same message: first through `ErrorOccurred`, then as the `Error` it returns. Show the returned error for a connect your code called, and keep `ErrorOccurred` for failures that no call returns, such as a reconnect attempt that fails. `ErrorOccurred` runs for every failed reconnect attempt, not only the last, while the client stays `Reconnecting`; the client has given up when `StateChanged` reports `Offline`. `Disconnect()` during a connect makes `Connect()` return `Cancelled`, which `ErrorOccurred` does not report.

### Connecting and Disconnecting

<!-- ikon-example: cpp-sdk-connect-disconnect -->
```cpp
if (Error error = client.Connect())
{
    std::cerr << "Could not connect: " << error.message << std::endl;
    return;
}

// Poll until a client of the product my-product is ready in the session, for up to 30 s
uint64_t deadline = client.System().NowMs() + 30000;

while (!client.HasReadyClient("my-product") && client.System().NowMs() < deadline)
{
    client.Poll(100);
}

// Tells the server this client is leaving, so it ends the session at once, and closes the connection
client.Disconnect();
```

Once the connection is open, `Connect()` waits for the server to send the session state and the app's functions, up to `timeouts.joinTimeoutMs` (30 seconds). If they do not arrive, it closes the connection and fails with `JoinTimeout`, and the client goes `Offline` without running `Ready`; call `Connect()` again to retry. A reconnect attempt that runs out of this time counts as a failed attempt.

`Disconnect()` tells the server that the client is leaving, so the server ends the session at once instead of holding it for a reconnect, and closes the connection. It does not wait for the server, only for a connect or reconnect attempt running on another thread to stop. It is safe from any thread, and stops a connect or reconnect in progress; the client goes to `Idle`. The destructor calls it.

A program whose output goes to a file or a pipe (a log, a CI step) has its `std::cout` buffered, so lines that show a join in progress can appear long after it happened. Set `std::cout.setf(std::ios::unitbuf);` at the start of `main`, or end lines with `std::endl`, as the examples do.

### Accessing Client State

<!-- ikon-example: cpp-sdk-client-state -->
```cpp
const ClientConfig& config = client.Config();
std::cout << config.productId << " is " << ToString(client.GetState()) << " as session " << client.SessionId() << std::endl;

// The session state the server sent last, read in place. The client keeps it from changing until
// the callback returns. WithGlobalState returns false before the first join
bool hasState = client.WithGlobalState([](const GlobalState::View& state)
{
    for (const auto& entry : state.Clients)
    {
        std::cout << "Client " << entry.first << " of user " << entry.second.UserId << std::endl;
    }
});

// A copy to keep, decoded into the owning type
std::vector<uint8_t> bytes = client.GlobalStateBytes();
GlobalState copy;

if (hasState && TeleportDecode(TeleportBytes(bytes), copy))
{
    std::cout << "Clients in the session: " << copy.Clients.size() << std::endl;
}
```

`WithGlobalState` reads the session state in place and holds the client's lock while the callback runs. The view is valid only inside the callback, and the callback must not call the client. `GlobalStateBytes()` copies it, and is empty before the first join and, with `lowMemory`, after each join. `HasReadyClient(productId, userId)` says whether a ready client of that product, and of that user when one is given, is in the session.

### Reconnection

When a connection the client joined drops, `Poll` reconnects. It waits `reconnectBackoffMs` before the first attempt and twice as long before each next one, each wait shortened at random by up to a quarter so that the clients one server dropped do not all come back at once. When the backend answers an attempt with `RateLimited`, the next attempt waits at least `retryAfterSeconds`, capped at 5 minutes, plus up to half as long again. After `maxReconnectAttempts` failed attempts the client goes `Offline`, and only a new `Connect()` connects it again. The client stops reconnecting at once and goes `Offline` when retrying cannot help: on any error whose `IsFinal()` is true (a `joinOnly` client finds no live session, the platform refuses the SDK or the app, the app is not available, the credential or the user is refused), and when a server refuses the token in an `externalConnectUrl`.

Each attempt first tries to resume the session the client had. It opens a new connection to the same server address with that session's auth ticket and does not ask for a new session, so the app still has the client's session and what it keeps for it. The server holds a dropped session for its soft-retry window, which it sends in `AuthResponse.SoftRetryWindowMs` and which is 5 minutes by default. The attempt starts a new session instead, as `Connect()` does, when the window has passed, or when the server closes the connection before the client has joined, which is how it refuses a session it no longer holds. The server then ends the old session. Once the server has answered a request for a new session, the later attempts start new sessions too.

An attempt that cannot open a connection at all, as while the network is down, costs the server nothing. During the first half of the attempts, the next attempt tries to resume the session again. From the second half on, such an attempt also asks for a new session, so a server that has gone away is replaced. A request for a new session that reaches nothing leaves the old session held, and the next attempt tries to resume it again. With the default 6 attempts and 500 ms backoff, the attempts run at most about 0.5, 1.5, 3.5, 7.5, 15.5 and 31.5 seconds after the drop, and each of them tries to resume first, so a client whose network comes back before the last attempt, about 30 seconds after the drop, returns to the session it had.

A resumed join ends the same way as a new one: the add-ons announce what they must (such as this client's functions), the client reports ready, and `Ready` runs.

To test the program's own recovery, call `IkonClient::DebugDropConnection()`. It closes the live connection as a network failure does, without telling the server the client is leaving, and the client goes `Reconnecting` and back to `Connected` as after any drop. `Send` delivers at most once: a message sent while the connection is down is not sent (`Send` returns false), and one sent just before the drop may or may not have arrived, so resend what the app needs after `Ready` runs again.

## Messages

### The App's Own Messages

An app declares its own messages in `schema/*.tp`. `ikon schema` compiles them into one header per message, in namespace `ikon::app`: into `frontend-cpp/` when the app has one, and into every folder `schema/outputs.toml` names for `cpp`, such as a program in another repository (see `teleport-schema-spec.md`). A header includes `ikon_teleport.h`, so put the SDK's folder on the include path. If you cannot change the app, run `ikon teleport generate --input <app>/schema --type cpp --output <folder>/generated/protocol` in the program's own repository instead. It writes the same headers with the same opcodes and changes nothing in the app.

`Send` sends one, and `On<T>` calls a handler with every message of type `T` the client receives, with the sender's session id. The opcode, payload version and flags come from the type, so the program never names them. The handler lasts across reconnects until `Off`; add it before `Connect()` to see the first message the app sends once the client is ready. A message that does not decode is logged as a warning and skipped.

<!-- ikon-example: cpp-sdk-app-messages -->
```cpp
// SdkTypedPing and SdkTypedPong are the app's own messages, from its schema/*.tp, generated into
// headers in namespace ikon::app. Add the handler before Connect() so it sees the first message
// the app sends.
IkonClient::HandlerId handler = client.On<app::SdkTypedPong>([](const app::SdkTypedPong::View& pong, const MessageView& message)
{
    std::cout << "Pong " << pong.Sequence << " from " << message.SenderId() << ": " << pong.Text << std::endl;
});

if (Error error = client.Connect())
{
    std::cerr << "Could not connect: " << error.message << std::endl;
    return handler;
}

app::SdkTypedPing ping;
ping.Sequence = 1;
ping.Text = "hello";
client.Send(ping);

// client.Off(handler) removes the handler
```

### Views and Decoding

A handler gets the message as a `View`: its fields read in place from the received bytes, its strings as `std::string_view` and its lists and maps as views too. Nothing is copied, so receiving costs no memory beyond the receive buffer, and the view is valid only while the handler runs. To keep a message, decode it into its owning type, whose fields are `std::string` and `std::vector`:

<!-- ikon-example: cpp-sdk-message-views -->
```cpp
// A View reads the message where it arrived, copying nothing: its strings are std::string_view
// into the received bytes, valid only while the handler runs
client.On<ActionOpenExternalUrl>([](const ActionOpenExternalUrl::View& view, const MessageView& message)
{
    std::cout << "Session " << message.SenderId() << " asks to open " << view.Url << std::endl;
});

// Decode copies every string and collection into the owning type, which outlives the handler
client.OnAny([&kept](const MessageView& message)
{
    ActionOpenExternalUrl url;

    if (message.Decode(url))
    {
        kept.push_back(std::move(url));
    }
});
```

`On(opcode, handler)` and `OnAny(handler)` give the raw `MessageView`, which `Read<T>` reads as a view and `Decode<T>` copies into an owning type. `OnAny` sees every message, after the add-ons have taken theirs. `Off(handlerId)` removes a handler, also from inside one.

### Platform Messages

`Send` takes the platform's own generated messages from `ikon_protocol.h` the same way.

<!-- ikon-example: cpp-sdk-send-typed -->
```cpp
// A generated message of the platform's, from protocol/action_custom_user_message.h
ActionCustomUserMessage payload;
payload.TypeName = "ChatMessage";
payload.JsonPayload = R"({"text":"Hello"})";

// False when it did not go out: not connected, or the connection failed
bool isSent = client.Send(payload);
```

### Messages Before Ready

The app learns which user a client belongs to only when the client reports ready, which it does as soon as it has joined, before `Ready` runs. A message sent while the client is joining, from another thread, waits and goes out right after the client reports ready, in the order it was sent. At most 4096 messages wait; any sent after that are dropped, with one warning in the log. Audio and video sent then are dropped at once, because they would play late. If the connection drops before the client is ready, the waiting messages are dropped too. While the client is `Reconnecting`, `Idle` or `Offline`, `Send` sends nothing and returns false.

## Add-Ons

What a program does beyond messages comes from add-ons, each in its own header. A program makes each one with the client, before `Connect()`, and only for what it uses: an add-on the program never makes is not linked into it. Each one attaches itself to the client and takes itself off again in its destructor. Declare add-ons after the client, so they are destroyed before it, and `Reactives` after the `Functions` it uses.

<!-- example-skip: the add-ons' constructors -->
```cpp
IkonClient client(config, network, http, system, log);
Functions functions(client);
Reactives reactives(client, functions);
IkonUI ui(client);
Uploads uploads(client);
Media media(client);
```

### Shared Functions

`Functions` (`ikon_functions.h`) registers functions for the app to call and calls the functions the app registers; the server offers a client's functions to the app only, not to other clients. Each join waits until the client holds the app's functions, so they can be called from `Ready` and after `Connect()` returns; a join that does not get them fails with `JoinTimeout`. Register `Shared` functions before `Connect()`, as below. They are announced on every join, reconnects included, before the client reports ready, so the app has them when it first sees the client. One registered while connected is announced at once. A `Local` function is not announced: only this program calls it.

<!-- ikon-example: cpp-sdk-shared-functions -->
```cpp
// A function the app can call. Registered before Connect(), it is announced on every join, before
// the client reports ready
FunctionDefinition echo;
echo.name = "Echo";
echo.description = "Echoes back the first argument";
echo.parameters = {{"value", "System.String", "The value to echo back"}};
echo.resultTypeName = "System.String";
echo.visibility = FunctionVisibility::Shared;

// Runs on the thread that polls the client. A handler that returns without answering returns null
functions.Register(echo, [](FunctionCall& call)
{
    call.Return(call.Arg(0));
});

if (Error error = client.Connect())
{
    std::cerr << "Could not connect: " << error.message << std::endl;
    return;
}

// Calls a function of the app. The callback runs once, on the polling thread, with the result or the error
functions.Call("Add", FunctionArgs{5, 3}, [](const Error& error, const FunctionResult& result)
{
    if (error)
    {
        std::cerr << "Add failed: " << error.message << std::endl;
        return;
    }

    std::cout << "5 + 3 = " << result.Value().AsInt() << std::endl;
});
```

`Call` returns at once, and its callback runs once, on the driving thread, with the result or the error. A call made while the client is connecting or reconnecting waits until the client is ready, then goes out to the functions that join brought, in the order the calls were made; at most `maxWaitingCalls` (1024) wait, and more fail at once. When the client is not trying to connect (before `Connect()`, after `Disconnect()`, or offline after its reconnects ran out), a call fails at once with `NotConnected`, before `Call` returns, and a waiting call fails the same way when the client stops trying. A name the app has not registered fails with `FunctionNotFound`. A call already sent when the connection drops, or when the program calls `Disconnect()`, gets no answer, so it fails then with `Disconnected`. `Call` has no time limit of its own; `Cancel` ends a call the program no longer wants. `CallOptions::version` asks for the registration that fits the version the program was built against.

The app's functions belong to the app's own session, and a local run replaces that session each time the app hot reloads. When a session leaves, its functions leave too, and a call it has not answered fails at once with `Disconnected`. The new session registers the functions again moments later, and calls by name go to it. A call made between the two fails with `FunctionNotFound`, so after a hot reload wait for the function (`FunctionRegistered`, or `WaitForFunction` from a thread that is not driving) before calling again.

When the app calls a function this client registered, the handler runs on the driving thread with a `FunctionCall`. `Arg(i)` is an argument parsed as JSON, `ArgJson(i)` its text and `ArgData(i)` the bytes of a `byte[]` argument; all of them are valid only while the handler runs. The handler answers with `Return`, `ReturnJson`, `ReturnBytes` or `Fail`; one that returns without answering answers null. A handler that cannot answer before it returns, such as one that first calls the app itself, calls `Defer()` and answers later through the `FunctionReply` it returns, from any thread.

`CallAndWait` waits for the result on the calling thread, which must not be the driving thread (see [Threading Rules](#threading-rules)). A call that times out is cancelled, on the app too when it was sent.

Additional members: `HasFunction`, `FunctionNames`, `Unregister`, `RegisterStream`, `CallStream`, `Cancel`, and the `FunctionRegistered` and `FunctionUnregistered` callbacks. `ApprovalRequested` runs when the app asks this client's user to approve a call; answer with `RespondToApproval`.

#### Streams, Cancellation and Binary Values

<!-- ikon-example: cpp-sdk-function-streams -->
```cpp
// A function that returns a stream (IAsyncEnumerable<int> in a C# app). onItem gets each item as
// the app produces it, and returning false from it stops the stream on the app too
functions.CallStream("CountTo", FunctionArgs{3},
    [](const FunctionResult& item)
    {
        std::cout << "Item: " << item.Json() << std::endl;
        return true;
    },
    [](const Error& error)
    {
        std::cout << (error ? "Stream failed: " + error.message : "Stream ended") << std::endl;
    });

// Cancel a call: the app's function is cancelled, and the callback gets Cancelled
TeleportGuid slow = functions.Call("SlowReport", {}, [](const Error& error, const FunctionResult&)
{
    std::cout << "SlowReport: " << ToString(error.code) << std::endl;
});
functions.Cancel(slow);

// A byte[] argument goes in with AddBytes, and a byte[] result arrives as Data()
functions.Call("Checksum", FunctionArgs().AddBytes({1, 2, 3}), [](const Error& error, const FunctionResult& result)
{
    if (!error && result.IsBinary())
    {
        std::cout << "Got " << result.Data().size << " bytes" << std::endl;
    }
});
```

A value the app sends arrives as the JSON of the C# value. A record or class keeps its C# property names, which are PascalCase (`{ "Name": ..., "Score": ... }`), so read it with `value["Name"]`. A list arrives as a JSON array and a dictionary as a JSON object keyed by the dictionary's keys. A `FunctionResult` is read in place and is valid only during the callback: `Json()` is the JSON text, `Value()` the JSON parsed, and `Data()` the bytes of a `byte[]` value.

A stream's `onEnd` runs once, with no error after the last item or with why it failed: the app's function failed, the connection dropped or the client disconnected. Returning false from `onItem` stops the stream on the app too. Calling a function that returns a single value with `CallStream` ends the stream without an item. `Cancel` returns false when the call had already completed. A handler this client registers takes a `byte[]` argument through `ArgData`, and `ReturnBytes` reaches the app as `byte[]`.

#### Stream Functions on the Client

A client can register a function that returns a stream too, which a C# app reads as an `IAsyncEnumerable`:

<!-- ikon-example: cpp-sdk-client-streams -->
```cpp
struct Countdown
{
    FunctionReply reply;
    int64_t next = 0;
    uint64_t dueMs = 0;
};

std::vector<Countdown> countdowns;

// A function the app reads as an IAsyncEnumerable<int>, counting down one number a second
void RegisterCountdown(Functions& functions)
{
    FunctionDefinition countdown;
    countdown.name = "Countdown";
    countdown.description = "Counts down from a number, one a second";
    countdown.parameters = {{"from", "System.Int32", "Where to start"}};
    countdown.resultTypeName = "System.Int32";  // The type of each item
    countdown.visibility = FunctionVisibility::Shared;

    functions.RegisterStream(countdown, [](FunctionCall& call)
    {
        // Defer keeps the stream open after the handler returns. The arguments are valid only until
        // then, so read them now
        countdowns.push_back({call.Defer(), call.Arg(0).AsInt(), 0});
    });
}

// Called after each Poll: writes the next number of every countdown that is due
void TickCountdowns(uint64_t nowMs)
{
    for (size_t i = 0; i < countdowns.size();)
    {
        Countdown& countdown = countdowns[i];

        if (nowMs < countdown.dueMs)
        {
            ++i;
            continue;
        }

        // False once the app cancelled the call or stopped reading
        bool isWritten = countdown.reply.Write(countdown.next);

        if (!isWritten || countdown.next == 0)
        {
            countdown.reply.End();
            countdowns.erase(countdowns.begin() + static_cast<std::ptrdiff_t>(i));
            continue;
        }

        countdown.next--;
        countdown.dueMs = nowMs + 1000;
        ++i;
    }
}
```

`resultTypeName` is the C# type (`"System.Int32"`, `"System.String"`) that the app reads each item's JSON as. Each `Write` sends one item, and the stream ends when the handler returns, unless it called `Defer()`; then `End()` ends it, and `Fail` ends it with an error the app sees. When the app cancels the call or stops reading, `Write` returns false and sends nothing, and the program should stop; `IsCancelled()` gives the same answer without writing. A dropped connection or a new session cancels every running stream the same way. When every copy of a deferred reply is destroyed without `End()`, the stream ends.

#### Built-In Client Functions

The app calls these on a client through `ClientFunctions` (`GetLocationAsync`, `LoginShowAsync`, `SetUrlAsync` and the rest). The C++ SDK registers none of them: a program that wants to answer registers the ones it supports as shared functions, under these names. Arguments arrive in the order below.

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

### Reactive Subscriptions

`Reactives` (`ikon_reactive.h`) follows values the app holds in a `Reactive<T>`, `ClientReactive<T>`, `UserReactive<T>` or `MountReactive<T>` without rendering its UI. It is made with the client and its `Functions`. It follows a value by its stable id. The app's built-in function `Ikon.Reactive.GetStableIdByName` returns the stable id of a reactive field or auto-property of the app's `[App]` class, by its name in the source (`"_score"` for `private readonly Reactive<int> _score`), so the app needs no code of its own for it. For a reactive kept anywhere else, the app returns its `StableId` from an `External` function.

<!-- ikon-example: cpp-sdk-reactive -->
```cpp
// The app looks up a reactive's stable id by the member's name in its [App] class
functions.Call("Ikon.Reactive.GetStableIdByName", FunctionArgs{"_score"}, [&reactives, &subscriptions](const Error& error, const FunctionResult& result)
{
    if (error)
    {
        std::cerr << "No stable id: " << error.message << std::endl;
        return;
    }

    // The callback gets the current value, then every change in order, on the polling thread
    subscriptions.push_back(reactives.Subscribe(result.Value().AsString(), [](const JsonValue& value)
    {
        std::cout << "Score: " << value.AsInt() << std::endl;
    }));
});

// Later, subscriptions[0].Unsubscribe() stops the callback. The last subscriber of a value also
// stops the app sending it
```

`Subscribe` returns at once. Its callback gets the current value, then every change in the order the app made it, on the driving thread; a reactive with no value yet is not delivered. A subscription made while the client connects waits until the client is ready, as a call does. The optional `subscribed` handler runs once with the outcome: no error when the app holds the subscription, or an error saying why not, and then the subscription is removed. Pass `mountId` for a `MountReactive<T>`. A `ClientReactive<T>` is delivered only for the client that subscribes: another client's value never reaches it. A value this client already follows is delivered from what it holds, without asking the app again.

Destroying a `ReactiveSubscription` keeps its callback; `Unsubscribe()` removes it, and the last subscriber of a value ends the app's subscription. A reconnect can end up in a new session that has none of the subscriptions, so each time the client is ready, the add-on subscribes every followed value again and delivers its current value; `Resubscribe()` does the same on demand. A `ReactiveSubscription` must not outlive the `Reactives` that made it.

### UI

`IkonUI` (`ikon_ui.h`) holds the app's UI as this client receives it: one tree per stream, the styles the trees use, and the actions their controls call. It applies the app's diffs, asks for a whole tree again when a diff cannot be applied, and acknowledges each update, as the web client does. It draws nothing. Render a stream's `root` with a UI of your own, and call the actions of its controls. A client without an `IkonUI` asks the server for no UI at all.

<!-- ikon-example: cpp-sdk-ui -->
```cpp
// Runs on the polling thread for every change: hand the tree to your renderer
ui.StreamUpdated = [](const UIStream& stream)
{
    std::cout << "Stream " << stream.StreamId() << " is at version " << stream.version << std::endl;
};

// Find a button by its label in the trees as they are now. The pointer is valid until the next Poll
const UINode* save = ui.FindNode([](const UINode& node)
{
    return node.type == "std.button" && node.GetText("label").value_or("") == "Save";
});

if (save != nullptr)
{
    // Press it, as a person would. The callback runs on the polling thread with the app's answer
    ui.CallAction(*save, "onInvokeId", "{}", [](const Error& error, bool completed)
    {
        std::cout << "Saved: " << (error ? error.message : completed ? "yes" : "no") << std::endl;
    });
}

// A text field takes its new value as the text itself, not as JSON
ui.WithNode([](const UINode& node) { return node.type == "std.text-field"; },
            [&ui](const UINode& field) { ui.CallAction(field, "onValueChangeId", "Ada"); });

// The app's page, and how many styles its trees use
ui.WithPage([&ui](const UIStream& page)
{
    std::cout << "Page root: " << (page.root ? page.root->type : "none") << ", " << ui.StyleCount() << " styles" << std::endl;
});
```

A `UIStream` is the tree of one mount: `category` is `"ikon-ui"` for the app's page (which `WithPage` visits), `"ikon.platform"` for the platform's layer above it, or another mount the app declares. `ForEachStream` visits them all. A `UINode` has an `id`, a `type` naming the component (`"std.button"`, `"std.text-field"`), its `props` as JSON text, its `children` and its `styleIds` for `Style(id, format)`. `PropJson(name)` is a prop's JSON, `GetText(name)` a string prop's text, `GetActionId` and `ForEachAction` its actions, and `Find` searches below it depth first.

The add-on changes the nodes in place as diffs arrive. A node pointer from `FindNode`, or a reference in `StreamUpdated`, is valid on the driving thread until the next `Poll`. Another thread reads the trees only inside `ForEachStream`, `WithPage` and `WithNode`, which hold the UI's lock, or takes a copy with `WaitForNode`. A copy of a node is a deep copy that stays as it was.

`WaitForNode(match, timeoutMs)` returns a copy of a matching node at once when one exists, else waits up to `timeoutMs` (30 s by default) and returns `std::nullopt` when none appeared. A node the screen does not have makes that wait last the whole timeout, so a program that only inspects the screen calls `WaitForFirstTree(timeoutMs)` first: it waits until a stream holds a tree and returns a copy of the root of the first stream that has one, which may be the platform's layer (`ikon.platform`) rather than the page. `Find` on it then answers at once. `WithPage` tells whether the page's own tree has arrived yet. On the driving thread both only look, without waiting.

The standard components put their text and labels in these props:

| Node type | Text and labels | Action and its payload |
|---|---|---|
| `std.text` | `content` | |
| `std.heading` | `content`, and `level` 1 to 6 | |
| `std.button` | `label` on a button with text only. A button with an icon or its own content has no `label`: its text is the `content` of a `std.text` child, and also its `aria-label`; `disabled` | `onInvokeId`, `{}`; absent while the button is disabled |
| `std.text-field`, `std.text-area` | `value`, `placeholder`, `disabled`. A field label is a `std.text` node just before the field, and also the field's `aria-label` | `onValueChangeId`, the text itself; absent while the field is disabled |
| `std.checkbox`, `std.switch` | `checked`, `disabled`. The label is a `std.text` node just after it, and also its `aria-label` | `onCheckedChangeId`, `true` or `false`; absent while disabled |
| `std.select` | `value`, `disabled`. The label is a `std.text` node just before it, and also its `aria-label` (also on its `std.select-trigger` child). Each option is a `std.select-item` with the option's `value`, its text in a `std.text` below it | `onValueChangeId`, the option's value itself; absent while the select is disabled |
| `std.slider` | `value`, a JSON array with one number per thumb, and `min`, `max`, `step` | `onValueChangeId` while it moves and `onValueCommitId` when it is let go, a JSON array with one number per thumb (`[0.8]`, `[0.2,0.8]`) |
| `std.color-picker` | `value`, and `format` (`hex` by default) | `onValueChangeId` and `onValueCommitId`, the color as text in that format (`#ff0000`) |
| `std.tabs` | `value` (`defaultValue` when the app does not control it). Each tab is a `std.tabs-trigger` with the tab's `value`, its label in a `std.text` below it. Every panel is in the tree as a `std.tabs-content`, and the open one's `value` equals the `std.tabs` node's `value` | `onValueChangeId`, the tab's `value` itself |
| `std.file-upload`, `std.file-upload-zone` | `uploadActionId`, a prop holding the id an upload names; `accept`, the MIME types or extensions the control takes (absent when it takes any); `disabled` | None: pass the id to the upload call. The SDK does not check `accept` or `disabled` before it uploads, so a program reads them first |
| A control declared with a `path`: `std.button`, `std.nav-item` and others | `actionPaths`, a JSON object from the control's action id to the app path its press opens (`/stable/{value}` for a control that sends a value) | The call moves the client to that path itself, as `SetPath` does, and moves back when the app refuses it or the call fails |
| `std.button` with an `href` (a link) | `href`, `target`, and the text as on a button | None unless the app also handles the press (`onInvokeId`, `{}`). A client follows the `href` itself |
| `std.action-button` | `label`, `actionType` (`CopyToClipboard`, `DownloadFile`, `Share` and the others) and `actionOptionsJson`, the action's options as JSON | The action runs in the browser, so a client does it itself. `onActionCompleteId` reports it, `{"ActionType":"CopyToClipboard","Success":true}` |
| `std.feed-scroller` | `activeIndex` when the app controls it, `slideCount`, `muted`. Each slide is a `std.feed-slide` with its `index` | `onActiveChangeId` when a slide becomes the active one and `onScrollNearEndId` near the end, the slide's index as a number (`3`); `onMuteChangeId`, `true` or `false` |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

`CallAction` returns once the call went out. Its callback runs on the driving thread with the app's answer: no error, and whether the handler completed. It runs with an error when no answer came within `timeoutMs` (30 s by default, `Timeout`), when the client moved onto a new server session first, or when the client went `Offline`; `Disconnect()` runs it with `Cancelled`. `CallAction` itself returns an error, and sends nothing, when the client is not `Connected` (`NotConnected`) or the node has no action in that prop. A payload the app cannot read, such as `0.8` for a slider instead of `[0.8]`, fails the call like a handler that throws: the callback gets `completed` false, and the reason is only in the app's log (`ikon logs --level warn`).

A disabled control has the `disabled` prop and no action, so `CallAction(node, prop)` on it returns an error and nothing reaches the app. A program that must press it waits for the app to enable it (`WaitForNode` with a match on `GetActionId`). `CallActionAndWait` blocks until the app answers. On the driving thread it fails at once instead.

A reconnect into the same session asks for every tree again; one into a new session ends every stream, and `StreamEnded` runs for each. `RequestResync(stream)` asks for a whole tree on demand. `UIOptions` turns off what a program does not need: `keepStyles = false` keeps no styles, which can run to tens of kilobytes of CSS, and `keepPayloads = false` keeps no binary values, such as image bytes.

### File Upload

`Uploads` (`ikon_upload.h`) sends a file through one of the app's `FileUpload` or `FileUploadZone` controls, which names the upload in its `uploadActionId` prop. The app's `onUploadComplete` gets the file.

<!-- ikon-example: cpp-sdk-upload -->
```cpp
// The app's FileUpload control names the upload in its uploadActionId prop
std::optional<std::string> actionId;
ui.WithNode([](const UINode& node) { return node.PropJson("uploadActionId") != nullptr; },
            [&actionId](const UINode& node) { actionId = node.GetText("uploadActionId"); });

if (!actionId)
{
    return;
}

// The file is read again for what a dropped connection lost, so it stays open until the upload ends
auto file = std::make_shared<std::ifstream>("report.pdf", std::ios::binary);

UploadOptions options;
options.mimeType = "application/pdf";
options.progress = [](const UploadProgress& progress)
{
    std::cout << progress.acknowledgedBytes << " of " << progress.totalBytes << " bytes" << std::endl;
};

// The callback runs once, on the polling thread, after the app's onUploadComplete has run or the upload failed
Error error = uploads.StartUpload(*actionId, StreamUploadSource(*file), "report.pdf", [file](const UploadResult& result)
{
    if (result.Ok())
    {
        std::cout << "Uploaded " << result.size << " bytes, SHA-256 " << result.hash << std::endl;
    }
    else
    {
        std::cout << "Upload failed: " << result.error.message << (result.isRetriable ? " (worth retrying)" : "") << std::endl;
    }
}, options);

if (error)
{
    std::cout << "The upload did not start: " << error.message << std::endl;
}
```

The control is in the UI tree only on the screen the app shows for the client's path, so a program that cannot find it opens that screen first: set `ClientConfig::initialPath` (for example `"/photos"`) before `Connect()`, or call `SetPath` and wait for the node again.

`StartUpload` returns at once and is safe from any thread. It returns an error, and starts nothing, for an empty id or name, a source with no size (such as a stream that cannot seek), an `uploadId` already in progress, or a client that is not connected. Otherwise its callback runs once, on the driving thread, when the upload ends: after the app's `onUploadComplete` has run, with the upload's id, size and the lowercase hex SHA-256 of the bytes, which the app verified before storing them, or with why it failed. `isRetriable` says whether sending the same bytes again is likely to work.

An `UploadSource` reads any range of the content. `StreamUploadSource` reads a `std::istream` from its position to its end, and again from the last acknowledged byte after a resume, so the stream must be seekable and outlive the upload. If the connection drops after the client has hashed the content and started the transfer, and the client reconnects within `reconnectTimeoutMs` (45 s), the upload resumes on the new connection. A drop before that, while the app has not yet accepted the file or the client is still hashing the content, fails the upload with `isRetriable` set. `UploadOptions` also takes the `uploadId` the app sees in every upload hook (empty generates one), for a file from a folder its `relativePath`, and the content's `sha256` when the program has it, which saves reading the file a second time to hash it. `progress` runs on the driving thread, so it should return quickly. At most 8 uploads per client run at once; later ones wait. `UploadFile` waits for the upload to end. On the driving thread it fails at once instead.

### Audio and Video

`Media` (`ikon_media.h`) sends and receives media as encoded frames. It does not encode or decode them: the program brings its own encoders and decoders ([Encoding and Decoding Audio](#encoding-and-decoding-audio) shows Opus). Audio is Opus; video is H.264 (Annex B), VP8 or VP9. For H.264 each frame is one access unit (every slice of one picture) in Annex B form with start codes, each keyframe with its SPS and PPS before the IDR slice, because a receiver decodes from the parameters in the stream. ffmpeg's libx264 writes such a stream with `-c:v libx264 -x264-params aud=1:repeat-headers=1 -f h264`: an access unit delimiter starts each picture, so a reader of its output splits the frames there, and every keyframe repeats the SPS and PPS. A client's audio and video reach the app only. In the example, `opusPackets` holds your encoder's packets and `playingStreamId` names a stream your player is playing.

<!-- ikon-example: cpp-sdk-media -->
```cpp
// Media the app sends this client, as encoded frames, on the polling thread. The callbacks name each
// stream "{senderId}_{trackId}"; begin.StreamId is the app's own id for it. The views are valid only
// during the call: hand the frames to a decoder
media.AudioStreamBegan = [](const std::string& streamId, int32_t senderId, const AudioStreamBegin::View& begin)
{
    // Make an Opus stream's decoder with begin.SampleRate and begin.Channels
    bool isOpus = static_cast<AudioCodec>(begin.Codec) == AudioCodec::Opus;
    std::cout << "Audio " << streamId << " from " << senderId << " (" << begin.StreamId << "): " << (isOpus ? "Opus, " : "not Opus, ")
              << begin.SampleRate << " Hz, " << begin.Channels << " ch" << std::endl;
};
media.AudioFrameReceived = [](const std::string& streamId, const AudioFrame::View& frame)
{
    // frame.Samples is one Opus packet
};
media.VideoStreamBegan = [](const std::string& streamId, int32_t senderId, const VideoStreamBegin::View& begin)
{
    std::cout << "Video " << streamId << ": " << begin.Width << "x" << begin.Height << std::endl;
};
media.VideoFrameReceived = [](const std::string& streamId, const VideoFrame::View& frame)
{
    // frame.Data is one encoded frame in the codec the begin named
};

// The app asks for a keyframe when it starts showing the stream or loses frames
auto keyFrameWanted = std::make_shared<std::atomic<bool>>(true);
media.VideoKeyFrameRequested = [keyFrameWanted](const std::string& streamId, int32_t requesterId)
{
    *keyFrameWanted = true;
};

if (Error error = client.Connect())
{
    std::cerr << "Could not connect: " << error.message << std::endl;
    return;
}

// Tell the app how far your player has played a stream, in seconds from the start of the current
// segment, and how many milliseconds it holds that it has not played yet. Either id of the stream works
media.SendAudioPlaybackReport(playingStreamId, 1.5, 120);

// Send the packets of your Opus encoder: 20 ms each at 48 kHz mono. The correlation id ties the
// audio to what the app asked for, such as a press of a talk button
for (size_t i = 0; i < opusPackets.size(); i++)
{
    media.SendAudioFrame("mic", opusPackets[i], 48000, 1, 960, i == 0, i == opusPackets.size() - 1, "talk-1");
}

media.EndAudioStream("mic");

// Send H.264 (Annex B) frames at 30 fps; EncodeNextH264Frame stands for your encoder. A change
// of size, codec or correlation id announces the stream again
for (int i = 0; i < 90; i++)
{
    bool isKey = keyFrameWanted->exchange(false);
    media.SendVideoFrame("camera", EncodeNextH264Frame(isKey), isKey, i * 33333, VideoCodec::H264, 640, 480);
}

media.EndVideoStream("camera");
```

`SendAudioFrame` takes a sample rate of 8000, 12000, 16000, 24000 or 48000 Hz and 1 or 2 channels, fixed by the stream's first packet; another format returns `MediaResult::InvalidArgument` with a warning in the log, so start a new stream id for it. `isFirst` and `isLast` mark a segment such as one utterance. The app's speech recognition ignores audio without a correlation id, and a new correlation id on a first packet announces the stream again. `SendVideoFrame`'s `sourceType` (`"camera"` by default, or `"screen"`) sets the kind of video input the app sees. Send a keyframe first, because a receiver shows nothing until one arrives. Both return `MediaResult::NotSent` when the client is not connected or not ready yet: a frame is never held for later, because it would play late. A reconnect announces every open stream again. The send methods are safe from any thread.

Each incoming stream has two ids. The callbacks, `RequestVideoKeyFrame` and `SendAudioPlaybackReport` use `"{senderId}_{trackId}"`, such as `"1_3"`. `begin.StreamId` is the id the app gave the stream, such as the `streamId` it passed to `Audio.Raw.SendFrameAsync`; use it to tell the app which of its streams you mean. `begin.Description` names the kind of source, not the stream.

Incoming streams end with `AudioStreamEnded` and `VideoStreamEnded`, which also run for every open stream on `Disconnect()`, on `Connect()` and on a reconnect into a new session. `RequestVideoKeyFrame(streamId)` asks the sender of an incoming video stream for a keyframe, and returns false when that stream is not open or the request did not go out.

A program that plays the app's audio reports how far it has played each stream with `SendAudioPlaybackReport(streamId, playedSeconds, bufferedMs, state)`: every 500 ms of playback, and at once when the state changes, as the browser does. `playedSeconds` is the position from the start of the current segment (negative when unknown), `bufferedMs` is what the player holds that it has not played, and `state` is `AudioPlaybackState::Playing` (the default), `Blocked` or `Hidden`. The app reads the latest report with `Audio.Raw.GetPlaybackStatus` and `Audio.Raw.PlaybackReportReceivedAsync`, to pace what it says to what was heard. Only the streams the app sends with `Audio.Raw.SendFrameAsync` ask for reports: `WantsPlaybackReports(begin)` is true for them, and false for every other stream, such as the one that carries what `Audio.Play` plays. An app built on older IKON libraries, from before a stream could say whether it wants reports, asks for reports on every stream it sends. `SendAudioPlaybackReport` sends nothing and returns false for a stream that is not open or wants no reports, and when the report did not go out.

#### Encoding and Decoding Audio

Any Opus library works. libopus comes from vcpkg (`opus` in a `vcpkg.json`, or `vcpkg install opus`). Find it with `find_package(Opus CONFIG REQUIRED)` in a build configured with vcpkg's toolchain file (see [Building a Program](#building-a-program)), and link it as `Opus::opus`. On Linux the system package works without vcpkg: `apt install libopus-dev` (`dnf install opus-devel` on Fedora), then `find_package(PkgConfig REQUIRED)` and `pkg_check_modules(opus REQUIRED IMPORTED_TARGET opus)`, and link `PkgConfig::opus`. Either way, include it as `#include <opus.h>`. Encode 20 ms frames, 960 samples per channel at 48 kHz, as the .NET SDK does, and pass the same sample rate, channel count and frame size to `SendAudioFrame`. Make a decoder for each incoming stream in `AudioStreamBegan` from `begin.SampleRate` and `begin.Channels`, never from what the app sent: audio the app sends as mono can arrive as 48 kHz stereo.

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

Client events tell this client about the other clients in the session and about the app itself. Only clients in the same app instance, which the app's `SessionIdentity` decides, and on a surface with `Presence = true` are reported; a client on a surface without presence is reported to the app alone. Each handler gets the other client's `Context`: its `SessionId`, `UserId` (for a client that connected with an API key, an id the platform assigned, not its `externalUserId`; the app's own is `local`), `ContextType`, and what it reports of its viewport, theme, time zone and visibility (`ViewportWidth`, `ViewportHeight`, `Theme`, `Timezone`, `Visibility`). `SessionId` is an `int32_t` and `UserId` a `std::string`. `Context::ContextType` is an `int32_t`, so compare it through a cast: `static_cast<ContextType>(other.ContextType) == ContextType::Browser`. This program joins as `ContextType::Plugin`, as a program built with the .NET, Rust or TypeScript SDK without a page does; a page and a Flutter app join as `ContextType::Browser`. The app is a `Plugin` with `IsInternal` true.

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

The client tracks the other clients only when at least one of the three handlers is set before `Connect()`. `ClientJoined` runs once a client has finished joining, so the app can answer it from then on. On joining, it runs for each client already there. A client that leaves before it has finished joining is never reported, and `ClientUpdated` and `ClientLeft` run only for a client `ClientJoined` reported. Each client is reported once per session. A reconnect into the same session reports only what changed while this client was away: `ClientJoined` for a client that joined meanwhile, `ClientUpdated` for one whose viewport, theme, time zone or visibility changed, and `ClientLeft` for one that left. A reconnect into a new session reports its clients as a first join does. The app is one of the clients, and it gets a new `SessionId` each time it restarts or hot reloads. The events are never about this client. They run on the driving thread.

## Client Context and Path

The app sees each client's viewport size, theme, time zone and visibility in the client's context. Tell it when one of them changes, and when the client shows another path:

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

// All five fields: viewport in CSS pixels, "light" or "dark", an IANA time zone id,
// and "visible" or "hidden"
ClientContextUpdate everything;
everything.viewportWidth = 1920;
everything.viewportHeight = 1080;
everything.theme = "light";
everything.timezone = "Europe/Helsinki";
everything.visibility = "hidden";
client.UpdateClientContext(everything);

// Tell the app this client now shows another path, as a browser does when it navigates
client.SetPath("/room/7");
```

Both return false, sending nothing, when the client is not connected. The server replaces all five context values on each update, so the SDK fills each empty field with the value last sent, or else the one the client connected with. A reconnect into a new session, and the next `Connect()` after `Disconnect()`, send the last values and the last path again. The path is relative to the app's root; a query string reaches the app's path handlers but is not kept as the path.

## Writing an Add-On

An add-on is a `ClientExtension`: the client calls its hooks when it joins, receives a message, becomes ready, changes state and polls. The SDK's own add-ons are built this way, and a program can write its own, such as one that drives a status light:

<!-- ikon-example: cpp-sdk-client-extension -->
```cpp
// Shows the connection on a status LED: on while connected, blinking while reconnecting, else off
class StatusLight : public ClientExtension
{
public:
    explicit StatusLight(IkonClient& client)
    {
        client.AddExtension(*this);
    }

    // Takes the add-on off the client first, so no hook runs on a half-destroyed object
    ~StatusLight() override
    {
        Detach();
    }

    void OnStateChanged(IkonClient& client, ConnectionState state) override
    {
        _isBlinking = state == ConnectionState::Reconnecting;
        _isOn = state == ConnectionState::Connected;
        _nextToggleMs = client.System().NowMs() + BlinkMs;
        SetLed(_isOn);
    }

    // At the end of every Poll, in every state
    void OnPoll(IkonClient& client) override
    {
        uint64_t now = client.System().NowMs();

        if (_isBlinking && now >= _nextToggleMs)
        {
            _isOn = !_isOn;
            _nextToggleMs = now + BlinkMs;
            SetLed(_isOn);
        }
    }

    // At the start of every Poll. The limit keeps Poll from waiting for input past the next toggle
    int PollWaitLimitMs(IkonClient& client) override
    {
        if (!_isBlinking)
        {
            return -1;
        }

        uint64_t now = client.System().NowMs();
        return now >= _nextToggleMs ? 0 : static_cast<int>(_nextToggleMs - now);
    }

private:
    static constexpr int BlinkMs = 500;
    bool _isBlinking = false;
    bool _isOn = false;
    uint64_t _nextToggleMs = 0;
};
```

Every hook runs on the driving thread, except `OnStateChanged` for the `Idle` that `Disconnect()` sets, which runs on the thread that called it. Each has a default that does nothing:

| Hook | When it runs |
|---|---|
| `OnJoined(client, isNewSession)` | The join has the session state and the app's functions; the client reports ready after every add-on returns. `isNewSession` is false for a resume of the session joined before |
| `OnMessage(client, message)` | Every message, in the order add-ons were added, before the program's handlers. Returning true keeps it from the add-ons after this one and from the handlers |
| `OnReady(client, isNewSession)` | The client reported ready and is `Connected` |
| `OnStateChanged(client, state)` | After each change of state |
| `OnPoll(client)` | At the end of each `Poll`, in every state, so an add-on runs its timers on the client's clock |
| `PollWaitLimitMs(client)` | At the start of each `Poll`: the longest the poll may wait for input before `OnPoll` runs, 0 when the add-on has work waiting, or -1 for no limit |
| `TakesUI(client)` | True when the add-on keeps and acknowledges the app's UI. Without one, the client asks the server for no UI |
| `MaxSdkCapability(client)` | The highest SDK capability the add-on implements; the client advertises the lowest of all add-ons |

The client does not own an add-on. `AddExtension` attaches it before `Connect()`; `RemoveExtension`, or `Detach()` from the add-on, takes it off. Once either returns, no hook of the add-on runs or is running, so it may be destroyed. A derived destructor calls `Detach()` first, so no hook runs on a half-destroyed object.

## Microcontrollers

The SDK runs on a chip with no operating system, or with an RTOS such as FreeRTOS under ESP-IDF, as it runs on a desktop. A port gives it the chip's TLS stack, clock and log through four small interfaces, polls the client from its main loop, and includes only the headers it uses.

### Interfaces a Port Implements

#### INetwork

A TLS connection over TCP to the server. The SDK connects to the server's TLS entrypoint, so a plain TCP socket is not enough. On a chip it is built on the vendor's TLS stack, such as mbedTLS over lwIP:

<!-- ikon-example: cpp-sdk-network-interface -->
```cpp
class BoardNetwork : public INetwork
{
public:
    // Open a TLS connection to host:port within options.timeoutMs. Check the server's certificate and
    // host name unless options.verifyCertificate is false, which the SDK sets only for a local IKON
    // server, whose certificate is self-signed.
    bool Connect(const char* host, int port, const ConnectOptions& options) override
    {
        _connection = board_tls_open(host, port, options.verifyCertificate, options.timeoutMs);
        return _connection >= 0;
    }

    // With threads, Close can run while another thread waits in Read, which then returns -1
    void Close() override
    {
        if (_connection >= 0)
        {
            board_tls_close(_connection);
            _connection = -1;
        }
    }

    // Write every byte, or return false
    bool Write(const uint8_t* data, size_t size) override
    {
        while (size > 0)
        {
            int written = board_tls_write(_connection, data, size);

            if (written <= 0)
            {
                return false;
            }

            data += written;
            size -= static_cast<size_t>(written);
        }

        return true;
    }

    // The number of bytes read, 0 when none arrived within timeoutMs, -1 once the connection is closed
    int Read(uint8_t* buffer, size_t capacity, int timeoutMs) override
    {
        return _connection < 0 ? -1 : board_tls_read(_connection, buffer, capacity, timeoutMs);
    }

private:
    int _connection = -1;
};
```

With threads, `Read` and `Write` may run at the same time on two threads, a reader waiting in `Read` while another thread writes, and `Close` may run while a thread waits in `Read`. With `IKON_NO_THREADS` the SDK calls them from one thread only.

`ConnectOptions::verifyCertificate` is true for the cloud, and false only for a local IKON server, whose certificate is self-signed: when `ClientConfig::local` is set, or when `externalConnectUrl` is on this computer (`localhost`, `127.0.0.0/8`, `::1`). A port that verifies certificates needs a bundle of trusted root certificates, as a browser has.

On Linux and macOS, a write to a connection the peer has reset raises `SIGPIPE`, which ends the program by default. A network client must turn it into a failed write: `ExampleNetworkClient` sets `SO_NOSIGPIPE` on the socket where it exists (macOS) and on Linux gives OpenSSL a socket BIO of its own that writes with `send(..., MSG_NOSIGNAL)`. A client that writes with `send` itself passes `MSG_NOSIGNAL` the same way. `ExampleNetworkClient` verifies the chain against the system's trusted roots (the Windows root store on Windows, OpenSSL's default paths elsewhere) and checks the host name or IP address; a failed connect is logged as a warning with its reason, and `LastError()` returns it.

#### IHttp

`Send` makes one HTTPS request and returns its response. The SDK sends a few: the sign-in, the request that starts or finds the app's session, and the request for a connection to it.

<!-- ikon-example: cpp-sdk-http-interface -->
```cpp
class MyHttp : public IHttp
{
public:
    HttpResponse Send(const HttpRequest& request) override
    {
        // Send request.body to request.url with request.headers, as a POST when request.isPost is true
        // and else a GET, and give up after request.timeoutMs. Skip the certificate check only when
        // request.verifyCertificate is false, which the SDK sets only for a local IKON server, whose
        // certificate is self-signed.
        std::cout << (request.isPost ? "POST " : "GET ") << request.url << std::endl;

        HttpResponse response;
        response.status = 200;  // 0 or less when no server answered
        response.headers = {};
        response.body = "";
        return response;
    }
};
```

A request that reached no server returns a status of 0 or less, which the SDK reports as `ServerUnreachable`. `HttpResponse::Header(name)` compares header names without regard to case.

#### ISystem

The clock, random numbers and a way to wait:

<!-- ikon-example: cpp-sdk-system-interface -->
```cpp
class BoardSystem : public ISystem
{
public:
    // A clock that never goes back: a timer since boot, not the time of day, which can be set
    uint64_t NowMs() override
    {
        return board_millis();
    }

    // For the device id and the random parts of reconnect delays and call ids
    uint64_t Random() override
    {
        return (static_cast<uint64_t>(board_random32()) << 32) | board_random32();
    }

    // With IKON_NO_THREADS nothing calls Wake, so Sleep only waits
    void Sleep(int milliseconds) override
    {
        if (milliseconds > 0)
        {
            board_delay_ms(milliseconds);
        }
    }
};
```

`NowMs` counts milliseconds from any fixed point and never goes back. With threads, `Wake` ends a `Sleep` early from another thread, which `Disconnect` and `StopPolling` use; its default does nothing, so a `Sleep` that cannot be woken only delays them by up to one poll's timeout. `StdSystem` in `ikon_platform_std.h` implements `ISystem` with the standard library.

#### Log

A `Log` is a plain function pointer and a context pointer, so it needs no heap of its own. The SDK calls the sink once per line, on the thread that logs it:

<!-- ikon-example: cpp-sdk-log-interface -->
```cpp
// Called once per line, on the thread that logs it. context is the Log's own pointer, here unused
void ConsoleLogSink(void* context, LogLevel level, const char* message)
{
    (void)context;
    static const char* const names[] = {"TRACE", "DEBUG", "INFO", "WARN", "ERROR"};
    std::cout << "[" << names[static_cast<int>(level)] << "] " << message << std::endl;
}

Log MakeConsoleLog()
{
    Log log;
    log.sink = &ConsoleLogSink;
    return log;
}
```

A client made without a `Log` logs nothing. `StderrLogSink` in `ikon_platform_std.h` writes each line to stderr with its level, and `ExampleLog(minimum)` in `example_logger.h` writes to the console with the time, in colour on a terminal and plain when stdout is redirected to a file or pipe. `IKON_LOG_LEVEL` compiles out the lines below a level: 0 keeps every line, 1 (the default) drops trace, 2 also drops debug, 3 also info, 4 also warnings, and 5 drops every line, with their text, from the image.

### Build Settings

| Define | What it does |
|---|---|
| `IKON_NO_THREADS` | No `std::thread` or `std::mutex`: the SDK's locks compile to nothing, and `StartPolling` and the waiting calls are left out. The program calls `Poll` from one thread |
| `IKON_NO_FILESYSTEM` | `ikon_local_run.h` leaves out `LocalConfigFromAppFolder`, which needs `std::filesystem` |
| `IKON_LOG_LEVEL` | The lowest log level compiled in (see [Log](#log)) |

The SDK needs no exceptions or RTTI, so firmware builds it with `-fno-exceptions -fno-rtti`. It needs the C++17 standard library's containers and `std::string`, which the chip's GCC or Clang toolchain provides. Include `ikon_client.h` and the add-ons the device uses, not `ikon_sdk.h`, which includes them all.

### Memory Settings

<!-- ikon-example: cpp-sdk-memory-settings -->
```cpp
// The receive buffer starts at this size and grows to fit a larger message
config.receiveBufferSize = 2048;

// A larger message from the server closes the connection
config.maxMessageSize = 256 * 1024;

// The receive buffer shrinks back after each larger message, and the client frees the session
// state once the add-ons have read it at the join
config.lowMemory = true;
```

- `receiveBufferSize` (4096 bytes) is the receive buffer between messages. It grows to fit a larger message, and with `lowMemory` it goes back to this size once that message is handled.
- `maxMessageSize` (10 MiB) is the largest message the client accepts. A larger one closes the connection, so a device never tries to hold more than it can.
- `lowMemory` also frees the session state once the add-ons have joined, and keeps none until the next join. `GlobalStateBytes()` then returns nothing, `WithGlobalState` returns false and `HasReadyClient` finds no one.
- `Uploads::maxChunkSize` lowers the largest chunk an upload reads and sends at once (1 MiB at most) for a device that cannot hold one.
- `UIOptions{false, false}` keeps no styles or binary values of the app's UI, and a device that shows no UI makes no `IkonUI` at all, so the server sends it none.

The session state lists every client of the session that this client is told about. The client reads it where it arrived, without copying it, but the whole message has to fit in memory while it is read, so the heap a join needs grows with the session. A device that enters a surface with `Presence = false` in the app's config gets a session state that lists only the app's own clients, however many people are in the session.

### Size

A program pays only for what it includes: `ikon_client.h` is the connection and the protocol, and each add-on adds its own code. Build firmware for size, with `-Os` and `-ffunction-sections -fdata-sections` and the linker's `--gc-sections`, as for any C++ firmware.

The table below is for a client that signs in and joins, with no add-ons, built that way and without exceptions. Each figure is the image minus an empty image for the same chip, and includes the test's scripted server, about 5 KB:

| Chip | Flash | Static RAM |
|---|---:|---:|
| Cortex-M0+ | 90 KB | 0.6 KB |
| Cortex-M4F, Cortex-M33 | 79 KB | 0.5 KB |
| RISC-V RV32IMAC | 100 KB | 0.9 KB |
| ESP32-S3 | 85 KB | 0.6 KB |
| ESP32-C3 | 98 KB | 0.6 KB |

With `lowMemory`, a join takes about 10 KB of heap at its peak in a session of 2 clients and 25 KB in one of 20, and the client keeps about 10 KB once joined. Reading a message allocates nothing. The figures leave out the device's TLS library and network stack, which on an ESP32 with 16 KB TLS records take about 40 KB of heap for the connection.

## Advanced Configuration

### Timeouts

<!-- ikon-example: cpp-sdk-timeouts -->
```cpp
config.timeouts.connectTimeoutMs = 30000;       // Each sign-in request, and the wait for a starting server
config.timeouts.provisioningTimeoutMs = 60000;  // How long /init waits for the app's server to start
config.timeouts.joinTimeoutMs = 30000;          // The session state and the app's functions, once connected
config.timeouts.maxReconnectAttempts = 6;
config.timeouts.reconnectBackoffMs = 500;       // Before the first attempt, doubled for each next one
```

The backend request that waits up to `provisioningTimeoutMs` for the app's server to start is itself sent with an HTTP timeout of `connectTimeoutMs`, so raise `connectTimeoutMs` to at least `provisioningTimeoutMs` for a longer provisioning wait to take effect. `maxReconnectAttempts = 0` makes the client go `Offline` at the first drop.

### Post-Quantum Key Exchange

<!-- ikon-example: cpp-sdk-require-post-quantum -->
```cpp
config.requirePostQuantum = true;
```

With `requirePostQuantum` set, every TLS connection the client makes (the sign-in, the session requests and the connection to the server) offers only the hybrid post-quantum key exchange group X25519MLKEM768, and only TLS 1.3. A server that cannot negotiate it fails the handshake, and the client never falls back to a classical key exchange. The deployed IKON servers and the load balancers negotiate it. A server on Windows (a local `ikon run` reached at a non-loopback address) has no ML-KEM in its TLS, so the handshake fails there. A connection to a loopback address (`localhost`, `127.0.0.0/8`, `::1`), such as a local `ikon run` on the same machine, crosses no network and is exempt. The client reaches a server only over TLS, so no other transport needs to be turned off. An app that sets `RequirePostQuantum` in its `ikon-config.toml` makes its server offer only transports that can use post-quantum key exchange, but only this flag makes the client refuse a classical key exchange.

The SDK passes the flag to your interface implementations: `HttpRequest::requirePostQuantum` on each request, and `ConnectOptions::requirePostQuantum` on each `Connect`. Both are `false` for a loopback host. An implementation says it honours them by overriding `SupportsPostQuantumOnly()` to return `true`. With the flag set, `Connect()` fails with `PostQuantumUnsupported` before sending anything when either interface returns `false`, the default, so an implementation written without the flag in mind cannot connect with a classical key exchange. With OpenSSL, the TLS setup is two calls:

<!-- ikon-example: cpp-sdk-post-quantum-tls -->
```cpp
// Call it on the SSL_CTX before the handshake when HttpRequest::requirePostQuantum or
// ConnectOptions::requirePostQuantum is true, and fail the request or the connect when it returns false.
// OpenSSL before 3.5 has no X25519MLKEM768, and the second call fails there.
bool OfferOnlyPostQuantum(SSL_CTX* context)
{
    return SSL_CTX_set_min_proto_version(context, TLS1_3_VERSION) == 1 &&
           SSL_CTX_set1_groups_list(context, "X25519MLKEM768") == 1;
}
```

`ExampleHttpClient` and `ExampleNetworkClient` honour the flag and need OpenSSL 3.5 or later for it. With an older OpenSSL their `SupportsPostQuantumOnly()` returns `false`, so `Connect()` refuses. WinHTTP has no setting for the key exchange group, so on Windows `ExampleHttpClient` sends a request that requires post-quantum key exchange over OpenSSL instead.

### Protocol Options

<!-- ikon-example: cpp-sdk-protocol-options -->
```cpp
// The message groups the client asks the server for, and the ones it sends. These values are the
// default: every group, plus GROUP_APP_LOCAL for the app's own schema messages
config.opcodeGroupsFromServer = static_cast<int32_t>(Opcode::GROUP_ALL) | static_cast<int32_t>(Opcode::GROUP_APP_LOCAL);
config.opcodeGroupsToServer = config.opcodeGroupsFromServer;
```

A client without an `IkonUI` add-on leaves `Opcode::GROUP_UI` out of what it asks for, whatever `opcodeGroupsFromServer` says, and a client that leaves it out of `opcodeGroupsFromServer` gets no UI even with one (`IkonUI::IsEnabled()` is then false).

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

`userAgent`, left empty, is the SDK's own, `ikon-sdk-cpp (<OS>; <architecture>)`, which the app reads with `ClientPlatform.FromContext`. It knows Windows, macOS, Linux, Android and iOS, and the x64, x86, Arm64 and Arm architectures; a program on ESP-IDF or another embedded target reads as `ClientOS.Unknown`, and an Xtensa or RISC-V chip as `ClientArchitecture.Unknown`.

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `IkonClient` | Connects to an app, sends and receives messages, and runs the add-ons |
| `ClientConfig` | The client's configuration |
| `ConnectionState` | Enum: `Idle`, `Connecting`, `Connected`, `Reconnecting`, `Offline`; `ToString(state)` gives the name |
| `Error`, `ErrorCode` | A failure as a value; see [Errors](#errors) |
| `MessageView` | One received message, read in place: `SenderId()`, `OpcodeValue()`, `Read<T>`, `Decode<T>` |
| `MessageOptions` | The track id and other header fields `Send` sets |
| `ClientContextUpdate` | The context values `UpdateClientContext` changes; empty fields keep their value |
| `ClientExtension` | The base of an add-on |
| `JsonValue` | A parsed JSON value, for function arguments, results and reactive values |
| `TeleportGuid` | A GUID, such as a call's id |

### Add-On Types

| Type | Description |
|------|-------------|
| `Functions` | Functions for the app to call, and calls of the app's functions (`ikon_functions.h`) |
| `FunctionDefinition`, `FunctionParameterInfo`, `FunctionVisibility` | A function this client registers; `Local` or `Shared` |
| `FunctionArgs` | A call's arguments |
| `FunctionCall`, `FunctionReply` | A call of this client's function, and its answer |
| `FunctionResult` | A result or stream item, valid only during its callback |
| `Reactives`, `ReactiveSubscription` | Follows the app's reactive values (`ikon_reactive.h`) |
| `IkonUI`, `UIOptions` | The app's UI trees, styles and actions (`ikon_ui.h`) |
| `UIStream`, `UINode` | The tree of one mount, and one of its nodes |
| `Uploads`, `UploadOptions`, `UploadResult`, `UploadProgress`, `UploadSource` | File uploads (`ikon_upload.h`) |
| `Media`, `MediaResult`, `AudioPlaybackState` | Audio and video (`ikon_media.h`) |

### Configuration Types

| Type | Description |
|------|-------------|
| `ApiKeyConfig` | An API key |
| `LocalConfig` | A local run; `LocalConfigFromAppFolder` (in `ikon_local_run.h`) reads it from the app's folder |
| `GuestConfig` | A signed-out visitor |
| `UserTokenConfig` | A person's session token |
| `TimeoutConfig` | Timeouts and reconnection |
| `BackendType` | Enum: `Production`, `Development` |

### Interface Types

| Type | Description |
|------|-------------|
| `INetwork` | A TLS connection over TCP (`ikon_platform.h`) |
| `IHttp`, `HttpRequest`, `HttpResponse` | HTTPS requests |
| `ISystem` | The clock, random numbers and waiting |
| `Log`, `LogLevel` | Where log lines go |
| `StdSystem`, `StderrLogSink` | `ISystem` and a log sink on the standard library (`ikon_platform_std.h`) |
| `ExampleNetworkClient`, `ExampleHttpClient`, `ExampleLog` | Desktop implementations on OpenSSL (`example_*.h`) |

### Protocol Types

| Type | Description |
|------|-------------|
| `Context` | One client of the session |
| `GlobalState` | The session state |
| `Opcode` | Message opcodes and groups |

## License

This SDK is licensed under the Apache License 2.0. See `LICENSE` for details.

## Support

For issues and feature requests, contact IKON support or open an issue on GitHub.
