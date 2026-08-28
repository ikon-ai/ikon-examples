namespace Ikon.App.Examples.Momentum.Test;

/// <summary>
/// The frame fit is the part worth testing hardest: it turns a phone's arbitrary axes into forward
/// and lateral, and everything reported downstream is a projection onto them. Get it wrong and the
/// numbers stay entirely plausible — a braking figure, a cornering figure — while measuring the
/// wrong direction, which is a far worse failure than a crash.
/// </summary>
public class AnalysisTests
{
    private const double G = 9.80665;

    /// <summary>A phone lying at some arbitrary angle, which is the normal case.</summary>
    private static readonly Axis PocketForward = new Axis(0.48, -0.62, 0.62).Normalized();
    private static readonly Axis PocketUp = new Axis(0.62, 0.75, 0.25).Normalized();

    /// <summary>
    /// Builds an outing that accelerates, holds, brakes, and turns — with the motion trace generated
    /// from the same profile the fixes are generated from, projected onto a tilted phone.
    /// </summary>
    private static (List<RecordedFix> Fixes, List<MotionSample> Motion) Outing(
        Func<double, double> longitudinalG,
        Func<double, double> yawRateDegPerSec,
        double seconds = 120)
    {
        const double baseMs = 1_700_000_000_000;
        var fixes = new List<RecordedFix>();
        var motion = new List<MotionSample>();

        // Speed as a function of time, shared by both loops. Reading it off the end of the fix loop
        // would give the motion trace one constant — the speed the outing finished at.
        double SpeedAt(double at)
        {
            double v = 12;

            for (double t = 0; t < at; t += 1)
            {
                v = Math.Clamp(v + longitudinalG(t) * G, 3, 60);
            }

            return v;
        }

        double heading = 90;

        for (int i = 0; i <= (int)seconds; i++)
        {
            double t = i;
            heading = (heading + yawRateDegPerSec(t) + 360) % 360;
            fixes.Add(new RecordedFix(baseMs + t * 1000, 61.5 + t * 1e-4, 23.8, 4, SpeedAt(t + 1), heading, 100));
        }

        // 50 Hz, the rate the app records at.
        for (int i = 0; i < seconds * 50; i++)
        {
            double t = i / 50.0;
            double along = longitudinalG(t) * G;
            double across = yawRateDegPerSec(t) * Math.PI / 180 * SpeedAt(t);

            var lateral = PocketForward.Cross(PocketUp).Normalized();

            motion.Add(new MotionSample(
                baseMs + t * 1000,
                PocketForward.X * along + lateral.X * across,
                PocketForward.Y * along + lateral.Y * across,
                PocketForward.Z * along + lateral.Z * across,
                MotionSensors.UserAcceleration));

            motion.Add(new MotionSample(
                baseMs + t * 1000,
                PocketUp.X * yawRateDegPerSec(t) * Math.PI / 180,
                PocketUp.Y * yawRateDegPerSec(t) * Math.PI / 180,
                PocketUp.Z * yawRateDegPerSec(t) * Math.PI / 180,
                MotionSensors.Gyroscope));
        }

        return (fixes, motion);
    }

    [Fact]
    public void The_forward_axis_is_recovered_from_a_tilted_phone()
    {
        // Alternating gentle acceleration and braking, no turning.
        var (fixes, motion) = Outing(t => Math.Sin(t / 6) * 0.15, _ => 0);

        var frame = MotionAnalysis.FitFrame(fixes, [.. motion.Where(s => s.Sensor == MotionSensors.UserAcceleration)], motion);

        // Recovered up to sign — which way is "forward" versus "backward" is not determined by a
        // correlation, and nothing downstream depends on it beyond calling one of them braking.
        double alignment = Math.Abs(frame.Forward.X * PocketForward.X + frame.Forward.Y * PocketForward.Y + frame.Forward.Z * PocketForward.Z);

        Assert.True(alignment > 0.97, $"forward axis off: alignment {alignment:0.000}");

        // Nothing turned, so the gyroscope carries no signal to fit an up axis against. The right
        // answer is to report no lateral direction and a reduced confidence — inventing one would
        // hand every downstream reading a cornering figure measured along an arbitrary direction.
        Assert.Equal(0, frame.Lateral.Length, 6);
        Assert.InRange(frame.Confidence, 0.3, 0.6);
    }

    [Fact]
    public void Braking_and_cornering_are_told_apart_rather_than_summed()
    {
        // A hard stop, then later a hard corner at constant speed. If the axes were wrong these would
        // bleed into each other and both peaks would land in the middle.
        var (fixes, motion) = Outing(
            t => t is > 20 and < 22 ? -0.3 : t is > 30 and < 32 ? 0.3 : 0,
            t => t is > 60 and < 70 ? 12 : 0);

        var insights = MotionAnalysis.Analyze(fixes, motion, ActivityKind.Car);

        Assert.NotNull(insights);
        Assert.True(insights.PeakBrakingG > 0.25, $"braking {insights.PeakBrakingG:0.00} g");
        Assert.True(insights.PeakLateralG > 0.15, $"lateral {insights.PeakLateralG:0.00} g");

        // The stop was harder than the corner, and the analysis has to say so.
        Assert.True(insights.PeakBrakingG > insights.PeakLateralG);
    }

    [Fact]
    public void The_beat_is_the_footfall_rate_and_not_a_multiple_of_it()
    {
        // Two beats a second. A periodic signal correlates just as well at twice its period, so the
        // strongest peak is not the answer — taking it reports half the real rate about as often as
        // not, and 60 is every bit as plausible a number as 120.
        var beats = Impulses([0.0, 0.5], strideSeconds: 1.0, evenBeats: true);

        var (rate, strength) = MotionAnalysis.Rhythm(beats);

        Assert.InRange(rate, 110, 130);
        Assert.True(strength > 0.3, $"a clean beat should read as strong, got {strength:0.00}");
    }

    [Fact]
    public void A_ragged_signal_reads_as_a_weaker_beat_than_a_clean_one()
    {
        // Strength is how much of the signal the beat explains, which is what separates a steady
        // rhythm from a phone rattling around. It does NOT separate a trot from a canter: an
        // unevenly divided stride is still perfectly periodic, so both score alike. Telling those
        // apart needs beat structure and labelled rides to check it against; see
        // docs/private/research/horse-gait-from-rider-phone-research.md.
        var clean = Impulses([0.0, 0.5], strideSeconds: 1.0, evenBeats: true);
        var noisy = Noisy(clean, seed: 7);

        var (_, cleanStrength) = MotionAnalysis.Rhythm(clean);
        var (_, noisyStrength) = MotionAnalysis.Rhythm(noisy);

        Assert.True(cleanStrength > noisyStrength + 0.1,
            $"clean {cleanStrength:0.00} should explain more than noisy {noisyStrength:0.00}");
    }

    private static List<MotionSample> Noisy(List<MotionSample> samples, int seed)
    {
        var random = new Random(seed);

        return [.. samples.Select(s => s with { X = s.X + (random.NextDouble() - 0.5) * 4 })];
    }

    private static List<MotionSample> Impulses(double[] beatsWithinStride, double strideSeconds, bool evenBeats)
    {
        const double baseMs = 1_700_000_000_000;
        var samples = new List<MotionSample>();

        for (int i = 0; i < 50 * 40; i++)
        {
            double t = i / 50.0;
            double phase = t % strideSeconds / strideSeconds;
            double value = 0;

            foreach (var (beat, index) in beatsWithinStride.Select((b, n) => (b, n)))
            {
                double distance = Math.Abs(phase - beat);
                distance = Math.Min(distance, 1 - distance);

                // A hoof landing is a sharp impulse, not a sine — which is exactly why the rhythm is
                // found by autocorrelation rather than by looking for a spectral peak.
                double weight = evenBeats ? 1.0 : 1.0 - index * 0.35;
                value += weight * Math.Exp(-distance * distance / 0.0009);
            }

            samples.Add(new MotionSample(baseMs + t * 1000, value, 0, 0, MotionSensors.UserAcceleration));
        }

        return samples;
    }

    [Fact]
    public void A_recording_too_short_to_fit_says_so_instead_of_guessing()
    {
        var (fixes, motion) = Outing(_ => 0.1, _ => 0, seconds: 2);

        Assert.Null(MotionAnalysis.Analyze(fixes, motion, ActivityKind.Car));
    }

    [Fact]
    public void A_phone_that_never_moved_produces_no_rhythm()
    {
        const double baseMs = 1_700_000_000_000;
        var still = Enumerable.Range(0, 2000)
            .Select(i => new MotionSample(baseMs + i * 20, 0.001, 0, 0, MotionSensors.UserAcceleration))
            .ToList();

        var (rate, _) = MotionAnalysis.Rhythm(still);

        Assert.Equal(0, rate);
    }
}
