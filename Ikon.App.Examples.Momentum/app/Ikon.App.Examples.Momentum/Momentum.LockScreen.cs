public partial class MomentumApp
{
    /// <summary>
    /// The tag is what makes this a readout rather than a stream of alerts: a notification sent under
    /// a tag it has already used replaces the one on screen instead of stacking a new one beneath it.
    /// </summary>
    private const string LockScreenTag = "momentum-live";

    /// <summary>
    /// How far the rider goes between updates. This is deliberately a distance and not a clock.
    ///
    /// The first attempt ticked every twenty seconds at <see cref="NotificationPriority.Low"/>, which
    /// never appeared at all: Low maps to iOS's passive interruption level, and a passive notification
    /// goes straight to Notification Centre without ever lighting the lock screen. At a level that
    /// does show, every update alerts — iOS has no "update quietly" for an ordinary notification — so
    /// a twenty-second tick would buzz a wrist two hundred times on a long ride. A kilometre is a
    /// milestone worth feeling, and it is the number a runner wants anyway.
    /// </summary>
    private const double LockScreenIntervalM = 1000;

    private double _lastLockScreenDistanceM = -1;
    private RecordingState _lastLockScreenState = RecordingState.Idle;
    private bool _liveActivityStarted;
    private long _lastLiveActivityAtMs;

    /// <summary>
    /// How often the banner's numbers are refreshed. Unlike a notification this costs the rider
    /// nothing — the banner updates in place and never alerts — so it can move at the pace of the
    /// numbers rather than at the pace of what a wrist can tolerate.
    /// </summary>
    private const long LiveActivityIntervalMs = 3_000;

    /// <summary>
    /// Keeps the numbers reachable while the phone is in a pocket — distance and moving time on the
    /// first line, pace and climbing on the second — by posting under one tag that replaces itself.
    ///
    /// **This is not a Live Activity.** A readout that sits on the lock screen and updates silently
    /// and continuously needs iOS's ActivityKit, which is native Swift the Flutter shell does not
    /// carry. What this does instead is mark the milestones, which is most of the value and none of
    /// the platform work.
    /// </summary>
    private async Task LockScreenLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await PushLiveActivityAsync(ct);
                await PushLockScreenAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // The outing ended; the closing notification is sent by the finish path, not from here.
        }
    }

    /// <summary>
    /// The live banner: distance, moving time and pace, on the lock screen and in the Dynamic Island,
    /// updating while the phone is in a pocket. This is what the milestone notification was standing
    /// in for — a notification can alert or stay hidden, and neither of those is a readout.
    /// </summary>
    private async Task PushLiveActivityAsync(CancellationToken ct)
    {
        TrackRecorder? recorder;
        RecordingState state;
        int clientId;

        lock (_sessionLock)
        {
            recorder = _recorder;
            state = _state;
            clientId = _recordingSessionId != 0 ? _recordingSessionId : _sessionClientId;
        }

        if (recorder is not { HasFix: true } || state == RecordingState.Idle || clientId == 0)
        {
            return;
        }

        long nowMs = Environment.TickCount64;

        if (_liveActivityStarted && nowMs - _lastLiveActivityAtMs < LiveActivityIntervalMs)
        {
            return;
        }

        _lastLiveActivityAtMs = nowMs;

        var frame = recorder.Snapshot([], "", _simulated, state);
        bool paused = state != RecordingState.Recording;

        var metrics = new List<LiveMetric>
        {
            new(Momentum.FormatDistance(frame.DistanceM), "distance"),
            new(Momentum.FormatDuration(frame.MovingSeconds), "moving"),
            new(Momentum.FormatRate(frame.Kind, frame.AvgSpeedMps), "average"),
        };

        string status = paused
            ? "Auto-paused"
            : Momentum.KindLabel(frame.Kind, frame.DistanceM, frame.MovingSeconds);

        try
        {
            if (_liveActivityStarted)
            {
                await app.LiveActivity.UpdateAsync(metrics, status, paused, clientId, ct);
            }
            else
            {
                _liveActivityStarted = await app.LiveActivity.StartAsync(
                    "Momentum", Brand.Magenta, metrics, status, paused, clientId, ct);
                Log.Instance.Debug($"Live activity start on session {clientId}: {_liveActivityStarted}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // No banner on this client — a browser, an Android phone, an older iOS. The outing is
            // unaffected and the milestone notification still carries the numbers.
            Log.Instance.Debug($"Live activity update for session {clientId} failed: {ex.Message}");
        }
    }

    private async Task PushLockScreenAsync(CancellationToken ct)
    {
        TrackRecorder? recorder;
        RecordingState state;

        lock (_sessionLock)
        {
            recorder = _recorder;
            state = _state;
        }

        if (recorder is not { HasFix: true } || state == RecordingState.Idle || _sessionUserId.Length == 0)
        {
            return;
        }

        var frame = recorder.Snapshot([], "", _simulated, state);

        // A milestone is a whole kilometre, or the moment the outing pauses or picks up again — the
        // two things a rider wants to feel without taking the phone out.
        bool crossedMilestone = _lastLockScreenDistanceM < 0
            || frame.DistanceM - _lastLockScreenDistanceM >= LockScreenIntervalM;
        bool changedState = state != _lastLockScreenState;

        if (!crossedMilestone && !changedState)
        {
            return;
        }

        _lastLockScreenDistanceM = crossedMilestone ? frame.DistanceM : _lastLockScreenDistanceM;
        _lastLockScreenState = state;

        string title = $"{Momentum.FormatDistance(frame.DistanceM)} · {Momentum.FormatDuration(frame.MovingSeconds)}";
        string body = state switch
        {
            RecordingState.AutoPaused => "Auto-paused — start moving and it picks up again",
            RecordingState.Paused => "Paused",
            _ => $"{Momentum.FormatRate(frame.Kind, frame.AvgSpeedMps)} average · {frame.AscentM:0} m climbed",
        };

        await SendLockScreenAsync(title, body, ct);
    }

    /// <summary>The last thing the readout says: the outing is over and what it came to.</summary>
    private async Task CloseLockScreenAsync(Activity activity)
    {
        _lastLockScreenDistanceM = -1;
        _lastLockScreenState = RecordingState.Idle;

        await ClearLiveActivityAsync();

        await SendLockScreenAsync(
            $"{activity.KindLabel} finished · {Momentum.FormatDistance(activity.DistanceM)}",
            "Tap to name it and publish it to your feed",
            CancellationToken.None);
    }

    private async Task ClearLiveActivityAsync()
    {
        _liveActivityStarted = false;
        await app.LiveActivity.EndEverywhereAsync();
    }

    private async Task SendLockScreenAsync(string title, string body, CancellationToken ct)
    {
        if (_sessionUserId.Length == 0)
        {
            return;
        }

        try
        {
            // To the user rather than a session id captured when the outing started: a phone that
            // reconnects comes back under a new id, and a readout addressed to the old one is a
            // readout nobody ever sees again.
            await app.Notifications.SendToUserAsync(
                _sessionUserId,
                new NotificationContent(title, body, Tag: LockScreenTag, LaunchUrl: "/?section=move"),
                ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The rider has notifications off, or every device is away. The recording is unaffected.
            Log.Instance.Debug($"Lock-screen readout for user {_sessionUserId} failed: {ex.Message}");
        }
    }
}
