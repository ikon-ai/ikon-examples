/// <summary>What a horse was doing. Finnish names are what a rider actually says.</summary>
public enum Gait
{
    Unknown,

    /// <summary>Käynti — four beats, evenly spaced, no suspension.</summary>
    Walk,

    /// <summary>Ravi — two beats, diagonal pairs, evenly spaced.</summary>
    Trot,

    /// <summary>Laukka — three beats, and the reason this is not a cadence problem.</summary>
    Canter,

    /// <summary>Neliravi — four beats, unevenly spaced.</summary>
    Gallop,
}

/// <summary>A stretch of one gait.</summary>
public sealed record GaitSegment(Gait Gait, double StartSeconds, double EndSeconds, double BeatsPerMinute, double Confidence)
{
    public double DurationSeconds => EndSeconds - StartSeconds;
}

/// <summary>
/// Works out which gait a horse was in, from the beats rather than from the speed.
/// </summary>
/// <remarks>
/// Speed cannot do this. A collected canter and an extended trot cover ground at the same rate, and
/// that is exactly the boundary a rider cares about. What separates them is how a stride divides:
/// a trot is two evenly spaced beats, a canter is three unevenly spaced ones, a gallop four. That
/// structure survives being felt through a saddle and a rider, and it is in the accelerometer.
///
/// So the method is to find the footfalls, take the intervals between them, and look for the pattern
/// those intervals repeat with. Evenly spaced intervals mean a symmetric gait — walk or trot, which
/// the beat rate then separates. Intervals repeating in threes mean a canter. Fours, unevenly, a
/// gallop.
///
/// **The thresholds come from what the gaits are, not from fitted data.** No labelled riding was
/// available to tune them against, so treat the output as a well-founded reading rather than a
/// measured fact, and expect the boundaries to move once real rides can be checked against it. What
/// is deliberately avoided is the alternative — classifying on speed bands, which is not a weaker
/// version of this but simply the wrong instrument.
/// See platform-dotnet/Ikon.App.Examples.Momentum/horse-gait-from-rider-phone-research.md.
/// </remarks>
public static class GaitAnalysis
{
    /// <summary>Nothing living puts two feet down closer together than this.</summary>
    private const double MinBeatGapSeconds = 0.14;

    /// <summary>How much signal each decision sees. Long enough to hold several strides of a walk.</summary>
    private const double WindowSeconds = 4.0;

    private const double HopSeconds = 1.0;

    /// <summary>Below this a segment is a flicker between two real ones, not a gait.</summary>
    private const double MinSegmentSeconds = 2.5;

    private static readonly Gait[] States = [Gait.Unknown, Gait.Walk, Gait.Trot, Gait.Canter, Gait.Gallop];

    /// <summary>
    /// Splits an outing into stretches of gait. Empty when there is not enough signal to say.
    /// </summary>
    public static IReadOnlyList<GaitSegment> Segment(IReadOnlyList<RecordedFix> fixes, IReadOnlyList<MotionSample> motion)
    {
        var accel = motion.Where(s => s.Sensor == MotionSensors.UserAcceleration).ToArray();

        if (accel.Length < 200 || fixes.Count < 5)
        {
            return [];
        }

        double originMs = accel[0].AtMillis;
        var beats = DetectBeats(accel);

        if (beats.Count < 8)
        {
            return [];
        }

        double spanSeconds = (accel[^1].AtMillis - originMs) / 1000.0;
        var windows = new List<(double At, double[] Scores, double Rate)>();

        for (double start = 0; start + WindowSeconds <= spanSeconds; start += HopSeconds)
        {
            var inWindow = beats.Where(b => b >= start && b < start + WindowSeconds).ToArray();
            windows.Add((start + WindowSeconds / 2, ScoreWindow(inWindow, SpeedAt(fixes, originMs, start + WindowSeconds / 2)), BeatRate(inWindow)));
        }

        if (windows.Count == 0)
        {
            return [];
        }

        var path = Viterbi(windows.Select(w => w.Scores).ToList());

        return Coalesce(windows, path);
    }

    /// <summary>
    /// Finds footfalls: the crest of each impulse that rises clear of a drifting baseline.
    /// </summary>
    /// <remarks>
    /// The threshold rides on an exponential baseline and envelope rather than sitting at a fixed
    /// number of m/s², because a walk's footfall is an order of magnitude softer than a gallop's and
    /// no single number finds both.
    /// </remarks>
    public static List<double> DetectBeats(IReadOnlyList<MotionSample> accel)
    {
        var beats = new List<double>();
        double originMs = accel[0].AtMillis;
        double baseline = accel[0].Magnitude;
        double envelope = 1.0;
        double previous = baseline;
        double lastBeat = double.NegativeInfinity;
        bool rising = false;

        foreach (var sample in accel)
        {
            double magnitude = sample.Magnitude;
            baseline += (magnitude - baseline) * 0.02;
            envelope += (Math.Abs(magnitude - baseline) - envelope) * 0.01;

            double threshold = baseline + Math.Max(0.3, envelope * 1.2);
            double at = (sample.AtMillis - originMs) / 1000.0;

            if (magnitude > threshold && magnitude > previous)
            {
                rising = true;
            }
            else if (rising && magnitude < previous)
            {
                rising = false;

                if (at - lastBeat >= MinBeatGapSeconds)
                {
                    lastBeat = at;
                    beats.Add(at);
                }
            }

            previous = magnitude;
        }

        return beats;
    }

    /// <summary>
    /// How well one window's beats fit each gait, as a score per state.
    /// </summary>
    /// <remarks>
    /// The intervals between beats are the evidence. A symmetric gait leaves them all alike; a canter
    /// leaves them repeating every third; a gallop every fourth and unevenly.
    ///
    /// That settles canter and gallop but not walk against trot, since both are perfectly even. Those
    /// two separate on ground covered per footfall — stride length over beats per stride — which is
    /// about 0.45 m walking and 1.3 m trotting. Beat rate cannot do it: a walk puts down four feet a
    /// stride against a trot's two, so it beats FASTER while travelling a third of the speed, and the
    /// two rates overlap outright.
    /// </remarks>
    private static double[] ScoreWindow(double[] beats, double speedMps)
    {
        var scores = new double[States.Length];

        if (beats.Length < 5)
        {
            scores[0] = 1;
            return scores;
        }

        var intervals = new double[beats.Length - 1];

        for (int i = 0; i < intervals.Length; i++)
        {
            intervals[i] = beats[i + 1] - beats[i];
        }

        double mean = intervals.Average();

        if (mean <= 0)
        {
            scores[0] = 1;
            return scores;
        }

        double variation = Math.Sqrt(intervals.Sum(v => (v - mean) * (v - mean)) / intervals.Length) / mean;
        double rate = 60.0 / mean;

        // Evenness, and how strongly the intervals repeat every third and every fourth.
        double even = Math.Exp(-variation * variation / 0.02);
        double threes = PatternStrength(intervals, 3);
        double fours = PatternStrength(intervals, 4);

        // Ground covered per footfall — stride length divided by beats per stride. This is what
        // separates the two EVEN gaits, and beat rate is not: a walk puts down four feet a stride and
        // a trot two, so a walk actually beats FASTER than a trot while going a third of the speed.
        // Per beat, a walk covers about 0.45 m and a trot about 1.3 — a threefold gap where the beat
        // rates overlap outright.
        double perBeat = speedMps > 0.3 && rate > 0 ? speedMps / (rate / 60.0) : 0;

        scores[1] = even * (perBeat > 0 ? Fit(perBeat, 0.5, 0.25) : RateFit(rate, 210, 70));
        scores[2] = even * (perBeat > 0 ? Fit(perBeat, 1.3, 0.5) : RateFit(rate, 165, 55));
        scores[3] = threes * (1 - even * 0.5) * SpeedFit(speedMps, 6.0, 3.0);
        scores[4] = fours * (1 - even * 0.5) * SpeedFit(speedMps, 11.0, 4.0);
        scores[0] = 0.06;

        double total = scores.Sum();

        if (total <= 0)
        {
            scores[0] = 1;
            return scores;
        }

        for (int i = 0; i < scores.Length; i++)
        {
            scores[i] /= total;
        }

        return scores;
    }

    /// <summary>
    /// How strongly the interval sequence repeats with the given period — a canter's short-short-long
    /// against a trot's flat run of equals.
    /// </summary>
    private static double PatternStrength(double[] intervals, int period)
    {
        if (intervals.Length < period * 2)
        {
            return 0;
        }

        double mean = intervals.Average();
        double variance = intervals.Sum(v => (v - mean) * (v - mean)) / intervals.Length;

        // A flat sequence repeats at every period equally well, so a pattern only means something when
        // there is something to pattern. This is what stops a trot scoring as a canter.
        if (variance < 1e-6 || Math.Sqrt(variance) / mean < 0.06)
        {
            return 0;
        }

        double sum = 0;
        int count = 0;

        for (int i = 0; i + period < intervals.Length; i++)
        {
            sum += (intervals[i] - mean) * (intervals[i + period] - mean);
            count++;
        }

        return count == 0 ? 0 : Math.Clamp(sum / count / variance, 0, 1);
    }

    private static double RateFit(double rate, double centre, double width)
        => Math.Exp(-Math.Pow(rate - centre, 2) / (2 * width * width));

    private static double Fit(double value, double centre, double width)
        => Math.Exp(-Math.Pow(value - centre, 2) / (2 * width * width));

    /// <summary>
    /// A soft nudge, never a gate. Speed genuinely cannot separate the gaits that overlap — that is
    /// the whole reason for measuring the beat — so this is broad enough to inform and too broad to
    /// decide.
    /// </summary>
    private static double SpeedFit(double speedMps, double centre, double width)
        => speedMps <= 0 ? 1 : 0.35 + 0.65 * Math.Exp(-Math.Pow(speedMps - centre, 2) / (2 * width * width));

    private static double SpeedAt(IReadOnlyList<RecordedFix> fixes, double originMs, double seconds)
    {
        double target = originMs + seconds * 1000;
        var nearest = fixes[0];

        foreach (var candidate in fixes)
        {
            if (Math.Abs(candidate.AtMillis - target) < Math.Abs(nearest.AtMillis - target))
            {
                nearest = candidate;
            }
        }

        return Math.Abs(nearest.AtMillis - target) > 4000 ? 0 : nearest.SpeedMps;
    }

    private static double BeatRate(double[] beats)
    {
        if (beats.Length < 2)
        {
            return 0;
        }

        return 60.0 * (beats.Length - 1) / (beats[^1] - beats[0]);
    }

    /// <summary>
    /// Picks the most likely run of gaits over the whole outing rather than the best guess for each
    /// window on its own.
    /// </summary>
    /// <remarks>
    /// Gaits do not change arbitrarily: a horse goes walk to trot to canter and back, rarely walk
    /// straight to canter, and it holds one for seconds at a time. Scoring whole paths lets a
    /// confident neighbour on each side outvote a single ambiguous window in the middle — which no
    /// per-window decision can do, however good its features are.
    /// </remarks>
    private static Gait[] Viterbi(List<double[]> windows)
    {
        int n = windows.Count;
        var best = new double[n, States.Length];
        var from = new int[n, States.Length];

        for (int s = 0; s < States.Length; s++)
        {
            best[0, s] = Math.Log(windows[0][s] + 1e-9);
        }

        for (int i = 1; i < n; i++)
        {
            for (int s = 0; s < States.Length; s++)
            {
                double top = double.NegativeInfinity;
                int argTop = 0;

                for (int p = 0; p < States.Length; p++)
                {
                    double score = best[i - 1, p] + Math.Log(Transition(p, s));

                    if (score > top)
                    {
                        top = score;
                        argTop = p;
                    }
                }

                best[i, s] = top + Math.Log(windows[i][s] + 1e-9);
                from[i, s] = argTop;
            }
        }

        var path = new Gait[n];
        int cursor = 0;

        for (int s = 1; s < States.Length; s++)
        {
            if (best[n - 1, s] > best[n - 1, cursor])
            {
                cursor = s;
            }
        }

        for (int i = n - 1; i >= 0; i--)
        {
            path[i] = States[cursor];
            cursor = from[i, cursor];
        }

        return path;
    }

    /// <summary>
    /// How plausible one gait following another is. Staying put is much the likeliest thing; stepping
    /// to a neighbouring gait is ordinary; skipping one is not.
    /// </summary>
    private static double Transition(int from, int to)
    {
        if (from == to)
        {
            return 0.90;
        }

        if (from == 0 || to == 0)
        {
            return 0.04;
        }

        return Math.Abs(from - to) == 1 ? 0.05 : 0.01;
    }

    private static List<GaitSegment> Coalesce(List<(double At, double[] Scores, double Rate)> windows, Gait[] path)
    {
        var segments = new List<GaitSegment>();
        int start = 0;

        for (int i = 1; i <= path.Length; i++)
        {
            if (i < path.Length && path[i] == path[start])
            {
                continue;
            }

            var gait = path[start];

            if (gait != Gait.Unknown)
            {
                double from = windows[start].At - WindowSeconds / 2;
                double to = windows[i - 1].At + WindowSeconds / 2;
                var rates = windows.Skip(start).Take(i - start).Select(w => w.Rate).Where(r => r > 0).ToArray();
                double confidence = windows.Skip(start).Take(i - start).Average(w => w.Scores[Array.IndexOf(States, gait)]);

                if (to - from >= MinSegmentSeconds)
                {
                    segments.Add(new GaitSegment(gait, from, to, rates.Length > 0 ? rates.Average() : 0, confidence));
                }
            }

            start = i;
        }

        return segments;
    }
}
