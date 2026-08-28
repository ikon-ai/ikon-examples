// What the recorder has to get right, written as the failures that actually happened while building
// it. Each of these was a real bug: a walker's distance halved, a flat loop that climbed two hundred
// metres, and a pause that threw away the fixes the car detectors need.

public class RecorderTests
{
    private static TrackRecorder Record(SimulationPlan plan)
    {
        var recorder = new TrackRecorder(plan.Kind);

        foreach (var fix in new TrackSimulator(plan).Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc)))
        {
            recorder.Push(fix);
        }

        return recorder;
    }

    [Fact]
    public void WalkingDistanceIsNotSwallowedByTheStepFloor()
    {
        // A walker covers about 1.4 m between one-second fixes. A minimum-step floor set above that
        // discards every step, and the distance comes out at roughly half of what was walked.
        var route = Routes.ById("toolonlahti");
        var track = Record(new SimulationPlan(route, ActivityKind.Foot, Seed: 4106, Preset: "walk")).Finish();

        double error = Math.Abs(track.DistanceM - route.TotalMeters) / route.TotalMeters;

        Assert.True(error < 0.08, $"Walked distance {track.DistanceM:0} m against a {route.TotalMeters:0} m route ({error:P1} out)");
    }

    [Theory]
    [InlineData("toolonlahti", ActivityKind.Foot, "run", 4101)]
    [InlineData("nuuksio", ActivityKind.Bike, "", 4102)]
    [InlineData("porkkala", ActivityKind.Car, "spirited", 4105)]
    public void RecordedDistanceTracksTheRouteItWasWalked(string routeId, ActivityKind kind, string preset, int seed)
    {
        var route = Routes.ById(routeId);
        var track = Record(new SimulationPlan(route, kind, seed, Preset: preset)).Finish();

        double error = Math.Abs(track.DistanceM - route.TotalMeters) / route.TotalMeters;

        Assert.True(error < 0.06, $"{routeId}: recorded {track.DistanceM:0} m against {route.TotalMeters:0} m ({error:P1} out)");
    }

    [Fact]
    public void AFlatLoopDoesNotInventClimbing()
    {
        // Töölönlahti rises about twelve metres end to end. GPS altitude noise integrated without
        // filtering and hysteresis reported two hundred, which is what this guards.
        var route = Routes.ById("toolonlahti");
        var track = Record(new SimulationPlan(route, ActivityKind.Foot, Seed: 4101, Preset: "run")).Finish();

        double trueRange = route.Samples.Max(s => s.ElevationM) - route.Samples.Min(s => s.ElevationM);

        Assert.True(track.AscentM < 60, $"Claimed {track.AscentM:0} m of climbing on a loop spanning {trueRange:0} m");
    }

    [Fact]
    public void ARealClimbIsStillMeasured()
    {
        // The mirror of the test above: filtering hard enough to kill the noise must not also kill
        // three hundred metres of actual mountain.
        var route = Routes.ById("lavaux");
        var track = Record(new SimulationPlan(route, ActivityKind.Bike, Seed: 4103)).Finish();

        Assert.InRange(track.AscentM, 240, 380);
    }

    [Fact]
    public void AutoPauseStopsTheClockWithoutDroppingFixes()
    {
        // The rule the traffic-light detector depends on: a pause stops moving time and distance, and
        // keeps every fix, because the stop itself is the thing being measured.
        var route = Routes.ById("porkkala");
        var recorder = Record(new SimulationPlan(route, ActivityKind.Car, Seed: 4105, Preset: "spirited"));
        var track = recorder.Finish();

        var stopped = track.Points.Where(p => !p.Moving).ToList();

        Assert.True(stopped.Count > 0, "A drive with six sets of lights never auto-paused");
        Assert.True(track.MovingSeconds < track.ElapsedSeconds, "Moving time was never held back by the stops");
        Assert.All(stopped, p => Assert.True(p.DistanceM > 0 || p.Seconds == 0));
    }

    [Fact]
    public void AFixTooVagueToTrustIsRejected()
    {
        var recorder = new TrackRecorder(ActivityKind.Foot);
        var at = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);

        recorder.Push(new RawFix(at, new GeoPoint(60.1712, 24.9414), 6, 1.4, 90, 8));
        recorder.Push(new RawFix(at.AddSeconds(1), new GeoPoint(60.1713, 24.9415), 6, 1.4, 90, 400));

        Assert.Single(recorder.PointsSnapshot());
    }

    [Fact]
    public void ARestoredRecorderCarriesTheOutingOn()
    {
        // What makes a ride survive the server restarting under it.
        var route = Routes.ById("toolonlahti");
        var original = Record(new SimulationPlan(route, ActivityKind.Foot, Seed: 4101, Preset: "run", DistanceLimitM: 1500));
        var saved = original.Finish();

        var restored = TrackRecorder.Restore(
            saved.Kind, saved.StartedAt, saved.Points, saved.DistanceM, saved.MovingSeconds,
            saved.AscentM, saved.DescentM, saved.MaxSpeedMps);

        var after = restored.Finish();

        Assert.Equal(saved.Points.Count, after.Points.Count);
        Assert.Equal(saved.DistanceM, after.DistanceM, 3);
        Assert.Equal(saved.MovingSeconds, after.MovingSeconds, 3);
        Assert.Equal(saved.AscentM, after.AscentM, 3);
    }
}
