<!-- checked-against: af164070b67cb556e3b0c442 -->

# IKON C++ SDK

The IKON C++ SDK connects C++ applications to an IKON app. It is a header-only library requiring C++17, and every example below compiles as C++17.

## Features

- Two authentication modes: API Key, Local Development
- Connection state management with callbacks
- Protocol message sending and receiving
- Function registry for registering and calling shared cross-client functions, including streams in both directions, cancellation and binary values
- Peer events: other clients joining, becoming ready, changing their context and leaving
- Reactive subscriptions to values the app holds
- A headless UI layer: the app's UI trees, kept current, with their controls' actions to call
- File upload through the app's upload controls, resumed across reconnects
- Client context updates (viewport, theme, timezone, visibility) and path changes
- Audio and video sent and received as encoded frames (Opus, H.264, VP8)
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

The SDK sends and receives media as encoded frames. It does not encode or decode them. Audio is Opus; video is H.264 (Annex B), VP8 or VP9. A client's audio and video reach the app only.

<!-- ikon-example: cpp-sdk-media -->
```cpp
// Media the app sends this client, as encoded frames. Each stream id is "{senderId}_{trackId}".
// These run on the receive thread: hand the frames to a decoder, do not call the app from here
client.AudioStreamBegan = [](const std::string& streamId, int32_t senderId, const AudioStreamBegin& begin)
{
    std::cout << "Audio " << streamId << ": " << begin.SampleRate << " Hz, " << begin.Channels << " ch" << std::endl;
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

// Send 20 ms Opus packets at 48 kHz mono. The correlation id ties the audio to what the app
// asked for, such as a press of a talk button
const int packets = 50;

for (int i = 0; i < packets; i++)
{
    client.SendAudioFrame("mic", EncodeNextOpusPacket(), 48000, 1, 960, i == 0, i == packets - 1, "talk-1");
}

client.EndAudioStream("mic");

// The app asks for a keyframe when it starts showing the stream or loses frames
auto keyFrameWanted = std::make_shared<std::atomic<bool>>(true);
client.VideoKeyFrameRequested = [keyFrameWanted](const std::string& streamId, int32_t requesterId)
{
    *keyFrameWanted = true;
};

// Send H.264 (Annex B) frames at 30 fps. A change of size, codec or correlation id announces
// the stream again
for (int i = 0; i < 90; i++)
{
    bool isKey = keyFrameWanted->exchange(false);
    client.SendVideoFrame("camera", EncodeNextH264Frame(isKey), isKey, std::chrono::microseconds(i * 33333),
                          VideoCodec::H264, 640, 480);
}

client.EndVideoStream("camera");
```

`SendAudioFrame` takes a sample rate of 8000, 12000, 16000, 24000 or 48000 Hz and 1 or 2 channels, fixed by the stream's first packet; another format throws `std::invalid_argument`, so start a new stream id for it. `isFirst` and `isLast` mark a segment such as one utterance. The app's speech recognition ignores audio without a correlation id, and a new correlation id on a first packet announces the stream again. `SendVideoFrame`'s `sourceType` (`"camera"` by default, or `"screen"`) sets the kind of video input the app sees. Send a keyframe first, because a receiver shows nothing until one arrives. Both throw `std::runtime_error` when the client is not connected, and media sent before the client is ready is dropped. A reconnect announces every open stream again.

Incoming streams end with `AudioStreamEnded` and `VideoStreamEnded`, which also run for every open stream on `Disconnect()` and on a reconnect into a new session. `RequestVideoKeyFrame(streamId)` asks the sender of an incoming video stream for a keyframe, and returns false when that stream is not open or the client is not connected.

## Peer Events

Peer events tell this client about the other clients in the session and about the app itself. Each handler gets the peer's `Context`: its `SessionId`, `UserId`, `ContextType`, and what it reports of its viewport, theme, timezone and visibility.

<!-- ikon-example: cpp-sdk-peer-events -->
```cpp
// Set before Connect(): on joining, PeerJoined runs for each client already there
client.PeerJoined = [](const Context& peer)
{
    std::cout << "Joined: " << peer.SessionId << " (" << peer.UserId << ")" << std::endl;
};
client.PeerReady = [](const Context& peer)
{
    std::cout << "Ready: " << peer.SessionId << std::endl;
};
client.PeerUpdated = [](const Context& peer)
{
    std::cout << "Updated: " << peer.SessionId << " now uses the " << peer.Theme << " theme" << std::endl;
};
client.PeerLeft = [](const Context& peer)
{
    std::cout << "Left: " << peer.SessionId << std::endl;
};

client.Connect();
```

On joining, `PeerJoined` runs for each client already there. The app might not answer a new client before its `PeerReady`. `PeerUpdated` runs when a peer reports a new viewport, theme, timezone or visibility, and is read from the peer's context in each `GlobalState` the server sends. The events are never about this client, and a lifecycle batch the server sends is reported event by event. They run on the receive thread, so a handler should return quickly and must not wait for a remote call's result. Set them before `Connect()`.

## Reactive Subscriptions

`client.Reactives()` follows values the app holds in a `Reactive<T>`, `ClientReactive<T>`, `UserReactive<T>` or `MountReactive<T>` without rendering its UI. The app hands out a value's stable id (`Reactive<T>.StableId`), for example from one of its functions.

<!-- ikon-example: cpp-sdk-reactive -->
```cpp
// The app hands out the stable id of its Reactive<int>, here through a function of its own
std::string stableId = client.Functions().CallRemote("get_score_stable_id").get<std::string>();

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
    return node.type == "std.button" && node.GetText("label") == std::optional<std::string>("Save");
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

The SDK calls `SetVerifyServerCertificate` before each `Connect`: `true` (the default) for the cloud, and `false` only when `IkonClientConfig::local` is set, because a local IKON server's certificate is self-signed. `ExampleNetworkClient` verifies the chain against the system's trusted roots (the Windows root store on Windows, OpenSSL's default paths elsewhere) and checks the host name or IP address; when verification fails, `Connect` throws `std::runtime_error` naming the host and the reason.

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
| `RemoteStream` | The items of a remote function that returns a stream, from `CallRemoteStream` |
| `FunctionStreamWriter` | What a handler registered with `RegisterStreamFunction` writes its items to |
| `FunctionCallCancelledException` | Thrown by the future of a call cancelled with `CancelRemoteCall` |
| `ClientContextUpdate` | The context values `UpdateClientContext` changes; empty fields keep their value |
| `ReactiveRegistry` | Follows the app's reactive values, from `client.Reactives()` |
| `ReactiveSubscription` | One callback following one reactive value, from `Subscribe` |
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
