/// <summary>One outing as the admin view sees it, with the things that say whether it worked.</summary>
public sealed record AdminRow(
    Activity Activity,
    int PointCount,
    int HighlightCount,
    bool HasMotionAnalysis,
    bool InProgress,
    bool Simulated,
    bool ArchiveStored,
    long ArchiveBytes);

/// <summary>
/// An operator's view of what the app has actually recorded, reachable at <c>?section=admin</c>.
///
/// It exists because the questions that matter after a real outing — did the device archive arrive,
/// did the motion analysis run, is an outing wedged in progress — were only answerable by opening a
/// psql session against the space. That is a bad place to keep the truth about whether a feature
/// works.
/// </summary>
/// <remarks>
/// Deliberately not a tab. It is reached by URL, so it stays out of the way of anyone using the app
/// while remaining one link away for whoever is debugging it.
/// </remarks>
public partial class MomentumApp
{
    private readonly Reactive<IReadOnlyList<AdminRow>> _adminRows = new([]);
    private readonly Reactive<string> _adminBusy = new("");
    private readonly Reactive<string> _adminNote = new("");

    private async Task LoadAdminAsync()
    {
        try
        {
            var rows = await LoadAdminRowsAsync();
            var enriched = new List<AdminRow>(rows.Count);

            foreach (var row in rows)
            {
                var uri = ArchiveUriFor(row.Activity.Id);
                long bytes = 0;
                bool stored = false;

                try
                {
                    stored = await Asset.Instance.ExistsAsync(uri);

                    if (stored)
                    {
                        bytes = (await Asset.Instance.GetMetadataAsync(uri)).Size ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    // Asset storage being unreachable says nothing about the outing; the row still
                    // shows everything the database knows.
                    Log.Instance.Debug($"Could not check the archive for {row.Activity.Id}: {ex.Message}");
                }

                enriched.Add(row with { ArchiveStored = stored, ArchiveBytes = bytes });
            }

            _adminRows.Value = enriched;
        }
        catch (Exception ex)
        {
            _adminNote.Value = "Could not read the log";
            Log.Instance.Error($"Admin view could not load: {ex}");
        }
    }

    private AssetUri ArchiveUriFor(string activityId) =>
        new(AssetClass.CloudFile, $"recordings/{activityId}.ikar", app.GlobalState.SpaceId);

    /// <summary>
    /// Re-runs everything derived from an outing's stored data: the GPS detectors always, and the
    /// motion analysis and gait segmentation when a device archive is there to run them on.
    /// </summary>
    /// <remarks>
    /// The point is to be able to improve a detector and see it applied to rides already recorded,
    /// rather than only to the next one. The rider's own title and story are theirs and are not
    /// touched.
    /// </remarks>
    private async Task ReanalyseAsync(string activityId)
    {
        _adminBusy.Value = activityId;
        _adminNote.Value = "";

        try
        {
            var activity = await LoadActivityAsync(activityId);

            if (activity == null)
            {
                _adminNote.Value = "That outing is gone";
                return;
            }

            var points = await LoadPointsAsync(activityId);

            if (points.Count < 2)
            {
                _adminNote.Value = "No track to analyse";
                return;
            }

            var bests = activity.UserId.Length > 0 ? await LoadPersonalBestsAsync(activity.UserId, activity.Kind) : [];
            var highlights = Detectors.Detect(activityId, activity.Kind, points, detector => bests.GetValueOrDefault(detector)).ToList();
            string note = $"{highlights.Count} from the track";

            var uri = ArchiveUriFor(activityId);

            if (await Asset.Instance.ExistsAsync(uri))
            {
                var (_, fixes, motion) = RecordingArchiveCodec.Decode(await Asset.Instance.GetBytesAsync(uri));
                var insights = MotionAnalysis.Analyze(fixes, motion, activity.Kind);
                var gaits = activity.Kind == ActivityKind.Horse ? GaitAnalysis.Segment(fixes, motion) : [];

                if (insights != null)
                {
                    var before = highlights.Count;
                    highlights.AddRange(MotionHighlights.From(activityId, activity.Kind, insights, gaits, activity.ElapsedSeconds));

                    // The numbers, not just the count. A run that finds nothing is the interesting
                    // case, and "0 highlights" does not say whether the axes failed to fit or the
                    // driving was simply gentle.
                    note += $" · {motion.Count} samples, {fixes.Count} fixes, axes {insights.Frame.Confidence:0.00}"
                        + $", brake {insights.PeakBrakingG:0.00}g, accel {insights.PeakAccelG:0.00}g"
                        + $", lat {insights.PeakLateralG:0.00}g, combined {insights.PeakCombinedG:0.00}g"
                        + $", beat {insights.BeatsPerMinute:0}/min (strength {insights.RhythmStrength:0.00})"
                        + $" → {highlights.Count - before} highlights";
                }
                else
                {
                    note += $" · {motion.Count} samples, {fixes.Count} fixes — too little to analyse";
                }
            }
            else
            {
                note += " — no device archive for this outing";
            }

            await ReplaceTrackAsync(activity with { MomentumScore = MomentumScore(highlights) }, points, highlights);

            // The write-up follows the measurements. Re-running analysis and leaving the old story in
            // place would leave the outing described by facts that are no longer the best ones.
            await RecurateAsync(activityId);

            _adminNote.Value = note;

            await LoadAdminAsync();
        }
        catch (Exception ex)
        {
            _adminNote.Value = "Analysis failed — see the server log";
            Log.Instance.Error($"Re-analysing {activityId} failed: {ex}");
        }
        finally
        {
            _adminBusy.Value = "";
        }
    }

    private void RenderAdmin(UIView view)
    {
        var rows = _adminRows.Value;

        view.Column(["gap-5"], content: view =>
        {
            view.Row(["items-end justify-between gap-4"], content: view =>
            {
                view.Column(["gap-1"], content: view =>
                {
                    Kicker(view, "operator");
                    view.Heading("What was recorded", [Brand.Title, "text-[28px]"]);
                });

                PillButton(view, "Reload", LoadAdminAsync, icon: "refresh-cw");
            });

            if (_adminNote.Value.Length > 0)
            {
                view.Text(_adminNote.Value, ["text-[12.5px] text-[#f2da00]"]);
            }

            if (rows.Count == 0)
            {
                view.Text("Nothing loaded yet — press Reload.", ["text-[13px] text-muted-foreground"]);
                return;
            }

            int withArchive = rows.Count(r => r.ArchiveStored);
            int withMotion = rows.Count(r => r.HasMotionAnalysis);
            int wedged = rows.Count(r => r.InProgress);

            view.Grid(["grid-cols-2 sm:grid-cols-4 gap-x-6"], content: view =>
            {
                Tile(view, "Outings", $"{rows.Count}", "");
                Tile(view, "Archives", $"{withArchive}", $"of {rows.Count}");
                Tile(view, "Analysed", $"{withMotion}", "motion");
                Tile(view, "In progress", $"{wedged}", wedged > 0 ? "stuck?" : "");
            });

            foreach (var row in rows)
            {
                AdminCard(view, row);
            }
        });
    }

    private void AdminCard(UIView view, AdminRow row)
    {
        bool busy = _adminBusy.Value == row.Activity.Id;

        view.Column([Brand.Panel, "gap-2.5 p-3.5"], key: row.Activity.Id, content: view =>
        {
            view.Row(["items-center justify-between gap-3 flex-wrap"], content: view =>
            {
                view.Row(["items-center gap-2"], content: view =>
                {
                    view.Icon(Momentum.ProfileOf(row.Activity.Kind).Icon, ["text-[#e62e7d]"], size: IconSize.Sm);
                    view.Text(row.Activity.StartedAt.ToString("dd MMM HH:mm"), [Brand.Mono, "text-[11px]"]);
                });

                view.Row(["items-center gap-1.5 flex-wrap"], content: view =>
                {
                    if (row.Simulated)
                    {
                        Badge(view, "SIMULATED", "#6e6e74");
                    }

                    if (row.InProgress)
                    {
                        Badge(view, "IN PROGRESS", "#f2da00");
                    }

                    Badge(view, row.ArchiveStored ? $"ARCHIVE {row.ArchiveBytes / 1024} KB" : "NO ARCHIVE", row.ArchiveStored ? "#3ddc84" : "#6e6e74");
                    Badge(view, row.HasMotionAnalysis ? "MOTION" : "GPS ONLY", row.HasMotionAnalysis ? "#3ddc84" : "#6e6e74");
                });
            });

            view.Row(["items-baseline gap-4 flex-wrap"], content: view =>
            {
                view.Text(Momentum.FormatDistance(row.Activity.DistanceM), [Brand.Numeral, "text-[20px]"]);
                view.Text(Momentum.FormatDuration(row.Activity.MovingSeconds), [Brand.Mono, "text-[12px] text-muted-foreground"]);
                view.Text($"{row.PointCount} points", [Brand.Mono, "text-[11px] text-muted-foreground"]);
                view.Text($"{row.HighlightCount} highlights", [Brand.Mono, "text-[11px] text-muted-foreground"]);
            });

            view.Row(["items-center gap-2"], content: view =>
            {
                view.Button(
                    ["inline-flex items-center gap-1.5 h-8 px-3 rounded-full bg-transparent border border-border text-[11px]",
                     busy ? "opacity-40 text-muted-foreground" : "text-[#f7f7f7]"],
                    text: busy ? "Analysing…" : "Re-run analysis",
                    icon: "activity",
                    disabled: busy,
                    onClick: () => ReanalyseAsync(row.Activity.Id));

                view.Text(row.Activity.Id[..8], [Brand.Mono, "text-[10px] text-[#4d4d52]"]);
            });
        });
    }

    private static void Badge(UIView view, string text, string hex)
    {
        view.Text(text, [$"font-mono text-[9px] uppercase tracking-[0.14em] px-1.5 py-0.5 rounded-[3px] text-[{hex}] ring-1 ring-[{hex}]/40"]);
    }
}
