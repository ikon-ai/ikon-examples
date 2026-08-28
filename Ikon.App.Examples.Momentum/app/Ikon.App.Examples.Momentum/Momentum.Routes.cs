/// <summary>
/// The routes the simulator moves along. The control points follow real geography — a Helsinki bay,
/// the road west out of the city, vineyard switchbacks above Lake Geneva — so the tracks sit where
/// they should when the map draws them, and the corner radii and gradients the detectors read are the
/// ones the ground would actually have produced.
/// </summary>
public static class Routes
{
    // Helsinki, around Töölönlahti bay: station forecourt, up the west shore past Finlandia, around
    // the north end at Linnunlaulu and back down the east side. Flat, with one short rise.
    private static readonly (double, double, double, double)[] ToolonlahtiLoop =
    [
        (60.1712, 24.9414, 6, 0),
        (60.1731, 24.9377, 6, 0),
        (60.1745, 24.9345, 7, 0),
        (60.1762, 24.9330, 8, 0),
        (60.1789, 24.9316, 9, 0),
        (60.1817, 24.9299, 11, 0),
        (60.1846, 24.9285, 14, 0),
        (60.1871, 24.9308, 18, 0),
        (60.1884, 24.9351, 16, 0),
        (60.1869, 24.9392, 11, 0),
        (60.1841, 24.9407, 8, 0),
        (60.1808, 24.9418, 7, 0),
        (60.1776, 24.9424, 6, 0),
        (60.1746, 24.9428, 6, 0),
        (60.1712, 24.9414, 6, 0),
    ];

    // Espoo: out of Nuuksio's Haukkalampi car park, over the ridges to Kattila and back. Finland's
    // hills are short and repeated rather than long — several 40–60 m ramps, no single big climb.
    private static readonly (double, double, double, double)[] NuuksioRidges =
    [
        (60.3105, 24.5140, 58, 26),
        (60.3142, 24.5063, 74, 26),
        (60.3178, 24.4981, 92, 24),
        (60.3211, 24.4902, 81, 24),
        (60.3246, 24.4838, 69, 26),
        (60.3290, 24.4771, 88, 24),
        (60.3334, 24.4712, 112, 22),
        (60.3372, 24.4640, 124, 22),
        (60.3401, 24.4562, 108, 24),
        (60.3428, 24.4661, 86, 26),
        (60.3392, 24.4757, 71, 26),
        (60.3341, 24.4849, 83, 24),
        (60.3283, 24.4936, 96, 24),
        (60.3221, 24.5019, 78, 26),
        (60.3160, 24.5091, 64, 28),
        (60.3105, 24.5140, 58, 28),
    ];

    // Lavaux, above Lake Geneva: out of Rivaz on the lake shore, up the switchbacks through the
    // vineyards to the Chexbres plateau, and back down the eastern road. Three hundred metres of
    // climbing at a steady eight per cent — the one route in the set with a climb Finland cannot
    // offer, and the only one that exercises the climb categoriser above its lowest rung.
    private static readonly (double, double, double, double)[] LavauxWall =
    [
        (46.4803, 6.7756, 378, 0),
        (46.4810, 6.7810, 413, 0),
        (46.4824, 6.7760, 448, 0),
        (46.4838, 6.7812, 483, 0),
        (46.4852, 6.7762, 518, 0),
        (46.4866, 6.7814, 553, 0),
        (46.4880, 6.7764, 588, 0),
        (46.4894, 6.7816, 620, 0),
        (46.4908, 6.7768, 648, 0),
        (46.4924, 6.7800, 665, 0),
        (46.4948, 6.7818, 674, 0),
        (46.4976, 6.7830, 680, 0),
        (46.4970, 6.7868, 655, 0),
        (46.4944, 6.7886, 625, 0),
        (46.4916, 6.7898, 595, 0),
        (46.4888, 6.7906, 565, 0),
        (46.4860, 6.7910, 535, 0),
        (46.4832, 6.7900, 505, 0),
        (46.4806, 6.7882, 470, 0),
        (46.4788, 6.7846, 440, 0),
        (46.4780, 6.7808, 410, 0),
        (46.4788, 6.7772, 385, 0),
        (46.4803, 6.7756, 378, 0),
    ];

    // A bridle path through the forest north of Vihti: winding, slow, and climbing gently. The
    // sinuosity here is what the trail detector looks for.
    private static readonly (double, double, double, double)[] VihtiBridlePath =
    [
        (60.4188, 24.3241, 74, 0),
        (60.4209, 24.3288, 78, 0),
        (60.4218, 24.3352, 83, 0),
        (60.4205, 24.3414, 89, 0),
        (60.4224, 24.3468, 96, 0),
        (60.4257, 24.3491, 102, 0),
        (60.4284, 24.3452, 108, 0),
        (60.4301, 24.3389, 113, 0),
        (60.4331, 24.3358, 106, 0),
        (60.4358, 24.3402, 99, 0),
        (60.4351, 24.3474, 92, 0),
        (60.4318, 24.3521, 87, 0),
        (60.4279, 24.3548, 84, 0),
        (60.4241, 24.3512, 80, 0),
        (60.4212, 24.3441, 77, 0),
        (60.4193, 24.3358, 75, 0),
        (60.4188, 24.3241, 74, 0),
    ];

    // Helsinki to Porkkala: Lauttasaari's 50-limit streets and their lights, then Länsiväylä's long
    // 80 and 100 straights, then the coastal road's sweepers down to the point. Everything the car
    // detectors look for is somewhere on this drive.
    private static readonly (double, double, double, double)[] PorkkalaRun =
    [
        (60.1608, 24.9218, 8, 50),
        (60.1592, 24.9105, 7, 50),
        (60.1588, 24.8994, 9, 50),
        (60.1601, 24.8879, 11, 50),
        (60.1614, 24.8744, 10, 60),
        (60.1607, 24.8592, 12, 80),
        (60.1583, 24.8421, 14, 80),
        (60.1561, 24.8243, 16, 100),
        (60.1544, 24.8038, 19, 100),
        (60.1531, 24.7817, 22, 100),
        (60.1518, 24.7594, 24, 100),
        (60.1497, 24.7368, 27, 100),
        (60.1462, 24.7154, 31, 80),
        (60.1408, 24.6981, 28, 80),
        (60.1341, 24.6852, 24, 60),
        (60.1268, 24.6773, 19, 60),
        (60.1189, 24.6718, 15, 80),
        (60.1102, 24.6660, 12, 80),
        (60.1014, 24.6597, 14, 60),
        (60.0931, 24.6512, 11, 60),
        (60.0862, 24.6408, 9, 50),
        (60.0812, 24.6294, 7, 50),
    ];

    /// <summary>
    /// A rural road that bends. Unlike the others this one is constructed rather than traced: it is a
    /// sine laid along a heading, with a second slower sine for the hills. Everything else in the set
    /// follows real ground, and the fast road out to Porkkala is genuinely arrow-straight for most of
    /// its length — which is exactly what the clean-straight detector is for, and exactly why nothing
    /// there gives the corner detector a corner. This road is here to be that corner.
    /// </summary>
    private static (double Lat, double Lon, double Elev, double LimitKmh)[] WindingRoad(
        double startLat, double startLon, double headingDeg, double lengthM,
        double wavelengthM, double amplitudeM, double limitKmh)
    {
        double metresPerLat = 111_320.0;
        double metresPerLon = metresPerLat * Math.Cos(Geo.ToRad(startLat));
        double heading = Geo.ToRad(headingDeg);
        (double North, double East) along = (Math.Cos(heading), Math.Sin(heading));
        (double North, double East) across = (-along.East, along.North);

        // An eighth of a wavelength per control point: the spline has to be told where the bend turns,
        // and sampled any coarser Catmull-Rom rounds the corners straight out of the road.
        double step = wavelengthM / 8;
        int count = (int)(lengthM / step) + 1;
        var points = new (double, double, double, double)[count];

        for (int i = 0; i < count; i++)
        {
            double s = step * i;
            double offset = amplitudeM * Math.Sin(2 * Math.PI * s / wavelengthM);
            double north = along.North * s + across.North * offset;
            double east = along.East * s + across.East * offset;
            double elevation = 45 + 22 * Math.Sin(2 * Math.PI * s / 2400) + 9 * Math.Sin(2 * Math.PI * s / 850);

            points[i] = (
                startLat + north / metresPerLat,
                startLon + east / metresPerLon,
                Math.Round(elevation, 1),
                limitKmh);
        }

        return points;
    }

    public static readonly IReadOnlyList<Route> All =
    [
        new("toolonlahti", "Töölönlahti loop", "Helsinki", ActivityKind.Foot, ToolonlahtiLoop),
        new("nuuksio", "Nuuksio ridges", "Espoo", ActivityKind.Bike, NuuksioRidges),
        new("lavaux", "Up to Chexbres", "Lavaux", ActivityKind.Bike, LavauxWall),
        new("vihti", "Vihti bridle path", "Vihti", ActivityKind.Horse, VihtiBridlePath),
        // The lights sit where Lauttasaari's junctions actually fall, and one more where the coast
        // road meets the Kirkkonummi turn.
        new("porkkala", "Porkkala run", "Helsinki → Porkkala", ActivityKind.Car, PorkkalaRun,
            trafficLightsAtM: [420, 1180, 2050, 2760, 3400, 14_900]),
        new("siuntio", "Siuntio back roads", "Siuntio", ActivityKind.Car,
            WindingRoad(60.1400, 24.2600, headingDeg: 285, lengthM: 6600, wavelengthM: 700, amplitudeM: 110, limitKmh: 80)),
    ];

    public static Route ById(string id) => All.First(r => r.Id == id);

    public static IReadOnlyList<Route> ForKind(ActivityKind kind) => All.Where(r => r.Kind == kind).ToList();

    /// <summary>The route a demo of this kind starts on when the rider has not picked one.</summary>
    public static Route DefaultFor(ActivityKind kind) => ForKind(kind).First();
}
