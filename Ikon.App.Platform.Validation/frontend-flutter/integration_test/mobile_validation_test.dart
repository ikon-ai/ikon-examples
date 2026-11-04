// IKON Mobile Validation: the Validation app's Flutter frontend driven on a phone, an emulator or a
// simulator, against a local run of the app. `validation/validate.py mobile` runs it. Each check
// prints one `IKON-MOBILE` line, and the test fails at the end if any check failed.
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ikon_app_platform_validation/main.dart' as app;
import 'package:ikon_sdk/ikon_sdk.dart';
import 'package:integration_test/integration_test.dart';

import 'mobile/driver.dart';

const List<(String, String)> tabs = [
  ('typography', 'Typography'),
  ('icons', 'Icons'),
  ('buttons', 'Buttons'),
  ('inputs', 'Inputs'),
  ('forms', 'Forms'),
  ('cards', 'Cards'),
  ('layout', 'Layout'),
  ('overlays', 'Overlays'),
  ('navigation', 'Navigation'),
  ('nav-menu', 'Nav Menu'),
  ('app-nav', 'App Nav'),
  ('advanced-inputs', 'Advanced Inputs'),
  ('drag-drop', 'Drag & Drop'),
  ('pan-zoom', 'Pan & Zoom'),
  ('crosswind', 'Crosswind'),
  ('brand', 'Brand'),
  ('charts', 'Charts'),
  ('files', 'Files'),
  ('assets', 'Assets'),
  ('actions', 'Actions'),
  ('notifications', 'Notifications'),
  ('video', 'Video'),
  ('audio', 'Audio'),
  ('shadertoy', 'Shadertoy'),
  ('ikon-ai', 'Ikon.AI Library'),
  ('mcp', 'MCP'),
  ('cells', 'Cells'),
  ('cron', 'Cron'),
  ('virtualization', 'Virtualization'),
  ('drawing', 'Drawing'),
  ('profiling', 'Profiling'),
  ('memory', 'Memory'),
  ('session-identity', 'Session Identity'),
  ('account', 'Account'),
  ('react-sdk', 'React SDK'),
  ('consent', 'Consent'),
  ('payments', 'Payments'),
  ('email', 'Email'),
  ('costs', 'Costs'),
  ('custom-messages', 'Custom Messages'),
  ('database', 'Database'),
  ('persistent-state', 'Persistent State'),
  ('reactive-scopes', 'Reactive Scopes'),
  ('self-test', 'Self-Test'),
  ('device', 'Device'),
  ('telephony', 'Telephony'),
  ('signatures', 'Signatures'),
  ('sharepoint', 'SharePoint'),
  ('google-drive', 'Google Drive'),
  ('hot-reload', 'Hot Reload'),
];

const List<String> mcpChecks = [
  'list', 'ping', 'echo-refused', 'echo', 'whoami-refused', 'whoami', 'scope-refused', 'scope', 'resource', 'progress', 'cancel',
];

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('IKON Mobile Validation', (tester) async {
    final d = MobileDriver(tester);
    d.captureFlutterErrors();

    // Start always runs, since every other phase needs the app connected
    const only = String.fromEnvironment('IKON_MOBILE_PHASES');
    bool runs(String phase) => only.isEmpty || only.split(',').contains(phase);

    try {
      await _start(d);

      if (runs('tabs')) await _tabs(d);
      if (runs('app')) await _appChecks(d);
      if (runs('device')) await _device(d);
      if (runs('media')) await _media(d);
      if (runs('lifecycle')) await _lifecycle(d);
    } finally {
      d.restoreFlutterErrors();
    }

    expect(d.failures, isEmpty, reason: d.failures.join('\n'));
  }, timeout: const Timeout(Duration(minutes: 40)));
}

Future<void> _start(MobileDriver d) async {
  await d.check('start', 'connect', () async {
    final started = DateTime.now();
    app.main();
    await d.waitFor(find.text('Typography'), timeout: const Duration(seconds: 120), what: 'the first page');
    return 'first page after ${DateTime.now().difference(started).inMilliseconds} ms on ${Platform.operatingSystem}';
  });

  if (d.failures.isNotEmpty) {
    throw CheckFailure('the app never showed its first page, so nothing else can run');
  }

  await d.check('start', 'start-up-screen-kept', () async {
    await d.pumpFor(const Duration(seconds: 3));
    final kept = await IkonLiveSnapshot.load(app.IkonAppConfig.snapshotKey);

    if (kept == null) {
      throw CheckFailure('no start-up screen was kept for the next launch');
    }

    return 'kept for ${app.IkonAppConfig.snapshotKey}';
  });
}

Future<void> _tabs(MobileDriver d) async {
  for (final (route, label) in tabs) {
    await d.check('tabs', route, () async {
      await d.openTab(label, route, isDefault: route == tabs.first.$1);
      final unexpected = d.fallbackTypesOnScreen().difference(expectedFallbackTypes);

      if (unexpected.isNotEmpty) {
        throw CheckFailure('no Flutter renderer for ${unexpected.join(', ')}');
      }

      return 'rendered';
    });
  }
}

Future<void> _appChecks(MobileDriver d) async {
  await d.check('app', 'theme-pair', () async {
    await d.openTab('Typography', 'typography', isDefault: true);
    return d.clickExpect('theme-pair-run', 'theme-pair-result', RegExp(r'^PASS'));
  });

  await d.check('app', 'press-hold', () async {
    await d.openTab('Buttons', 'buttons');
    var pressed = '';
    await d.hold(d.byId('press-hold-button'), const Duration(milliseconds: 900), whileHeld: () async {
      pressed = await d.expectLine('press-hold-state', RegExp(r'^Pressed'), timeout: const Duration(seconds: 5));
    });
    final released = await d.expectLine('press-hold-state', RegExp(r'^Released after'));
    return '$pressed, then $released';
  });

  await d.check('app', 'composer', () async {
    await d.openTab('Inputs', 'inputs');
    final field = find.descendant(of: d.byId('composer-root'), matching: find.byType(EditableText));
    await d.waitFor(field, what: 'the composer text field');
    final token = 'mobile-${DateTime.now().millisecondsSinceEpoch}';
    await d.tester.ensureVisible(field.first);
    await d.tester.tap(field.first, warnIfMissed: false);
    await d.tester.enterText(field.first, token);
    await d.tester.testTextInput.receiveAction(TextInputAction.send);
    await d.pumpFor(const Duration(seconds: 1));

    if (!d.textOf('composer-echo').contains(token)) {
      await d.tester.sendKeyEvent(LogicalKeyboardKey.enter);
    }

    return d.expectLine('composer-echo', RegExp('^Echo: ${RegExp.escape(token)}'));
  });

  await d.check('app', 'feature-id', () async {
    await d.openTab('Forms', 'forms');
    return d.expectLine('feature-id-check', RegExp(r'PASS invalid id rejected'));
  });

  await d.check('app', 'footer-dialog', () async {
    await d.openTab('Overlays', 'overlays');
    await d.tapId('footer-dialog-open');
    await d.tapId('footer-dialog-archive');
    return d.expectLine('footer-dialog-result', RegExp(r'^Footer: Archive$'));
  });

  await d.check('app', 'step-dialog', () async {
    await d.openTab('Overlays', 'overlays');
    await d.tapId('step-dialog-open');

    for (final step in ['Next', 'Back', 'Next', 'Next', 'Done']) {
      await d.tapText(step);
      await d.pumpFor(const Duration(milliseconds: 400));
    }

    return d.expectLine('step-dialog-result', RegExp(r'^PASS step dialog finished after 3 steps$'));
  });

  await d.check('app', 'app-nav', () async {
    await d.openTab('App Nav', 'app-nav');
    await d.tapId('app-nav-item-slow');
    final slow = await d.expectLine('app-nav-result', RegExp(r'^PASS page slow arrived'));
    await d.tapId('app-nav-button-inbox');
    await d.expectLine('app-nav-result', RegExp(r'^PASS page inbox arrived'));
    return slow;
  });

  await d.check('app', 'failing-action', () async {
    await d.openTab('Actions', 'actions');
    return d.clickExpect('action-fail-trigger', 'action-fail-result', RegExp(r'^PASS: Action failed'));
  });

  for (final (button, pattern) in [
    ('Set Theme (dark)', RegExp(r'SetTheme: Success \(dark\)')),
    ('Copy Text', RegExp(r'Copy: Success')),
    ('Get Language', RegExp(r'Language: .+')),
    ('Get Timezone', RegExp(r'Timezone: .+')),
    ('Get Visibility', RegExp(r'Visibility: Visible')),
  ]) {
    await d.check('app', 'client-${button.toLowerCase().replaceAll(RegExp(r'[^a-z]+'), '-')}', () async {
      await d.openTab('Actions', 'actions');
      await d.tapText(button);
      await d.waitFor(find.textContaining(pattern), what: 'a result matching ${pattern.pattern}');
      return 'answered';
    });
  }

  await d.check('app', 'notifications-inbox', () async {
    await d.openTab('Notifications', 'notifications');
    return d.clickExpect('notif-inbox-selftest-run', 'notif-inbox-selftest', RegExp(r'^PASS inbox:'));
  });

  await d.check('app', 'consent', () async {
    await d.openTab('Consent', 'consent');
    return d.clickExpect('consent-selfcheck-run', 'consent-selfcheck-result', RegExp(r'^PASS'));
  });

  await d.check('app', 'mcp', () async {
    await d.openTab('MCP', 'mcp');
    await d.tapId('mcp-checks-run');
    final failed = <String>[];

    for (final key in mcpChecks) {
      final line = await d.expectLine('mcp-check-$key', RegExp(r'^(PASS|FAIL)'), timeout: const Duration(seconds: 90))
          .catchError((Object e) => '$e');

      if (!line.startsWith('PASS')) {
        failed.add('$key: $line');
      }
    }

    if (failed.isNotEmpty) {
      throw CheckFailure(failed.join('; '));
    }

    return '${mcpChecks.length} checks passed';
  });

  await d.check('app', 'client-scope', () async {
    await d.openTab('Reactive Scopes', 'reactive-scopes');
    final before = int.tryParse(d.textOf('scope-client-value')) ?? 0;
    return d.clickExpect('scope-client-inc', 'scope-client-value', RegExp('^${before + 1}\$'));
  });

  await d.check('app', 'self-test', () async {
    await d.openTab('Self-Test', 'self-test');
    return d.expectLine('selftest-summary', RegExp(r'^Self-test: \d+ passed, 0 failed'), timeout: const Duration(minutes: 3));
  });
}

Future<void> _device(MobileDriver d) async {
  await d.check('device', 'capabilities', () async {
    await d.openTab('Device', 'device');
    return d.clickExpect('dev-capabilities-probe', 'dev-capabilities-probe-result', RegExp(r'^PASS probe:'));
  });

  await d.check('device', 'location-one-shot', () async {
    await d.openTab('Device', 'device');
    return d.clickExpect('dev-location-oneshot-run', 'dev-location-oneshot', RegExp(r'^PASS one-shot'),
        timeout: const Duration(seconds: 90));
  });

  await d.check('device', 'location-tracking', () async {
    await d.openTab('Device', 'device');
    await d.clickExpect('dev-location-start', 'dev-location-tracking', RegExp(r'^Tracking: started \(true\)'));
    final fix = await d.expectLine('dev-location-last', RegExp(r'^PASS fix #'), timeout: const Duration(seconds: 90));
    await d.clickExpect('dev-location-stop', 'dev-location-tracking', RegExp(r'^Tracking: stopped \(true\)'));
    return fix;
  });

  await d.check('device', 'motion', () async {
    await d.openTab('Device', 'device');
    final started = await d.clickExpect('dev-motion-start', 'dev-motion-status', RegExp(r'^Motion start: streaming \(true\)'));
    await d.pumpFor(const Duration(seconds: 3));
    await d.clickExpect('dev-motion-stop', 'dev-motion-status', RegExp(r'^Motion stop: stopped \(true\)'));
    return started;
  });

  await d.check('device', 'recording', () async {
    await d.openTab('Device', 'device');
    final started = await d.clickExpect('dev-recording-start', 'dev-recording-status', RegExp(r'^Recording start: recording .* \(true\)'));
    await d.pumpFor(const Duration(seconds: 3));
    await d.tapId('dev-recording-stop');
    await d.pumpFor(const Duration(seconds: 3));
    return started;
  });

  await d.check('device', 'recording-codec', () async {
    await d.openTab('Device', 'device');
    return d.clickExpect('dev-recording-codec-run', 'dev-recording-codec', RegExp(r'^PASS codec:'));
  });
}

int _counter(MobileDriver d, String testId) => int.tryParse(d.textOf(testId).trim()) ?? -1;

Future<String> _holdToRecord(MobileDriver d) async {
  await d.openTab('Audio', 'audio');
  final before = _counter(d, 'media-audio-from');
  var recording = false;
  var capturing = false;
  var serverLine = '';
  await d.hold(find.text('Hold to Record', skipOffstage: false), const Duration(milliseconds: 2500), whileHeld: () async {
    recording = find.textContaining('hold: recording').evaluate().isNotEmpty;
    capturing = d.mediaManager?.audioCapture?.isCapturing ?? false;
    serverLine = find.textContaining('Server observed').evaluate().isEmpty
        ? '(not on screen)'
        : (find.textContaining('Server observed').evaluate().first.widget as Text).data ?? '';
  });
  await d.pumpFor(const Duration(seconds: 2));
  final after = _counter(d, 'media-audio-from');

  if (!recording) {
    throw CheckFailure('the server never saw the held press as recording; mid-press the microphone was '
        '${capturing ? '' : 'not '}capturing and the server line read "$serverLine"');
  }

  if (after <= before) {
    throw CheckFailure('no captured audio frames reached the app ($before before, $after after)');
  }

  // A microphone left open after the finger lifts keeps sending, which a person only notices when
  // the other side hears them
  if (d.mediaManager?.audioCapture?.isCapturing ?? false) {
    throw CheckFailure('the microphone was still capturing 2 s after the press was released');
  }

  if (find.textContaining('hold: idle').evaluate().isEmpty) {
    throw CheckFailure('the server still saw the press as recording 2 s after it was released');
  }

  return '${after - before} frames reached the app, and the microphone stopped on release';
}

Future<void> _media(MobileDriver d) async {
  await d.check('media', 'webrtc-peer', () async {
    final media = d.mediaManager?.webRtcMedia;

    if (media == null) {
      throw CheckSkipped('this build has no WebRTC media (IKON_WEBRTC_MEDIA is not set)');
    }

    final deadline = DateTime.now().add(const Duration(seconds: 45));

    while (media.state != WebRtcConnectionState.connected) {
      if (DateTime.now().isAfter(deadline)) {
        throw CheckFailure('the WebRTC peer never connected (last state ${media.state.name})');
      }

      await d.pumpFor(const Duration(milliseconds: 500));
    }

    return 'connected';
  });

  await d.check('media', 'app-audio', () async {
    await d.openTab('Audio', 'audio');
    await d.tapText('Add Sine Wave');

    try {
      final media = d.mediaManager;
      final played = media?.audioPlayback?.framesPlayed ?? 0;
      final webRtc = media?.webRtcMedia?.state == WebRtcConnectionState.connected;
      await d.pumpFor(const Duration(seconds: 4));
      final delta = (media?.audioPlayback?.framesPlayed ?? 0) - played;

      if (delta > 0) {
        return '$delta frames played over the protocol channel';
      }

      if (webRtc) {
        return 'playing over the WebRTC peer';
      }

      throw CheckFailure('no app audio reached the client');
    } finally {
      await _removeSineWaves(d);
    }
  });

  await d.check('media', 'hold-to-record', () => _holdToRecord(d));
}

Future<void> _removeSineWaves(MobileDriver d) async {
  for (var i = 0; i < 5 && find.textContaining(RegExp(r'^Sine Wave \([1-9]')).evaluate().isNotEmpty; i++) {
    await d.tapText('Remove');
    await d.pumpFor(const Duration(milliseconds: 500));
  }
}

Future<void> _lifecycle(MobileDriver d) async {
  await d.check('lifecycle', 'background', () async {
    final session = d.parallaxView?.client?.sessionId;
    await d.hostFault('background', args: {'seconds': '20'});
    final recovery = await _recover(d, session, whenPageIsBack: () => _holdToRecord(d));
    return 'back after 20 s; $recovery';
  });

  await d.check('lifecycle', 'network-off', () async {
    final session = d.parallaxView?.client?.sessionId;
    await d.hostFault('network-off', args: {'seconds': '15'});
    final recovery = await _recover(d, session);
    return 'network back after 15 s; $recovery; ${await _holdToRecord(d)}';
  });
}

/// Waits for the app's page and, when the build carries media over WebRTC, for its peer, and says
/// how long each took once the fault had ended. The server keeps a session for minutes after its
/// client goes quiet, so a short outage must end in the session the app had, with its state and its
/// streams: a new one starts the person over. [whenPageIsBack] runs as soon as the page is, before
/// the peer has returned, as a person pressing the microphone on reopening the app would.
Future<String> _recover(MobileDriver d, int? sessionBefore, {Future<String> Function()? whenPageIsBack}) async {
  final started = DateTime.now();
  await d.waitForPage();
  final sessionAfter = d.parallaxView?.client?.sessionId;

  if (sessionBefore != null && sessionAfter != sessionBefore) {
    throw CheckFailure('the app came back in a new session ($sessionBefore → $sessionAfter) instead of the one it had');
  }

  final pageAfter = DateTime.now().difference(started).inSeconds;
  final early = whenPageIsBack == null ? '' : '; at once: ${await whenPageIsBack()}';
  final media = d.mediaManager?.webRtcMedia;

  if (media == null) {
    return 'page after ${pageAfter}s$early';
  }

  final deadline = started.add(const Duration(seconds: 90));

  while (media.state != WebRtcConnectionState.connected) {
    if (DateTime.now().isAfter(deadline)) {
      throw CheckFailure('the WebRTC peer did not come back within 90 s (last state ${media.state.name})');
    }

    await d.pumpFor(const Duration(seconds: 1));
  }

  return 'page after ${pageAfter}s$early; WebRTC peer after ${DateTime.now().difference(started).inSeconds}s';
}
