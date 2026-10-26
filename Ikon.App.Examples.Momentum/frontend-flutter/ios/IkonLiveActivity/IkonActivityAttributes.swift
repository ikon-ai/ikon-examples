import Foundation

#if canImport(ActivityKit)
import ActivityKit
#if canImport(AppIntents)
import AppIntents
#endif

/// The contract between an IKON app and its live banner.
///
/// Deliberately generic: an app sends *values*, never layout. A tracker sends distance and time, a
/// delivery app sends stops remaining and an ETA, a timer sends what is left — the widget draws
/// whatever it is given, so no app author ever writes Swift. That is what makes this something the
/// scaffold can ship for every Flutter app rather than something each one builds.
@available(iOS 16.1, *)
public struct IkonActivityAttributes: ActivityAttributes {
  public struct Metric: Codable, Hashable {
    public var value: String
    public var label: String

    public init(value: String, label: String) {
      self.value = value
      self.label = label
    }
  }

  /// A button on the banner. A press reaches the app without opening it (iOS 17 and later).
  public struct Action: Codable, Hashable {
    public var id: String
    public var label: String
    /// An SF Symbol before the label, or none.
    public var icon: String?

    public init(id: String, label: String, icon: String? = nil) {
      self.id = id
      self.label = label
      self.icon = icon
    }
  }

  public struct ContentState: Codable, Hashable {
    /// Up to three metrics. Formatted by the app, because the app owns its units and the widget
    /// must not re-implement them.
    public var metrics: [Metric]
    /// The small tracked line above the metrics — a status, a phase, a name.
    public var status: String
    /// Whether the activity is showing a held or paused state, which mutes the accent.
    public var muted: Bool
    /// An SF Symbol the Dynamic Island shows in place of the accent dot; nil keeps the dot. Optional,
    /// so a banner from before it existed still decodes.
    public var icon: String?
    /// Up to two buttons; nil or empty shows none.
    public var actions: [Action]?

    public init(metrics: [Metric], status: String, muted: Bool, icon: String? = nil, actions: [Action]? = nil) {
      self.metrics = metrics
      self.status = status
      self.muted = muted
      self.icon = icon
      self.actions = actions
    }
  }

  /// Fixed for the life of the activity.
  public var title: String
  /// The app's accent, as `#rrggbb`, so the banner matches the app it came from.
  public var accentHex: String

  public init(title: String, accentHex: String) {
    self.title = title
    self.accentHex = accentHex
  }
}

extension Notification.Name {
  /// Posted in the app when one of the banner's buttons is pressed; the object is the action's id.
  static let ikonLiveActivityAction = Notification.Name("IkonLiveActivityAction")
}

#if canImport(AppIntents)
/// A press of a banner button. As a live activity intent it runs in the app's own process, which is
/// alive while the app plays or records audio in the background, so the bridge can tell the server
/// without the app coming to the front. Compiled into the widget too, which only names it.
@available(iOS 17.0, *)
public struct IkonActivityActionIntent: LiveActivityIntent {
  public static var title: LocalizedStringResource = "Live activity action"
  public static var isDiscoverable: Bool = false

  @Parameter(title: "Action")
  public var actionId: String

  public init() {
    actionId = ""
  }

  public init(actionId: String) {
    self.actionId = actionId
  }

  public func perform() async throws -> some IntentResult {
    await MainActor.run {
      NotificationCenter.default.post(name: .ikonLiveActivityAction, object: actionId)
    }
    return .result()
  }
}
#endif

@available(iOS 16.1, *)
extension IkonActivityAttributes {
  var accent: (r: Double, g: Double, b: Double) {
    var hex = accentHex.trimmingCharacters(in: CharacterSet(charactersIn: "#"))

    if hex.count != 6 {
      hex = "DB176E"
    }

    let value = UInt32(hex, radix: 16) ?? 0xDB176E
    return (Double((value >> 16) & 0xFF) / 255.0,
            Double((value >> 8) & 0xFF) / 255.0,
            Double(value & 0xFF) / 255.0)
  }
}
#endif
