namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private async Task DocLocationTrackingAsync(int sessionId)
    {
        #region example:location-tracking
        // Observe fixes once (e.g. in Main). Handlers run on the pushing client's scope, so writing
        // per-user / per-session reactive state from inside just works.
        app.Locations.OnUpdate(update =>
        {
            // update: SessionId, UserId, Latitude, Longitude, AccuracyMeters, SpeedMps, Heading, AltitudeMeters (NaN if none),
            // At (server arrival, UTC), MeasuredAt (device fix time, UTC) — derive speed/pace from MeasuredAt, not At
            _couriers.Update(cs => cs.Select(c =>
                c.SessionId == update.SessionId ? c with { Lat = update.Latitude, Lon = update.Longitude } : c));
        });

        // Start streaming on a client session — e.g. when a courier goes on shift.
        await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(
            IntervalSeconds: 5, DistanceFilterMeters: 10, Background: true,
            NotificationTitle: "Sharing your location", NotificationBody: "Visible while you're delivering."));

        // Stop when the shift ends.
        await app.Locations.StopTrackingAsync(sessionId);
        #endregion
    }
}
