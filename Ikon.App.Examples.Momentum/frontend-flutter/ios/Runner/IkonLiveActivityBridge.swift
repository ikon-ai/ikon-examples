import Flutter
import UIKit

#if canImport(ActivityKit)
import ActivityKit
#endif

/// The Swift half of `ikon.client.*LiveActivity`. Everything above it — the Dart client functions,
/// the C# call an app makes — is platform-neutral; this is the only part that has to be native, and
/// the scaffold ships it so no app author writes it.
///
/// Registered from `AppDelegate`. On a device below iOS 16.2, or with activities switched off, every
/// call answers false rather than throwing: a live banner is a nicety and its absence must never take
/// an app down with it.
public final class IkonLiveActivityBridge: NSObject {
  private static let channelName = "ikon/live_activity"

  #if canImport(ActivityKit)
  @available(iOS 16.2, *)
  private static var current: Activity<IkonActivityAttributes>? {
    get { _current as? Activity<IkonActivityAttributes> }
    set { _current = newValue }
  }
  private static var _current: Any?
  #endif

  public static func register(with registrar: FlutterPluginRegistrar) {
    let channel = FlutterMethodChannel(name: channelName, binaryMessenger: registrar.messenger())

    channel.setMethodCallHandler { call, result in
      #if canImport(ActivityKit)
      guard #available(iOS 16.2, *), ActivityAuthorizationInfo().areActivitiesEnabled else {
        result(false)
        return
      }

      let args = call.arguments as? [String: Any] ?? [:]

      switch call.method {
      case "start":
        result(start(args))
      case "update":
        result(update(args))
      case "end":
        result(end())
      default:
        result(FlutterMethodNotImplemented)
      }
      #else
      result(false)
      #endif
    }
  }

  #if canImport(ActivityKit)
  @available(iOS 16.2, *)
  private static func contentState(from args: [String: Any]) -> IkonActivityAttributes.ContentState {
    var metrics: [IkonActivityAttributes.Metric] = []

    if let json = args["metrics"] as? String,
       let data = json.data(using: .utf8),
       let decoded = try? JSONDecoder().decode([IkonActivityAttributes.Metric].self, from: data) {
      metrics = decoded
    }

    return .init(
      metrics: metrics,
      status: args["status"] as? String ?? "",
      muted: args["muted"] as? Bool ?? false)
  }

  @available(iOS 16.2, *)
  private static func start(_ args: [String: Any]) -> Bool {
    // Starting twice would leave the first banner orphaned on the lock screen with numbers that stop
    // moving, so an existing activity is updated rather than replaced.
    if current != nil {
      return update(args)
    }

    // An activity outlives the process that started it, so a relaunched app finds `current` nil while
    // its banner is still on the lock screen. Adopting the running one keeps a restart from stacking
    // a second banner beside the first.
    if let running = Activity<IkonActivityAttributes>.activities.first {
      current = running
      return update(args)
    }

    let attributes = IkonActivityAttributes(
      title: args["title"] as? String ?? "",
      accentHex: args["accent"] as? String ?? "#db176e")

    do {
      current = try Activity.request(
        attributes: attributes,
        content: .init(state: contentState(from: args), staleDate: staleDate()))
      return true
    } catch {
      NSLog("[IkonLiveActivity] start failed: \(error)")
      return false
    }
  }

  /// When the readout stops being believable if nothing refreshes it.
  ///
  /// With no stale date iOS keeps an activity live until its own multi-hour cap, so an app that is
  /// force-quit — or a server that stops sending — leaves a live-looking banner reporting a ride that
  /// ended long ago. Every update pushes this forward, so it only takes effect once updates stop.
  @available(iOS 16.2, *)
  private static func staleDate() -> Date {
    Date().addingTimeInterval(15 * 60)
  }

  @available(iOS 16.2, *)
  private static func update(_ args: [String: Any]) -> Bool {
    guard let activity = current else {
      return false
    }

    Task {
      await activity.update(.init(state: contentState(from: args), staleDate: staleDate()))
    }

    return true
  }

  /// Ends every activity this app owns, not just the one this process started.
  ///
  /// `current` is a process-lifetime reference: after a force-quit and relaunch it is nil while the
  /// banner is still there, so keying off it made the app structurally unable to clear exactly the
  /// case that needs clearing most.
  @available(iOS 16.2, *)
  private static func end() -> Bool {
    current = nil

    Task {
      for activity in Activity<IkonActivityAttributes>.activities {
        await activity.end(nil, dismissalPolicy: .immediate)
      }
    }

    return true
  }
  #endif
}
