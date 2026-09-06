import 'dart:async';

import 'package:flutter/material.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

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

  /// Local dev only: the developer's backend user id — the Flutter counterpart of the web
  /// dev server's auto-login; without it a sign-in-gated app enters as a synthetic user and
  /// never gets past its gate. Injected at launch; ignored when deployed.
  static const String devUserId = String.fromEnvironment('IKON_DEV_USER_ID');

  static bool get isDeployed => spaceId.isNotEmpty;
}

class IkonFlutterApp extends StatelessWidget {
  const IkonFlutterApp({super.key});

  @override
  Widget build(BuildContext context) {
    // The app's own appearance comes from the Ikon theme it streams, so the shell must not
    // impose one: a light-themed app forced onto a dark Material theme renders white-on-white
    // wherever the streamed styles do not paint a colour themselves. Following the platform
    // keeps Material's own chrome (text selection, scrollbars, dialogs) legible either way,
    // and leaving scaffoldBackgroundColor unset lets the streamed page background paint.
    return MaterialApp(
      title: 'Ikon App',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(brightness: Brightness.light, useMaterial3: true),
      darkTheme: ThemeData(brightness: Brightness.dark, useMaterial3: true),
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

  @override
  void initState() {
    super.initState();
    _connect();
  }

  void _manageSlowConnect() {
    if (_connectionState == IkonConnectionState.connected) {
      _slowConnectTimer?.cancel();
      _slowConnectTimer = null;
      if (_showSlowConnect && mounted) {
        setState(() => _showSlowConnect = false);
      }
    } else if (_slowConnectTimer == null && !_showSlowConnect) {
      _slowConnectTimer = Timer(const Duration(seconds: 5), () {
        if (mounted) setState(() => _showSlowConnect = true);
      });
    }
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
                userId: IkonAppConfig.devUserId.isNotEmpty ? IkonAppConfig.devUserId : null,
              );

        client.onStateChange.listen((state) {
          if (mounted) {
            setState(() => _connectionState = state);
            _manageSlowConnect();
          }
        });

        _client = client;
        _uiCore = IkonUiCore(client);
        _audioPlayback = enableIkonAudioPlayback(client);
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

  @override
  void dispose() {
    _slowConnectTimer?.cancel();
    _audioCapture?.dispose();
    _audioPlayback?.dispose();
    _uiCore?.dispose();
    _client?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_errorMessage != null) {
      return Scaffold(
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 48, color: Colors.red),
                const SizedBox(height: 16),
                Text('Connection error', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                Text(_errorMessage!, style: Theme.of(context).textTheme.bodyMedium, textAlign: TextAlign.center),
                const SizedBox(height: 24),
                ElevatedButton(
                  onPressed: _connect,
                  child: const Text('Retry'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (_connectionState != IkonConnectionState.connected || _uiCore == null) {
      // Hold a plain background until the connect is clearly slow, so brief
      // (re)connects don't flash a spinner.
      if (!_showSlowConnect) {
        return const Scaffold();
      }
      return Scaffold(
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const CircularProgressIndicator(),
              const SizedBox(height: 16),
              Text(
                'Connecting to server...',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: Colors.white70),
              ),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      body: SafeArea(
        child: IkonParallaxView(
          uiCore: _uiCore!,
          client: _client,
          mediaManager: _mediaManager,
        ),
      ),
    );
  }
}
