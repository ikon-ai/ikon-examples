public enum ActivityKind
{
    Foot,
    Bike,
    Horse,
    Car,
}

public enum RecordingState
{
    Idle,
    Recording,
    AutoPaused,
    Paused,
}

public enum MedalTier
{
    None,
    Bronze,
    Silver,
    Gold,
}

public enum MomentumSection
{
    Move,
    Feed,
    Activity,
    You,

    /// <summary>The operator view. Not a tab — reached with ?section=admin.</summary>
    Admin,
}

/// <summary>One accepted fix on a recorded track, with everything derived from it already computed.</summary>
public sealed record TrackPoint(
    double Seconds,
    GeoPoint Point,
    double ElevationM,
    double SpeedMps,
    double HeadingDeg,
    double AccuracyM,
    double DistanceM,
    bool Moving);

/// <summary>
/// One thing worth showing off, found by a detector. <see cref="Score"/> is 0–100 within the
/// detector's own scale, and <see cref="Tier"/> compares it against the rider's own history.
/// </summary>
public sealed record Highlight(
    string Id,
    string ActivityId,
    string Detector,
    string Title,
    string Detail,
    double StartSeconds,
    double EndSeconds,
    double Score,
    MedalTier Tier,
    string Icon)
{
    public double DurationSeconds => EndSeconds - StartSeconds;
}

/// <summary>A finished (or in-flight) activity and its rolled-up numbers.</summary>
public sealed record Activity(
    string Id,
    string UserId,
    ActivityKind Kind,
    string Title,
    string Story,
    DateTime StartedAt,
    double DistanceM,
    double MovingSeconds,
    double ElapsedSeconds,
    double AscentM,
    double DescentM,
    double AvgSpeedMps,
    double MaxSpeedMps,
    double MomentumScore,
    bool Published)
{
    public string KindLabel => Momentum.KindLabel(Kind, DistanceM, MovingSeconds);
}

/// <summary>What the live screen renders — rebuilt from the recorder on every frame bump.</summary>
public sealed record LiveFrame(
    RecordingState State,
    ActivityKind Kind,
    double DistanceM,
    double MovingSeconds,
    double ElapsedSeconds,
    double SpeedMps,
    double AvgSpeedMps,
    double MaxSpeedMps,
    double AscentM,
    double DescentM,
    double GradePct,
    double HeadingDeg,
    double AccuracyM,
    GeoPoint? Position,
    IReadOnlyList<GeoPoint> Track,
    IReadOnlyList<Highlight> LiveHighlights,
    string CoachCue,
    bool Simulated);

/// <summary>Per-kind constants: everything the recorder, the detectors and the labels need.</summary>
public sealed record KindProfile(
    ActivityKind Kind,
    string Label,
    string Icon,
    double PauseSpeedMps,
    double PauseDwellSeconds,
    double ResumeSpeedMps,
    double ClimbMinGainM,
    double ClimbMinGradePct,
    double FastThresholdMps,
    bool ShowsSpeed);

public static class Momentum
{
    public static readonly IReadOnlyDictionary<ActivityKind, KindProfile> Profiles = new Dictionary<ActivityKind, KindProfile>
    {
        // A car idling at a red light must not end the drive, so its dwell is far longer than a
        // runner's; the traffic-light detector then reads those same stops back out of the track.
        [ActivityKind.Foot] = new(ActivityKind.Foot, "Run", "footprints", 0.5, 8, 0.9, 25, 3, 3.3, false),
        [ActivityKind.Bike] = new(ActivityKind.Bike, "Ride", "bike", 1.0, 8, 1.8, 40, 3, 8.3, true),
        [ActivityKind.Horse] = new(ActivityKind.Horse, "Horse", "fence", 0.5, 12, 1.0, 25, 3, 4.5, true),
        [ActivityKind.Car] = new(ActivityKind.Car, "Drive", "car", 0.6, 20, 1.5, 60, 2, 22.2, true),
    };

    public static KindProfile ProfileOf(ActivityKind kind) => Profiles[kind];

    /// <summary>
    /// What to call the activity. Walking and running are one kind on the wire — the phone cannot
    /// tell them apart at the start — so the label is resolved from the pace that was actually held.
    /// </summary>
    public static string KindLabel(ActivityKind kind, double distanceM, double movingSeconds)
    {
        if (kind != ActivityKind.Foot)
        {
            return Profiles[kind].Label;
        }

        double kmh = movingSeconds > 0 ? distanceM / movingSeconds * 3.6 : 0;
        return kmh >= 7 ? "Run" : "Walk";
    }

    public static string FormatDistance(double meters) =>
        meters >= 1000 ? $"{meters / 1000:0.00} km" : $"{meters:0} m";

    public static string FormatDuration(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }

    public static string FormatSpeed(double mps) => $"{mps * 3.6:0.0} km/h";

    /// <summary>Pace as min/km, which is how feet and hooves are read; empty above 30 min/km.</summary>
    public static string FormatPace(double mps)
    {
        if (mps < 0.55)
        {
            return "—";
        }

        double secondsPerKm = 1000 / mps;
        return $"{(int)(secondsPerKm / 60)}:{(int)(secondsPerKm % 60):00} /km";
    }

    /// <summary>The rider-facing metric for a kind: pace on foot and horseback, speed on wheels.</summary>
    public static string FormatRate(ActivityKind kind, double mps) =>
        Profiles[kind].ShowsSpeed ? FormatSpeed(mps) : FormatPace(mps);
}
