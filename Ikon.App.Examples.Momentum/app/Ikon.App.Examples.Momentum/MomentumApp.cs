return await App.Run(args);

// One instance per rider: the log, the live recording and the coach are all theirs. A recording keeps
// running inside that instance whether or not anyone is looking at it — which is the whole point.
public record SessionIdentity(string? UserId);

// `?section=feed` opens a screen directly, from a share link or a home-screen shortcut.
public record ClientParameters(string Name = "", string Section = "");

[App]
public partial class MomentumApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, Brand.Theme);

    private Audio Audio { get; } = new(app);

    private ClientProfiles Profiles { get; } = new(app);

    private readonly ClientReactive<MomentumSection> _section = new(MomentumSection.Move);
    private readonly ClientReactive<string?> _openActivityId = new(null);
    private readonly ClientReactive<string?> _focusedHighlightId = new(null);
    private readonly ClientReactive<bool> _isNativeClient = new(false);
    private readonly ClientReactive<bool> _startSheetOpen = new(false);

    private readonly UserReactive<string> _riderName = new("You");
    private readonly UserReactive<bool> _coachVoice = new(true);
    private readonly UserReactive<ActivityKind> _plannedKind = new(ActivityKind.Foot);
    private readonly UserReactive<string> _plannedRouteId = new("toolonlahti");
    private readonly UserReactive<bool> _useSimulator = new(false);
    private readonly UserReactive<bool> _feedLoading = new(false);

    // One bump per recorded fix batch. The live screen reads plain fields under the recorder's own
    // lock at render time rather than mirroring every metric into its own reactive, so a 1 Hz stream
    // costs one diff per tick instead of a dozen.
    private readonly Reactive<long> _frame = new(0);

    // Not user-scoped: an outing can finish on the simulator's own thread, which has no client or user
    // scope to write a UserReactive from. The app instance is already one per rider, so an instance
    // reactive is the same value with none of that hazard.
    private readonly Reactive<string> _titleDraft = new("");
    private readonly Reactive<long> _logVersion = new(0);

    private readonly ConcurrentDictionary<string, IReadOnlyList<Activity>> _activitiesByUser = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<TrackPoint>> _pointsByActivity = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<Highlight>> _highlightsByActivity = new();
    private readonly ConcurrentDictionary<int, string> _trackedSessions = new();

    private readonly object _sessionLock = new();
    private TrackRecorder? _recorder;
    private RecordingState _state = RecordingState.Idle;
    private CancellationTokenSource? _sessionCts;
    private Task? _simulationTask;
    private Task? _coachTask;
    private Task? _lockScreenTask;
    private Task? _acquiringTask;
    private IAsyncDisposable? _backgroundWork;
    private bool _simulated;

    // Captured when the outing starts. ReactiveScope.UserIdOrNull is empty on the simulator's thread,
    // and an activity saved against an empty user id is one the rider never sees again.
    private string _sessionUserId = "";
    private int _sessionClientId;
    private int _recordingSessionId;
    private long _lastAcceptedFixAtMs;
    private DateTime _sessionStartedAt = DateTime.MinValue;

    // The row being written as the outing happens, and how much of the track has reached it.
    private string _activeActivityId = "";
    private int _flushedPointCount;
    private Task? _flushTask;
    private string _coachCue = "";
    private IReadOnlyList<Highlight> _liveHighlights = [];
    private PendingPublication? _pending;

    public async Task Main()
    {
        await EnsureSchemaAsync();

        // Every fix a tracked client produces lands here, whether the app is in the foreground, in the
        // background, or the phone is locked in a pocket.
        app.Locations.OnUpdate(OnLocationUpdate);

        // Stride cadence, which GPS cannot see: a phone's speed trace cannot tell a collected canter
        // from a fast trot, and the difference is in the rhythm.
        app.Motion.OnBatch(OnMotionBatch);

        // What a phone kept for itself while it was out of signal. It arrives after the outing and
        // repairs the saved track; nothing reads it live.
        app.Recordings.OnArchive(archive => _ = OnArchiveAsync(archive));

        app.OnClientJoined(async (ctx, parameters) =>
        {
            if (Enum.TryParse<MomentumSection>(parameters.Section, ignoreCase: true, out var section))
            {
                _section.Value = section;
            }

            _isNativeClient.Value = ctx.SdkType == Ikon.Common.Core.Protocol.SdkType.Dart;

            // A phone may be carrying an outing it could not send when it ended. Asking costs nothing
            // when there is none, and is the only thing that gets a ride home after a dead spot.
            _ = app.Recordings.RequestPendingAsync(ctx.ClientSessionId);

            // A banner outlives the app that started it, so a phone can come back with one still on
            // the lock screen for an outing that is over. Nothing is recording, so nothing should be
            // showing.
            bool idle;

            lock (_sessionLock)
            {
                idle = _state == RecordingState.Idle;
            }

            if (idle)
            {
                _ = app.LiveActivity.EndAsync(ctx.ClientSessionId);
            }

            if (ReactiveScope.UserIdOrNull is not { Length: > 0 } userId)
            {
                return;
            }

            var profile = await Profiles.GetProfileAsync(ctx);
            string? name = profile?.VisibleName is { Length: > 0 } visible ? visible : profile?.Name;

            if (name is { Length: > 0 })
            {
                _riderName.Value = name;
            }

            // Loading the log takes a few round trips to Postgres; the join must not wait on it or the
            // first paint does.
            if (!_activitiesByUser.ContainsKey(userId))
            {
                _feedLoading.Value = true;
                _ = LoadLogAsync(userId);
            }

            // If the app restarted under a rider who was mid-outing — a deploy, a crash, an idle
            // shutdown — pick their ride back up rather than losing it. And whether or not it did,
            // make sure this client is streaming: a reconnecting phone is a new session, and an
            // outing already running would otherwise never hear from it again.
            _ = ResumeAndTrackAsync(userId);
        });

        // A departing client releases both its stream slot and, if it held it, the fix lock — so the
        // device that comes back in its place is heard rather than ignored.
        app.OnClientLeft(async ctx => ReleaseClient(ctx.SessionId));

        app.OnStopping(StopEverythingAsync);

        BuildUi();
    }

    private RecordingState State
    {
        get
        {
            lock (_sessionLock)
            {
                return _state;
            }
        }
    }

    private bool IsRecording => State is RecordingState.Recording or RecordingState.AutoPaused or RecordingState.Paused;

    private IReadOnlyList<Activity> ActivitiesFor(string userId)
    {
        _ = _logVersion.Value;
        return _activitiesByUser.TryGetValue(userId, out var activities) ? activities : [];
    }

    private async Task LoadLogAsync(string userId)
    {
        try
        {
            var activities = await LoadActivitiesAsync(userId);

            if (activities.Count == 0)
            {
                await SeedLogAsync(userId);
                activities = await LoadActivitiesAsync(userId);
            }

            _activitiesByUser[userId] = activities;
            _logVersion.Value++;

            // The feed paints without the tracks and fills its thumbnails in as they arrive; blocking
            // the log on a second query per activity would hold the whole screen for the slowest one.
            _ = WarmThumbnailsAsync(activities.Take(12).ToList());
        }
        catch (Exception ex)
        {
            // The feed stays empty and says so; nothing else in the app depends on the log having
            // loaded, and a rider can still start recording.
            Log.Instance.Error($"Loading the activity log for user {userId} failed: {ex}");
        }
        finally
        {
            _feedLoading.Value = false;
        }
    }

    private async Task WarmThumbnailsAsync(IReadOnlyList<Activity> activities)
    {
        foreach (var activity in activities)
        {
            if (_pointsByActivity.ContainsKey(activity.Id))
            {
                continue;
            }

            try
            {
                _pointsByActivity[activity.Id] = await LoadPointsAsync(activity.Id);
                _logVersion.Value++;
            }
            catch (Exception ex)
            {
                // One missing thumbnail is a grey box on one row; the rest of the feed is unaffected.
                Log.Instance.Warning($"Loading the track for activity {activity.Id} failed: {ex.Message}");
            }
        }
    }

    private async Task<IReadOnlyList<TrackPoint>> PointsForAsync(string activityId)
    {
        if (_pointsByActivity.TryGetValue(activityId, out var cached))
        {
            return cached;
        }

        var points = await LoadPointsAsync(activityId);
        _pointsByActivity[activityId] = points;
        return points;
    }

    private async Task<IReadOnlyList<Highlight>> HighlightsForAsync(string activityId)
    {
        if (_highlightsByActivity.TryGetValue(activityId, out var cached))
        {
            return cached;
        }

        var highlights = await LoadHighlightsAsync(activityId);
        _highlightsByActivity[activityId] = highlights;
        return highlights;
    }
}
