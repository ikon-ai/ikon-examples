import Flutter
import UIKit

@main
@objc class AppDelegate: FlutterAppDelegate, FlutterImplicitEngineDelegate {
  override func application(
    _ application: UIApplication,
    didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
  ) -> Bool {
    return super.application(application, didFinishLaunchingWithOptions: launchOptions)
  }

  func didInitializeImplicitFlutterEngine(_ engineBridge: FlutterImplicitEngineBridge) {
    GeneratedPluginRegistrant.register(with: engineBridge.pluginRegistry)

    // The native half of ikon.client.*LiveActivity. Everything above it is platform-neutral.
    if let registrar = engineBridge.pluginRegistry.registrar(forPlugin: "IkonLiveActivityBridge") {
      IkonLiveActivityBridge.register(with: registrar)
    }
  }
}
