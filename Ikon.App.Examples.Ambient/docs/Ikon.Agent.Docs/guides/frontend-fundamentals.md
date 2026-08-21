# Frontend & Custom Components

## Frontend Overview

The frontend is a React app that acts as a thin client. It handles authentication gating, connection to the Ikon server, and renders UI streamed from C#. The root component lives in `app.tsx` (mounted from the entry module `main.tsx`).

The component hierarchy depends on whether auth is enabled:
- **Auth enabled:** `I18nProvider` → `AuthProvider` → `AuthGuard` → `AuthorizedApp`
- **Auth disabled:** `I18nProvider` → `AuthorizedApp`

`AuthorizedApp` uses the `useIkonApp` hook to manage the connection lifecycle and renders the `IkonApp` component, which mounts the server-driven UI surface and layers the connection overlays on top.

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
  webtransport: undefined,// Opt into WebTransport (default: off — WebSocket is used unless this is true)
  proxy: undefined,       // Force proxy mode (default: auto-detected)
  authConfig: undefined,  // Override auth config (default: from window.__IKON_AUTH_CONFIG__)
  timeouts: undefined,    // Connection timeout configuration (see Connection Lifecycle)
});
```

- `modules` — UI component modules to register. The standard module and Lucide icons module are included by default in scaffolded apps. Custom modules can be added here (see Custom UI Components)
- `audio` / `video` — Enable or disable audio and video playback from the C# app. Enabled by default
- `webRtc` — WebRTC transport for audio/video is the default; set `false` to fall back to the SDK-managed playback pipeline
- `backgroundAudio` — Allow audio to continue playing when the browser tab is not focused, on both desktop and mobile
- `websocket` / `webtransport` / `proxy` — Force a specific transport. By default the SDK uses WebSocket (then proxy variants); WebTransport is off by default and must be explicitly enabled
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

**IkonApp pattern** (the scaffolded template's `AuthorizedApp`, minus its lazy-font prop threading):

```typescript
<IkonApp
  {...app}
  connectingOverlay={(isSlow) => (isSlow ? <ConnectingOverlay /> : null)}
  reconnectingOverlay={<ReconnectingOverlay />}
  offlineOverlay={(error) => <OfflineOverlay error={error} isServerFull={app.isServerFull} />}
  accessDeniedScreen={(reason) => <AccessDeniedScreen reason={reason} />}
/>
```

### Connection Lifecycle

Connection states: `connecting` → `connected`, with `reconnecting` on temporary disconnection and `offline` as the terminal state. (The slow-connection signal is surfaced as the `isConnectingSlow` boolean, not a separate state; `idle`, `connectingSlow`, and `offlineError` remain in the `ConnectionState` type for back-compat but are no longer emitted.)

**Timeline:**
1. **0–5s:** `connecting` state (show blank or nothing)
2. **5s threshold:** `isConnectingSlow` becomes true (show a loading indicator); the state stays `connecting`
3. **180s timeout:** if still not connected, transitions to `offline`
4. **On disconnect:** enters `reconnecting` state — attempt 1 is immediate, attempt 2 after 2s delay. After 2 failed attempts, falls back to full re-authentication
5. **Stability:** after 5s of stable connection, the reconnect counter resets

**Transport selection:** By default the SDK uses WebSocket (then proxy variants). WebTransport is off by default (found unreliable on poor networks) and must be opted into via `webtransport: true` on `useIkonApp` or the `?ikon-webtransport=true` query parameter. Override the transport with the `websocket`, `webtransport`, or `proxy` options on `useIkonApp`, or with query parameters.

**Keepalive:** The server sends periodic keepalive messages and tells the client its watchdog timeout during the auth handshake (180s for current clients; only legacy clients hard-code 15s). If no keepalive arrives within that window, the connection is considered lost.

**Timeout configuration** via the `timeouts` option:

| Option | Default | Description |
|---|---|---|
| `slowConnectionThresholdMs` | 5000 | Time before the slow-connection signal (`isConnectingSlow`) |
| `connectionTimeoutMs` | 180000 | Max time to establish connection |
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

**Supported login methods:** google, facebook, apple, microsoft, signicat (eID), email (magic link), passkey (WebAuthn), plus the two not-signed-in flavors guest and global.

**Not-signed-in entry:** `guest` gives each visitor their own device-scoped anonymous user; `global` puts every visitor on one space-wide shared anonymous user. Both render the same "Continue as Guest" button, and `global` wins when a space lists both — the app can then upgrade a visitor with `login('guest')`. On the server, `Context.IsAnonymous` is true for both and `Context.IsGlobal` tells them apart.

**Guest auto-login:** Add `?guest` to the URL. If either `guest` or `global` is included in the configured methods, the user is logged in automatically without seeing the auth screen.

**Session:** Stored in localStorage. Expires at the earlier of JWT expiry or a hard 7-day client-side window.

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

The frontend is fully user-customizable via CSS. `app.css` contains the connection and overlay styles, and `src/auth/auth.css` the auth-screen styles — all using `.ikon-*` class names. Override any `.ikon-*` class to customize the look.

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

### Media in custom components — sibling connections, capture, video tiles

Some custom components carry live media on a **second connection** — the canonical case is a
call/huddle room hosted by a keyed cell, where every participant's media must terminate at ONE
process (the cell's host) rather than each user's own app instance. `@ikonai/sdk-react-ui`
ships hooks and a view for the whole leg, so a component never manages `IkonClient` lifecycles
or capture handles by hand:

```tsx
import {
  VideoStreamView,
  useCameraCapture,
  useMicrophoneCapture,
  useScreenCapture,
  useSiblingClient,
} from '@ikonai/sdk-react-ui';

const ROOM_MEDIA = { audio: { enabled: true }, video: { enabled: true } };

function RoomMedia({ context, roomId, muted, cameraOn, screenOn, tiles }) {
  // Opens a sibling connection next to the app connection (same auth/transport, the cell's
  // identity parameters). Connects while roomId is non-null; unmount = leave the room.
  const { client } = useSiblingClient(
    context.client,
    roomId ? { 'ikon-cell-type': 'RoomCell', RoomId: roomId } : null,
    ROOM_MEDIA,
  );

  // Capture runs while the flag is true; flipping it off (or unmounting) stops it.
  useMicrophoneCapture(client, !muted, { userGesture: true });
  useCameraCapture(client, cameraOn, { userGesture: true });
  useScreenCapture(client, screenOn, { userGesture: true });

  return tiles.map((tile) => (
    <VideoStreamView key={tile.streamId} client={client} streamId={tile.streamId} />
  ));
}
```

**Always render incoming streams with `VideoStreamView`** (or the built-in
`view.VideoStreamCanvas`, which uses it). The SDK carries video over two transports — WebRTC
`MediaStream`s by default, encoded frames over the protocol channel as the fallback — and
which one is active is per connection, decided at runtime. A hand-rolled
`media.video.attachCanvas(...)` implements only the fallback path and renders a permanently
black tile whenever WebRTC carries the media; `VideoStreamView` handles both, plus the retry
needed because the fallback pipeline builds lazily behind a dynamic import. Incoming **audio**
needs no component at all — the client plays the connection's audio automatically.

Reference app: **Ikon.App.Buzz** (`frontend-node/src/lib/huddle/`) — a Slack-style huddle
whose media leg is exactly this pattern against a `HuddleCell` room.

### App icons & branding

`branding/logo.png` is the single source of truth for every app icon. It is a square 1024×1024 PNG scaffolded into each new app — replace it with your own square design (same size and format; SVG is not supported) and run **`ikon app icon generate`** to regenerate the complete icon set across **all** frontends from it:

- **frontend-node** — web favicons (`favicon.ico`, `favicon-16/32/192/512.png`, `apple-touch-icon.png`, `maskable-512.png`) and the PWA `manifest.webmanifest` icon entries.
- **frontend-flutter** (when present) — Android launcher icons (mipmap + adaptive), the iOS `AppIcon.appiconset`, and the Flutter web icons.

You don't hand-create or hand-place any of these PNGs — one square source PNG drives them all. `ikon app icon generate` rewrites the generated icon files, each manifest's `icons` array, the Flutter web manifest's `background_color`/`theme_color`, and the Android launcher background color, so manual manifest edits (a custom PWA `name`/`short_name`, extra fields) are preserved across runs. The browser tab title is separate from the icons — it lives in `frontend-node/index.html` (`<title>` and `apple-mobile-web-app-title`).
