public partial class MomentumApp
{
    /// <summary>
    /// A rider arriving with an empty log gets one outing of each kind, so the feed, the detail screen
    /// and the medals all have something real to show before they have been anywhere. Each is the
    /// simulator run to completion through the same recorder a phone feeds, so the seeded activities
    /// are not mock-ups — they are outings, and their highlights were detected the same way a real
    /// one's would be.
    /// </summary>
    private async Task SeedLogAsync(string userId)
    {
        (string RouteId, ActivityKind Kind, string Preset, string Title, int DaysAgo, int Hour, int Seed)[] seeds =
        [
            ("toolonlahti", ActivityKind.Foot, "run", "Around the bay", 2, 7, 4_101),
            ("nuuksio", ActivityKind.Bike, "", "Nuuksio in the rain", 4, 17, 4_102),
            ("lavaux", ActivityKind.Bike, "", "The wall at Chexbres", 9, 9, 4_103),
            ("vihti", ActivityKind.Horse, "", "Long hack through the pines", 6, 15, 4_104),
            ("porkkala", ActivityKind.Car, "spirited", "Out to the point", 1, 19, 4_105),
            ("toolonlahti", ActivityKind.Foot, "walk", "Walking it off", 11, 20, 4_106),
        ];

        foreach (var seed in seeds)
        {
            try
            {
                await SeedOneAsync(userId, seed.RouteId, seed.Kind, seed.Preset, seed.Title, seed.DaysAgo, seed.Hour, seed.Seed);
            }
            catch (Exception ex)
            {
                // One seed failing leaves the others in place; an empty-ish feed beats no feed and beats
                // failing the rider's first join.
                Log.Instance.Warning($"Seeding the {seed.Title} outing for user {userId} failed: {ex.Message}");
            }
        }
    }

    private async Task SeedOneAsync(string userId, string routeId, ActivityKind kind, string preset, string title,
        int daysAgo, int hour, int seed)
    {
        var route = Routes.ById(routeId);
        var plan = new SimulationPlan(route, kind, seed, Preset: preset);
        var startedAt = DateTime.UtcNow.Date.AddDays(-daysAgo).AddHours(hour);
        var recorder = new TrackRecorder(kind);

        foreach (var fix in new TrackSimulator(plan).Fixes(startedAt))
        {
            recorder.Push(fix);
        }

        var track = recorder.Finish();

        if (track.Points.Count < 10)
        {
            return;
        }

        string activityId = Guid.NewGuid().ToString("N");
        var bests = await LoadPersonalBestsAsync(userId, kind);
        var highlights = Detectors.Detect(activityId, kind, track.Points, detector => bests.GetValueOrDefault(detector));

        var activity = new Activity(
            activityId,
            userId,
            kind,
            title,
            SeedStory(kind, track),
            track.StartedAt,
            track.DistanceM,
            track.MovingSeconds,
            track.ElapsedSeconds,
            track.AscentM,
            track.DescentM,
            track.AvgSpeedMps,
            track.MaxSpeedMps,
            MomentumScore(highlights),
            Published: true);

        // A seeded outing is thousands of points and nobody scrubs a seed second by second; every
        // third keeps the map and the charts honest at a third of the rows.
        var stored = track.Points.Where((_, i) => i % 3 == 0).ToList();
        await FinalizeActivityAsync(activity, stored, highlights);
    }

    private static string SeedStory(ActivityKind kind, RecordedTrack track) => kind switch
    {
        ActivityKind.Foot => $"{Momentum.FormatDistance(track.DistanceM)} at {Momentum.FormatPace(track.AvgSpeedMps)}. Held it together to the end.",
        ActivityKind.Bike => $"{Momentum.FormatDistance(track.DistanceM)} and {track.AscentM:0} m of climbing. The descent paid for all of it.",
        ActivityKind.Horse => $"{Momentum.FormatDuration(track.MovingSeconds)} out, mostly at a trot. She was keen the whole way.",
        _ => $"{Momentum.FormatDistance(track.DistanceM)} west with the light going. Quiet roads once past the bridge.",
    };
}
