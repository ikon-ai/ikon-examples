public readonly record struct GeoPoint(double Lat, double Lon);

/// <summary>One resampled point on a planned route: where it is, how high, and how far along.</summary>
public readonly record struct RouteSample(GeoPoint Point, double ElevationM, double DistanceM, double SpeedLimitKmh);

public static class Geo
{
    private const double EarthRadiusM = 6_371_000;

    public static double DistanceMeters(GeoPoint a, GeoPoint b)
    {
        double dLat = ToRad(b.Lat - a.Lat);
        double dLon = ToRad(b.Lon - a.Lon);
        double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                   + Math.Cos(ToRad(a.Lat)) * Math.Cos(ToRad(b.Lat)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadiusM * Math.Asin(Math.Sqrt(h));
    }

    public static double BearingDegrees(GeoPoint from, GeoPoint to)
    {
        double lat1 = ToRad(from.Lat);
        double lat2 = ToRad(to.Lat);
        double dLon = ToRad(to.Lon - from.Lon);
        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Cos(lat2) * Math.Sin(lat1) * Math.Cos(dLon);
        return (ToDeg(Math.Atan2(y, x)) + 360) % 360;
    }

    /// <summary>Signed turn from one bearing to another, in (-180, 180]. Negative is a left turn.</summary>
    public static double BearingDelta(double fromDeg, double toDeg)
    {
        double delta = (toDeg - fromDeg + 540) % 360 - 180;
        return delta;
    }

    public static GeoPoint Offset(GeoPoint origin, double northMeters, double eastMeters)
    {
        double lat = origin.Lat + ToDeg(northMeters / EarthRadiusM);
        double lon = origin.Lon + ToDeg(eastMeters / (EarthRadiusM * Math.Cos(ToRad(origin.Lat))));
        return new GeoPoint(lat, lon);
    }

    public static GeoPoint Lerp(GeoPoint a, GeoPoint b, double t) =>
        new(a.Lat + (b.Lat - a.Lat) * t, a.Lon + (b.Lon - a.Lon) * t);

    /// <summary>
    /// Radius of the circle through three consecutive points, in metres. Returns
    /// <see cref="double.PositiveInfinity"/> for a straight line, which is what the corner detectors
    /// want — an infinite radius carries no lateral load.
    /// </summary>
    public static double TurnRadiusMeters(GeoPoint a, GeoPoint b, GeoPoint c)
    {
        // Work in a local metre plane centred on b; over the few tens of metres between fixes the
        // curvature of the earth is far below the noise floor of the fixes themselves.
        double latScale = 111_320.0;
        double lonScale = latScale * Math.Cos(ToRad(b.Lat));
        double ax = (a.Lon - b.Lon) * lonScale, ay = (a.Lat - b.Lat) * latScale;
        double cx = (c.Lon - b.Lon) * lonScale, cy = (c.Lat - b.Lat) * latScale;

        double sideA = Math.Sqrt(ax * ax + ay * ay);
        double sideC = Math.Sqrt(cx * cx + cy * cy);
        double sideB = Math.Sqrt((cx - ax) * (cx - ax) + (cy - ay) * (cy - ay));
        double cross = Math.Abs(ax * cy - ay * cx);

        if (cross < 1e-6 || sideA < 1e-6 || sideC < 1e-6)
        {
            return double.PositiveInfinity;
        }

        return sideA * sideB * sideC / (2 * cross);
    }

    public static double ToRad(double deg) => deg * Math.PI / 180;

    public static double ToDeg(double rad) => rad * 180 / Math.PI;
}

/// <summary>
/// A planned route: control points smoothed with a Catmull-Rom spline and resampled at a fixed
/// spacing, so the simulator can ask "where am I at 4,312 m along" without carrying the spline math.
/// </summary>
public sealed class Route
{
    public string Id { get; }

    public string Name { get; }

    public string Where { get; }

    public ActivityKind Kind { get; }

    public IReadOnlyList<RouteSample> Samples { get; }

    /// <summary>Distances along the route at which the simulator should find a red light.</summary>
    public IReadOnlyList<double> TrafficLightsAtM { get; }

    public double TotalMeters => Samples[^1].DistanceM;

    public GeoPoint Start => Samples[0].Point;

    public Route(string id, string name, string where, ActivityKind kind,
        (double Lat, double Lon, double Elev, double LimitKmh)[] controlPoints,
        double spacingM = 10,
        IReadOnlyList<double>? trafficLightsAtM = null)
    {
        Id = id;
        Name = name;
        Where = where;
        Kind = kind;
        Samples = Resample(controlPoints, spacingM);
        TrafficLightsAtM = trafficLightsAtM ?? [];
    }

    /// <summary>The route sample at a distance along the route, interpolated between resample steps.</summary>
    public RouteSample At(double distanceM)
    {
        if (distanceM <= 0)
        {
            return Samples[0];
        }

        if (distanceM >= TotalMeters)
        {
            return Samples[^1];
        }

        // Uniform spacing makes the index arithmetic rather than a search.
        double spacing = Samples[1].DistanceM;
        int index = Math.Min(Samples.Count - 2, (int)(distanceM / spacing));
        double t = (distanceM - Samples[index].DistanceM) / (Samples[index + 1].DistanceM - Samples[index].DistanceM);
        var a = Samples[index];
        var b = Samples[index + 1];

        return new RouteSample(
            Geo.Lerp(a.Point, b.Point, t),
            a.ElevationM + (b.ElevationM - a.ElevationM) * t,
            distanceM,
            a.SpeedLimitKmh);
    }

    /// <summary>Grade over a window centred on a distance, as a fraction (0.05 is a 5 % climb).</summary>
    public double GradeAt(double distanceM, double windowM = 40)
    {
        var back = At(Math.Max(0, distanceM - windowM / 2));
        var forward = At(Math.Min(TotalMeters, distanceM + windowM / 2));
        double run = forward.DistanceM - back.DistanceM;
        return run < 1 ? 0 : (forward.ElevationM - back.ElevationM) / run;
    }

    /// <summary>
    /// Turn radius of the route itself at a distance, in metres. The window is the distance a rider
    /// actually negotiates a corner over: measured across a short chord, a spline through a switchback
    /// reports a cusp of a metre or two and every model built on it crawls round the hairpin.
    /// </summary>
    public double RadiusAt(double distanceM, double windowM = 50)
    {
        var a = At(Math.Max(0, distanceM - windowM));
        var b = At(distanceM);
        var c = At(Math.Min(TotalMeters, distanceM + windowM));
        return Geo.TurnRadiusMeters(a.Point, b.Point, c.Point);
    }

    private static List<RouteSample> Resample((double Lat, double Lon, double Elev, double LimitKmh)[] control, double spacingM)
    {
        var dense = new List<(GeoPoint Point, double Elev, double Limit)>();

        for (int i = 0; i < control.Length - 1; i++)
        {
            var p0 = control[Math.Max(0, i - 1)];
            var p1 = control[i];
            var p2 = control[i + 1];
            var p3 = control[Math.Min(control.Length - 1, i + 2)];

            // 40 sub-steps per control span is finer than the resample spacing at every span length
            // these routes use, so the arc-length walk below never has to interpolate a long chord.
            for (int step = 0; step < 40; step++)
            {
                double t = step / 40.0;
                dense.Add((
                    new GeoPoint(CatmullRom(p0.Lat, p1.Lat, p2.Lat, p3.Lat, t), CatmullRom(p0.Lon, p1.Lon, p2.Lon, p3.Lon, t)),
                    CatmullRom(p0.Elev, p1.Elev, p2.Elev, p3.Elev, t),
                    p1.LimitKmh));
            }
        }

        dense.Add((new GeoPoint(control[^1].Lat, control[^1].Lon), control[^1].Elev, control[^1].LimitKmh));

        var samples = new List<RouteSample> { new(dense[0].Point, dense[0].Elev, 0, dense[0].Limit) };
        double carried = 0;

        for (int i = 1; i < dense.Count; i++)
        {
            double segment = Geo.DistanceMeters(dense[i - 1].Point, dense[i].Point);

            if (segment < 1e-9)
            {
                continue;
            }

            double walked = spacingM - carried;

            while (walked <= segment)
            {
                double t = walked / segment;
                samples.Add(new RouteSample(
                    Geo.Lerp(dense[i - 1].Point, dense[i].Point, t),
                    dense[i - 1].Elev + (dense[i].Elev - dense[i - 1].Elev) * t,
                    samples.Count * spacingM,
                    dense[i].Limit));
                walked += spacingM;
            }

            carried = segment - (walked - spacingM);
        }

        return samples;
    }

    private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
    {
        double t2 = t * t;
        double t3 = t2 * t;
        return 0.5 * ((2 * p1)
                      + (-p0 + p2) * t
                      + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                      + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }
}
