<!-- checked-against: 202405aaf691273de3b0c442 -->

# Ikon AI C++ SDK

The Ikon AI C++ SDK provides a way to connect to Ikon AI App from C++ applications. It is a header-only library requiring C++17, and every example below compiles as C++17.

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

    client.Ready = [&client]()
    {
        std::cout << "Connected!" << std::endl;
        client.SignalReady();
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

Use this for programmatic access to Ikon AI App. Get your API key from the Ikon portal.

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

### Local Development

Connect directly to a local Ikon server during development.

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
| `Connected` | Fully connected and ready |
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

// Connection established and ready, after Connect() and after every reconnect
client.Ready = [&client]()
{
    // Perform initialization here
    client.SignalReady();  // Signal that this client is ready (mandatory)
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

### Accessing Client State

<!-- ikon-example: cpp-sdk-client-state -->
```cpp
// Access the client configuration
const IkonClientConfig& config = client.GetConfig();

// Access the global state (available after connection).
// The returned pointer aliases the plugin's live GlobalState, which the
// network thread can replace at any time. Only safe to read from the
// receive/network thread; otherwise use SnapshotGlobalState() for a copy.
const GlobalState* state = client.GetGlobalState();
if (state)
{
    // Use global state...
}

// Thread-safe snapshot of the current GlobalState. Returns std::nullopt
// before Connect(), after Disconnect(), and when a Connect() from Idle or a
// reconnect attempt fails at its auth/token HTTP request. A Connect() retried
// from Offline keeps the earlier attempt's state, and after a later failure or
// a dropped connection it returns an empty or stale state.
std::optional<GlobalState> snapshot = client.SnapshotGlobalState();
```

## Sending Messages

`SendMessage` queues the message for a send thread and returns. `Disconnect()` closes the connection without waiting for that queue to drain, so a message still queued when it is called is dropped.

### Raw Protocol Messages

<!-- ikon-example: cpp-sdk-send-raw -->
```cpp
// Wrap a payload in a ProtocolMessage yourself and send it as is
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

`FunctionRegistry` lets a client register functions for the app to call and call the functions the app registers; the server offers a client's functions to the app only, not to other clients. The client owns one and attaches it itself on connect; get it with `client.Functions()`. A `Shared` function is announced only when registered while connected, and is not re-announced after a reconnect, so register it in `Ready` as below. It handles function-related protocol messages through its own message handler, independent of `MessageReceived`.

<!-- ikon-example: cpp-sdk-shared-functions -->
```cpp
auto& registry = client.Functions();

client.Ready = [&]()
{
    // Register a function the app can call
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

    client.SignalReady();
};

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
        // is true when connecting to a local Ikon server, whose certificate is self-signed, and
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

### Protocol Options

<!-- ikon-example: cpp-sdk-protocol-options -->
```cpp
// Filter which message types to receive/send
config.opcodeGroupsFromServer = Opcode::GROUP_ALL;
config.opcodeGroupsToServer = Opcode::GROUP_ALL;

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
| `IkonClient` | Main client class for connecting to Ikon servers |
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

For issues and feature requests, contact Ikon support or open an issue on GitHub.
