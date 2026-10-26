import Flutter
import UIKit

#if canImport(ActivityKit)
import ActivityKit
#endif

/// The Swift half of `ikon.client.*LiveActivity`. Everything above it — the Dart client functions,
/// the C# call an app makes — is platform-neutral; this is the only part that has to be native.
/// `ikon new` does not create it; an app that wants the banner copies this file, its `AppDelegate`
/// registration and the `IkonLiveActivity` widget extension.
///
/// Registered from `AppDelegate`. On a device below iOS 16.2, or with activities switched off, every
/// call answers false rather than throwing: a live banner is a nicety and its absence must never take
/// an app down with it.
public final class IkonLiveActivityBridge: NSObject {
  private static let channelName = "ikon/live_activity"

  #if canImport(ActivityKit)
  // An activity the person swiped away, or one iOS ended at its time limit, takes no updates, so it
  // counts as gone and the next start puts a new banner up. A stale one, past its stale date because
  // nothing updated it, is still on the lock screen and takes updates, so it is still the banner.
  @available(iOS 16.2, *)
  private static var current: Activity<IkonActivityAttributes>? {
    get {
      guard let activity = _current as? Activity<IkonActivityAttributes>, isShowing(activity) else {
        return nil
      }
      return activity
    }
    set { _current = newValue }
  }
  private static var _current: Any?
  #endif

  private static var channel: FlutterMethodChannel?

  public static func register(with registrar: FlutterPluginRegistrar) {
    let channel = FlutterMethodChannel(name: channelName, binaryMessenger: registrar.messenger())
    self.channel = channel

    // A press on the banner, which IkonActivityActionIntent posts in this process, goes to the Dart
    // client, which tells the server
    NotificationCenter.default.addObserver(forName: .ikonLiveActivityAction, object: nil, queue: .main) { note in
      if let actionId = note.object as? String {
        IkonLiveActivityBridge.channel?.invokeMethod("action", arguments: actionId)
      }
    }

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

    var actions: [IkonActivityAttributes.Action] = []

    if let json = args["actions"] as? String,
       let data = json.data(using: .utf8),
       let decoded = try? JSONDecoder().decode([IkonActivityAttributes.Action].self, from: data) {
      actions = Array(decoded.prefix(2))
    }

    let icon = args["icon"] as? String ?? ""

    return .init(
      metrics: metrics,
      status: args["status"] as? String ?? "",
      muted: args["muted"] as? Bool ?? false,
      icon: icon.isEmpty ? nil : icon,
      actions: actions.isEmpty ? nil : actions)
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
    if let running = Activity<IkonActivityAttributes>.activities.first(where: { isShowing($0) }) {
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

  @available(iOS 16.2, *)
  private static func isShowing(_ activity: Activity<IkonActivityAttributes>) -> Bool {
    activity.activityState == .active || activity.activityState == .stale
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
