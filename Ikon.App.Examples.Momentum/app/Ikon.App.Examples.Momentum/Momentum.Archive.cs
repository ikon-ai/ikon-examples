/// <summary>
/// Takes the recording a phone kept for itself and repairs the saved outing with it.
/// </summary>
/// <remarks>
/// The live stream and the archive are deliberately not rivals. Everything the rider looks at while
/// they are out — the numbers, auto-pause, the coach, the lock-screen banner — is computed here from
/// fixes as they arrive, and that has to keep working when the archive is missing, late, or came
/// from a client too old to record one. So the archive never takes over mid-outing; it arrives at
/// the end and corrects what the network cost.
///
/// That is also why it carries raw fixes rather than a finished track. <see cref="TrackRecorder"/> is
/// the processor — smoothing, auto-pause, elevation — it is deterministic, and re-running it over a
/// complete set of fixes gives a better track than one assembled live out of whatever arrived. A
/// phone that spent ten minutes in a tunnel produces the same track either way, except that this one
/// has the tunnel in it.
/// </remarks>
public partial class MomentumApp
{
    private async Task OnArchiveAsync(RecordingArchive archive)
    {
        if (archive.Fixes.Count == 0)
        {
            // Motion only, or a stray tap. The asset is kept either way — it is still a recording of
            // something, and the corpus is the reason half of it exists.
            Log.Instance.Info($"Recording archive {archive.ArchiveId} carried no fixes; keeping it at {archive.Asset.Path}");
            return;
        }

        var activity = await LoadActivityAsync(archive.ArchiveId);

        if (activity == null)
        {
            // The archive outlived the row — an outing whose save failed, or one uploaded from a
            // phone after the activity was deleted. Nothing to repair, and nothing to be done.
            Log.Instance.Warning($"Recording archive {archive.ArchiveId} has no matching activity; keeping the asset and doing nothing else");
            return;
        }

        lock (_sessionLock)
        {
            if (_state != RecordingState.Idle && _activeActivityId == archive.ArchiveId)
            {
                // Still recording it. Replacing the track underneath a running outing would fight the
                // live recorder for the same rows; the archive is offered again when it finishes.
                Log.Instance.Info($"Recording archive {archive.ArchiveId} arrived while the outing is still running; leaving it for the finish");
                return;
            }
        }

        var recorder = new TrackRecorder(activity.Kind);

        foreach (var recorded in archive.Fixes)
        {
            recorder.Push(new RawFix(
                DateTimeOffset.FromUnixTimeMilliseconds((long)recorded.AtMillis).UtcDateTime,
                new GeoPoint(recorded.Latitude, recorded.Longitude),
                recorded.AltitudeMeters,
                recorded.SpeedMps,
                recorded.Heading,
                recorded.AccuracyMeters));
        }

        if (!recorder.HasFix)
        {
            Log.Instance.Warning($"Recording archive {archive.ArchiveId} had {archive.Fixes.Count} fixes but none the recorder would take");
            return;
        }

        var track = recorder.Finish();

        // A repair that would shorten the outing is refused. The live stream cannot invent distance,
        // so a shorter archive means it is the incomplete one — a recording that started late, or one
        // from a second device that was only along for part of the ride.
        if (track.DistanceM < activity.DistanceM - 1)
        {
            Log.Instance.Info($"Recording archive {archive.ArchiveId} is shorter than what was recorded live ({track.DistanceM:0} m vs {activity.DistanceM:0} m); keeping the live track");
            return;
        }

        double recovered = track.DistanceM - activity.DistanceM;

        try
        {
            var bests = activity.UserId.Length > 0 ? await LoadPersonalBestsAsync(activity.UserId, track.Kind) : [];
            var highlights = Detectors.Detect(activity.Id, track.Kind, track.Points, detector => bests.GetValueOrDefault(detector)).ToList();

            // The part GPS could never answer. Only an uploaded archive carries motion at a usable
            // rate — the live stream is decimated to what a screen needs — so this is the one place
            // it can run.
            var insights = MotionAnalysis.Analyze(archive.Fixes, archive.Motion, track.Kind);

            // Only a horse has gaits to find; running the segmentation on a car would report the
            // suspension's rhythm as a canter.
            var gaits = track.Kind == ActivityKind.Horse
                ? GaitAnalysis.Segment(archive.Fixes, archive.Motion)
                : [];

            if (insights != null)
            {
                highlights.AddRange(MotionHighlights.From(activity.Id, track.Kind, insights, gaits, track.ElapsedSeconds));
                Log.Instance.Info($"Motion analysis for {activity.Id}: {insights.SampleCount} samples, axes confidence {insights.Frame.Confidence:0.00}, peak {insights.PeakCombinedG:0.00} g, beat {insights.BeatsPerMinute:0}/min, {gaits.Count} gait segments");
            }

            var repaired = activity with
            {
                DistanceM = track.DistanceM,
                MovingSeconds = track.MovingSeconds,
                ElapsedSeconds = track.ElapsedSeconds,
                AscentM = track.AscentM,
                DescentM = track.DescentM,
                AvgSpeedMps = track.AvgSpeedMps,
                MaxSpeedMps = track.MaxSpeedMps,
                MomentumScore = MomentumScore(highlights),
            };

            await ReplaceTrackAsync(repaired, track.Points, highlights);

            // The write-up was made at the finish, before any of this existed. Whatever the archive
            // added is very often the better story — how the outing was driven rather than how fast.
            if (insights != null)
            {
                await RecurateAsync(activity.Id);
            }

            Log.Instance.Info($"Repaired activity {archive.ArchiveId} from its device archive: {recovered:0} m recovered, {track.Points.Count} points, {archive.Motion.Count} motion samples kept at {archive.Asset.Path}");

            _frame.Value++;
        }
        catch (Exception ex)
        {
            // The saved outing is untouched and still whatever the live stream built, which is the
            // floor this whole path is measured against. The archive asset stays for another attempt.
            Log.Instance.Error($"Could not repair activity {archive.ArchiveId} from its archive: {ex}");
        }
    }
}
