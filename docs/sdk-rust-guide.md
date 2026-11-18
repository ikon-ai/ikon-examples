<!-- checked-against: 4318f750fe0b46dde3b0c442 -->

# IKON Rust SDK

The IKON Rust SDK (the `ikon-sdk` crate) connects a Rust program to an IKON app: a device, a bot, a test harness or a service that joins the app's session as a client. It runs on tokio.

## Features

- Five ways to connect: an API key, local development, a guest, a user token and a connect URL
- Automatic reconnection with backoff, into the same session while the server holds it
- Calling the app's functions, including streamed results, cancellation and binary arguments and results
- Functions the app can call on the client, blocking or async
- Following the app's reactive values
- The app's UI as a tree of nodes, with its controls' actions to call, without rendering it
- File uploads through the app's upload controls, resuming across reconnects
- Events for other clients joining, changing their context and leaving
- Client context (viewport, theme, timezone, visibility) and path updates, as a browser reports them
- Sending and receiving encoded audio (Opus) and video (H.264, VP8, VP9), with key frame requests and playback reports
- Sending and receiving the app's own message types, generated from its schema

## Installation

The SDK is on crates.io as `ikon-sdk`:

```bash
cargo add ikon-sdk serde_json
cargo add tokio --features full
```

The crates need Rust 1.89 or newer. Each platform release publishes `ikon-sdk`, `ikon-protocol` and `ikon-teleport` together at the same new version: a minor version, or a patch version for a hotfix.

The protocol types the SDK uses are re-exported as `ikon_sdk::protocol`, so they need no dependency of their own. The example of a client function that streams also uses `futures-util = "0.3"`.

Run `cargo doc -p ikon-sdk -p ikon-protocol --no-deps --open` in the program's own crate to build the SDK's reference, with the doc comment of every public type, method and error. `ikon-protocol` holds the protocol's messages and enums, such as `Context` and `UserType`; "Other Clients" below says which of `Context`'s fields a client event carries.

The examples below use these names. The SDK's own types are at the crate root; the protocol's enums and messages, such as `UserType`, `ClientType`, `VideoCodec` and `AudioPlaybackState`, are in `ikon_sdk::protocol`:

<!-- ikon-example: rust-sdk-imports -->
```rust
use std::sync::Arc;
use std::time::Duration;

use ikon_sdk::protocol::{AudioPlaybackState, ClientType, UserType, VideoCodec};
use ikon_sdk::{
    ApiKeyConfig, AudioFrameOptions, BackendType, ClientContextUpdate, ClientEvent,
    FunctionParameterInfo, FunctionRegistry, FunctionValue, FunctionVisibility, GuestConfig,
    IkonClient, IkonClientConfig, LocalConfig, MediaEvent, ReactiveRegistry, SdkError, UiEvent,
    UploadOptions, UserTokenConfig, VideoFrameOptions,
};
```

## Connecting

### API Key Authentication

A program joins a deployed app with an API key for the app's space. `BackendType::Development` is for an app served on `*.dev.ikonai.app`, and the client then calls the development backend, `https://api.dev.ikon.live` (`BackendType::base_url`); `BackendType::Production` is for one on `*.ikonai.app` and calls `https://api.prod.ikon.live`.

<!-- ikon-example: rust-sdk-connect -->
```rust
let config = IkonClientConfig::with_api_key(ApiKeyConfig {
    api_key,
    space_id,
    external_user_id: "my-device".to_string(),
    session_identity_hash: None,
    backend: BackendType::Production,
    user_type: UserType::Machine,
    join_only: false,
    client_type: ClientType::DesktopApp,
});

let client = IkonClient::new(config)?;
let mut messages = client.events().message_received.subscribe();
client.connect().await?;

if let Ok(message) = messages.recv().await {
    println!("received opcode {}", message.opcode());
}

client.disconnect().await?;
```

`IkonClient::new` returns `Result<Arc<IkonClient>>`, and the client's methods take `self: &Arc<Self>`, so a function of the program's own takes the client as `fn f(client: &Arc<IkonClient>)`, and a spawned task gets `Arc::clone(&client)`.

`connect` returns once the client is in the session and the app knows it, so code after it can call the app at once. `join_only: true` with `UserType::Machine` joins only a session that is already running and does not keep it open; the connect then fails with `SdkError::NoLiveSession` when nothing runs.

### Local Development

Against `ikon run` on the same machine, the client connects to the local server, whose self-signed certificate it accepts. `ikon run` prints the server's URL, and also writes it as `serverUrl` in `build/app/artifacts/bin/<App>/debug/ikon-server-info.json` in the app's folder. The port can change between runs: a run takes the app's port from its last run while that port is free, and otherwise the first free one from 8443, so another app's run can hold the port your app had. Read the port again before each new client:

<!-- ikon-example: rust-sdk-connect-local -->
```rust
// The run's address, which can change between runs
let info: serde_json::Value = serde_json::from_str(&std::fs::read_to_string(
    "../my-app/build/app/artifacts/bin/MyApp/debug/ikon-server-info.json",
)?)?;
let https_port = info["serverUrl"]
    .as_str()
    .and_then(|url| url.rsplit_once(':'))
    .and_then(|(_, port)| port.parse().ok())
    .unwrap_or(8443);

let config = IkonClientConfig::with_local(LocalConfig {
    https_port,
    // Refuses a port that now serves another app's run
    space_id: Some(app_id),
    // localhost, user "local", UserType::Machine, ClientType::DesktopApp
    ..LocalConfig::default()
});

let client = IkonClient::new(config)?;
client.connect().await?;
println!(
    "joined as session {:?}",
    client.client_context().await.map(|c| c.session_id)
);
client.disconnect().await?;
```

`space_id` is the app's id (`[Target] SpaceId` in the app's `ikon-config.<environment>.toml`). With it set, a connect to a server that runs another app fails with `SdkError::WrongLocalApp`, which names both app ids, and the client does not join that app. Each reconnect checks again, so a client whose run was restarted on another port does not reconnect into the app that took the old one. Without `space_id` the client joins whatever app holds the port. A client keeps its port for its whole life; after a new `ikon run`, make a new `IkonClient` with the port the run printed.

The app sees a local client as `user_id` (`"local"` when it is `None`), `user_type` (`UserType::Machine` unless set) and `client_type` (`ClientType::DesktopApp` unless set). Set `user_type: UserType::Human` for a client that acts for a person. The app's own connections also use the user `local`, so a program tells them apart by `is_internal` (see Other Clients), not by the user.

### Guests

A client can join a deployed app as a signed-out visitor, as a browser that has not signed in does. The app must let guests in; otherwise the connect fails with `SdkError::HttpStatus` 403.

<!-- ikon-example: rust-sdk-connect-guest -->
```rust
let mut config = IkonClientConfig::with_guest(GuestConfig {
    space_id,
    backend: BackendType::Production,
    client_type: ClientType::DesktopApp,
});
// The guest user belongs to the device id, so keep it to come back as the same guest
config.device_id = Some(saved_device_id());

let client = IkonClient::new(config)?;
client.connect().await?;
```

Each device id gets a guest user of its own, unless the app's guest sign-in puts every guest in one shared user. The app sees a guest as `UserType::Human`.

A guest has no token to renew. The client asks for a new guest token on every `connect` and on every reconnect that joins a new session; a reconnect that resumes the dropped session needs none. When the backend or the app refuses the guest (401 or 403), a reconnect stops at once and the client goes `Offline` (see [Connection Lifecycle](#connection-lifecycle)); only a new `connect` tries again.

### User Tokens

A program that already holds a person's session token for the app, issued by another service, connects with it instead of an API key. `backend_url` is the backend that issued the token: `https://api.prod.ikon.live` (`BackendType::Production.base_url()`) for an app on `*.ikonai.app`, `https://api.dev.ikon.live` (`BackendType::Development.base_url()`) for one on `*.dev.ikonai.app`.

An API key's `external_user_id` names a user of the program's own: the same id is the same user on every connect, but it is never one of the app's existing people and has no email, so a role the app gives by email does not reach it. Acting as an existing person needs that person's session token in this mode, and the IKON tool hands out none; only the .NET SDK's `UserLogin` connects as the person signed in with `ikon login`.

<!-- ikon-example: rust-sdk-connect-user-token -->
```rust
let config = IkonClientConfig::with_user_token(UserTokenConfig {
    space_id,
    token,
    backend_url: BackendType::Production.base_url().to_string(),
    user_type: UserType::Human,
    client_type: ClientType::DesktopApp,
    join_only: false,
});

let client = IkonClient::new(config)?;

match client.connect().await {
    Err(ikon_sdk::SdkError::HttpStatus { status: 401, .. }) => {
        // The token expired: get a new one where this one came from, then connect again
        client.update_auth_token(fetch_new_token().await)?;
        client.connect().await?;
    }
    other => other?,
}
```

`SdkError` is `#[non_exhaustive]` because a later release can add variants. A `match` on it therefore needs a catch-all arm, such as the `other` arm here.

The client cannot renew the token itself. When the backend refuses it (401) or the app refuses the user (403), `connect` fails. During a reconnect, the client stops reconnecting at once, goes `Offline` and reports the refusal on `error_occurred`. `update_auth_token` sets the token that every later connect and reconnect uses. It fails for a client that was not configured with a user token.

### Connect URLs

A client can also join through a server's connect URL (`https://<server>/connect?token=...`) that a service it trusts created for it. It does not sign in at all.

<!-- ikon-example: rust-sdk-connect-url -->
```rust
let client = IkonClient::new(IkonClientConfig::with_connect_url(connect_url))?;
client.connect().await?;
```

A reconnect into a new session uses the same URL, so once its token has expired the server refuses it and the client goes `Offline`. Get a new URL and make a new `IkonClient`.

### Connection Lifecycle

`client.events()` is an `EventBus` of tokio broadcast channels. Subscribe before `connect` to see everything:

| Event | When |
|---|---|
| `state_changed` | `Connecting`, `Connected`, `Reconnecting`, `Offline`, `Idle` |
| `ready` | each join, once the app knows the client: before `connect` returns, and after every reconnect |
| `message_received` | every protocol message from the server |
| `media` | audio and video from the app, and key frame requests (see Media) |
| `ui` | a UI stream changed or ended (see UI) |
| `clients` | another client joined, changed its context or left (see Other Clients) |
| `disconnected`, `stopping`, `error_occurred` | the link dropped, the server is stopping, a connect or reconnect failed |

When a joined link drops, the client reconnects on its own, `timeouts.max_reconnect_attempts` times (0 turns it off) with a doubling `timeouts.reconnect_backoff`, each wait shortened at random by up to a quarter so that the clients one server dropped do not all come back at once. When the backend answers an attempt with `SdkError::RateLimited`, the next attempt waits at least its `retry_after`, capped at 5 minutes, plus up to half as long again. It goes `Offline` when the attempts run out or the server said it was stopping. It also goes `Offline` at once when retrying cannot help: a join-only client finds no live session, the release policy refuses the SDK's build (426), the backend says the app is not available (`SdkError::SpaceNotFound`, a 404 or 410 from the sign-in or `/init`, as for a suspended app), or, for a client that is not local, the backend or the server refuses the credential (401) or the app refuses the user (403). `disconnect` leaves the session and goes `Idle`. The HTTPS port is used only to get the session. The session itself runs over one TLS link to the TCP address the server gives when it grants the session, which on a local run is another port on the same machine. A proxy in front of the HTTPS port therefore never sees the session's traffic, and only that link dropping starts a reconnect.

`ikon stop` does not test a reconnect: the server says it is stopping (`stopping`) before it closes the link, so the client goes `Offline` and does not try again. The next `ikon run` can take another port, so a program that should keep going makes a new `IkonClient` with the port that run prints. The IKON tool has no command that drops one client's link on a local run.

An app hot reload is not a reconnect either. The client stays in the session, and the app's own connection leaves and comes back under a new session id, which registers the app's functions again. A call the old connection had not answered fails with `SdkError::TargetLeft`, a call made in between fails with `SdkError::FunctionNotFound` (`wait_for_function` waits for the function to be back), and later calls go to the new connection. Reactive subscriptions and the functions the program registered keep working.

`client.clients()` lists the session's clients as the server describes them, this one included, and `wait_for_client(ClientPredicate { user_id: Some(..), ..Default::default() }, timeout)` waits until the server lists one.

### Other Clients

`clients` reports the other clients of the session, never this one:

<!-- ikon-example: rust-sdk-clients -->
```rust
let mut events = client.events().clients.subscribe();

while let Ok(event) = events.recv().await {
    // The app's own connections, which come back under a new session id when the app hot-reloads
    if event.client().is_internal {
        continue;
    }

    match event {
        ClientEvent::Joined(other) => {
            let kind = if other.user_type == UserType::Machine {
                "a program"
            } else {
                "a person"
            };
            println!("{} ({}, {kind}) is here", other.session_id, other.user_id)
        }
        ClientEvent::Updated(other) => {
            println!("{} switched to {}", other.session_id, other.theme)
        }
        ClientEvent::Left(other) => println!("{} left", other.session_id),
    }
}
```

`Joined` comes once a client is ready, when the server has sent it the session's state and the app knows who it is, so the app can answer it from then on. It also comes for each client already in the session when this one joined; those arrive just after `connect` returns. `Updated` carries the new viewport, theme, timezone or visibility. `Updated` and `Left` come only for a client reported as joined, so a client that leaves before it is ready is never reported. After a reconnect into the same session, `clients` reports only what changed while this client was away: `Joined` for a client that came meanwhile, and `Updated` for one that changed its viewport, theme, timezone or visibility. A client that left meanwhile comes as `Left` once the server has sent the clients still there, and `clients()` lists the session as it is then. A reconnect into a new session is a new join, so `clients` reports the clients there as it does on joining. When presence is off for the app's surface, a client is told only about the app's own connections.

Each event carries the other client's `ikon_sdk::protocol::Context`, which `event.client()` returns. `is_internal` is true for the app's own connections: they are reported too, and leave and join again under a new `session_id` each time the app hot-reloads. Of the rest, another client's `Context` carries `session_id`, `user_id`, `description` (`IkonClientConfig::description` for an SDK client), `product_id`, `version_id`, `locale`, `is_anonymous`, `is_ready`, and the viewport, theme, timezone and visibility it reports. `user_type`, `client_type`, `context_type` and `sdk_type` have the types `UserType`, `ClientType`, `ContextType` and `SdkType`: compare with `UserType::Machine`. Each holds the wire value as `.0`, so a value a newer server sends that names no constant is kept, and `name()` returns `None` for it. `sdk_type` names the SDK the client runs on, and a web page joins as `ContextType::Browser`. The server leaves another client's device, sign-in, host and location fields empty; only the app sees them.

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

The arguments are a JSON array. A call made before the client is ready waits for it, within the timeout. One still waiting when the timeout runs out fails with `SdkError::NotConnectedInTime` and is never sent. One made while the client is not connecting fails at once with `SdkError::NotConnected`. A function the app does not have fails with `SdkError::FunctionNotFound`, and an exception the app throws with `SdkError::RemoteFunctionError`. A sent call whose timeout runs out fails with `SdkError::CallTimeout`, which names the function and the session it went to, and the call is then cancelled on the app. A sent call fails at once with `SdkError::Disconnected` when its link closes before the answer arrives, and with `SdkError::TargetLeft` when the app connection it went to leaves first, as on a hot reload. Whether the call ran is then unknown. Dropping the call's future cancels it on the app too, which a C# function taking a `CancellationToken` sees.

### Streamed Results

A function that returns `IAsyncEnumerable<T>` in C# is called with `call_remote_stream`. Its items arrive as they are produced; the timeout bounds the wait for each one, and a wait that runs out ends the stream with `SdkError::CallTimeout`. Dropping the stream, or `cancel`, stops the enumeration on the app.

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

The app sees a function the program registers as `Shared` and calls it with the client's session as the target. A blocking handler runs on tokio's blocking pool; an async handler runs on a task of its own, so it may call the app back. Neither holds up the client's messages. When the app cancels the call, or the app connection that made it leaves, an async handler's future is dropped.

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

The app reads the result as the type its call names, such as `CallAsync<Reading>`. A JSON object binds to a C# record or class by member name, case-insensitively, so `{"celsius": 21.5}` and `{"Celsius": 21.5}` both fill `Reading.Celsius`; a member the JSON leaves out keeps its default, without an error. The result type name (`"System.Int32"`, `"System.String"` above) is advertised with the function, and the app records it as the function's return type when that type is loaded there, else as `object`. It does not change how a call reads the result.

### Client Functions That Stream

`register_stream_function` registers a function whose handler returns a `futures_core::Stream` of `Result`s. The app calls it as an `IAsyncEnumerable<T>`, for example with `FunctionRegistry.CallAsyncEnumerable<T>` and the client's session as `targetId`, and reads each item the stream yields:

<!-- ikon-example: rust-sdk-client-stream-function -->
```rust
// The app reads it as IAsyncEnumerable<int>. When the app cancels, the stream is dropped
functions
    .register_stream_function(
        "CountDown",
        "Counts down from the number given to zero",
        |args| {
            let from = args[0].as_i64().unwrap_or(0);
            futures_util::stream::iter(
                (0..=from)
                    .rev()
                    .map(|n| Ok::<_, SdkError>(serde_json::json!(n))),
            )
        },
        vec![FunctionParameterInfo {
            name: "from".to_string(),
            type_name: "System.Int32".to_string(),
            ..FunctionParameterInfo::default()
        }],
        "System.Int32",
        FunctionVisibility::Shared,
        "",
    )
    .await?;
```

The handler is called when the app calls the function and must return at once; the stream is read on a task of its own. An item is anything that converts into a `FunctionValue`: JSON, or raw bytes for `FunctionValue::Bytes`. Items go in batches as the C# SDK sends them: an item waits at most 100 ms for others to join it, a batch holds at most 50, and an item of 1 KB or more goes at once. The stream's end ends the app's enumeration. The stream's first error fails the app's call with the error's text, after the items before it. The stream is dropped when the app cancels the call, when the app connection that called it leaves (as on a hot reload), and when the client reconnects into a new session or disconnects, because the app there no longer has the caller. When items cannot go out because the connection dropped, the stream stops, and the app's call fails once the client is back in the same session. The type name, `System.Int32` here, names the item type. As with a result, the app reads each item as the type its call names (`CallAsyncEnumerable<T>`), and an object item binds to a C# record by member name, case-insensitively.

## Reactive Values

A program can follow a value the app holds in a `Reactive<T>` (or `ClientReactive<T>`, `UserReactive<T>`, `MountReactive<T>`) without rendering the app's UI. It follows the value by its stable id. The app's built-in function `Ikon.Reactive.GetStableIdByName` returns the stable id of a reactive field or auto-property of the app's `[App]` class, by its name in the source (`"_score"` for `private readonly Reactive<int> _score`), so the app needs no code of its own for it. For a reactive kept anywhere else, the app returns its `StableId` from an `External` function.

<!-- ikon-example: rust-sdk-reactive -->
```rust
let reactive = ReactiveRegistry::attach(&client, &functions).await?;

// The app's `private readonly Reactive<int> _score`, found by its name
let stable_id = functions
    .call_remote(
        "Ikon.Reactive.GetStableIdByName",
        serde_json::json!(["_score"]),
        "",
        Duration::from_secs(30),
    )
    .await?;

let score = reactive
    .subscribe(stable_id.as_str().unwrap_or_default(), "", |score: i64| {
        println!("score is now {score}")
    })
    .await?;

// Later, to stop following it
score.unsubscribe().await;
```

The callback gets the current value before `subscribe` returns, then the value after each change, in order. The app sends one value at a time. Changes made while a value is on its way arrive as one call with the latest value, so the last call always has the current value. After a reconnect the registry subscribes again and delivers the value as it is then. Pass the mount id for a `MountReactive<T>`, or `""`.

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

The standard components put their text and labels in these props:

| Node type | Text and labels | Action and its payload |
|---|---|---|
| `std.text` | `content` | |
| `std.heading` | `content`, and `level` 1 to 6 | |
| `std.button` | `label` on a button with text only. A button with an icon or its own content has no `label`: its text is the `content` of a `std.text` child, and also its `aria-label` | `onInvokeId`, `{}` |
| `std.text-field`, `std.text-area` | `value`, `placeholder`. A field label is a `std.text` node just before the field, and also the field's `aria-label` | `onValueChangeId`, the text itself |
| `std.checkbox`, `std.switch` | `checked`. The label is a `std.text` node just after it, and also its `aria-label` | `onCheckedChangeId`, `true` or `false` |
| `std.select` | `value`. The label is a `std.text` node just before it, and also its `aria-label` (also on its `std.select-trigger` child). Each option is a `std.select-item` with the option's `value`, its text in a `std.text` below it | `onValueChangeId`, the option's value itself |
| `std.file-upload`, `std.file-upload-zone` | | `uploadActionId`, the id an upload names |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

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

The id comes from an upload control the app shows that program's client, as above. It can show it to those clients alone, such as clients whose `UserType` is `Machine`, so people never see it. An app can instead register a fixed id in C# with `app.Uploads.Register("my-app.reports", onStart, onComplete)`, which the program passes as `upload_action_id` without reading the UI.

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

A received stream's id is `"{sender session id}_{track id}"`. The sender's own name for the stream, such as the id the app passed to `Audio.Raw.SendFrameAsync`, is `begin.stream_id` in `AudioStreamBegan` and `VideoStreamBegan`. `request_video_key_frame` asks the sender of a video stream for a key frame, and returns `false` for a stream that has not begun or has ended.

### Playback Reports

A program that plays an app's audio tells the app how far it has got, as the browser does. The app reads the latest report with `Audio.Raw.GetPlaybackStatus`, for example to pace what it says to what was heard. Only the app's `Audio.Raw` streams ask for reports: `wants_playback_reports` in `MediaEvent::AudioStreamBegan` is true for them, and false for every other stream, such as the one that carries what `Audio.Play` plays, except that every stream of an app built before the flag existed asks. Report every 500 ms of playback, and at once when the state changes: `AudioPlaybackState::Blocked` when the program can no longer play the audio, and `Hidden` while it plays it muted, as a browser tab does in the background:

<!-- ikon-example: rust-sdk-playback-report -->
```rust
let mut every_half_second = tokio::time::interval(Duration::from_millis(500));

loop {
    every_half_second.tick().await;
    let (played, buffered) = position();
    // False once the stream has ended, and from the start for a stream that wants no reports
    let sent = client
        .send_audio_playback_report(
            &stream_id,
            Some(played),
            buffered,
            AudioPlaybackState::Playing,
        )
        .await?;

    if !sent {
        break;
    }
}
```

`played` is the time played from the start of the current segment, `None` when the position is unknown, and `buffered` what was received but not yet played. The report goes to the stream's sender and names the segment of the stream's latest frame. `stream_id` is the receiver's id or the sender's own name for the stream. It sends nothing and returns `false` for a stream that has not begun, has ended or wants no reports, and fails with `SdkError::NotConnected` when the client has no session.

## App Messages

An app can declare its own message types in `schema/*.tp` (see the [Teleport schema spec](teleport-schema-spec.md)) for data that does not fit a function call, such as a stream of positions. `ikon schema` generates Rust for them into `frontend-rust/src/generated/protocol/` when the app has a `frontend-rust/` folder, or into the folder a `language = "rust"` entry of the app's `schema/outputs.toml` names, such as one in the program's own crate. The generated files are modules of the program's crate: declare the folder (`mod generated { pub mod protocol; }` in `main.rs`) and depend on `ikon-protocol`, which the generated code names, at the same version as `ikon-sdk`:

```toml
ikon-protocol = "<the ikon-sdk version>"
```

Each generated type knows its own opcode, so `send_message` sends one and `messages::<T>()` receives one without the program naming the opcode:

<!-- ikon-example: rust-sdk-app-messages -->
```rust
// Taken before connect, so nothing the app sends right after the join is missed
let mut pongs = client.messages::<Pong>();
client.connect().await?;

client
    .send_message(&Ping {
        sequence: 1,
        text: "hello".to_string(),
        data: vec![1, 2, 3],
    })
    .await?;

while let Some(received) = pongs.next().await {
    println!(
        "pong {} from session {}: {}",
        received.message.sequence, received.sender_id, received.message.text
    );
}
```

The app receives a `Ping` with `app.OnMessage<Ping>` and answers with `app.SendMessageAsync`. A stream yields only its own type, with the sender's session id, lasts across reconnects until it is dropped, and is also a `futures_core::Stream`. A stream holds at most 4096 unread messages and drops newer ones, with a warning. A message that does not decode as its type is logged as an error and skipped. A message sent while the client is still joining goes out right after the join, or is discarded if the link drops first. `send_message` fails with `SdkError::NotConnected` when the client has no link, as between a drop and the reconnect. The client receives an app's own types only while `opcode_groups_from_server` includes `Opcode::GROUP_APP_LOCAL`, as the default does.

`message_received` delivers every message from the server as a `ProtocolMessage`; `is::<T>()` and `decode::<T>()` read one as a generated type.

## Configuration

`IkonClientConfig::with_local`, `with_api_key`, `with_guest`, `with_user_token` and `with_connect_url` fill in the defaults; change a field on the value before `IkonClient::new`.

- `timeouts` is a `TimeoutConfig` holding `connection` (the join, a `Duration`, 30 s), `provisioning` (waiting for the app's server to start, 60 s), `max_reconnect_attempts` (`u32`, 6) and `reconnect_backoff` (`Duration`, 500 ms).
- `description` and `locale` (`String`, defaults `"Ikon SDK Rust"` and `"en-US"`) and `product_id`, `version_id`, `install_id` and `user_agent` (`Option<String>`) describe the client to the app.
- `device_id` (`Option<String>`) names the device. Left `None`, `IkonClient::new` sets a random one, which every connect and reconnect of that client sends; `client.config().device_id` reads it.
- `parameters` (`BTreeMap<String, String>`) are the app's client parameters. A program has no URL to carry them, so it picks the app instance here: `config.parameters.insert("room".into(), "harbour".into())` joins the instance a browser joins at `?room=harbour`, for an app whose `SessionIdentity` has a `Room` field (the name's case does not matter). Without it the client joins the instance with that field empty.
- `opcode_groups_from_server` and `opcode_groups_to_server` (`u32`) choose the kinds of message the client receives and sends; the default is every group, `Opcode::GROUP_ALL | Opcode::GROUP_APP_LOCAL`.
- `local_client_proof` (`Option<String>`) matters only for a local connect that does not look local to the run, such as one through `ikon run --host-public`. Without it, such a connect joins as a guest, or is refused where the app admits no guests. With the run's `localClientProof` from its `ikon-server-info.json`, which only a program on the run's machine can read, the run keeps `LocalConfig::user_id`. A program on the same machine connecting to `localhost` needs none.

### Post-Quantum Key Exchange

Every TLS connection the client makes, to the backend and to the app's server, offers the hybrid post-quantum key exchange X25519MLKEM768 first, which the load balancers and deployed IKON servers choose. An IKON server on Windows, such as a local `ikon run` there, serves TLS through SChannel, which has no ML-KEM, and negotiates a classical key exchange. A server without it, or a proxy in between that terminates TLS, can still settle on a classical key exchange such as X25519.

`require_post_quantum = true` on the config makes the client offer X25519MLKEM768 alone, over TLS 1.3 only. A server or proxy without it then fails the handshake, and the connect fails with `SdkError::Tls` or `SdkError::Http`. There is no fallback. The client's link to the app is always TLS over TCP, so no other kind of link has to be turned off. A local connect to a loopback host (`localhost`, `127.0.0.1` or `::1`) is exempt, because its traffic stays on the machine, and a local run on Windows offers no X25519MLKEM768, so it would otherwise refuse the client. A local connect to another machine's address is not exempt.

A program that calls the backend itself gets the same with `IkonApi::require_post_quantum(true)` and `api::request_guest_token_with_tls`. `Transport::connect_with_config` uses the `ClientConfig` it is given unchanged.

`RequirePostQuantum = true` in an app's `ikon-config.toml` makes the app's server offer only links that run over TLS on TCP, but the server cannot refuse a client that offers classical groups. Only the client's setting guarantees a post-quantum key exchange.

### Logging

The SDK logs through the `log` crate, under targets that start with `ikon_sdk`, and prints nothing until the program installs a logger. With `env_logger`, for example, call `env_logger::init()` at the start of `main` and run the program with `RUST_LOG=ikon_sdk=info`. `info` shows each session the client is granted and each reconnect, `warn` a dropped link, a failed reconnect attempt and messages the SDK had to drop, and `debug` the steps in between.
