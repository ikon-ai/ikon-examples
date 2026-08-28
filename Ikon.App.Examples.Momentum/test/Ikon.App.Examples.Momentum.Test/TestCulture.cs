using System.Globalization;
using System.Runtime.CompilerServices;

// The detectors format their measurements with the ambient culture, so on this machine a corner reads
// "0,41 g" and on a CI box in another locale "0.41 g". Tests that parse those strings would pass in one
// place and fail in the other, so the whole run is pinned to the invariant culture — formatting and
// parsing then agree wherever it runs.
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void Pin()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
