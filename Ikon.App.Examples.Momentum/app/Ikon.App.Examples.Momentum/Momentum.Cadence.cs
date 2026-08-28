/// <summary>
/// Turns a stream of accelerometer samples into a rhythm — steps, strides or hoofbeats per minute.
///
/// This is the thing GPS cannot see. A speed trace cannot separate a collected canter from a fast
/// trot, or a shuffling jog from an easy run, because both pairs cover ground at the same rate. The
/// difference is in the beat, and the beat is in the accelerometer.
/// </summary>
public sealed class CadenceTracker
{
    /// <summary>
    /// Nothing living moves faster than this, so a peak sooner than the last one is the same footfall
    /// seen twice through the noise rather than a new one.
    /// </summary>
    private const double MinBeatIntervalMs = 180;

    /// <summary>How far back a cadence is averaged. Short enough to follow a change of gait.</summary>
    private const double WindowMs = 8_000;

    private readonly object _lock = new();
    private readonly Queue<double> _beats = new();

    // A slow-moving mean, used as the line that peaks rise above. Gravity is already out of user
    // acceleration, but a phone in a pocket still has an offset that drifts with how it is sitting.
    private double _baseline;
    private double _envelope = 1.0;
    private double _lastBeatAtMs;
    private double _previous;
    private bool _rising;

    /// <summary>Beats per minute over the recent window, or 0 when there is not enough rhythm to call.</summary>
    public double PerMinute
    {
        get
        {
            lock (_lock)
            {
                return Compute();
            }
        }
    }

    /// <summary>Feeds one batch. Samples must be in the order the device produced them.</summary>
    public void Push(IReadOnlyList<MotionSample> samples)
    {
        lock (_lock)
        {
            foreach (var sample in samples)
            {
                if (sample.Sensor != MotionSensors.UserAcceleration)
                {
                    continue;
                }

                Step(sample.Magnitude, sample.AtMillis);
            }

            Trim(_lastBeatAtMs);
        }
    }

    private void Step(double magnitude, double atMs)
    {
        // Exponential baseline and envelope: a threshold fixed in m/s² would find a runner's footfalls
        // and miss a walker's entirely, since one is an order of magnitude softer than the other.
        _baseline += (magnitude - _baseline) * 0.02;
        double deviation = Math.Abs(magnitude - _baseline);
        _envelope += (deviation - _envelope) * 0.01;

        double threshold = _baseline + Math.Max(0.35, _envelope * 1.2);

        if (magnitude > threshold && magnitude > _previous)
        {
            _rising = true;
        }
        else if (_rising && magnitude < _previous)
        {
            // The sample after the crest is where the footfall actually was.
            _rising = false;

            if (atMs - _lastBeatAtMs >= MinBeatIntervalMs)
            {
                _lastBeatAtMs = atMs;
                _beats.Enqueue(atMs);
            }
        }

        _previous = magnitude;
    }

    private void Trim(double nowMs)
    {
        while (_beats.Count > 0 && nowMs - _beats.Peek() > WindowMs)
        {
            _beats.Dequeue();
        }
    }

    private double Compute()
    {
        if (_beats.Count < 4)
        {
            return 0;
        }

        double span = _beats.Last() - _beats.Peek();

        // A handful of beats spread over a long window is noise that happened to line up, not a rhythm.
        return span < 1500 ? 0 : (_beats.Count - 1) / (span / 1000.0) * 60.0;
    }
}

public partial class MomentumApp
{
    private readonly CadenceTracker _cadence = new();

    private void OnMotionBatch(MotionBatch batch)
    {
        lock (_sessionLock)
        {
            // Only the device that owns the outing gets a say, for the same reason its fixes do: two
            // phones would interleave two rhythms into one meaningless average.
            if (_state == RecordingState.Idle || (_recordingSessionId != 0 && _recordingSessionId != batch.SessionId))
            {
                return;
            }
        }

        _cadence.Push(batch.Samples);
    }
}
