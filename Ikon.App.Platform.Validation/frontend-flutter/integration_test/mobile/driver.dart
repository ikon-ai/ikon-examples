import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ikon_sdk/ikon_sdk.dart';

/// Where the host that runs the battery listens for fault requests, as `http://host:port`. Empty
/// when nothing controls the device from outside, and the checks that need it are skipped.
const String hostControlUrl = String.fromEnvironment('IKON_MOBILE_CONTROL');

/// The node types the Flutter renderer leaves to a fallback on purpose
/// (`platform-dotnet/Ikon.Parallax.Test/unsupported-node-types.txt`), and the Validation app's own
/// React components, which its Flutter frontend does not register.
const Set<String> expectedFallbackTypes = {
  'std.audio-url-player',
  'std.video-url-player',
  'std.image-editor-canvas',
  'std.shadertoy-canvas',
  'std.coachmark',
  'std.sign-in-button',
  'std.email-code-sign-in',
  'std.phone-sign-in',
};

class CheckFailure implements Exception {
  final String message;

  CheckFailure(this.message);

  @override
  String toString() => message;
}

class CheckSkipped implements Exception {
  final String reason;

  CheckSkipped(this.reason);

  @override
  String toString() => reason;
}

/// Drives the Validation app from inside it, the way a person on the phone would, and reports each
/// check as one `IKON-MOBILE <phase> <check> PASS|FAIL|SKIP <detail>` line for the host to read.
class MobileDriver {
  final WidgetTester tester;
  final List<String> failures = [];
  final List<FlutterErrorDetails> _flutterErrors = [];
  FlutterExceptionHandler? _previousOnError;

  MobileDriver(this.tester);

  /// Collects Flutter errors (exceptions, layout overflows) instead of letting the first one end the
  /// run, so each check reports the errors that happened during it.
  void captureFlutterErrors() {
    _previousOnError = FlutterError.onError;
    FlutterError.onError = _flutterErrors.add;
  }

  void restoreFlutterErrors() {
    FlutterError.onError = _previousOnError;
  }

  List<FlutterErrorDetails> takeFlutterErrors() {
    final errors = List.of(_flutterErrors);
    _flutterErrors.clear();
    return errors;
  }

  static String _firstLine(FlutterErrorDetails error) => error.exceptionAsString().split('\n').first;

  Future<void> check(String phase, String name, Future<String> Function() body) async {
    final started = DateTime.now();
    takeFlutterErrors();

    try {
      final detail = await body();
      final errors = takeFlutterErrors();
      // An overflow is drawn as a striped edge on a page that still works, so it is reported without
      // failing the check; any other error means something did not render or did not run
      bool isOverflow(FlutterErrorDetails e) => _firstLine(e).contains(' overflowed by ');
      final overflows = errors.where(isOverflow).map(_firstLine).toSet();
      final broken = errors.where((e) => !isOverflow(e)).toList();

      if (broken.isNotEmpty) {
        // The first error in full, with the widget it came from and its stack, for whoever reads the log
        // ignore: avoid_print
        print(broken.first.toString().split('\n').take(60).join('\n'));
        throw CheckFailure('Flutter reported ${broken.length} error(s): ${broken.map(_firstLine).toSet().take(3).join(' | ')}');
      }

      if (overflows.isNotEmpty) {
        _report(phase, name, 'WARN', '$detail; ${overflows.length} layout overflow(s): ${overflows.take(3).join(' | ')}', started);
        return;
      }

      _report(phase, name, 'PASS', detail, started);
    } on CheckSkipped catch (skip) {
      _report(phase, name, 'SKIP', skip.reason, started);
    } catch (error) {
      failures.add('$phase/$name: $error');
      _report(phase, name, 'FAIL', '$error', started);
    }
  }

  void _report(String phase, String name, String verdict, String detail, DateTime started) {
    final ms = DateTime.now().difference(started).inMilliseconds;
    // One line per check, the format validation/validate_mobile.py parses
    // ignore: avoid_print
    print('IKON-MOBILE $phase $name $verdict ${ms}ms ${detail.replaceAll('\n', ' ')}');
  }

  /// Real time passes on a device, so this both waits and lets frames and messages through.
  Future<void> pumpFor(Duration duration) async {
    final end = DateTime.now().add(duration);

    while (DateTime.now().isBefore(end)) {
      await tester.pump(const Duration(milliseconds: 100));
    }
  }

  Future<void> waitFor(Finder finder, {Duration timeout = const Duration(seconds: 30), String? what}) async {
    final deadline = DateTime.now().add(timeout);

    while (finder.evaluate().isEmpty) {
      if (DateTime.now().isAfter(deadline)) {
        throw CheckFailure('Timed out after ${timeout.inSeconds}s waiting for ${what ?? finder.describeMatch(Plurality.one)}');
      }

      await tester.pump(const Duration(milliseconds: 100));
    }
  }

  Finder byId(String testId) => find.bySemanticsIdentifier(testId);

  /// The text a node with this test id shows, all of its runs joined.
  String textOf(String testId) {
    final node = byId(testId);

    if (node.evaluate().isEmpty) {
      return '';
    }

    final runs = tester.widgetList<RichText>(find.descendant(of: node.first, matching: find.byType(RichText)));
    return runs.map((r) => r.text.toPlainText()).join(' ').trim();
  }

  Future<void> tapId(String testId) async {
    await waitFor(byId(testId), what: 'the control "$testId"');
    await tester.ensureVisible(byId(testId).first);
    await tester.pump(const Duration(milliseconds: 200));
    await tester.tap(byId(testId).first, warnIfMissed: false);
    await tester.pump(const Duration(milliseconds: 100));
  }

  Future<void> tapText(String text) async {
    final finder = find.text(text);
    await waitFor(finder, what: 'the text "$text"');
    await tester.ensureVisible(finder.first);
    await tester.pump(const Duration(milliseconds: 200));
    await tester.tap(finder.first, warnIfMissed: false);
    await tester.pump(const Duration(milliseconds: 100));
  }

  /// Scrolls the page down until [finder] is built: a scroll view builds only what is near the
  /// screen, so a control further down a tab does not exist until the page reaches it.
  Future<void> reveal(Finder finder, {String? what}) async {
    final scrollables = find.byWidgetPredicate((w) => w is Scrollable && w.axisDirection == AxisDirection.down);

    for (var i = 0; i < 40 && finder.evaluate().isEmpty; i++) {
      if (scrollables.evaluate().isEmpty) {
        break;
      }

      await tester.drag(scrollables.first, const Offset(0, -400), warnIfMissed: false);
      await tester.pump(const Duration(milliseconds: 200));
    }

    await waitFor(finder, what: what);
  }

  /// Presses and holds a control like a finger, for push-to-talk and long-press checks.
  Future<void> hold(Finder finder, Duration duration, {Future<void> Function()? whileHeld}) async {
    await reveal(finder);
    await tester.ensureVisible(finder.first);
    await tester.pump(const Duration(milliseconds: 200));
    final gesture = await tester.startGesture(tester.getCenter(finder.first));

    try {
      await pumpFor(duration);

      if (whileHeld != null) {
        await whileHeld();
      }
    } finally {
      await gesture.up();
      await tester.pump(const Duration(milliseconds: 100));
    }
  }

  /// Waits for a result line to match, and fails at once when it reads as a failure, as the web
  /// battery's `_expect_line` does.
  Future<String> expectLine(String testId, RegExp pattern, {Duration timeout = const Duration(seconds: 45)}) async {
    final failed = RegExp(r'^(FAIL|Error:)');
    final deadline = DateTime.now().add(timeout);
    var text = '';

    while (true) {
      text = textOf(testId);

      if (pattern.hasMatch(text)) {
        return text;
      }

      if (failed.hasMatch(text)) {
        throw CheckFailure('"$testId" reads "$text"');
      }

      if (DateTime.now().isAfter(deadline)) {
        throw CheckFailure('Timed out after ${timeout.inSeconds}s waiting for "$testId" to match ${pattern.pattern}; it reads "$text"');
      }

      await tester.pump(const Duration(milliseconds: 200));
    }
  }

  Future<String> clickExpect(String buttonId, String resultId, RegExp pattern, {Duration timeout = const Duration(seconds: 45)}) async {
    await tapId(buttonId);
    return expectLine(resultId, pattern, timeout: timeout);
  }

  /// Opens a tab through the nav, as a person does: scrolls the page back to the nav first. The
  /// default tab is shown at `/` until another one has been opened.
  Future<void> openTab(String label, String route, {bool isDefault = false}) async {
    final tab = find.text(label);
    await waitFor(tab, what: 'the "$label" tab');
    await tester.ensureVisible(tab.first);
    await tester.pump(const Duration(milliseconds: 200));
    await tester.tap(tab.first, warnIfMissed: false);

    final deadline = DateTime.now().add(const Duration(seconds: 20));

    bool arrived() {
      final path = IkonNavigation.instance.currentUrl.split('?').first;
      return path == '/$route' || (isDefault && path == '/');
    }

    while (!arrived()) {
      if (DateTime.now().isAfter(deadline)) {
        throw CheckFailure('Tapping "$label" left the app at ${IkonNavigation.instance.currentUrl}');
      }

      await tester.pump(const Duration(milliseconds: 100));
    }

    await pumpFor(const Duration(milliseconds: 1500));
  }

  /// The node types on screen that have no Flutter builder. In a debug build, which an integration
  /// test always runs, a leaf the renderer could not draw shows its type name in a small grey label.
  Set<String> fallbackTypesOnScreen() {
    final labels = tester.widgetList<Text>(find.byWidgetPredicate((w) =>
        w is Text && w.style?.fontSize == 10 && w.style?.color == const Color(0xFF808080) && (w.data ?? '').contains('.')));
    return labels.map((t) => t.data!).toSet();
  }

  /// The app's page; null while the app shows its connecting screen instead.
  IkonParallaxView? get parallaxView {
    final views = find.byType(IkonParallaxView);
    return views.evaluate().isEmpty ? null : tester.widget<IkonParallaxView>(views.first);
  }

  IkonMediaManager? get mediaManager => parallaxView?.mediaManager;

  /// Waits for the app's page to be back after the connection was lost.
  Future<void> waitForPage({Duration timeout = const Duration(seconds: 90)}) =>
      waitFor(find.byType(IkonParallaxView), timeout: timeout, what: "the app's page after reconnecting");

  /// Asks the host to do something to the device the app cannot do to itself, and waits until it is
  /// done. Skips the check when no host controls the device.
  Future<void> hostFault(String name, {Map<String, String> args = const {}}) async {
    if (hostControlUrl.isEmpty) {
      throw CheckSkipped('no host controls this device (IKON_MOBILE_CONTROL is not set)');
    }

    final query = Uri(queryParameters: {'name': name, ...args}).query;
    final started = await _hostRequest('/fault?$query');

    if (started['ok'] != true) {
      throw CheckSkipped('the host cannot do "$name" here: ${started['reason']}');
    }

    final id = started['id'];
    final deadline = DateTime.now().add(const Duration(minutes: 3));

    // The fault can take the device's network away, so the request that starts it returns at once and
    // the test asks again until the host reports it done; a request made while offline just fails.
    while (DateTime.now().isBefore(deadline)) {
      await pumpFor(const Duration(seconds: 2));

      try {
        final status = await _hostRequest('/status?id=$id');

        if (status['done'] == true) {
          if (status['error'] != null) {
            throw CheckFailure('the host failed to do "$name": ${status['error']}');
          }

          return;
        }
      } on SocketException {
        // The device is offline while the fault lasts; the next poll asks again
      } on TimeoutException {
        // The request could not reach the host while the fault lasts; the next poll asks again
      } on HttpException {
        // The connection broke while the fault lasts; the next poll asks again
      }
    }

    throw CheckFailure('the host did not finish "$name" in 3 minutes');
  }

  Future<Map<String, dynamic>> _hostRequest(String pathAndQuery) async {
    final client = HttpClient()..connectionTimeout = const Duration(seconds: 5);

    try {
      final request = await client.getUrl(Uri.parse('$hostControlUrl$pathAndQuery')).timeout(const Duration(seconds: 5));
      final response = await request.close().timeout(const Duration(seconds: 10));
      final body = await response.transform(utf8.decoder).join();
      return jsonDecode(body) as Map<String, dynamic>;
    } finally {
      client.close(force: true);
    }
  }
}
