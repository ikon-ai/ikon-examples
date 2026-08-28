// The simulator has to be wrong in the ways a phone is wrong and right in the ways physics is right.
// Both of the failures below shipped and were only caught by looking at the numbers.

public class SimulationTests
{
    [Fact]
    public void TheBikeDoesNotStallOnDescents()
    {
        // Solving the power equation with Newton's method from a flat-road seed lands on the wrong
        // side of zero once gravity outweighs resistance, and every descent came out at 0.9 m/s —
        // a rider freewheeling downhill at walking pace.
        var route = Routes.ById("lavaux");
        var recorder = new TrackRecorder(ActivityKind.Bike);

        foreach (var fix in new TrackSimulator(new SimulationPlan(route, ActivityKind.Bike, Seed: 4103))
                     .Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc)))
        {
            recorder.Push(fix);
        }

        var track = recorder.Finish();
        double descentTopSpeed = track.Points.Where(p => p.DistanceM > route.TotalMeters * 0.6).Max(p => p.SpeedMps);

        Assert.True(descentTopSpeed > 8, $"Fastest the bike went downhill was {descentTopSpeed * 3.6:0.0} km/h");
        Assert.True(descentTopSpeed < 20, $"Descent hit {descentTopSpeed * 3.6:0.0} km/h, beyond anyone's nerve on a vineyard road");
    }

    [Fact]
    public void HeadingNeverCollapsesAtTheEndOfARoute()
    {
        // A look-ahead clamped at the finish reports the bearing between a point and itself, which is
        // zero — a hard left nobody took, which the corner detector then reported as a corner.
        var route = Routes.ById("porkkala");
        var fixes = new TrackSimulator(new SimulationPlan(route, ActivityKind.Car, Seed: 4105))
            .Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc))
            .ToList();

        var tail = fixes.TakeLast(30).ToList();

        for (int i = 1; i < tail.Count; i++)
        {
            double turn = Math.Abs(Geo.BearingDelta(tail[i - 1].HeadingDeg, tail[i].HeadingDeg));
            Assert.True(turn < 45, $"Heading jumped {turn:0} degrees in one second near the end of the route");
        }
    }

    [Fact]
    public void ACarObeysItsSpeedLimitsAndClearsEveryLight()
    {
        // A light that is never retired stops the car again on the very next step, and the drive never
        // leaves the junction — the whole route came out as four hundred metres in two hours.
        var route = Routes.ById("porkkala");
        var recorder = new TrackRecorder(ActivityKind.Car);

        foreach (var fix in new TrackSimulator(new SimulationPlan(route, ActivityKind.Car, Seed: 4105, Preset: "spirited"))
                     .Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc)))
        {
            recorder.Push(fix);
        }

        var track = recorder.Finish();

        Assert.True(track.DistanceM > route.TotalMeters * 0.9, $"The drive covered only {track.DistanceM:0} m of {route.TotalMeters:0} m");
        Assert.True(track.MaxSpeedMps * 3.6 < 130, $"Top speed {track.MaxSpeedMps * 3.6:0} km/h on a road limited to 100");
    }

    [Fact]
    public void GpsNoiseIsCorrelatedRatherThanWhite()
    {
        // Detectors validated against white noise are not validated: real multipath error wanders and
        // does not average away over the window a detector looks at.
        var route = Routes.ById("toolonlahti");
        var fixes = new TrackSimulator(new SimulationPlan(route, ActivityKind.Foot, Seed: 4101, Preset: "run"))
            .Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc))
            .Take(300)
            .ToList();

        // Against the route's own line, consecutive errors should look alike; white noise would not.
        var errors = fixes.Select((f, i) => Geo.DistanceMeters(f.Point, route.At(route.TotalMeters * i / 3000.0).Point)).ToList();
        double meanStep = errors.Zip(errors.Skip(1), (a, b) => Math.Abs(b - a)).Average();
        double spread = errors.Max() - errors.Min();

        Assert.True(meanStep < spread / 3, $"Error moved {meanStep:0.0} m per second across a {spread:0.0} m spread — that is white noise");
    }
}
