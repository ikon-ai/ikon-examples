# Flutter SDK Guide

## Overview

The Ikon Flutter SDK enables Flutter apps to render Parallax UI from a C# server — the same server that drives web frontends. One codebase, two renderers. The app developer writes zero platform-specific code.

## Why native Flutter rather than a WebView wrapper

Wrapping the web frontend in a WebView would have been the cheaper-looking mobile story, and it
was rejected deliberately:

- **The capabilities people expect from an app are compromised inside a WebView.** Background
  audio, microphone capture, and camera access — all core Ikon features (server-streamed audio,
  live capture) — are restricted, permission-fragmented, or outright broken across WebView
  implementations, and behave differently per Android vendor and OS version. The platform's
  later WebRTC work narrowed parts of this gap, but it postdates the decision and leaves the
  background-audio and permission stories untouched.
- **Whole use cases need the device itself.** Local camera pipelines, motion and environment
  sensors, and whatever the next hardware capability is — apps that treat the phone as a sensor
  package are first-class targets, and a WebView offers at best a lagging, permission-gated
  subset of that surface. Going native keeps every device capability reachable the day a use
  case needs it.
- **App-store submission risk.** A repackaged website runs into minimum-functionality review
  (Apple guideline 4.2); a Flutter build is a real native app with native navigation, scroll
  physics, text input, and accessibility.
- **WebView fragmentation cannot be fixed from the server.** The one WebView note elsewhere in
  these docs is a known failure mode (ancient WebViews silently dropping modern CSS — see the
  Crosswind/Tailwind divergences doc). A server-driven UI cannot control which WebView engine a
  device ships.
- **The architecture makes native cheap — and that is the point.** Parallax sends a component
  tree, not HTML, so the second renderer maps the same tree to native widgets. The usual reason
  to wrap a WebView — native being expensive — does not apply here. Platforms whose output is a
  web app have no native path at all; real native performance and real native support for
  background tasks, audio, microphone, sensors, and payments is a capability this architecture
  uniquely affords, and a deliberate advantage.

### The middle option: a Capacitor-style hybrid shell

A third rung sits between the two — a Capacitor/Cordova-style shell, WebView UI plus a native
plugin bridge. It fixes *reachability* of device APIs, and it is not the primary mobile path
for three reasons:

- **The bridge only moves the boundary.** What a maintained plugin covers is easy — one-shot
  camera capture, geolocation, share sheets, basic sensors. Anything deeper means writing the
  per-OS native code yourself anyway, now split across a JS bridge: background audio behaviour,
  streaming sensor data, live camera pipelines, Apple Wallet / PassKit beyond adding a simple
  pass, HealthKit, and whatever the next OS capability is.
- **The UI still lives in the WebView**, so every fragmentation quirk and permission oddity of
  the plain wrap remains. First-hand experience with WebView shells was that a large share of
  the time went to testing, polishing, and working around strange engine bugs rather than
  building the product.
- **It buys back a cost this platform doesn't pay.** The hybrid pitch is avoiding a native UI
  rewrite — but Parallax has no UI rewrite to avoid, since the server drives the UI either way.
  The middle rung's main saving was worth nothing here while its WebView costs remained whole.

A wrapped-WebView frontend option may still be offered alongside `frontend-flutter` for less
demanding apps that just need a quick store presence — the hybrid rung is a convenience tier,
not the native story.

## Architecture

```
C# App (unchanged)
  view.Button(style: ["px-4 py-2 bg-blue-500 rounded"], label: "Go")
       │
       ▼
  Server builds UI tree, diffs it, sends to connected clients
       │
  ┌────┴────┐
  │  Web    │  → CSS styles, React components
  │ Flutter │  → Flutter tokens, native widgets
  └─────────┘
```

The server sends the same component tree to all clients. Web clients receive CSS styles; Flutter clients receive typed tokens (EdgeInsets, Color, BorderRadius, etc.) resolved from the same Crosswind utility classes. The `StyleFormat` capability is negotiated at connect time.

## Styling: shared classes, per-target overrides

You style Flutter exactly like the web — Crosswind utility classes in C#, resolved
server-side to native Flutter tokens. Most classes resolve on both renderers, including
the theme's semantic colours (`bg-background`, `text-secondary`, …), which Flutter
resolves to concrete values. So the common case needs no Flutter-specific work.

Some web-only styling has no Flutter equivalent and is dropped on Flutter today: focus
rings, `hover:` / `focus:` / `data-[state]` variants, and arbitrary CSS. Light and dark
theme token variants and responsive breakpoints are both resolved on Flutter.

When you need styling that differs by renderer, scope it with the **target variants** —
`flutter:` applies only on Flutter, `web:` only on the web/CSS renderer, and unprefixed
applies to both. Use the variant-group form to scope many classes with one prefix:

<!-- ikon-code: flutter-target-variants -->
```csharp
view.Box(
    style: [
        "px-3 py-2 rounded-md",                                  // shared
        "web:(bg-background text-secondary border border-input)",// web only
        "flutter:(bg-slate-900 text-slate-100 border border-slate-700)" // Flutter only
    ],
    content: view => view.Text(text: "Adapts per target"));
```

See the **Crosswind Styling and Motion Guide** for the full variant and variant-group
syntax.

## Quick Start

### 1. Create a Flutter frontend

Add a `frontend-flutter/` directory alongside your existing `frontend-node/`:

```
Ikon.App.MyApp/
├── app/                    ← C# backend
├── frontend-node/          ← Web frontend (existing)
├── frontend-flutter/       ← Flutter frontend (new)
│   ├── pubspec.yaml
│   ├── lib/main.dart
│   └── ...
└── ikon-config.development.toml
```

### 2. pubspec.yaml

```yaml
name: my_app_flutter
description: Flutter frontend for My Ikon App

environment:
  sdk: ^3.3.0
  flutter: '>=3.10.0'

dependencies:
  flutter:
    sdk: flutter
  ikon_sdk: ^1.0.0
```

### 3. lib/main.dart

```dart
import 'package:flutter/material.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

const _ikonPort = int.fromEnvironment('IKON_PORT', defaultValue: 8443);

void main() => runApp(const MyApp());

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'My Ikon App',
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark(useMaterial3: true).copyWith(
        scaffoldBackgroundColor: const Color(0xFF0F172A),
      ),
      home: const IkonScreen(),
    );
  }
}

class IkonScreen extends StatefulWidget {
  const IkonScreen({super.key});
  @override
  State<IkonScreen> createState() => _IkonScreenState();
}

class _IkonScreenState extends State<IkonScreen> {
  IkonClient? _client;
  IkonUiCore? _uiCore;
  IkonConnectionState _state = IkonConnectionState.idle;
  String? _error;

  @override
  void initState() {
    super.initState();
    _connect();
  }

  Future<void> _connect() async {
    setState(() { _state = IkonConnectionState.connecting; _error = null; });
    try {
      final client = await IkonClient.connectLocal(host: 'localhost', port: _ikonPort);
      client.onStateChange.listen((s) { if (mounted) setState(() => _state = s); });
      _client = client;
      _uiCore = IkonUiCore(client);
      if (mounted) setState(() => _state = IkonConnectionState.connected);
    } catch (e) {
      if (mounted) setState(() { _state = IkonConnectionState.offlineError; _error = '$e'; });
    }
  }

  @override
  void dispose() { _uiCore?.dispose(); _client?.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    if (_error != null) {
      return Scaffold(body: Center(child: Column(mainAxisSize: MainAxisSize.min, children: [
        const Icon(Icons.error_outline, size: 48, color: Colors.red),
        const SizedBox(height: 16),
        Text(_error!, textAlign: TextAlign.center),
        const SizedBox(height: 16),
        ElevatedButton(onPressed: _connect, child: const Text('Retry')),
      ])));
    }
    if (_state != IkonConnectionState.connected || _uiCore == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    return Scaffold(body: SafeArea(child: IkonParallaxView(uiCore: _uiCore!, client: _client)));
  }
}
```

### 4. Run

```bash
# Terminal 1: Start the C# server with Flutter support
cd Ikon.App.MyApp
ikon app run --flutter            # Flutter app in Chrome
ikon app run --flutter-ios        # ... on the iOS simulator (macOS)
ikon app run --flutter-android    # ... on an Android emulator

# Or manually in two terminals:
# Terminal 1: C# server
ikon app run

# Terminal 2: Flutter app
cd frontend-flutter
flutter run -d chrome --dart-define=IKON_PORT=8446
```

The `--flutter-*` flags combine, so `--flutter --flutter-ios` opens both. `--flutter-ios` and
`--flutter-android` use the simulator or emulator that is already running, boot one when none is
(`flutter emulators --launch`), and fall back to a plugged-in phone of that platform. They also pass
the right server host for the device — the Android emulator reaches the host machine at `10.0.2.2`,
not `localhost`. A device-only run (`--flutter-ios` or `--flutter-android` without `--flutter`) also
skips opening the web frontend in a browser tab; the web frontend still serves.

## Running on Different Platforms

### Web (Chrome)

```bash
flutter run -d chrome --dart-define=IKON_PORT=8446
```

### macOS Desktop

Requires Xcode installed:

```bash
flutter run -d macos --dart-define=IKON_PORT=8446
```

### Android

Requires Android SDK. For the emulator, use `10.0.2.2` instead of `localhost`:

```bash
# Emulator — ikon app run --flutter-android does this for you
flutter run -d android --dart-define=IKON_PORT=8446 --dart-define=IKON_SERVER_HOST=10.0.2.2

# Physical device — run server on LAN
ikon app run --host-lan
# Then use the LAN IP in the Flutter app
```

### iOS

Requires Xcode + Apple Developer account:

```bash
# Simulator (localhost works) — ikon app run --flutter-ios does this for you
flutter run -d ios --dart-define=IKON_PORT=8446

# Physical device — run server on LAN
ikon app run --host-lan
```

The dev server's certificate is self-signed; `IkonClient.connectLocal` trusts it for the host it
connects to, so simulators, emulators and phones on the LAN connect without installing anything.
On an Apple Silicon Mac the simulator build links against a copy of the `opus_flutter_ios` plugin
that carries the arm64 simulator slice the published package lacks; inside the ikon-platform
repo the ikon tool writes that override into `pubspec_overrides.yaml` for you.

### Release Builds

```bash
# Android APK
flutter build apk --dart-define=IKON_PORT=8446

# iOS IPA
flutter build ipa --dart-define=IKON_PORT=8446

# Web
flutter build web --dart-define=IKON_PORT=8446
```

## Authentication

### Local Development

`IkonClient.connectLocal()` handles everything — no config needed:

```dart
final client = await IkonClient.connectLocal(host: 'localhost', port: 8446);
```

### Deployed Apps — Guest/Anonymous

```dart
final client = await IkonClient.connectGuest(
  serverHost: 'myapp.ikon.ai',
  spaceId: 'my-space-id',
  authUrl: 'https://auth.ikonai.com',
);
```

### Deployed Apps — OAuth (Google, Apple, Microsoft, etc.)

```dart
// 1. Start OAuth in system browser
final auth = IkonAuthenticator(host: 'myapp.ikon.ai', port: 443);
await auth.startOAuthLogin(
  method: LoginMethod.google,  // or .apple, .microsoft, .github, etc.
  spaceId: 'my-space-id',
  authUrl: 'https://auth.ikonai.com',
  returnUrl: 'myapp://auth/callback',  // your app's deep link
);

// 2. Handle the deep link callback in your app
final token = IkonAuthenticator.extractTokenFromCallbackUrl(callbackUri);

// 3. Connect with the token
final client = await IkonClient.connectWithToken(
  serverHost: 'myapp.ikon.ai',
  spaceId: 'my-space-id',
  token: token!,
);
```

### Deployed Apps — API Key

```dart
final auth = IkonAuthenticator(host: 'myapp.ikon.ai', port: 443);
final result = await auth.authenticateApiKey(
  spaceId: 'my-space-id',
  apiKey: 'my-api-key',
);
// Use result.websocketUrl and result.authTicket with IkonClientConfig
```

## Custom Components

Register custom Flutter widgets for node types the server sends:

```dart
final registry = IkonComponentRegistry();
registry.register('my-custom-widget', (node, style, children, context) {
  final data = node.props['data'] as String? ?? '';
  return MyCustomWidget(data: data);
});

// Use the custom registry
IkonParallaxView(uiCore: uiCore, client: client, registry: registry)
```

## Multi-mount apps

Apps that declare multiple Parallax sub-trees via `IAppBase.Mounts` can be selectively embedded by passing the `mount:` argument:

```dart
IkonParallaxView(uiCore: uiCore, client: client, mount: 'aiCanvas')
```

When `mount` is `null`, the most recently updated tree is rendered (single-mount apps). The lower-level streams API is available on `IkonUiCore`: `streams`, `streamFor(category)`, `streamForMount(id)`, `streamsByCategory(category)`, plus `onStreamUpdate` / `onStreamEnded` listeners.

## Client Functions

The server can call functions registered on the Flutter client:

```dart
client.functionRegistry.register(
  FunctionDefinition(name: 'my.customAction', resultTypeName: 'string'),
  (args) async {
    // Handle the call from the server
    return 'result';
  },
);
```

Built-in Flutter functions (called automatically by the server):
- `ikon.client.getTheme` / `setTheme` — theme (placeholder; managed by the host app's `ThemeData`)
- `ikon.client.getLanguage` — device locale
- `ikon.client.getTimezone` — device timezone
- `ikon.client.getViewport` — viewport dimensions
- `ikon.client.getVisibility` — app lifecycle state
- `ikon.client.vibrate` — haptic feedback
- `ikon.client.keepScreenAwake` — wakelock
- `ikon.client.scrollTo` — scroll (handled by the Parallax view)
- `ikon.client.getUrl` / `setUrl` — URL placeholders
- `ikon.client.getLocation` — geolocation (via `geolocator`)
- `ikon.client.getNotificationPermission` / `showNotification` — local notifications (via `flutter_local_notifications`, permission requested lazily on first send)
- `ikon.client.getPushSubscription` — FCM token for offline push (returns null unless the app wires `ikonFcmTokenProvider`; Firebase stays opt-in)
- `ikon.client.captureImage` — system camera (via `ImagePicker`)

## File Upload

```dart
final uploader = IkonFileUpload(client);
final result = await uploader.upload(
  uploadActionId: uploadActionId,  // routes lifecycle callbacks to the server-side upload node
  filename: 'photo.jpg',
  mimeType: 'image/jpeg',
  data: fileBytes,
  onProgress: (sent, total) => print('$sent / $total'),
);
```

## Media

The SDK bundles Opus + PCM + WebRTC implementations behind pluggable interfaces
(`AudioEncoder`, `AudioDecoder`, `AudioOutput`, `MicSource`); the `enableIkon*` helpers
wire the bundled ones:

```dart
// Opus mic capture (bundled OpusAudioEncoder + record-based mic source)
final capture = enableIkonAudioCapture(client);
await capture.start(sampleRate: 48000);
// Or construct manually and feed PCM frames yourself:
// final capture = IkonAudioCapture(client: client, encoder: OpusAudioEncoder());
// await capture.feedPcmFrame(pcmData);
await capture.stop();

// Opus playback (bundled OpusAudioDecoder + PcmStreamOutput)
final playback = enableIkonAudioPlayback(client);

// Live WebRTC A/V (peer connection to the server SFU via flutter_webrtc);
// the Opus stack above remains the fallback when WebRTC is unavailable
final webRtcMedia = enableIkonWebRtcMedia(client);
await webRtcMedia.start();
await webRtcMedia.startCameraCapture();
```

`IkonMediaManager` aggregates audio capture, playback, and WebRTC media (and routes
share/clipboard actions through `IkonShare`) and can be passed to
`IkonParallaxView(mediaManager: ...)`.

## Supported Crosswind Styles

The Flutter SDK resolves ~100 Crosswind utilities to native Flutter types:

**Layout:** padding, margin, width/height/min/max, flex (direction/wrap/gap/align/justify), overflow, aspect ratio, position (absolute/relative), z-index

**Visual:** background color, border (all sides), border radius, opacity, shadow (sm through 2xl), gradient (linear, 8 directions)

**Typography:** font size/weight/family, line height, letter spacing, text align, text color, italic, text decoration (underline/line-through), text overflow (ellipsis/clip), truncate, max lines, text transform (uppercase/lowercase/capitalize), white space

**Interactive:** cursor, visibility (hidden/invisible/visible)

**Transform:** rotate, scale (uniform + x/y), translate (x/y), skew (x/y)

## Supported Components

The Flutter SDK renders 105+ registered component types including:

- **Layout:** div (auto Row/Column from flex direction), scroll area, separator, aspect ratio
- **Text:** text, heading, label, markdown (full rendering)
- **Input:** text field (with echo suppression), text area, checkbox, switch, slider, radio group, toggle, select
- **Buttons:** button, action button, icon button, form submit
- **Display:** avatar (image + fallback), image (network), progress bar
- **Disclosure:** accordion, collapsible, tabs
- **Overlays:** dialog, alert dialog, tooltip, popover, toast
- **Media:** Rive animations, charts (bar/line/pie via fl_chart)
- **Structure:** form, file upload zone, drag & drop containers, keyboard listener

## CI/CD

Flutter is wired into CI via `platform-dart/builder.py`, which provides `build`
(`flutter pub get`), `analyze`, `test`, and `full_build` targets. The Dart protocol
files under `platform-dart/ikon_sdk/lib/protocol/generated/` are regenerated by the
platform-dotnet build (the `IkonServer` project), so run that first after schema
changes.
