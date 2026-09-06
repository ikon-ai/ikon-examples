/// <summary>One thing the coach says while the rider is moving.</summary>
public sealed record CoachCue(string Line);

/// <summary>The write-up the curator proposes once an outing is done.</summary>
public sealed record Curation(string Title, string Story, List<string> ReelDetectors);

public partial class MomentumApp
{
    private const double CoachIntervalSeconds = 45;

    private static readonly string[] CoachPersona =
    [
        "You are the coach riding along with one person. You can see their live telemetry and nothing else.",
        "One sentence. Under fifteen words. Spoken aloud into an earbud while they are moving, so no lists, no numbers they cannot hold in their head, no punctuation they cannot hear.",
        "Say the one thing that is true right now. Never invent a metric you were not given. Never congratulate them for nothing.",
    ];

    /// <summary>
    /// Speaks to the rider every three quarters of a minute while they move. It is deliberately a
    /// separate loop from the recorder: a slow model call, a speech generation or a dropped network
    /// must never delay a fix or a pause decision.
    /// </summary>
    private async Task CoachLoopAsync(CancellationToken ct)
    {
        try
        {
            // Nothing useful to say about the first few seconds of anything.
            await Task.Delay(TimeSpan.FromSeconds(20), ct);

            while (!ct.IsCancellationRequested)
            {
                await SpeakCoachCueAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(CoachIntervalSeconds), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // The outing ended; the coach stops mid-sentence, which is correct.
        }
    }

    private async Task SpeakCoachCueAsync(CancellationToken ct)
    {
        TrackRecorder? recorder;
        RecordingState state;

        lock (_sessionLock)
        {
            recorder = _recorder;
            state = _state;
        }

        if (recorder == null || state != RecordingState.Recording)
        {
            return;
        }

        var frame = recorder.Snapshot(_liveHighlights, _coachCue, _simulated, state);

        if (frame.MovingSeconds < 20)
        {
            return;
        }

        try
        {
            var cue = await Emerge.Run<CoachCue>(LLMModel.Claude45Haiku, pass =>
            {
                pass.SystemPrompt = string.Join("\n", CoachPersona);
                pass.Command = $"""
                    Kind: {Momentum.KindLabel(frame.Kind, frame.DistanceM, frame.MovingSeconds)}
                    Elapsed: {Momentum.FormatDuration(frame.MovingSeconds)}
                    Distance: {Momentum.FormatDistance(frame.DistanceM)}
                    Right now: {Momentum.FormatRate(frame.Kind, frame.SpeedMps)}
                    Average so far: {Momentum.FormatRate(frame.Kind, frame.AvgSpeedMps)}
                    Gradient: {frame.GradePct:0.0} %
                    Climbed: {frame.AscentM:0} m
                    Just detected: {(frame.LiveHighlights.Count > 0 ? string.Join("; ", frame.LiveHighlights.Take(3).Select(h => $"{h.Title} — {h.Detail}")) : "nothing yet")}

                    Say the one thing worth saying.
                    """;
                pass.MaxWallTime = TimeSpan.FromSeconds(20);
            }, ct);

            if (string.IsNullOrWhiteSpace(cue.Line))
            {
                return;
            }

            _coachCue = cue.Line.Trim();
            _frame.Value++;

            if (_coachVoice.Value)
            {
                // The phone is in a pocket; the cue only lands if it is spoken. Sarah is the softest and
                // least accented of the library voices — a coach in your ear at six in the morning
                // should sound like someone beside you, not like a station announcement. Flash 2.5
                // ignores delivery instructions, so the voice itself has to carry the tone.
                await Audio.SpeakAsync(MediaTargets.Everyone, _coachCue, SpeechGeneratorModel.ElevenFlash25,
                    voice: "Sarah",
                    speed: 0.96,
                    cancellationToken: ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A missed cue costs the rider nothing — the next one is forty-five seconds away, and the
            // recording never depended on it.
            Log.Instance.Warning($"Coach cue generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Names the finished outing, writes it up, and orders the reel. It runs after the detectors, on
    /// what they found — the model chooses what to lead with and how to say it, never whether a
    /// highlight happened.
    /// </summary>
    private async Task CurateAsync(string activityId)
    {
        PendingPublication? pending;

        lock (_sessionLock)
        {
            pending = _pending;
        }

        if (pending == null || pending.Activity.Id != activityId)
        {
            return;
        }

        var activity = pending.Activity;

        try
        {
            var curation = await ComposeCurationAsync(activity, pending.Highlights);

            lock (_sessionLock)
            {
                if (_pending?.Activity.Id != activityId)
                {
                    // The rider discarded it, or started something else, while the model was thinking.
                    return;
                }

                var ordered = OrderByReel(pending.Highlights, curation.ReelDetectors);
                var dropped = pending.Highlights
                    .Where(h => !curation.ReelDetectors.Contains(h.Detector))
                    .Select(h => h.Id)
                    .ToHashSet();

                _pending = pending with
                {
                    Activity = activity with
                    {
                        Title = string.IsNullOrWhiteSpace(curation.Title) ? activity.Title : curation.Title.Trim(),
                        Story = curation.Story?.Trim() ?? "",
                    },
                    Highlights = ordered,
                    Dropped = dropped,
                };
            }

            if (!string.IsNullOrWhiteSpace(curation.Title))
            {
                _titleDraft.Value = curation.Title.Trim();
            }

            _frame.Value++;
        }
        catch (Exception ex)
        {
            // The rider keeps the outing, the detectors' own titles and the full reel — everything
            // except the prose. Nothing is lost that the app measured.
            Log.Instance.Warning($"Curating activity {activityId} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Puts the reel in the order the curator asked for, with anything it did not mention kept behind
    /// it. Dropping a highlight the model forgot would lose a measurement the rider earned.
    /// </summary>
    private static IReadOnlyList<Highlight> OrderByReel(IReadOnlyList<Highlight> highlights, IReadOnlyList<string> detectors)
    {
        var ranked = new List<Highlight>();

        foreach (string detector in detectors)
        {
            ranked.AddRange(highlights.Where(h => h.Detector == detector && !ranked.Contains(h)));
        }

        ranked.AddRange(highlights.Where(h => !ranked.Contains(h)));
        return ranked;
    }

    /// <summary>
    /// Writes an already-saved outing up again, now that more is known about it.
    /// </summary>
    private async Task RecurateAsync(string activityId)
    {
        var activity = await LoadActivityAsync(activityId);

        if (activity == null)
        {
            return;
        }

        var highlights = await LoadHighlightsAsync(activityId);

        if (highlights.Count == 0)
        {
            return;
        }

        try
        {
            var curation = await ComposeCurationAsync(activity, highlights);

            await SaveWriteUpAsync(
                activityId,
                string.IsNullOrWhiteSpace(curation.Title) ? activity.Title : curation.Title.Trim(),
                string.IsNullOrWhiteSpace(curation.Story) ? activity.Story : curation.Story.Trim());

            Log.Instance.Info($"Rewrote the story for {activityId} from {highlights.Count} highlights");
        }
        catch (EmergenceStoppedException ex)
        {
            // The outing keeps the write-up it already had, which is a worse story rather than none.
            Log.Instance.Warning($"Could not rewrite the story for {activityId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes the title and story for one outing from what was measured.
    /// </summary>
    /// <remarks>
    /// Separate from the finish path because it has to be able to run twice. A device archive lands
    /// after the outing ends, so the first write only ever sees the GPS detectors — the first real
    /// drive was written up as "the standout moment was hitting 163.5 km/h" while the far better
    /// fact, that it held 0.58 m/s³ of jerk at those speeds, had not arrived yet.
    /// </remarks>
    private static async Task<Curation> ComposeCurationAsync(Activity activity, IReadOnlyList<Highlight> highlights)
    {
        return await Emerge.Run<Curation>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.SystemPrompt = """
                You write up one person's outing for their own log. You are given what actually
                happened, measured. Use it and nothing else.

                Title: three to five words, specific to this outing, no exclamation marks, no
                puns on the app's name, never the word "epic".
                Story: two sentences. The first says what the outing was. The second names the one
                moment that made it worth keeping. Prefer how the outing was ridden or driven over how
                fast: measured cornering, braking, grip, smoothness or rhythm beat a peak figure
                whenever they are present, and an easy line held at speed is a better story than the
                speed. State that fact plainly — never remark on what other measurements could or
                could not show, and never mention stopwatches, sensors or data.
                ReelDetectors: the detector keys you were given, ordered best first, at most five.
                Include only the ones a person would actually want to see again.
                """;
            pass.Command = $"""
                Kind: {activity.KindLabel}
                Started: {activity.StartedAt.ToLocalTime():dddd HH:mm}
                Distance: {Momentum.FormatDistance(activity.DistanceM)}
                Moving time: {Momentum.FormatDuration(activity.MovingSeconds)}
                Average: {Momentum.FormatRate(activity.Kind, activity.AvgSpeedMps)}
                Climbed: {activity.AscentM:0} m, descended {activity.DescentM:0} m

                Detected highlights:
                {string.Join("\n", highlights.Select(h => $"- {h.Detector}: {h.Title} — {h.Detail} (score {h.Score:0}, {Brand.MedalLabel(h.Tier)})"))}
                """;
            pass.MaxWallTime = TimeSpan.FromSeconds(60);
        });
    }

}
