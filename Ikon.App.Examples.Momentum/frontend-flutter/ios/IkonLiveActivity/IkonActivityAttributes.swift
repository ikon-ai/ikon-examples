import Foundation

#if canImport(ActivityKit)
import ActivityKit

/// The contract between an Ikon app and its live banner.
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

  public struct ContentState: Codable, Hashable {
    /// Up to three metrics. Formatted by the app, because the app owns its units and the widget
    /// must not re-implement them.
    public var metrics: [Metric]
    /// The small tracked line above the metrics — a status, a phase, a name.
    public var status: String
    /// Whether the activity is showing a held or paused state, which mutes the accent.
    public var muted: Bool

    public init(metrics: [Metric], status: String, muted: Bool) {
      self.metrics = metrics
      self.status = status
      self.muted = muted
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
