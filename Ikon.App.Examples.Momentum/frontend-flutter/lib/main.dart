import 'dart:async';

import 'package:flutter/material.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

import 'momentum_map.dart';

void main() {
  runApp(const IkonFlutterApp());
}

/// Connection configuration injected at build time via `--dart-define`.
///
/// In local development no identity is injected, so [isDeployed] is false and
/// the app auto-discovers the local Ikon server (the host launches the Flutter
/// frontend with `--dart-define=IKON_PORT=<port>`).
///
/// When bundled with `ikon app bundle --flutter-*`, the tool injects the
/// space identity and server host as dart-defines, so [isDeployed]
/// is true and the app connects to its deployed space.
class IkonAppConfig {
  static const String serverHost = String.fromEnvironment('IKON_SERVER_HOST', defaultValue: 'localhost');
  static const int port = int.fromEnvironment('IKON_PORT', defaultValue: 8443);
  static const String spaceId = String.fromEnvironment('IKON_SPACE_ID');
  static const String authUrl = String.fromEnvironment('IKON_AUTH_URL');

  static bool get isDeployed => spaceId.isNotEmpty;
}

class IkonFlutterApp extends StatelessWidget {
  const IkonFlutterApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Ikon App',
      debugShowCheckedModeBanner: false,
      // Momentum's page colour, not the scaffold template's slate. The Parallax view sits inside a
      // SafeArea, so whatever this is shows as bars above the status bar and below the home
      // indicator — the template's blue-black read as two stripes that belong to another app.
      theme: ThemeData.dark(useMaterial3: true).copyWith(
        scaffoldBackgroundColor: const Color(0xFF0B0B0D),
      ),
      home: const IkonAppScreen(),
    );
  }
}

class IkonAppScreen extends StatefulWidget {
  const IkonAppScreen({super.key});

  @override
  State<IkonAppScreen> createState() => _IkonAppScreenState();
}

class _IkonAppScreenState extends State<IkonAppScreen> {
  // The default registry plus this app's own map node. Built once: rebuilding it per frame would
  // throw away flutter_map's controller and the tiles under it on every UI diff.
  final IkonComponentRegistry _registry = buildMomentumRegistry();

  IkonClient? _client;
  IkonUiCore? _uiCore;
  IkonAudioPlayback? _audioPlayback;
  IkonAudioCapture? _audioCapture;
  IkonMediaManager? _mediaManager;
  IkonConnectionState _connectionState = IkonConnectionState.idle;
  String? _errorMessage;
  // Avoid flashing a spinner for quick (re)connects: only reveal it once we have
  // been unconnected for a few seconds (mirrors the web SDK's connectingSlow).
  bool _showSlowConnect = false;
  Timer? _slowConnectTimer;
  DateTime? _disconnectedSince;
  Timer? _disconnectTicker;
  Timer? _reconnectTimer;
  int _reconnectAttempt = 0;

  @override
  void initState() {
    super.initState();
    _connect();
  }

  /// Below this a drop is not worth mentioning: the socket reconnects on its own and saying anything
  /// would be more disruptive than the gap.
  static const Duration _blipGrace = Duration(seconds: 3);

  void _manageSlowConnect() {
    if (_connectionState == IkonConnectionState.connected) {
      _slowConnectTimer?.cancel();
      _slowConnectTimer = null;
      _disconnectTicker?.cancel();
      _disconnectTicker = null;
      _disconnectedSince = null;

      if (_showSlowConnect && mounted) {
        setState(() => _showSlowConnect = false);
      }

      return;
    }

    _disconnectedSince ??= DateTime.now();

    // Repaint once a second while disconnected so the overlay can appear when the grace period
    // lapses; nothing else drives a frame once the connection carrying the UI has gone.
    _disconnectTicker ??= Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) {
        setState(() {});
      }
    });

    if (_slowConnectTimer == null && !_showSlowConnect) {
      _slowConnectTimer = Timer(const Duration(seconds: 5), () {
        if (mounted) setState(() => _showSlowConnect = true);
      });
    }
  }

  /// How long the connection has been away, or null while it is up or still inside the grace period.
  Duration? get _outageBeyondGrace {
    final since = _disconnectedSince;

    if (since == null || _connectionState == IkonConnectionState.connected) {
      return null;
    }

    final elapsed = DateTime.now().difference(since);
    return elapsed < _blipGrace ? null : elapsed;
  }

  Future<void> _connect() async {
    setState(() {
      _connectionState = IkonConnectionState.connecting;
      _errorMessage = null;
    });
    _manageSlowConnect();

    // A deployed cloud cell may be cold on the first connect and take a while to spin up, so retry
    // a few times with backoff before surfacing an error.
    const maxAttempts = 4;
    for (var attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        final client = IkonAppConfig.isDeployed
            ? await IkonClient.connectGuest(
                serverHost: IkonAppConfig.serverHost,
                spaceId: IkonAppConfig.spaceId,
                authUrl: IkonAppConfig.authUrl,
              )
            : await IkonClient.connectLocal(
                host: IkonAppConfig.serverHost,
                port: IkonAppConfig.port,
              );

        client.onStateChange.listen((state) {
          if (mounted) {
            setState(() => _connectionState = state);
            _manageSlowConnect();
          }

          // The retry loop around this method only covers the FIRST connect. A session that goes away
          // later — the app being redeployed, the server cycling, a long tunnel — used to leave the
          // phone holding a dead socket with a UI that still drew but answered nothing, and only
          // force-quitting the app recovered it. That is the worst possible failure mid-outing, so a
          // dropped connection now reconnects on its own and the server picks the recording back up.
          if (state == IkonConnectionState.offline || state == IkonConnectionState.offlineError) {
            _scheduleReconnect();
          }
        });

        _client = client;
        _uiCore = IkonUiCore(client);
        _audioPlayback = enableIkonAudioPlayback(client, backgroundAudio: true);
        _audioCapture = enableIkonAudioCapture(client);
        _mediaManager = IkonMediaManager(audioPlayback: _audioPlayback, audioCapture: _audioCapture);

        if (mounted) {
          setState(() => _connectionState = IkonConnectionState.connected);
        }
        return;
      } catch (e) {
        if (attempt >= maxAttempts) {
          if (mounted) {
            setState(() {
              _connectionState = IkonConnectionState.offlineError;
              _errorMessage = e.toString();
            });
          }
          return;
        }
        await Future<void>.delayed(Duration(seconds: attempt * 2));
      }
    }
  }

  /// Reconnects after a drop, backing off so a server that is still restarting is not hammered.
  void _scheduleReconnect() {
    if (_reconnectTimer?.isActive ?? false) {
      return;
    }

    _reconnectAttempt = (_reconnectAttempt + 1).clamp(1, 6);
    final delay = Duration(seconds: 1 << (_reconnectAttempt - 1));

    _reconnectTimer = Timer(delay, () async {
      if (!mounted || _connectionState == IkonConnectionState.connected) {
        return;
      }

      try {
        _client?.dispose();
      } catch (_) {
        // Already gone; the point is only to release the dead socket before dialling again.
      }

      _client = null;
      _uiCore = null;
      await _connect();

      if (_connectionState == IkonConnectionState.connected) {
        _reconnectAttempt = 0;
      } else {
        _scheduleReconnect();
      }
    });
  }

  @override
  void dispose() {
    _reconnectTimer?.cancel();
    _disconnectTicker?.cancel();
    _slowConnectTimer?.cancel();
    _audioCapture?.dispose();
    _audioPlayback?.dispose();
    _uiCore?.dispose();
    _client?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    // Nothing has ever connected: there is no UI to keep, so this is the only case that owns the
    // whole screen.
    if (_uiCore == null) {
      return Scaffold(
        body: Center(
          child: _showSlowConnect || _errorMessage != null
              ? _ConnectingPanel(message: _errorMessage, onRetry: _connect)
              : const SizedBox.shrink(),
        ),
      );
    }

    // Once there is a UI, it stays on screen through every outage. A rider mid-outing must not lose
    // sight of their numbers because the socket blinked, and the recording is on the server anyway —
    // the phone losing touch with it changes nothing about what is being recorded.
    final outage = _outageBeyondGrace;

    return Scaffold(
      body: SafeArea(
        child: Stack(
          children: [
            IkonParallaxView(
              uiCore: _uiCore!,
              client: _client,
              mediaManager: _mediaManager,
              registry: _registry,
            ),
            if (outage != null) _OutageScrim(state: _connectionState, elapsed: outage),
          ],
        ),
      ),
    );
  }
}

/// Shown only before anything has ever connected, and only once the wait is long enough to be worth
/// a word.
class _ConnectingPanel extends StatelessWidget {
  const _ConnectingPanel({this.message, required this.onRetry});

  final String? message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const CircularProgressIndicator(strokeWidth: 2),
          const SizedBox(height: 20),
          Text(
            message == null ? 'Connecting…' : 'Could not connect',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(color: const Color(0xFFF7F7F7)),
          ),
          if (message != null) ...[
            const SizedBox(height: 8),
            Text(message!, textAlign: TextAlign.center, style: const TextStyle(color: Color(0xFF9EA2A9), fontSize: 13)),
            const SizedBox(height: 20),
            TextButton(onPressed: onRetry, child: const Text('Try again')),
          ],
        ],
      ),
    );
  }
}

/// The scrim over a live UI while the connection is away.
///
/// Three tiers, and the first is the one that matters most: a drop shorter than the grace period is
/// never mentioned at all, because the socket recovers on its own and an alert would be more
/// disruptive than the gap. Past that the UI dims and stops accepting input — a button that cannot
/// reach the server is worse than a button that says it cannot, since a tap that silently does
/// nothing reads as the app being broken.
class _OutageScrim extends StatelessWidget {
  const _OutageScrim({required this.state, required this.elapsed});

  final IkonConnectionState state;
  final Duration elapsed;

  @override
  Widget build(BuildContext context) {
    final connecting = state == IkonConnectionState.connecting;
    final label = connecting ? 'Connecting…' : 'Waiting for the network';

    return Positioned.fill(
      child: AbsorbPointer(
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          color: const Color(0xFF0B0B0D).withValues(alpha: 0.72),
          child: Center(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
              decoration: BoxDecoration(
                color: const Color(0xFF1C1C1F),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: Colors.white.withValues(alpha: 0.08)),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFFE62E7D)),
                  ),
                  const SizedBox(height: 14),
                  Text(label, style: const TextStyle(color: Color(0xFFF7F7F7), fontSize: 15, fontWeight: FontWeight.w600)),
                  const SizedBox(height: 6),
                  const Text(
                    'Your outing keeps recording — it runs on the server, not on this phone.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Color(0xFF9EA2A9), fontSize: 12.5, height: 1.4),
                  ),
                  if (elapsed.inSeconds >= 15) ...[
                    const SizedBox(height: 10),
                    Text(
                      'Away for ${elapsed.inSeconds}s',
                      style: const TextStyle(color: Color(0xFF61656C), fontSize: 11),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
