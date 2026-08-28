/// <summary>The finished track a recorder hands over, ready for the detectors and for Postgres.</summary>
public sealed record RecordedTrack(
    ActivityKind Kind,
    DateTime StartedAt,
    IReadOnlyList<TrackPoint> Points,
    double DistanceM,
    double MovingSeconds,
    double ElapsedSeconds,
    double AscentM,
    double DescentM,
    double MaxSpeedMps)
{
    public double AvgSpeedMps => MovingSeconds > 0 ? DistanceM / MovingSeconds : 0;
}

/// <summary>
/// Turns a stream of raw fixes into a track worth keeping. It rejects fixes it cannot trust, filters
/// the ones it does, decides when the rider has stopped, and keeps the running totals the live screen
/// reads.
///
/// One rule shapes the whole class: **an auto-pause never drops a fix.** It stops the clock and stops
/// the distance, and that is all. Every point stays on the track, because the stop itself is what the
/// traffic-light-launch detector is looking for.
/// </summary>
public sealed class TrackRecorder(ActivityKind kind)
{
    /// <summary>Beyond this the fix says more about the sky than about where the rider is.</summary>
    private const double RejectAccuracyM = 50;

    /// <summary>A fix this vague may be recorded but must not start or end a pause on its own.</summary>
    private const double UntrustedAccuracyM = 25;

    /// <summary>
    /// Below this a step is GPS creep rather than travel. It has to stay well under one second of
    /// walking — a floor above 1.4 m silently discards every step a walker takes and the distance
    /// total comes out barely half of what they covered.
    /// </summary>
    private const double MinStepM = 0.4;

    /// <summary>
    /// A step has to be at least this long before it says anything about which way the rider is facing.
    /// The distance floor is far below it deliberately: a short step still counts as travel, but the
    /// bearing across it is almost entirely noise.
    /// </summary>
    private const double MinHeadingStepM = 3.0;

    /// <summary>Net rise that has to accumulate before it counts as ascent, so vertical noise does not.</summary>
    private const double ElevationHysteresisM = 4.0;

    private readonly object _lock = new();
    private readonly KindProfile _profile = Momentum.ProfileOf(kind);
    private readonly List<TrackPoint> _points = [];

    private DateTime _startedAt = DateTime.MinValue;
    private DateTime _lastFixAt = DateTime.MinValue;
    private GeoPoint _filtered;
    private double _positionVariance;
    private double _filteredElevation = double.NaN;
    private double _elevationVariance;
    private double _elevationAnchor = double.NaN;
    private double _lastElevationSampleM;
    private double _distanceM;
    private double _movingSeconds;
    private double _elapsedSeconds;
    private double _ascentM;
    private double _descentM;
    private double _maxSpeedMps;
    private double _lastSpeedMps;
    private double _lastHeadingDeg;
    private double _lastAccuracyM;
    private double _slowSinceSeconds = double.NaN;
    private double _fastSinceSeconds = double.NaN;
    private GeoPoint _pausedAt;
    private bool _paused;

    public ActivityKind Kind => kind;

    public bool HasFix => _points.Count > 0;

    public DateTime StartedAt => _startedAt;

    /// <summary>
    /// Rebuilds a recorder from a track that was already written down, so an outing survives the
    /// server restarting under it. The stored points are replayed as state, not re-pushed: they have
    /// already been through the accuracy gate and the filters, and running them a second time would
    /// smooth an already-smoothed line and inflate the totals.
    /// </summary>
    public static TrackRecorder Restore(
        ActivityKind kind,
        DateTime startedAt,
        IReadOnlyList<TrackPoint> points,
        double distanceM,
        double movingSeconds,
        double ascentM,
        double descentM,
        double maxSpeedMps)
    {
        var recorder = new TrackRecorder(kind);

        if (points.Count == 0)
        {
            return recorder;
        }

        var last = points[^1];
        recorder._points.AddRange(points);
        recorder._startedAt = startedAt;
        recorder._lastFixAt = startedAt.AddSeconds(last.Seconds);
        recorder._filtered = last.Point;
        recorder._positionVariance = Math.Max(last.AccuracyM, 1.0) * Math.Max(last.AccuracyM, 1.0);
        recorder._filteredElevation = last.ElevationM;
        recorder._elevationVariance = 16.0;
        recorder._elevationAnchor = last.ElevationM;
        recorder._lastElevationSampleM = distanceM;
        recorder._distanceM = distanceM;
        recorder._movingSeconds = movingSeconds;
        recorder._elapsedSeconds = last.Seconds;
        recorder._ascentM = ascentM;
        recorder._descentM = descentM;
        recorder._maxSpeedMps = maxSpeedMps;
        recorder._lastSpeedMps = last.SpeedMps;
        recorder._lastHeadingDeg = last.HeadingDeg;
        recorder._lastAccuracyM = last.AccuracyM;
        recorder._paused = !last.Moving;
        recorder._pausedAt = last.Point;

        return recorder;
    }

    /// <summary>Points recorded at or after an index — what a flush still has to write down.</summary>
    public IReadOnlyList<TrackPoint> PointsFrom(int index)
    {
        lock (_lock)
        {
            return index >= _points.Count ? [] : _points.Skip(index).ToArray();
        }
    }

    /// <summary>The running totals, for the progress row that makes a restore exact.</summary>
    public RecordedTrack Progress()
    {
        lock (_lock)
        {
            return new RecordedTrack(kind, _startedAt, [], _distanceM, _movingSeconds, _elapsedSeconds, _ascentM, _descentM, _maxSpeedMps);
        }
    }

    /// <summary>
    /// Accepts one fix. Safe to call from the location handler of any client session; the recorder
    /// owns all its own state behind a lock and never calls back out.
    /// </summary>
    public void Push(RawFix fix)
    {
        lock (_lock)
        {
            if (fix.AccuracyM > RejectAccuracyM || double.IsNaN(fix.Point.Lat) || double.IsNaN(fix.Point.Lon))
            {
                return;
            }

            if (_points.Count == 0)
            {
                Begin(fix);
                return;
            }

            double dt = (fix.AtUtc - _lastFixAt).TotalSeconds;

            if (dt <= 0)
            {
                // A fix from before the last one we kept — a reordered delivery, or a device clock
                // that stepped. Nothing derived from it would be meaningful.
                return;
            }

            _elapsedSeconds = (fix.AtUtc - _startedAt).TotalSeconds;
            _lastFixAt = fix.AtUtc;

            var previous = _filtered;
            FilterPosition(fix, dt);
            FilterElevation(fix);

            double speed = fix.SpeedMps > 0 ? fix.SpeedMps : Geo.DistanceMeters(previous, _filtered) / dt;
            _lastSpeedMps = speed;
            _lastAccuracyM = fix.AccuracyM;

            if (fix.HeadingDeg >= 0)
            {
                _lastHeadingDeg = fix.HeadingDeg;
            }
            else if (Geo.DistanceMeters(previous, _filtered) > MinHeadingStepM)
            {
                _lastHeadingDeg = Geo.BearingDegrees(previous, _filtered);
            }

            UpdatePauseState(speed, fix.AccuracyM, dt, fix.Point);

            if (!_paused)
            {
                double step = Geo.DistanceMeters(previous, _filtered);

                if (step >= MinStepM)
                {
                    _distanceM += step;
                }

                _movingSeconds += dt;
                _maxSpeedMps = Math.Max(_maxSpeedMps, speed);
                AccumulateElevation();
            }

            _points.Add(new TrackPoint(
                _elapsedSeconds,
                _filtered,
                _filteredElevation,
                speed,
                _lastHeadingDeg,
                fix.AccuracyM,
                _distanceM,
                !_paused));
        }
    }

    public LiveFrame Snapshot(IReadOnlyList<Highlight> liveHighlights, string coachCue, bool simulated, RecordingState state)
    {
        lock (_lock)
        {
            return new LiveFrame(
                state,
                kind,
                _distanceM,
                _movingSeconds,
                _elapsedSeconds,
                _paused ? 0 : _lastSpeedMps,
                _movingSeconds > 0 ? _distanceM / _movingSeconds : 0,
                _maxSpeedMps,
                _ascentM,
                _descentM,
                CurrentGradePct(),
                _lastHeadingDeg,
                _lastAccuracyM,
                _points.Count > 0 ? _filtered : null,
                TrackForMap(),
                liveHighlights,
                coachCue,
                simulated);
        }
    }

    /// <summary>The points as they stand, for the detectors to read mid-activity.</summary>
    public IReadOnlyList<TrackPoint> PointsSnapshot()
    {
        lock (_lock)
        {
            return _points.ToArray();
        }
    }

    public RecordedTrack Finish()
    {
        lock (_lock)
        {
            return new RecordedTrack(
                kind,
                _startedAt,
                _points.ToArray(),
                _distanceM,
                _movingSeconds,
                _elapsedSeconds,
                _ascentM,
                _descentM,
                _maxSpeedMps);
        }
    }

    private void Begin(RawFix fix)
    {
        _startedAt = fix.AtUtc;
        _lastFixAt = fix.AtUtc;
        _filtered = fix.Point;
        _positionVariance = fix.AccuracyM * fix.AccuracyM;
        _filteredElevation = double.IsNaN(fix.AltitudeM) ? 0 : fix.AltitudeM;
        _elevationVariance = Math.Pow(fix.AccuracyM * 2.5, 2);
        _elevationAnchor = _filteredElevation;
        _lastHeadingDeg = fix.HeadingDeg >= 0 ? fix.HeadingDeg : 0;
        _lastAccuracyM = fix.AccuracyM;
        _points.Add(new TrackPoint(0, _filtered, _filteredElevation, fix.SpeedMps, _lastHeadingDeg, fix.AccuracyM, 0, true));
    }

    /// <summary>
    /// A scalar Kalman update per axis, in the one form that matters here: the prediction adds the
    /// uncertainty of however far the rider could have travelled since the last fix, and the
    /// correction weighs the new fix by the accuracy the device claims for it. A vague fix moves the
    /// filtered position very little, which is what keeps the distance total from inflating while
    /// standing at a junction.
    /// </summary>
    private void FilterPosition(RawFix fix, double dt)
    {
        double travel = Math.Max(_lastSpeedMps, 1.0) * dt;
        _positionVariance += travel * travel;

        double accuracy = Math.Max(fix.AccuracyM, 1.0);
        double gain = _positionVariance / (_positionVariance + accuracy * accuracy);

        _filtered = new GeoPoint(
            _filtered.Lat + gain * (fix.Point.Lat - _filtered.Lat),
            _filtered.Lon + gain * (fix.Point.Lon - _filtered.Lon));
        _positionVariance *= 1 - gain;
    }

    private void FilterElevation(RawFix fix)
    {
        if (double.IsNaN(fix.AltitudeM))
        {
            return;
        }

        // Altitude needs far heavier smoothing than position, and for a different reason: the error is
        // small but the *signal* is small too. Ground truth cannot climb faster than the rider's speed
        // times the steepest grade they could be on, so that bounds how much of a jump between fixes is
        // ever real. Integrating unsmoothed altitude deltas is what makes a flat lakeside loop report
        // two hundred metres of climbing.
        double plausibleClimbRate = Math.Max(0.25, Math.Abs(_lastSpeedMps) * 0.15);
        double accuracy = Math.Max(fix.AccuracyM * 0.4, 2.0);
        _elevationVariance += Math.Pow(plausibleClimbRate * 1.0, 2);
        double gain = _elevationVariance / (_elevationVariance + accuracy * accuracy);
        _filteredElevation += gain * (fix.AltitudeM - _filteredElevation);
        _elevationVariance *= 1 - gain;

        if (double.IsNaN(_elevationAnchor))
        {
            _elevationAnchor = _filteredElevation;
        }
    }

    /// <summary>
    /// Gain and loss accumulate only once the filtered elevation has moved clear of the hysteresis
    /// band. Counting every up-tick would turn the filter's residual noise into several hundred metres
    /// of imaginary climbing over an hour.
    /// </summary>
    private void AccumulateElevation()
    {
        // Sampled by distance, not by time. A rider held at a light for two minutes produces a hundred
        // and twenty chances for the barometer to drift across the hysteresis band and none of them
        // are climbing.
        if (_distanceM - _lastElevationSampleM < 25)
        {
            return;
        }

        _lastElevationSampleM = _distanceM;
        double delta = _filteredElevation - _elevationAnchor;

        if (delta > ElevationHysteresisM)
        {
            _ascentM += delta;
            _elevationAnchor = _filteredElevation;
        }
        else if (delta < -ElevationHysteresisM)
        {
            _descentM += -delta;
            _elevationAnchor = _filteredElevation;
        }
    }

    private void UpdatePauseState(double speed, double accuracyM, double dt, GeoPoint rawPoint)
    {
        // Deliberately asymmetric. A vague fix may not *start* a pause — that is how a lost signal
        // turns into a stop that never happened. But it must never be able to *hold* one: being stuck
        // paused costs the rest of the outing, while pausing a few seconds late costs a few seconds.
        if (accuracyM > UntrustedAccuracyM && !_paused)
        {
            return;
        }

        if (!_paused)
        {
            if (speed <= _profile.PauseSpeedMps)
            {
                if (double.IsNaN(_slowSinceSeconds))
                {
                    _slowSinceSeconds = _elapsedSeconds;
                }
                else if (_elapsedSeconds - _slowSinceSeconds >= _profile.PauseDwellSeconds)
                {
                    _paused = true;
                    _pausedAt = _filtered;
                    _slowSinceSeconds = double.NaN;
                }
            }
            else
            {
                _slowSinceSeconds = double.NaN;
            }

            return;
        }

        // Displacement is measured from the RAW fix, not the filtered one. While the filter is being
        // fed speeds near zero its gain collapses, so the filtered position barely follows the device
        // — and a resume test that reads it can end up waiting on a position that is not going to
        // move. The raw fix has no such feedback loop.
        bool movedAway = Geo.DistanceMeters(_pausedAt, rawPoint) > 15;

        if (speed >= _profile.ResumeSpeedMps)
        {
            if (double.IsNaN(_fastSinceSeconds))
            {
                _fastSinceSeconds = _elapsedSeconds;
            }
        }
        else
        {
            _fastSinceSeconds = double.NaN;
        }

        bool heldSpeed = !double.IsNaN(_fastSinceSeconds) && _elapsedSeconds - _fastSinceSeconds >= 3;

        if (heldSpeed || movedAway)
        {
            _paused = false;
            _fastSinceSeconds = double.NaN;
            _movingSeconds += dt;
        }
    }

    private double CurrentGradePct()
    {
        if (_points.Count < 12)
        {
            return 0;
        }

        var back = _points[^12];
        double run = _distanceM - back.DistanceM;
        return run < 15 ? 0 : (_filteredElevation - back.ElevationM) / run * 100;
    }

    /// <summary>
    /// The track thinned for drawing. A two-hour drive is 7,000 points and no map needs them all; the
    /// thinning keeps every point that carries a turn and drops the ones on the straights.
    /// </summary>
    private List<GeoPoint> TrackForMap()
    {
        var result = new List<GeoPoint>();

        if (_points.Count == 0)
        {
            return result;
        }

        result.Add(_points[0].Point);
        var lastKept = _points[0];

        for (int i = 1; i < _points.Count - 1; i++)
        {
            var point = _points[i];
            double sinceKept = Geo.DistanceMeters(lastKept.Point, point.Point);
            double turn = Math.Abs(Geo.BearingDelta(lastKept.HeadingDeg, point.HeadingDeg));

            if (sinceKept > 25 || turn > 12)
            {
                result.Add(point.Point);
                lastKept = point;
            }
        }

        result.Add(_points[^1].Point);
        return result;
    }
}
