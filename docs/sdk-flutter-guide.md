<!-- checked-against: 466761d688dcbd63e3b0c442 -->

# Flutter SDK Guide

## Overview

The IKON Flutter SDK lets Flutter apps render Parallax UI from a C# server, the same server that drives web frontends. The app has one codebase and two renderers, and the app developer writes no platform-specific code.

## Why native Flutter rather than a WebView wrapper

Wrapping the web frontend in a WebView looked like the cheaper way to reach mobile, and it was
rejected for these reasons:

- **The capabilities people expect from an app are limited inside a WebView.** Background audio,
  microphone capture and camera access are all core IKON features (server-streamed audio, live
  capture). Across WebView implementations they are restricted, split across separate
  permissions, or broken, and they behave differently per Android vendor and OS version. The
  platform's later WebRTC work closed part of this gap, but it came after the decision and does
  not help with background audio or permissions.
- **Some kinds of app need the device itself.** Apps that use the phone as a set of sensors (local
  camera pipelines, motion and environment sensors, and future hardware capabilities) are fully
  supported targets. A WebView offers at best a subset of those APIs, behind permissions and later
  than native. A native app can use any device capability as soon as a use case needs it.
- **App-store submission risk.** A repackaged website runs into minimum-functionality review
  (Apple guideline 4.2); a Flutter build is a real native app with native navigation, scroll
  physics, text input, and accessibility.
- **WebView fragmentation cannot be fixed from the server.** These docs already record one failure
  an older engine causes: 2022 smart televisions on Chromium 85 ignore theme-colour opacity
  modifiers (see the Crosswind/Tailwind divergences doc). A server-driven UI cannot control which
  WebView engine a device ships.
- **The architecture makes native cheap.** Parallax sends a component tree, not HTML, so the
  second renderer maps the same tree to native widgets. The usual reason to wrap a WebView, that
  native is expensive, does not apply here. Platforms whose output is a web app have no native
  path at all. Native performance and native support for background tasks, audio, microphone,
  sensors and payments are an advantage this architecture has over them, and a deliberate one.

### The middle option: a Capacitor-style hybrid shell

A third option sits between the two: a Capacitor/Cordova-style shell, which is a WebView UI plus a
native plugin bridge. It lets the app reach device APIs, but it is not the primary mobile path,
for three reasons:

- **The bridge does not remove the native code.** What a maintained plugin covers is easy: one-shot
  camera capture, geolocation, share sheets, basic sensors. Anything deeper still means writing
  the per-OS native code yourself, now split across a JS bridge: background audio behaviour,
  streaming sensor data, live camera pipelines, Apple Wallet / PassKit beyond adding a simple
  pass, HealthKit, and whatever the next OS capability is.
- **The UI still runs in the WebView**, so every engine difference and permission problem of a
  plain WebView wrap remains. In the team's experience with WebView shells, a large share of the
  time went to testing, polishing and working around engine bugs instead of building the product.
- **It saves a cost this platform does not have.** The main argument for a hybrid shell is that
  it avoids rewriting the UI natively. Parallax has no UI to rewrite, because the server drives
  the UI on both renderers. So the shell's main saving is worth nothing here, while all its
  WebView costs remain.

A wrapped-WebView frontend may still be offered alongside `frontend-flutter`, for simpler apps
that only need a quick presence in the app stores. It is a convenience option, not the native
frontend.

## Architecture

```text
C# App (unchanged)
  view.Button(style: ["px-4 py-2 bg-blue-500 rounded"], text: "Go")
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
rings, `hover:` / `focus:` / `data-[state]` variants, `motion-*` animations, and arbitrary CSS. Light and dark
theme token variants and responsive breakpoints are both resolved on Flutter, and so is `active:`:
while a finger is down on an element, its `active:` classes apply (`active:scale-95`,
`active:brightness-[0.97]`, `active:bg-*`, `active:opacity-*`, `active:shadow-*`,
`active:translate-*`), animated by its `transition-*`, `duration-*`, `ease-*` and `delay-*`
classes as on the web. A finger that moves off to scroll releases it. `disabled:` classes apply
while a control is disabled, so `disabled:opacity-100 disabled:bg-*` keeps a disabled button
opaque and gives it that fill; a disabled control whose classes have no `disabled:` look is drawn
at half opacity. Linear gradients draw too:
`bg-linear-to-*` / `bg-gradient-to-*` and `bg-linear-<angle>` with `from-`/`via-`/`to-` colours
(opacity and `transparent` included) and stop positions such as `from-10%`; an arbitrary
`bg-[linear-gradient(...)]`, radial and conic gradients are web only. A `Switch` or
`Checkbox` still takes its checked colours from the `data-[state=checked]:` classes in its
style, which the server sends to Flutter separately from the other styles. On a phone, a
`Checkbox` or `Switch` keeps the size its style gives it, and a tap on empty space within a
48-pixel square centred on it also reaches it, so a 16px web checkbox needs no Flutter-only size.
A tap on another element in that square stays with that element, and when two controls are in
reach the nearer one gets the tap.

Theme fonts are the exception to "no Flutter-specific work": a family the theme names
(`["font-heading"] = "Barlow Condensed"`) renders on Flutter only when the Flutter app bundles
that font under the same family name in its `pubspec.yaml`. Otherwise Flutter falls back to its
default font.

When you need styling that differs by renderer, scope it with the **target variants** —
`flutter:` applies only on Flutter, `web:` only on the web/CSS renderer, and unprefixed
applies to both. Use the variant-group form to scope many classes with one prefix:

<!-- ikon-example: flutter-target-variants -->
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

Flutter has no CSS `env()`, so a length containing one, such as
`pb-[calc(24px+env(safe-area-inset-bottom))]`, is dropped there. Give such a class the
`web:` prefix and let `IkonPageScaffold` (below) keep Flutter content out of the status bar
and the home indicator.

## Quick Start

### 1. Create a Flutter frontend

Add a `frontend-flutter/` directory alongside your existing `frontend-node/`:

```text
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
description: Flutter frontend for my IKON app

environment:
  sdk: ^3.3.0
  flutter: '>=3.44.0'

dependencies:
  flutter:
    sdk: flutter
  ikon_sdk: ^1.15.0

flutter:
  uses-material-design: true
```

### 3. lib/main.dart

<!-- ikon-example: flutter-sdk-quick-start -->
```dart
import 'package:flutter/material.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

const _ikonHost = String.fromEnvironment('IKON_SERVER_HOST', defaultValue: 'localhost');
const _ikonPort = int.fromEnvironment('IKON_PORT');

void main() => runApp(const MyApp());

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'My IKON app',
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
      final client = await IkonClient.connectLocal(host: _ikonHost, port: _ikonPort);
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
    // Paints the status-bar and home-indicator strips in the colours along the streamed page's top
    // and bottom edges; a plain Scaffold + SafeArea shows the Material theme's colour there instead.
    return IkonPageScaffold(child: IkonParallaxView(uiCore: _uiCore!, client: _client));
  }
}
```

### 4. Run

```bash
# Terminal 1: Start the C# server with Flutter support
cd Ikon.App.MyApp
ikon run --flutter            # Flutter app in Chrome
ikon run --flutter-ios        # ... on the iOS simulator (macOS)
ikon run --flutter-android    # ... on an Android emulator

# Or manually in two terminals:
# Terminal 1: C# server
ikon run

# Terminal 2: Flutter app
cd frontend-flutter
flutter run -d chrome --dart-define=IKON_PORT=8443
```

`ikon run` serves HTTPS on the port the app's last run used while it is free, else on 8443 or the
next free port, so a run can stay on 8444 after 8443 frees up; pass the port the run printed by hand,
while the `--flutter-*` flags pass it for you. `--flutter` opens the page in Chrome: `CHROME_EXECUTABLE`, else the
browser `ikon browse` uses. On Linux with no display it runs `flutter run -d web-server` instead, and the
page is at the Flutter address the run prints, for any browser. The `--flutter-*` flags combine, so `--flutter --flutter-ios` opens both. `--flutter-ios` and
`--flutter-android` use the simulator or emulator that is already running, then a plugged-in phone of
that platform, and boot one only when neither is there (`xcrun simctl boot` on iOS,
`flutter emulators --launch` on Android). They also pass
the right server host for the device — the Android emulator reaches the host machine at `10.0.2.2`,
not `localhost`, and a plugged-in phone at the machine's LAN address, so it needs to be on the same
network and joins as a guest (below). A device-only run (`--flutter-ios` or `--flutter-android` without `--flutter`) also
skips opening the web frontend in a browser tab; the web frontend still serves.

### From Studio's Devices tab

Studio's Devices tab runs the frontend on a phone or simulator of a connected computer without a
local run: the computer brings its checkout of the app up to date, runs `flutter run -d <device>`
in `frontend-flutter/`, and passes the dart-define `IKON_CONNECT_URL` through a defines file
(`--dart-define-from-file`, so the URL's query string survives every shell). The value is a
connect URL Studio minted for the preview it is running. `IkonClient.connectLocal` (ikon_sdk 1.15.0 or later) sees that
define and calls `IkonClient.connectWithUrl` instead of looking for a local server, so a
`main.dart` written for `ikon run` needs no change; the `parameters` and `userId` it passes do
not apply then, because the URL already carries the identity and parameters it was minted with.
The device joins with the same credential the tab's "Open in browser" link carries. A self-signed
certificate is trusted only when the URL names this machine or one on its network
(`isLocalNetworkHost`). IKON Connect on the computer needs the app's `devices` grant, and its
`agents` grant because the run checks the app out. The computer needs Flutter (`ikon install flutter`) and the app's git
needs the `frontend-flutter/` folder, so a frontend added by a task reaches the device after the
task's work is saved to the app. IKON Connect boots an iOS simulator itself, but refuses an Android
emulator that is not already running ("Boot the emulator first, then run the app on it").

## Running on Different Platforms

### Web (Chrome)

```bash
flutter run -d chrome --dart-define=IKON_PORT=8443
```

A Flutter web build plays no audio from the server, because the PCM speaker output
(`flutter_pcm_sound`) has no web implementation. `Audio.Speak` and every other audio stream is
silent, and the client logs `Audio playback failed to initialise`. For the web, ship the React frontend (`frontend-node`), which
plays audio, and keep the Flutter frontend for Android and iOS.

### macOS Desktop

Requires Xcode installed:

```bash
flutter run -d macos --dart-define=IKON_PORT=8443
```

### Linux Desktop

Requires a C++ toolchain, GTK 3, libsecret (the SDK keeps its install id and sign-in in the system
keyring) and the system's Opus library. On Ubuntu and Debian, `ikon install flutter` installs them
with apt when sudo needs no password. Otherwise it prints the `apt-get install` command for the
missing packages and finishes the rest of the install. On other distributions, install the matching
packages yourself:

```bash
sudo apt-get install -y clang cmake ninja-build pkg-config libgtk-3-dev libsecret-1-dev libstdc++-12-dev libopus0
flutter run -d linux --dart-define=IKON_PORT=8443
```

The program is a GTK window, so it needs a display: on a headless machine start one with `Xvfb :99` and run
the program with `DISPLAY=:99`, or it exits with `cannot open display`. The keyring needs a running Secret
Service (gnome-keyring, KeePassXC); without one the SDK logs `The secure store refused the install id` and
may fall back to an install id for that run only.

Audio on Linux needs a sound server (PulseAudio or PipeWire) or a sound card:

- WebRTC media (`enableIkonWebRtcMedia`) aborts the process in native code with `Failed to initialize
  the ADM` when the machine has no audio device, before any Dart code can catch it. On a machine with
  no sound, leave `enableIkonWebRtcMedia` out.
- The microphone runs `parecord`, so it needs `pulseaudio-utils` and a running sound server; without them
  the capture fails with `PlatformException(record, Cannot run parecord ...)`.
- The SDK's Opus playback (`enableIkonAudioPlayback`) plays on Android, iOS and macOS only: its speaker
  output, `flutter_pcm_sound`, has no Linux, Windows or web build. On Linux it logs `Audio playback
  failed to initialise`, and the app's audio plays only through WebRTC media.
- `libpulse` is optional: without it the build warns `libpulse NOT found` and only system-audio
  loopback capture is off.

### Android

Requires Android SDK. For the emulator, use `10.0.2.2` instead of `localhost`:

```bash
# Emulator — ikon run --flutter-android does this for you
flutter run -d android --dart-define=IKON_PORT=8443 --dart-define=IKON_SERVER_HOST=10.0.2.2

# Physical device — run server on LAN
ikon run --host-lan
# Then use the LAN IP in the Flutter app
```

### iOS

Requires Xcode + Apple Developer account:

```bash
# Simulator (localhost works) — ikon run --flutter-ios does this for you
flutter run -d ios --dart-define=IKON_PORT=8443

# Physical device — run server on LAN
ikon run --host-lan
```

The dev server's certificate is self-signed; `IkonClient.connectLocal` trusts it for the host it
connects to, so simulators, emulators and phones on the LAN connect without installing anything.
A phone on the LAN joins as a guest of its own install, never as the developer or a test user, so
the run refuses it when the surface admits no guest.
On an Apple Silicon Mac the simulator build links against a copy of the `opus_flutter_ios` plugin
that carries the arm64 simulator slice the published package lacks; inside the ikon-platform
repo the IKON tool writes that override into `pubspec_overrides.yaml` for you.

### A program without a window

`ikon_sdk` needs the Flutter engine: it imports `dart:ui` and Flutter, so `dart run` cannot compile a
program that uses it. A program that may open a window, such as a tool or a kiosk, is a Flutter
desktop app (`flutter run -d windows`, `macos` or `linux`). A program with no window, such as a bot,
a test or an uploader, runs as a test file under `flutter test`:

<!-- ikon-example: flutter-sdk-headless-program -->
```dart
// test/uploader_test.dart, run with: flutter test test/uploader_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

void main() {
  // A plain test(), never testWidgets(): the widget test binding answers every HTTP request with 400
  test('uploader', () async {
    final client = await IkonClient.connectLocal(
      port: 8443, // the port of the Server URL the app's ikon run printed
      spaceId: 'my-app-id', // refuses another app's run on that port
      userType: UserType.machine, // the app sees a program, not a person
    );

    try {
      // The program's work: functions, messages, the UI tree, uploads
      print(await client.functionRegistry.call<String>('my_app.status'));
    } finally {
      client.dispose();
    }
  }, timeout: Timeout.none);
}
```

`flutter_test` goes in the program's `dev_dependencies`. Pass arguments with `--dart-define` or
environment variables, which `Platform.environment` reads. The program needs no Flutter binding, and
`TestWidgetsFlutterBinding.ensureInitialized()` would break it, because that binding answers every
HTTP request with 400. Without a binding the platform plugins are not available, so the install id lasts
for one run only, the client reports the timezone as `DateTime.timeZoneName` gives it rather than as
an IANA id (call `client.viewportUpdater.sendTimezone('Europe/Helsinki')` to give one), and the
microphone, camera and speaker plugins do not work. Use `IkonAudioStreams` and `IkonVideoStreams` for
media instead (see "Encoded frames without a device").

### Release Builds

```bash
# Android APK
flutter build apk --dart-define=IKON_PORT=8443

# iOS IPA
flutter build ipa --dart-define=IKON_PORT=8443

# Web
flutter build web --dart-define=IKON_PORT=8443
```

These builds still connect to a local server. A build for a deployed space needs the
`IKON_SPACE_ID`, `IKON_AUTH_URL`, `IKON_SERVER_HOST` and `IKON_AUTH_*`
dart-defines (a non-empty `IKON_SPACE_ID` is what makes the app treat itself as deployed), which `ikon bundle --flutter-android|--flutter-ios|--flutter-web` injects.

## Authentication

### Local Development

`IkonClient.connectLocal` needs only the port of the Server URL that `ikon run` printed. It has no default port, because several runs share one machine:

<!-- ikon-example: flutter-sdk-auth-local -->
```dart
final client = await IkonClient.connectLocal(host: 'localhost', port: 8443);
```

A local run serves on the port of the Server URL that `ikon run` prints: 8443, or another port when
another app's run already holds 8443. Pass the app's id as `spaceId:` (`ikon status` shows it), and
a server that runs another app is refused with `WrongLocalAppException`, which names both apps. A
reconnect that finds another app goes offline with the exception in `IkonClient.lastError`. Without
`spaceId:` the client joins whatever app answers on the port.

`userType: UserType.machine` tells the app the client is a program rather than a person's screen.
The app reads it as the client's `UserType`, and such a client does not hold the session open. It is
`UserType.human` by default.

Every Flutter client, on the web, a phone or a desktop, joins with `ContextType.browser`, because it
connects over WebSocket and renders the app's UI as a browser does. A .NET, C++ or Rust program joins
as `ContextType.plugin` by default. So the app tells a program from a person's screen by `UserType`, not by
`ContextType`. `UserType`, `ContextType` and the other protocol enums are `int`-backed extension types:
compare them with `==`, and `.name` gives the member's name (`'browser'`) or the number for a value
that names no member.

`localClientProof:` matters only for a local connect that does not look local to the run, such as one
through `ikon run --host-public`. Without it, such a connect joins as a guest, or is refused where the
app admits no guests. With the run's `localClientProof` from its `ikon-server-info.json`, which only a
program on the run's machine can read, the run keeps the `userId:` the client gave. A simulator or an
emulator connecting to the machine it runs on needs none.

### Deployed Apps — Guest/Anonymous

<!-- ikon-example: flutter-sdk-auth-guest -->
```dart
// A development app: the host of the url `ikon app show <app-id>` prints, and dev's auth service
final client = await IkonClient.connectGuest(
  serverHost: 'my-app-1a2b3c4d.dev.ikonai.app',
  spaceId: 'my-space-id',
  authUrl: 'https://auth.dev.ikonai.com',
);
```

`serverHost` is the host of the app's `url` that `ikon app show <app-id>` prints from any folder (`ikon status`
shows it as `cloudUrl` only in a folder linked to the app), and `authUrl` is the
auth service of the app's environment. The auth service of the other environment refuses the app
with 403.

| Environment | `serverHost` | `authUrl` |
| --- | --- | --- |
| Development | `<domain>.dev.ikonai.app` | `https://auth.dev.ikonai.com` |
| Production | `<domain>.ikonai.app` | `https://auth.ikonai.com` |

`https://<serverHost>/ikon/auth` reaches the same auth service through the app's own host, in either
environment. `IkonNativeSignIn` below takes the same `serverHost` and `authUrl`.

### Deployed Apps — Signing In (Google, Apple, Microsoft, bank ID, email)

`IkonNativeSignIn` answers the server's `ClientFunctions.LoginAsync`, `LoginShowAsync` and
`LogoutAsync` calls on a phone, where a browser would leave for the provider and come back. OAuth
providers open in a web view that returns to the app's own https origin. The auth service returns a
token to no other kind of address, so a custom-scheme deep link never receives one. When the
provider's page cannot load because the phone has no connection, the web view says so and offers
Try again. Email sign-in uses the code the auth service mails. The sign-in is kept in secure
storage, so the next launch connects straight in as that user. Its token lasts 15 minutes, and the
SDK renews it with a refresh token before it expires and whenever the server refuses it. When the
session has ended because it expired or was revoked (for example, an organisation's directory
removed the person), the person is signed out. At launch the app then connects as a guest. While
the app is open, the client registered with `signIn.register` signs them out and `onIdentityChanged`
reconnects the app as a guest, which shows its sign-in. `sso:` connections and passkeys are not offered on a phone yet.

`ikon bundle --flutter-*` passes every method the app's surfaces list as `IKON_AUTH_METHODS` and, when
none admits anonymous visitors, `IKON_AUTH_REQUIRE_SIGN_IN=true`. The app template already does this; the essential parts are:

<!-- ikon-example: flutter-sdk-native-sign-in -->
```dart
final navigatorKey = GlobalKey<NavigatorState>(); // MaterialApp(navigatorKey: navigatorKey, ...)

final signIn = IkonNativeSignIn(
  serverHost: 'my-app-1a2b3c4d.ikonai.app', // a production app
  spaceId: 'my-space-id',
  authUrl: 'https://auth.ikonai.com',
  methods: const String.fromEnvironment('IKON_AUTH_METHODS').split(','),
  navigatorKey: navigatorKey,
  onIdentityChanged: reconnect, // dispose the client and connect again
);

// An app that admits no guest shows its sign-in first when no one is signed in on this phone:
// IkonSignInChooser(methods: signIn.supportedMethods, canClose: false, onChosen: signIn.signInWith)
if (!signIn.admitsGuests && signIn.supportedMethods.isNotEmpty && await signIn.storedToken() == null) {
  return null;
}

final client = await signIn.connect(); // the stored user, else a guest
signIn.register(client);               // answers login, loginShow and logout
```

`signIn.admitsGuests` is false when `IKON_AUTH_REQUIRE_SIGN_IN` is true, or, in a build without that
define, when `methods` lists neither `guest` nor `global`. Such an app shows the chooser before
connecting when `signIn.storedToken()` is null, and `connect()` does not connect it as a guest: with
no one signed in, or with a stored sign-in the backend refused, it throws `IkonSignInRequiredException`
(an `IkonAuthRejectedException`), and the app shows the chooser then too. When `supportedMethods` is
empty (the app lists only `sso:` or passkey sign-in) there is no chooser to show, so `connect()` tries
a guest and throws the backend's refusal.

The reason the app passes to `LoginShowAsync` appears under the heading of the chooser, and of the email
page that opens directly when email is the only method. `Title`, `Subtitle` and `EmailHint` in the
app's `[SignIn]` replace the stock heading, the line under it (a reason still takes its place) and the
email field's hint, on the web sign-in screen and here alike. `ikon bundle` and `ikon run` pass them in as
`IKON_SIGN_IN_TITLE`, `IKON_SIGN_IN_SUBTITLE` and `IKON_SIGN_IN_EMAIL_HINT`, which
`IkonSignInText.fromEnvironment` holds; pass an `IkonSignInText` as `text:` to set them in code. Text left
empty keeps the stock wording, English or Finnish, in the app's language: the `locale` of its
`MaterialApp` when set, else its only language when `supportedLocales` holds one (the default, English),
else the phone's language among the ones it lists. A Finnish phone sees an English-only app's sign-in in
English.

An app can draw that first screen itself instead, in its own branding. It still passes OAuth
providers to `signIn.signInWith(method)`, so they return to the https origin. If it asks for the email
code on its own screen, it exchanges the code with `IkonAuthenticator.verifyEmailCode`, passing the
address and space the code was sent for, and passes the session it returns to
`signIn.signInWithSession(session)`. The SDK stores that session like any other and the app
reconnects. It returns false when the phone's secure storage refuses the session, so the screen can
say so and let the user try again. An app's own sign-out button calls `signIn.signOut()`, which ends
the session at the auth service as well. `clearToken()` only removes it from the phone. Codes are nine
letters and digits unless the app's `[SignIn]` says otherwise, so give the code field
`emailCodeInputFormatters` and a text keyboard, and pass the typed code through `normalizeEmailCode`
before verifying it. `ikon bundle` passes the code's format and length as `IKON_AUTH_CODE_FORMAT` and
`IKON_AUTH_CODE_LENGTH`, and with `CodeFormat = "digits"` the SDK's own email screen opens a number
keyboard.

A person the app already knows can be signed in without typing anything: the app's server issues a
sign-in ticket with `app.SignIn.IssueTicketAsync(subject)`, the app gets its `Ticket` to the phone, and
`signIn.signInWithTicket(ticket)` redeems it, stores the session and reconnects. The ticket lasts two
minutes, works once, and signs in only the app's own user for that subject. `signInWithTicket` returns
false when the ticket is refused, and always in a local run, which signs in by email.

### Signing In on a Local Run

`IkonNativeSignIn.local(host:, port:, methods:, devUserId:, navigatorKey:, onIdentityChanged:)` runs the
same flow against a local `ikon run`, and the app template builds it whenever no space is injected, so
a local run tests the same sign-in code a deployed build ships. `ikon run --flutter` passes
the same `[SignIn]` and methods defines a bundle gets. Every method opens the email page; no mail is
sent and any code is accepted, and the address entered becomes the run's test user with that email (the
users `ikon browse --as-user` keeps in `browse-users.json`), whose profile lookups answer with that
email. After sign-in the app reconnects as that user, and a sign-out reconnects as a guest. With the
run's automatic sign-in the app starts as the developer (`IKON_DEV_USER_ID`); start it with
`ikon run --flutter --no-auto-frontend-login` to begin as a guest. The run refuses a test user when another
machine can reach it (`--host-lan`, `--host-public`, `--host`). A browser keeps the local sign-in only
until the page reloads; a simulator keeps it in secure storage. A launch from Studio's Devices tab
connects with the URL Studio passed and offers no sign-in.

### Deployed Apps — Signing the Web Page In from the Phone

A person signed in on the phone can sign the same app's web page in without signing in again. The
app turns this on by listing `phone` in a surface's `[Surface.Auth] Methods`. There are two ways,
and both open only this app's page, never another app's:

- `signIn.createWebSignInLink()` returns an address that opens the app's web page signed in, or null
  when no one is signed in on the phone. Open it on another of the person's devices within two
  minutes. Whoever opens it first is signed in, so never share it with someone else.
- `signIn.confirmWebSignIn(scanned)` approves the QR code the web page's sign-in screen shows. Pass
  it the text from the app's own scanner, or the address a
  deep link opened the app with. It shows which browser is asking and the code the page shows, and
  returns true once the person approves. It throws `IkonWebHandoffException` when the page's code has
  expired (`gone` is true) or the page is not this app's. The SDK has no camera dependency, so the
  app brings its own scanner.

A phone with no app can approve too. Its camera opens the QR code's address in the browser, and a
signed-in page there asks the person to approve.

### Deployed Apps — API Key

A program, such as a test, a bot or a device without a person signing in, connects with its space's
API key. The key is exchanged at the platform's backend, not at the app's host, so the call names
the backend (`IkonBackendType.production` by default, or `development` for an app on
`*.dev.ikonai.app`, which `ikon status` shows as the app's `environment`) and no app host:

<!-- ikon-example: flutter-sdk-auth-api-key -->
```dart
final client = await IkonClient.connectWithApiKey(
  apiKey: 'my-api-key',
  spaceId: 'my-space-id',
  externalUserId: 'my-program-user', // the same id is the same user on every run
  backendType: IkonBackendType.production,
);
```

Like the other `connect*` helpers it signs in again when the cached tickets expire, registers the
built-in client functions and sends `ClientReady`. A key the backend refuses throws
`IkonAuthRejectedException`. It also takes `parameters`, `userType`, `joinOnly` and `surface`, as
`connectGuest` does.

`ikon api-key create` in the app's folder makes an API key and prints it once. `ikon api-key create --app-id <id> --format json --output key.json` works from any folder and writes the key to `key.json` as its `apiKey` field, beside `spaceId`. The app's surface needs no `guest` or `global` entry in its `[Surface.Auth] Methods` for a key.

The user an API key connects as is the program's own: it is never one of the app's existing people
and has no email, so a role the app gives by email does not reach it. A program that a person runs
acts as that person by signing them in by email, where the surface it enters lists `email`:
`IkonAuthenticator.startEmailLogin` mails them a code, the person types it in, and
`IkonAuthenticator.verifyEmailCode` returns a session, and `connectWithToken` connects with its `token`.
That token lasts 15 minutes. `IkonAuthenticator.refreshSignInSession` renews it with the session's
`refreshToken`, and each renewal returns a new refresh token.
These are instance methods that reach only the `authUrl` they are given, never the authenticator's
host and port, so any authenticator works: `IkonAuthenticator(port: 443).startEmailLogin(...)`.
The IKON tool hands out no session token for a person.

A deleted API key (`ikon api-key delete <id> --app-id <app-id> --yes`, or without `--app-id` in the app's folder) is refused from the next sign-in on: a new connect, or a reconnect that has to sign in again, throws `IkonAuthRejectedException` with `statusCode` 403 and `Invalid key`, the backend's answer to a key it does not know. That 403 is about the key, not the app refusing the user; a key of another app gets `Invalid key: this key belongs to app <id>`. Deleting it does not end any session: a client the key already connected keeps going, and a reconnect that resumes the same session with the tickets the client holds needs no sign-in.

### Choosing the app instance

`parameters` on a `connect*` call are the app's client parameters, and they pick the app instance:
`parameters: {'room': 'harbour'}` joins the instance a browser joins at `?room=harbour`, for an app
whose `SessionIdentity` has a `Room` field (the name's case does not matter). The query of the path
the first connect opens at counts too, as a browser's address does: `IkonNavigation.instance.open('/?room=harbour')`
or a launch link before the connect picks the same instance, and a name passed in `parameters` wins
over the path's. Without either, the client joins the instance with that field empty.

### Joined and ready

A client joins its session in two steps: it connects, then sends `ClientReady`. Only at
`ClientReady` does the server tell the app the client is there, and only then does the app know who
the user is. Until then the client sends only its own setup: its function registrations, its
context, WebRTC signaling, events and analytics, and answers to calls it receives. Everything meant for the app, such as a
function call, an action or an upload, waits and goes out right after `ClientReady`, in the order it
was sent. Audio and video sent before `ClientReady` are dropped. Other than function calls, what is
still waiting when the connection drops is dropped too; an upload resumes after the next `ClientReady`.

A `functionRegistry.call` made while the client is connecting or reconnecting waits for the next
`ClientReady`, through any number of dropped attempts, and goes out right after it, behind the messages
held until then. A connection counts as open, and gets `ClientReady`, only once the server's first
message shows that it accepted the client's ticket. So a waiting call never goes into a session the
server no longer has, and goes out on the new session that replaces it instead. The wait counts against the call's 30 s timeout; a call that is never
sent in that time fails with a `TimeoutException` that names the function and says the client was not
connected. A call fails at once with `IkonNotConnectedException` when the client is not trying to
connect: it never connected, it was disposed, or it gave up reconnecting (`IkonClient.lastError`). A
waiting call fails the same way when the client stops trying.

A call already sent, and a `callStream` already sent, fails with `IkonNotConnectedException` the
moment its connection drops, even when the client then resumes the same session. The server does not
keep what it sends while the client is away, so the answer would never come. The app may or may
not have run the call. Retry it only when running it twice
does no harm, or ask the app whether it ran.

The `connect*` helpers send `ClientReady` before they return, and so does `IkonClient.connect()` on a
client built from an auth result (see "A client built from an auth result"). Every reconnect sends it
again, also one that resumes the same session.

`IkonClient.state` says where the connection is, and `onStateChange` reports each change: `idle`
before the first connect, `connecting` during it, `connected`, `reconnecting` from the moment a drop
is seen until a reconnect's connection opens, and `offline` or `offlineError` once the client has
stopped trying. `dispose()` sets `idle` again, and that change is the last `onStateChange` reports. A UI that shows the connection watches `onStateChange`. Work that must not send while the app
cannot be reached watches readiness instead, below.

`IkonClient.debugDropConnection()` closes the connection as a network failure does, without telling the server the client is leaving, and the client reconnects as after any drop. It does nothing unless the client is connected. It is for tests: a test calls it to simulate a drop and check the program's own recovery against a deployed app, whose socket a test cannot reach.

A connect that reaches no server (no network, no DNS answer, a refused port) throws `IkonServerUnreachableException` from `connectGuest`, `connectWithToken`, `connectWithApiKey`, `connectWithUrl` and `connectLocal`, and says nothing about the credentials, so the program retries it later. A refusal is an `IkonAuthRejectedException`.

Three members follow readiness, and they differ in timing:

- `IkonClient.isReady` says whether the client is ready now. It turns false the moment the connection
  drops and true as soon as a reconnect sends `ClientReady`.
- `IkonClient.watchReady(listener)` calls the listener synchronously on each of those changes, with
  `false` on a drop and `true` on each `ClientReady`, and returns the function that stops it. Use it
  for work that should pause on a drop and go on as soon as the client can send again.
- `IkonClient.onReady` fires once a reconnect's connection has stayed open for 3 s, so an attempt the
  server closes at once does not fire it. Use it for work that should run once per completed
  reconnect, such as reloading state. It fires up to 3 s after `isReady` turned true, and not at all
  for a reconnect whose connection drops within those 3 s.

`IkonClient.reconnectNow()` never drops a connection. While the client waits between two reconnect
attempts, it starts the next attempt at once; while connected, or while an attempt runs, it does
nothing. The connect helpers call it when the app comes back to the foreground.

A client from a `connect*` helper also reconnects after the app's server stops: after `ikon stop` and
a new `ikon run`, or a deploy. The old session is gone, so the client signs in again and joins a new
session, as it does whenever the server no longer has its session. It keeps trying until it gets in,
until a refusal that no retry can fix stops it (`IkonClient.lastError`), or until it is disposed. When
the backend answers a new-session attempt with a failure, the next one waits 10 s, doubling after each
failure up to 5 minutes and shortened at random by up to a quarter. After a
`ServerRateLimitedException` (HTTP 429), the next attempt waits at least the `retryAfter` it names.
The wait starts again from 10 s once the client has connected. A
`joinOnly` client goes offline instead (see "Joining only a running session"). Function registrations
and reactive subscriptions go to the new session by themselves.

To test how a program handles a drop, drop the connection in a proxy in the session's path. A proxy
in front of the port given to `connectLocal` does not see the session: that port serves only the
sign-in, and the session runs over the WebSocket URL the server returns (`IkonAuthResult.websocketUrl`).
To put a proxy in the session's path, build the client from an auth result with that URL changed.

### A client built from an auth result

The `connect*` helpers sign in with an `IkonAuthenticator` and build the client from its result. A
program can do the same steps itself, for example to read or change the session's URL first:

<!-- ikon-example: flutter-sdk-client-from-auth-result -->
```dart
final auth = IkonAuthenticator(host: 'localhost', port: 8443, expectedSpaceId: 'my-app-id');
Future<IkonAuthResult> signIn() => auth.authenticateLocal(userId: 'my-program', userType: UserType.machine);

final result = await signIn();
print(result.websocketUrl); // the session's WebSocket, chosen by the server

final client = IkonClient(IkonClientConfig.fromAuthResult(result));
// Lets a reconnect sign in again and join a new session when the server no longer has this one
client.configureReauth(authenticator: auth, connectUrl: result.connectUrl, hardReauth: signIn);
await client.connect(); // sends ClientReady, so the client is ready when it returns
```

Without `configureReauth` the client can only resume the session it had. Once the server has ended
that session, the client keeps retrying it and never gets back in. A client built this way does not register the built-in `ikon.client.*` functions and
does not reconnect when the app comes back to the foreground; the helpers do both.

### Entering a surface

`surface:` on `IkonClient.connectWithToken` and `IkonClient.connectGuest` names the `[[Surface]]` of the
app's `ikon-config.toml` the client enters. The backend checks the person against that surface's
`Methods` and `Roles` before it provisions anything, and refuses the connect otherwise. A guest enters
only a surface listing `guest` or `global`. Without `surface:` the client enters, leaving out sign-in pages,
the only surface left, else the one with neither `Routes` nor `Host`, else the first declared. A native
build sends no browser `Origin`, so it can never enter a surface that has a `Host`, named or by default:
the connect fails with 403. The app reads which surface a client entered from its `Context` (the `Surface` field). `IkonClient.connectLocal` takes `surface:` too, for a local run.

### Joining only a running session

Every connect above starts the app's server when none is running, and the app's organisation pays
for it while it runs. A client that should only join a session someone else opened — a companion
device, a monitor — passes `joinOnly: true` to `IkonClient.connectWithToken`, `IkonClient.connectGuest`
or the `IkonAuthenticator.authenticate*` call behind them. Such a connect never starts a server.
When no session is live, the connect throws `NoLiveSessionException`, whose `retryAfter` is how long
the backend asks the client to wait before trying again. An `authenticateApiKey` connect looks only in
its `userId`'s own instance of a per-user app, and the exception names that user (`externalUserId`). A client whose session ends goes offline
with that exception in `IkonClient.lastError` instead of reconnecting into a new server. Pass `userType: UserType.machine`
with it so the connection does not count as a person in the session and the session still ends when
its people leave.

### When the server is starting or has been replaced

While the app's server is starting, a connect retries for about 15 seconds. When the server refuses
the connect token because it was issued for a server that has since been replaced, or because it has
expired, the connect stops retrying and asks `/init` for a new token once. If that one is refused
too, it throws `ServerChangedException`. The sign-in is still valid, so keep the stored token and
connect again later. A connect refused by a full server asks `/init` again up to three times, naming
the servers that refused it, so a sharded app places the person on another shard; then it throws
`ServerFullException`.

### When the backend refuses the sign-in

A connect whose token the backend refuses throws `IkonAuthRejectedException`. Its `statusCode` is 401
when the token itself was refused (expired, or the sign-in revoked) and 403 when the app or surface
does not admit the person, or when an API key is unknown or deleted (`Invalid key`). A reconnect that meets either one stops retrying, because the same token
would be refused again: the client goes `offlineError` with the exception in `IkonClient.lastError`.
On a 401 it also fires `IkonClient.onCredentialRejected`, so the app can get a new token and pass it
to `client.updateAuthToken(token)`, which resumes the reconnect with it, or sign the person out.
`IkonNativeSignIn.register` does the sign-out for you.

### When the app is not available

A connect to an app that is suspended, unpublished, removed or not yet deployed throws
`SpaceNotFoundException` (HTTP 404 or 410). Only the app's owner can change that, so when a reconnect
gets this answer, the client goes `offlineError` instead of retrying, with the exception in
`IkonClient.lastError`. The sign-in is still valid, so keep the stored token and connect again later,
for example when the person asks to.

### When the platform no longer accepts the client

Only a new build fixes these, so the connect throws at once and a reconnect goes offline with the
exception in `IkonClient.lastError`:

| Exception | What is too old | The fix |
|---|---|---|
| `SdkUpgradeRequiredException` | This version of `ikon_sdk` | Update the package and ship a new build of the app |
| `AppUpgradeRequiredException` | The IKON libraries the app's deployed server was built on | The app's developer runs `ikon update` and deploys again |

`reason` is the platform's message and `minimum` the oldest version it accepts.

## Post-quantum key exchange

On every native platform the SDK's connections offer the post-quantum key exchange X25519MLKEM768
first, and deployed IKON servers choose it. A server on Windows, such as a local `ikon run` there,
serves TLS through SChannel, which has no ML-KEM, so it negotiates a classical key exchange.
Unlike the Rust, C++, .NET (Linux) and Node TypeScript
SDKs, the Dart SDK has no setting that refuses a classical key exchange: `dart:io` cannot limit
the groups it offers or report the one the server chose. A proxy on the device's network that
terminates TLS with a certificate the device trusts can therefore still choose a classical key
exchange. On the web the browser chooses. An app with `RequirePostQuantum = true` in its
`ikon-config.toml` offers no WebRTC, so the client plays and captures audio over its connection
and has no video: it neither shows video streams nor captures the camera or screen.

## Links and Notification Taps

On the web the browser opens the app at the address that was followed, and the app reads that path as
`Context.InitialPath`. A native app gets the same from `IkonNavigation`: every connect sends its
current entry as the session's initial path, and that entry is `/` unless something set it first.

- `IkonNavigation.instance.open(path)` sets the path (with its query, such as `/events/42?tab=info`) that the
  next connection opens at, and returns false for a path that does not start with a single `/`. Once a connection is up it navigates there instead, which the server
  raises as `Navigation.PathChangedAsync`, the same as a link tapped inside the app.
- After a dropped connection, the new session opens at the screen the app was showing, as a reloaded
  browser tab does.
- The app instance is chosen once, at the first connect (see "Choosing the app instance"). A path
  opened after it, even one with another `?room=`, only navigates inside the instance, and a reconnect
  keeps it. To move to another instance, connect a new client with that instance's `parameters`.
  `ClientFunctions.NavigateToAsync`, which a browser follows to another instance's `JoinUrl`, returns
  false on a Flutter client.
- `IkonDeepLinks` reads the link or the tapped notification that launched the app and opens its
  path before the first connect, so the app starts on that screen and `/` is never shown first. It
  then follows the links and notification taps that arrive while the app runs. It handles a link only
  when it is an https (or http) link to one of the app's hosts, which `ikon bundle` passes in. The SDK's own `ikon-`
  query parameters are dropped from the path.

The frontend that `ikon new --flutter` creates already does this. An older frontend adds an
`IkonDeepLinks` to the state of the widget that connects, starts it before the first connect and
stops it on dispose:

<!-- ikon-example: flutter-sdk-deep-links -->
```dart
final IkonDeepLinks _deepLinks = IkonDeepLinks();

@override
void initState() {
  super.initState();
  // In place of a direct _connect(): the launch link's path becomes the first connect's path
  _deepLinks.start().whenComplete(() {
    if (mounted) _connect();
  });
}

@override
void dispose() {
  _deepLinks.stop();
  super.dispose();
}
```

A push notification delivered through Firebase while the app was closed is not seen by the SDK. Pass
its path to `IkonNavigation.instance.open` before connecting, for example from
`FirebaseMessaging.instance.getInitialMessage()`.

For a phone to open https links in the app rather than in the browser, the app's domain must serve
files that name the app. See [Store identity and app links](#store-identity-and-app-links).

## Store Identity and App Links

By default `ikon bundle`, `ikon distribute` and `ikon device install` give the native app the bundle
id `app.ikon.s<space id>` on iOS and Android, with `.dev` added for the development platform. An app
that already exists in the App Store or Google Play keeps its listing, its users' data and its
updates only when the new build has the same ids and is signed by the same Apple team and Play app
signing key. Name them under `[Flutter]` in `ikon-config.toml`:

```toml
[Flutter]
IOSBundleId = "com.example.events"
# Only when it differs from IOSBundleId
AndroidApplicationId = "com.example.events"
AppleTeamId = "ABCDE12345"
# The upload key's and the Play App Signing key's SHA-256, from the Play Console
AndroidCertFingerprints = ["AB:CD:...:EF"]
# Hosts besides the app's own whose links open it
AssociatedDomains = []
```

- A declared id is the production id as written. A development build adds `.dev`, so a test build
  installs beside the store app instead of replacing it.
- One id stands for both platforms when only one is given. An id that one platform refuses (`-` is
  valid only on iOS, `_` only on Android) is refused when the config loads, with a message that asks
  for the other one.
- `AppleTeamId` is also the team `ikon bundle --signing self` signs with when `--apple-team` is not given.
  Keystores and passwords never go in the config; pass them to `ikon bundle --signing self`.

The same section turns on universal links (iOS) and app links (Android) for the app's hosts: its
platform hostname, each custom hostname that serves the app (`www.` of one that serves at www), and
`AssociatedDomains`.

- With `AppleTeamId`, the iOS build gets the `com.apple.developer.associated-domains` entitlement with
  `applinks:<host>` for each host, and every deploy serves `/.well-known/apple-app-site-association`
  on the app's domain, naming `<team>.<bundle id>` for all paths except `/api`, `/ikon`, `/__ikon` and `/.well-known`.
  The App ID needs the Associated Domains capability, which Xcode's automatic signing adds; a free
  personal team cannot sign it.
- With `AndroidCertFingerprints`, the Android build gets an `android:autoVerify` intent filter for
  https on each host, and every deploy serves `/.well-known/assetlinks.json`.
- A host in `AssociatedDomains` is served by someone else, so copy the two files to it from the app's
  own domain.
- A file the app's frontend ships itself under `public/.well-known/` is kept, and the deploy says so.
- A build signed with IKON's identities (`ikon distribute`) is not the app the files name, so its
  links open in the browser.

## Responsiveness

`IkonParallaxView` sends the app its size, the device theme and whether the app is in the
foreground each time one of them changes, and sends the device's time zone with the first of these
updates. So the app lays out for the screen with no code in your Flutter program. `client.viewportUpdater.sendTimezone` reports another
time zone.

A Flutter app reacts to a tap before the server answers, with no app code:

- Value controls (tabs, selects, radio groups, toggle groups, switches, checkboxes, sliders) show
  the picked value at once and keep it until the server has handled the tap, as on the web.
- A tap the server has not answered after 150 ms runs a thin bar along the top of
  `IkonParallaxView` until the server's update arrives (`showPendingBar`, `pendingBarColor`).
  `IkonClient.actionTracker` reports how long each tap took to reach the screen.
- A press on a control with a `path:` moves the app to that path at once: a `view.NavBar` item
  turns selected and, until the server sends the page, `view.NavOutlet` shows the page as the phone
  last showed it, without taking taps. A page the phone has not shown yet appears as a skeleton
  drawn from the deploy's capture for the route on the surface the client entered, with the
  styles that capture carries. The capture is the surface's own snapshot of the route, or else
  the file of its first matching seed rule. Without one, or when the capture's outlet is empty,
  the page being left stays, dimmed and not taking taps, until the new page arrives.

An app can also start on the screen it showed last time instead of a blank one. Give the view a
`liveSnapshotKey`, one per app and environment such as its space id, and while connecting show
what `IkonLiveSnapshot.load` returns. A screen saved over 5 minutes ago is drawn as a skeleton.
When the phone has none, the boot snapshot the deploy captured for the route stands in: the view
downloads it after it connects. Call `IkonLiveSnapshot.clear` when the person signs out. The
template's `main.dart` does all of this.

<!-- ikon-example: flutter-sdk-start-screen -->
```dart
// While connecting: what IkonLiveSnapshot.load('my-space-id') found, the screen this phone
// showed last time or else the deploy's skeleton
if (uiCore == null) {
  return startScreen == null ? const Scaffold() : Scaffold(body: SafeArea(child: IkonSnapshotView(snapshot: startScreen)));
}

// Connected: the view keeps its screen for the next start, under the same key
return IkonParallaxView(uiCore: uiCore, client: client, liveSnapshotKey: 'my-space-id');
```

## Keyboard

A tap outside the focused text field closes the keyboard, and on iOS so does dragging a scrolling
list. A tap on a button or another field leaves it open, so a chat's Send button keeps the keyboard
up, and on Android scrolling leaves it open too. The Parallax view applies this to the streamed UI,
its dialogs and its menus, and the email sign-in page does the same. In a dialog or sheet only a tap
on its panel closes the keyboard; a tap beside the panel closes the dialog. iOS number pads have no
key that closes the keyboard, so without it a field in a streamed form could keep the keyboard open
over the page. Screens an app draws itself get the same behaviour when it wraps them in
`IkonKeyboardDismissal`. By default it takes a tap anywhere in its box, empty space included; pass
`behavior: HitTestBehavior.deferToChild` to take only taps on the child's own content, so a tap on
empty space reaches whatever lies underneath:

<!-- ikon-example: flutter-sdk-keyboard-dismissal -->
```dart
return MaterialApp(
  builder: (context, child) => IkonKeyboardDismissal(child: child!),
  home: const MyHomeScreen(),
);
```

## Custom Components

Register custom Flutter widgets for node types the server sends:

<!-- ikon-example: flutter-sdk-custom-component -->
```dart
final registry = IkonComponentRegistry();
registry.register('my-custom-widget', (UiNode node, ResolvedStyle style, List<Widget> children, IkonRenderContext context) {
  final data = node.props['data'] as String? ?? '';
  // The id of a view.CreateAction<string> the C# side passed as a prop. A String payload is sent
  // as it is, anything else as JSON
  final onTapId = node.props['onTap'] as String?;
  return GestureDetector(
    onTap: () => context.dispatchAction(onTapId, data),
    child: MyCustomWidget(data: data),
  );
});

// Use the custom registry
final view = IkonParallaxView(uiCore: uiCore, client: client, registry: registry);
```

## Multi-mount apps

Apps that declare multiple Parallax sub-trees via `IAppBase.Mounts` can be selectively embedded by passing the `mount:` argument:

<!-- ikon-example: flutter-sdk-mount -->
```dart
final view = IkonParallaxView(uiCore: uiCore, client: client, mount: 'aiCanvas');
```

When `mount` is `null`, the most recently updated `ikon-ui` stream is rendered, or else the only stream other than the platform overlay; with several such streams nothing renders. The lower-level streams API is available on `IkonUiCore`: `streams`, `streamFor(category)`, `streamForMount(id)`, `streamsByCategory(category)`, plus `onStreamUpdate` / `onStreamEnded` listeners.

## UI Without a View

A program can read and drive the app's UI without rendering it. `IkonUiCore(client)` keeps the
tree the app sends: `currentTree` is its root `UiNode`, or null before the first update, and
`onUpdate(listener)` calls the listener with the root after each update and returns the function that
stops it. Each `UiNode` has a `type`, an `id`, its `props` and its `children`. The standard components put their text and labels in these props:

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
| `std.file-upload`, `std.file-upload-zone` | | `uploadActionId`, the id an upload names |
| A control declared with a `path`: `std.button`, `std.nav-item` and others | `actionPaths`, a JSON object from the control's action id to the app path its press opens (`/stable/{value}` for a control that sends a value) | The Flutter view moves the client to that path itself when the action is pressed, and back when the app refuses it; a client without the view calls the action with `sendAction`, and the app then moves it |
| `std.button` with an `href` (a link) | `href`, `target`, and the text as on a button | None unless the app also handles the press (`onInvokeId`, `{}`). A client follows the `href` itself |
| `std.action-button` | `label`, `actionType` (`CopyToClipboard`, `DownloadFile`, `Share` and the others) and `actionOptionsJson`, the action's options as JSON | The action runs in the browser, so a client does it itself. `onActionCompleteId` reports it, `{"ActionType":"CopyToClipboard","Success":true}` |
| `std.feed-scroller` | `activeIndex` when the app controls it, `slideCount`, `muted`. Each slide is a `std.feed-slide` with its `index` | `onActiveChangeId` when a slide becomes the active one and `onScrollNearEndId` near the end, the slide's index as a number (`3`); `onMuteChangeId`, `true` or `false` |

To find a control by its name, read its `aria-label` when it has one, else its `label`, else the text of its `std.text` children. To find a text whatever its style, match both `std.text` and `std.heading`.

A payload the app cannot read, such as `0.8` for a slider instead of `[0.8]`, fails the call like a handler that throws: `onResult` gets false, and the reason is only in the app's log (`ikon logs --level warn`).

`client.sendAction(actionId, payloadJson, onResult)` sends an action as a tap would. A button press
sends `'{}'`, the default. A text field change sends the new text itself as the payload, not JSON.
`onResult` is called with whether the app's handler succeeded, once it has run. A handler that waits
on an AI call can take minutes, and sending the action again meanwhile runs the handler a second
time, so watch the app's UI for its busy state instead:

<!-- ikon-example: flutter-sdk-ui-without-view -->
```dart
UiNode? findNode(UiNode? node, bool Function(UiNode node) match) {
  if (node == null || match(node)) {
    return node;
  }

  for (final child in node.children) {
    final found = findNode(child, match);

    if (found != null) {
      return found;
    }
  }

  return null;
}

// Completes with the first node that matches, in the tree now or in a later update
Future<UiNode> waitForNode(IkonUiCore ui, bool Function(UiNode node) match) {
  final found = Completer<UiNode>();

  void check(UiNode? root) {
    final node = findNode(root, match);

    if (node != null && !found.isCompleted) {
      found.complete(node);
    }
  }

  final stop = ui.onUpdate(check);
  check(ui.currentTree);
  return found.future.whenComplete(stop);
}

final ui = IkonUiCore(client);

// Press a button: its onInvokeId is the action, and a press sends '{}'
final button = await waitForNode(ui, (node) => node.type == 'std.button' && node.props['label'] == 'Refresh');
final handled = Completer<bool>();
client.sendAction(button.props['onInvokeId'] as String, '{}', handled.complete);
print('the app handled the press: ${await handled.future}');

// Type into a text field: the new text is the whole payload
final field = await waitForNode(ui, (node) => node.type == 'std.text-field');
client.sendAction(field.props['onValueChangeId'] as String, 'hello');

// Read what a text shows
final status = await waitForNode(ui, (node) => node.type == 'std.text' && '${node.props['content']}'.startsWith('Status:'));
print(status.props['content']);
```

Create the `IkonUiCore` right after the connect helper returns: the client keeps the UI messages that
arrive before it exists and passes them to it, so the first tree is not lost. After a reconnect it asks
the app for the whole tree again, and `onUpdate` fires with it. A reconnect into a new session can
change the action ids, so read them from the current tree rather than keeping them.

## Client Events

`IkonClient.onClientJoined`, `onClientUpdated` and `onClientLeft` are streams of the other clients in
the session, the app included, joining, reporting a new viewport, theme, timezone or visibility, and
leaving. A client is reported as joined once it is ready: the app knows who it is, and it has been sent
the session's state, so the app can answer it at once. Each event carries the client's `Context` as
other clients see it: `sessionId`, `userId`, `contextType` and what it reports of its viewport, theme,
timezone and visibility, without the device and sign-in fields only the app sees. None is ever about
this client; its own join is `onReady`. Events reach the listeners of the three streams in the order
the server sent them, so a map kept from them never sees a client leave before it joined.

<!-- ikon-example: flutter-sdk-client-events -->
```dart
final clients = <int, Context>{};
client.onClientJoined.listen((other) => clients[other.sessionId] = other);
client.onClientUpdated.listen((other) {
  clients[other.sessionId] = other;
  print('${other.sessionId} now uses the ${other.theme} theme');
});
client.onClientLeft.listen((other) => clients.remove(other.sessionId));
```

The client reports each other client once per session. On joining, `onClientJoined` fires for each
client already there. The server sends these events in answer to the ClientReady message that
`connect()` and the connect helpers send just before they return. So add the listeners as soon as the
connect returns, with no `await` in between, or, on a client built with `IkonClient(...)`, before
`connect()`. Events that arrive before a listener is added are not kept for it. To catch up later, read
`client.globalStateHandler.state.clients`. It holds the session's clients by session id, including this
client and the app, and the client keeps it up to date from the same events. A reconnect into the same session
reports only what changed while the client was away: `onClientJoined` for a client that became ready
meanwhile, and `onClientUpdated` for a known client whose viewport, theme, timezone or visibility
changed. A reconnect into a new session reports every client again, as the first join did.
`onClientUpdated` and `onClientLeft` fire only for a client `onClientJoined` reported, so a client that
leaves before it is ready raises nothing. A client that left while this client was away raises
`onClientLeft` once the server has sent the clients still there after the reconnect. A disposed client
tells the server it is leaving, so the others get `onClientLeft` at once rather than when its reconnect
window ends. A client whose connection drops, a program that was killed included, stays listed for that
soft-retry window, 5 minutes by default, so it can resume: until then its `Context` has
`isSoftDisconnected` true and `softDisconnectAt` set, and `onClientLeft` fires when the window has passed.

## Client Functions

The server can call functions registered on the Flutter client:

<!-- ikon-example: flutter-sdk-client-function -->
```dart
client.functionRegistry.register(
  FunctionDefinition(name: 'my.customAction', resultTypeName: 'System.String'),
  (args) async {
    // Handle the call from the server
    return 'result';
  },
);
```

`resultTypeName` is the .NET full name of what the handler returns, such as `System.String`,
`System.Double` or `System.String[]`; for a streaming function, the type of each item. The app keeps
it as the function's declared return type. It does not change what a call returns: the app's
`FunctionRegistry.CallAsync<T>` converts the JSON result to its own `T`. It is `System.Object` when
left out, and a name the app cannot load, such as `string`, is taken as `System.Object` too.

Built-in Flutter functions, registered by the `IkonClient.connect*` helpers and called by the server, include:
- `ikon.client.getTheme` / `setTheme` — `dark` or `light`; `setTheme` drives the renderer's light/dark token variants and returns false for an unrecognised name
- `ikon.client.getLanguage` — device locale
- `ikon.client.getTimezone` — device timezone
- `ikon.client.getViewport` — viewport dimensions
- `ikon.client.getVisibility` — app lifecycle state
- `ikon.client.vibrate` — haptic feedback
- `ikon.client.keepScreenAwake` — wakelock; returns false when the platform refuses it, as a browser does without a user gesture
- `ikon.client.scrollTo` — a no-op that returns true
- `ikon.client.getUrl` / `setUrl` — a URL stack that `setUrl` pushes or replaces; the back gesture pops it and reports the path underneath
- `ikon.client.goBack` — the server's `Navigation.BackAsync`: `IkonNavigation.instance.goBackOr(fallbackPath)` pops one entry as the back gesture does, or, with nothing to pop, replaces the current entry with the fallback path; either way the new path is reported, except that a client already on the fallback path reports nothing and returns true
- `ikon.client.getLocation` — geolocation (via `geolocator`)
- `ikon.client.getNotificationPermission` / `showNotification` — local notifications (via `flutter_local_notifications`, permission requested lazily on first send)
- `ikon.client.getPushSubscription` — FCM token for offline push (returns null unless the app wires `ikonFcmTokenProvider`; Firebase stays opt-in)
- `ikon.client.captureImage` — system camera (via `ImagePicker`)

### Client functions that stream

`functionRegistry.registerStream` registers a function whose handler returns a `Stream`. The app
calls it as an `IAsyncEnumerable<T>`, for example with `FunctionRegistry.CallAsyncEnumerable<T>` and
the client's session as `targetId`, and reads each item the stream emits:

<!-- ikon-example: flutter-sdk-client-stream-function -->
```dart
// The app reads it as IAsyncEnumerable<double>. When the app cancels, the subscription is cancelled
client.functionRegistry.registerStream(
  FunctionDefinition(name: 'my.recentReadings', resultTypeName: 'System.Double'),
  (args) => readings.take(args.single as int),
);
```

The handler's stream is listened to when the app calls the function. Items go as JSON, or as binary
for a `Uint8List`, in batches as the C# SDK sends them: an item waits at most 100 ms for others to
join it, a batch holds at most 50, and an item of 1 KB or more goes at once. The stream's end ends the
app's enumeration. The stream's first error fails the app's call with the error's text, after the
items before it. An item that cannot be encoded as JSON fails the call the same way. The subscription
is cancelled when the app cancels the call, and when the client reconnects into a new session, because
the app there no longer has the caller. When items or the end are due while the connection is down,
the subscription is cancelled too, and once the client is back in the same session the app's call
fails with an `IOException`, so the app never reads a stream with a gap as if it were whole.
`resultTypeName` names the item type.

### Calling app functions

`functionRegistry.call<T>(name, args)` calls a function the app registered and returns its result.
`callStream<T>(name, args)` calls one that returns `IAsyncEnumerable<T>` and delivers its items as a
`Stream<T>`; a function that returns a single value gives a stream of that one value. A `byte[]`
argument or result is a `Uint8List`:

<!-- ikon-example: flutter-sdk-function-stream -->
```dart
// An app function returning IAsyncEnumerable<int>; cancelling the subscription,
// here by take(3), cancels the enumeration on the app
await for (final n in client.functionRegistry.callStream<int>('count_to', [100]).take(3)) {
  print(n);
}

// byte[] goes both ways as a Uint8List
final thumbnail = await client.functionRegistry.call<Uint8List>('make_thumbnail', [photo]);
```

The call goes out when the stream is listened to. Cancelling the subscription before the stream ends
sends the app a cancel, which cancels the function's `CancellationToken`. A stream fails with a
`TimeoutException` and is cancelled when the app has neither acknowledged it nor sent an item within
30 s; once it has, the stream has no time limit. An error the function throws ends the stream with
that error. A value the app encodes as Teleport rather than JSON fails the call with a
`FormatException`.

`callWithCancel<T>(name, args, cancel: future)` is `call` with a cancel. When the future completes
before the result arrives, the call fails with `IkonCallCancelledException` and the app is told to
cancel it, which fires the function's `CancellationToken`. A call still waiting for the client to be
ready is never sent, and a result that arrives after the cancel is dropped:

<!-- ikon-example: flutter-sdk-call-cancel -->
```dart
try {
  final summary = await client.functionRegistry.callWithCancel<String>('summarize', ['report.pdf'], cancel: userClosedDialog);
  print(summary);
} on IkonCallCancelledException {
  // The app's CancellationToken fired too
}
```

## Reactive Values

`client.reactiveRegistry` follows a reactive of the app. `subscribeByName` finds a reactive field or
auto-property of the app's `[App]` class by its name in the source, through the app's
`Ikon.Reactive.GetStableIdByName` function, and its future fails when the app has no reactive by that
name, or more than one. For a reactive kept anywhere else, the app returns its `StableId` from an
`External` function and the program passes that id to `subscribe`. Both return a function that
unsubscribes:

<!-- ikon-example: flutter-sdk-reactive-by-name -->
```dart
// The callback runs once with the current value before the future completes, then on every change
final unsubscribe = await client.reactiveRegistry.subscribeByName<int>('_visitorCount', (count) {
  print('Visitors today: $count');
});

// Later: stop receiving changes
unsubscribe();
```

The value arrives as parsed JSON (`jsonDecode`) with the C# property names. Changes made close
together can arrive as one call with the latest value. After a reconnect the client subscribes to
each reactive again.

## App Messages

An app can declare its own messages in `schema/*.tp` and exchange them with clients as typed objects,
for data that changes too often for the UI or a function call, such as cursors or game input. `ikon
schema` (also run by `ikon build` and `ikon run`) generates a Dart class for each into
`frontend-flutter/lib/generated/protocol/`; a client outside the app's folder gets them from the
folder `schema/outputs.toml` names (see the Teleport schema spec). Each class implements
`TeleportMessage`, and its static `teleportMessageType` carries its opcode, payload version and
decoder, so a client never names an opcode:

<!-- ikon-example: flutter-sdk-app-messages -->
```dart
// CursorMove and CursorMoved are generated from the app's schema/CursorMove.tp and CursorMoved.tp
final cursors = <int, ({double x, double y})>{};
final stop = client.onMessage(CursorMoved.teleportMessageType, (moved, senderId) {
  cursors[moved.clientSessionId] = (x: moved.x, y: moved.y);
});

client.sendMessage(CursorMove(x: 0.25, y: 0.5));
```

`sendMessage` is the client's side of the app's `app.OnMessage<T>`. A message sent before the client
is ready is held and goes out right after ClientReady. A message sent with no connection is dropped.
`onMessage` is the client's side of `app.SendMessage<T>`: the handler gets each message of that type,
decoded, with the sender's session id, and no message of another type. It lasts across reconnects
until the returned function is called or the client is disposed. A client built with `IkonClient(...)`
can add it before `connect()`, so it also gets what arrives with the connection. With a connect helper,
add it as soon as the helper returns: the app learns of the client only from the ClientReady the helper
sends just before it returns. A message that does not decode, or whose handler throws, is logged and
lost.

## File Upload

<!-- ikon-example: flutter-sdk-file-upload -->
```dart
final uploader = IkonFileUpload(client);
final result = await uploader.upload(
  uploadActionId: uploadActionId, // the upload node's uploadActionId prop, or an id from app.Uploads.Register
  filename: 'photo.jpg',
  mimeType: 'image/jpeg',
  data: fileBytes,
  onProgress: (sent, total) => print('$sent / $total'),
);
```

The upload action id tells the app which handlers receive the file. It is the `uploadActionId` prop
of the app's file upload node (`std.file-upload`) in the UI tree, which the view reads when a person
picks a file and a program reads as "UI Without a View" shows. An upload with no node on screen, such
as a background sync, names a fixed id that the app registers with
`app.Uploads.Register("my-app.photos", onStart: ...)`.

An upload over an `IkonClient` starts once the client is ready, and keeps going when the connection
drops: when the client is ready again, also after signing in again, it carries on from what the
server already has. It gives up when the client is not ready again within 45 s. It sizes its chunks to the speed of the link, so a slow mobile connection still
finishes. `onProgress` reports the bytes the server has acknowledged, a chunk at a time. Chunks grow
with the speed of the link, so on a fast link the count moves in large steps. `uploadStream` sends a
file from disk without loading it into memory, and reads it again from disk when the upload resumes:

<!-- ikon-example: flutter-sdk-upload-from-disk -->
```dart
final file = File('photos/harbour.png');
final zone = await waitForNode(ui, (node) => node.type == 'std.file-upload');
final result = await IkonFileUpload(client).uploadStream(
  uploadActionId: zone.props['uploadActionId'] as String,
  filename: 'harbour.png',
  mimeType: 'image/png',
  length: await file.length(),
  openRead: file.openRead, // read again from disk if the upload resumes
  onProgress: (sent, total) => print('$sent / $total acknowledged'),
);

if (!result.success) {
  // Retry without an uploadId when retriable: each upload then gets a new id
  print('${result.error} (retriable: ${result.retriable})');
}
```

A failed `result` says in `retriable` whether sending the same file again is likely to work. It is
false when the server may already have stored the file. `uploadId` names the upload to the app, and
each upload gets a new one when it is left out. Leave it out of a retry too: the server can hold a
failed upload under its id for up to 60 s, and refuses another upload under the same id until then.
A retry under a new id sends the whole file again. A refusal at the start is not retriable. The server
refuses at the start when, among other reasons, the app declined the file, the upload action id has no
handlers, or the id was in use. `error` gives the reason the app sent, and the app's log names it.

The file-upload and file-picker nodes the server sends open the phone's own picker. To offer other
sources, such as recording with an in-app camera, implement `IkonFilePicker` and pass it as
`IkonParallaxView(filePicker: ...)`. The view reads it once, when it first appears.

When the server node sets `maxImageDimension`, `imageQuality` or `imageFormat`, the view shrinks a
picked JPEG, PNG, WebP or HEIC photo before it checks `maxFileSize` and uploads: it decodes with
`dart:ui` (HEIC on iOS and on Android 9 and later) and encodes PNG with `dart:ui` and JPEG with the
`image` package. Flutter has no WebP encoder, so a photo that would be encoded as WebP uploads as
JPEG; a WebP photo that needs no resize uploads as it is unless `imageFormat` asks for JPEG or PNG. A picker
that implements `IkonScalingFilePicker`, as `NativeFilePicker` does, also gets the options, and
`NativeFilePicker` passes them to `image_picker` as `maxWidth`, `maxHeight` and `imageQuality`, so a
gallery photo arrives already scaled (and on iOS a HEIC photo as JPEG).

## Media

The SDK bundles Opus + PCM + WebRTC implementations behind pluggable interfaces
(`AudioEncoder`, `AudioDecoder`, `AudioOutput`, `MicSource`); the `enableIkon*` helpers
wire the bundled ones:

<!-- ikon-example: flutter-sdk-media -->
```dart
// Opus mic capture (bundled OpusAudioEncoder + record-based mic source)
final capture = enableIkonAudioCapture(client);
await capture.start(sampleRate: 48000);
// Or construct manually without a micSource, start it, then feed PCM frames yourself
// (frames fed before start() are dropped):
// final capture = IkonAudioCapture(client: client, encoder: OpusAudioEncoder());
// await capture.start(sampleRate: 48000);
// await capture.feedPcmFrame(pcmData);
await capture.stop();

// Opus playback (bundled OpusAudioDecoder + PcmStreamOutput)
final playback = enableIkonAudioPlayback(client);

// Live WebRTC A/V (peer connection to the server SFU via flutter_webrtc);
// the Opus stack above remains the fallback when WebRTC is unavailable. The peer
// comes up with the connection unless the surface's Media is none or the
// server offered no WebRTC entrypoint (client.offersWebRtc is false).
final webRtcMedia = enableIkonWebRtcMedia(client);
await webRtcMedia.startCameraCapture();
```

`OpusAudioEncoder` and `OpusAudioDecoder` bring their own libopus on Android, iOS, Windows and the
web. On Linux and macOS they open the system's, so a Linux machine needs the `libopus0` package
(`apt install libopus0`) and a Mac `brew install opus`; without it their `initialize` throws an
`UnsupportedError` that says so. This holds for a program without a window too.

The capture `enableIkonAudioCapture` returns can also be started and stopped by the server, with
`ClientFunctions.StartAudioCaptureAsync` and `StopCaptureAsync`, as on the web, for a microphone an
app keeps open across pages. It is one microphone: a start while it captures, or while another
start waits for the permission prompt, fails on the server and does nothing from a button.

The SDK has no equivalent of the TypeScript SDK's `client.viseme`: it plays the audio but drops the
shape values an `IAudioAnalyzer` sends with it, so lip-sync avatars and level meters driven by
`VisemeAnalyzer` or your own analyzer work in the React frontend only.

`IkonMediaManager` combines audio capture, playback, and WebRTC media (and routes
share/clipboard actions through `IkonShare`) and can be passed to
`IkonParallaxView(mediaManager: ...)`.

### Encoded frames without a device

A program without a microphone, camera or Flutter view, such as a test or a bridge from another
media source, sends and receives encoded frames with `IkonAudioStreams` and `IkonVideoStreams`:

<!-- ikon-example: flutter-sdk-encoded-media -->
```dart
// Audio: one 20 ms Opus frame per call; the correlation id ties it to the app's capture
final audio = IkonAudioStreams(client);
audio.sendOpusFrame(streamId: 'mic', data: opusFrame, isFirst: true, correlationId: 'press-1');
audio.onFrame.listen((frame) => print('${frame.streamId}: ${frame.data.length} bytes'));

// A program that plays the app's audio itself says how far it got, every 500 ms and when the state changes
audio.sendPlaybackReport(streamId, played: position, buffered: queued);

// Video: encoded frames, keyframe first; a new size or codec announces the stream again
final video = IkonVideoStreams(client);
video.sendFrame(
  streamId: 'camera',
  data: h264Frame,
  isKey: true,
  timestamp: Duration.zero,
  codec: VideoCodec.H264,
  width: 640,
  height: 480,
);
video.onKeyFrameRequested.listen((request) => print('next frame of ${request.streamId}: a keyframe'));
video.onStreamBegin.listen((info) => video.requestKeyFrame(info.streamId));
```

What a client sends reaches only the app. A stream the client receives has the id
`<sender session id>_<track id>`; `senderStreamId` on its info is the id the sender gave it.
`sendPcm` encodes 16-bit PCM to Opus in 20 ms frames, padding the last one when `isLast` is set;
it throws `StateError` unless the streams were built with an encoder,
`IkonAudioStreams(client, encoderFactory: OpusAudioEncoder.new)`.
Frames sent before `ClientReady` are dropped and the send returns false. After a reconnect the next
frame announces the stream again. `isFirst` starts a new audio segment, and a new `correlationId`
given with it announces the stream again. The app's speech recognition skips an audio stream without
a correlation id. `onKeyFrameRequested` fires when the app asks for a keyframe, and the
next frame sent should be one. For H.264 each frame is one access unit (every slice of one picture) in Annex B form with start codes, each keyframe with its SPS and PPS before the IDR slice, because a receiver decodes from the parameters in the stream. ffmpeg's libx264 writes such a stream with `-c:v libx264 -x264-params aud=1:repeat-headers=1 -f h264`: an access unit delimiter starts each picture, so a reader of its output splits the frames there, and every keyframe repeats the SPS and PPS.

`sendPlaybackReport` tells the app how far this client has played a stream it receives. The app
reads the latest report with `Audio.Raw.GetPlaybackStatus` and paces what it says to what was heard.
The stream is named by its `streamId` or its `senderStreamId`. `played` is the position from the
start of the current segment, null when it is not known, and `buffered` is what was received but not
yet played. `state` is `AudioPlaybackState.playing` by default, or `blocked` or `hidden`. The report
names the epoch of the newest frame received. Only the app's `Audio.Raw` streams ask for reports,
and every stream of an app built before streams could say so: `wantsPlaybackReports` on the stream's
info is true for them, and false for every other stream, such as the one that carries what
`Audio.Play` plays. It sends nothing and returns false for a stream that
is not open or wants no reports. The web
SDK reports every 500 ms of playback for each stream, and at once when the state changes; a program
that plays the audio should do the same. The SDK's own Opus playback (`enableIkonAudioPlayback`)
sends no reports.

A headless client also reports its context: `IkonViewportUpdater.sendViewportSize`, `sendTheme`,
`sendTimezone` and `sendVisibility` update what the app reads as the client's context, and
`IkonNavigation.instance.navigateTo` changes the path the app sees. A timezone given with
`sendTimezone` is not replaced by the one the SDK looks up.

## Supported Crosswind Styles

The Flutter SDK resolves ~100 Crosswind utilities to native Flutter types:

**Layout:** padding, margin (including `auto`), width/height/min/max (including the `3xs`–`7xl` container widths), flex (direction/wrap/gap/align/justify/shrink), `gap-x`/`gap-y` apart, grid (`grid-cols-N`, and `grid-flow-col` with `grid-rows-N`), overflow, aspect ratio (`aspect-square`, `aspect-video`, `aspect-[20/9]`, `aspect-[2.5]`), position (absolute/relative), z-index. Logical sides (`ps`, `pe`, `ms`, `me`, `start`, `end`) resolve left-to-right. Layout follows CSS where Flutter's defaults differ:
- A row with no `items-*` stretches its children to its height; a child with its own height (`h-24`, an image) stays at the top.
- A stretching column leaves a child with its own width (`w-[56px]`, `w-1/2`, `w-fit`) at that width. In a container without `flex`, a button or an image is as wide as its content.
- A negative margin gives up its space, so the element overlaps its neighbour and the content after it moves up.
- `shrink-0` keeps a `truncate` child at its full width.
- `text-align` on a container reaches the texts inside it.
- An absolute child keeps its own height inside a smaller `overflow-hidden` box, which crops it.
- An absolute child with no width of its own is as wide as its content, up to its containing block, so a badge in a card's corner (`absolute top-3 left-3`) hugs its label; one pinned to both sides (`inset-x-0`, `left-0 right-0`) spans between them.
- An `overflow-hidden` box with a border clips its content inside the border, to the padding box, with the corner radius less the border width, so a `rounded-full border-[3px]` ring stays visible around an image.
- Children paint and take taps in CSS stacking order: a `relative z-10` child is above an `absolute z-0` sibling, a negative `z-*` is below the in-flow content, and a `relative` child is above a later sibling that overlaps it (a `-mt-7` logo pulled up into a `relative` cover), in a div and in the content of a `ScrollArea` or `InfiniteScrollView` alike.
- A percent height (`min-h-full`, `h-full`) on the content of a scroll view resolves against the scroll view's height inside its padding, when the scroll view has a height of its own or fills a flex column; a `max-h-*` one is as tall as its content, so there it does not resolve. A `flex-1` child of a `min-h-*` column takes what is left of the min-height, so a footer after a `flex-1` spacer sits at the bottom of a short list.
- Auto margins take the free space: `mx-auto` centres a child of a flex column (and a block child with a width), `ml-auto` pushes a row child to the end, and `mt-auto` puts a child at the bottom of a flex column with a height. A growing (`flex-1`) child in the same line leaves them none.
- `grid-flow-col` fills each of `grid-rows-N` rows before starting the next column. With `grid-cols-N` the columns share the width; without it each column is as wide as its widest cell.
- The padding of an `overflow-*-auto` box scrolls with its content.
- A `backdrop-blur-*` box keeps the width its parent or its insets (`absolute inset-x-0`) give it.

**Visual:** background color, border (all sides), border radius, opacity, shadow (`2xs` through `2xl`, arbitrary outer shadows, and a shadow color on the same element; inset shadows are not drawn), gradient (linear: the 8 directions and `bg-linear-<angle>`, with `via-` and stop positions)

**Typography:** font size/weight/family, line height, letter spacing, text align, text color, italic, text decoration (underline/line-through), text overflow (ellipsis/clip), truncate, max lines, text transform (uppercase/lowercase/capitalize). `whitespace-*` classes have no effect on Flutter; `truncate` works through max lines and ellipsis

**Interactive:** cursor, visibility (hidden/invisible/visible), `pointer-events-none` (taps go through the element and everything in it; a `pointer-events-auto` child inside it does not take them back)

**Transform:** rotate, scale (uniform + x/y), translate (x/y), skew (x/y)

## Supported Components

The Flutter SDK renders 160+ registered component types including:

- **Layout:** div (auto Row/Column from flex direction), scroll area, separator, aspect ratio
- **Text:** text, heading, label, markdown (full rendering)
- **Input:** text field (with echo suppression), text area, checkbox, switch, slider, radio group, toggle, select. A text area always takes line breaks. One with `rows: 1` grows with its text, because Flutter has no one-line multi-line field
- **Buttons:** button, action button, icon button, form submit
- **Display:** avatar (image + fallback), image (network), progress bar
- **Disclosure:** accordion, collapsible, tabs
- **Overlays:** dialog, alert dialog, tooltip, popover, toast. A Sheet or Drawer is placed by its panel's classes, as on the web. `fixed inset-x-0 bottom-0` makes it a bottom sheet: full width, sized by its height classes (`h-[80vh]`, `max-h-[85vh]`), padded inside for the safe area, kept above the keyboard and closed by a downward drag when it has `onOpenChange`. A `flex-1 min-h-0` scroll area in it scrolls between the header and a footer that stays in view. With `snapPoints`, that scroll area scrolls at every snap point, as vaul's drawer does on the web; a pull down that starts with it at its top moves the sheet down (a pull that starts with it scrolled stops at its top and leaves the sheet), and a drag on the handle or anywhere else on the panel moves the sheet between snap points. A release settles on the next snap point in the direction of a flick, or on the nearest after a slow drag; a release below the lowest closes a sheet that has `onOpenChange`
- **Media:** charts (bar/line/pie via fl_chart)
- **Structure:** form, file upload zone, keyboard listener, sortable lists (column, sideways row, wrapping grid and `rows:` shelf, with handles and `longPressDelay:`; a `rows:` shelf spaces its columns by `gap-x` and its rows by `gap-y`, each column as wide as its widest item); a standalone `Draggable`/`Droppable` draws its content but does not drag

## CI/CD

CI builds Flutter through `platform-dart/builder.py`, which provides `build`
(`flutter pub get`), `analyze` (the SDK and the examples this guide quotes, in
`platform-dart/ikon_sdk_doc_examples/`), `api_surface`, `test`, and `full_build` targets. The Dart protocol
files under `platform-dart/ikon_sdk/lib/protocol/generated/` are regenerated by the
platform-dotnet build (the `IkonServer` project), so run that first after schema
changes.
