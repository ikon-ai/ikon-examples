<!-- checked-against: b86fcb916fc2b96be3b0c442 -->

# IKON Rust SDK

The IKON Rust SDK (the `ikon-sdk` crate) connects a Rust program to an IKON app: a device, a bot, a test harness or a service that joins the app's session as a client. It runs on tokio.

## Features

- Two authentication modes: API Key and Local Development
- Automatic reconnection with backoff, into the same session while the server holds it
- Calling the app's functions, including streamed results, cancellation and binary arguments and results
- Functions the app can call on the client, blocking or async
- Following the app's reactive values
- The app's UI as a tree of nodes, with its controls' actions to call, without rendering it
- File uploads through the app's upload controls, resuming across reconnects
- Events for other clients joining, changing their context and leaving
- Client context (viewport, theme, timezone, visibility) and path updates, as a browser reports them
- Sending and receiving encoded audio (Opus) and video (H.264, VP8, VP9), with key frame requests
- Low-level protocol message access

## Installation

The crate is not on crates.io. Depend on it by path from a checkout of the platform repository:

```toml
[dependencies]
ikon-sdk = { path = "../ikon-platform/platform-rust/ikon-sdk/crates/ikon-sdk" }
tokio = { version = "1", features = ["full"] }
serde_json = "1"
```

The protocol types the SDK uses are re-exported as `ikon_sdk::protocol`, so they need no dependency of their own.

## Connecting

### API Key Authentication

A program joins a deployed app with an API key for the app's space. `BackendType::Development` is for an app on `*.dev.ikonai.app`.

<!-- ikon-example: rust-sdk-connect -->
```rust
let config = IkonClientConfig::with_api_key(ApiKeyConfig {
    api_key,
    space_id,
    external_user_id: "my-device".to_string(),
    session_identity_hash: None,
    backend: BackendType::Production,
    user_type: UserType::Machine as i32,
    join_only: false,
    client_type: ClientType::DesktopApp as i32,
});

let client = IkonClient::new(config)?;
let mut messages = client.events().message_received.subscribe();
client.connect().await?;

if let Ok(message) = messages.recv().await {
    println!("received opcode {}", message.opcode());
}

client.disconnect().await?;
```

`connect` returns once the client is in the session and the app knows it, so code after it can call the app at once. `join_only: true` with `UserType::Machine` joins only a session that is already running and does not keep it open; the connect then fails with `SdkError::NoLiveSession` when nothing runs.

### Local Development

Against `ikon run` on the same machine, the client connects to the local server, whose self-signed certificate it accepts:

<!-- ikon-example: rust-sdk-connect-local -->
```rust
let config = IkonClientConfig::with_local(LocalConfig {
    host: "localhost".to_string(),
    https_port: 8443,
    user_id: None,
});

let client = IkonClient::new(config)?;
client.connect().await?;
println!(
    "joined as session {:?}",
    client.client_context().await.map(|c| c.session_id)
);
client.disconnect().await?;
```

### Connection Lifecycle

`client.events()` is an `EventBus` of tokio broadcast channels. Subscribe before `connect` to see everything:

| Event | When |
|---|---|
| `state_changed` | `Connecting`, `Connected`, `Reconnecting`, `Offline`, `Idle` |
| `ready` | each join, once the app knows the client: before `connect` returns, and after every reconnect |
| `message_received` | every protocol message from the server |
| `media` | audio and video from the app, and key frame requests (see Media) |
| `ui` | a UI stream changed or ended (see UI) |
| `peers` | another client joined, became ready, changed its context or left (see Other Clients) |
| `disconnected`, `stopping`, `error_occurred` | the link dropped, the server is stopping, a connect or reconnect failed |

When a joined link drops, the client reconnects on its own, `timeouts.max_reconnect_attempts` times (0 turns it off) with a doubling `timeouts.reconnect_backoff`. It goes `Offline` when the attempts run out or the server said it was stopping. `disconnect` leaves the session and goes `Idle`.

`client.clients()` lists the session's clients as the server describes them, this one included, and `wait_for_client(ClientPredicate { user_id: Some(..), ..Default::default() }, timeout)` waits for one to join.

### Other Clients

`peers` reports the other clients of the session, never this one:

<!-- ikon-example: rust-sdk-peers -->
```rust
let mut peers = client.events().peers.subscribe();

while let Ok(event) = peers.recv().await {
    match event {
        PeerEvent::Joined(peer) => println!("{} is joining", peer.session_id),
        PeerEvent::Ready(peer) => println!("{} ({}) is here", peer.session_id, peer.user_id),
        PeerEvent::Updated(peer) => println!("{} switched to {}", peer.session_id, peer.theme),
        PeerEvent::Left(peer) => println!("{} left", peer.session_id),
    }
}
```

`Joined` comes first, also for each client already in the session when this one joined. The app might not answer a client before its `Ready`. `Updated` carries the new viewport, theme, timezone or visibility. After a reconnect into the same session `clients()` lists the session as it is then, with no events for the clients that came or went while this one was away. A reconnect into a new session is a new join, so `peers` reports the clients there as it does on joining. When presence is off for the app's surface, a client is told only about the app's own connections.

## Functions

A `FunctionRegistry` attached to the client knows the app's functions and the ones the program registers. Attach it before `connect`, so the join registers the program's functions before the app learns that the client joined.

### Calling App Functions

<!-- ikon-example: rust-sdk-call-function -->
```rust
let functions = FunctionRegistry::new();
functions.attach(Arc::clone(&client)).await?;
client.connect().await?;

let sum = functions
    .call_remote(
        "Add",
        serde_json::json!([2, 3]),
        "",
        Duration::from_secs(30),
    )
    .await?;
println!("2 + 3 = {sum}");
```

The arguments are a JSON array. A call made before the client is ready waits for it, within the timeout. A function the app does not have fails with `SdkError::FunctionNotFound`, an exception the app throws with `SdkError::RemoteFunctionError`, and a call whose timeout runs out with `SdkError::Timeout`; a call already sent is then cancelled on the app. Dropping the call's future cancels it on the app too, which a C# function taking a `CancellationToken` sees.

### Streamed Results

A function that returns `IAsyncEnumerable<T>` in C# is called with `call_remote_stream`. Its items arrive as they are produced; the timeout bounds the wait for each one. Dropping the stream, or `cancel`, stops the enumeration on the app.

<!-- ikon-example: rust-sdk-stream-function -->
```rust
let mut count = functions
    .call_remote_stream(
        "CountTo",
        vec![serde_json::json!(10).into()],
        "",
        Duration::from_secs(30),
    )
    .await?;

while let Some(item) = count.next().await {
    let number = item?.into_json()?;

    if number == 3 {
        // Dropping the stream does the same
        count.cancel().await;
        break;
    }
}
```

`FunctionStream` also implements `futures_core::Stream`, for use with stream combinators.

### Binary Arguments and Results

`call_remote_values` takes and returns `FunctionValue`s, so a C# `byte[]` travels as raw bytes rather than JSON:

<!-- ikon-example: rust-sdk-binary-function -->
```rust
let thumbnail = functions
    .call_remote_values(
        "MakeThumbnail",
        vec![FunctionValue::Bytes(image), serde_json::json!(128).into()],
        "",
        Duration::from_secs(30),
    )
    .await?;

if let Some(png) = thumbnail.as_bytes() {
    println!("thumbnail of {} bytes", png.len());
}
```

`FunctionValue::Json` holds any other value. Read through `call_remote`, a `byte[]` result arrives as the base64 string C# would write.

### Functions the App Calls

The app sees a function the program registers as `Shared` and calls it with the client's session as the target. A blocking handler runs on tokio's blocking pool; an async handler runs on a task of its own, so it may call the app back. Neither holds up the client's messages. When the app cancels the call, an async handler's future is dropped.

<!-- ikon-example: rust-sdk-register-function -->
```rust
functions
    .register_function(
        "GetBatteryLevel",
        "The device's battery level in percent",
        |_args| Ok(serde_json::json!(87)),
        Vec::new(),
        "System.Int32",
        FunctionVisibility::Shared,
        "",
    )
    .await?;

// An async handler may await, here a call back to the app
let app = Arc::clone(&functions);
functions
    .register_async_function(
        "Greet",
        "Greets a person by the name the app has for them",
        move |args| {
            let app = Arc::clone(&app);
            async move {
                let user_id = args[0].clone();
                let name = app
                    .call_remote(
                        "GetName",
                        serde_json::json!([user_id]),
                        "",
                        Duration::from_secs(30),
                    )
                    .await?;
                Ok(serde_json::json!(format!(
                    "Hello, {}!",
                    name.as_str().unwrap_or("you")
                )))
            }
        },
        vec![FunctionParameterInfo {
            name: "userId".to_string(),
            type_name: "System.String".to_string(),
            ..FunctionParameterInfo::default()
        }],
        "System.String",
        FunctionVisibility::Shared,
        "",
    )
    .await?;
```

The handler receives the arguments as a JSON array; a `byte[]` argument arrives as a base64 string. `remove_function` removes one, and `detach` removes the registry from the client.

## Reactive Values

A program can follow a value the app holds in a `Reactive<T>` (or `ClientReactive<T>`, `UserReactive<T>`, `MountReactive<T>`) without rendering the app's UI. The app hands out the reactive's stable id, for example from a function that returns `Reactive<T>.StableId`.

<!-- ikon-example: rust-sdk-reactive -->
```rust
let reactive = ReactiveRegistry::attach(&client, &functions).await?;

let score = reactive
    .subscribe(&stable_id, "", |score: i64| {
        println!("score is now {score}")
    })
    .await?;

// Later, to stop following it
score.unsubscribe().await;
```

The callback gets the current value before `subscribe` returns, then every change, in the order the app made them. After a reconnect the registry subscribes again and delivers the value as it is then. Pass the mount id for a `MountReactive<T>`, or `""`.

## Client Context

A browser tells the app its viewport, theme, timezone and visibility, and the path it shows. A program does the same:

<!-- ikon-example: rust-sdk-client-context -->
```rust
client
    .update_client_context(ClientContextUpdate {
        viewport_width: Some(1280),
        viewport_height: Some(720),
        theme: Some("dark".to_string()),
        ..ClientContextUpdate::default()
    })
    .await?;

client.set_path("/rooms/7").await?;
```

A value left `None` keeps the one last sent. The app's client-updated and path handlers run. Both are sent again after every reconnect.

## UI

A client receives the app's UI as a tree of nodes, as the web client does, and can use its controls without drawing anything. `client.ui()` holds one `UiStream` for each mount the app shows the client; `page()` is the app's page (mount `PAGE_MOUNT`, "ikon-ui"). The SDK keeps each tree up to date from the server's updates and acknowledges them, and asks for the whole tree again when an update cannot be applied or after a reconnect.

A `UiNode` has its component type (`"std.button"`), its props as JSON, its children, and `actions`: each prop whose name ends in "Id" and holds a GUID. A button's press is `onInvokeId`, called with `"{}"`; a text field's new value is `onValueChangeId`, called with the text itself:

<!-- ikon-example: rust-sdk-ui-actions -->
```rust
let ui = client.ui();
let timeout = Duration::from_secs(30);

let button = ui
    .wait_for_node(
        |node| node.text("label").as_deref() == Some("Start"),
        timeout,
    )
    .await?;
ui.call_node_action(&button, "onInvokeId", "{}", timeout)
    .await?;

// A text field's new value is the text itself, not JSON
let field = ui
    .wait_for_node(|node| node.action_id("onValueChangeId").is_some(), timeout)
    .await?;
ui.call_node_action(&field, "onValueChangeId", "Helsinki", timeout)
    .await?;

let status = ui
    .wait_for_node(
        |node| {
            node.text("content")
                .is_some_and(|text| text.starts_with("Weather in Helsinki"))
        },
        timeout,
    )
    .await?;
println!("{:?}", status.text("content"));
```

`wait_for_node` returns at once when a node already matches, and otherwise waits for the update that adds one. `call_node_action` returns whether the app's handler completed; `false` means the app refused the call, for example for a control that has since been removed from the UI. A call made while the client reconnects waits for it, as a function call does.

Nodes never change: an update replaces the nodes on the changed path, so a node read earlier keeps its old props. The `ui` event delivers each stream as it changes:

<!-- ikon-example: rust-sdk-ui-events -->
```rust
let mut ui_events = client.events().ui.subscribe();

while let Ok(event) = ui_events.recv().await {
    match event {
        UiEvent::StreamUpdated(stream) => {
            let nodes = stream
                .root
                .iter()
                .flat_map(|root| root.descendants_and_self())
                .count();
            println!("{} v{}: {nodes} nodes", stream.category, stream.version);
        }
        UiEvent::StreamEnded(stream) => println!("{} ended", stream.stream_id),
    }
}
```

Moving onto a new session ends every stream. `styles()` holds the CSS the nodes' `style_ids` refer to, and a stream's `payloads` the binary values its tree names, such as an image's bytes. A client that leaves `Opcode::GROUP_UI` out of `opcode_groups_from_server` gets no UI.

## File Uploads

`upload_file` sends a file through one of the app's upload controls, as a person choosing a file there does. It takes the control's `uploadActionId` prop and any `AsyncRead + AsyncSeek` source:

<!-- ikon-example: rust-sdk-upload -->
```rust
let control = client
    .ui()
    .wait_for_node(
        |node| node.text("aria-label").as_deref() == Some("Attach report"),
        Duration::from_secs(30),
    )
    .await?;
let upload_action_id = control
    .text("uploadActionId")
    .ok_or_else(|| SdkError::InvalidArgument("not an upload control".into()))?;

let file = tokio::fs::File::open("report.pdf").await?;
let options = UploadOptions {
    mime_type: Some("application/pdf".to_string()),
    progress: Some(Arc::new(|progress| {
        println!(
            "{} of {} bytes",
            progress.acknowledged_bytes, progress.total_bytes
        );
    })),
    ..UploadOptions::default()
};

match client
    .upload_file(&upload_action_id, file, "report.pdf", options)
    .await
{
    Ok(result) => println!("stored as {} ({})", result.upload_id, result.hash),
    Err(SdkError::UploadFailed {
        message, retriable, ..
    }) => println!("failed: {message}; worth trying again: {retriable}"),
    Err(other) => return Err(other),
}
```

It returns once the app's `onUploadComplete` has run, with the SHA-256 the app checked. If the connection drops during an upload and the client reconnects within 45 seconds, the upload resumes from the last byte the server acknowledged. `UploadFailed::retriable` says whether sending the same file again is likely to work: true after a stall or a lost connection, false when the app refused the file or may already have stored it. At most 8 uploads per client run at once; later calls wait their turn.

## Media

The SDK sends and receives encoded frames. The program does the encoding and decoding. A client's audio and video reach the app only. Frames sent while the client is still joining are dropped.

### Sending Audio

Each frame is one Opus packet, 20 ms (960 samples per channel at 48 kHz) unless `samples_per_channel` says otherwise:

<!-- ikon-example: rust-sdk-send-audio -->
```rust
let last = opus_packets.len().saturating_sub(1);

for (index, packet) in opus_packets.iter().enumerate() {
    let options = AudioFrameOptions {
        is_first: index == 0,
        is_last: index == last,
        correlation_id: Some("utterance-1".to_string()),
        ..AudioFrameOptions::default()
    };
    client
        .send_audio_frame("microphone", packet, &options)
        .await?;
    tokio::time::sleep(Duration::from_millis(20)).await;
}
```

The first frame announces the stream; `is_first` starts a segment, which the app sees as a new utterance, and `is_last` marks the end of one. A new `correlation_id` on an `is_first` frame announces the stream again under that id; on any other frame it is ignored.

### Sending Video

Each frame is one encoded H.264 access unit or VP8/VP9 frame:

<!-- ikon-example: rust-sdk-send-video -->
```rust
let frame_time = Duration::from_millis(33);

for (number, (h264, is_key)) in frames.iter().enumerate() {
    let options = VideoFrameOptions {
        codec: VideoCodec::H264,
        width: 1280,
        height: 720,
        is_key: *is_key,
        timestamp: frame_time * number as u32,
        duration: frame_time,
        ..VideoFrameOptions::default()
    };
    client.send_video_frame("camera", h264, &options).await?;
}

client.end_video_stream("camera").await?;
```

The stream is announced before its first frame and again when its size, codec, source type, correlation id or frame rate changes. When the app needs a key frame, `MediaEvent::VideoKeyFrameRequested` names the stream; encode the next frame as one.

### Receiving Media

<!-- ikon-example: rust-sdk-receive-media -->
```rust
let mut media = client.events().media.subscribe();

while let Ok(event) = media.recv().await {
    match event {
        MediaEvent::AudioFrame { stream_id, frame } => {
            println!("{stream_id}: {} bytes of Opus", frame.samples.len());
        }
        MediaEvent::VideoStreamBegan { stream_id, .. } => {
            // Ask for a key frame to start decoding from
            client.request_video_key_frame(&stream_id).await?;
        }
        MediaEvent::VideoKeyFrameRequested { stream_id, .. } => {
            println!("encode the next frame of {stream_id} as a key frame");
        }
        _ => {}
    }
}
```

A received stream's id is `"{sender session id}_{track id}"`. `request_video_key_frame` asks the sender of a video stream for a key frame, and returns `false` for a stream that has not begun or has ended.

## Raw Protocol Messages

`client.send_message(&message)` sends a `ProtocolMessage`, and `message_received` delivers every message from the server. `ProtocolMessage::encode` builds one from a type in `ikon_sdk::protocol`, and `decode_payload::<T>()` reads one.

## Configuration

`IkonClientConfig::timeouts` holds `connection` (the join), `provisioning` (waiting for the app's server to start), `max_reconnect_attempts` and `reconnect_backoff`. `description`, `product_id`, `version_id`, `user_agent` and `locale` describe the client to the app.
