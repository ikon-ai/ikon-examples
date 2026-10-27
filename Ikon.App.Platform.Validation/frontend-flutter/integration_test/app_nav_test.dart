// Drives the App Nav tab on a phone or simulator against a local run of the Validation app:
//
//   ikon run --platform-repo <repo> --detach        (in the app folder)
//   flutter test integration_test/app_nav_test.dart -d <device> \
//     --dart-define=IKON_PORT=<server port> --dart-define=IKON_SERVER_HOST=localhost
//
// It proves on the device what the widget tests prove with fakes: a press on a control with a path
// moves the app to that path and shows the new selection, the page's skeleton and the pending bar
// before the server answers, and the app keeps the screen it showed for its next start.
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ikon_app_platform_validation/main.dart' as app;
import 'package:ikon_sdk/ikon_sdk.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> pumpUntil(WidgetTester tester, Finder finder, {Duration timeout = const Duration(seconds: 60)}) async {
    final deadline = DateTime.now().add(timeout);
    while (finder.evaluate().isEmpty) {
      if (DateTime.now().isAfter(deadline)) {
        fail('Timed out waiting for $finder');
      }
      await tester.pump(const Duration(milliseconds: 100));
    }
  }

  testWidgets('a press with a path shows its page at once and the app keeps its screen', (tester) async {
    app.main();
    await pumpUntil(tester, find.text('App Nav'));
    await tester.tap(find.text('App Nav').first);
    await pumpUntil(tester, find.text('NavBar and NavOutlet'));
    await tester.pump(const Duration(seconds: 1));

    await tester.ensureVisible(find.text('Slow'));
    await tester.tap(find.text('Slow'));
    await tester.pump(const Duration(milliseconds: 50));
    expect(IkonNavigation.instance.currentUrl, '/app-nav/slow', reason: 'the app moves before the server answers');
    expect(find.byKey(const ValueKey('ikon-nav-skeleton')), findsOneWidget, reason: 'the skeleton shows before the server answers');
    await tester.pump(const Duration(milliseconds: 200));
    expect(find.byType(LinearProgressIndicator), findsOneWidget, reason: 'the pending bar shows while the server holds the tap');

    await pumpUntil(tester, find.text('PASS page slow arrived'));
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.byKey(const ValueKey('ikon-nav-skeleton')), findsNothing);
    expect(find.text('Slow page'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsNothing);

    await tester.ensureVisible(find.text('Open Inbox'));
    await tester.tap(find.text('Open Inbox'));
    await tester.pump(const Duration(milliseconds: 50));
    expect(IkonNavigation.instance.currentUrl, '/app-nav/inbox', reason: 'a plain button with a path moves too');
    await pumpUntil(tester, find.text('PASS page inbox arrived'));

    await tester.pump(const Duration(seconds: 2));
    final kept = await IkonLiveSnapshot.load(app.IkonAppConfig.snapshotKey);
    expect(kept, isNotNull, reason: 'the screen is kept for the next start');
  });
}
