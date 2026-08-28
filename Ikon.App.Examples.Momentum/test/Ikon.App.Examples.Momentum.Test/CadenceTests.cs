// Cadence has to come out of a signal that looks like a phone in a pocket, not a clean sine — the
// point of reading the accelerometer at all is to see rhythm that GPS cannot.

public class CadenceTests
{
    /// <summary>A footfall trace: a sharp impulse per step, riding on drift, with noise on top.</summary>
    private static List<MotionSample> Strides(double stepsPerMinute, int seconds, double amplitude, int seed = 7)
    {
        var random = new Random(seed);
        var samples = new List<MotionSample>();
        double stepMs = 60_000.0 / stepsPerMinute;

        for (int i = 0; i < seconds * 50; i++)
        {
            double atMs = i * 20.0;
            double phase = (atMs % stepMs) / stepMs;

            // A footfall is a spike, not a wave: sharp on contact, decaying through the stride.
            double impulse = amplitude * Math.Exp(-phase * 9);
            double drift = 0.4 * Math.Sin(atMs / 4000.0);
            double noise = (random.NextDouble() - 0.5) * amplitude * 0.25;

            samples.Add(new MotionSample(atMs, impulse + drift + noise, 0, 0, MotionSensors.UserAcceleration));
        }

        return samples;
    }

    [Theory]
    [InlineData(160.0, 6.0)]   // running
    [InlineData(110.0, 1.6)]   // walking, a far softer footfall
    [InlineData(78.0, 3.0)]    // a horse at a trot
    public void ARhythmIsRecoveredFromTheAccelerometer(double stepsPerMinute, double amplitude)
    {
        var tracker = new CadenceTracker();
        tracker.Push(Strides(stepsPerMinute, seconds: 20, amplitude: amplitude));

        double measured = tracker.PerMinute;

        Assert.True(Math.Abs(measured - stepsPerMinute) / stepsPerMinute < 0.12,
            $"Read {measured:0} per minute from a {stepsPerMinute:0} rhythm");
    }

    [Fact]
    public void StillnessHasNoCadence()
    {
        var tracker = new CadenceTracker();
        var random = new Random(11);
        var samples = new List<MotionSample>();

        for (int i = 0; i < 1000; i++)
        {
            samples.Add(new MotionSample(i * 20.0, (random.NextDouble() - 0.5) * 0.1, 0, 0, MotionSensors.UserAcceleration));
        }

        tracker.Push(samples);

        Assert.Equal(0, tracker.PerMinute);
    }

    [Fact]
    public void OtherSensorsAreIgnored()
    {
        // A gyroscope stream would otherwise be counted as footfalls alongside the accelerometer.
        var tracker = new CadenceTracker();
        var gyro = Strides(160, 20, 6.0).Select(s => s with { Sensor = MotionSensors.Gyroscope }).ToList();
        tracker.Push(gyro);

        Assert.Equal(0, tracker.PerMinute);
    }
}
