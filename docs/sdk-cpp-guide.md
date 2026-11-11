<!-- checked-against: 276c1fe291ddd174e3b0c442 -->

# IKON C++ SDK

The IKON C++ SDK connects C++ applications to an IKON app. It is a header-only library requiring C++17, and every example below compiles as C++17.

## Features

- Two authentication modes: API Key, Local Development
- Connection state management with callbacks
- Protocol message sending and receiving
- Function registry for registering and calling shared cross-client functions
- Configurable timeouts and reconnection
- Header-only library

## Requirements

- C++17 compatible compiler
- Implementations of required interfaces:
  - `ILogInterface` - Logging
  - `IHttpInterface` - HTTP client
  - `INetworkInterface` - TLS client over TCP

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

The SDK supports two authentication modes. Exactly one must be configured.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once.

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

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: cpp-sdk-auth-local -->
```cpp
LocalConfig local;
local.host = "localhost";
local.httpsPort = 8443;
local.userId = "dev-user";

IkonClientConfig config;
config.local = local;
```

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
    std::cout << "State: " << static_cast<int>(state) << std::endl;
};

// Joined and ready, after Connect() and after every reconnect: the app now knows this client's
// user, and client.Functions() already holds the app's functions
client.Ready = []()
{
    std::cout << "Ready" << std::endl;
};

// This client is stopping: on Disconnect(), destruction, a dropped connection or before
// a reconnect attempt. A message sent from here is not guaranteed to be delivered
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

### Raw Protocol Messages

<!-- ikon-example: cpp-sdk-send-raw -->
```cpp
// Wrap a payload in a ProtocolMessage yourself and send it as is. Read the
// context inside a client callback: on another thread a reconnect can free it.
auto* ctx = client.GetClientContext();
if (ctx)
{
    auto message = ProtocolMessage::Create(ctx->SessionId, payload);
    client.SendMessage(message);
}
```

### Typed Payloads

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

`FunctionRegistry` lets a client register functions for the app to call and call the functions the app registers; the server offers a client's functions to the app only, not to other clients. The client owns one and attaches it itself on connect; get it with `client.Functions()`. Each join waits until the registry holds the app's functions, so they can be called from `Ready` and after `Connect()` returns. Register `Shared` functions before `Connect()`, as below. The client announces them on every join, reconnects included, before it reports ready, so the app has them when it first sees the client. One registered while connected is announced at once. It handles function-related protocol messages through its own message handler, independent of `MessageReceived`.

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

Additional members: `Call` (invoke a local function), `HasFunction`, `GetFunctionNames`, `RemoveFunction`, and the `FunctionRegistered` / `FunctionUnregistered` callbacks.

## Interface Implementations

The SDK requires you to provide implementations of three interfaces. Example implementations are included in the SDK: `ExampleLogger`, `ExampleHttpClient` and `ExampleNetworkClient` in `example_logger.h`, `example_http_client.h` and `example_network_client.h`. `ExampleNetworkClient` needs OpenSSL; `ExampleHttpClient` uses WinHTTP on Windows, and elsewhere needs OpenSSL and `CPPHTTPLIB_OPENSSL_SUPPORT` defined.

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
    // Open a TLS connection to host:port.
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

## Advanced Configuration

### Timeouts

<!-- ikon-example: cpp-sdk-timeouts -->
```cpp
config.timeouts.connectionTimeoutSec = 30;    // Connection timeout
config.timeouts.provisioningTimeoutSec = 60;  // Server startup timeout
config.timeouts.maxReconnectAttempts = 6;     // Max reconnection attempts
config.timeouts.reconnectBackoffMs = 500;     // Initial backoff (ms), doubled on each attempt
```

In API-key mode the `/init` request that waits up to `provisioningTimeoutSec` is itself sent with an HTTP timeout of `connectionTimeoutSec`, so raise `connectionTimeoutSec` to at least `provisioningTimeoutSec` for a longer provisioning wait to take effect.

### Reconnection

When a connection the client joined drops, the client reconnects by itself. It waits `reconnectBackoffMs` before the first attempt and twice as long before each next one. After `maxReconnectAttempts` failed attempts it goes `Offline`, and only a new `Connect()` connects it again.

Each attempt first tries to resume the session the client had. It opens a new connection to the same server address with that session's auth ticket and does not ask for a new session, so the app still has the client's session and what it keeps for it. The server holds a dropped session for its soft-retry window: 5 minutes by default, or the time the server sends in `AuthResponse.SoftRetryWindowMs`. The attempt starts a new session instead, as `Connect()` does, when the window has passed, or when the server closes the connection before the client has joined, which is how it refuses a session it no longer holds. The server then ends the old session. Once the server has answered a request for a new session, the later attempts start new sessions too.

An attempt that cannot open a connection at all, as while the network is down, costs the server nothing. During the first half of the attempts, the next attempt tries to resume the session again. From the second half on, such an attempt also asks for a new session, so a server that has gone away is replaced. A request for a new session that reaches nothing leaves the old session held, and the next attempt tries to resume it again. With the default 6 attempts and 500 ms backoff, the attempts run about 0.5, 1.5, 3.5, 7.5, 15.5 and 31.5 seconds after the drop, so a client whose network comes back within about 15 seconds returns to the session it had.

A resumed join ends the same way as a new one: the client announces its shared functions, reports ready, and `Ready` runs.

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
| `ConnectionState` | Enum: `Idle`, `Connecting`, `Connected`, `Reconnecting`, `Offline` |
| `FunctionRegistry` | Registry for local and shared (cross-client) functions |
| `FunctionVisibility` | Enum: `Local`, `Shared` |

### Configuration Types

| Type | Description |
|------|-------------|
| `LocalConfig` | Configuration for local server development |
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

This SDK is licensed under the Ikon AI SDK License. See `LICENSE` for details.

## Support

For issues and feature requests, contact IKON support or open an issue on GitHub.
