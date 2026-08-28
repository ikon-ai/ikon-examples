/// <summary>
/// Everything the app finds worth showing off, found by geometry and physics over the recorded track
/// rather than by asking a model. Two reasons it works this way: a detector can be unit-tested against
/// a seeded simulated outing and asserted exactly, and the same stretch of road produces the same
/// highlight every time, which is what makes a personal best mean anything.
///
/// The AI ranks and narrates what comes out of here. It never decides whether a highlight exists.
/// </summary>
public static class Detectors
{
    /// <summary>Speeds a top-speed highlight is scored against, per kind, in m/s.</summary>
    private static readonly Dictionary<ActivityKind, double> TopSpeedReference = new()
    {
        [ActivityKind.Foot] = 6.5,
        [ActivityKind.Bike] = 16.0,
        [ActivityKind.Horse] = 11.0,
        [ActivityKind.Car] = 33.0,
    };

    /// <summary>
    /// Runs every detector that applies to the kind. <paramref name="previousBest"/> returns the
    /// rider's best previous score for a detector, which is what turns a raw score into a medal —
    /// gold means "better than you have ever done", not "better than some constant we picked".
    /// </summary>
    public static IReadOnlyList<Highlight> Detect(
        string activityId,
        ActivityKind kind,
        IReadOnlyList<TrackPoint> points,
        Func<string, double>? previousBest = null)
    {
        if (points.Count < 10)
        {
            return [];
        }

        var found = new List<Highlight>();

        found.AddRange(DetectClimbs(activityId, kind, points));
        found.AddRange(DetectTopSpeed(activityId, kind, points));
        found.AddRange(DetectSplits(activityId, kind, points));

        switch (kind)
        {
            case ActivityKind.Foot:
                found.AddRange(DetectSurges(activityId, points));
                found.AddRange(DetectNegativeSplit(activityId, points));
                found.AddRange(DetectMetronome(activityId, points));
                break;
            case ActivityKind.Bike:
                found.AddRange(DetectFlyer(activityId, kind, points));
                break;
            case ActivityKind.Horse:
                found.AddRange(DetectGaits(activityId, points));
                found.AddRange(DetectTrail(activityId, points));
                break;
            case ActivityKind.Car:
                found.AddRange(DetectCleanStraights(activityId, points));
                found.AddRange(DetectTrafficLightLaunches(activityId, points));
                found.AddRange(DetectSweepers(activityId, points));
                found.AddRange(DetectSmoothness(activityId, points));
                break;
        }

        return found
            .Select(h => h with { Tier = TierFor(h.Score, previousBest?.Invoke(h.Detector) ?? 0) })
            .OrderByDescending(h => h.Score)
            .ToList();
    }

    /// <summary>
    /// A medal compares the rider against the rider. A score that beats everything they have done
    /// before is gold whatever its absolute value — a first ride on flat ground still earns its climb
    /// — and an outstanding score is gold even on a day they have beaten before.
    /// </summary>
    public static MedalTier TierFor(double score, double previousBest)
    {
        if (score >= 85 || (previousBest > 0 && score > previousBest * 1.02))
        {
            return MedalTier.Gold;
        }

        if (score >= 60 || (previousBest > 0 && score > previousBest * 0.9))
        {
            return MedalTier.Silver;
        }

        return score >= 30 ? MedalTier.Bronze : MedalTier.None;
    }

    #region Climbs and descents

    private static IEnumerable<Highlight> DetectClimbs(string activityId, ActivityKind kind, IReadOnlyList<TrackPoint> points)
    {
        // Two of each. A ridge route throws up a dozen ramps and a reel of twelve near-identical
        // "Climb, 44 m" cards tells the rider nothing about any of them.
        var climbs = FindClimbs(activityId, kind, points).ToList();

        return climbs.Where(h => h.Detector == "climb").OrderByDescending(h => h.Score).Take(2)
            .Concat(climbs.Where(h => h.Detector == "descent").OrderByDescending(h => h.Score).Take(2));
    }

    private static IEnumerable<Highlight> FindClimbs(string activityId, ActivityKind kind, IReadOnlyList<TrackPoint> points)
    {
        var profile = Momentum.ProfileOf(kind);
        var segments = ElevationSegments(points, prominenceM: Math.Max(10, profile.ClimbMinGainM / 2));

        foreach (var (from, to) in segments)
        {
            double gain = points[to].ElevationM - points[from].ElevationM;
            double run = points[to].DistanceM - points[from].DistanceM;

            if (run < 200)
            {
                continue;
            }

            double gradePct = gain / run * 100;
            bool isClimb = gain > 0;
            double magnitude = Math.Abs(gain);

            if (magnitude < profile.ClimbMinGainM || Math.Abs(gradePct) < profile.ClimbMinGradePct)
            {
                continue;
            }

            // Cycling's own index — length in metres times average grade in per cent — because it
            // ranks a long drag and a short wall the way a rider would.
            double index = run * Math.Abs(gradePct);
            double score = Math.Clamp(index / 200, 0, 100);
            string category = ClimbCategory(index);

            yield return new Highlight(
                $"{activityId}:{(isClimb ? "climb" : "descent")}:{from}",
                activityId,
                isClimb ? "climb" : "descent",
                isClimb
                    ? category.Length > 0 ? $"{category} climb" : "Climb"
                    : "Descent",
                $"{magnitude:0} m over {Momentum.FormatDistance(run)} at {Math.Abs(gradePct):0.0} %",
                points[from].Seconds,
                points[to].Seconds,
                score,
                MedalTier.None,
                isClimb ? "trending-up" : "trending-down");
        }
    }

    /// <summary>Strava's thresholds on the same length × grade index, so the labels mean what riders expect.</summary>
    private static string ClimbCategory(double index) => index switch
    {
        >= 80_000 => "HC",
        >= 64_000 => "Cat 1",
        >= 32_000 => "Cat 2",
        >= 16_000 => "Cat 3",
        >= 8_000 => "Cat 4",
        _ => "",
    };

    /// <summary>
    /// Splits the elevation profile into monotone runs by finding the turning points that stand clear
    /// of the filter's residual noise. A plain "elevation went up" scan would chop one hill into
    /// dozens of fragments; prominence is what keeps a hill a hill.
    /// </summary>
    private static List<(int From, int To)> ElevationSegments(IReadOnlyList<TrackPoint> points, double prominenceM)
    {
        var turningPoints = new List<int> { 0 };
        int candidate = 0;
        int direction = 0;

        for (int i = 1; i < points.Count; i++)
        {
            double delta = points[i].ElevationM - points[candidate].ElevationM;

            if (direction >= 0 && delta > 0)
            {
                candidate = i;
                direction = 1;
            }
            else if (direction <= 0 && delta < 0)
            {
                candidate = i;
                direction = -1;
            }
            else if (Math.Abs(delta) > prominenceM)
            {
                turningPoints.Add(candidate);
                candidate = i;
                direction = -direction;
            }
        }

        turningPoints.Add(points.Count - 1);

        var segments = new List<(int, int)>();

        for (int i = 0; i < turningPoints.Count - 1; i++)
        {
            if (turningPoints[i + 1] > turningPoints[i])
            {
                segments.Add((turningPoints[i], turningPoints[i + 1]));
            }
        }

        return segments;
    }

    #endregion

    #region Speed

    private static IEnumerable<Highlight> DetectTopSpeed(string activityId, ActivityKind kind, IReadOnlyList<TrackPoint> points)
    {
        // Three seconds, because one GPS sample of 140 km/h is a glitch and three consecutive are a
        // straight.
        var best = BestWindow(points, windowSeconds: 3, value: window => window.Average(p => p.SpeedMps));

        if (best is not { } window || window.Value < 0.5)
        {
            yield break;
        }

        double score = Math.Clamp(window.Value / TopSpeedReference[kind] * 100, 0, 100);

        yield return new Highlight(
            $"{activityId}:topspeed",
            activityId,
            "top-speed",
            "Top speed",
            Momentum.FormatSpeed(window.Value),
            points[window.From].Seconds,
            points[window.To].Seconds,
            score,
            MedalTier.None,
            "gauge");
    }

    private static IEnumerable<Highlight> DetectSplits(string activityId, ActivityKind kind, IReadOnlyList<TrackPoint> points)
    {
        double total = points[^1].DistanceM;

        foreach (double splitM in (double[])[1000, 5000, 10_000])
        {
            if (total < splitM)
            {
                continue;
            }

            var fastest = FastestDistanceWindow(points, splitM);

            if (fastest is not { } split)
            {
                continue;
            }

            double speed = splitM / split.Seconds;
            double score = Math.Clamp(speed / TopSpeedReference[kind] * 105, 0, 100);
            string label = splitM >= 1000 ? $"{splitM / 1000:0} km" : $"{splitM:0} m";

            yield return new Highlight(
                $"{activityId}:split:{splitM:0}",
                activityId,
                $"split-{splitM:0}",
                $"Fastest {label}",
                $"{Momentum.FormatDuration(split.Seconds)} · {Momentum.FormatRate(kind, speed)}",
                points[split.From].Seconds,
                points[split.To].Seconds,
                score,
                MedalTier.None,
                "timer");
        }
    }

    private static IEnumerable<Highlight> DetectFlyer(string activityId, ActivityKind kind, IReadOnlyList<TrackPoint> points)
    {
        double threshold = Momentum.ProfileOf(kind).FastThresholdMps;
        var run = LongestRun(points, (p, _) => p.SpeedMps >= threshold);

        if (run is not { } stretch)
        {
            yield break;
        }

        double distance = points[stretch.To].DistanceM - points[stretch.From].DistanceM;

        if (distance < 800)
        {
            yield break;
        }

        yield return new Highlight(
            $"{activityId}:flyer",
            activityId,
            "flyer",
            "Flyer",
            $"{Momentum.FormatDistance(distance)} held above {Momentum.FormatSpeed(threshold)}",
            points[stretch.From].Seconds,
            points[stretch.To].Seconds,
            Math.Clamp(distance / 100, 0, 100),
            MedalTier.None,
            "wind");
    }

    #endregion

    #region On foot

    private static IEnumerable<Highlight> DetectSurges(string activityId, IReadOnlyList<TrackPoint> points)
    {
        var moving = points.Where(p => p.Moving).ToList();

        if (moving.Count < 60)
        {
            yield break;
        }

        // Against a half-minute rolling pace, not against each second: a runner's stride and the GPS
        // between them wobble several per cent every second, so a per-sample threshold measures the
        // noise and never finds the effort underneath it. And against the median rather than the mean,
        // because a surge drags the mean up towards itself and then fails to clear its own threshold.
        var smoothed = RollingSpeed(points, 30);
        double cruise = Median(smoothed);
        double threshold = cruise * 1.10;
        var run = LongestRun(points, (p, i) => p.Moving && smoothed[i] >= threshold);

        if (run is not { } stretch)
        {
            yield break;
        }

        double distance = points[stretch.To].DistanceM - points[stretch.From].DistanceM;

        if (distance < 200)
        {
            yield break;
        }

        double surgeSpeed = points.Skip(stretch.From).Take(stretch.To - stretch.From + 1).Average(p => p.SpeedMps);
        double average = cruise;

        yield return new Highlight(
            $"{activityId}:surge",
            activityId,
            "surge",
            "Surge",
            $"{Momentum.FormatDistance(distance)} at {Momentum.FormatPace(surgeSpeed)}",
            points[stretch.From].Seconds,
            points[stretch.To].Seconds,
            Math.Clamp(distance / 12 + (surgeSpeed / average - 1) * 120, 0, 100),
            MedalTier.None,
            "zap");
    }

    private static IEnumerable<Highlight> DetectNegativeSplit(string activityId, IReadOnlyList<TrackPoint> points)
    {
        double half = points[^1].DistanceM / 2;
        int mid = IndexOfFirst(points, p => p.DistanceM >= half);

        if (mid <= 0 || mid >= points.Count - 1)
        {
            yield break;
        }

        double firstSeconds = points[mid].Seconds - points[0].Seconds;
        double secondSeconds = points[^1].Seconds - points[mid].Seconds;

        if (firstSeconds <= 0 || secondSeconds <= 0 || secondSeconds >= firstSeconds)
        {
            yield break;
        }

        double gainPct = (firstSeconds - secondSeconds) / firstSeconds * 100;

        if (gainPct < 1.5)
        {
            yield break;
        }

        yield return new Highlight(
            $"{activityId}:negative-split",
            activityId,
            "negative-split",
            "Negative split",
            $"Second half {gainPct:0.0} % faster",
            points[mid].Seconds,
            points[^1].Seconds,
            Math.Clamp(gainPct * 12, 0, 100),
            MedalTier.None,
            "chevrons-up");
    }

    private static IEnumerable<Highlight> DetectMetronome(string activityId, IReadOnlyList<TrackPoint> points)
    {
        var moving = points.Where(p => p.Moving).ToList();

        if (moving.Count < 120)
        {
            yield break;
        }

        var smoothed = RollingSpeed(points, 30);
        double average = moving.Average(p => p.SpeedMps);
        var run = LongestRun(points, (p, i) => p.Moving && Math.Abs(smoothed[i] - average) / average < 0.04);

        if (run is not { } stretch)
        {
            yield break;
        }

        double seconds = points[stretch.To].Seconds - points[stretch.From].Seconds;

        if (seconds < 240)
        {
            yield break;
        }

        yield return new Highlight(
            $"{activityId}:metronome",
            activityId,
            "metronome",
            "Metronome",
            $"{Momentum.FormatDuration(seconds)} inside 4 % of {Momentum.FormatPace(average)}",
            points[stretch.From].Seconds,
            points[stretch.To].Seconds,
            Math.Clamp(seconds / 12, 0, 100),
            MedalTier.None,
            "activity");
    }

    #endregion

    #region On horseback

    /// <summary>Speed bands for the four gaits, in m/s, with the hysteresis a real trace needs.</summary>
    private static readonly (string Name, double Low, double High)[] GaitBands =
    [
        ("Walk", 0.8, 2.4),
        ("Trot", 2.6, 5.2),
        ("Canter", 5.4, 8.2),
        ("Gallop", 8.4, 20.0),
    ];

    private static IEnumerable<Highlight> DetectGaits(string activityId, IReadOnlyList<TrackPoint> points)
    {
        var runs = new List<(int Gait, int From, int To)>();
        int current = -1;
        int start = 0;

        for (int i = 0; i < points.Count; i++)
        {
            int band = GaitOf(points[i].SpeedMps, current);

            if (band == current)
            {
                continue;
            }

            if (current >= 0 && points[i - 1].Seconds - points[start].Seconds >= 15)
            {
                runs.Add((current, start, i - 1));
            }

            current = band;
            start = i;
        }

        if (current >= 0 && points[^1].Seconds - points[start].Seconds >= 15)
        {
            runs.Add((current, start, points.Count - 1));
        }

        // One highlight per gait — the best example of each — rather than every stretch, which on a
        // long hack would be dozens of near-identical cards.
        foreach (var group in runs.Where(r => r.Gait >= 1).GroupBy(r => r.Gait))
        {
            var best = group.MaxBy(r => points[r.To].Seconds - points[r.From].Seconds);
            double seconds = points[best.To].Seconds - points[best.From].Seconds;
            double distance = points[best.To].DistanceM - points[best.From].DistanceM;
            string name = GaitBands[best.Gait].Name;

            yield return new Highlight(
                $"{activityId}:gait:{name.ToLowerInvariant()}",
                activityId,
                $"gait-{name.ToLowerInvariant()}",
                $"Longest {name.ToLowerInvariant()}",
                $"{Momentum.FormatDuration(seconds)} · {Momentum.FormatDistance(distance)}",
                points[best.From].Seconds,
                points[best.To].Seconds,
                Math.Clamp(seconds / (best.Gait >= 3 ? 0.5 : 4), 0, 100),
                MedalTier.None,
                best.Gait >= 3 ? "flame" : "footprints");
        }
    }

    /// <summary>
    /// Which gait a speed belongs to, keeping the current gait through the dead band between two
    /// bands. Without that, a horse cruising on the boundary flickers between trot and canter every
    /// second and every run comes out too short to report.
    /// </summary>
    private static int GaitOf(double speedMps, int currentGait)
    {
        if (currentGait >= 0 && speedMps >= GaitBands[currentGait].Low && speedMps <= GaitBands[currentGait].High)
        {
            return currentGait;
        }

        for (int i = 0; i < GaitBands.Length; i++)
        {
            if (speedMps >= GaitBands[i].Low && speedMps <= GaitBands[i].High)
            {
                return i;
            }
        }

        return currentGait >= 0 ? currentGait : 0;
    }

    private static IEnumerable<Highlight> DetectTrail(string activityId, IReadOnlyList<TrackPoint> points)
    {
        // Sinuosity: how much further the path is than the straight line between its ends. A winding
        // forest track runs well above 1.4; a field edge sits near 1.0.
        var best = BestWindow(points, windowSeconds: 240, value: window =>
        {
            double along = window[^1].DistanceM - window[0].DistanceM;
            double across = Geo.DistanceMeters(window[0].Point, window[^1].Point);
            return across < 40 ? 0 : along / across;
        });

        if (best is not { } window || window.Value < 1.35)
        {
            yield break;
        }

        yield return new Highlight(
            $"{activityId}:trail",
            activityId,
            "trail",
            "Winding trail",
            $"{window.Value:0.00}× the straight line for {Momentum.FormatDuration(points[window.To].Seconds - points[window.From].Seconds)}",
            points[window.From].Seconds,
            points[window.To].Seconds,
            Math.Clamp((window.Value - 1) * 90, 0, 100),
            MedalTier.None,
            "spline");
    }

    #endregion

    #region Behind the wheel

    private static IEnumerable<Highlight> DetectCleanStraights(string activityId, IReadOnlyList<TrackPoint> points)
    {
        int from = 0;
        var straights = new List<(int From, int To, double Length, double Speed)>();

        for (int i = 1; i < points.Count; i++)
        {
            double drift = Math.Abs(Geo.BearingDelta(points[from].HeadingDeg, points[i].HeadingDeg));

            if (drift <= 4 && points[i].SpeedMps > 8)
            {
                continue;
            }

            double length = points[i - 1].DistanceM - points[from].DistanceM;

            if (length >= 500)
            {
                var span = points.Skip(from).Take(i - from).ToList();
                straights.Add((from, i - 1, length, span.Average(p => p.SpeedMps)));
            }

            from = i;
        }

        foreach (var straight in straights.OrderByDescending(s => s.Length * s.Speed).Take(2))
        {
            double index = straight.Length * straight.Speed * 3.6;

            yield return new Highlight(
                $"{activityId}:straight:{straight.From}",
                activityId,
                "clean-straight",
                "Clean straight",
                $"{Momentum.FormatDistance(straight.Length)} arrow-straight at {Momentum.FormatSpeed(straight.Speed)}",
                points[straight.From].Seconds,
                points[straight.To].Seconds,
                Math.Clamp(index / 8000, 0, 100),
                MedalTier.None,
                "move-right");
        }
    }

    /// <summary>
    /// The traffic-light launch. A full stop, then the pull away from it: 0–50 and, where it happens,
    /// 0–100, plus the peak longitudinal g. This is the detector the whole "a pause never drops a
    /// fix" rule in <see cref="TrackRecorder"/> exists to serve.
    /// </summary>
    private static IEnumerable<Highlight> DetectTrafficLightLaunches(string activityId, IReadOnlyList<TrackPoint> points)
    {
        const double stoppedMps = 0.8;
        const double fifty = 50 / 3.6;
        const double hundred = 100 / 3.6;

        var launches = new List<Highlight>();
        int index = 0;

        while (index < points.Count)
        {
            if (points[index].SpeedMps > stoppedMps)
            {
                index++;
                continue;
            }

            int stopStart = index;

            while (index < points.Count && points[index].SpeedMps <= stoppedMps)
            {
                index++;
            }

            double stopSeconds = points[Math.Min(index, points.Count - 1)].Seconds - points[stopStart].Seconds;

            if (stopSeconds < 3 || index >= points.Count)
            {
                continue;
            }

            int launchStart = index;
            double? to50 = null;
            double? to100 = null;
            double peakG = 0;

            for (int i = launchStart; i < points.Count; i++)
            {
                double since = points[i].Seconds - points[launchStart].Seconds;

                if (since > 20)
                {
                    break;
                }

                if (i > launchStart)
                {
                    double dt = points[i].Seconds - points[i - 1].Seconds;

                    if (dt > 0)
                    {
                        peakG = Math.Max(peakG, (points[i].SpeedMps - points[i - 1].SpeedMps) / dt / 9.81);
                    }
                }

                if (to50 == null && points[i].SpeedMps >= fifty)
                {
                    to50 = since;
                }

                if (to100 == null && points[i].SpeedMps >= hundred)
                {
                    to100 = since;
                    break;
                }

                // The rider lifted off again — a queue crawling forward, not a launch.
                if (to50 == null && since > 4 && points[i].SpeedMps < 2)
                {
                    break;
                }
            }

            if (to50 is not { } fiftyTime)
            {
                continue;
            }

            string detail = to100 is { } hundredTime
                ? $"0–50 in {fiftyTime:0.0} s · 0–100 in {hundredTime:0.0} s · {peakG:0.00} g"
                : $"0–50 in {fiftyTime:0.0} s · {peakG:0.00} g";

            launches.Add(new Highlight(
                $"{activityId}:launch:{launchStart}",
                activityId,
                "light-launch",
                "Traffic-light launch",
                detail,
                points[stopStart].Seconds,
                points[Math.Min(points.Count - 1, launchStart + (int)fiftyTime)].Seconds,
                Math.Clamp((7.0 - fiftyTime) / 4.0 * 100, 0, 100),
                MedalTier.None,
                "traffic-cone"));
        }

        // Only the best launch of the drive; six sets of lights would otherwise fill the reel.
        return launches.OrderByDescending(l => l.Score).Take(1);
    }

    /// <summary>
    /// The best corner of the drive, measured from how fast the car was turning rather than from the
    /// shape of three consecutive fixes. At a hundred km/h consecutive fixes are thirty metres apart
    /// and carry several metres of error each, so a circle fitted through them reports whatever the
    /// noise felt like — radii of twenty metres and lateral loads no road car could survive. Yaw rate
    /// from the heading the device reports is smooth, and lateral load falls straight out of it:
    /// a = v · ω.
    /// </summary>
    private static IEnumerable<Highlight> DetectSweepers(string activityId, IReadOnlyList<TrackPoint> points)
    {
        const double windowSeconds = 5;

        double bestG = 0;
        int bestFrom = 0;
        int bestTo = 0;
        double bestRadius = 0;

        for (int i = 0; i < points.Count; i++)
        {
            int to = i;

            while (to + 1 < points.Count && points[to + 1].Seconds - points[i].Seconds <= windowSeconds)
            {
                to++;
            }

            double span = points[to].Seconds - points[i].Seconds;

            if (span < windowSeconds * 0.6)
            {
                continue;
            }

            var window = points.Skip(i).Take(to - i + 1).ToList();

            // The whole window has to be moving, not just its first point. A device reports a heading it
            // is guessing at when the vehicle is barely rolling, and one such sample in the window is
            // enough to invent a corner.
            if (window.Any(p => p.SpeedMps < 8))
            {
                continue;
            }

            double turn = Math.Abs(Geo.BearingDelta(points[i].HeadingDeg, points[to].HeadingDeg));
            double yawRate = Geo.ToRad(turn) / span;
            double speed = window.Average(p => p.SpeedMps);
            double lateralG = speed * yawRate / 9.81;
            double radius = yawRate > 1e-4 ? speed / yawRate : double.PositiveInfinity;

            // Under 30 m it is a junction being turned at, not a corner being carried through.
            if (radius < 30 || radius > 800 || lateralG <= bestG)
            {
                continue;
            }

            bestG = lateralG;
            bestFrom = i;
            bestTo = to;
            bestRadius = radius;
        }

        if (bestG < 0.15)
        {
            yield break;
        }

        yield return new Highlight(
            $"{activityId}:sweeper",
            activityId,
            "sweeper",
            "Best corner",
            $"{bestG:0.00} g through a {bestRadius:0} m radius",
            points[bestFrom].Seconds,
            points[bestTo].Seconds,
            Math.Clamp(bestG / 0.55 * 100, 0, 100),
            MedalTier.None,
            "rotate-3d");
    }

    private static IEnumerable<Highlight> DetectSmoothness(string activityId, IReadOnlyList<TrackPoint> points)
    {
        var jerks = new List<double>();

        for (int i = 2; i < points.Count; i++)
        {
            double dt1 = points[i - 1].Seconds - points[i - 2].Seconds;
            double dt2 = points[i].Seconds - points[i - 1].Seconds;

            if (dt1 <= 0 || dt2 <= 0)
            {
                continue;
            }

            double accelBefore = (points[i - 1].SpeedMps - points[i - 2].SpeedMps) / dt1;
            double accelAfter = (points[i].SpeedMps - points[i - 1].SpeedMps) / dt2;
            jerks.Add((accelAfter - accelBefore) / dt2);
        }

        if (jerks.Count < 30 || points[^1].DistanceM < 3000)
        {
            yield break;
        }

        double rms = Math.Sqrt(jerks.Average(j => j * j));
        double score = Math.Clamp((1 - rms / 2.5) * 100, 0, 100);

        if (score < 30)
        {
            yield break;
        }

        string verdict = score switch
        {
            >= 80 => "a passenger could have read a book",
            >= 60 => "barely a jolt the whole way",
            _ => "a few sharper moments, but tidy overall",
        };

        yield return new Highlight(
            $"{activityId}:smooth",
            activityId,
            "smoothness",
            "Smooth hands",
            $"{rms:0.00} m/s³ average jerk — {verdict}",
            points[0].Seconds,
            points[^1].Seconds,
            score,
            MedalTier.None,
            "hand");
    }

    #endregion

    #region Window helpers

    private static (int From, int To, double Value)? BestWindow(
        IReadOnlyList<TrackPoint> points,
        double windowSeconds,
        Func<IReadOnlyList<TrackPoint>, double> value)
    {
        (int From, int To, double Value)? best = null;
        int from = 0;

        for (int to = 0; to < points.Count; to++)
        {
            while (points[to].Seconds - points[from].Seconds > windowSeconds && from < to - 1)
            {
                from++;
            }

            if (points[to].Seconds - points[from].Seconds < windowSeconds * 0.6)
            {
                continue;
            }

            var window = points.Skip(from).Take(to - from + 1).ToList();
            double score = value(window);

            if (best == null || score > best.Value.Value)
            {
                best = (from, to, score);
            }
        }

        return best;
    }

    /// <summary>The quickest the rider ever covered a given distance, anywhere in the activity.</summary>
    private static (int From, int To, double Seconds)? FastestDistanceWindow(IReadOnlyList<TrackPoint> points, double distanceM)
    {
        (int From, int To, double Seconds)? best = null;
        int from = 0;

        for (int to = 0; to < points.Count; to++)
        {
            while (points[to].DistanceM - points[from].DistanceM > distanceM && from < to)
            {
                from++;
            }

            if (from == 0 || points[to].DistanceM - points[from - 1].DistanceM < distanceM)
            {
                continue;
            }

            double seconds = points[to].Seconds - points[from - 1].Seconds;

            if (seconds > 0 && (best == null || seconds < best.Value.Seconds))
            {
                best = (from - 1, to, seconds);
            }
        }

        return best;
    }

    private static int IndexOfFirst(IReadOnlyList<TrackPoint> points, Func<TrackPoint, bool> predicate)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (predicate(points[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static double Median(double[] values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted.Length == 0 ? 0 : sorted[sorted.Length / 2];
    }

    /// <summary>Ground speed averaged over a window centred on each point, in m/s.</summary>
    private static double[] RollingSpeed(IReadOnlyList<TrackPoint> points, double windowSeconds)
    {
        var smoothed = new double[points.Count];
        int from = 0;
        int to = 0;
        double sum = 0;

        for (int i = 0; i < points.Count; i++)
        {
            while (to < points.Count && points[to].Seconds <= points[i].Seconds + windowSeconds / 2)
            {
                sum += points[to].SpeedMps;
                to++;
            }

            while (points[from].Seconds < points[i].Seconds - windowSeconds / 2)
            {
                sum -= points[from].SpeedMps;
                from++;
            }

            smoothed[i] = sum / Math.Max(1, to - from);
        }

        return smoothed;
    }

    private static (int From, int To)? LongestRun(IReadOnlyList<TrackPoint> points, Func<TrackPoint, int, bool> predicate)
    {
        (int From, int To)? best = null;
        double bestSeconds = 0;
        int start = -1;

        for (int i = 0; i <= points.Count; i++)
        {
            bool inside = i < points.Count && predicate(points[i], i);

            if (inside && start < 0)
            {
                start = i;
            }
            else if (!inside && start >= 0)
            {
                double seconds = points[i - 1].Seconds - points[start].Seconds;

                if (seconds > bestSeconds)
                {
                    bestSeconds = seconds;
                    best = (start, i - 1);
                }

                start = -1;
            }
        }

        return best;
    }

    #endregion
}
