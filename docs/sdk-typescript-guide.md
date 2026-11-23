<!-- checked-against: f6e6e2c876a45e87e3b0c442 -->

# IKON TypeScript SDK

The IKON TypeScript SDK connects browser and Node.js applications to an IKON app. It supports modern browsers with ES2020+ and Node.js 22+ (it needs a global `WebSocket`, which earlier Node.js versions lack).

## Features

- Four authentication modes: Guest, API Key, Local Development, Session Token
- UI-friendly connection states, with slow-connection detection in the React layer (`isConnectingSlow`)
- Automatic reconnection, backing off from 10 seconds to 5 minutes while a new session cannot be started
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

A version this guide names is the first release that has the feature. A package built from the
platform's repository between releases carries the number of the last release, and it has
everything this guide describes, including what the guide says needs a later version.

For UI state management (optional):

```bash
npm install @ikonai/sdk-ui
```

### In Node.js

A Node program, in short:

- Node.js 22 or later, an ES module (below), `@ikonai/sdk` and `@ikonai/sdk-libopus`; `@ikonai/sdk-ui` to read and use the app's UI, including the ids an upload needs.
- Join a deployed app with `guest` or `apiKey` and a local run with `local` ("Authentication Modes"); without a page it joins as a `ContextType.Plugin` client.
- Tell the app the path and context a page would report: `client.setPath` and `client.sendUpdateClientContext` (below).
- Upload with `uploadFile` and a `File` made from a `Buffer` or `fs.openAsBlob` ("Uploading Files").
- Read and drive the UI with `IkonUiCore` and the node table in "Custom UI Renderers"; Node has no `Worker`, so `uiWorker: 'auto'` runs on the main thread.
- Send and receive audio and video as frames through `client.rawMedia` ("Audio and Video Without a Page").

A Node program is an ES module: `npm pkg set type=module` in its folder, because the `package.json`
that `npm init -y` writes makes it CommonJS, where a top-level `await` fails.

WebRTC and Web Workers exist only in a browser. In Node the SDK carries audio and video over its
protocol channels, and a program sends and receives them as frames through `client.rawMedia` (see
"Audio and Video Without a Page" below). A Node program has no page to report, so it tells the app
its viewport, theme, timezone and visibility with `client.sendUpdateClientContext(...)`. A value a
later update leaves out keeps the one sent before. The program tells the app the path it shows with
`client.setPath('/room/7')`. Both are sent again when the client joins again. A Node program uploads
a file with `uploadFile` and a `File` made from a `Buffer` (`new File([buffer], 'report.pdf')`) or
`fs.openAsBlob(path)` (see "Uploading Files" below). `rawMedia`, `setPath` and keeping the values an
update leaves out need `@ikonai/sdk` 1.19 or later.
A Node program needs no `webRtc` setting, because a runtime without `RTCPeerConnection` never
negotiates WebRTC; there is no `webRtc.enabled`, and `webRtc: { media: 'none' }` is the option that
turns it off in a browser. `setLogLevel(LogLevel.WARN)` hides the SDK's info lines.

Run a program with `tsx`. `npx tsx client.ts` runs it in the foreground. A program started in the
background or by a supervisor runs as `node --import tsx client.ts`, which is one process, so the
process id you hold is the program's, where `npx` leaves a parent in between. A program that imports
an app's generated message codec (`frontend-node/src/generated/protocol/*.ts`, see "App Messages"
below) needs `tsx`: `node client.ts` fails, because the codec imports its other files without a file
extension and uses TypeScript enums, and Node's built-in TypeScript support handles neither.

`tsx` runs a program without type-checking it. A program that uses the SDK from npm and imports no
codec checks with `npx tsc --noEmit` (with `typescript` and `@types/node` installed) and this
`tsconfig.json`, from `@ikonai/sdk` 1.19 on. Earlier packages' type declarations do not resolve
under `NodeNext`, and every SDK type becomes `any` without an error:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "NodeNext",
    "moduleResolution": "NodeNext",
    "strict": true,
    "noEmit": true,
    "skipLibCheck": true,
    "types": ["node"]
  }
}
```

A program kept with an app goes in the app's `devices/<name>/` folder, with a `package.json` of its
own, and imports the codec from `../../frontend-node/src/generated/protocol/`. Its `tsconfig.json`
extends the app's, which makes `tsx` and `tsc` load the same SDK the app's frontend builds with,
which is the platform's source when the app builds against a platform checkout:

```json
{
  "extends": "../../frontend-node/tsconfig.paths.json",
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "noEmit": true,
    "skipLibCheck": true,
    "lib": ["ES2022", "DOM"],
    "types": ["node"]
  }
}
```

`"moduleResolution": "Bundler"` is needed for the generated codec, whose imports have no file
extension. When the app builds against a platform checkout, the paths point at the SDK's sources,
which read Vite's `import.meta.env`, so install `vite` too and add `"vite/client"` to `types`.

A whole Node program: it joins with an API key, reports who joins and leaves, and prints the text of
each screen the app sends it.

<!-- ikon-example: ts-sdk-node-program -->
```ts
import { ContextType, IkonClient } from '@ikonai/sdk';
import { IkonUiCore, type UiNode } from '@ikonai/sdk-ui';

const client = new IkonClient({
  apiKey: {
    apiKey: process.env.IKON_API_KEY!, // From `ikon api-key create` in the app's folder
    spaceId: 'your-app-id',
    externalUserId: 'watcher-1',
    backendType: 'production',
  },
});

// Join, see who else is in the session, and read the screen the app sends this client
client.onClientJoined((other) => console.log(`${other.SessionId} joined as ${ContextType[other.ContextType]}, user ${other.UserId}`));
client.onClientLeft((other) => console.log(`${other.SessionId} left`));

const uiCore = new IkonUiCore({ client });

function textOf(node: UiNode): string {
  const own = typeof node.props['content'] === 'string' ? node.props['content'] : '';
  return [own, ...node.children.map(textOf)].filter(Boolean).join(' ');
}

uiCore.subscribeToUiUpdates(() => {
  for (const snapshot of uiCore.uiStore.getSnapshots()) {
    const root = snapshot.rootViewId ? snapshot.views.get(snapshot.rootViewId) : undefined;

    if (root) {
      console.log(`${snapshot.streamId}: ${textOf(root)}`);
    }
  }
});

try {
  await client.connect();
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
}
```

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
  onReady: async () => {
    console.log('Connected and ready!');
  },
});

await client.connect();
```

## Authentication Modes

The SDK supports four authentication modes. Exactly one must be configured. None of them acts as the person signed in with `ikon login`; only the .NET SDK's `UserLogin` does.

### Guest

Join a deployed app as a guest, a visitor who has not signed in. It needs no key, only the app id,
and is the TypeScript counterpart of the .NET SDK's `Guest` mode. When the app lists `guest`, the
platform's anonymous sign-in gives the client a guest user keyed by `deviceId`: clients with the same
device id are the same guest, and a client without one gets a new device id each time it cannot
store one (every run of a Node program). When the app lists `global`, the sign-in ignores `deviceId`
and every guest is the same space-wide user. The connect fails with 403 "Anonymous users not allowed" when none of the app's surfaces
lists `guest` or `global` in its `Methods`, and with 403 "Surface 'name' requires sign-in with one of: …"
when the surface the client enters does not list one. A surface with no `[Surface.Auth]` keeps the default methods, which include `guest`; `ikon new --private` writes `email` and `passkey` only. `guest` listed on another surface does not let the
client in. Leaving out sign-in pages (surfaces another surface names as its sign-in page), the client
enters the only surface left, else the one with neither `Routes` nor `Host`, else the first declared,
unless `surface` names another (it controls admission only; it does not set the page path, which `setPath` does), in the `guest` (or other connection) block or at the top of the config, where it applies to that block when it names none. A client with no `surface` joins that catch-all surface, and its `Presence` decides which other clients the client sees. A program connects to such a private app with an API key. The app must have been deployed once
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

`parameters` is a field of the connection config, so it works the same in the `guest`, `apiKey`, `sessionToken` and `local` modes. The app reads it as its client parameters. A program in Node has no page URL, so it picks the app instance here: `parameters: { room: 'harbour' }` joins the instance a browser joins at `?room=harbour`, for an app whose `SessionIdentity` has a `Room` field (the name's case does not matter). Without it the client joins the instance with that field empty.

### API Key Authentication

Use this for programmatic access to an IKON app. `ikon api-key create` in the app's folder makes an API key and prints it once. `ikon api-key create --app-id <id> --format json --output key.json` works from any folder and writes the key to `key.json` as its `apiKey` field, beside `spaceId`.

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

`userType` and `clientType` apply to every mode, `local` included. The app reads them as the client's `Context.UserType` and `Context.ClientType`. `joinOnly` applies to `apiKey`, `sessionToken` and `guest` alike. `joinOnly: true` joins the app's live session only and never starts one: the connect fails with `NoLiveSessionError` when nothing is running, and a reconnect after the server went away goes offline instead of retrying. Pair it with `userType: UserType.Machine` so the connection also does not hold the session open. An `apiKey` client that sets no `userType` is a `UserType.Human`, so it holds the session open. A local server is always running, so `local` has no `joinOnly`.

A deleted API key (`ikon api-key delete <id> --app-id <app-id> --yes`, or without `--app-id` in the app's folder) is refused from the next sign-in on: a new connect, or a reconnect that has to sign in again, fails with the key refused. Deleting it does not end any session: a client the key already connected keeps going, and a reconnect that resumes the same session with the tickets the client holds needs no sign-in. A reconnect into a new session signs in with the key again, so a client built with an `apiKey` needs no renewal; to rotate the key, build a new client with the new one.

### Local Development

Connect directly to a local IKON server during development. A Node program on Linux starts with
`NODE_EXTRA_CA_CERTS=ikon-local.pem` (the certificate note below says where it comes from), or its connect fails with
`fetch failed`. Both `host` and `httpsPort` are required; without them the connect throws an `AuthenticationError` naming them.

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

A run takes the port the project's last run used while it is free, else 8443 or the next free port,
so a run can stay on 8444 after 8443 frees up. Use the port of the Server URL the run printed, which is also
the `serverUrl` in `build/app/artifacts/bin/<project>/debug/ikon-server-info.json`. The run writes
that file each time it starts, so a program can read the port from it:

<!-- ikon-example: ts-sdk-local-server-info -->
```ts
// A program in the app's devices/<name>/ folder. MyApp is the app's project name.
const serverInfoUrl = new URL('../../build/app/artifacts/bin/MyApp/debug/ikon-server-info.json', import.meta.url);
const { serverUrl, localClientProof } = JSON.parse(readFileSync(serverInfoUrl, 'utf8')) as { serverUrl: string; localClientProof?: string };

const client = new IkonClient({
  local: {
    host: 'localhost',
    httpsPort: Number(new URL(serverUrl).port),
    spaceId: 'your-app-id',
    backendType: 'development', // 'production' for an app on *.ikonai.app
    localClientProof, // Lets a run started with --host-public keep the program's user id
  },
});
```

`localClientProof` matters only for a connect that does not look local to the run. A run started
with `ikon run --host-public` treats every connect as not local, one to `localhost` included; on any
other run, only a connect from another machine is not local. Without the proof, such a connect joins as a guest, or is
refused where the app admits no guests. With the run's `localClientProof` from its
`ikon-server-info.json`, which only a program on the run's machine can read, the run keeps the user
id the connect asks for. The SDK sends it in the `X-Ikon-Local-Client-Proof` header of the run's
`/connect-token` request. It is for Node programs: a page cannot read the file.

A program without a page (Node) joins a local run as a `ContextType.Plugin` client, as it does in the
`apiKey` and `guest` modes, so the app does not count it as a person on a page. A page joins as
`ContextType.Browser`. `userType` and `clientType` work as in the other modes (`userType:
UserType.Machine` for a program that acts for no person). Earlier than `@ikonai/sdk` 1.19, a local
connect is always a `Browser` client of the `Human` user type.

With `spaceId` set to the app id, a connect to a server that runs another app fails with an
`AuthenticationError` that names both apps, instead of joining it. Without `userId`, a `spaceId`
also makes the SDK sign in anonymously to the app's platform for a user id. A Node program names that platform with
`backendType` (`'development'` for an app on `*.dev.ikonai.app`, `'production'` by default);
otherwise the sign-in for a development app fails with `403 Space ... not found`.

**Calling `[RequireLogin]` functions from a Node program.** The anonymous sign-in above gives a user
id, not a signed-in user, so a `[RequireLogin]` function of an app that lets no guests in refuses the
connection. Pass `apiKey` (a space API key from `ikon api-key create`) inside `local`, together with
`spaceId` and `backendType`: `local: { host: 'localhost', httpsPort, spaceId, apiKey, backendType }`.
The key is exchanged for a backend token, as in `apiKey` mode. It is ignored when `userId` or `token`
is also set. A `local` connection that names `userId` is a guest under that id (a local run accepts any id), so a `[RequireLogin]` function refuses it; use `apiKey` for a signed-in call.

A local run serves a development certificate. Browsers trust it once `ikon install certificate` has
added it to the computer's certificate store, but Node reads only its own list of authorities, so
a Node program's connect (or `fetch`) fails with `fetch failed`, caused by
`DEPTH_ZERO_SELF_SIGNED_CERT` or `UNABLE_TO_VERIFY_LEAF_SIGNATURE`. Start the program with
`NODE_OPTIONS=--use-system-ca` (Node 22.15 or later) so that Node also reads the computer's store
on Windows and macOS. On Linux, set `NODE_EXTRA_CA_CERTS` to the certificate exported with
`dotnet dev-certs https --export-path ikon-local.pem --format Pem`. A deployed app needs neither.

### Session Token Authentication

Use this when the session token was obtained from OAuth flow or anonymous authentication. The JWT token already contains the internal userId.

A program without a page (Node) joins in this mode as a `ContextType.Plugin` client and a page as `ContextType.Browser`, as in the `local` mode. Earlier than `@ikonai/sdk` 1.21, a `sessionToken` client is always a `Browser` client, which the app counts as a person on a page.

An API key's `externalUserId` names a user of the program's own: the same id is the same user on every connect, but it is never one of the app's existing people and has no email, so a role the app gives by email does not reach it. `ikon profile role add <role> --external-user <id>`, run in the app's folder, gives it one, even before its first connect. A surface that lists `Roles`, as every surface of an `ikon new --private` app does, refuses it until then. Acting as an existing person needs that person's session token in this mode, and the IKON tool hands out none; only the .NET SDK's `UserLogin` connects as the person signed in with `ikon login`.

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

When a reconnect presents a token the backend refuses (HTTP 401), because the sign-in was revoked or
the token expired without being renewed, the client goes `offline` and waits for `updateAuthToken`. It tells the
host through the `onCredentialRejected` callback. `useIkonApp` answers it by renewing the sign-in, or,
when the renewal is refused, by signing the person out and reloading the page. While the auth service
cannot be reached, it tries the renewal again every 30 seconds.

A deleted or wrong API key during a reconnect (HTTP 403) goes `offline` the same way and stops retrying: `client.lastError` and `onError` carry an `AccessDeniedError` (an `AuthenticationError`), but `onCredentialRejected` is not called, since it is only for a refused token (401). Only a new `IkonClient` with a working key connects again.

To open a second session next to the app connection — a media leg to a cell room, say — use
`createSibling`, which keeps the sibling's credential in step with this client's. A client can join
a cell only when the cell is declared `[Cell(Access = CellAccess.Clients)]`, or was built before cells
declared their access. Any other cell admits only the app's own instances and refuses the connect:

<!-- ikon-example: ts-sdk-create-sibling -->
```ts
const sibling = client.createSibling({ 'ikon-cell-type': 'room', RoomId: 'r1' });
```

`deriveConfig` still exists for building a config by hand, but the credential it returns is a
copy that is not updated when the token is renewed.

## Connection Lifecycle

### Connection States

The client tracks its connection state via the `connectionState` property:

| State | Description |
|-------|-------------|
| `idle` | No connection yet: a new `IkonClient` before `connect()`, and the React layer's state while a page of a surface whose `Connect` is `"onAction"` shows its boot snapshot and waits for the visitor's first action, or forever on a surface whose `Connect` is `"none"`. A client never returns to it |
| `waitingForExternalConnectUrl` | Waiting for external connect URL (used in preview mode) |
| `connecting` | Authentication and connection in progress, from the moment `connect()` is called; `onConnectionStateChange` reports it then |
| `connected` | Protocol channels connected; the session is joined and ready (GlobalState received, ClientReady sent, `onReady` called) shortly after. Function calls, actions and uploads made before that wait for it |
| `reconnecting` | Lost connection, attempting automatic reconnect |
| `offline` | Disconnected (timeout, server stopped, user-initiated, or a session that could not be resumed with `reconnectToNewSession: false`). An unsupported browser, an internal failure, a failed `connect()`, a timeout or a failed reconnect (a refused credential, no live session, a terminal answer) leaves its error on `client.lastError`, set before `onError` and the state callbacks run, and also reports it through `onError`. A client that was connected before moves to `reconnecting` after a failed `connect()` and keeps the error there until it connects. After your own `disconnect()`, `lastError` is `undefined`, so an `offline` with no `lastError` means you disconnected |

Earlier than `@ikonai/sdk` 1.19, a new client is already `connecting` before `connect()`, so
`onConnectionStateChange` is not called with `connecting` on its first connect.

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
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one

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

  // The server knows this client: calls and actions made from here on reach the app
  // (called on every join, reconnects included)
  onReady: async () => {
    // Load what the page needs from the app here
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

// GlobalState is null until it arrives, which can be after connect() resolves; onReady marks it
console.log(client.globalState);

// Notify the server of client context changes; false when it was dropped (not connected).
// Earlier than @ikonai/sdk 1.21 it returns nothing
const sent = client.sendUpdateClientContext({
  viewportWidth: window.innerWidth,
  viewportHeight: window.innerHeight,
  theme: 'dark',
});
console.log(`Context update sent: ${sent}`);

// Disconnect (connect() runs once per client; to connect again, build a new client)
client.disconnect();

// Access the last error (set when an unsupported browser, an internal failure or a failed preview-mode connect took the client offline)
if (client.lastError) {
  console.error(client.lastError);
}
```

A cloud connect that a full server refuses asks /init again up to three times, naming the servers that refused it, so a sharded app places the person on another shard; when none has room, `connect()` throws `ServerFullError`.

When the first connect fails, the client first goes `offline` (`onConnectionStateChange`), then calls `onError` with the error, and then `connect()` rejects with that same error. A credential the backend refuses, such as a deleted or wrong API key (HTTP 403 `Invalid key`), is an `AuthenticationError` whose message names the status; a sign-in for an app that is not available, or a guest sign-in for an app id that does not exist (the auth service answers 403 "Space … not found"), is a `SpaceNotFoundError`. Tell errors apart with `instanceof` or `error.name`; `error.constructor.name` is shortened in the published bundle. `onCredentialRejected` is not called for a first connect: it is only for a token refused while the client re-authenticates to reconnect.

### Automatic Reconnection

The SDK automatically attempts to reconnect when the connection is lost unexpectedly. Configure reconnection behavior:

<!-- ikon-example: ts-sdk-reconnection -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
  timeouts: {
    slowConnectionThresholdMs: 5000,   // Slow-connection threshold (drives isConnectingSlow in the React layer)
    connectionTimeoutMs: 180000,        // Connection timeout (3 minutes)
    reconnectBackoffMs: 2000,          // Fixed delay between reconnect attempts
    maxReconnectAttempts: 2,           // Attempts before a soft reconnect against the cached entrypoints, then full re-authentication
  },
});
```

The first reconnect attempt is immediate; each later one waits a fixed delay (2s by default).

When the client cannot start a new session, it tries again after 10 seconds, doubling the wait after each failure up to 5 minutes. The wait goes back to 10 seconds once it connects. A 429 from the backend makes it wait at least its `Retry-After`. It stops and goes `offline` when retrying cannot help: a refused sign-in (401, 403), an app that is not available (404, 410: suspended, unpublished or not deployed, raised as `SpaceNotFoundError`), an SDK or app build the platform no longer runs (426), a full app or a failed app start.

With `reconnectToNewSession: false` the client only ever resumes the session it has. Where it would authenticate a new one, it goes `offline` with `client.sessionLost` set, and the caller decides when to connect again. The React layer sets it for a surface with `Recovery = "seamless"`.

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

Only an `opcodeGroupsMask` without `opcodes` narrows what the protocol worker forwards to the main
thread. A subscriber with `opcodes` or without `opcodeGroupsMask`, or an `onProtocolMessage` in the
config, makes the worker forward every message, media frames included, and the opcode filter then
runs on the main thread.

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

### Other Clients

The client events say who else is in the session. Only clients in the same app instance, which the app's `SessionIdentity` decides, and on a surface with `Presence = true` are reported; a client on a surface without presence is reported to the app alone. Each event carries the other client's `Context`
(`SessionId`, `UserId`, `ContextType`, and what it reports of its viewport, theme, timezone and
visibility). A client counts as joined once it is ready: it has finished joining, the app knows who
it is, and it can be reached. `onClientJoined` is called then, and on joining it is called once for
each client already there. `onClientUpdated` and `onClientLeft` are called only for a client
`onClientJoined` reported, so a client that leaves before it is ready is never reported at all. None
of them is called about this client itself.

Each client is reported once per session. After a reconnect that resumes the same session, the
clients that stayed in it are not reported again. A client whose viewport, theme, timezone or
visibility changed while this one was away is reported through `onClientUpdated`, and one that left
meanwhile through `onClientLeft`. A reconnect into a new session reports its clients as a first join
does. Needs `@ikonai/sdk` 1.19 or later.

A client whose connection drops is not reported as gone at once: the server holds its session for the
soft-retry window, 5 minutes by default, so it can resume. Until then its `Context` has
`IsSoftDisconnected` true and `SoftDisconnectAt` set, and `onClientLeft` is called when the window has
passed. `PreciseJoinedAt` and `SoftDisconnectAt` are `bigint`, which `JSON.stringify` refuses, so print
a `Context` with a replacer: `JSON.stringify(other, (_, value) => typeof value === 'bigint' ? value.toString() : value)`.

<!-- ikon-example: ts-sdk-client-events -->
```ts
import { ContextType } from '@ikonai/sdk';

// The other clients in the session. The app is one of them: a ContextType.Plugin client with IsInternal
// true, which gets a new SessionId each time the app restarts or hot reloads. Each returns an unsubscribe function.
// ContextType, UserType and ClientType are numeric enums: ContextType[other.ContextType] is the name.
const unsubscribers = [
  client.onClientJoined((other) => console.log(`${other.SessionId} joined as ${ContextType[other.ContextType]}, user ${other.UserId}`)),
  client.onClientUpdated((other) => console.log(`${other.SessionId} now uses the ${other.Theme} theme`)),
  client.onClientLeft((other) => console.log(`${other.SessionId} left`)),
];

// Later: stop listening
for (const unsubscribe of unsubscribers) {
  unsubscribe();
}
```

### Sending Messages

<!-- ikon-example: ts-sdk-send-messages -->
```ts
// Send a protocol message
client.sendProtocolMessage(message);

// Send an action call: actionId is a UI node's action id (a GUID, such as a button's onInvokeId).
// A button's payload is '{}', a text field's is the text itself (the table under "Custom UI Renderers")
client.sendActionCall(actionId, '{}');
```

`sendProtocolMessage` returns false when the client has no connection to send on, or no channel of
it carries the message's opcode group, and the message is dropped.

`sendActionCall` returns nothing and reports no failure. `sendActionCallWithResult(actionId, payload, timeoutMs)`
returns a promise of `{ success, errorMessage }`, which resolves when the app's handler returns:
`success` is false when it failed. `errorMessage` is empty when the handler threw, and `Unknown action` when the app has no such action for this client: one it was not given, or one the app has retired. It rejects when no result
arrives within `timeoutMs`, 15000 by default, and the outcome is then unknown, not failed. A handler
that waits on an AI call can take longer, so pass a longer timeout. Calling again after a rejection
runs the handler a second time.

### App Messages

`appMessaging(client)` sends and receives an app's own `schema/*.tp` messages, the counterpart of
the app's `app.SendMessageAsync` and `app.OnMessage<T>`; the `realtime-game-loop` and
`device-typed-messages` patterns show both sides. The app's build, or `ikon schema` in the app's
folder, compiles each schema file into a module in `frontend-node/src/generated/protocol/`
(`ChatLine.tp` becomes `chat-line.ts`), and into every `typescript` folder `schema/outputs.toml`
names, which is how a program outside the app's folder gets them (the Teleport schema spec,
`teleport-schema-spec.md`). Each module exports the message's interface and `<Name>Message`, which
`send` and `on` take as it is, so the program never names an opcode. A module that an older `ikon` generated has no
`<Name>Message`. Build the app again with a current `ikon` to add it. If you cannot change the app, run `ikon teleport generate --input <app>/schema --type typescript --output src/generated/protocol` in the program's own folder instead. It writes the same modules with the same opcodes and changes nothing in the app. An `int64` field arrives as a `bigint` (`line.Id` is `12n`, not `12`), and a `bigint` goes in when sending.

<!-- ikon-example: ts-sdk-app-messages -->
```ts
import { appMessaging } from '@ikonai/sdk';
// Written by the app's build from schema/ChatLine.tp
import { ChatLineMessage } from './generated/protocol/chat-line';

const messaging = appMessaging(client);

// Subscribed before connect(), so a message the app sends as soon as the client joins is not missed
const subscription = messaging.on(ChatLineMessage, (line, senderId) => console.log(`${senderId}: ${line.Author}: ${line.Text}`));

await client.connect();

// The fields keep the schema's names; false when the client had no connection to send it on
const sent = messaging.send(ChatLineMessage, { Author: 'Ada', Text: 'Hello' });

// Later: stop receiving
subscription.close();
```

The generated interfaces keep the schema's field names, usually PascalCase (`Author`), and
`AppMessaging.send` throws a `TypeError` for an object that has none of the type's fields
(`{ author }`), which would otherwise arrive with every field empty. `AppMessaging.send` returns
false, and logs a warning for the first such send, when the client has no connection to send on.
`AppMessaging.on` can subscribe before `connect()`, and the subscription lasts across reconnects.
A client receives app messages only while its `opcodeGroupsFromServer` includes
`Opcode.GROUP_APP_LOCAL`, as the default does. The app logs a warning, once per type, for an app
message that arrives with no `app.OnMessage<T>` handler. In the app, the `senderId` that
`app.OnMessage<T>` gets for a message from this client is `client.sessionId`, the same number as
`ReactiveScope.ClientId` and `Context.SessionId` for this client, and
`app.SendMessageAsync(reply, senderId)` answers only this client. For a message the app sent, the
`senderId` an `AppMessaging.on` handler gets is the app's own session id, the `SessionId` of its
`ContextType.Plugin` client (see "Other Clients"). It changes each time the app restarts or hot
reloads, so a program should not keep it.

### Uploading Files

`uploadFile(client, { file, uploadId, uploadActionId, onProgress })` sends one file to an upload
action of the app. A file upload component on a page has its action id in the UI tree, as the
`uploadActionId` prop of its node (see "UI State Management"). The app generates that id, so a
program cannot know it before the UI tree arrives. A program uses a fixed id instead, one the app
registers in C# with `app.Uploads.Register("live-poll.votes-csv", onStart, onComplete)`. `onStart`
returns a `FileUploadResult` that accepts the file (`true`) or refuses it (`Accepted = false`).

An upload carries only the file: the app's callbacks get its upload id, file name, MIME type, size,
hash and relative path, and nothing else the program adds. To say what the file is for (the record it belongs to), the
program first calls a function of the app with the upload id and that information. The app keeps
the pair, and its `onStart` accepts only an upload id it was told about and refuses any other.

<!-- ikon-example: ts-sdk-upload-file -->
```ts
import { openAsBlob } from 'node:fs';
import { randomUuid, uploadFile, UploadFailedError } from '@ikonai/sdk';

// In Node; a page passes the File an <input type="file"> gives it
const file = new File([await openAsBlob('votes.csv')], 'votes.csv', { type: 'text/csv' });

// Chosen by the program, a new one for each attempt; the app sees it in each upload callback
const uploadId = randomUuid();

// An upload carries only the file, so first tell the app what the file is for. The app's
// function keeps the id, and its onStart accepts only an upload id it was told about.
await client.call('poll.expectVotes', [pollId, uploadId]);

try {
  await uploadFile(client, {
    file,
    uploadId,
    uploadActionId: 'live-poll.votes-csv', // The id the app passed to app.Uploads.Register
    onProgress: (acknowledgedBytes, totalBytes) => console.log(`${acknowledgedBytes} of ${totalBytes} bytes`),
  });
  console.log('The app has the file');
} catch (error) {
  if (!(error instanceof UploadFailedError)) {
    throw error;
  }

  // retriable: sending the same file again may work (a stall or a dropped connection)
  console.error(`Upload failed: ${error.message} (retriable: ${error.retriable})`);
}
```

The program chooses `uploadId`, a new one for each attempt. The server refuses an id it still
holds, and it keeps an upload whose connection dropped for about a minute, longer than the client
waits for it. `onProgress` gets the bytes the server has acknowledged so far. The chunks grow as
the upload speeds up, so the count rises in steps of different sizes. The promise resolves once the
app has the file: every byte arrived and matched the file's size and SHA-256 hash, and
`onComplete`, if the app gave one, returned. Otherwise it rejects with an `UploadFailedError`.
`retriable` is true when sending the file again may work, such as after a stall, or after a
connection that dropped and did not come back within 45 seconds. The upload carries on by itself
across a shorter drop. `retriable` is false when the server refused the upload, such as with
`The app rejected the upload` when `onStart` refused the file or no upload action has that id; a refusal at the pre-start step adds the app's reason, `The app rejected the upload: <reason>`. It is
also false when the client cannot learn the outcome after the whole file was sent, and that error's
message says to check whether the file was stored before sending it again. A call before the client has a session rejects at once with
`Session not ready`. A client sends up to 8 files at once, and the rest wait their turn.

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

### Streaming Results to the App

A function registered with `isEnumerable: true` returns an async iterable (an `async function*`) or
an iterable, and the app reads its items as they are yielded, as from a C# `IAsyncEnumerable<T>`.
The app calls the function with `CallAsyncEnumerable`, naming this client's session as the target.
`returnType` describes one item. Items ready together go out in one message of up to 50, and an
item followed by a pause goes out within 100 ms. When the app stops reading, the iteration stops.
The generator returns at its next `yield`, so its `finally` runs. The iteration stops the same way
when the client's connection drops, since items sent while it is down would be lost. If the client
resumes the same session, the app's call fails with `System.IO.IOException`. If it joins a new
session instead, the call ended with the old session. A function not registered with
`isEnumerable` that returns an async iterable fails the call with
`Function <name> returned a stream; register it with isEnumerable: true`. Needs `@ikonai/sdk` 1.19
or later.

<!-- ikon-example: ts-sdk-streaming-function -->
```ts
const readingsDefinition: FunctionDefinition = {
  name: 'readTemperatures',
  description: 'Reads the temperature count times',
  isEnumerable: true,
  returnType: { kind: 'number' }, // the type of one item
  parameters: [{ name: 'count', type: { kind: 'number' } }],
};

client.functionRegistry.register(readingsDefinition, async function* (count) {
  try {
    for (let i = 0; i < (count as number); i++) {
      yield await readTemperature();
    }
  } finally {
    // Runs when the stream ends, fails or is cancelled by the app
    console.log('Stopped reading');
  }
});
```

### Remote Function Calling

Call functions the app server registers (another browser client's functions are not reachable).
`client.call` and `client.callStream` are the same methods as `client.functionRegistry.call` and
`client.functionRegistry.callStream`; this guide uses the shorter form.
A call made while the client connects or reconnects, such as one straight after
`await client.connect()` or one made while the connection is down, waits until the client has
sent ClientReady and goes out then, in the order the calls were made. Earlier than `@ikonai/sdk`
1.17, a call made while reconnecting fails at once with `Not connected`. The function is looked up
in the list of functions the app sends on that join, after waiting up to 10 seconds for the list,
so a name the list does not have fails with `Remote function not found`, and so does a function
the app no longer has after a reconnect into a new session. The wait counts against the call's
timeout, which is 30 seconds unless the call's `timeoutMs` option sets another (0 for none). A call
that could not be sent in time fails with
`Function call '<name>' timed out after 30000ms: the client was not connected`, and one sent but
not answered in time fails with `Function call '<name>' timed out after 30000ms` and is cancelled on
the app. At most 4096 calls wait, and later ones fail at once.
A call fails at once with `Not connected` when the client is not trying to connect: before
`connect()`, after `disconnect()`, after the server stopped, or when the reconnect has given up.
A waiting call fails the same way when the client reaches one of those states. A call already sent when the
connection drops, or when the program calls `disconnect()`, fails with a `DisconnectedError` (message `Disconnected`, exported by `@ikonai/sdk`). Whether the app ran the function is unknown, so retry it after the client is connected again only when the function can safely run twice; a call that never got sent fails with `Not connected` or a timeout instead, and is safe to send again.
`getRemoteFunctions()` is empty until the list arrives, and again from a dropped connection until
the next join's list arrives, so await `waitForRemoteFunctions()` (`@ikonai/sdk` 1.17 or later)
first, which waits the same 10 seconds at most. It resolves to true once the list has arrived, and to
false after the 10 seconds, or at once when the client is not connecting or connected (from
`@ikonai/sdk` 1.19; earlier it resolves with nothing either way).
Each entry of `getRemoteFunctions()` (and `getRemoteFunction(name)`) is a `RemoteFunction`: `name`,
`id`, `description`, `resultTypeName`, `isEnumerable`, `requiresInstance`, `clientSessionId` (the
session that registered it) and `parameters`. `parameters` is the protocol's list as the app sent it,
with PascalCase fields: `ParameterName`, `TypeName` (the C# type's full name, such as
`System.String` or `System.Int32`), `HasDefaultValue`, `DefaultValueJson` (JSON text, empty without a
default), `Description`, `IsEnumerable` and `ParameterIndex`, in call order.
A function that returns a C# record or class resolves to the parsed JSON, with the C# property
names as they are (`{ Name, SinceUtc }`, PascalCase) and enums as their names, so type the call
with an interface that has those names:

<!-- ikon-example: ts-sdk-remote-call -->
```ts
import { FunctionCallError } from '@ikonai/sdk';

// Call a remote function by name (client.call is client.functionRegistry.call)
const result = await client.call<string>('remoteGreet', ['World']);

// List all remote functions, once the server's list has arrived; false after 10 seconds without it
if (await client.functionRegistry.waitForRemoteFunctions()) {
  const remoteFuncs = client.functionRegistry.getRemoteFunctions();
  console.log(`Remote functions: ${client.functionRegistry.remoteSize}`);
}

// Get a specific remote function
const func = client.functionRegistry.getRemoteFunction('remoteGreet');

// Handle remote call errors. Only a remote failure or an unparseable result is a FunctionCallError; not
// connected, an unknown name, a missing instanceId, the timeout and a disconnect reject with a plain Error.
try {
  await client.call('riskyOperation');
} catch (error) {
  if (error instanceof FunctionCallError) {
    console.error(`Remote error: ${error.remoteErrorType}`);
    console.error(`Stack trace: ${error.remoteStackTrace}`);
  }
}
```

### Streaming and Cancelling Calls

A function the app declares as returning `IAsyncEnumerable<T>` streams its results: call it with
`callStream`, whose loop gets each item as the app yields it (`RemoteFunction.isEnumerable` says
which functions do). `call` on such a function fails at once with
`Function '<name>' streams its results; call it with callStream()`, and `callStream` on any other
with `Function '<name>' returns one result; call it with call()`. Leaving the loop early cancels the
call on the app. Both take a `signal` (an `AbortSignal`) and a `timeoutMs`. Aborting the signal
fails the call with the signal's reason and cancels it on the app. For `callStream`, `timeoutMs`
limits the wait for each next item, 30 seconds by default, and a stream that waits longer throws
`Function call '<name>' timed out after 30000ms waiting for its next item`. A byte array item
arrives as a `Uint8Array`, any other as its parsed JSON. Items the loop has not read yet are kept,
up to 4096, after which the call is cancelled. Needs `@ikonai/sdk` 1.19 or later; earlier, a call
to a streaming function times out after 30 seconds.

<!-- ikon-example: ts-sdk-streaming-call -->
```ts
// A C# function returning IAsyncEnumerable<int>: each item arrives as the app yields it
for await (const value of client.callStream<number>('count_to', [5])) {
  console.log(value);
}

// Leaving the loop early cancels the call on the app
for await (const value of client.callStream<number>('count_to', [1000])) {
  if (value === 3) {
    break;
  }
}

// An AbortSignal cancels either kind of call; timeoutMs bounds a call (0: no limit), and for a
// stream the wait for each next item
const controller = new AbortController();
setTimeout(() => controller.abort(), 5000);
const report = await client.call<string>('build_report', [], { signal: controller.signal, timeoutMs: 120_000 });
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

// A reactive of the app's [App] class, by the member's name. The callback runs once with the current
// value, then on every change. Changes made close together can arrive as one call with the latest value.
const unsubscribe = await client.reactiveRegistry.subscribeByName<number>('_visitorCount', (count) => {
  console.log(`Visitors today: ${count}`);
});

// Later: stop receiving changes
unsubscribe();
```

`subscribeByName` finds a reactive field or auto-property of the app's `[App]` class by its name in
the source, through the app's `Ikon.Reactive.GetStableIdByName` function, and rejects with a
`FunctionCallError` when the app has no reactive by that name. For a reactive kept anywhere else,
the app returns its `StableId` from an `External` function, and the program passes that id to
`client.reactiveRegistry.subscribe`. `subscribeByName` needs `@ikonai/sdk` 1.19 or later; earlier,
call `Ikon.Reactive.GetStableIdByName` with the name and pass the id it returns to `subscribe`.
The program sees the instance its identity joins, so state that the app shares
with everyone belongs in an app with one instance (`public record SessionIdentity();`) or in a
`GlobalReactive` or `PersistentGlobalReactive` field, which every instance shares. The program
usually sees another instance's write within seconds. A counter there uses `Update(v => v + 1)`,
because an assignment is last-writer-wins. The value arrives as
parsed JSON with the C# property names. A JSON object's keys are always strings, so a C# dictionary
with numeric keys arrives as an object whose keys are the numbers as text (`Dictionary<int, string>`
`{ [1] = "a" }` is `{ "1": "a" }`; read it with `value["1"]` or `Object.entries`). The callback gets the value as it is when the app sends it,
and is not called once per change. Changes made close together, such as five calls in a row that each add an item,
can arrive as fewer calls, the last of them with the latest value. Any client that joins, a guest
included, can subscribe to
every reactive of the `[App]` class, so keep what a guest must not see out of shared reactives.
After a reconnect the client subscribes to each of them again, and `client.disconnect()` ends them.

When `subscribe` or `subscribeByName` resolves, its callback has already run with the current
value. A program that needs the value only once keeps it and unsubscribes at once:

<!-- ikon-example: ts-sdk-read-reactive-once -->
```ts
// When subscribeByName resolves, the callback has already run with the current value
let question = '';
const unsubscribe = await client.reactiveRegistry.subscribeByName<string>('_question', (value) => {
  question = value;
});
unsubscribe();

console.log(`The question is: ${question}`);
```

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
  let retryScheduled = false;

  const client = new IkonClient({
    guest: { spaceId: 'your-app-id', backendType: 'production', deviceId: 'lobby-screen' }, // 'development' for an app on *.dev.ikonai.app
    webRtc: { media: 'none' },
    onConnectionStateChange: (state) => {
      // A failed connect and a dropped server can both report offline: schedule the next client once
      if (state === 'offline' && !retryScheduled) {
        retryScheduled = true;
        setTimeout(() => {
          client.disconnect();
          watchVisitors();
        }, 15_000);
      }
    },
  });

  void (async () => {
    await client.connect();
    await client.reactiveRegistry.subscribeByName<number>('_visitorCount', (count) => {
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

A new client does not fix a credential the backend refuses (a deleted API key, a guest the app does not admit): `connect()` rejects with an `AuthenticationError` and the loop above would try again every 15 seconds forever. A watcher that should stop then keeps the timer and clears it when the connect fails that way:

<!-- example-skip: a fragment of the watcher above, not a program of its own -->
```ts
// The offline handler keeps its timer: `retry = setTimeout(...)`
let retry: ReturnType<typeof setTimeout> | undefined;

// ...and the connect's catch stops it, because the refused connect went offline first
if (error instanceof AuthenticationError) {
  clearTimeout(retry);
  return;
}
```

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
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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

The SDK carries audio and video over WebRTC. The surface's `Media` in `ikon-config.toml`, which a page passes on, says whether the peer is negotiated: with the connection (`atConnect`, the default) or never (`none`). With the connection means as soon as the app's first UI has arrived, or a second after connecting for an app that sends none, so the first frame is not slowed down by the peer's setup. A program sets it in the configuration:

<!-- ikon-example: ts-sdk-webrtc -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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

### Audio and Video Without a Page

A program without a page (Node) has no microphone, camera, playback or WebRTC, so it sends and
receives media as frames through `client.rawMedia` (`@ikonai/sdk` 1.19 or later). A client's media
reaches only the app. `sendAudio` encodes PCM to Opus (it needs `@ikonai/sdk-libopus`) at 8, 12, 16,
24 or 48 kHz in 20 ms frames. Samples that do not fill a frame wait for the next call, and `isLast`
pads the last frame. `sendOpusFrame` sends a frame already encoded. The app's speech recognition
skips audio without a `correlationId`, so give one to audio meant for it. `sendVideoFrame` sends one encoded frame: for H.264 one access unit (every slice of one
picture) in Annex B form with start codes, each keyframe with its SPS and PPS before the IDR slice, because a receiver
decodes from the parameters in the stream. ffmpeg's libx264 writes such a stream with `-c:v libx264 -x264-params aud=1:repeat-headers=1 -f h264`: an access unit delimiter starts each picture, so a reader of its output splits the frames there, and every keyframe repeats the SPS and PPS. It announces the stream first and again when its size, codec, source type, correlation id or frame rate changes; the
SDK does not encode video. `sendVideoFrame` is fire-and-forget: it reports no result, so check on the app's side that frames arrive (`ikon logs`). In `local` mode a Node program shows as a Plugin client to the app, under the `userId` it gave, else the id the anonymous sign-in or the device id supplies. A reconnect into a new session announces every stream again before its
next frame. Sending throws when the client is not connected. Received streams are named
`"{senderId}_{trackId}"`, and `senderStreamId` is the id the sender gave; `requestVideoKeyFrame`
takes the received id. A dropped connection ends every received stream, and the app begins those
still open again when the client rejoins.

`client.globalState.AudioStreams` and `VideoStreams` hold the streams that were open when the
`GlobalState` arrived, keyed by the same `"{senderId}_{trackId}"` id, each with its `ClientSessionId`,
`TrackId` and begin `Info`. The SDK does not change them afterwards, so follow later streams with
`rawMedia.onAudioStreamBegin`, `onVideoStreamBegin` and their `End` events. An app's audio clip ends with
`isLast` on its last frame, and the stream usually stays open for the next clip, so `onAudioStreamEnd`
does not fire after a clip: wait for the frame with `isLast`.

<!-- ikon-example: ts-sdk-raw-media -->
```ts
// Send PCM (interleaved floats in -1..1), encoded to Opus 20 ms frames; isFirst/isLast mark a segment
await client.rawMedia.sendAudio(pcm, { streamId: 'mic', sampleRate: 48000, channels: 1, isFirst: true, isLast: true, correlationId: 'press-1' });

// Send video frames already encoded (H.264 in Annex B form, VP8, VP9 or AV1); a keyframe first
encodedFrames.forEach((data, i) => {
  client.rawMedia.sendVideoFrame('camera', { data, isKey: i === 0, timestampUs: i * 33_333, codec: 'h264', width: 640, height: 480 });
});

// The app asks for a keyframe when it joins a stream mid-way or loses a frame
client.rawMedia.onVideoKeyFrameRequested(({ streamId }) => {
  client.rawMedia.sendVideoFrame(streamId, { data: keyFrame, isKey: true, timestampUs: 0, codec: 'h264', width: 640, height: 480 });
});

// Receive the app's audio, decoded, and its video as encoded frames. A stream's id is "{senderId}_{trackId}"
client.rawMedia.onAudioFrame((frame) => console.log(`${frame.streamId}: ${frame.samples?.length} samples`), { decode: true });
client.rawMedia.onVideoStreamBegin((info) => console.log(`${info.streamId} (${info.senderStreamId}): ${info.codec} ${info.width}x${info.height}`));
client.rawMedia.onVideoFrame((frame) => console.log(`${frame.streamId}: ${frame.data.length} bytes, key ${frame.isKey}`));

client.rawMedia.endVideoStream('camera');
client.rawMedia.endAudioStream('mic');
```

## UI State Management (sdk-ui)

The `@ikonai/sdk-ui` library provides framework-agnostic UI state management for rendering IKON UI updates.

### IkonUiCore

The core class handles protocol messages and maintains UI state. `uiWorker: 'auto'` runs the UI
state in a Web Worker where the runtime has `Worker`, and on the main thread where it has none, as in
Node. The build-time boot snapshot (`bootSnapshot`) is read only on a page, so a program without one
can leave it at its default.

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
  // update.snapshot contains the parsed UI tree; uiCore.uiStore already holds this update
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
import type { UiNode } from '@ikonai/sdk-ui';

// An array with one snapshot per stream (the page, the platform's layer, other mounts), each with its
// streamId and category. Each holds views and payloads as Maps, which JSON.stringify prints as {}
const snapshots = uiCore.uiStore.getSnapshots();

// Get specific stream
const snapshot = uiCore.uiStore.getSnapshot(streamId);

// Print a stream's UI tree, walking it from the root view
function printNode(node: UiNode, depth = 0): void {
  console.log(`${'  '.repeat(depth)}${node.type} ${JSON.stringify(node.props)}`);
  node.children.forEach((child) => printNode(child, depth + 1));
}

const root = snapshot?.rootViewId ? snapshot.views.get(snapshot.rootViewId) : undefined;

if (root) {
  printNode(root);
}
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
| `std.code-editor` | `value`, `language`, `placeholder`. A `label:` is a `std.text` node just before the editor, and also its `aria-label` | `onValueChangeId`, the text itself; `onInvokeId` on Ctrl+Enter, the text itself |
| `std.file-upload`, `std.file-upload-zone` | `uploadActionId`, a prop holding the id an upload names | None: pass the id to the upload call |
| A control declared with a `path`: `std.button`, `std.nav-item` and others | `actionPaths`, a JSON object from the control's action id to the app path its press opens (`/stable/{value}` for a control that sends a value) | The renderer moves the page itself. To do the same, pass the path to `sendActionCall(actionId, payload, { path })`; without it the app moves the client |
| `std.button` with an `href` (a link) | `href`, `target`, and the text as on a button | None unless the app also handles the press (`onInvokeId`, `{}`). A client follows the `href` itself |
| `std.action-button` | `label`, `actionType` (`CopyToClipboard`, `DownloadFile`, `Share` and the others) and `actionOptionsJson`, the action's options as JSON | The action runs in the browser, so a client does it itself. `onActionCompleteId` reports it, `{"ActionType":"CopyToClipboard","Success":true}` |
| `std.toast` | None itself: its title is a `std.text` inside a `std.toast-title` child and its description one inside a `std.toast-description` child. It sits in a `std.toast-provider` parent, and `open` is its prop | None |
| `std.feed-scroller` | `activeIndex` when the app controls it, `slideCount`, `muted`. Each slide is a `std.feed-slide` with its `index` | `onActiveChangeId` when a slide becomes the active one and `onScrollNearEndId` near the end, the slide's index as a number (`3`); `onMuteChangeId`, `true` or `false` |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

A payload the app cannot read, such as `0.8` for a slider instead of `[0.8]`, fails the call like a handler that throws: `sendActionCallWithResult` resolves with `success: false` and an empty `errorMessage` (not the `Unknown action` of a missing action), and the reason is only in the app's log (`ikon logs --level warn`).

The `sdk-ui` library is designed to be framework-agnostic. For React integration, see `@ikonai/sdk-react-ui`.

## Advanced Configuration

### Timeouts

<!-- ikon-example: ts-sdk-timeouts -->
```ts
const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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

### Post-Quantum Key Exchange

IKON's deployed servers and load balancers negotiate the post-quantum group X25519MLKEM768 with any client that offers it. `requirePostQuantum: true` makes the client use only connections whose key exchange is post-quantum, with no fallback. It connects over WebSocket or the HTTP stream over TLS, and never over WebTransport or WebRTC, so audio and video go over the protocol channel. Plain connections to `localhost` are also allowed. When the server offers nothing the client may use, `connect()` fails with `NoPostQuantumEndpointError`. An app can also set `RequirePostQuantum = true` in its `ikon-config.toml`, and then its server offers only those transports to every client.

- **Node:** the constructor sets `tls.DEFAULT_ECDH_CURVE` to `X25519MLKEM768` and `tls.DEFAULT_MIN_VERSION` to `TLSv1.3`. These settings apply to the whole process. Every TLS connection the process opens afterwards, including your program's own `fetch` calls, offers only that group, and a server that cannot negotiate it fails the handshake. Create the client before anything else connects, because `fetch` keeps reusing a connection opened earlier with the key exchange it started with. Node's OpenSSL (`process.versions.openssl`) must be 3.5 or later, the first with ML-KEM, or the constructor throws. A local run on Windows cannot negotiate the group, so use the flag against a deployed app or a local run on Linux.
- **Browser:** a page cannot choose its TLS groups. The browser chooses them, and every current browser offers X25519MLKEM768 first. The flag still keeps WebTransport and WebRTC off, and logs once that the browser chooses the key exchange.

<!-- ikon-example: ts-sdk-require-post-quantum -->
```ts
// In Node, before anything else opens a TLS connection: the constructor changes Node's TLS defaults for the whole process
const client = new IkonClient({
  apiKey: { apiKey: '...', spaceId: '...', externalUserId: 'worker-1' },  // or local / sessionToken / guest: exactly one
  requirePostQuantum: true,  // only X25519MLKEM768 over TLS 1.3; never WebTransport or WebRTC
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

When the app offers feedback (a platform setting, changed in Studio or with `ikon feedback set`), `IkonApp` draws the platform's feedback button and sheet. The button's label is in the language of the app's `locale` option (`useIkonApp().locale`), or of the browser without it, and in English for a language it has no label in. To place a "Send feedback" control of your own, use `useIkonFeedback` with the app's client:

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
| `ikon-proxy` | `false`, `true`, `lb` | Pin the connect tier: the server's own host and port, the on-host proxy on 443, or the app's own origin through the fleet gateway (`stream` is an alias for `lb`). Ignored, as is the `proxy` option, when the client connects with `local` |
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
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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

The server draws the new page some time after the press, so the page scrolls when that page is
drawn, not at the press: to the top after a link, and back to where the visitor left it after Back
or Forward. Until then the page being left stays where it was. The client remembers the window's
position for each history entry and sets `history.scrollRestoration` to `manual`, because the
browser would restore it while the page being left is still on screen. The positions are kept in
`sessionStorage`, so a reload, or a return from another site, opens the entry where the visitor left
it. A fragment link on the page shown is left to the browser. A move whose page changes nothing on
screen scrolls after one second. `IkonUiCore` provides the wait; a UI layer of your own provides it
with `IkonClient.setRouteScrollScheduler`, and a client used without one scrolls at once.

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
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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
| `SpaceNotFoundError` | The app is not available: suspended, unpublished, removed or never deployed (HTTP 404 or 410). Terminal, and the sign-in stays valid |
| `AccessDeniedError` | Server denied access (e.g., domain allowlist blocks email domain) |
| `ServerFullError` | Every instance the connect may use is at its client limit, after asking /init for another up to three times (terminal) |
| `BrowserNotSupportedError` | Browser lacks required runtime features (terminal, exposes `missingFeatures`) |
| `NoMatchingEndpointTypeError` | `?ikon-transport` / `?ikon-proxy` pin a combination the server did not offer (terminal, exposes `pinnedTransport`, `pinnedTier`, `offeredTypes`) |
| `AppStartupFailedError` | The app's server failed during startup (terminal) |
| `SsoRequiredError` | The app requires signing in through the organisation's SSO (extends `AccessDeniedError`, exposes `connectionId`) |
| `CredentialRejectedError` | Backend rejected the presented credential, HTTP 401 (terminal) |
| `SdkUpgradeRequiredError` | The platform no longer accepts this SDK version, HTTP 426: update the app's IKON packages with `ikon update` and deploy it again (terminal, exposes `reason`, the platform's message) |
| `AppUpgradeRequiredError` | The platform no longer runs the app's deployed bundle, HTTP 426, because it was built on IKON libraries that are too old: the app's developer runs `ikon update` and deploys again (terminal, exposes `reason`) |
| `SessionNotFoundError` | `?ikon-session` names no live session (terminal, exposes `sessionIdentityHash`) |
| `NoLiveSessionError` | A `joinOnly` connect found no running session and started none (terminal; exposes `spaceId`, `retryAfterMs` (the backend's suggested wait), and the `externalUserId` and `sessionIdentityHash` it looked for, which the message also names. With a per-user `SessionIdentity`, a session running for another external user is not joined) |
| `ServerUnavailableError` | Backend returned HTTP 5xx (retried, exposes `status`) |
| `ServerRateLimitedError` | A rate limiter refused the request with HTTP 429: the ikon server's, which counts per address, or the backend's. The connect asks an ikon server again after the wait it named, 30 s at most, and raises this when its budget runs out while a rate limiter's refusal is the most telling answer it got. Says nothing about the sign-in (extends `ServerUnavailableError`, exposes `retryAfterMs`) |
| `ServerNotReadyError` | The connect ran out of its budget on an ikon server that kept not answering, refusing the connection or answering 5xx. A credential refusal on any connect URL is raised as `AuthenticationError` instead, and a rate limiter's as `ServerRateLimitedError` (extends `ServerUnavailableError`, exposes `status`, 0 when the try got no answer) |
| `ServerChangedError` | The ikon server refused the connect token as not its own or expired, usually because the server was replaced. A cloud connect gets a fresh token from `/init` once before raising it; a local or connect-URL connect, which has no `/init` to ask, raises `AuthenticationError` instead. Not an `AuthenticationError`: the sign-in is still valid (exposes `status`) |

In a browser the protocol runs in a Web Worker, and errors raised there reach `onError` as a plain `Error` that keeps only the `name` and `message`, so identify transport-level errors by `error.name` rather than `instanceof`.

<!-- ikon-example: ts-sdk-error-handling -->
```ts
import { AuthenticationError } from '@ikonai/sdk';

const client = new IkonClient({
  local: { host: 'localhost', httpsPort: 8443 },  // or apiKey / sessionToken / guest: exactly one
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
| `ConnectionState` | Union type: `idle`, `waitingForExternalConnectUrl`, `connecting`, `connected`, `reconnecting`, `offline` |
| `ProtocolMessage` | Raw protocol message (Uint8Array) |
| `GlobalState` | Server state received after connection |
| `Context` | A client as the server describes it, which the client events carry |
| `ClientListener` | The callback of `onClientJoined`, `onClientUpdated` and `onClientLeft` |

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
| `FunctionDefinition` | Function metadata (name, parameters, return type, `isEnumerable` for one that streams its results) |
| `FunctionHandler` | Handler callback type |
| `ValueDescriptor` | Type descriptor for parameters/returns |
| `FunctionResultWithData` | Result type with binary data attachment |
| `FunctionCallError` | Error from remote function call with `remoteErrorType` and `remoteStackTrace` |
| `FunctionCallOptions` | `instanceId`, `signal` and `timeoutMs` of one `call` or `callStream` |
| `RemoteFunction` | An app function the client can call, with `isEnumerable` for a streaming one |

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
| `RawMedia` | Audio and video as frames for a program without a page (`client.rawMedia`) |

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
| `SpaceNotFoundError` | App not available (terminal) |
| `AccessDeniedError` | Server denied access (e.g., domain allowlist) |
| `ServerFullError` | No instance with room (terminal) |
| `BrowserNotSupportedError` | Browser missing required features (terminal) |
| `NoMatchingEndpointTypeError` | Pinned transport/tier not offered by the server (terminal) |
| `NoPostQuantumEndpointError` | `requirePostQuantum` is set and the server offered no post-quantum transport (a `NoMatchingEndpointTypeError`) |
| `AppStartupFailedError` | App server failed during startup (terminal) |
| `SsoRequiredError` | Organisation SSO sign-in required |
| `CredentialRejectedError` | Credential rejected, HTTP 401 (terminal) |
| `SdkUpgradeRequiredError` | SDK version no longer accepted, HTTP 426 (terminal) |
| `AppUpgradeRequiredError` | App bundle built on IKON libraries too old to run, HTTP 426 (terminal) |
| `SessionNotFoundError` | `?ikon-session` names no live session (terminal) |
| `ServerUnavailableError` | Backend HTTP 5xx (retried) |
| `ServerRateLimitedError` | HTTP 429 from a rate limiter (retried after its wait, 30 s at most) |
| `ServerNotReadyError` | Connect budget ran out on a server that timed out, refused or answered 5xx |
| `ServerChangedError` | Server refused a stale connect token (one fresh `/init` first; not an `AuthenticationError`) |
| `FunctionCallError` | Remote function call failure with error type and stack trace |

### Utility Functions

| Function | Description |
|----------|-------------|
| `isConnecting(state)` | Returns true if waitingForExternalConnectUrl, connecting, or reconnecting |
| `isConnected(state)` | Returns true if connected |
| `isOffline(state)` | Returns true if offline |
| `readOpcode(message)` | Extract opcode from protocol message |
| `readProtocolMessageHeaders(message)` | Extract headers from protocol message |
| `isWebTransportSupported()` | Check WebTransport browser support |
| `getOpcodeName(opcode)` | Get human-readable opcode name |
| `subscribeToLogEvents(listener, options?)` | Subscribe to SDK log events for forwarding |
| `initializeDebugMode()` | Initialize debug mode from URL parameter |
| `isDebugModeEnabled()` | Check if debug mode is active |

## License

This SDK is licensed under the Apache License 2.0. See `LICENSE` in each package for details.

## Support

For issues and feature requests, contact IKON support or open an issue on GitHub.
