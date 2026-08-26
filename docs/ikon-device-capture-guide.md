# Ikon Device Capture Guide

How an Ikon app reads a phone's sensors, keeps a record when the network does not cooperate, shows a running activity on the lock screen, and receives files nothing on screen asked for. Four services, all reached from `app`, all designed for the case where the app is in a pocket rather than in front of someone.

| Service | Reached by | What it is for |
|---|---|---|
| `MotionService` | `app.Motion` | Accelerometer, gyroscope and magnetometer, streamed in batches |
| `RecordingArchiveService` | `app.Recordings` | The device records to its own storage and uploads when done |
| `LiveActivityService` | `app.LiveActivity` | The iOS lock-screen banner, updating with the app closed |
| `UploadService` | `app.Uploads` | Uploads no rendered component asked for |

All four are default-implemented on the app interface and throw `NotSupportedException` on a host that cannot provide them, so a server-side test double does not have to implement any of them.

## Motion — what GPS cannot see

Location answers *where*. Motion answers *how it is moving*, which is the thing a position trace cannot tell you: a speed trace cannot separate a collected canter from a fast trot, and it cannot see a swing, a gesture or a single step at all. Stride and rotation are in the accelerometer.

```csharp
app.Motion.OnBatch(batch =>
{
    foreach (var sample in batch.Samples)
    {
        _peak.Value = Math.Max(_peak.Value, sample.Magnitude);
    }
});

await app.Motion.StartTrackingAsync(sessionId, new MotionOptions(
    Hertz: 50,
    Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope,
    BatchMilliseconds: 200));
```

`MotionOptions` carries the whole configuration:

- **`Hertz`** — samples per second per sensor. 25 is plenty to tell a walk from a trot; a controller wants 60 or more. Devices honour this approximately.
- **`Sensors`** — a `[Flags]` combination of `MotionSensors`: `UserAcceleration` (gravity removed — what the person or vehicle is doing), `Acceleration` (raw, which also tells you which way is down), `Gyroscope` (rad/s), `Magnetometer` (heading, and noisy indoors or near anything with a motor in it).
- **`BatchMilliseconds`** — how long the client buffers before sending. Sending each sample on its own would put a round trip on every one of them; batching turns fifty calls a second into five.
- **`Background`** — keep streaming while backgrounded. On iOS this needs an already-running background mode; motion alone does not keep an app alive, so pair it with location tracking if the app must read from a pocket.
- **`LiveHertz`** — send only this many samples a second live while the recording archive keeps every one on the device. Zero streams everything.

Each `MotionBatch` carries the samples in device order, the session and user it came from, and the server time it arrived. Each `MotionSample` carries device time in `AtMillis`, the three axes in the device's own frame (x across the screen, y up it, z out of it), which sensor produced it, and a `Magnitude` convenience across all three axes.

**Derive rates from `AtMillis`, never from arrival time.** `MotionBatch.At` is when the *server* received the batch; a slow network would otherwise read as a slow athlete.

`OnBatch` handlers run on the pushing client's reactive scope, so writing per-user or per-session reactive state from inside one needs no captured ids. Subscribe once in `OnStarting`, not per client.

**This is not the transport for a low-latency controller.** Batched calls carry at least one batch of scheduling delay and deliver every sample reliably whether or not it still matters. A phone used as a pointing device wants an unreliable app-defined `.tp` message, where a dropped sample is superseded by the next. Use motion for analysis — gait, cadence, activity, impact — and a `.tp` channel for control.

`StartTrackingAsync` returns `false` when the client has no such sensor or a build that cannot read it. That is "this device can't", not an error.

### Motion and location together

Motion answers *how*, location answers *where*, and most captures want both. `app.Location` tracks a
client with `LocationTrackingOptions` and delivers each fix as a `LocationUpdate`; `ClientLocation`
is the last known position for a session. Start them together and stop them together — a recording
that keeps reading motion after location stops looks alive while producing a track that stands
still.

## Recordings — the track that survives a tunnel

A fix that fails to send in a tunnel or on a flat cell is gone. No server-side durability recovers it, because it never arrived. `app.Recordings` has the device write its own fixes and motion to local storage and upload the file when the activity ends.

```csharp
app.Recordings.OnArchive(archive => Repair(archive));

await app.Recordings.StartAsync(sessionId, outingId, new RecordingOptions(
    Fixes: true, Motion: true, MaxBytes: 128L * 1024 * 1024));
```

`RecordingOptions` decides what goes in the file: `Fixes` (almost always yes — this is what survives an outage), `Motion` (at the full rate asked of `app.Motion`, independent of any `LiveHertz` decimation), and `MaxBytes`, which is a refusal rather than a target — a device out of space must fail the recording, not the phone.

A `RecordingArchive` arrives with the activity id the app gave it, the uploading session and user, when the device opened the file, the recorded `Fixes` and `Motion` in device order, and an `Asset` pointing at the raw bytes. Each `RecordedFix` is raw on purpose — no smoothing, no auto-pause, no elevation fill — because the app's own recorder is the processor, and re-running it over a complete set beats a track assembled live from whatever the network delivered. `RecordingRecordKind` distinguishes the two record types inside the encoded file if you ever decode it directly.

Three rules make this work:

1. **Pair it with the live stream; do not replace it.** The live stream drives the screen; the archive lands at the end and repairs the record. A failed upload or a client too old to record then degrades to the live track rather than to nothing.
2. **The archive replaces, it does not append.** The live track was gappy by definition. Re-derive from the complete fix list rather than merging, or you keep exactly the holes this exists to remove.
3. **Ask for pending uploads on every client join.** `RequestPendingAsync(sessionId)` is how a stranded recording gets home. The device keeps each file until the server acknowledges it, so an upload that failed days ago is still waiting.

Archives arrive through the upload transport under the fixed id `RecordingArchiveService.UploadActionId` — fixed rather than generated, because the session that finally uploads may be a different one from any that ever rendered.

## Live activities — the lock-screen banner

`app.LiveActivity` puts a banner on the iOS lock screen and in the Dynamic Island while something is running, updating in place with nobody looking at the app.

```csharp
await app.LiveActivity.StartAsync("Momentum", "#db176e",
    [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");

await app.LiveActivity.UpdateAsync(metrics, status: "Run");
await app.LiveActivity.EndAsync();
```

It carries **values, never layout**. One widget draws every app's banner, which is why this needs no per-app native code even though the banner itself cannot be Flutter — iOS renders it through WidgetKit from SwiftUI archived at update time.

- A `LiveMetric` is a formatted `Value` and a `Label`. The widget does no formatting: units, precision and padding are yours.
- **Three metrics maximum.** Anything past the third is not shown.
- `title` is fixed for the life of the activity, usually the app's name; `status` is the tracked line above the metrics — a phase, a state, a kind.
- `muted: true` is the paused or held look, which desaturates the accent.
- Prefer `UpdateAsync` to a repeated `StartAsync`. A second start would orphan the first banner with numbers that never move again; the client folds a repeat start into an update, but calling the right one says what you meant.
- **End it on client-left as well as when the activity finishes.** A banner left behind outlives the app and freezes at whatever it last said, on a screen the person cannot dismiss it from.

Every call answers `false` rather than throwing where a banner cannot be shown — a browser, an Android device, iOS below 16.2, a shell that predates the bridge. A banner is a nicety and its absence must never take an app down with it.

## Uploads — files nothing on screen asked for

`view.FileUpload` covers a person picking a file, and registers itself as it renders. `app.Uploads` is the same transport for the case where nothing is on screen: a client sending what it recorded while the app was in a pocket, a background sync, a device catching up on work it did offline. Both paths share one handler, so an upload behaves identically whichever asked for it.

```csharp
app.Uploads.Register("my-app.telemetry",
    onStart: args => Task.FromResult(new FileUploadResult
    {
        AssetUri = new AssetUri(AssetClass.CloudFile, $"telemetry/{args.FileName}", app.GlobalState.SpaceId),
    }),
    onComplete: async args =>
    {
        if (args.AssetUri is { } uri) { await ProcessAsync(uri); }
    });
```

- **Namespace the id.** The ids that rendered `view.FileUpload` components generate live in the same table.
- Registering the same id again replaces the previous handlers.
- Returning an `AssetUri` from `onStart` streams the bytes straight into asset storage without ever holding them in the app — which is what a large file needs, since an app container has far less memory than the files people send it. Return a result that is not accepted to refuse the upload.
- `onError` runs when a transfer fails partway. A device will generally try again on its next connection.

## Putting them together

A tracker uses all four: motion and location stream live to drive the screen, `LiveHertz` keeps that stream cheap while the recording archive keeps every sample on the device, a live activity shows distance and time on the lock screen while the phone is away, and the archive arrives through uploads at the end to repair the record.

Start from the live path and add the archive second. An app that only streams still works — it just loses the tunnel.
