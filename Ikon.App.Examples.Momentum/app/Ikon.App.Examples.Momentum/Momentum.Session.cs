/// <summary>
/// A finished activity waiting on the rider. The AI has named it, written it up and picked the reel;
/// nothing reaches the feed until the rider says so.
/// </summary>
public sealed record PendingPublication(
    Activity Activity,
    IReadOnlyList<TrackPoint> Points,
    IReadOnlyList<Highlight> Highlights,
    HashSet<string> Dropped);

public partial class MomentumApp
{
    /// <summary>
    /// One second between fixes, no distance filter. A tracker wants the stationary fixes as much as
    /// the moving ones: without them auto-pause cannot tell a red light from a lost signal, and the
    /// traffic-light-launch detector has nothing to measure from.
    /// </summary>
    private static readonly LocationTrackingOptions TrackingOptions = new(
        IntervalSeconds: 1,
        DistanceFilterMeters: 0,
        Background: true,
        NotificationTitle: "Momentum is recording",
        NotificationBody: "Your route is being tracked. Tap to open.");

    private const double LiveDetectorIntervalSeconds = 15;

    /// <summary>
    /// Fifty hertz resolves a footfall comfortably — a sprinter's stride is about three a second, and
    /// Nyquist wants well above six. Half-second batches keep it to two calls a second.
    /// </summary>
    /// <summary>What is being recorded right now, for the paths that arm a device after the start.</summary>
    private ActivityKind ActiveKind
    {
        get
        {
            lock (_sessionLock)
            {
                return _recorder?.Kind ?? ActivityKind.Foot;
            }
        }
    }

    /// <summary>
    /// Fifty hertz with the gyroscope for everything, because the phone keeps every sample itself and
    /// the network never sees most of them. A car earns it as much as a horse does: braking, launches
    /// and cornering load are all in the accelerometer and none of them are in a speed trace.
    ///
    /// Only twelve a second go over the wire. That is ample for the cadence on the live screen — a
    /// sprinter's stride is about three a second — and it is the difference between spending 21 MB an
    /// hour of the rider's own data and spending about 2.
    /// </summary>
    private static MotionOptions MotionOptionsFor(ActivityKind kind) => new(
        Hertz: 50,
        Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope,
        BatchMilliseconds: 500,
        Background: true,
        LiveHertz: 12);

    /// <summary>
    /// How often the live screen is allowed to redraw, in wall-clock time. A phone reports once a
    /// second and needs no throttle at all; a simulated outing replaying at thirty times normal speed
    /// would otherwise push thirty UI diffs a second at every connected client and starve the very
    /// screen it is trying to update.
    /// </summary>
    private const long FrameIntervalMs = 200;

    /// <summary>
    /// How long the owning device may go silent before another one may take over. Long enough that a
    /// short tunnel does not hand the outing to a laptop's wifi position, short enough that a rider
    /// whose phone reconnects is recording again within seconds.
    /// </summary>
    private const long OwnershipLapseMs = 30_000;

    private double _lastLiveDetectorRunSeconds;
    private long _lastFrameAtMs;

    private async Task StartAsync(ActivityKind kind, bool simulated, string routeId, string preset)
    {
        if (IsRecording)
        {
            return;
        }

        var recorder = new TrackRecorder(kind);
        var cts = new CancellationTokenSource();

        lock (_sessionLock)
        {
            _recorder = recorder;
            _state = RecordingState.Recording;
            _sessionCts = cts;
            _simulated = simulated;
            _sessionUserId = ReactiveScope.UserIdOrNull ?? "";
            _sessionStartedAt = DateTime.UtcNow;
            _activeActivityId = Guid.NewGuid().ToString("N");
            _flushedPointCount = 0;
            _sessionClientId = ReactiveScope.ClientIdOrNull ?? 0;
            _recordingSessionId = 0;
            _lastFrameAtMs = 0;
            _coachCue = "";
            _liveHighlights = [];
            _lastLiveDetectorRunSeconds = 0;
            _pending = null;
        }

        _startSheetOpen.Value = false;
        _section.Value = MomentumSection.Move;

        // The row exists from the first second. Everything after this is an update, so a deploy or a
        // crash under the rider costs the seconds since the last flush rather than the whole outing.
        if (_sessionUserId.Length > 0)
        {
            try
            {
                await BeginActivityAsync(_activeActivityId, _sessionUserId, kind, DateTime.UtcNow, simulated);
            }
            catch (Exception ex)
            {
                // Recording still works; it just is not crash-proof this time.
                Log.Instance.Error($"Could not open the activity row for {_activeActivityId}: {ex}");
            }
        }

        // Without this the instance idles out the moment the phone's screen goes off and the last
        // client stops drawing — taking the recording with it.
        _backgroundWork = await app.BackgroundWork.StartAsync();

        if (simulated)
        {
            _simulationTask = Task.Run(() => PlaySimulationAsync(recorder, kind, routeId, preset, cts.Token), cts.Token);
        }
        else
        {
            await StartDeviceTrackingAsync();
        }

        _coachTask = Task.Run(() => CoachLoopAsync(cts.Token), cts.Token);
        _lockScreenTask = Task.Run(() => LockScreenLoopAsync(cts.Token), cts.Token);
        _acquiringTask = Task.Run(() => AcquiringTickAsync(recorder, cts.Token), cts.Token);
        _flushTask = Task.Run(() => FlushLoopAsync(recorder, _activeActivityId, cts.Token), cts.Token);
        _frame.Value++;
    }

    /// <summary>
    /// Asks every one of the rider's connected devices to start streaming, and lets the first one to
    /// produce a usable fix become the recorder for the rest of the outing.
    ///
    /// The rider is one person with one instance of this app, and the device they press Start on is
    /// very often not the device in their pocket — pressing it on a laptop and having the phone record
    /// is the behaviour they expect, not an error. Locking onto the first device to answer is what
    /// keeps that from turning into two tracks braided together.
    /// </summary>
    /// <summary>
    /// Ticks once a second until the first fix lands. Nothing else bumps the frame before then — the
    /// recorder has had nothing to record — so without this the waiting screen would freeze on the
    /// second it appeared, which reads exactly like the app having done nothing at all.
    /// </summary>
    private async Task AcquiringTickAsync(TrackRecorder recorder, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && !recorder.HasFix)
            {
                _frame.Value++;
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // The outing ended before a fix ever arrived.
        }
    }

    /// <summary>
    /// Writes the track down as it is recorded. Five seconds is the most an interrupted outing loses,
    /// and at one fix a second that is a five-row insert — far cheaper than the round trip per fix a
    /// tighter interval would cost.
    /// </summary>
    private async Task FlushLoopAsync(TrackRecorder recorder, string activityId, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                await FlushAsync(recorder, activityId);
            }
        }
        catch (OperationCanceledException)
        {
            // The outing ended; the finish path does the last flush itself.
        }
    }

    private async Task FlushAsync(TrackRecorder recorder, string activityId)
    {
        if (activityId.Length == 0 || _sessionUserId.Length == 0)
        {
            return;
        }

        var pending = recorder.PointsFrom(_flushedPointCount);

        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            await SaveProgressAsync(activityId, recorder.Progress(), pending);
            _flushedPointCount += pending.Count;
        }
        catch (Exception ex)
        {
            // Keep the unflushed points queued; the next flush carries them. The ride is unaffected.
            Log.Instance.Warning($"Flushing activity {activityId} failed, {pending.Count} points still pending: {ex.Message}");
        }
    }

    /// <summary>
    /// Picks up an outing the app was recording when it last stopped. A deploy, a crash or an idle
    /// shutdown mid-ride would otherwise throw the whole thing away — the one failure a tracker cannot
    /// have. A simulated outing is not resumed: its simulator is gone, so it is closed off instead.
    /// </summary>
    private async Task ResumeAndTrackAsync(string userId)
    {
        await ResumeInProgressAsync(userId);
        await EnsureTrackingForClientAsync();
    }

    private async Task ResumeInProgressAsync(string userId)
    {
        if (IsRecording)
        {
            return;
        }

        (Activity Activity, bool Simulated)? found;

        try
        {
            found = await LoadInProgressAsync(userId);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Looking for an interrupted outing for user {userId} failed: {ex}");
            return;
        }

        if (found is not { } resume)
        {
            return;
        }

        var points = await LoadPointsAsync(resume.Activity.Id);

        if (resume.Simulated || points.Count < 5)
        {
            // Nothing worth carrying over, and a simulated route cannot be picked up mid-stride.
            await DeleteActivityAsync(resume.Activity.Id);
            return;
        }

        var recorder = TrackRecorder.Restore(
            resume.Activity.Kind,
            resume.Activity.StartedAt,
            points,
            resume.Activity.DistanceM,
            resume.Activity.MovingSeconds,
            resume.Activity.AscentM,
            resume.Activity.DescentM,
            resume.Activity.MaxSpeedMps);

        var cts = new CancellationTokenSource();

        lock (_sessionLock)
        {
            _recorder = recorder;
            _state = RecordingState.Recording;
            _sessionCts = cts;
            _simulated = false;
            _sessionUserId = userId;
            _sessionClientId = ReactiveScope.ClientIdOrNull ?? 0;
            _sessionStartedAt = resume.Activity.StartedAt;
            _activeActivityId = resume.Activity.Id;
            _flushedPointCount = points.Count;
            _recordingSessionId = 0;
            _lastFrameAtMs = 0;
            _liveHighlights = [];
            // The rider was out there while the server went down and came back. Say so — silence after
            // a reconnect reads as "did I just lose my ride?".
            _coachCue = $"Back with you — your {Momentum.KindLabel(resume.Activity.Kind, resume.Activity.DistanceM, resume.Activity.MovingSeconds).ToLowerInvariant()} is still going. {Momentum.FormatDistance(resume.Activity.DistanceM)} kept.";
            _pending = null;
        }

        _backgroundWork = await app.BackgroundWork.StartAsync();
        await StartDeviceTrackingAsync();

        _coachTask = Task.Run(() => CoachLoopAsync(cts.Token), cts.Token);
        _lockScreenTask = Task.Run(() => LockScreenLoopAsync(cts.Token), cts.Token);
        _flushTask = Task.Run(() => FlushLoopAsync(recorder, resume.Activity.Id, cts.Token), cts.Token);

        _section.Value = MomentumSection.Move;
        _frame.Value++;

        Log.Instance.Info($"Resumed interrupted outing {resume.Activity.Id} for user {userId} at {resume.Activity.DistanceM:0} m");
    }

    /// <summary>
    /// Re-arms location streaming for a client that has just (re)joined while an outing is running.
    ///
    /// A phone that reconnects — a dropped socket, a restarted app, a redeploy — comes back as a NEW
    /// session. The outing is still recording on the server, but the stream feeding it went with the
    /// old session, and nothing else asks the new one to start. Without this the recording is alive
    /// and deaf: the track simply stops where the connection dropped, the auto-pause never lifts
    /// because no fix ever contradicts it, and restarting the app makes it worse rather than better.
    /// </summary>
    private async Task EnsureTrackingForClientAsync()
    {
        int sessionId = ReactiveScope.ClientIdOrNull ?? 0;

        lock (_sessionLock)
        {
            if (sessionId == 0 || _simulated || _state == RecordingState.Idle)
            {
                return;
            }
        }

        if (_trackedSessions.ContainsKey(sessionId))
        {
            return;
        }

        try
        {
            if (await app.Locations.StartTrackingAsync(sessionId, TrackingOptions))
            {
                _trackedSessions[sessionId] = _sessionUserId;
                Log.Instance.Info($"Re-armed location tracking for reconnected session {sessionId}");

                // Armed on the same path as location so the two cannot drift apart.
                await app.Motion.StartTrackingAsync(sessionId, MotionOptionsFor(ActiveKind));
                await app.Recordings.StartAsync(sessionId, _activeActivityId);
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Re-arming location tracking for session {sessionId} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Lets go of a client that has left. The fix lock has to go with it: it names the one device
    /// whose fixes count, and if it keeps naming a session that no longer exists then the phone's
    /// fixes are discarded the moment it reconnects under a new id.
    /// </summary>
    private void ReleaseClient(int sessionId)
    {
        _trackedSessions.TryRemove(sessionId, out _);

        lock (_sessionLock)
        {
            if (_recordingSessionId == sessionId)
            {
                _recordingSessionId = 0;
            }
        }
    }

    private async Task StartDeviceTrackingAsync()
    {
        var sessions = app.Clients.Ids.ToList();
        int accepted = 0;

        foreach (int sessionId in sessions)
        {
            try
            {
                if (await app.Locations.StartTrackingAsync(sessionId, TrackingOptions))
                {
                    _trackedSessions[sessionId] = _sessionUserId;
                    accepted++;

                    await app.Motion.StartTrackingAsync(sessionId, MotionOptionsFor(ActiveKind));

                    // The copy that survives a tunnel. Everything above is the live view of the
                    // outing; this is its record, and it is written to the phone's own storage.
                    await app.Recordings.StartAsync(sessionId, _activeActivityId);
                }
            }
            catch (Exception ex)
            {
                // One device declining or dropping out says nothing about the others.
                Log.Instance.Debug($"Session {sessionId} would not start location tracking: {ex.Message}");
            }
        }

        if (accepted == 0)
        {
            // Nothing here can produce a fix — no geolocation, or the permission was denied outright.
            // Say so rather than showing a recording screen that will never move.
            _coachCue = "None of your devices would share their location. Check the app's location permission, or record a simulated outing instead.";
            _frame.Value++;
        }
    }

    private void OnLocationUpdate(LocationUpdate update)
    {
        TrackRecorder? recorder;

        lock (_sessionLock)
        {
            // A simulated outing ignores the device: mixing a real phone's fixes into a simulated
            // track would tear the route in two.
            if (_simulated || _state is RecordingState.Idle or RecordingState.Paused)
            {
                return;
            }

            // The first device to deliver a fix owns the outing. Every other device the rider has open
            // is still streaming — a laptop's coarse wifi position among them — and letting a second
            // one in would braid two tracks into one.
            //
            // The lock also has to heal. A phone can stop delivering without ever leaving cleanly (a
            // tunnel, a suspended app, a killed socket), and a lock held by a session that has gone
            // quiet would discard every fix from the device that comes back in its place.
            long nowMs = Environment.TickCount64;

            if (_recordingSessionId == 0 || nowMs - _lastAcceptedFixAtMs > OwnershipLapseMs)
            {
                if (_recordingSessionId != update.SessionId && _recordingSessionId != 0)
                {
                    Log.Instance.Info($"Location ownership passed from session {_recordingSessionId} to {update.SessionId} after a lapse");
                }

                _recordingSessionId = update.SessionId;
            }
            else if (_recordingSessionId != update.SessionId)
            {
                return;
            }

            _lastAcceptedFixAtMs = nowMs;

            recorder = _recorder;
        }

        if (recorder == null)
        {
            return;
        }

        recorder.Push(new RawFix(
            update.MeasuredAt,
            new GeoPoint(update.Latitude, update.Longitude),
            update.AltitudeMeters,
            update.SpeedMps,
            update.Heading,
            update.AccuracyMeters));

        AfterFix(recorder);
    }

    /// <summary>Everything that has to happen once per accepted fix, from either source.</summary>
    private void AfterFix(TrackRecorder recorder)
    {
        long nowMs = Environment.TickCount64;
        bool dueForFrame = nowMs - _lastFrameAtMs >= FrameIntervalMs;
        var points = recorder.PointsSnapshot();

        if (dueForFrame && points.Count > 0 && points[^1].Seconds - _lastLiveDetectorRunSeconds >= LiveDetectorIntervalSeconds)
        {
            _lastLiveDetectorRunSeconds = points[^1].Seconds;

            // Cheap enough at this cadence — a couple of thousand points through the detector set is
            // well under a millisecond, and it is what makes a highlight appear while you are still
            // moving rather than only in the write-up.
            _liveHighlights = Detectors.Detect("live", recorder.Kind, points);
        }

        bool stateChanged = false;

        lock (_sessionLock)
        {
            if (_state == RecordingState.Recording && points.Count > 0 && !points[^1].Moving)
            {
                _state = RecordingState.AutoPaused;
                stateChanged = true;
            }
            else if (_state == RecordingState.AutoPaused && points.Count > 0 && points[^1].Moving)
            {
                _state = RecordingState.Recording;
                stateChanged = true;
            }
        }

        // An auto-pause is the one thing the rider must see immediately; everything else can wait for
        // the next frame slot.
        if (!dueForFrame && !stateChanged)
        {
            return;
        }

        _lastFrameAtMs = nowMs;
        _frame.Value++;
    }

    /// <summary>
    /// Plays a simulated outing into the recorder. The physics step is always one simulated second;
    /// <see cref="SimulationPlan.SpeedMultiplier"/> only compresses the wait between them, so a
    /// forty-minute drive can be watched in a minute without changing a single number the detectors see.
    /// </summary>
    private async Task PlaySimulationAsync(TrackRecorder recorder, ActivityKind kind, string routeId, string preset, CancellationToken ct)
    {
        try
        {
            var route = Routes.All.FirstOrDefault(r => r.Id == routeId) ?? Routes.DefaultFor(kind);
            var plan = new SimulationPlan(route, kind, Seed: Environment.TickCount, SpeedMultiplier: 30, Preset: preset);
            var startedAt = DateTime.UtcNow;
            double delayMs = 1000 / plan.SpeedMultiplier;

            foreach (var fix in new TrackSimulator(plan).Fixes(startedAt))
            {
                ct.ThrowIfCancellationRequested();
                recorder.Push(fix);
                AfterFix(recorder);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
            }

            await FinishAsync();
        }
        catch (OperationCanceledException)
        {
            // The rider finished or discarded the outing; the recorder already holds everything.
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Simulated outing on route {routeId} failed: {ex}");
        }
    }

    private void TogglePause()
    {
        lock (_sessionLock)
        {
            _state = _state switch
            {
                RecordingState.Paused => RecordingState.Recording,
                RecordingState.Recording or RecordingState.AutoPaused => RecordingState.Paused,
                _ => _state,
            };
        }

        _frame.Value++;
    }

    /// <summary>
    /// What the Finish button does. The screen change belongs here rather than in
    /// <see cref="FinishAsync"/>, because that one also runs when the simulator reaches the end of its
    /// route — on its own thread, where there is no client to navigate.
    /// </summary>
    private async Task FinishFromUiAsync()
    {
        _openActivityId.Value = null;
        _section.Value = MomentumSection.Move;
        await FinishAsync();
    }

    private async Task FinishAsync()
    {
        TrackRecorder? recorder;

        lock (_sessionLock)
        {
            if (_state == RecordingState.Idle)
            {
                return;
            }

            recorder = _recorder;
            _state = RecordingState.Idle;
        }

        await StopSourcesAsync();
        await ClearLiveActivityAsync();

        if (recorder == null || !recorder.HasFix)
        {
            _frame.Value++;
            return;
        }

        var track = recorder.Finish();

        if (track.Points.Count < 10 || track.DistanceM < 50)
        {
            // Too short to be an outing — a stray tap on Start. Nothing is saved and nothing is said.
            _frame.Value++;
            return;
        }

        string userId = _sessionUserId;
        string activityId = _activeActivityId.Length > 0 ? _activeActivityId : Guid.NewGuid().ToString("N");

        // Everything since the last flush, before the recorder is let go.
        await FlushAsync(recorder, activityId);

        var bests = userId.Length > 0 ? await LoadPersonalBestsAsync(userId, track.Kind) : [];
        var highlights = Detectors.Detect(activityId, track.Kind, track.Points, detector => bests.GetValueOrDefault(detector));

        var activity = new Activity(
            activityId,
            userId,
            track.Kind,
            DefaultTitle(track),
            "",
            track.StartedAt,
            track.DistanceM,
            track.MovingSeconds,
            track.ElapsedSeconds,
            track.AscentM,
            track.DescentM,
            track.AvgSpeedMps,
            track.MaxSpeedMps,
            MomentumScore(highlights),
            Published: false);

        lock (_sessionLock)
        {
            _pending = new PendingPublication(activity, track.Points, highlights, []);
        }

        _titleDraft.Value = activity.Title;
        _frame.Value++;

        _ = CloseLockScreenAsync(activity);

        // The write-up is a model call and takes a few seconds; the reel is already on screen while it
        // runs, and the title updates in place when it lands.
        _ = CurateAsync(activityId);
    }

    private async Task DiscardPendingAsync()
    {
        PendingPublication? pending;

        lock (_sessionLock)
        {
            pending = _pending;
            _pending = null;
        }

        if (pending != null)
        {
            try
            {
                // The row and its points were written as the outing happened, so discarding has to
                // actually remove them rather than simply forget the in-memory copy.
                await DeleteActivityAsync(pending.Activity.Id);
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Discarding activity {pending.Activity.Id} failed: {ex.Message}");
            }
        }

        _frame.Value++;
    }

    private async Task PublishPendingAsync()
    {
        PendingPublication? pending;

        lock (_sessionLock)
        {
            pending = _pending;
        }

        if (pending == null)
        {
            return;
        }

        var kept = pending.Highlights.Where(h => !pending.Dropped.Contains(h.Id)).ToList();
        string title = string.IsNullOrWhiteSpace(_titleDraft.Value) ? pending.Activity.Title : _titleDraft.Value.Trim();
        var activity = pending.Activity with { Title = title, MomentumScore = MomentumScore(kept), Published = true };

        try
        {
            await FinalizeActivityAsync(activity, pending.Points, kept);
            _pointsByActivity[activity.Id] = pending.Points;
            _highlightsByActivity[activity.Id] = kept;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Saving activity {activity.Id} failed: {ex}");
            _coachCue = "That outing could not be saved. It is still on screen — try publishing again.";
            _frame.Value++;
            return;
        }

        lock (_sessionLock)
        {
            _pending = null;
        }

        if (activity.UserId.Length > 0)
        {
            await LoadLogAsync(activity.UserId);
        }

        _openActivityId.Value = activity.Id;
        _section.Value = MomentumSection.Activity;
        _frame.Value++;
    }

    private void ToggleDropped(string highlightId)
    {
        lock (_sessionLock)
        {
            if (_pending == null)
            {
                return;
            }

            if (!_pending.Dropped.Remove(highlightId))
            {
                _pending.Dropped.Add(highlightId);
            }
        }

        _frame.Value++;
    }

    /// <summary>
    /// One number for the outing: the best of each medal-worthy thing it contained, weighted so a
    /// single spectacular moment cannot carry a whole hour on its own.
    /// </summary>
    private static double MomentumScore(IReadOnlyList<Highlight> highlights)
    {
        if (highlights.Count == 0)
        {
            return 0;
        }

        var ranked = highlights.OrderByDescending(h => h.Score).ToList();
        double weighted = 0;
        double weight = 0;

        for (int i = 0; i < ranked.Count && i < 6; i++)
        {
            double w = 1.0 / (i + 1);
            weighted += ranked[i].Score * w;
            weight += w;
        }

        return Math.Round(weighted / weight, 1);
    }

    private static string DefaultTitle(RecordedTrack track)
    {
        string kind = Momentum.KindLabel(track.Kind, track.DistanceM, track.MovingSeconds);
        string when = track.StartedAt.ToLocalTime().Hour switch
        {
            < 6 => "Night",
            < 11 => "Morning",
            < 15 => "Midday",
            < 19 => "Afternoon",
            _ => "Evening",
        };

        return $"{when} {kind.ToLowerInvariant()}";
    }

    private async Task StopSourcesAsync()
    {
        CancellationTokenSource? cts;
        Task? simulation;
        Task? coach;
        Task? lockScreen;
        Task? acquiring;
        Task? flush;

        lock (_sessionLock)
        {
            cts = _sessionCts;
            simulation = _simulationTask;
            coach = _coachTask;
            lockScreen = _lockScreenTask;
            acquiring = _acquiringTask;
            flush = _flushTask;
            _sessionCts = null;
            _simulationTask = null;
            _coachTask = null;
            _lockScreenTask = null;
            _acquiringTask = null;
            _flushTask = null;
        }

        if (cts != null)
        {
            await cts.CancelAsync();
        }

        string finishedActivityId = _activeActivityId;

        foreach (int sessionId in _trackedSessions.Keys)
        {
            try
            {
                if (finishedActivityId.Length > 0)
                {
                    // Seals the phone's own recording and starts it uploading. It may land minutes
                    // later, or after the next reconnect if the outing ended somewhere with no signal.
                    await app.Recordings.StopAsync(sessionId, finishedActivityId);
                }

                await app.Locations.StopTrackingAsync(sessionId);
            }
            catch (Exception ex)
            {
                // The client is already gone, which is the outcome we wanted anyway.
                Log.Instance.Debug($"Stopping location tracking for session {sessionId} failed: {ex.Message}");
            }
        }

        _trackedSessions.Clear();

        foreach (var task in new[] { simulation, coach, lockScreen, acquiring, flush })
        {
            if (task == null)
            {
                continue;
            }

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected: this is how both loops end.
            }
        }

        cts?.Dispose();

        if (_backgroundWork != null)
        {
            await _backgroundWork.DisposeAsync();
            _backgroundWork = null;
        }
    }

    private async Task StopEverythingAsync()
    {
        lock (_sessionLock)
        {
            _state = RecordingState.Idle;
        }

        await StopSourcesAsync();
    }
}
