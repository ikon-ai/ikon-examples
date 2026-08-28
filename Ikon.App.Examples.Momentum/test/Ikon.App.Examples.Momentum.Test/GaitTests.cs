namespace Ikon.App.Examples.Momentum.Test;

/// <summary>
/// Each gait is synthesised from what it actually is — a trot as two evenly spaced beats, a canter as
/// three unevenly spaced ones — and has to come back out. The point of these is the pair that speed
/// cannot separate: a collected canter and an extended trot covering ground at the same rate.
/// </summary>
public class GaitTests
{
    private const double BaseMs = 1_700_000_000_000;
    private const int Hertz = 50;

    /// <summary>
    /// Builds an acceleration trace with footfalls at the given phases of each stride.
    /// </summary>
    /// <param name="beatPhases">Where in the stride each foot lands, 0–1.</param>
    private static List<MotionSample> Ride(double[] beatPhases, double strideSeconds, double seconds, int seed = 3)
    {
        var random = new Random(seed);
        var samples = new List<MotionSample>();

        for (int i = 0; i < seconds * Hertz; i++)
        {
            double t = i / (double)Hertz;
            double phase = t % strideSeconds / strideSeconds;
            double value = 0;

            foreach (double beat in beatPhases)
            {
                double distance = Math.Abs(phase - beat);
                distance = Math.Min(distance, 1 - distance);
                value += Math.Exp(-distance * distance / 0.0012);
            }

            // A real trace is never clean; a classifier that only survives noiseless input is not one.
            value += (random.NextDouble() - 0.5) * 0.25;

            samples.Add(new MotionSample(BaseMs + t * 1000, value * 6, 0, 0, MotionSensors.UserAcceleration));
        }

        return samples;
    }

    private static List<RecordedFix> Fixes(double speedMps, double seconds)
    {
        var fixes = new List<RecordedFix>();

        for (int i = 0; i <= (int)seconds; i++)
        {
            fixes.Add(new RecordedFix(BaseMs + i * 1000, 61.5, 23.8, 5, speedMps, 90, 100));
        }

        return fixes;
    }

    private static Gait Dominant(IReadOnlyList<GaitSegment> segments)
        => segments.Count == 0
            ? Gait.Unknown
            : segments.GroupBy(s => s.Gait).OrderByDescending(g => g.Sum(s => s.DurationSeconds)).First().Key;

    [Fact]
    public void A_trot_reads_as_a_trot()
    {
        // Two beats, evenly spaced, ~0.75 s stride — 160 beats a minute.
        var segments = GaitAnalysis.Segment(Fixes(3.6, 40), Ride([0.0, 0.5], strideSeconds: 0.75, seconds: 40));

        Assert.Equal(Gait.Trot, Dominant(segments));
    }

    [Fact]
    public void A_canter_reads_as_a_canter()
    {
        // Three beats and a suspension: two close together, then a gap. That gap is the gait.
        var segments = GaitAnalysis.Segment(Fixes(6.0, 40), Ride([0.0, 0.22, 0.45], strideSeconds: 0.62, seconds: 40));

        Assert.Equal(Gait.Canter, Dominant(segments));
    }

    [Fact]
    public void A_walk_reads_as_a_walk()
    {
        // Four beats, evenly spaced, slow.
        var segments = GaitAnalysis.Segment(Fixes(1.6, 40), Ride([0.0, 0.25, 0.5, 0.75], strideSeconds: 1.6, seconds: 40));

        Assert.Equal(Gait.Walk, Dominant(segments));
    }

    [Fact]
    public void A_collected_canter_is_not_read_as_an_extended_trot()
    {
        // THE case. Both cover ground at 5 m/s, so every speed-band classifier calls them the same
        // thing. They differ only in how the stride divides — which is the entire argument for
        // measuring the beat instead of the pace.
        const double speed = 5.0;

        var canter = GaitAnalysis.Segment(Fixes(speed, 40), Ride([0.0, 0.22, 0.45], strideSeconds: 0.68, seconds: 40));
        var trot = GaitAnalysis.Segment(Fixes(speed, 40), Ride([0.0, 0.5], strideSeconds: 0.72, seconds: 40));

        Assert.Equal(Gait.Canter, Dominant(canter));
        Assert.Equal(Gait.Trot, Dominant(trot));
    }

    [Fact]
    public void A_change_of_gait_is_found_rather_than_averaged()
    {
        var walk = Ride([0.0, 0.25, 0.5, 0.75], strideSeconds: 1.6, seconds: 20);
        var trot = Ride([0.0, 0.5], strideSeconds: 0.75, seconds: 20)
            .Select(s => s with { AtMillis = s.AtMillis + 20_000 })
            .ToList();

        // Speed changes with the gait, because a horse that starts trotting speeds up. Holding one
        // figure across both halves describes neither.
        var fixes = new List<RecordedFix>();

        for (int i = 0; i <= 40; i++)
        {
            fixes.Add(new RecordedFix(BaseMs + i * 1000, 61.5, 23.8, 5, i < 20 ? 1.6 : 3.6, 90, 100));
        }

        var segments = GaitAnalysis.Segment(fixes, [.. walk, .. trot]);
        var kinds = segments.Select(s => s.Gait).Distinct().ToArray();

        Assert.Contains(Gait.Walk, kinds);
        Assert.Contains(Gait.Trot, kinds);

        // And in the right order, rather than as a scatter of flickers.
        Assert.True(segments.First(s => s.Gait == Gait.Walk).StartSeconds < segments.First(s => s.Gait == Gait.Trot).StartSeconds);
    }

    [Fact]
    public void A_phone_going_nowhere_produces_no_gaits()
    {
        var still = Enumerable.Range(0, 2000)
            .Select(i => new MotionSample(BaseMs + i * 20, 0.02, 0, 0, MotionSensors.UserAcceleration))
            .ToList();

        Assert.Empty(GaitAnalysis.Segment(Fixes(0, 40), still));
    }

    [Fact]
    public void Short_flickers_are_not_reported_as_gaits()
    {
        var segments = GaitAnalysis.Segment(Fixes(3.6, 40), Ride([0.0, 0.5], strideSeconds: 0.75, seconds: 40));

        Assert.All(segments, s => Assert.True(s.DurationSeconds >= 2.5, $"{s.Gait} lasted {s.DurationSeconds:0.0}s"));
    }
}
