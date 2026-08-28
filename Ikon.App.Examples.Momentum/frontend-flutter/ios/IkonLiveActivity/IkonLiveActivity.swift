import SwiftUI
import WidgetKit

#if canImport(ActivityKit)
import ActivityKit

// The live banner on the lock screen and in the Dynamic Island — the thing a notification cannot do,
// and the reason a running app can be glanced at without unlocking the phone.
//
// Nothing here knows what app it is drawing for. It renders the metrics it is handed in the accent it
// is handed, which is what lets one widget serve every Ikon app.
@available(iOS 16.1, *)
struct IkonLiveActivity: Widget {
  var body: some WidgetConfiguration {
    ActivityConfiguration(for: IkonActivityAttributes.self) { context in
      LockScreenView(state: context.state, attributes: context.attributes)
        .activityBackgroundTint(Color(red: 0.043, green: 0.043, blue: 0.051))
        .activitySystemActionForegroundColor(.white)
    } dynamicIsland: { context in
      let accent = context.attributes.accent
      let accentColor = Color(red: accent.r, green: accent.g, blue: accent.b)

      return DynamicIsland {
        DynamicIslandExpandedRegion(.leading) {
          if let first = context.state.metrics.first {
            MetricView(metric: first)
          }
        }
        DynamicIslandExpandedRegion(.trailing) {
          if context.state.metrics.count > 1 {
            MetricView(metric: context.state.metrics[1])
          }
        }
        DynamicIslandExpandedRegion(.bottom) {
          Text(context.state.status)
            .font(.system(size: 13, weight: .medium))
            .foregroundStyle(context.state.muted ? Color.white.opacity(0.5) : accentColor)
        }
      } compactLeading: {
        Circle().fill(context.state.muted ? Color.white.opacity(0.4) : accentColor).frame(width: 8, height: 8)
      } compactTrailing: {
        Text(context.state.metrics.first?.value ?? "")
          .font(.system(size: 13, weight: .semibold))
          .monospacedDigit()
      } minimal: {
        Circle().fill(context.state.muted ? Color.white.opacity(0.4) : accentColor).frame(width: 8, height: 8)
      }
    }
  }
}

@available(iOS 16.1, *)
private struct MetricView: View {
  let metric: IkonActivityAttributes.Metric

  var body: some View {
    VStack(alignment: .leading, spacing: 2) {
      Text(metric.value).font(.system(size: 22, weight: .semibold)).monospacedDigit()
      Text(metric.label.uppercased())
        .font(.system(size: 9, weight: .medium))
        .tracking(1.4)
        .foregroundStyle(Color.white.opacity(0.55))
    }
  }
}

@available(iOS 16.1, *)
private struct LockScreenView: View {
  let state: IkonActivityAttributes.ContentState
  let attributes: IkonActivityAttributes

  var body: some View {
    let accent = attributes.accent
    let accentColor = Color(red: accent.r, green: accent.g, blue: accent.b)
    let liveColor = state.muted ? Color.white.opacity(0.45) : accentColor
    let metrics = Array(state.metrics.prefix(3))

    return VStack(alignment: .leading, spacing: 14) {
      HStack(spacing: 8) {
        Circle().fill(liveColor).frame(width: 8, height: 8)
        Text(state.status.uppercased())
          .font(.system(size: 11, weight: .bold))
          .tracking(1.8)
          .foregroundStyle(liveColor)
          .lineLimit(1)
        Spacer(minLength: 8)
        Text(attributes.title.uppercased())
          .font(.system(size: 10, weight: .medium))
          .tracking(1.4)
          .foregroundStyle(Color.white.opacity(0.35))
          .lineLimit(1)
      }

      // The first metric is the headline. Which number that is belongs to the app — a tracker leads
      // with distance, a delivery app with stops remaining — so the widget just renders whatever it
      // was handed first at the size that says "this is the one".
      if let lead = metrics.first {
        HStack(alignment: .lastTextBaseline, spacing: 8) {
          Text(lead.value)
            .font(.system(size: 46, weight: .semibold, design: .rounded))
            .monospacedDigit()
            .minimumScaleFactor(0.6)
            .lineLimit(1)
            .foregroundStyle(.white)
          Text(lead.label.uppercased())
            .font(.system(size: 10, weight: .medium))
            .tracking(1.4)
            .foregroundStyle(Color.white.opacity(0.4))
          Spacer(minLength: 0)
        }
      }

      // A hairline in the accent, brightening left to right: motion without animation, which is all a
      // live activity is allowed — WidgetKit archives the view and cannot run a loop.
      GeometryReader { geo in
        ZStack(alignment: .leading) {
          Capsule().fill(Color.white.opacity(0.08))
          Capsule()
            .fill(LinearGradient(
              colors: [liveColor.opacity(0.25), liveColor],
              startPoint: .leading,
              endPoint: .trailing))
            .frame(width: state.muted ? geo.size.width * 0.25 : geo.size.width)
        }
      }
      .frame(height: 3)

      if metrics.count > 1 {
        HStack(spacing: 0) {
          ForEach(Array(metrics.dropFirst().enumerated()), id: \.offset) { index, metric in
            VStack(alignment: .leading, spacing: 3) {
              Text(metric.value)
                .font(.system(size: 20, weight: .semibold))
                .monospacedDigit()
                .foregroundStyle(.white)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
              Text(metric.label.uppercased())
                .font(.system(size: 9, weight: .medium))
                .tracking(1.3)
                .foregroundStyle(Color.white.opacity(0.42))
            }
            .frame(maxWidth: .infinity, alignment: .leading)

            if index == 0 && metrics.count > 2 {
              Rectangle()
                .fill(Color.white.opacity(0.09))
                .frame(width: 1, height: 26)
                .padding(.trailing, 14)
            }
          }
        }
      }
    }
    .padding(.horizontal, 18)
    .padding(.vertical, 16)
  }
}

@available(iOS 16.1, *)
@main
struct IkonWidgetBundle: WidgetBundle {
  var body: some Widget {
    IkonLiveActivity()
  }
}
#endif
