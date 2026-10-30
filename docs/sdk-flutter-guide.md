<!-- checked-against: 0c556b8463fbdc99e3b0c442 -->

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
description: Flutter frontend for My IKON App

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
const _ikonPort = int.fromEnvironment('IKON_PORT', defaultValue: 8443);

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
while the `--flutter-*` flags pass it for you. The `--flutter-*` flags combine, so `--flutter --flutter-ios` opens both. `--flutter-ios` and
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
task's work is saved to the app.

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
repo the ikon tool writes that override into `pubspec_overrides.yaml` for you.

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

`IkonClient.connectLocal()` needs no configuration:

<!-- ikon-example: flutter-sdk-auth-local -->
```dart
final client = await IkonClient.connectLocal(host: 'localhost', port: 8443);
```

### Deployed Apps — Guest/Anonymous

<!-- ikon-example: flutter-sdk-auth-guest -->
```dart
final client = await IkonClient.connectGuest(
  serverHost: 'myapp.ikon.ai',
  spaceId: 'my-space-id',
  authUrl: 'https://auth.ikonai.com',
);
```

### Deployed Apps — Signing In (Google, Apple, Microsoft, bank ID, email)

`IkonNativeSignIn` answers the server's `ClientFunctions.LoginAsync`, `LoginShowAsync` and
`LogoutAsync` calls on a phone, where a browser would leave for the provider and come back. OAuth
providers open in a web view that returns to the app's own https origin. The auth service returns a
token to no other kind of address, so a custom-scheme deep link never receives one. When the
provider's page cannot load because the phone has no connection, the web view says so and offers
Try again. Email sign-in uses the code the auth service mails. The token is kept in secure storage,
so the next launch connects straight in as that user. If the server no longer accepts the token, it is dropped and the
app connects as a guest. `sso:` connections and passkeys are not offered on a phone yet.

`ikon bundle --flutter-*` passes every method the app's surfaces list as `IKON_AUTH_METHODS` and, when
none admits anonymous visitors, `IKON_AUTH_REQUIRE_SIGN_IN=true`. The app template already does this; the essential parts are:

<!-- ikon-example: flutter-sdk-native-sign-in -->
```dart
final navigatorKey = GlobalKey<NavigatorState>(); // MaterialApp(navigatorKey: navigatorKey, ...)

final signIn = IkonNativeSignIn(
  serverHost: 'myapp.ikon.ai',
  spaceId: 'my-space-id',
  authUrl: 'https://auth.ikonai.com',
  methods: const String.fromEnvironment('IKON_AUTH_METHODS').split(','),
  navigatorKey: navigatorKey,
  onIdentityChanged: reconnect, // dispose the client and connect again
);

final client = await signIn.connect(); // the stored user, else a guest
signIn.register(client);               // answers login, loginShow and logout
```

An app that requires sign-in shows `IkonSignInChooser(methods: signIn.supportedMethods, canClose: false, onChosen: signIn.signInWith)`
before connecting when `signIn.storedToken()` is null.

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
address and space the code was sent for, and passes the token to `signIn.signInWithToken(token)`.
The SDK stores that token like any other and the app reconnects. It returns false when the phone's
secure storage refuses the token, so the screen can say so and let the user try again. Codes are nine
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

There is no `connect*` helper for an API key, so the client is built from the auth result:

<!-- ikon-example: flutter-sdk-auth-api-key -->
```dart
final auth = IkonAuthenticator(host: 'myapp.ikon.ai', port: 443);
final result = await auth.authenticateApiKey(
  spaceId: 'my-space-id',
  apiKey: 'my-api-key',
);

// What the connect* helpers do for you: wire reauth, connect, register the built-in client
// functions, then send the ready message without which the app never sees the client join.
// Without configureReauth a reconnect never re-authenticates once the cached tickets expire,
// and client.assetBaseUrl resolves root-relative images against the per-session socket host.
final client = IkonClient(IkonClientConfig.fromAuthResult(result));
client.configureReauth(
  authenticator: auth,
  connectUrl: result.connectUrl,
  hardReauth: () => auth.authenticateApiKey(spaceId: 'my-space-id', apiKey: 'my-api-key'),
);
await client.connect();
registerFlutterFunctions(client.functionRegistry, uploadTransport: client, feedbackSink: client);
client.sendProtocolMessage(ClientReady().toProtocolMessage(client.sessionId));
```

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
the backend asks the client to wait before trying again. A client whose session ends goes offline
with that exception in `IkonClient.lastError` instead of reconnecting into a new server. Pass `userType: UserType.machine`
with it so the connection does not count as a person in the session and the session still ends when
its people leave.

### When the server is starting or has been replaced

While the app's server is starting, a connect retries for about 15 seconds. When the server refuses
the connect token because it was issued for a server that has since been replaced, or because it has
expired, the connect stops retrying and asks `/init` for a new token once. If that one is refused
too, it throws `ServerChangedException`. The sign-in is still valid, so keep the stored token and
connect again later.

## Links and Notification Taps

On the web the browser opens the app at the address that was followed, and the app reads that path as
`Context.InitialPath`. A native app gets the same from `IkonNavigation`: every connect sends its
current entry as the session's initial path, and that entry is `/` unless something set it first.

- `IkonNavigation.instance.open(path)` sets the path (with its query, such as `/events/42?tab=info`) that the
  next connection opens at, and returns false for a path that does not start with a single `/`. Once a connection is up it navigates there instead, which the server
  raises as `Navigation.PathChangedAsync`, the same as a link tapped inside the app.
- After a dropped connection, the new session opens at the screen the app was showing, as a reloaded
  browser tab does.
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

A Flutter app reacts to a tap before the server answers, with no app code:

- Value controls (tabs, selects, radio groups, toggle groups, switches, checkboxes, sliders) show
  the picked value at once and keep it until the server has handled the tap, as on the web.
- A tap the server has not answered after 150 ms runs a thin bar along the top of
  `IkonParallaxView` until the server's update arrives (`showPendingBar`, `pendingBarColor`).
  `IkonClient.actionTracker` reports how long each tap took to reach the screen.
- A press on a control with a `path:` moves the app to that path at once: a `view.NavBar` item
  turns selected and, until the server sends the page, `view.NavOutlet` shows the page as the phone
  last showed it, without taking taps. A page the phone has not shown yet appears as a skeleton
  drawn from the deploy's boot snapshot of the route, or as placeholder bars.

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
its dialogs and its menus, and the email sign-in page does the same. iOS number pads have no key
that closes the keyboard, so without it a field in a streamed form could keep the keyboard open over
the page. Screens an app draws itself get the same behaviour when it wraps them in
`IkonKeyboardDismissal`:

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
registry.register('my-custom-widget', (node, style, children, context) {
  final data = node.props['data'] as String? ?? '';
  return MyCustomWidget(data: data);
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

## Client Functions

The server can call functions registered on the Flutter client:

<!-- ikon-example: flutter-sdk-client-function -->
```dart
client.functionRegistry.register(
  FunctionDefinition(name: 'my.customAction', resultTypeName: 'string'),
  (args) async {
    // Handle the call from the server
    return 'result';
  },
);
```

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

## File Upload

<!-- ikon-example: flutter-sdk-file-upload -->
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
// comes up with the connection unless the surface's Media is none.
final webRtcMedia = enableIkonWebRtcMedia(client);
await webRtcMedia.startCameraCapture();
```

The capture `enableIkonAudioCapture` returns can also be started and stopped by the server, with
`ClientFunctions.StartAudioCaptureAsync` and `StopCaptureAsync`, as on the web, for a microphone an
app keeps open across pages. It is one microphone: a start while it captures, or while another
start waits for the permission prompt, fails on the server and does nothing from a button.

`IkonMediaManager` combines audio capture, playback, and WebRTC media (and routes
share/clipboard actions through `IkonShare`) and can be passed to
`IkonParallaxView(mediaManager: ...)`.

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
