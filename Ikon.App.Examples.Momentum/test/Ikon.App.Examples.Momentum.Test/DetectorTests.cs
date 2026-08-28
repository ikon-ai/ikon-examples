// The detectors, checked against outings the simulator produces. Every assertion here is a range a
// physicist would accept rather than the number the code happens to return today — a test that pins
// the current value is a change detector, not a check that the measurement is right.

public class DetectorTests
{
    private static (RecordedTrack Track, IReadOnlyList<Highlight> Highlights) Detect(
        string routeId, ActivityKind kind, int seed, string preset = "")
    {
        var route = Routes.ById(routeId);
        var recorder = new TrackRecorder(kind);

        foreach (var fix in new TrackSimulator(new SimulationPlan(route, kind, seed, Preset: preset))
                     .Fixes(new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc)))
        {
            recorder.Push(fix);
        }

        var track = recorder.Finish();
        return (track, Detectors.Detect("t", kind, track.Points));
    }

    [Fact]
    public void TheChexbresClimbIsFoundAndCategorised()
    {
        var (_, highlights) = Detect("lavaux", ActivityKind.Bike, 4103);
        var climb = highlights.FirstOrDefault(h => h.Detector == "climb");

        Assert.NotNull(climb);
        Assert.Contains("Cat", climb!.Title);
        Assert.Contains(highlights, h => h.Detector == "descent");
    }

    [Fact]
    public void AFlatRunClaimsNoClimb()
    {
        var (_, highlights) = Detect("toolonlahti", ActivityKind.Foot, 4101, "run");

        Assert.DoesNotContain(highlights, h => h.Detector is "climb" or "descent");
    }

    [Fact]
    public void ATrafficLightLaunchIsMeasuredAndPlausible()
    {
        var (_, highlights) = Detect("porkkala", ActivityKind.Car, 4105, "spirited");
        var launch = highlights.FirstOrDefault(h => h.Detector == "light-launch");

        Assert.NotNull(launch);
        // A quick road car reaches 50 km/h in about three seconds. Under two means the traction model
        // has come loose from anything a car can do.
        Assert.Matches(@"0–50 in \d+\.\d s", launch!.Detail);
        double toFifty = double.Parse(launch.Detail.Split("0–50 in ")[1].Split(" s")[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(toFifty, 2.2, 8.0);
    }

    [Fact]
    public void CornerLoadStaysWithinWhatARoadCarCanDo()
    {
        // Fitting a circle to three consecutive fixes reported 0.83 g through a 29 m radius — at
        // 110 km/h consecutive fixes are 30 m apart and carry metres of error each, so the circle was
        // measuring noise. Yaw rate is the honest measure and it has to stay under the tyres.
        var (_, highlights) = Detect("siuntio", ActivityKind.Car, 4108, "spirited");
        var sweeper = highlights.FirstOrDefault(h => h.Detector == "sweeper");

        Assert.NotNull(sweeper);
        double lateralG = double.Parse(sweeper!.Detail.Split(" g")[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(lateralG, 0.15, 0.60);
    }

    [Fact]
    public void AStraightRoadHasNoCornerAndAWindingOneHasNoStraight()
    {
        var (_, fast) = Detect("porkkala", ActivityKind.Car, 4105, "spirited");
        var (_, winding) = Detect("siuntio", ActivityKind.Car, 4108, "spirited");

        Assert.Contains(fast, h => h.Detector == "clean-straight");
        Assert.DoesNotContain(winding, h => h.Detector == "clean-straight");
        Assert.Contains(winding, h => h.Detector == "sweeper");
    }

    [Fact]
    public void HorseGaitsAreSegmentedFromTheSpeedTrace()
    {
        var (_, highlights) = Detect("vihti", ActivityKind.Horse, 4104);
        var gaits = highlights.Where(h => h.Detector.StartsWith("gait-")).ToList();

        Assert.NotEmpty(gaits);
        Assert.Contains(gaits, g => g.Detector is "gait-trot" or "gait-canter");
    }

    [Fact]
    public void TopSpeedNeverExceedsWhatWasRecorded()
    {
        var (track, highlights) = Detect("nuuksio", ActivityKind.Bike, 4102);
        var top = highlights.First(h => h.Detector == "top-speed");

        double reported = double.Parse(top.Detail.Split(" km/h")[0], System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(reported <= track.MaxSpeedMps * 3.6 + 0.1,
            $"Top-speed highlight claims {reported:0.0} km/h, above the recorded maximum of {track.MaxSpeedMps * 3.6:0.0}");
    }

    [Fact]
    public void TheSameSeedFindsTheSameHighlights()
    {
        // The whole reason detection is geometry rather than a model: the same outing has to produce
        // the same highlights, or a personal best means nothing.
        var (_, first) = Detect("porkkala", ActivityKind.Car, 4105, "spirited");
        var (_, second) = Detect("porkkala", ActivityKind.Car, 4105, "spirited");

        Assert.Equal(first.Select(h => h.Id), second.Select(h => h.Id));
        Assert.Equal(first.Select(h => Math.Round(h.Score, 6)), second.Select(h => Math.Round(h.Score, 6)));
    }

    [Fact]
    public void AMedalIsGoldWhenItBeatsWhatCameBefore()
    {
        Assert.Equal(MedalTier.Gold, Detectors.TierFor(40, previousBest: 30));
        Assert.Equal(MedalTier.Gold, Detectors.TierFor(90, previousBest: 95));
        Assert.Equal(MedalTier.None, Detectors.TierFor(10, previousBest: 80));
    }
}
