# Location & Background GPS

## Location & Background GPS

Continuous device-location tracking — the platform side of a tracker (delivery, ride-hail, field-work) app — through **`app.Locations`** (type `LocationService`). It streams a client's GPS position to the server **including while the app is backgrounded** (an Android foreground-service notification, the iOS background-location mode) and surfaces each fix server-side through a callback. It is the push counterpart to the one-shot `ClientFunctions.GetLocationAsync` (a pull that only works while the client is awake).

### Tracking a client

```csharp
// Observe fixes once (e.g. in Main). Handlers run on the pushing client's scope, so writing
// per-user / per-session reactive state from inside just works.
app.Locations.OnUpdate(update =>
{
    // update: SessionId, UserId, Latitude, Longitude, AccuracyMeters, SpeedMps, Heading, At (UTC)
    _couriers.Update(cs => cs.Select(c =>
        c.SessionId == update.SessionId ? c with { Lat = update.Latitude, Lon = update.Longitude } : c));
});

// Start streaming on a client session — e.g. when a courier goes on shift.
await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(
    IntervalSeconds: 5, DistanceFilterMeters: 10, Background: true,
    NotificationTitle: "Sharing your location", NotificationBody: "Visible while you're delivering."));

// Stop when the shift ends.
await app.Locations.StopTrackingAsync(sessionId);
```

`LocationTrackingOptions(int IntervalSeconds = 10, int DistanceFilterMeters = 10, bool Background = true, string NotificationTitle, string NotificationBody)` — interval and distance filter throttle the fixes; `Background: false` ends the stream when the app is backgrounded; the notification text is the Android foreground-service label the user sees while sharing. `StartTrackingAsync` returns `true` when the client accepted (geolocation is available and permission was not denied outright); `OnUpdate` / `RemoveHandler` add and remove observers. Each `LocationUpdate` carries the pushing `SessionId` and `UserId`, so the server attributes the fix — a client cannot claim to be another session.

### Permissions, background modes, and review

Continuous background location needs the user's **"Always" / background** permission and is subject to app-store review, so **start it only for a real reason** (an active delivery, a live trip) and **stop it the moment it's done** — the visible sharing notification and an obvious on/off control are what a reviewer expects. The iOS background-location mode and the Android `ACCESS_BACKGROUND_LOCATION` + foreground-service permissions are added to the app bundle for you. If you start or stop from a background task rather than a client scope, capture the session id first (`var cid = ReactiveScope.ClientId;` inside the callback) and pass it explicitly.

### One-shot vs. continuous

For a single "where am I now" — prefilling a delivery address, a nearest-store lookup — use the one-shot `ClientFunctions.GetLocationAsync` instead: it requests foreground permission and returns one fix. Reach for `app.Locations` only when you need the live, background-surviving stream.
