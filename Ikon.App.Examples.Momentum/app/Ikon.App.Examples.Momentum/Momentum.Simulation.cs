/// <summary>One fix as it comes off a device, before the recorder has judged it.</summary>
public readonly record struct RawFix(
    DateTime AtUtc,
    GeoPoint Point,
    double AltitudeM,
    double SpeedMps,
    double HeadingDeg,
    double AccuracyM);

/// <summary>What a movement model is told about where it is before it decides how fast to go.</summary>
public readonly record struct MovementContext(
    Route Route,
    double DistanceM,
    double Grade,
    double RadiusM,
    double SpeedLimitKmh,
    double ElapsedSeconds,
    double SpeedMps);

/// <summary>
/// How one kind of traveller moves. Each implementation is a small physical model rather than a
/// speed table, because the detectors read acceleration, corner load and gradient response — a
/// scripted speed curve would test them against numbers we invented rather than numbers movement
/// produces.
/// </summary>
public interface IMovementModel
{
    /// <summary>Ground speed in m/s at the end of a step of <paramref name="dt"/> seconds.</summary>
    double Step(double dt, MovementContext context);

    /// <summary>What the model is doing right now, for the live screen — "trot", "red light", "climbing".</summary>
    string Note { get; }
}

/// <summary>
/// A runner or walker. Pace is grade-adjusted the way running physiology actually responds — uphill
/// costs far more than downhill returns — then drifts with fatigue and breathes with an
/// Ornstein-Uhlenbeck wobble rather than white noise, which would average out and hide the surges
/// the detectors look for.
/// </summary>
public sealed class FootModel(Random random, double basePaceMps, double fatiguePerHour = 0.06) : IMovementModel
{
    private readonly Random _random = random;
    private double _wobble;
    private double _stopUntil = -1;
    private double _nextCrossingCheck = 240;
    private double _effortUntil = -1;
    private double _effortLift = 1;
    private double _nextEffortCheck = 240;

    public string Note { get; private set; } = "steady";

    public double Step(double dt, MovementContext context)
    {
        if (context.ElapsedSeconds < _stopUntil)
        {
            Note = "waiting to cross";
            return 0;
        }

        // A city loop has road crossings; without them the auto-pause state machine is never
        // exercised by a run at all.
        if (context.ElapsedSeconds > _nextCrossingCheck)
        {
            _nextCrossingCheck = context.ElapsedSeconds + 300 + _random.NextDouble() * 240;

            if (_random.NextDouble() < 0.45)
            {
                _stopUntil = context.ElapsedSeconds + 12 + _random.NextDouble() * 25;
                Note = "waiting to cross";
                return 0;
            }
        }

        // Almost nobody runs one pace for an hour. A tempo block or a finish kick is what a surge
        // detector exists to find, and a perfectly even simulated run would never give it one.
        if (context.ElapsedSeconds > _effortUntil)
        {
            _effortLift = 1;

            if (context.ElapsedSeconds > _nextEffortCheck)
            {
                _nextEffortCheck = context.ElapsedSeconds + 240 + _random.NextDouble() * 300;

                if (_random.NextDouble() < 0.55)
                {
                    _effortUntil = context.ElapsedSeconds + 180 + _random.NextDouble() * 240;
                    _effortLift = 1.15 + _random.NextDouble() * 0.13;
                }
            }
        }

        double grade = context.Grade;
        double graded = (grade > 0
            ? basePaceMps / (1 + 5.0 * grade)
            : basePaceMps * Math.Min(1.20, 1 + 1.6 * -grade)) * _effortLift;

        // Past about -12 % the legs start braking instead of free-wheeling.
        if (grade < -0.12)
        {
            graded *= 1 - (-grade - 0.12) * 2.2;
        }

        double fatigue = 1 - fatiguePerHour * (context.ElapsedSeconds / 3600.0);
        _wobble = _wobble * Math.Exp(-dt / 25.0) + NextGaussian() * 0.022 * Math.Sqrt(1 - Math.Exp(-2 * dt / 25.0));

        Note = _effortLift > 1 ? "pushing" : grade > 0.04 ? "climbing" : grade < -0.04 ? "descending" : "steady";
        return Math.Max(0.4, graded * fatigue * (1 + _wobble));
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>
/// A cyclist. Speed is solved from the power the rider is putting out against rolling resistance,
/// aerodynamic drag and gravity, so a climb slows the bike by the amount the hill actually costs and
/// a descent runs away until the corners or the rider's nerve stop it.
/// </summary>
public sealed class BikeModel(Random random, double ftpWatts = 240) : IMovementModel
{
    private const double MassKg = 82;
    private const double Gravity = 9.81;
    private const double Crr = 0.005;
    private const double CdA = 0.32;
    private const double AirDensity = 1.22;

    private readonly Random _random = random;
    private double _effortWobble;

    public string Note { get; private set; } = "rolling";

    public double Step(double dt, MovementContext context)
    {
        double grade = context.Grade;

        // Riders push on climbs and stop pedalling on descents; a flat power number would make the
        // simulated bike faster uphill and slower downhill than any real one.
        double effort = grade > 0.02 ? 1.15 : grade < -0.02 ? 0.20 : 0.82;
        _effortWobble = _effortWobble * Math.Exp(-dt / 20.0) + NextGaussian() * 0.05 * Math.Sqrt(1 - Math.Exp(-2 * dt / 20.0));
        double watts = ftpWatts * effort * (1 + _effortWobble);

        double target = SolveSpeed(watts, grade);

        // Corner speed ceiling: a bike leans to about 0.5 g before the rider stops enjoying it.
        if (!double.IsInfinity(context.RadiusM))
        {
            target = Math.Min(target, Math.Sqrt(0.5 * Gravity * context.RadiusM));
        }

        // A route that states what the surface allows caps the bike too. Without it a gravel track
        // through the woods gets ridden like a descent on tarmac.
        if (context.SpeedLimitKmh > 0)
        {
            target = Math.Min(target, context.SpeedLimitKmh / 3.6);
        }

        // Nerve, not physics: nobody descends a narrow road at the eighty km/h the gradient would
        // otherwise give them.
        target = Math.Min(target, 17);

        // Mass keeps the bike from snapping to the new target; the transition is what a descent's
        // acceleration trace looks like.
        double rate = target > context.SpeedMps ? 0.9 : 2.2;
        double next = context.SpeedMps + Math.Clamp(target - context.SpeedMps, -rate * dt, rate * dt);

        Note = grade > 0.03 ? "climbing" : grade < -0.03 ? "descending" : "rolling";
        return Math.Max(0.8, next);
    }

    /// <summary>
    /// The speed at which the rider's power balances what the road is taking:
    /// P(v) = v·(Crr·m·g·cosθ + m·g·sinθ) + ½·ρ·CdA·v³.
    ///
    /// Bisection rather than Newton, and the reason is the descents. Pointed downhill the gravity term
    /// goes negative, P(v) falls before it rises, and a Newton step from a flat-road seed lands on the
    /// far side of zero every time — which reads as a stalled rider and turns every descent in the app
    /// into a crawl. P is negative below the turning point and grows without bound above it, so a
    /// bracketed search finds the one root that means anything on any gradient.
    /// </summary>
    private static double SolveSpeed(double watts, double grade)
    {
        double theta = Math.Atan(grade);
        double resistive = Crr * MassKg * Gravity * Math.Cos(theta) + MassKg * Gravity * Math.Sin(theta);

        double PowerAt(double v) => v * resistive + 0.5 * AirDensity * CdA * v * v * v;

        double low = 0.05;
        double high = 30.0;

        if (PowerAt(high) < watts)
        {
            return high;
        }

        for (int i = 0; i < 40; i++)
        {
            double mid = (low + high) / 2;

            if (PowerAt(mid) < watts)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return Math.Clamp((low + high) / 2, 0.9, 25);
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>
/// A horse and rider. Speed comes from the gait, not the other way round: the rider picks walk, trot,
/// canter or gallop, the horse settles into that gait's band, and transitions take a couple of
/// seconds. That is exactly the structure the gait detector has to recover from the speed trace.
/// </summary>
public sealed class HorseModel(Random random) : IMovementModel
{
    private static readonly (string Name, double Mps, double MaxSeconds)[] Gaits =
    [
        ("halt", 0.0, 40),
        ("walk", 1.5, 900),
        ("trot", 3.7, 600),
        ("canter", 6.4, 240),
        ("gallop", 9.6, 45),
    ];

    private readonly Random _random = random;
    private int _gait = 1;
    private double _inGaitSeconds;
    private double _wobble;

    public string Note => Gaits[_gait].Name;

    public double Step(double dt, MovementContext context)
    {
        _inGaitSeconds += dt;

        bool tooLong = _inGaitSeconds > Gaits[_gait].MaxSeconds;
        bool steep = context.Grade > 0.07;
        bool twisty = context.RadiusM < 25;

        // The rider reconsiders every few seconds, and immediately when the ground argues for it.
        if (tooLong || steep || twisty || _random.NextDouble() < dt / 55.0)
        {
            int wanted = steep || twisty ? Math.Min(_gait, 2) : PickGait(context);

            if (wanted != _gait)
            {
                _gait = wanted;
                _inGaitSeconds = 0;
            }
        }

        _wobble = _wobble * Math.Exp(-dt / 12.0) + NextGaussian() * 0.05 * Math.Sqrt(1 - Math.Exp(-2 * dt / 12.0));
        double target = Gaits[_gait].Mps * (1 + _wobble) / (1 + 2.5 * Math.Max(0, context.Grade));

        // Gait changes are a second or two of transition, never a step; the detector's hysteresis is
        // only meaningful against a trace that actually ramps.
        double rate = target > context.SpeedMps ? 1.6 : 2.4;
        return Math.Max(0, context.SpeedMps + Math.Clamp(target - context.SpeedMps, -rate * dt, rate * dt));
    }

    private int PickGait(MovementContext context)
    {
        double roll = _random.NextDouble();
        double progress = context.Route.TotalMeters > 0 ? context.DistanceM / context.Route.TotalMeters : 0;

        // Horses are warmed up before they are asked for a canter, and are walked home at the end.
        if (progress < 0.12 || progress > 0.9)
        {
            return roll < 0.75 ? 1 : 2;
        }

        return roll switch
        {
            < 0.08 => 0,
            < 0.38 => 1,
            < 0.78 => 2,
            < 0.96 => 3,
            _ => 4,
        };
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>
/// A car. Longitudinal acceleration is power- and traction-limited, cornering is limited by lateral
/// grip, and the route's traffic lights are obeyed — the stop, the wait and the pull away from it are
/// what the traffic-light-launch detector reads back out.
/// </summary>
public sealed class CarModel(Random random, Route route, double aggression = 0.55) : IMovementModel
{
    private const double MassKg = 1550;
    private const double Gravity = 9.81;
    private const double MaxLateralG = 0.42;
    private const double PowerWatts = 110_000;

    private readonly Random _random = random;
    private readonly Dictionary<double, bool> _lightIsRed = [];
    private readonly HashSet<double> _cleared = [];
    private double _stopUntil = -1;
    private double _stoppedAt = -1;

    public string Note { get; private set; } = "cruising";

    public double Step(double dt, MovementContext context)
    {
        if (context.ElapsedSeconds < _stopUntil)
        {
            Note = "red light";
            return 0;
        }

        if (_stoppedAt >= 0)
        {
            // The wait is over. Without retiring the light here the car brakes for it again on the
            // very next step and never leaves the junction.
            _cleared.Add(_stoppedAt);
            _stoppedAt = -1;
        }

        double target = context.SpeedLimitKmh > 0 ? context.SpeedLimitKmh / 3.6 * (1 + aggression * 0.10) : 14;

        if (!double.IsInfinity(context.RadiusM))
        {
            target = Math.Min(target, Math.Sqrt(MaxLateralG * Gravity * context.RadiusM));
        }

        double? lightAt = NextRedLightAhead(context);

        if (lightAt is { } stopAt)
        {
            double toGo = stopAt - context.DistanceM;

            if (toGo <= 3)
            {
                _stopUntil = context.ElapsedSeconds + 8 + _random.NextDouble() * 40;
                _stoppedAt = stopAt;
                Note = "red light";
                return 0;
            }

            // The speed that still allows a comfortable 2.5 m/s² stop at the line.
            target = Math.Min(target, Math.Sqrt(2 * 2.5 * Math.Max(0, toGo - 3)));
        }

        double speed = context.SpeedMps;
        double accel;

        if (target > speed)
        {
            // Traction off the line, then power-limited as speed builds — which is why 0–50 is quick
            // and 50–100 is not.
            double tractionLimit = 2.6 + aggression * 1.6;
            double powerLimit = speed > 1 ? PowerWatts / (MassKg * speed) : tractionLimit;
            accel = Math.Min(tractionLimit, powerLimit);
            Note = speed < 3 ? "away from the line" : "cruising";
        }
        else
        {
            accel = -Math.Min(4.5, (speed - target) / Math.Max(dt, 0.5));
            Note = "braking";
        }

        double next = speed + accel * dt;
        return Math.Max(0, accel > 0 ? Math.Min(target, next) : next);
    }

    private double? NextRedLightAhead(MovementContext context)
    {
        foreach (double at in route.TrafficLightsAtM)
        {
            if (at <= context.DistanceM || at - context.DistanceM > 220 || _cleared.Contains(at))
            {
                continue;
            }

            // Each light's colour is decided once and then remembered, so braking for it does not
            // flicker between steps.
            if (!_lightIsRed.TryGetValue(at, out bool red))
            {
                red = _random.NextDouble() < 0.62;
                _lightIsRed[at] = red;
            }

            return red ? at : null;
        }

        return null;
    }
}

/// <summary>
/// The layer that turns a perfect simulated position into something a phone would have reported.
/// Detectors validated only against clean synthetic tracks are not validated: the horizontal error
/// wanders rather than flickering, altitude is several times worse than position, accuracy tracks the
/// surroundings, and the stream drops out now and then.
/// </summary>
public sealed class GpsNoise(Random random, double sigmaMeters = 4.0)
{
    private readonly Random _random = random;
    private double _errorNorth;
    private double _errorEast;
    private double _altitudeDrift;
    private double _dropoutUntil = -1;
    private double _nextDropoutCheck = 60;

    /// <summary>Null when the receiver has lost the sky — a tunnel, an underpass, a dense stand of spruce.</summary>
    public RawFix? Apply(DateTime atUtc, GeoPoint truth, double trueAltitude, double trueSpeed, double trueHeading, double elapsedSeconds, double dt)
    {
        if (elapsedSeconds < _dropoutUntil)
        {
            return null;
        }

        if (elapsedSeconds > _nextDropoutCheck)
        {
            _nextDropoutCheck = elapsedSeconds + 200 + _random.NextDouble() * 400;

            if (_random.NextDouble() < 0.3)
            {
                _dropoutUntil = elapsedSeconds + 4 + _random.NextDouble() * 14;
                return null;
            }
        }

        // Correlated error with a ~30 s memory. White noise would average away over any window a
        // detector looks at, which is precisely what real multipath error does not do.
        const double tau = 30.0;
        double decay = Math.Exp(-dt / tau);
        double kick = Math.Sqrt(1 - decay * decay);
        _errorNorth = _errorNorth * decay + NextGaussian() * sigmaMeters * kick;
        _errorEast = _errorEast * decay + NextGaussian() * sigmaMeters * kick;

        // Altitude on a modern phone is barometric pressure fused with GNSS, and that behaves quite
        // differently from the horizontal fix: metres out in absolute terms and drifting slowly with
        // the weather, but steady from one second to the next. That split is what makes ascent
        // computable at all — it is the *changes* a climb is made of, not the absolute height.
        const double driftTau = 600.0;
        double driftDecay = Math.Exp(-dt / driftTau);
        _altitudeDrift = _altitudeDrift * driftDecay + NextGaussian() * 8.0 * Math.Sqrt(1 - driftDecay * driftDecay);
        double altitudeNoise = _altitudeDrift + NextGaussian() * 0.6;

        double accuracy = sigmaMeters * 1.6 + Math.Abs(NextGaussian()) * sigmaMeters * 0.5;

        return new RawFix(
            atUtc,
            Geo.Offset(truth, _errorNorth, _errorEast),
            trueAltitude + altitudeNoise,
            // A phone's speed comes from Doppler shift, not from differencing positions, so it is far
            // better than the positions are.
            Math.Max(0, trueSpeed + NextGaussian() * 0.25),
            trueHeading,
            accuracy);
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>How a simulated outing is set up. Seeded throughout, so a given plan always replays identically.</summary>
public sealed record SimulationPlan(
    Route Route,
    ActivityKind Kind,
    int Seed,
    double SpeedMultiplier = 1,
    double MaxSeconds = 7200,
    double DistanceLimitM = double.PositiveInfinity,
    double NoiseSigmaM = 4.0,
    string Preset = "");

/// <summary>
/// Walks a <see cref="SimulationPlan"/> and emits the fixes a phone would have sent. Stepping is
/// fixed at 1 Hz of simulated time — the rate the platform's own location stream reports at — and
/// <see cref="SimulationPlan.SpeedMultiplier"/> only changes how fast that simulated second is
/// delivered, never the physics.
/// </summary>
public sealed class TrackSimulator(SimulationPlan plan)
{
    private const double StepSeconds = 1.0;

    public SimulationPlan Plan { get; } = plan;

    public string Note { get; private set; } = "";

    public double DistanceM { get; private set; }

    public double ElapsedSeconds { get; private set; }

    /// <summary>
    /// Every fix the outing produces, in order. Enumerating is pure computation — the caller decides
    /// whether to pace it against a wall clock or drain it as fast as the CPU allows, which is what
    /// lets the tests run the same outing the UI plays back.
    /// </summary>
    public IEnumerable<RawFix> Fixes(DateTime startUtc)
    {
        var random = new Random(Plan.Seed);
        var noiseRandom = new Random(Plan.Seed * 31 + 7);
        var noise = new GpsNoise(noiseRandom, Plan.NoiseSigmaM);
        var model = CreateModel(random);

        double speed = 0;
        double distance = 0;
        double elapsed = 0;
        double heading = 0;
        double limit = Math.Min(Plan.DistanceLimitM, Plan.Route.TotalMeters);

        while (elapsed < Plan.MaxSeconds && distance < limit)
        {
            var context = new MovementContext(
                Plan.Route,
                distance,
                Plan.Route.GradeAt(distance),
                // The tighter of a near and a far measure. A corner is limited by its sharpest part,
                // and a single wide window smooths a sudden bend away entirely.
                Math.Min(Plan.Route.RadiusAt(distance, 25), Plan.Route.RadiusAt(distance, 60)),
                Plan.Route.At(distance).SpeedLimitKmh,
                elapsed,
                speed);

            speed = model.Step(StepSeconds, context);
            distance += speed * StepSeconds;
            elapsed += StepSeconds;

            var sample = Plan.Route.At(Math.Min(distance, Plan.Route.TotalMeters));
            var ahead = Plan.Route.At(Math.Min(distance + 12, Plan.Route.TotalMeters));

            // At the far end of the route the look-ahead clamps onto the point we are already at, and a
            // bearing between a point and itself is zero — a spurious hard left at the finish that the
            // corner detector reads as a corner nobody took. Keep the last real heading instead.
            if (Geo.DistanceMeters(sample.Point, ahead.Point) > 1)
            {
                heading = Geo.BearingDegrees(sample.Point, ahead.Point);
            }

            Note = model.Note;
            DistanceM = distance;
            ElapsedSeconds = elapsed;

            var fix = noise.Apply(startUtc.AddSeconds(elapsed), sample.Point, sample.ElevationM, speed, heading, elapsed, StepSeconds);

            if (fix is { } emitted)
            {
                yield return emitted;
            }
        }
    }

    private IMovementModel CreateModel(Random random) => Plan.Kind switch
    {
        ActivityKind.Foot => new FootModel(random, Plan.Preset == "walk" ? 1.45 : 3.15),
        ActivityKind.Bike => new BikeModel(random),
        ActivityKind.Horse => new HorseModel(random),
        _ => new CarModel(random, Plan.Route, Plan.Preset == "spirited" ? 0.85 : 0.5),
    };
}
