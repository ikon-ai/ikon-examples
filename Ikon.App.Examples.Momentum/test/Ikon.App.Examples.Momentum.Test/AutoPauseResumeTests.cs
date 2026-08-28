// The field failure: a run auto-paused at a rest, and never came back — not when the runner set off
// again, and not after the app was restarted. These reproduce it from the fixes a phone actually
// sends when it is standing still in a city.

public class AutoPauseResumeTests
{
    private static readonly DateTime Start = new(2026, 8, 25, 11, 0, 0, DateTimeKind.Utc);

    /// <summary>Walks north from a point at a given speed, emitting one fix a second.</summary>
    private static void PushLeg(TrackRecorder recorder, ref GeoPoint at, ref double second,
        double speedMps, int seconds, double accuracyM, bool reportSpeed = true)
    {
        for (int i = 0; i < seconds; i++)
        {
            at = Geo.Offset(at, speedMps, 0);
            second++;
            recorder.Push(new RawFix(
                Start.AddSeconds(second),
                at,
                10,
                reportSpeed ? speedMps : -1,
                0,
                accuracyM));
        }
    }

    [Fact]
    public void ARunnerWhoStartsAgainIsUnpaused()
    {
        var recorder = new TrackRecorder(ActivityKind.Foot);
        var at = new GeoPoint(60.1712, 24.9414);
        double second = 0;

        PushLeg(recorder, ref at, ref second, 3.0, 60, 8);
        // A rest: standing still long enough to trip the eight-second dwell.
        PushLeg(recorder, ref at, ref second, 0.0, 40, 8);
        Assert.False(recorder.Snapshot([], "", false, RecordingState.Recording).Track.Count == 0);
        Assert.True(recorder.PointsSnapshot()[^1].Moving == false, "the rest never auto-paused");

        // Off again at running pace.
        PushLeg(recorder, ref at, ref second, 3.0, 30, 8);

        Assert.True(recorder.PointsSnapshot()[^1].Moving, "the runner set off again and stayed paused");
    }

    [Fact]
    public void APocketedPhoneWithPoorAccuracyStillUnpauses()
    {
        // Standing still, iOS lets accuracy drift out — and a phone back in a pocket among buildings
        // stays there. If a vague fix can hold the pause, the outing never resumes, restart or not.
        var recorder = new TrackRecorder(ActivityKind.Foot);
        var at = new GeoPoint(60.1712, 24.9414);
        double second = 0;

        PushLeg(recorder, ref at, ref second, 3.0, 60, 8);
        PushLeg(recorder, ref at, ref second, 0.0, 40, 30);
        PushLeg(recorder, ref at, ref second, 3.0, 40, 30);

        Assert.True(recorder.PointsSnapshot()[^1].Moving, "poor accuracy kept the outing paused for good");
    }

    [Fact]
    public void APhoneReportingNoSpeedStillUnpauses()
    {
        // iOS reports speed -1 when it cannot measure it. Resume then rests entirely on displacement,
        // and displacement is measured through a filter that barely moves while the speed it is fed
        // stays near zero — a lock that feeds itself.
        var recorder = new TrackRecorder(ActivityKind.Foot);
        var at = new GeoPoint(60.1712, 24.9414);
        double second = 0;

        PushLeg(recorder, ref at, ref second, 3.0, 60, 10);
        PushLeg(recorder, ref at, ref second, 0.0, 40, 10, reportSpeed: false);
        PushLeg(recorder, ref at, ref second, 3.0, 40, 10, reportSpeed: false);

        Assert.True(recorder.PointsSnapshot()[^1].Moving, "with no reported speed the outing never resumed");
    }

    [Fact]
    public void ARestoredOutingCanStillUnpause()
    {
        // What the user hit: restarting the app did not help, because the restored recorder re-enters
        // exactly the state it was stuck in.
        var recorder = new TrackRecorder(ActivityKind.Foot);
        var at = new GeoPoint(60.1712, 24.9414);
        double second = 0;

        PushLeg(recorder, ref at, ref second, 3.0, 60, 30);
        PushLeg(recorder, ref at, ref second, 0.0, 40, 30);

        var saved = recorder.Finish();
        var restored = TrackRecorder.Restore(saved.Kind, saved.StartedAt, saved.Points, saved.DistanceM,
            saved.MovingSeconds, saved.AscentM, saved.DescentM, saved.MaxSpeedMps);

        PushLeg(restored, ref at, ref second, 3.0, 40, 30);

        Assert.True(restored.PointsSnapshot()[^1].Moving, "a restarted app stayed paused too");
    }
}
