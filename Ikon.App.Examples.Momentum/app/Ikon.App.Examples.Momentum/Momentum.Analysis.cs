/// <summary>A unit vector in the device's own axes.</summary>
public readonly record struct Axis(double X, double Y, double Z)
{
    public double Dot(MotionSample sample) => X * sample.X + Y * sample.Y + Z * sample.Z;

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Axis Normalized()
    {
        double length = Length;
        return length < 1e-9 ? new Axis(0, 0, 0) : new Axis(X / length, Y / length, Z / length);
    }

    public Axis Cross(Axis other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);
}

/// <summary>
/// Where the vehicle's forward and lateral directions sit in the device's own axes.
/// </summary>
/// <param name="Forward">The direction the outing travels in.</param>
/// <param name="Lateral">Across it, positive to one side; which side is not determined and does not matter.</param>
/// <param name="Confidence">0–1. Below about 0.3 the fit found nothing and the axes are a guess.</param>
public readonly record struct DeviceFrame(Axis Forward, Axis Lateral, double Confidence);

/// <summary>What the motion stream says about an outing, once its axes are known.</summary>
public sealed record MotionInsights(
    DeviceFrame Frame,
    double PeakBrakingG,
    double PeakAccelG,
    double PeakLateralG,
    double PeakCombinedG,
    double JerkRms,
    double BeatsPerMinute,
    double RhythmStrength,
    int SampleCount);

/// <summary>
/// Reads the motion an outing recorded, after it is over.
///
/// GPS answers where and how fast. It cannot answer how — whether a corner was smooth or snatched,
/// whether a stop was a squeeze or a stamp, whether a horse was trotting or cantering. All of that
/// is in the accelerometer, and none of it is in a speed trace.
/// </summary>
/// <remarks>
/// This runs at the finish over the whole recording rather than live, because it needs the outing in
/// one piece: the axes are fitted against the entire ride, and a gait is placed far better with what
/// came after it than without.
/// </remarks>
public static class MotionAnalysis
{
    /// <summary>Below this many samples there is nothing worth fitting.</summary>
    private const int MinSamples = 200;

    /// <summary>Standard gravity, for reporting accelerations the way anyone driving would say them.</summary>
    private const double G = 9.80665;

    public static MotionInsights? Analyze(IReadOnlyList<RecordedFix> fixes, IReadOnlyList<MotionSample> motion, ActivityKind kind)
    {
        var accel = motion.Where(s => s.Sensor == MotionSensors.UserAcceleration).ToArray();

        if (accel.Length < MinSamples || fixes.Count < 10)
        {
            return null;
        }

        // Vehicle dynamics live below about half a hertz; road vibration, engine and a phone shifting
        // in a cup holder live far above it and are much larger. Correlating or peak-finding on the
        // raw signal measures the road, not the driving — which is why a real motorway drive fitted
        // its axes at 0.13 confidence while clean synthetic traces fitted at 0.99.
        var steady = LowPass(accel, 1.0);
        var frame = FitFrame(fixes, steady, LowPass(motion, 1.0));

        double peakBraking = 0;
        double peakAccel = 0;
        double peakLateral = 0;
        double peakCombined = 0;

        if (frame.Confidence > 0.3)
        {
            foreach (var sample in steady)
            {
                double longitudinal = frame.Forward.Dot(sample) / G;
                double lateral = Math.Abs(frame.Lateral.Dot(sample)) / G;

                peakBraking = Math.Max(peakBraking, -longitudinal);
                peakAccel = Math.Max(peakAccel, longitudinal);
                peakLateral = Math.Max(peakLateral, lateral);

                // The traction circle: tyres spend one budget on turning and stopping together, so the
                // combined number is the one that says how close to the limit the outing actually got.
                peakCombined = Math.Max(peakCombined, Math.Sqrt(longitudinal * longitudinal + lateral * lateral));
            }
        }

        var (beats, strength) = Rhythm(accel);

        return new MotionInsights(
            frame,
            Smooth(peakBraking),
            Smooth(peakAccel),
            Smooth(peakLateral),
            Smooth(peakCombined),
            JerkRms(accel),
            beats,
            strength,
            accel.Length);
    }

    /// <summary>
    /// Works out which way the device was facing, by asking which direction in its axes best explains
    /// what GPS already knows.
    /// </summary>
    /// <remarks>
    /// A phone in a pocket or a cup holder sits at whatever angle it landed at, so its x, y and z mean
    /// nothing on their own. But the outing has a second, independent account of itself: speed and
    /// heading from GPS. Differentiate those and you have the longitudinal acceleration and the yaw
    /// rate, in the world's frame, at one hertz.
    ///
    /// The axis that best matches each is then a least-squares projection, and the closed form is
    /// simply the correlation vector normalised — no search and nothing to converge. Forward comes
    /// from acceleration against speed change; up comes from the gyroscope against heading change;
    /// lateral is their cross product.
    /// </remarks>
    public static DeviceFrame FitFrame(IReadOnlyList<RecordedFix> fixes, IReadOnlyList<MotionSample> accel, IReadOnlyList<MotionSample> motion)
    {
        var forward = Project(accel, SampleReference(fixes, static (a, b, dt) => (b.SpeedMps - a.SpeedMps) / dt));
        var gyro = motion.Where(s => s.Sensor == MotionSensors.Gyroscope).ToArray();
        var up = gyro.Length >= MinSamples
            ? Project(gyro, SampleReference(fixes, static (a, b, dt) => Geo.BearingDelta(a.Heading, b.Heading) * Math.PI / 180 / dt))
            : (Axis: new Axis(0, 0, 0), Confidence: 0.0);

        var forwardAxis = forward.Axis.Normalized();
        var upAxis = up.Axis.Normalized();
        var lateral = forwardAxis.Cross(upAxis).Normalized();

        if (lateral.Length < 0.5)
        {
            // Forward and up came out parallel, which means one of the two fits found nothing. There is
            // no lateral direction to be had; the longitudinal one may still be good.
            return new DeviceFrame(forwardAxis, new Axis(0, 0, 0), forward.Confidence * 0.5);
        }

        return new DeviceFrame(forwardAxis, lateral, Math.Min(forward.Confidence, Math.Max(up.Confidence, 0.35)));
    }

    /// <summary>
    /// The closed-form least-squares axis: the direction whose projection best tracks the reference,
    /// with the correlation it achieves.
    /// </summary>
    private static (Axis Axis, double Confidence) Project(IReadOnlyList<MotionSample> samples, IReadOnlyList<(double AtMillis, double Value)> reference)
    {
        if (reference.Count < 5)
        {
            return (new Axis(0, 0, 0), 0);
        }

        // The reference is mean-centred: a constant offset in it would drag the fitted axis toward
        // whatever direction the sensor happens to sit at rest.
        double referenceMean = reference.Average(r => r.Value);

        double sx = 0, sy = 0, sz = 0;
        int matched = 0;

        foreach (var (sample, value) in Pairs(samples, reference, referenceMean))
        {
            sx += sample.X * value;
            sy += sample.Y * value;
            sz += sample.Z * value;
            matched++;
        }

        if (matched < MinSamples)
        {
            return (new Axis(0, 0, 0), 0);
        }

        var axis = new Axis(sx, sy, sz).Normalized();

        if (axis.Length < 0.5)
        {
            return (new Axis(0, 0, 0), 0);
        }

        // Confidence is how well the signal ALONG the fitted axis tracks the reference — not how much
        // of the sensor's total energy happens to lie on it. The earlier form divided by the full
        // three-axis energy, so motion across the axis, drift and residual vibration all counted
        // against a fit that was perfectly good: a real drive scored 0.13 with its axes essentially
        // correct, and every measurement downstream was suppressed as untrustworthy.
        // Measured only where there was something to track. Most of a drive is cruising: the reference
        // is ~0 while the accelerometer still carries vibration and steering, so those samples dilute
        // the correlation however correct the axis is. Judging the fit on the moments the vehicle
        // actually changed speed asks the question that matters — and a 62 km motorway run scored
        // 0.26 across the whole ride with axes that turned out to be fine.
        double active = Math.Max(0.4, reference.Select(r => Math.Abs(r.Value - referenceMean)).OrderBy(v => v).ElementAt((int)(reference.Count * 0.8)));

        double sumPr = 0, sumPp = 0, sumRr = 0, sumP = 0, sumR = 0;
        int count = 0;

        foreach (var (sample, value) in Pairs(samples, reference, referenceMean))
        {
            if (Math.Abs(value) < active)
            {
                continue;
            }

            double projected = axis.Dot(sample);
            sumPr += projected * value;
            sumPp += projected * projected;
            sumRr += value * value;
            sumP += projected;
            sumR += value;
            count++;
        }

        double covariance = sumPr / count - sumP / count * (sumR / count);
        double varianceP = sumPp / count - Math.Pow(sumP / count, 2);
        double varianceR = sumRr / count - Math.Pow(sumR / count, 2);

        if (count < 50 || varianceP <= 1e-12 || varianceR <= 1e-12)
        {
            return (axis, 0);
        }

        return (axis, Math.Clamp(Math.Abs(covariance) / Math.Sqrt(varianceP * varianceR), 0, 1));
    }

    /// <summary>
    /// Each sample paired with the reference value covering its moment. The reference is one value a
    /// second and the samples are fifty, so they are matched rather than resampled onto a grid.
    /// </summary>
    private static IEnumerable<(MotionSample Sample, double Value)> Pairs(
        IReadOnlyList<MotionSample> samples,
        IReadOnlyList<(double AtMillis, double Value)> reference,
        double referenceMean)
    {
        int cursor = 0;

        foreach (var sample in samples)
        {
            while (cursor + 1 < reference.Count && reference[cursor + 1].AtMillis <= sample.AtMillis)
            {
                cursor++;
            }

            if (Math.Abs(reference[cursor].AtMillis - sample.AtMillis) <= 1500)
            {
                yield return (sample, reference[cursor].Value - referenceMean);
            }
        }
    }

    /// <summary>Differentiates a per-fix quantity, dropping the gaps a stalled signal leaves behind.</summary>
    private static List<(double AtMillis, double Value)> SampleReference(
        IReadOnlyList<RecordedFix> fixes,
        Func<RecordedFix, RecordedFix, double, double> derive)
    {
        var reference = new List<(double, double)>(fixes.Count);

        for (int i = 1; i < fixes.Count; i++)
        {
            double dt = (fixes[i].AtMillis - fixes[i - 1].AtMillis) / 1000.0;

            // A gap is not a measurement. Differentiating across one invents an acceleration that
            // never happened, and it would then be fitted to whatever the phone was doing at the time.
            if (dt < 0.2 || dt > 3)
            {
                continue;
            }

            reference.Add((fixes[i].AtMillis, derive(fixes[i - 1], fixes[i], dt)));
        }

        return reference;
    }

    /// <summary>
    /// Finds the outing's beat — footfalls, hoofbeats or pedal strokes a minute — and how regular it is.
    /// </summary>
    /// <remarks>
    /// Autocorrelation of the acceleration magnitude rather than a Fourier transform, because what is
    /// wanted is the period itself, and a footfall is a sharp impulse whose energy spreads across many
    /// harmonics — a spectrum shows the harmonics, the autocorrelation shows the beat.
    ///
    /// It reports the BEAT, not the stride, and the distinction is not a detail. A trot is two
    /// identical half-strides, so its signal repeats twice per stride and no autocorrelation can tell
    /// which of the two periods is "the" stride — they are equally good answers. Reporting the beat
    /// avoids inventing a stride the measurement does not support.
    ///
    /// <c>Strength</c> is how much of the signal that beat explains: a steady trot is high, a horse
    /// changing its mind is low, a phone rattling in a door pocket is near zero.
    ///
    /// **This does not classify gaits.** Separating ravi from laukka needs the beat STRUCTURE —
    /// how a stride divides — and the honest position is that a classifier for it cannot be written
    /// without labelled rides to check it against, which is the gap
    /// docs/private/research/horse-gait-from-rider-phone-research.md is about. What is here is the
    /// input such a classifier would take, measured and testable, and nothing beyond it.
    /// </remarks>
    public static (double PerMinute, double Strength) Rhythm(IReadOnlyList<MotionSample> accel)
    {
        if (accel.Count < MinSamples)
        {
            return (0, 0);
        }

        double spanSeconds = (accel[^1].AtMillis - accel[0].AtMillis) / 1000.0;

        if (spanSeconds <= 1)
        {
            return (0, 0);
        }

        double hertz = accel.Count / spanSeconds;
        var signal = new double[accel.Count];
        double mean = 0;

        for (int i = 0; i < accel.Count; i++)
        {
            signal[i] = accel[i].Magnitude;
            mean += signal[i];
        }

        mean /= signal.Length;

        for (int i = 0; i < signal.Length; i++)
        {
            signal[i] -= mean;
        }

        // Anything from a horse's walk to a sprinter's stride lives inside this band.
        int minLag = Math.Max(2, (int)(hertz * 0.25));
        int maxLag = Math.Min(signal.Length / 2, (int)(hertz * 2.0));

        if (maxLag <= minLag)
        {
            return (0, 0);
        }

        double zero = Correlate(signal, 0);

        if (zero < 1e-9)
        {
            return (0, 0);
        }

        var correlations = new double[maxLag + 1];
        double strongest = 0;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            correlations[lag] = Correlate(signal, lag);
            strongest = Math.Max(strongest, correlations[lag]);
        }

        if (strongest / zero < 0.1)
        {
            return (0, 0);
        }

        // The FIRST strong peak, not the strongest. A periodic signal correlates just as well at
        // twice the stride as at the stride, so taking the global maximum reports half the real
        // cadence about as often as not — which reads as a plausible number and is simply wrong.
        int bestLag = 0;
        double best = 0;

        for (int lag = minLag + 1; lag < maxLag; lag++)
        {
            bool isPeak = correlations[lag] >= correlations[lag - 1] && correlations[lag] >= correlations[lag + 1];

            if (isPeak && correlations[lag] >= strongest * 0.85)
            {
                bestLag = lag;
                best = correlations[lag];
                break;
            }
        }

        if (bestLag == 0)
        {
            return (0, 0);
        }

        return (60.0 * hertz / bestLag, Math.Clamp(best / zero, 0, 1));
    }

    /// <summary>
    /// Averages each axis over a window, leaving only what the vehicle or body did.
    /// </summary>
    /// <remarks>
    /// A boxcar rather than an exponential filter, because it is zero-phase: an exponential one lags,
    /// and a lag between the accelerometer and the GPS it is being fitted against is exactly the
    /// error this whole step exists to avoid.
    ///
    /// Not used for <see cref="Rhythm"/>, where the high frequencies ARE the measurement — a footfall
    /// is an impulse and smoothing it away leaves nothing to find.
    /// </remarks>
    private static MotionSample[] LowPass(IReadOnlyList<MotionSample> samples, double seconds)
    {
        if (samples.Count < 4)
        {
            return [.. samples];
        }

        double span = (samples[^1].AtMillis - samples[0].AtMillis) / 1000.0;
        int half = span <= 0 ? 0 : (int)(samples.Count / span * seconds / 2);

        if (half < 1)
        {
            return [.. samples];
        }

        var result = new MotionSample[samples.Count];

        // A running sum, so the cost does not grow with the window. Sensors are interleaved in this
        // list, so the window is taken per sensor to avoid averaging a gyroscope into an accelerometer.
        for (int i = 0; i < samples.Count; i++)
        {
            double x = 0, y = 0, z = 0;
            int count = 0;

            for (int j = Math.Max(0, i - half); j <= Math.Min(samples.Count - 1, i + half); j++)
            {
                if (samples[j].Sensor != samples[i].Sensor)
                {
                    continue;
                }

                x += samples[j].X;
                y += samples[j].Y;
                z += samples[j].Z;
                count++;
            }

            result[i] = count == 0
                ? samples[i]
                : samples[i] with { X = x / count, Y = y / count, Z = z / count };
        }

        return result;
    }

    private static double Correlate(double[] signal, int lag)
    {
        double sum = 0;

        for (int i = 0; i + lag < signal.Length; i++)
        {
            sum += signal[i] * signal[i + lag];
        }

        return sum / (signal.Length - lag);
    }

    /// <summary>
    /// How abruptly the acceleration itself changed — the difference between a squeeze and a stamp.
    /// </summary>
    private static double JerkRms(IReadOnlyList<MotionSample> accel)
    {
        double sum = 0;
        int count = 0;

        for (int i = 1; i < accel.Count; i++)
        {
            double dt = (accel[i].AtMillis - accel[i - 1].AtMillis) / 1000.0;

            if (dt is <= 0 or > 0.2)
            {
                continue;
            }

            double dx = (accel[i].X - accel[i - 1].X) / dt;
            double dy = (accel[i].Y - accel[i - 1].Y) / dt;
            double dz = (accel[i].Z - accel[i - 1].Z) / dt;

            sum += dx * dx + dy * dy + dz * dz;
            count++;
        }

        return count == 0 ? 0 : Math.Sqrt(sum / count);
    }

    /// <summary>
    /// Drops the single wildest sample.
    /// </summary>
    /// <remarks>
    /// A phone slipping in a pocket, knocked by a hand, or set down registers a spike far beyond
    /// anything the vehicle did, and a peak taken raw would report that spike as the outing's hardest
    /// braking. Rounding to two decimals is not what this does — it caps the absurd.
    /// </remarks>
    private static double Smooth(double peak) => peak > 3.5 ? 3.5 : Math.Round(peak, 2);
}

/// <summary>
/// Turns what the motion stream measured into things worth showing.
/// </summary>
/// <remarks>
/// These are deliberately separate from the GPS detectors rather than replacing them. A speed trace
/// can infer that a corner happened and roughly how hard; the accelerometer measured it. Where both
/// speak, the measured one is the one to believe — but the inferred ones still cover every outing
/// recorded by a client that has no motion stream at all.
/// </remarks>
public static class MotionHighlights
{
    public static IEnumerable<Highlight> From(string activityId, ActivityKind kind, MotionInsights insights, IReadOnlyList<GaitSegment> gaits, double elapsedSeconds)
    {
        bool axesUsable = insights.Frame.Confidence > 0.3;

        if (axesUsable && insights.PeakBrakingG >= 0.25)
        {
            yield return new Highlight(
                $"{activityId}:motion-braking",
                activityId,
                "motion-braking",
                "Hardest stop",
                $"{insights.PeakBrakingG:0.00} g — measured, not inferred from speed",
                0,
                elapsedSeconds,
                Math.Clamp(insights.PeakBrakingG / 1.0 * 100, 0, 100),
                MedalTier.None,
                "octagon-x");
        }

        if (axesUsable && insights.Frame.Lateral.Length > 0.5 && insights.PeakLateralG >= 0.2)
        {
            yield return new Highlight(
                $"{activityId}:motion-corner",
                activityId,
                "motion-corner",
                "Cornering load",
                $"{insights.PeakLateralG:0.00} g through the fastest corner",
                0,
                elapsedSeconds,
                Math.Clamp(insights.PeakLateralG / 0.9 * 100, 0, 100),
                MedalTier.None,
                "rotate-3d");
        }

        // Turning and stopping share one budget of grip, so what they came to together says more about
        // how close the outing got to the limit than either number does alone.
        if (kind == ActivityKind.Car && axesUsable && insights.PeakCombinedG >= 0.35)
        {
            yield return new Highlight(
                $"{activityId}:motion-traction",
                activityId,
                "motion-traction",
                "Peak grip used",
                $"{insights.PeakCombinedG:0.00} g combined, braking and cornering together",
                0,
                elapsedSeconds,
                Math.Clamp(insights.PeakCombinedG / 1.1 * 100, 0, 100),
                MedalTier.None,
                "circle-dot-dashed");
        }

        foreach (var segment in gaits.Where(g => g.DurationSeconds >= 20).OrderByDescending(g => g.DurationSeconds).Take(3))
        {
            string name = segment.Gait switch
            {
                Gait.Walk => "Käynti",
                Gait.Trot => "Ravi",
                Gait.Canter => "Laukka",
                Gait.Gallop => "Neliravi",
                _ => "",
            };

            yield return new Highlight(
                $"{activityId}:gait-{segment.Gait}-{segment.StartSeconds:0}",
                activityId,
                $"gait-{segment.Gait}".ToLowerInvariant(),
                $"{name} held",
                $"{Momentum.FormatDuration(segment.DurationSeconds)} at {segment.BeatsPerMinute:0} beats a minute",
                segment.StartSeconds,
                segment.EndSeconds,
                Math.Clamp(segment.Confidence * 100, 0, 100),
                MedalTier.None,
                "waves");
        }

        // A beat is only worth reporting when the signal actually has one; a phone in a door pocket
        // produces a number here just as readily as a horse does, and the strength is what tells them
        // apart.
        if (kind != ActivityKind.Car && insights.BeatsPerMinute > 0 && insights.RhythmStrength > 0.25)
        {
            string what = kind == ActivityKind.Horse ? "hoofbeats" : kind == ActivityKind.Bike ? "pedal strokes" : "footfalls";

            yield return new Highlight(
                $"{activityId}:motion-beat",
                activityId,
                "motion-beat",
                "Rhythm held",
                $"{insights.BeatsPerMinute:0} {what} a minute",
                0,
                elapsedSeconds,
                Math.Clamp(insights.RhythmStrength * 100, 0, 100),
                MedalTier.None,
                "activity");
        }
    }
}
