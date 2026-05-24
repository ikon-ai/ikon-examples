# Frontend & Custom Components

## Frontend Overview

The frontend is a React app that acts as a thin client. It handles authentication gating, connection to the Ikon server, and renders UI streamed from C#. The main entry point is `app.tsx`.

The component hierarchy depends on whether auth is enabled:
- **Auth enabled:** `I18nProvider` → `AuthProvider` → `AuthGuard` → `ConnectedApp`
- **Auth disabled:** `I18nProvider` → `ConnectedApp`

`ConnectedApp` uses the `useIkonApp` hook and `ConnectionStateRenderer` to manage the connection lifecycle, and `IkonUiSurface` to render the server-driven UI.

### useIkonApp

The `useIkonApp` hook from `@ikonai/sdk-react-ui` establishes the connection to the C# backend and manages UI state.

**Options:**

```typescript
const app = useIkonApp({
  modules: [registerStandardUiModule, registerLucideIconsModule],
  audio: true,            // Enable audio playback (default: true)
  video: true,            // Enable video playback (default: true)
  webRtc: true,           // Enable WebRTC for audio/video transport (default: true)
  backgroundAudio: false, // Allow audio playback when tab is in background (default: false)
  websocket: undefined,   // Force WebSocket transport (default: auto-detected)
  webtransport: undefined,// Force WebTransport transport (default: auto-detected)
  proxy: undefined,       // Force proxy mode (default: auto-detected)
  authConfig: undefined,  // Override auth config (default: from window.__IKON_AUTH_CONFIG__)
  timeouts: undefined,    // Connection timeout configuration (see Connection Lifecycle)
});
```

- `modules` — UI component modules to register. The standard module and Lucide icons module are included by default in scaffolded apps. Custom modules can be added here (see Custom UI Components)
- `audio` / `video` — Enable or disable audio and video playback from the C# app. Enabled by default
- `webRtc` — Use WebRTC transport for audio/video instead of the default SDK-managed pipeline
- `backgroundAudio` — Allow audio to continue playing when the browser tab is not focused, on both desktop and mobile
- `websocket` / `webtransport` / `proxy` — Force a specific transport. By default the SDK auto-selects: WebTransport → WebSocket → proxy variants
- `authConfig` — Override the auto-detected auth configuration. By default read from `window.__IKON_AUTH_CONFIG__` which is injected at build time from `ikon-config.toml`
- `timeouts` — Override connection timeout defaults (see Connection Lifecycle)

**Return values:**

| Property | Type | Description |
|---|---|---|
| `connectionState` | `ConnectionState` | Current connection state |
| `error` | `string \| null` | Current error message |
| `accessDeniedReason` | `string \| null` | Reason if access was denied |
| `stores` | `ReadonlyMap<string, IkonUiStoreEntry> \| undefined` | UI stores from server |
| `client` | `IkonClient \| null` | Active client instance |
| `registry` | `IkonUiRegistry \| null` | UI component registry |
| `onAction` | `(actionId: string, payload: unknown) => void` | Dispatch actions back to C# |
| `isReady` | `boolean` | Whether the connection is ready to render |
| `webRtcVideoStreams` | `Map<number, MediaStream>` | Active WebRTC video streams |

**ConnectionStateRenderer pattern:**

```typescript
<ConnectionStateRenderer
  {...app}
  renderIdle={() => null}
  renderConnecting={() => null}
  renderConnectingSlow={() => <ConnectingOverlay />}
  renderConnected={({ stores, registry, client, onAction, isReconnecting }) => (
    <>
      {isReconnecting && <ReconnectingOverlay />}
      <IkonUiSurface stores={stores} registry={registry} onAction={onAction} />
    </>
  )}
  renderOffline={() => <OfflineScreen />}
  renderError={(error) => <ErrorScreen error={error} />}
/>
```

### Connection Lifecycle

Connection states: `idle` → `connecting` → `connectingSlow` → `connected`, with `reconnecting` on temporary disconnection and `offline` / `offlineError` as terminal states.

**Timeline:**
1. **0–5s:** `connecting` state (show blank or nothing)
2. **5s threshold:** transitions to `connectingSlow` (show a loading indicator)
3. **180s timeout:** if still not connected, transitions to `offline` or `offlineError`
4. **On disconnect:** enters `reconnecting` state — attempt 1 is immediate, attempt 2 after 2s delay. After 2 failed attempts, falls back to full re-authentication
5. **Stability:** after 15s of stable connection, the reconnect counter resets

**Transport selection:** The SDK auto-selects the best transport: WebTransport → WebSocket → proxy variants. Override with the `websocket`, `webtransport`, or `proxy` options on `useIkonApp`, or with query parameters.

**Keepalive:** The server sends periodic keepalive messages. The SDK monitors these with a 15s timeout — if no keepalive is received, the connection is considered lost.

**Timeout configuration** via the `timeouts` option:

| Option | Default | Description |
|---|---|---|
| `slowConnectionThresholdMs` | 5000 | Time before `connectingSlow` state |
| `connectionTimeoutMs` | 180000 | Max time to establish connection |
| `keepaliveTimeoutMs` | 15000 | Max gap between keepalive messages |
| `reconnectBackoffMs` | 2000 | Delay between reconnect attempts |
| `maxReconnectAttempts` | 2 | Fast reconnect attempts before re-auth |

### Query Parameters

The SDK reads `ikon-*` URL query parameters at startup. Both hyphenated (`ikon-proxy`) and underscore (`ikon_proxy`) forms are accepted. Query parameters override `useIkonApp` options.

| Parameter | Type | Description |
|---|---|---|
| `ikon-proxy` | boolean | Force proxy mode on/off |
| `ikon-websocket` | boolean | Force WebSocket transport on/off |
| `ikon-webtransport` | boolean | Force WebTransport transport on/off |
| `ikon-audio` | boolean | Force audio on/off |
| `ikon-video` | boolean | Force video on/off |
| `ikon-webrtc` | boolean | Force WebRTC on/off |
| `ikon-debug` | boolean | Enable SDK debug logging |
| `ikon-lang` | string | Override UI language (e.g. `en`, `fi`) |
| `ikon-server-url` | string | External connect URL for shareable previews |
| `ikon-git-source` | boolean | Enable Git source deployment mode |
| `ikon-git-branch` | string | Git branch for deployment |
| `guest` | — | Auto-login as guest (not `ikon-` prefixed) |

### Authentication

The frontend acts as an optional auth gate — when auth is enabled, users must authenticate before the SDK connects to the server. Auth is configured in the `[Auth]` section of `ikon-config.toml` and injected at build time into `window.__IKON_AUTH_CONFIG__`.

**Component tree:** `AuthProvider` → `AuthGuard` → app content. `AuthGuard` checks the auth state and renders either the auth screen or the protected content.

**Supported login methods:** google, facebook, apple, microsoft, linkedin, github, email (magic link), passkey (WebAuthn), guest.

**Guest auto-login:** Add `?guest` to the URL. If `guest` is included in the configured methods, the user is logged in automatically without seeing the auth screen.

**Session:** Stored in localStorage. Expires after JWT expiry.

**Custom auth:** Two approaches:
1. **Use the `useAuth()` hook** — provides `login(method)`, `logout()`, `getToken()`, `registerPasskey()`. Build your own auth UI while using the SDK's auth backend
2. **Replace `AuthGuard`** — implement fully custom auth logic and pass `authConfig` to `useIkonApp` to control when the SDK connects

The template's `auth-guard.tsx` shows the default implementation: it uses `useAuthGuard({ config, guestUrlParam: 'guest' })` which returns `shouldRenderChildren`, `isCheckingAuth`, `isAuthenticated`, and `error` for conditional rendering.

#### Custom magic-link login email

Magic-link login emails default to a platform-branded template. To ship your own, drop files into an optional `emails/` folder at the **project root** (sibling to `app/` and `frontend-node/`). The bundler picks them up automatically on `ikon app deploy`.

Each template is a pair of sibling files:

```
emails/
  magic-link-code.html     # Handlebars HTML
  magic-link-code.json     # {"subject": "..."}
```

- File name without extension is the template key. Supported keys today: `magic-link-code`.
- The `.json` provides the subject and is **required** — missing/empty subject produces a warning on activation and the template is not saved.
- The subject string supports the same Handlebars tokens as the HTML.
- Only `.html` ships in the app bundle; the `.json` is read by IkonTool at bundle time to populate subject metadata.

**Allowed Handlebars tokens** for `magic-link-code`:

| Token | Value |
| --- | --- |
| `{{code}}` | Full 9-digit login code |
| `{{code1}}` | First 3 digits |
| `{{code2}}` | Middle 3 digits |
| `{{code3}}` | Last 3 digits |

**Minimal example:**

```html
<!-- emails/magic-link-code.html -->
<!DOCTYPE html><html><body>
  <p>Enter this code to finish signing in:</p>
  <p><strong>{{code1}} {{code2}} {{code3}}</strong></p>
</body></html>
```

```json
// emails/magic-link-code.json
{ "subject": "Your sign-in code" }
```

Omit the folder entirely to keep the platform default.

### Internationalization (i18n)

`I18nProvider` wraps the app with a translations map. Language detection priority (when `detectLanguage` is enabled):
1. `?ikon-lang=` query parameter
2. Browser language (`navigator.language`)
3. `defaultLanguage` prop (defaults to `'en'`)

```typescript
<I18nProvider translations={{ en: enTranslations, fi: fiTranslations }} detectLanguage>
  {children}
</I18nProvider>
```

Use the `useI18n()` hook in components:
- `t(key, params?)` — translate a key with optional parameter interpolation using `{paramName}` syntax
- `locale` — current locale string

The template ships with English translations in `i18n/en.ts`. Add languages by creating translation objects and adding them to the `I18nProvider` `translations` prop.

### Styling

The frontend is fully user-customizable via CSS. `app.css` contains all connection, auth, and overlay styles using `.ikon-*` class names. Override any `.ikon-*` class in your `app.css` to customize the look.

Server-driven styles: the C# app streams CSS via Crosswind to clients, and the SDK injects `<style>` elements dynamically. These styles use Crosswind's utility class system (see Crosswind documentation).

The default template uses a dark theme with aurora gradient effects — all of this is defined in `app.css` and is fully replaceable. Auth screens, connection overlays, and toast notifications all use `.ikon-*` CSS classes that you can override.

### Mobile viewport and overscroll

For full-screen apps on iOS and Android, add these rules to `app.css` to prevent rubber-band bounce and white background flash:

```css
html, body {
  overscroll-behavior: none;
  overflow: hidden;
  height: 100%;
  background-color: #09090b; /* match your app background */
}
```

Use `h-screen` (which maps to `100dvh` via Crosswind) instead of `100vh` for full-height layouts — this accounts for mobile browser address bars.

For Android keyboard handling, add to `index.html` viewport meta:
```html
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover, interactive-widget=resizes-content" />
```
`interactive-widget=resizes-content` makes the viewport shrink when the on-screen keyboard opens, keeping input fields visible.

For iOS safe area (home indicator), use `env(safe-area-inset-bottom)` on bottom-pinned elements:
```css
body { padding-bottom: env(safe-area-inset-bottom, 0px); }
```
## Custom UI Components

When the built-in Ikon.Parallax components are not sufficient, you can integrate any React library as a custom UI component. This requires changes in both the frontend (React) and the C# app (server-side registration).

### Overview

The integration has four parts:

1. **React component** - Implements the UI using any React/JS library
2. **Component resolver** - Tells the Ikon UI system how to render your component type
3. **Module registration** - Registers the resolver with the `useIkonApp` hook
4. **C# extension method** - Provides a typed API for using the component from C# code

### Step 1: React Component and Resolver

Create your React component and a resolver function that maps a node type string to it.

```typescript
// frontend-node/src/lib/my-component/components/my-component.tsx
import { memo } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';

const MyComponentRenderer = memo(function MyComponentRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  if (!node) return null;

  const someProp = node.props?.['someProp'] as string | undefined;
  const onClickId = node.props?.['onClickId'] as string | undefined;

  return (
    <div
      className={className}
      onClick={onClickId ? () => context.dispatchAction(onClickId, { clicked: true }) : undefined}
    >
      {someProp}
      {context.renderChildren(node.children ?? [])}
    </div>
  );
});

export function createMyComponentResolver(): IkonUiComponentResolver {
  return (node) => {
    if (node.type !== 'my-component') return undefined;
    return MyComponentRenderer;
  };
}
```

### Step 2: Module Registration

Create a module that registers your resolver with the Ikon UI registry.

```typescript
// frontend-node/src/lib/my-component/my-component-module.ts
import { type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createMyComponentResolver } from './components/my-component';

export const loadMyComponentModule: IkonUiModuleLoader = () => [createMyComponentResolver()];

export function registerMyComponentModule(registry: IkonUiRegistry): void {
  registry.registerModule('my-component', loadMyComponentModule);
}
```

Then add the module to `useIkonApp` in `app.tsx`:

```typescript
const app = useIkonApp({
  modules: [registerStandardUiModule, registerLucideIconsModule, registerMyComponentModule],
});
```

### Step 3: C# Extension Method

Create an extension method on `UIView` that calls `view.AddNode` with your component type and a props dictionary. Use `view.CreateAction<T>` to create action IDs for callbacks.

```csharp
public static class MyComponentExtensions
{
    public static void MyComponent(
        this UIView view,
        string someProp,
        Func<MyClickEventArgs, Task>? onClick = null,
        string[]? style = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onClickId = null;

        if (onClick != null)
        {
            onClickId = view.CreateAction<MyClickEventArgs>(args => onClick(args.Value));
        }

        view.AddNode(
            "my-component",
            new Dictionary<string, object?>
            {
                ["someProp"] = someProp,
                ["onClickId"] = onClickId
            },
            style: style,
            file: file,
            line: line);
    }
}
```

### Step 4: Use from C#

```csharp
view.MyComponent("Hello from custom component",
    onClick: async args => { Log.Instance.Info("Clicked!"); },
    style: ["w-full rounded-lg"]);
```

### Key Concepts

- **Node type string** - The string (e.g. `"my-component"`) must match between the C# `view.AddNode` call and the TypeScript resolver check
- **Props dictionary** - C# sends a `Dictionary<string, object?>` that maps directly to React component props
- **Action IDs** - C# creates action IDs via `view.CreateAction<T>()`, passes them as props, and the React component calls `context.dispatchAction(actionId, payload)` to send events back
- **Crosswind styles** - Style arrays passed from C# via `view.AddNode` work with custom components just like built-in components

### Stateful components — pass `key:` for stable identity

Without an explicit `key:`, the C# call site determines the node's identity. Across re-renders that swap the underlying *thing* the component represents (a different file, bot, document, etc.), the same React component instance is reused and the new props are pushed into it. For dumb display nodes that's fine — the new props re-render the view.

For **stateful components that wrap a third-party imperative library** — Monaco, video players, canvas-based games, audio synths, anything that owns its own internal buffer — controlled-prop sync is unreliable after the user has interacted. The component sees a new `value` prop but doesn't always swap its internal model.

The fix is to pass a `key:` derived from the underlying identity, so React unmounts the old instance and mounts a fresh one on identity change:

```csharp
view.AddNode(
    type: "custom.lua-editor",
    key: $"editor:{activeBot.Id}",  // remount when activeBot changes
    props: new Dictionary<string, object?>
    {
        ["value"] = activeBot.DraftCode,
        ["onValueChangeId"] = view.CreateAction<string>(args =>
        {
            UpdateActiveDraftCode(args.Value ?? "");
            return Task.CompletedTask;
        }),
    });
```

Rule of thumb: if your custom component holds state the user can mutate (a code editor's text buffer, a video's playhead, a canvas-game's frame state), and the C# side can swap which underlying entity it represents, **always pass a `key:` keyed on that entity's ID.** Without this, switching entities will appear to do nothing — the old buffer stays put.

### A custom node type with no registered renderer is silently blank

`view.AddNode(type: "x.canvas", …)` compiles fine and the Critic scores the C# 9/10 — but if there is **no resolver registered for that type** in `app.tsx`, the browser renders **nothing** for it and logs `[UIRenderer] No renderer for node type x.canvas`. The build gate and Critic only see C#, so this slips through as a "clean" app whose core surface is blank at runtime. **Whenever you emit a custom node type, ship all four parts together** (React component, resolver, `registerModule` in `app.tsx`, and the C# `view.AddNode` extension) — Steps 1–4 above. If you only need a drawing/diagram surface and don't want a full custom component, render it server-side as SVG/PNG and show it with `view.Image(data:, mimeType:)` instead (no frontend code needed).

### Pattern: high-volume / high-frequency custom components (Canvas batching + delta streaming)

When a custom component must render **many** items (hundreds–thousands of markers, strokes, points) or update at **high frequency** (a game loop, live cursors, a shared drawing surface), two naive approaches both collapse:

- **One DOM node per item** — N React/Leaflet/DOM elements is the dominant render cost; it stalls past ~1–3k items.
- **Re-sending the whole collection every change** — `["itemsJson"] = JsonSerializer.Serialize(_items.Value)` on every update is O(n) per event and floods the socket. (This is the trap a freehand whiteboard falls into: re-broadcasting every stroke on every pointer move.)

The production pattern — reference implementation **`Ikon.App.Veldra.OS`** (`frontend-node/src/lib/tactical-map/` + `app/Ikon.App.Veldra.OS/VeldraOS.Map*.cs`), which renders thousands of live tactical markers — has three parts:

1. **Batch everything into ONE canvas/SVG** inside the custom React component — never one DOM node per item. (Veldra's `BatchedMarkerLayer` draws every glyph into a single canvas.) Own a `<canvas>`/`<svg>`/Leaflet element and draw per-item imperatively in a `useEffect`, not as JSX children. Keep a stateful `useRef<Map<id, item>>` so updates mutate individual items.

2. **Stream DELTAS, not the full set.** The server pushes a compact delta as a single prop — a JSON string `{ seq, full, adds, updates, removes }`:
   - `seq` — monotonically increasing; the component ignores any delta with `seq <= lastApplied` (drops out-of-order frames).
   - `full: true` — clear + rebuild (first load, or after a viewport change).
   - `adds` / `updates` (id + changed fields only) / `removes` (ids) — applied to the stateful map, so render cost scales with *changed* items, not total.
   ```csharp
   private readonly Reactive<string> _markerDelta = new("");   // canonical state lives elsewhere; this carries the diff
   // when items change: compute adds/updates/removes vs the last-sent set, bump seq, set _markerDelta.Value = json
   view.AddNode("tactical-map", new Dictionary<string, object?>
   {
       ["markerDelta"]     = _markerDelta.Value,
       ["onMarkerClickId"] = view.CreateAction<MarkerClickData>(d => HandleClick(d.Value)),
   }, style: style, file: file, line: line);
   ```
   The reactive diff system ships only the changed `markerDelta` string; the component applies it incrementally.

3. **Scope per client to the viewport (for truly huge datasets).** The component posts its visible bounds back (debounced) via an action id (`onViewportChangedId`); the server replies with a fresh `full` delta scoped to those bounds, so each client only receives what it can see. (Veldra: "markers flow through the per-client viewport-scoped delta stream".)

For **multi-user collaborative surfaces** (a shared whiteboard / sketch board), use the same shape: keep the canonical strokes in a shared `Reactive<List<Stroke>>`, but broadcast only the *new* stroke as an `add` delta — never the whole list. For continuous high-frequency binary streams (live pointer paths, cursors), a custom Teleport `.tp` message (see below) is the further optimization over a JSON-string prop.

## Real-time multi-user with a custom Teleport `.tp` message

This is the ONE case where hand-building a custom React canvas + a custom protocol message is the right tool (not native components, not a server-side SVG snapshot): when many clients must see each other's **high-frequency** actions **live** — a shared whiteboard where everyone watches strokes appear, a multiplayer game with moving players, live cursors. Native `view.*` and reactive props have no client→client live channel; an app's own Teleport `.tp` message does.

Reference apps that implement this end-to-end: **`Ikon.App.CoWhiteboard`** (collaborative whiteboard) and **`Ikon.App.Arena`** (multiplayer game with local rendering). The shape has four pieces.

### 1. Declare the message — `Schema/<Name>.tp`

Drop a `.tp` file under a `Schema/` folder at the app root (sibling to `app/` and `frontend-node/`); the build compiles it to C# + TS bindings automatically. Coordinates are normalized `0..1` so every client renders the same world regardless of canvas size.

```toml
type = "Stroke"
version = 1
# App-local opcode: bit 30 (GROUP_APP_LOCAL) | per-app slot 0x01 | message index 0x01.
# One explicit opcode per message type: 0x40000101, 0x40000102, ...
opcode = 0x40000101
# unreliable = true routes drop-tolerant high-frequency streams (positions, strokes,
# cursors) over the low-latency UDP / WebRTC data channel, with reliable fallback.
# Omit it for messages that must not be dropped.
unreliable = true

[namespaces]
csharp = "Ikon.App.CoWhiteboard.Protocol"   # = <YourAppRootProjectName>.Protocol
typescript = ""

[fields]
StrokeId = "string"
Color = "string"
Width = "float32"
Points = "float32[]"   # generated C# type is List<float>; TS type is number[]
Done = "bool"
```

Add `global using <YourAppRootProjectName>.Protocol;` to the app's `GlobalUsings.cs` so the generated type is in scope. (`.tp` field names stay PascalCase in BOTH C# and TS: `stroke.StrokeId`, `stroke.Points`.) On a brand-new `Schema/`, the first build emits the generated file mid-build so it isn't compiled that pass — just build again and it's picked up.

### 2. Server — receive, route, fan out (`app.OnMessage<T>` / `app.SendMessageAsync<T>`)

The C# app is the **router**: it receives each client's message, decides who gets it, and fans out explicitly. The server never auto-broadcasts app-local messages.

```csharp
private readonly Dictionary<string, Stroke> _completedStrokes = new(); // for late-join replay
private readonly object _strokesLock = new();

public async Task Main()
{
    app.OnMessage<Stroke>(async (stroke, senderId) =>
    {
        // fan out to every OTHER client — exclude the sender AND this app's own
        // machine-client connection (app.Clients.Ids includes it; sending to
        // yourself causes an echo loop and double-counts presence).
        var others = app.Clients.Ids.Where(id => id != senderId && id != app.ClientContext.SessionId).ToList();
        if (others.Count > 0)
        {
            await app.SendMessageAsync(stroke, others);   // (or a single int target id)
        }

        if (stroke.Done)
        {
            lock (_strokesLock) { _completedStrokes[stroke.StrokeId] = stroke; }
        }
    });

    UI.Root([Page.Default], content: view =>
    {
        // toolbars / scoreboards / panels stay NATIVE reactive components;
        // only the live shared surface is the custom node.
        view.Whiteboard(color: _color.Value, width: _width.Value, style: ["flex-1 min-h-0 w-full"]);
    });
}
```

**Late-join replay is client-pull, not on-join push.** `app.OnClientJoined` fires when the connection opens — *before* the browser component's subscription is ready — so a push there is lost. Instead the component requests history once it's listening (a sentinel message), and the server replays to that sender:

```csharp
// inside OnMessage<Stroke>, before the fan-out:
if (stroke.StrokeId == "__sync__")
{
    List<Stroke> snapshot;
    lock (_strokesLock) { snapshot = _completedStrokes.Values.ToList(); }
    foreach (var s in snapshot) { await app.SendMessageAsync(s, senderId); }
    return;
}
```

`app.SendMessageAsync<T>(payload, targetIds)` and `app.OnMessage<T>((payload, senderId) => …)` are extension methods on the app — `T` is your generated `.tp` type. Low-frequency shared state (a scoreboard, the set of orbs) stays in plain `Reactive<…>` and streams over the normal reactive channel; only the high-frequency surface uses `.tp`.

### 3. Browser — send/receive via the SDK `appMessaging` helper

Tie the generated opcode + codecs into an `AppMessageType` descriptor, then `send` / `on` through `appMessaging(client)`:

```typescript
import { appMessaging, type AppMessageType } from '@ikonai/sdk';
import { STROKE_OPCODE, toProtocolMessageStroke, fromProtocolMessageStroke, type Stroke } from '../../generated/protocol/stroke';

const StrokeMessage: AppMessageType<Stroke> = {
  opcode: STROKE_OPCODE,
  toProtocolMessage: toProtocolMessageStroke,
  fromProtocolMessage: fromProtocolMessageStroke,
};

// inside the custom component's renderer, where `context.client` is the IkonClient:
const messaging = appMessaging(context.client);
const sub = messaging.on(StrokeMessage, (stroke) => { /* upsert by stroke.StrokeId, redraw */ });
messaging.send(StrokeMessage, { StrokeId: id, Color, Width, Points, Done: false });
// request existing state now that we're subscribed:
messaging.send(StrokeMessage, { StrokeId: '__sync__', Color: '', Width: 0, Points: [], Done: true });
// later: sub.close();
```

### 4. The canvas component itself

Build it exactly like any custom component (the four parts in "Custom UI Components" above) — a `<canvas>` that draws per-item imperatively, sending input over `.tp` and applying received messages. For a game, run a `requestAnimationFrame` loop with **client-side prediction** (move your own entity locally on input for instant response) and **interpolation** (ease remote entities toward their last received position). `context.client` gives you the `IkonClient` for `appMessaging`; `client.sessionId` is your own id.

Note: the `appMessaging` helper ships in `@ikonai/sdk`; the two reference apps include a local copy at `frontend-node/src/lib/app-messaging.ts` (built on `client.sendProtocolMessage` / `client.subscribeToProtocolMessages`) for SDK versions that predate it — copy that file if `@ikonai/sdk` doesn't export `appMessaging` yet.
