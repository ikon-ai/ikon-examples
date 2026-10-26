<!-- checked-against: 8759f1da3f563864e3b0c442 -->

# IKON TypeScript SDK

The IKON TypeScript SDK connects browser and Node.js applications to an IKON app. It supports modern browsers with ES2020+ and Node.js 22+ (it needs a global `WebSocket`, which earlier Node.js versions lack).

## Features

- Four authentication modes: Guest, API Key, Local Development, Session Token
- UI-friendly connection states, with slow-connection detection in the React layer (`isConnectingSlow`)
- Automatic reconnection with fixed-delay retries
- Web Worker support for off-UI-thread protocol processing
- Automatic endpoint selection (WebSocket → proxied variants → HTTP stream; opt-in WebTransport preferred when enabled)
- Function registration and remote invocation
- Audio/video playback and capture pipelines
- Framework-agnostic UI state management (sdk-ui)

## Installation

Install the npm package with its Opus codec:

```bash
npm install @ikonai/sdk @ikonai/sdk-libopus
```

The `guest` mode, a connect in `apiKey` mode, `local.spaceId` and `local.backendType`, the
`boolean` that `AppMessaging.send` and `sendProtocolMessage` return, `AppMessaging.on` before
`connect()`, the refusal of an app message without any of its fields, a function call that waits
for the app's function list, and reactive subscriptions that last across a reconnect need
`@ikonai/sdk` 1.13.3 or later.

For UI state management (optional):

```bash
npm install @ikonai/sdk-ui
```

### In Node.js

WebRTC and Web Workers exist only in a browser. In Node the SDK receives audio and video over its
protocol channels and does not decode them, which does not affect a program that sends and receives
messages or calls functions. `@ikonai/sdk` 1.13.2 and earlier try WebRTC in Node anyway and log a
stack trace and a retry every 10 seconds; `webRtc: { enabled: false }` in the configuration stops
that (those versions have no `media` option), and `setLogLevel(LogLevel.WARN)` hides the info lines.

Run a program that imports an app's generated message codec (`frontend-node/src/generated/protocol/*.ts`,
see "App Messages" below) with `tsx` (`npx tsx client.ts`). `node client.ts` fails, because the codec
imports its other files without a file extension and uses TypeScript enums, and Node's built-in
TypeScript support handles neither. A program kept with an app goes in the app's `devices/<name>/`
folder, with a `package.json` of its own, and imports the codec from
`../../frontend-node/src/generated/protocol/`. A `tsconfig.json` there holding
`{ "extends": "../../frontend-node/tsconfig.paths.json" }` makes `tsx` load the same SDK the app's
frontend builds with, which is the platform's source when the app builds against a platform checkout.

## Quick Start

<!-- ikon-example: ts-sdk-quick-start -->
```ts
import { IkonClient } from '@ikonai/sdk';

// Create configuration with API key authentication
const client = new IkonClient({
  apiKey: {
    apiKey: process.env.IKON_API_KEY!,
    spaceId: 'your-app-id',
    externalUserId: 'user-123',
    backendType: 'production', // 'development' for an app on *.dev.ikonai.app
  },
  onConnectionStateChange: (state) => {
    console.log(`Connection state: ${state}`);
  },
  onProtocolMessage: (message) => {
    console.log('Received protocol message');
  },
  onJoined: async () => {
    console.log('Connected and ready!');
  },
});

await client.connect();
```

## Authentication Modes

The SDK supports four authentication modes. Exactly one must be configured.

### Guest

Join a deployed app as a guest, a visitor who has not signed in. It needs no key, only the app id,
and is the TypeScript counterpart of the .NET SDK's `Guest` mode. The platform's anonymous sign-in
gives the client a guest user keyed by `deviceId`: clients with the same device id are the same
guest, and a client without one gets a new device id each time it cannot store one (every run of a
Node program). The connect fails with 403 "Anonymous users not allowed" when none of the app's surfaces
lists `guest` or `global` in its `Methods`, and with 403 "Surface 'name' requires sign-in with one of: …"
when the surface the client enters does not list one. `guest` listed on another surface does not let the
client in. Leaving out sign-in pages (surfaces another surface names as its sign-in page), the client
enters the only surface left, else the one with neither `Routes` nor `Host`, else the first declared,
unless `surface` in the config names another. A program connects to such a private app with an API key. The app must have been deployed once
(`ikon deploy`) before a Node or other outside client can connect to it by app id; a run that was only
ever local is reached with the `local` mode below.

<!-- ikon-example: ts-sdk-auth-guest -->
```ts
const client = new IkonClient({
  guest: {
    spaceId: 'your-app-id',     // The app's id ([Target] SpaceId in ikon-config.<environment>.toml)
    backendType: 'production',  // 'development' for an app on *.dev.ikonai.app
    deviceId: 'info-screen-1',  // Optional: the same device id is the same guest
    parameters: { room: 'harbour' },  // Optional: the app's client parameters, such as the room or instance to join
  },
});
```

`parameters` is a field of the connection config, so it works the same in the `guest`, `apiKey`, `sessionToken` and `local` modes. The app reads it as its client parameters; with a `SessionIdentity` built on one, it chooses which instance the client joins.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once.

<!-- ikon-example: ts-sdk-auth-api-key -->
```ts
import { IkonClient, UserType, ClientType } from '@ikonai/sdk';

const client = new IkonClient({
  apiKey: {
    apiKey: 'ikon-xxxxx',              // From `ikon api-key create` in the app's folder
    spaceId: '...',                     // The app's id (ikon app list)
    externalUserId: 'user-123',         // Your user identifier
    backendType: 'production',          // 'development' for an app on *.dev.ikonai.app
    userType: UserType.Human,           // Optional: UserType enum
    clientType: ClientType.DesktopWeb,  // Optional: ClientType enum
  },
});
```

`userType`, `clientType` and `joinOnly` apply to `apiKey`, `sessionToken` and `guest` alike. `joinOnly: true` joins the app's live session only and never starts one: the connect fails with `NoLiveSessionError` when nothing is running, and a reconnect after the server went away goes offline instead of retrying. Pair it with `userType: UserType.Machine` so the connection also does not hold the session open. A local server is always running, so `local` has no `joinOnly`.

### Local Development

Connect directly to a local IKON server during development.

<!-- ikon-example: ts-sdk-auth-local -->
```ts
const client = new IkonClient({
  local: {
    host: 'localhost',
    httpsPort: 8443,         // The port of the Server URL that the app's `ikon run` printed
    spaceId: 'your-app-id',  // Optional: refuse a server that runs another app
    backendType: 'development',  // With spaceId and no userId: the app's platform, as for guest
    userId: 'dev-user',      // Optional
  },
});
```

The port belongs to the run, not to the app: a run takes 8443 when it is free and the next free
port when another app's run holds it. Use the port of the Server URL the run printed, which is also
the `serverUrl` in `build/app/artifacts/bin/<project>/debug/ikon-server-info.json`. With `spaceId`
set to the app id, a connect to a server that runs another app fails with an `AuthenticationError`
that names both apps, instead of joining it. Without `userId`, a `spaceId` also makes the SDK sign
in anonymously to the app's platform for a user id. A Node program names that platform with
`backendType` (`'development'` for an app on `*.dev.ikonai.app`, `'production'` by default);
otherwise the sign-in for a development app fails with `403 Space ... not found`.

**Calling `[RequireLogin]` functions from a Node program.** The anonymous sign-in above gives a user
id, not a signed-in user, so a `[RequireLogin]` function of an app that lets no guests in refuses the
connection. Pass `apiKey` (a space API key from `ikon api-key create`) inside `local`, together with
`spaceId` and `backendType`: `local: { host: 'localhost', httpsPort, spaceId, apiKey, backendType }`.
The key is exchanged for a backend token, as in `apiKey` mode. It is ignored when `userId` or `token`
is also set.

A local run serves a development certificate. Browsers trust it once `ikon install certificate` has
added it to the computer's certificate store, but Node reads only its own list of authorities, so
a Node program's connect (or `fetch`) fails with `fetch failed`, caused by
`DEPTH_ZERO_SELF_SIGNED_CERT` or `UNABLE_TO_VERIFY_LEAF_SIGNATURE`. Start the program with
`NODE_OPTIONS=--use-system-ca` (Node 22.15 or later) so that Node also reads the computer's store
on Windows and macOS. On Linux, set `NODE_EXTRA_CA_CERTS` to the certificate exported with
`dotnet dev-certs https --export-path ikon-local.pem --format Pem`. A deployed app needs neither.

### Session Token Authentication

Use this when the session token was obtained from OAuth flow or anonymous authentication. The JWT token already contains the internal userId.

<!-- ikon-example: ts-sdk-auth-session-token -->
```ts
const client = new IkonClient({
  sessionToken: {
    token: 'eyJhbGc...',       // JWT token
    spaceId: '...',             // The app's id (ikon app list)
    backendType: 'production',  // Optional: backend environment
  },
});
```

An access token expires while the app is still running. The token is presented only at connect
time, and an open connection is already authenticated, so give a renewed token to the running client
instead of building a new client:

<!-- ikon-example: ts-sdk-update-auth-token -->
```ts
// Same user, fresher credential. A token for a different user is refused;
// that needs a new client, because the connection was resolved from the old identity.
client.updateAuthToken(renewedToken);
```

The React layer (`useIkonApp`) already does this for you: it rebuilds the client when the identity
behind the connection changes, and passes the new token to the running client when only the
credential changed.

To open a second session next to the app connection — a media leg to a cell room, say — use
`createSibling`, which keeps the sibling's credential in step with this client's:

<!-- ikon-example: ts-sdk-create-sibling -->
```ts
const sibling = client.createSibling({ 'ikon-cell-type': 'room', RoomId: 'r1' });
```

`deriveConfig` still exists for building a config by hand, but the credential it returns is a
copy that is not updated when the token is renewed.

## Connection Lifecycle

### Connection States

The client tracks its connection state via the `connectionState` property. Live states currently emitted by the runtime:

| State | Description |
|-------|-------------|
| `idle` | No connection yet: the React layer's state while a page of a surface whose `Connect` is `"onAction"` shows its boot snapshot and waits for the visitor's first action, or forever on a surface whose `Connect` is `"none"`. An `IkonClient` itself never reports it |
| `waitingForExternalConnectUrl` | Waiting for external connect URL (used in preview mode) |
| `connecting` | Authentication and connection in progress |
| `connected` | Protocol channels connected; the session is joined (GlobalState received, `onJoined` run) shortly after |
| `reconnecting` | Lost connection, attempting automatic reconnect |
| `offline` | Disconnected (timeout, server stopped, user-initiated). An unsupported browser, an internal failure or a failed preview-mode connect leaves its error on `client.lastError`; a timeout or a failed `connect()` reports only through `onError` |

The `ConnectionState` union also includes deprecated states (`connectingSlow`, `offlineError`) so existing string-equality checks keep type-checking, but the runtime no longer transitions into them.

Helper functions are available for common state checks:

<!-- ikon-example: ts-sdk-state-helpers -->
```ts
import { isConnecting, isConnected, isOffline } from '@ikonai/sdk';

if (isConnecting(client.connectionState)) {
  // waitingForExternalConnectUrl, connecting, or reconnecting
}

if (isConnected(client.connectionState)) {
  // connected
}

if (isOffline(client.connectionState)) {
  // offline (read client.lastError for the optional error message)
}
```

### Configuration Callbacks

<!-- ikon-example: ts-sdk-config-callbacks -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one

  // Connection state changes
  onConnectionStateChange: (state) => {
    console.log(`State: ${state}`);
  },

  // Protocol message received
  onProtocolMessage: (message) => {
    const opcode = readOpcode(message);
    console.log(`Received opcode: ${opcode}`);
  },

  // Error occurred
  onError: (error) => {
    console.error(`Error: ${error.message}`);
  },

  // Connected and ready (called after GlobalState received)
  onJoined: async () => {
    // Perform initialization here
    // SDK sends ClientReady after this callback completes
  },

  // Tab visibility changed (browser only)
  onVisibilityChange: (isVisible) => {
    console.log(`Tab visible: ${isVisible}`);
  },
});
```

### Connecting and Disconnecting

<!-- ikon-example: ts-sdk-connect-disconnect -->
```ts
// Connect (throws on an authentication or protocol failure; a connection timeout or an
// unsupported browser resolves with the client offline instead)
await client.connect();

// Check connection state
console.log(client.connectionState);  // 'connected'

// Access session ID after connection
console.log(client.sessionId);

// GlobalState is null until it arrives, which can be after connect() resolves; onJoined marks it
console.log(client.globalState);

// Notify the server of client context changes (only while connected)
client.sendUpdateClientContext({
  viewportWidth: window.innerWidth,
  viewportHeight: window.innerHeight,
  theme: 'dark',
});

// Disconnect (connect() runs once per client; to connect again, build a new client)
client.disconnect();

// Access the last error (set when an unsupported browser, an internal failure or a failed preview-mode connect took the client offline)
if (client.lastError) {
  console.error(client.lastError);
}
```

### Automatic Reconnection

The SDK automatically attempts to reconnect when the connection is lost unexpectedly. Configure reconnection behavior:

<!-- ikon-example: ts-sdk-reconnection -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  timeouts: {
    slowConnectionThresholdMs: 5000,   // Slow-connection threshold (drives isConnectingSlow in the React layer)
    connectionTimeoutMs: 180000,        // Connection timeout (3 minutes)
    reconnectBackoffMs: 2000,          // Fixed delay between reconnect attempts
    maxReconnectAttempts: 2,           // Attempts before a soft reconnect against the cached entrypoints, then full re-authentication
  },
});
```

The first reconnect attempt is immediate; each later one waits a fixed delay (2s by default).

Renewing the access token is not a reason to reconnect — see `updateAuthToken` above.

## Message Handling

### Subscribing to Protocol Messages

Subscribe to protocol messages with a callback:

<!-- ikon-example: ts-sdk-subscribe-messages -->
```ts
import { readOpcode, readProtocolMessageHeaders } from '@ikonai/sdk';

// Subscribe to all messages
const unsubscribe = client.subscribeToProtocolMessages((message) => {
  const opcode = readOpcode(message);
  const headers = readProtocolMessageHeaders(message);

  console.log(`Opcode: ${opcode}, Sender: ${headers.senderId}`);
});

// Later: unsubscribe
unsubscribe();
```

### Filtered Subscriptions

Filter messages by opcode group for better performance:

<!-- ikon-example: ts-sdk-filtered-subscriptions -->
```ts
import { Opcode } from '@ikonai/sdk';

// Subscribe to specific opcode groups
const unsubscribe = client.subscribeToProtocolMessages(
  (message) => {
    // Only receives UI and Actions messages
  },
  {
    opcodeGroupsMask: Opcode.GROUP_UI | Opcode.GROUP_ACTIONS,
  }
);

// Subscribe to specific opcodes
const unsubscribe2 = client.subscribeToProtocolMessages(
  (message) => {
    // Only receives GlobalState messages
  },
  {
    opcodes: [Opcode.CORE_GLOBAL_STATE],
  }
);
```

### Subscribing to State Changes

<!-- ikon-example: ts-sdk-subscribe-state -->
```ts
// Subscribe to state changes (called immediately with current state)
const unsubscribe = client.subscribeToState((state) => {
  console.log(`Connection state: ${state}`);
});

// Later: unsubscribe
unsubscribe();
```

### Sending Messages

<!-- ikon-example: ts-sdk-send-messages -->
```ts
// Send a protocol message
client.sendProtocolMessage(message);

// Send an action call: actionId is a UI node's action id (a GUID, such as a button's onClickId)
client.sendActionCall(actionId, JSON.stringify({ key: 'value' }));
```

`sendProtocolMessage` returns false when the client has no connection to send on, or no channel of
it carries the message's opcode group, and the message is dropped.

### App Messages

`appMessaging(client)` sends and receives an app's own `schema/*.tp` types, the counterpart of the
app's `app.SendMessageAsync` and `app.OnMessage<T>`; the `realtime-game-loop` and
`device-typed-messages` patterns show both sides. The generated interfaces keep the schema's field
names, usually PascalCase (`StationId`), and `AppMessaging.send` throws a `TypeError` for an object
that has none of the type's fields (`{ stationId }`), which would otherwise arrive with every field
empty. An `AppMessageType` takes the codec's synchronous `toProtocolMessageX`. `send` throws a
`TypeError` for the Promise that `toProtocolMessageXAsync` returns, which `@ikonai/sdk` 1.13.3 and
earlier dropped while `send` returned true. `AppMessaging.send` returns false, and logs a
warning for the first such send, when the client has no connection to send on. `AppMessaging.on`
can subscribe before `connect()`, and the subscription lasts across reconnects. The app logs a
warning, once per type, for an app message that arrives with no `app.OnMessage<T>` handler. In the
app, the `senderId` that `app.OnMessage<T>` gets for a message from this client is
`client.sessionId`, the same number as `ReactiveScope.ClientId` and `Context.SessionId` for this
client, and `app.SendMessageAsync(reply, senderId)` answers only this client.

## Functions

Register functions in the client's function registry for the app server to call. The server offers a browser client's functions to the app: the app can call them, and another browser client cannot, though it may see them listed (the server normally relays the registrations to the app alone).

### Function Registry

Access the function registry through the client:

<!-- ikon-example: ts-sdk-function-registry -->
```ts
const registry = client.functionRegistry;
```

### Registering Functions

<!-- ikon-example: ts-sdk-register-function -->
```ts
import type { FunctionDefinition } from '@ikonai/sdk';

// Define a function
const definition: FunctionDefinition = {
  name: 'greet',
  description: 'Greets a user by name',
  returnType: { kind: 'string' },
  parameters: [
    {
      name: 'name',
      type: { kind: 'string' },
      description: 'The name to greet',
    },
  ],
};

// Register with handler. A handler's parameters are typed `unknown`; each is the caller's JSON value
// as sent (a Uint8Array when sent as binary), checked against its descriptor only for null, so
// validate it before narrowing.
const unregister = client.functionRegistry.register(
  definition,
  (name) => `Hello, ${name as string}!`
);

// Later: unregister. Local only: the server is not told, so callers still see the function and get
// 'Unknown function' back from this client.
unregister();
```

### Async Functions

<!-- ikon-example: ts-sdk-async-function -->
```ts
const asyncDefinition: FunctionDefinition = {
  name: 'fetchData',
  description: 'Fetches data from an API',
  returnType: { kind: 'string' },
  parameters: [
    { name: 'url', type: { kind: 'string' } },
  ],
};

client.functionRegistry.register(
  asyncDefinition,
  async (url) => {
    const response = await fetch(url as string);
    return response.text();
  }
);
```

### Returning Binary Data

Use `withResultData` to return both a JSON value and binary data:

<!-- ikon-example: ts-sdk-result-data -->
```ts
import { withResultData } from '@ikonai/sdk';

const imageDefinition: FunctionDefinition = {
  name: 'captureImage',
  returnType: { kind: 'string' },
  parameters: [],
};

client.functionRegistry.register(
  imageDefinition,
  async () => {
    const imageData: Uint8Array = await captureScreenshot();
    return withResultData('captured successfully', imageData);
  }
);
```

### Remote Function Calling

Call functions the app server registers (another browser client's functions are not reachable).
A call made while the client connects, such as one straight after `await client.connect()`, waits
up to 10 seconds for the app's function list instead of failing with `Not connected`; a name the
list does not have fails at once.
A function that returns a C# record or class resolves to the parsed JSON, with the C# property
names as they are (`{ Name, SinceUtc }`, PascalCase) and enums as their names, so type the call
with an interface that has those names:

<!-- ikon-example: ts-sdk-remote-call -->
```ts
import { FunctionCallError } from '@ikonai/sdk';

// Call a remote function by name
const result = await client.functionRegistry.call<string>('remoteGreet', ['World']);

// List all remote functions
const remoteFuncs = client.functionRegistry.getRemoteFunctions();
console.log(`Remote functions: ${client.functionRegistry.remoteSize}`);

// Get a specific remote function
const func = client.functionRegistry.getRemoteFunction('remoteGreet');

// Handle remote call errors. Only a remote failure or an unparseable result is a FunctionCallError; not
// connected, an unknown name, a missing instanceId, the fixed 30 s timeout and a disconnect reject with a plain Error.
try {
  await client.functionRegistry.call('riskyOperation');
} catch (error) {
  if (error instanceof FunctionCallError) {
    console.error(`Remote error: ${error.remoteErrorType}`);
    console.error(`Stack trace: ${error.remoteStackTrace}`);
  }
}
```

### Subscribing to App State

A program reads an app's reactive values live through `client.reactiveRegistry`, the counterpart of
the .NET SDK's `ReactiveRegistry`:

<!-- ikon-example: ts-sdk-subscribe-reactive -->
```ts
const client = new IkonClient({
  guest: { spaceId: 'your-app-id', backendType: 'production', deviceId: 'lobby-screen' }, // 'development' for an app on *.dev.ikonai.app
  webRtc: { media: 'none' },
});

await client.connect();

// The app looks up a reactive's stable id by the member's name in its [App] class
const stableId = await client.functionRegistry.call<string>('Ikon.Reactive.GetStableIdByName', ['_visitorCount']);

// The callback runs once with the current value, then on every change
const unsubscribe = await client.reactiveRegistry.subscribe<number>(stableId, (count) => {
  console.log(`Visitors today: ${count}`);
});

// Later: stop receiving changes
unsubscribe();
```

`Ikon.Reactive.GetStableIdByName` finds a reactive field or auto-property of the app's `[App]`
class by its name in the source; for a reactive kept anywhere else, return its `StableId` from an
`External` function. The program sees the instance its identity joins, so state that the app shares
with everyone belongs in an app with one instance (`public record SessionIdentity();`) or in a
`GlobalReactive` or `PersistentGlobalReactive` field, which every instance shares. The program
usually sees another instance's write within seconds. A counter there uses `Update(v => v + 1)`,
because an assignment is last-writer-wins. The value arrives as
parsed JSON with the C# property names. Any client that joins, a guest included, can subscribe to
every reactive of the `[App]` class, so keep what a guest must not see out of shared reactives.
After a reconnect the client subscribes to each of them again, and `client.disconnect()` ends them.

Against a local run, connect with `local` and the app's `spaceId` ("Local Development" above), so
that a port that serves another app fails the connect instead of joining it.

A program that should keep running while the app restarts (`ikon stop` and a new `ikon run`, a
deploy, `ikon session stop`) builds a new client itself. The client reconnects by itself after a
dropped connection, but when the server stops, the client goes `offline` and stays there, and
`connect()` runs once per client:

<!-- ikon-example: ts-sdk-stay-connected -->
```ts
// A client that went offline cannot connect again, so a new client joins the app's next server 15 seconds later
function watchVisitors(): void {
  const client = new IkonClient({
    guest: { spaceId: 'your-app-id', backendType: 'production', deviceId: 'lobby-screen' }, // 'development' for an app on *.dev.ikonai.app
    webRtc: { media: 'none' },
    onConnectionStateChange: (state) => {
      if (state === 'offline') {
        setTimeout(() => {
          client.disconnect();
          watchVisitors();
        }, 15_000);
      }
    },
  });

  void (async () => {
    await client.connect();
    const stableId = await client.functionRegistry.call<string>('Ikon.Reactive.GetStableIdByName', ['_visitorCount']);
    await client.reactiveRegistry.subscribe<number>(stableId, (count) => {
      console.log(`Visitors today: ${count}`);
    });
  })().catch((error: unknown) => {
    // A failed connect also goes offline, which already schedules the next client
    console.error(`Not connected: ${error instanceof Error ? error.message : String(error)}`);
  });
}

watchVisitors();
```

Against a deployed app, each new client starts the app's server if none is running.

### Value Descriptors

Function parameters and return types use `ValueDescriptor`:

| Kind | Description | Example |
|------|-------------|---------|
| `boolean` | Boolean value | `{ kind: 'boolean' }` |
| `number` | Numeric value | `{ kind: 'number' }` |
| `string` | String value | `{ kind: 'string' }` |
| `array` | Array of elements | `{ kind: 'array', element: { kind: 'string' } }` |
| `dictionary` | Key-value map | `{ kind: 'dictionary', keyType: 'string', value: { kind: 'number' } }` |

All descriptors support an optional `nullable` property.

## Media Pipelines

The SDK has built-in audio and video playback and capture pipelines.

### Accessing Media

<!-- ikon-example: ts-sdk-media-access -->
```ts
// Backup (non-WebRTC) playback pipeline: null while WebRTC carries media (the default)
const audio = client.media?.audio;
const video = client.media?.video;

// Media capture: built on demand and cached; null outside a browser
const capture = await client.ensureMediaCapture();
```

### Configuration

<!-- ikon-example: ts-sdk-media-config -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  audio: {
    performance: { preferWebCodecs: true },
    background: { allowOnDesktop: true, allowOnMobile: false },
    diagnostics: { enabled: true, statusIntervalMs: 1000 },
  },
  video: {
    performance: { preferOffscreenCanvas: true },
  },
  webRtc: {
    media: 'atConnect',  // negotiate the peer with the connection (the default), or 'none' for no WebRTC
  },
  mediaSession: {
    title: 'My App Audio',
    artist: 'IKON',
  },
});
```

When WebRTC is disabled or its signaling fails, the playback pipelines process audio and video protocol messages instead.

### WebRTC

The SDK carries audio and video over WebRTC. The surface's `Media` in `ikon-config.toml`, which a page passes on, says whether the peer is negotiated: with the connection (`atConnect`, the default) or never (`none`). A program sets it in the configuration:

<!-- ikon-example: ts-sdk-webrtc -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  webRtc: { media: 'none' },  // never negotiate a peer; 'atConnect' (the default) negotiates it with the connection
});

// Check if WebRTC is enabled by config (false if both audio and video are disabled, or media is 'none')
console.log(client.isWebRtcEnabled);

// Check if WebRTC is actually carrying media (false after a fallback to the protocol channel)
console.log(client.isWebRtcMediaActive);

// Access video streams keyed by track index
const streams: Map<number, MediaStream> = client.webRtcVideoStreams;

// Look up a video stream by app-level stream ID
const stream = client.getWebRtcVideoStreamByStreamId('my-stream-id');

// Listen for track map changes from the server (any number of listeners; the single-slot
// `onWebRtcTrackMapChanged` property is replaced by whoever assigns it last)
const stopListening = client.addWebRtcTrackMapListener((info) => {
  console.log(`Track ${info.kind}:${info.trackIndex} active=${info.active}`);
});
```

## UI State Management (sdk-ui)

The `@ikonai/sdk-ui` library provides framework-agnostic UI state management for rendering IKON UI updates.

### IkonUiCore

The core class handles protocol messages and maintains UI state:

<!-- ikon-example: ts-sdk-ui-core -->
```ts
import { IkonUiCore } from '@ikonai/sdk-ui';

const uiCore = new IkonUiCore({
  client,
  threading: {
    uiWorker: 'auto',       // 'auto' | 'disabled'
    flushIntervalMs: 16,
    maxLatencyMs: 50,
  },
});

// Subscribe to UI updates
const unsubscribe = uiCore.subscribeToUiUpdates((streamId, update) => {
  console.log(`UI update for stream: ${streamId}`);
  // update.snapshot contains the parsed UI tree
});

// Subscribe to style changes
uiCore.subscribeToUiStyles((style) => {
  console.log(`Style updated: ${style.styleId}`);
});

// Get current styles
const styles = uiCore.getUiStyles();

// Cleanup
uiCore.dispose();
```

### UiStore

Access the UI store for stream snapshots:

<!-- ikon-example: ts-sdk-ui-store -->
```ts
// Get all stream snapshots
const snapshots = uiCore.uiStore.getSnapshots();

// Get specific stream
const snapshot = uiCore.uiStore.getSnapshot(streamId);
```

### Custom UI Renderers

To build custom UI renderers for frameworks other than React, use the exported types and utilities:

<!-- ikon-example: ts-sdk-custom-renderer -->
```ts
import { HandlerCache, type UiNode } from '@ikonai/sdk-ui';

// A UiNode is one UI element: readonly id, type, props (Record<string, unknown>), children and styleIds
function renderNode(node: UiNode, depth = 0): string {
  const line = `${'  '.repeat(depth)}<${node.type} id="${node.id}" class="${node.styleIds.join(' ')}">`;
  return [line, ...node.children.map((child) => renderNode(child, depth + 1))].join('\n');
}

// HandlerCache provides stable event handler references, one per node and action
const cache = new HandlerCache();
const handler = cache.getHandler('node-id', 'action-id', (actionId) => {
  client.sendActionCall(actionId);
});
```

The `sdk-ui` library is designed to be framework-agnostic. For React integration, see `@ikonai/sdk-react-ui`.

## Advanced Configuration

### Timeouts

<!-- ikon-example: ts-sdk-timeouts -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  timeouts: {
    slowConnectionThresholdMs: 5000,   // Default: 5000
    connectionTimeoutMs: 180000,        // Default: 180000 (3 minutes)
    reconnectBackoffMs: 2000,          // Default: 2000
    maxReconnectAttempts: 2,           // Default: 2
    actionAckTimeoutMs: 5000,          // Default: 5000 (server ACK window for action calls)
  },
});
```

### Protocol Options

<!-- ikon-example: ts-sdk-protocol-options -->
```ts
import { Opcode } from '@ikonai/sdk';

const client = new IkonClient({
  local: {
    host: 'localhost',
    httpsPort: 8443,
    // Filter which message types to receive/send
    opcodeGroupsFromServer: Opcode.GROUP_ALL,
    opcodeGroupsToServer: Opcode.GROUP_ALL,
  },
});
```

### Client Identification

<!-- ikon-example: ts-sdk-client-identification -->
```ts
const client = new IkonClient({
  apiKey: {
    apiKey: '...',
    spaceId: '...',
    externalUserId: 'user-123',
    backendType: 'production', // 'development' for an app on *.dev.ikonai.app
    // Client identification
    deviceId: 'unique-device-id',
    productId: 'my-app',
    versionId: '1.0.0',
    installId: 'install-xyz',
    locale: 'en-US',
    description: 'My Application',
    parameters: {
      custom_param: 'value',
    },
  },
});
```

### Debug Mode

Enable debug mode by adding `?ikon-debug=true` to the URL, and read it programmatically:

<!-- ikon-example: ts-sdk-debug-mode -->
```ts
import { initializeDebugMode, isDebugModeEnabled } from '@ikonai/sdk';

// Initialize based on URL parameter (?ikon-debug=true)
initializeDebugMode();

// Check if debug mode is active
if (isDebugModeEnabled()) {
  console.log('Debug mode is enabled');
}
```

### Feedback

When the app offers feedback (a platform setting, changed in Studio or with `ikon feedback set`), `IkonApp` draws the platform's feedback button and sheet. To place a "Send feedback" control of your own, use `useIkonFeedback` with the app's client:

<!-- ikon-example: ts-sdk-feedback -->
```tsx
import { useIkonFeedback, type UseIkonAppResult } from '@ikonai/sdk-react-ui';

export function SendFeedbackButton({ app }: { app: UseIkonAppResult }) {
  const { available, open } = useIkonFeedback(app.client);

  return available ? <button onClick={open}>Send feedback</button> : null;
}
```

`available` is false for anyone the app does not offer feedback. `open()` behaves as a tap on the platform's button, screenshot included, and needs an `IkonApp` on the page to show the sheet. Without React, `client.requestFeedback()` does the same. With `[Feedback] Button = "none"` the platform draws no button, and a control like this is the only way to open feedback.

### Query Parameters

The SDK reads `ikon-*` query parameters from the page URL at startup. They are for diagnostics and
testing, not for normal operation, and they override the matching constructor options. Both the hyphen form (`?ikon-proxy`) and the underscore form (`?ikon_proxy`)
are accepted. All of them are stripped before the URL is handed to the app, so they never appear as
app state.

| Parameter | Values | What it does |
|---|---|---|
| `ikon-transport` | `ws`, `wt`, `http`, `http-binary`, `http-sse`, `http-poll` | Pin one transport, and for the HTTP stream its downstream encoding. Long forms (`websocket`, `webtransport`, `httpstream`) also work |
| `ikon-proxy` | `false`, `true`, `lb` | Pin the connect tier: the server's own host and port, the on-host proxy on 443, or the app's own origin through the fleet gateway (`stream` is an alias for `lb`) |
| `ikon-connect` | token, `<port>~<token>`, or a URL | Connect to a specific IKON server, for shareable preview links. Carries a credential — treat the link like one |
| `ikon-session` | session identity hash | Join a specific live session instead of resolving one by identity |
| `ikon-version` | app bundle id | Run a deploy that is not live (`ikon deploy --no-activate`) in a session of its own. Only someone who may change the app is let in; anyone else gets `AccessDeniedError`. Stays in the address bar, so a reload stays on that version |
| `ikon-same-origin` | `true`, `false` | Route API and auth calls through the app's own origin. On by default for deployed apps. `true` cannot turn it on in local dev; in the cloud it only wins over a configured auth URL override |
| `ikon-debug` | `true` | Verbose SDK logging, and the devtools extension |
| `ikon-debug-overlay` | `true` | The on-screen debug panel. `ikon-debug` does not imply it |
| `ikon-inspect` | `true` | The element inspection overlay |
| `ikon-logs` | present | Mirror the console onto the screen. Read by the app's own `index.html` rather than the SDK, so it works even when the bundle never loads — which is how a device with no developer tools reports a blank screen |
| `ikon-lang` | e.g. `en`, `fi` | Override language detection |
| `ikon-audio`, `ikon-video`, `ikon-webrtc` | `true`, `false` | Force audio, video or WebRTC on or off |
| `ikon-ice-transport` | `relay`, `all` | Force the WebRTC ICE policy. `relay` gathers TURN candidates only, which is how the relay path gets verified |
| `ikon-retry` | `false` | Fail fast: one provisioning attempt, no channel reconnects, and no reconnect or reauth after a lost connection (the auth connect still retries, and a connect token the server refuses as stale still gets one fresh `/init`) |
| `ikon-snapshot` | `true` | Connect as a build-time snapshot client |
| `ikon-feedback` | present | Open the feedback sheet once the app is live, as a tap on the feedback button does. Only for a user who is offered the feedback overlay |

An unrecognised value is ignored rather than guessed at, so a typo leaves the default behaviour in
place instead of silently pinning something slow.

Pinning lets you test a fallback path before a customer's network forces the client onto it:

```text
https://your-app.ikonai.app/?ikon-transport=http-poll&ikon-debug=true
```

If a pin names something the server did not offer — `?ikon-proxy=lb&ikon-transport=wt`, where the
gateway advertises no WebTransport — the connect **fails** with `NoMatchingEndpointTypeError` rather
than falling back. A pin is there to measure one path, and connecting over another one while
reporting success would make that measurement wrong.

### Browser Functions

By default, the SDK auto-registers browser convenience functions (getTheme, setTheme, getLocation, etc.) when running in a browser. Disable this if needed:

<!-- ikon-example: ts-sdk-browser-functions -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  disableBrowserFunctions: true,
});
```

The sound-effect functions (`ikon.client.playSound` / `stopSound`) are part of that set. They play
through one Web Audio context, which needs one user gesture before it can run. Until then a passive
capture-phase listener on the window waits for that gesture, and the listener is removed as soon as
the context runs. An app that plays its own audio, or that does not want the SDK to touch the audio
session, can keep the other browser functions and turn off only these with
`disableSoundFunctions: true`; `useIkonApp` forwards both options.

### Internal Link Navigation

A plain left-click on a same-origin `<a href>` is turned into an in-place route change: the SDK
pushes the history entry and tells the server the URL changed, with the same signal a back/forward
gesture sends. The app switches route without reloading the document, dropping the connection or
creating a new client session. On the server this arrives as `app.Navigation.PathChangedAsync`, so
one router handles both link clicks and history gestures. A cold load or reload raises no event; its
path is in `app.Navigation.CurrentPath`.

Each history entry the SDK adds (a link click, the server's `Navigation.SetPathAsync`) records in
`history.state` how many in-app entries lie behind it. The server's `Navigation.BackAsync` uses that
count. When the entry behind is the app's own, the client calls `history.back()`. On the entry the
visit started on, it replaces the address with the fallback path and fires `popstate` instead, and
when it is already on the fallback path it does nothing. So the browser never leaves the app, and the
server receives either move as a back gesture. A frontend that writes its own history entries with
`pushState` leaves them out of that count, so a Back from one of them goes to the fallback.

The `href` stays in the DOM, so new-tab, copy-link, the status-bar preview and crawlers are
unaffected. These always navigate natively: modified and middle clicks, `target` links, `download`
links, `rel="external"`, cross-origin URLs, other browser-navigable schemes (`file:`, `blob:`, `data:` and the like), the platform-reserved `/ikon` and
`/api` prefixes, fragments on the current page, and any click made while the client has no
connection. A plain click on an external-handler scheme (`mailto:`, `tel:`, `sms:`, app deep links)
is launched through a hidden iframe instead, even with `interceptInternalLinks: false` or no
connection, so the connection survives it.

Opt one link out with an attribute:

```html
<a href="/legacy-report" data-ikon-navigate="reload">Report</a>
```

Or turn it off for the whole client — appropriate when the app's internal links address documents it
does not route:

<!-- ikon-example: ts-sdk-intercept-links -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  interceptInternalLinks: false,
});
```

## Logging

Configure SDK logging:

<!-- ikon-example: ts-sdk-logging -->
```ts
import { setLogLevel, setLogSink, LogLevel, createLogger, subscribeToLogEvents } from '@ikonai/sdk';

// Set minimum log level for console output
setLogLevel(LogLevel.DEBUG);  // DEBUG, INFO, WARN, ERROR, NONE

// Set a custom log sink for all messages
setLogSink((entry) => {
  // Forward to your logging service
  myLogger.log(entry.levelName, entry.component, entry.message);
});

// Create a component-specific logger
const logger = createLogger('MyComponent');
logger.info('Starting up');  // [MyComponent] Starting up

// Subscribe to log events (for forwarding to analytics or backend)
const unsubscribeLogs = subscribeToLogEvents(
  (entry) => {
    console.log(`[${entry.levelName}] ${entry.component}: ${entry.message}`);
  },
  { minLevel: LogLevel.WARN }  // Default: WARN and ERROR
);

// Later: unsubscribe
unsubscribeLogs();
```

## Error Types

The SDK has a typed error for each kind of failure:

| Error Type | Description |
|------------|-------------|
| `ConnectionError` | Base class for all connection-related errors |
| `AuthenticationError` | Authentication failed (invalid credentials, unauthorized) |
| `TransportError` | Transport-level failure (WebSocket error) |
| `KeepaliveTimeoutError` | No keepalive received within timeout period |
| `AuthRejectedError` | Server closed the transport immediately after handshake (e.g. cached auth ticket expired) |
| `MaxRetriesExceededError` | Maximum reconnection attempts exhausted |
| `ProvisioningTimeoutError` | Cloud app session provisioning timed out |
| `SpaceNotFoundError` | No app found for the given domain (exported, but the SDK does not currently raise it) |
| `AccessDeniedError` | Server denied access (e.g., domain allowlist blocks email domain) |
| `ServerFullError` | Server at capacity, connection rejected (terminal, no retry) |
| `BrowserNotSupportedError` | Browser lacks required runtime features (terminal, exposes `missingFeatures`) |
| `NoMatchingEndpointTypeError` | `?ikon-transport` / `?ikon-proxy` pin a combination the server did not offer (terminal, exposes `pinnedTransport`, `pinnedTier`, `offeredTypes`) |
| `AppStartupFailedError` | The app's server failed during startup (terminal) |
| `SsoRequiredError` | The app requires signing in through the organisation's SSO (extends `AccessDeniedError`, exposes `connectionId`) |
| `CredentialRejectedError` | Backend rejected the presented credential, HTTP 401 (terminal) |
| `SessionNotFoundError` | `?ikon-session` names no live session (terminal, exposes `sessionIdentityHash`) |
| `NoLiveSessionError` | A `joinOnly` connect found no running session and started none (terminal, exposes `spaceId` and `retryAfterMs`, the backend's suggested wait) |
| `ServerUnavailableError` | Backend returned HTTP 5xx (retried, exposes `status`) |
| `ServerChangedError` | The ikon server refused the connect token as not its own or expired, usually because the server was replaced. A cloud connect gets a fresh token from `/init` once before raising it; a local or connect-URL connect, which has no `/init` to ask, raises `AuthenticationError` instead. Not an `AuthenticationError`: the sign-in is still valid (exposes `status`) |

In a browser the protocol runs in a Web Worker, and errors raised there reach `onError` as a plain `Error` that keeps only the `name` and `message`, so identify transport-level errors by `error.name` rather than `instanceof`.

<!-- ikon-example: ts-sdk-error-handling -->
```ts
import { AuthenticationError } from '@ikonai/sdk';

const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken: exactly one
  onError: (error) => {
    if (error instanceof AuthenticationError) {
      console.error('Authentication failed:', error.message);
    } else if (error.name === 'MaxRetriesExceededError') {
      console.error('Connection lost, max retries exceeded');
    }
  },
});
```

## API Reference

### Core Types

| Type | Description |
|------|-------------|
| `IkonClient` | Main client class for connecting to IKON servers |
| `IkonClientConfig` | Configuration for the client |
| `ConnectionState` | Union type. Live states: `idle`, `waitingForExternalConnectUrl`, `connecting`, `connected`, `reconnecting`, `offline`. Deprecated members kept in the union: `connectingSlow`, `offlineError` |
| `ProtocolMessage` | Raw protocol message (Uint8Array) |
| `GlobalState` | Server state received after connection |

### Configuration Types

| Type | Description |
|------|-------------|
| `LocalConfig` | Configuration for local server development |
| `GuestConfig` | Configuration for guest authentication |
| `ApiKeyConfig` | Configuration for API key authentication |
| `SessionTokenConfig` | Configuration for session token authentication |
| `TimeoutConfig` | Timeout settings |
| `AudioConfig` | Audio playback configuration |
| `VideoConfig` | Video playback configuration |
| `WebRtcConfig` | WebRTC configuration for audio/video transport |
| `MediaSessionConfig` | OS-level media controls metadata |
| `BackendType` | String union: `production`, `development` |

### Function Types

| Type | Description |
|------|-------------|
| `FunctionRegistry` | Central registry for function registration |
| `FunctionDefinition` | Function metadata (name, parameters, return type) |
| `FunctionHandler` | Handler callback type |
| `ValueDescriptor` | Type descriptor for parameters/returns |
| `FunctionResultWithData` | Result type with binary data attachment |
| `FunctionCallError` | Error from remote function call with `remoteErrorType` and `remoteStackTrace` |

### UI Types (sdk-ui)

| Type | Description |
|------|-------------|
| `IkonUiCore` | Core class for UI state management |
| `UiStore` | Multi-stream UI state store |
| `UiStreamStore` | Single-stream UI state store |
| `UiNode` | UI element node |
| `ParsedUiUpdate` | Parsed UI update from protocol |
| `HandlerCache` | Stable event handler memoization |

### Media Types

| Type | Description |
|------|-------------|
| `IkonMedia` | Combined audio/video playback wrapper |
| `IkonAudioPlayback` | Audio playback pipeline |
| `IkonVideoPlayback` | Video playback pipeline |
| `IkonMediaCapture` | Combined capture wrapper (browser only) |
| `IkonAudioCapture` | Audio capture pipeline |
| `IkonVideoCapture` | Video capture pipeline |
| `IkonImageCapture` | Image capture pipeline |

### Error Types

| Type | Description |
|------|-------------|
| `ConnectionError` | Base connection error |
| `AuthenticationError` | Authentication failure |
| `TransportError` | Transport-level failure |
| `KeepaliveTimeoutError` | Keepalive timeout |
| `AuthRejectedError` | Server closed transport immediately after handshake (auth-rejection signal) |
| `MaxRetriesExceededError` | Max retries exceeded |
| `ProvisioningTimeoutError` | Provisioning timeout |
| `SpaceNotFoundError` | No app found (not currently raised) |
| `AccessDeniedError` | Server denied access (e.g., domain allowlist) |
| `ServerFullError` | Server at capacity (terminal, no retry) |
| `BrowserNotSupportedError` | Browser missing required features (terminal) |
| `NoMatchingEndpointTypeError` | Pinned transport/tier not offered by the server (terminal) |
| `AppStartupFailedError` | App server failed during startup (terminal) |
| `SsoRequiredError` | Organisation SSO sign-in required |
| `CredentialRejectedError` | Credential rejected, HTTP 401 (terminal) |
| `SessionNotFoundError` | `?ikon-session` names no live session (terminal) |
| `ServerUnavailableError` | Backend HTTP 5xx (retried) |
| `ServerChangedError` | Server refused a stale connect token (one fresh `/init` first; not an `AuthenticationError`) |
| `FunctionCallError` | Remote function call failure with error type and stack trace |

### Utility Functions

| Function | Description |
|----------|-------------|
| `isConnecting(state)` | Returns true if waitingForExternalConnectUrl, connecting, connectingSlow, or reconnecting |
| `isConnected(state)` | Returns true if connected |
| `isOffline(state)` | Returns true if offline or offlineError |
| `isError(state)` | Deprecated. Returns true if offlineError (no longer emitted) — read `client.lastError` instead |
| `readOpcode(message)` | Extract opcode from protocol message |
| `readProtocolMessageHeaders(message)` | Extract headers from protocol message |
| `isWebTransportSupported()` | Check WebTransport browser support |
| `getOpcodeName(opcode)` | Get human-readable opcode name |
| `subscribeToLogEvents(listener, options?)` | Subscribe to SDK log events for forwarding |
| `initializeDebugMode()` | Initialize debug mode from URL parameter |
| `isDebugModeEnabled()` | Check if debug mode is active |

## License

This SDK is licensed under the Ikon AI SDK License.

## Support

For issues and feature requests, contact IKON support or open an issue on GitHub.
