# Momentum tracker app spec

What `Ikon.App.Examples.Momentum` does today: a consumer movement tracker — run, ride, horse, drive —
recorded from real background GPS. Read this before changing the app, the detectors or the simulator.

## Why it exists

`docs/private/specs/strategic-positioning-spec.md` states the category line: **conventional apps wait
to be asked; Live Apps are already running.** Momentum is the most literal demonstration of it the
repo has. The phone sits in a pocket with the screen off and the browser tab closed, and the app keeps
recording, keeps coaching out loud, and keeps finding highlights — because the app is not on the
phone. Verified on the iOS simulator: with the app backgrounded behind Safari, CoreLocation delivered
89 fixes in 90 seconds and the server-side recording advanced from 0.13 km to 0.36 km.

It also lives in `Ikon.App.Examples.*` rather than `Ikon.App.Showcase.*`: the Showcase six share
`ShowcaseKit.cs`, a fixed paper-light theme and an operator-and-approval narrative that a consumer
tracker does not fit. It is the first Ikon app with a real map on Flutter.

## Brand

Ikon's own identity, per `brand/ikon-brand.md`, in dark. Monochrome charcoal with one electric magenta
accent; colour comes from the content — here the route line and the medals. `Momentum.Brand.cs` is the
implementation.

| Token | Value | Use |
|---|---|---|
| Page | `#0b0b0d` | background |
| Surface | `#161618` | the map's ground |
| Card | `#1c1c1f` | cards, sheets |
| Active | `#242427` | hover, selected — **neutral, never a brand wash** |
| Border | `#2f2f33` | hairline |
| Brand | `#db176e` | fills, the route line, primary buttons |
| Brand text | `#e62e7d` under 18px on dark; `#ff5ba0` bright | contrast per the guide's §2.6 |
| Reward | `#f2da00` | gold medals, personal bests, the recording indicator — **spark, never a CTA** |
| Success | `#38d3bd` | the elevation trace |
| Display / Body / Mono | Poppins / Inter / JetBrains Mono | |

Display type is tight and heavy, micro-type wide and tracked; radius base `0.25rem`; `ring-1` hairline
cards; flat shadows; `tabular-nums` on everything that updates in place. The recording indicator is
the brand's pixel-wave motif, not a spinner.

## Activity kinds

`ActivityKind` is `Foot`, `Bike`, `Horse`, `Car`. Walking and running are one kind on the wire — the
phone cannot tell them apart when you press start — and the label is resolved afterwards from the pace
actually held: under 7 km/h it is a Walk, above it a Run. Every per-kind constant the recorder and the
detectors need lives in one `KindProfile` in `Momentum.Models.cs`.

## Screens

1. **Move** — one huge display number, the live map, a mono metric grid, the auto-pause chip, the
   coach's last cue, the highlights found so far, and start/pause/finish. When an outing is waiting to
   be published this screen becomes the publish gate instead.
2. **Feed** — the log: route silhouette, kind, title, distance, moving time, climbing, momentum score.
3. **Activity** — full map, splits, speed and elevation charts, and the highlight reel. Selecting a
   highlight lights its span on the map in the reward yellow and overlays it on both charts.
4. **You** — totals, distance by week, a per-kind breakdown, and the coach toggle.

## Recording pipeline

```
phone (Flutter, backgrounded) --ikon.client.startLocationUpdates--> app.Locations.OnUpdate
simulator (server-side)       -------------------------------------->         |
                                                                     TrackRecorder (per rider)
                                        accuracy gate · smoothing · auto-pause · running totals
                                                                              |
                                                 Reactive frame ---> UI + coach (TTS) + lock screen
                                                                              |
                                                                  on finish: detectors
                                                                              |
                                                          AI curation -> publish gate -> Postgres
```

- **Every device, one recorder.** Start is pressed on whichever device is to hand, and
  `StartDeviceTrackingAsync` asks *all* of the rider's connected sessions to stream. The first session
  to deliver a fix owns the outing; every other session is ignored for its duration. Pressing start on
  a laptop and having the phone record is the behaviour riders expect, and the lock is what stops two
  devices braiding two tracks into one.
- **Accuracy gate** — fixes over 50 m accuracy are discarded; fixes over 25 m are recorded but may not
  start or end a pause.
- **Smoothing** — a scalar Kalman update per axis, weighted by the accuracy the device claims.
  Elevation is filtered far harder and against a plausible climb rate, and gain accumulates only past a
  4 m hysteresis band sampled every 25 m of travel — without all three, a flat lakeside loop reports
  two hundred metres of climbing.
- **Auto-pause** — a per-kind state machine. A car's dwell is 20 s so a red light is not a pause; a
  runner's is 8 s. **A pause never drops a fix.** It stops the clock and the distance, nothing else,
  because the stop itself is what the traffic-light detector measures from.
- **A minimum step of 0.4 m, not more.** A floor above 1.4 m silently discards every step a walker
  takes; distance then reports barely half of what they covered.

## Surviving a restart

An outing is written to Postgres **as it happens**, not when it ends. The `activities` row is created
on the first second with `in_progress` set, the track flushes every five seconds, and the running
totals go with it — so the row and its points are always at most five seconds behind the rider.

On the next client join, `ResumeInProgressAsync` looks for an unfinished outing for that rider and
rebuilds the recorder from what was stored. The stored points are replayed as **state, not re-pushed
fixes**: they have already been through the accuracy gate and the filters, and running them through a
second time would smooth an already-smoothed line and inflate every total. Tracking is restarted on
the rider's devices and the coach picks up where it left off.

A simulated outing is not resumed — its simulator is gone — so it is deleted instead.

This is what makes the app deployable while someone is out on it: a deploy stops the running session,
the phone reconnects, and the ride continues. Verified by killing the server mid-outing with the
iOS simulator streaming real GPS: the log recorded `Resumed interrupted outing … at 303 m` and the
screen came back still recording, at 0.66 km.

What is still lost is the outage window itself. Fixes produced while the client cannot reach the
server are dropped, because nothing buffers them on the device — a few seconds for a deploy, longer
for a tunnel. On-device buffering is the fix and is not built.

## A reconnecting phone is a new session

The failure this caused was the worst the app has had: a run auto-paused at a rest and never came
back — not when the runner set off again, and not after the app was restarted.

`StartDeviceTrackingAsync` was only ever called when an outing **started**, or when one was resumed
from cold. A phone that reconnects — a dropped socket, a restarted app, a redeploy — arrives as a new
session, and `ResumeInProgressAsync` returns early when an outing is already recording, so nothing
asked the new session to stream. The recording stayed alive and deaf: the track stopped where the
connection dropped, and the auto-pause never lifted because no fix ever arrived to contradict it.
Restarting the app made it worse, not better — another new id, still nobody listening.

Two things fix it, and both are needed:

- **`EnsureTrackingForClientAsync` on every join.** If an outing is running and this client is not
  already streaming, arm it.
- **The fix lock has to heal.** `_recordingSessionId` names the one device whose fixes count, so that
  two devices cannot braid two tracks into one. Held by a session that has gone away, it discards
  every fix from the phone that comes back in its place. It is released when that client leaves, and
  it lapses on its own after thirty seconds of silence — because a phone can stop delivering without
  ever leaving cleanly.

Verified by restarting the phone app mid-outing against real GPS: `Re-armed location tracking for
reconnected session 6`, `Location ownership passed from session 4 to 6 after a lapse`, and the
distance carried on from 3.07 km to 3.24 km.

The auto-pause state machine was hardened alongside it, on the principle that being stuck paused
costs the rest of the outing while pausing late costs seconds: a vague fix may not *start* a pause but
can no longer *hold* one, and the resume displacement is measured from the raw fix rather than the
filtered position, whose gain collapses when the speeds it is fed are near zero.

## Reconnecting

The Flutter shell's retry loop originally covered only the *first* connect. A session that went away
later — a redeploy, a server cycling, a long tunnel — left the phone holding a dead socket behind a UI
that still drew and answered nothing, recoverable only by force-quitting the app. Mid-outing that is
the worst failure the app has. `_scheduleReconnect` now redials with exponential backoff whenever the
connection reports `offline` or `offlineError`, and the server-side resume above means the ride is
still there when it lands.

## Highlights

Detection is deterministic geometry and physics over the recorded track (`Momentum.Detectors.cs`), so
it is reproducible and testable, and the same stretch of road always produces the same highlight —
which is what makes a personal best mean anything. The AI ranks and narrates; it never decides whether
a highlight exists.

Every kind: **climb** and **descent** (scored on cycling's length × grade index and categorised
HC/1/2/3/4, best two of each), **top speed** (fastest 3 s window), and **fastest 1/5/10 km**.

| Kind | Also |
|---|---|
| Foot | **surge** (a stretch 10 % above the median rolling pace), **negative split**, **metronome** (longest stretch inside 4 % of average) |
| Bike | **flyer** (longest stretch above the kind's fast threshold) |
| Horse | **gaits** — walk/trot/canter/gallop segmented from the speed profile with hysteresis, best example of each; **trail** (sinuosity against the straight line) |
| Car | **clean straight** (heading drift under 4° for 500 m or more), **traffic-light launch** (a stop of 3 s or more, then 0–50, 0–100 where reached, and peak longitudinal g), **best corner** (lateral load from yaw rate), **smooth hands** (RMS jerk) |

Two measurement notes that are easy to get wrong again:

- **Corner load comes from yaw rate, not from fitting a circle to three fixes.** At 100 km/h
  consecutive fixes are 30 m apart and carry several metres of error each; a circle through them
  reports 20 m radii and lateral loads no road car could survive. `a = v · ω` from the reported heading
  is smooth and correct.
- **Surge and metronome run on a 30 s rolling pace, against its median.** Per-sample thresholds measure
  the wobble rather than the effort, and a mean threshold is dragged up by the surge it is meant to find.

Each highlight carries a 0–100 score and a medal tier compared against the rider's own history for that
kind and detector, so gold means "better than you have ever done". The activity's **Momentum score** is
the weighted roll-up, and is what the app is named after.

## The AI

- **Live coach** — every 45 s while moving, `Emerge.Run<CoachCue>` produces one short cue and
  `Audio.SpeakAsync` speaks it. It is a separate loop from the recorder on purpose: a slow model call, a
  speech generation or a dropped network must never delay a fix or a pause decision, and a missed cue
  costs nothing.
- **Curator** — on finish it names the outing, writes two sentences, and orders the reel. Publishing
  **holds for the rider**: they can accept, edit the title, or put a dropped highlight back. Nothing
  reaches the feed unseen, and nothing is written to Postgres before they agree.

## The lock screen

At every whole kilometre — and whenever the outing pauses or picks up again — a notification tagged
`momentum-live` carries distance and moving time on the first line and pace and climbing on the
second, replacing the previous one rather than stacking under it. A closing notification says what the
outing came to.

**Why milestones and not a ticking readout.** The first version updated every twenty seconds at
`NotificationPriority.Low` and never appeared at all: Low maps to iOS's `passive` interruption level,
and a passive notification goes straight to Notification Centre without ever lighting the lock screen.
At any level that does show, every update alerts — iOS has no "update quietly" for an ordinary
notification — so a twenty-second tick would buzz a wrist two hundred times on a long ride. A
kilometre is a milestone worth feeling and the number a runner wants anyway.

It is addressed to the **user**, not to a session id captured when the outing started: a phone that
reconnects comes back under a new id, and a readout sent to the old one is one nobody ever sees.

**Not a Live Activity.** A readout that sits on the lock screen and updates silently and continuously
needs iOS's ActivityKit — native Swift the Flutter shell does not carry. Android's equivalent is an
ongoing foreground-service notification, which needs an `Ongoing` flag and a cancel call the SDK does
not have. Both are the real fix and neither is built.

## Cadence

`app.Motion` streams the phone's accelerometer in batches and `CadenceTracker` turns it into beats per
minute — steps, strides or hoofbeats. It is the one measurement here that GPS cannot reach: a speed
trace cannot separate a collected canter from a fast trot, or a shuffling jog from an easy run,
because each pair covers ground at the same rate. The difference is in the rhythm.

Fifty hertz resolves a footfall comfortably and half-second batches keep it to two calls a second. The
peak threshold rides on an exponential baseline and envelope rather than sitting at a fixed number of
m/s², because a walker's footfall is an order of magnitude softer than a runner's and one fixed
threshold cannot find both. Only the device that owns the outing is listened to, for the same reason
its fixes are: two phones would interleave two rhythms into one meaningless average.

Verified against synthetic footfall traces — sharp impulses on drift with noise, not clean sines — at
160, 110 and 78 per minute, recovered inside 12 %. Stillness and a gyroscope stream both correctly
produce nothing.

**Gaits are not detected from it yet.** Cadence is the input that makes *ravi* and *laukka*
separable; the classifier over it is the next step.

## Device-held recording

Every real outing is recorded twice, by two paths that want opposite things.

The **live stream** feeds the screen, auto-pause, the coach and the lock-screen banner. It is
decimated — twelve motion samples a second over the wire out of the fifty the phone reads — and gaps
in it are expected. The **device archive** is the record: the phone writes every fix and every motion
sample to its own storage as they happen, and uploads the file when the outing stops.

This is what makes it a tracker rather than a demo that needs a good connection. The server-side
recording survives a server restart but never survived a *network* one: a fix that failed to send in
a tunnel, a dead spot or on a flat cell was simply gone, because it never arrived. A phone that keeps
its own fixes loses nothing to the network at all.

**The archive repairs rather than replaces.** Everything the rider looks at mid-outing is computed
here from fixes as they arrive, and restart survival depends on the server's own progressive flush;
inverting authority mid-ride would risk all of that to fix something that only shows at the end. So
the live recording stays exactly as it is, holes and all, and `Momentum.Archive.cs` re-runs
`TrackRecorder` over the uploaded raw fixes at the end and swaps the result in. Every failure of the
new path — a failed upload, a dead phone, a client too old to record — degrades to precisely the
track the live stream already built.

Raw fixes, not processed points, for the same reason: the recorder is the processor, it is
deterministic, and re-running it over a complete set beats a track assembled from whatever arrived. A
repair that would *shorten* the outing is refused — the live stream cannot invent distance, so a
shorter archive is the incomplete one.

The phone keeps each file until the server acknowledges it and deletes it after, so an upload that
fails is retried on the next connection rather than lost, and the device holds at most the outings
that have not made it home. Every client that joins is asked for anything pending. One archive id is
one file, so starting and stopping repeatedly gives one archive per outing and never a blend of two.

Because the archive costs no cellular data, it is recorded for **every** kind including a drive, and
with the gyroscope on for all of them. A car earns it as much as a horse: braking, launches and
cornering load are all in the accelerometer and none of them are in a speed trace. The live stream
carries about 2 MB an hour where the undecimated one carried 21.

The uploaded file is kept as an asset, so it is also the gait corpus —
`platform-dotnet/Ikon.App.Examples.Momentum/horse-gait-from-rider-phone-research.md` explains why one is needed and what
would be trained on it. **The labels are still missing**, and they remain the real obstacle rather
than the classifier: nothing in the app yet asks the rider what gait they were in.

Platform side: `app.Recordings` (`RecordingArchiveService`), `app.Uploads` (`UploadService`) for the
headless upload registration, and `IkonRecordingArchive` in the Dart SDK. The format is written in
Dart and decoded in C#, so the byte layout is pinned by tests on both sides.

## Motion analysis

`Momentum.Analysis.cs` reads the recorded motion once the outing is over. It runs only on an uploaded
archive, because the live stream is decimated to what a screen needs and full-rate motion never
reaches the server any other way.

**The axes come first, and without them nothing else means anything.** A phone in a pocket or a cup
holder sits at whatever angle it landed at, so its x, y and z are arbitrary. But the outing has a
second, independent account of itself — speed and heading from GPS — and differentiating those gives
longitudinal acceleration and yaw rate in the world's frame at one hertz. The device axis that best
explains each is a least-squares projection with a closed form: the correlation vector, normalised.
Forward comes from acceleration against speed change, up from the gyroscope against heading change,
and lateral is their cross product. No search, nothing to converge.

When nothing turned, the gyroscope carries no signal and the fit correctly reports *no* lateral axis
and a halved confidence, rather than inventing a direction that every cornering number would then be
measured along.

From that: peak braking, peak acceleration, peak lateral g, and the combined figure — turning and
stopping spend one budget of grip, so what they reach together says more about how close the outing
came to the limit than either alone. Plus jerk, which is the difference between a squeeze and a stamp.

The rhythm is found by autocorrelation of the acceleration magnitude, not a Fourier transform: a
footfall is a sharp impulse whose energy spreads across many harmonics, so a spectrum shows the
harmonics where the autocorrelation shows the beat. Two details matter. It takes the **first** strong
peak rather than the strongest, because a periodic signal correlates just as well at twice its period
and taking the maximum reports half the true rate about as often as not. And it reports the **beat**,
not the stride: a trot is two identical half-strides, so its signal repeats twice per stride and no
autocorrelation can say which period is "the" stride — they are equally good answers.

`RhythmStrength` says how much of the signal the beat explains, which separates a steady rhythm from
a phone rattling in a door pocket. It does **not** separate a trot from a canter; that is what the
gait segmentation below is for.

### What a real drive taught it

The first 62 km motorway drive fitted its axes at **0.13 confidence** and produced nothing, while
synthetic traces fitted at 0.99. Three things were wrong, and all three only show on real data:

- **Vibration swamped the fit.** Vehicle dynamics live below about half a hertz; road, engine and a
  phone shifting in a cup holder live far above and are much larger. Both the fit and the peaks now
  run on a one-second boxcar — zero-phase, because a lag against the GPS being fitted to is the exact
  error the step exists to avoid. `Rhythm` still uses the raw signal, where the high frequencies *are*
  the measurement.
- **Confidence was mis-normalised.** It divided by the sensor's full three-axis energy, so motion
  across the axis, drift and residual vibration all counted against a fit that was correct. It is now
  the correlation of the signal *along* the fitted axis against the reference.
- **Cruising diluted it.** Most of a drive holds a steady speed: the reference is ~0 while the
  accelerometer still carries vibration and steering, so those samples drag the correlation down
  however right the axis is. Confidence is now measured only where the vehicle actually changed
  speed — when there was something to track, did the axis track it?

0.13 → 0.19 → 0.26 → **0.33**, and the drive reported 0.40 g braking, 0.36 g cornering and 0.43 g
combined. Nothing about the capture or upload had ever been wrong; all three faults were in the
reading of it, and none was visible from synthetic tests.

## The operator view

`?section=admin` — not a tab, reached by URL. Every outing with the counts that say whether it
worked: track points, highlights, whether a device archive is stored and how big, and whether motion
analysis has run. **Re-run analysis** re-derives everything from stored data, so a detector can be
improved and applied to rides already recorded rather than only to the next one, and it reports the
figures it got rather than a count — a run that finds nothing is the interesting case, and "0
highlights" does not say whether the axes failed or the driving was gentle.

It exists because the questions that matter after a real outing were only answerable by opening a
psql session against the space, which is a bad place to keep the truth about whether a feature works.

## Gaits

`Momentum.Gaits.cs` splits a horse outing into stretches of *käynti*, *ravi*, *laukka* and
*neliravi*. Speed cannot do this — a collected canter and an extended trot cover ground at the same
rate, and that is precisely the boundary a rider cares about.

What separates them is how a stride divides. Find the footfalls, take the intervals between them, and
look for the period those intervals repeat with: even intervals mean a symmetric gait, intervals
repeating every third mean a canter, every fourth and unevenly a gallop.

That settles canter and gallop but not walk against trot, which are both perfectly even. Those two
separate on **ground covered per footfall** — stride length over beats per stride — about 0.45 m
walking and 1.3 m trotting. Beat rate cannot do it, and this is the trap worth naming: a walk puts
down four feet a stride against a trot's two, so a walk beats *faster* than a trot while travelling a
third of the speed, and the two rates overlap outright.

Per-window scores then go through a Viterbi pass rather than being taken one at a time. Gaits do not
change arbitrarily — walk to trot to canter and back, rarely walk straight to canter — and a horse
holds one for seconds. Scoring whole paths lets confident neighbours outvote a single ambiguous
window, which no per-window decision can do however good its features are.

**The thresholds come from what the gaits are, not from fitted data.** No labelled riding was
available to tune them against, so this is a well-founded reading rather than a measured fact, and
the boundaries should be expected to move once real rides can be checked against it —
`platform-dotnet/Ikon.App.Examples.Momentum/horse-gait-from-rider-phone-research.md` is what that would take. It is
verified against synthetic traces built from each gait's actual beat structure, including the case
that matters: a collected canter and an extended trot at an identical 5 m/s, which every speed-band
classifier calls the same thing and this one does not.

The measured highlights sit alongside the GPS ones rather than replacing them: a speed trace can
infer that a corner happened and roughly how hard, the accelerometer measured it, and the inferred
ones still cover every outing recorded by a client with no motion stream at all.

## The simulator

Real GPS cannot be exercised from a desk, so a seeded simulator feeds the identical recorder behind the
same `RawFix` (`Momentum.Simulation.cs`). It has to be realistic enough that the detectors are actually
tested:

- **Routes** (`Momentum.Routes.cs`) — control points following real geography, resampled through a
  Catmull-Rom spline every 10 m with an elevation profile; car routes carry a speed limit per segment
  and traffic-light positions. The one exception is the Siuntio back road, which is generated: it is a
  sine laid along a heading, and it exists because the fast road out to Porkkala is genuinely
  arrow-straight and gives the corner detector nothing to find.
- **Physics per kind** — foot: grade-adjusted pace with fatigue, breathing wobble, road crossings and
  occasional effort blocks. Bike: speed solved from rider power against rolling resistance, drag and
  gravity. Horse: a gait state machine with real transition durations. Car: traction- and
  power-limited acceleration, a lateral-g ceiling through corners, and stops at red lights.
- **A GPS realism layer** — correlated positional error with a 30 s memory rather than white noise,
  plausible accuracy values, dropouts, and 1 Hz quantisation. Altitude is modelled as a barometric fuse:
  metres out in absolute terms and drifting slowly, but steady second to second, which is how a modern
  phone actually behaves and the reason ascent is computable at all.
- **Deterministic and speed-multiplied**, so a 45-minute outing replays in the UI in about a minute
  while the physics still steps one simulated second at a time.

Two bugs the simulator surfaced that are worth not reintroducing: solving the bike's speed equation
with Newton's method from a flat-road seed lands on the wrong side of zero on every descent (bisection
is used instead), and a look-ahead heading clamped at the end of a route reports a bearing between a
point and itself, which the corner detector reads as a hard turn nobody took.

## Persistence

The built-in `app` Postgres database — `activities`, `activity_points`, `activity_highlights` — created
idempotently on boot. A rider with an empty log is seeded with one outing of each kind, generated by
running the simulator through the same recorder, so the seeds are outings rather than mock-ups and their
highlights were detected the same way a real one's are.

## Frontends

- `frontend-node` — Leaflet, registered as the `momentum-map` node.
- `frontend-flutter` — `flutter_map` with OSM tiles, registered as the *same* node type through
  `IkonComponentRegistry`, so one `view.MomentumMap(...)` call in C# drives both. First Ikon app to add
  a Flutter dependency of its own.
- `ios/Runner/Info.plist` carries `NSLocationWhenInUseUsageDescription`,
  `NSLocationAlwaysAndWhenInUseUsageDescription`, `NSMotionUsageDescription` and the `location`
  background mode. **The scaffold does not include these** — an Ikon Flutter app that wants location
  has to add them itself, and without them iOS refuses the permission request outright. Android's
  scaffold already declares background location and the foreground-service permissions.

## Platform changes this app required

- **`LocationUpdate` carries altitude and device time.** Altitude is what the climb detectors run on,
  and without a device timestamp a batch of fixes delivered after a network stall all take the same
  arrival time and corrupt every derived speed. Both arrive as trailing optional arguments on
  `ikon.server.locationUpdate`, so clients published before them keep working. A null altitude means
  "unknown" — JSON carries no NaN, and a sentinel would read as sea level.
- **The Dart SDK sends the timestamp as a double.** A Dart `int` is inferred as `System.Int32` on the
  wire and a millisecond epoch does not fit in one; sent as an `int` it fails the whole call, and the
  Dart client swallows that failure silently, so every fix disappears with no error anywhere.
- **`AxisConfig.Hidden`.** An unset axis means "the default axis", so there was previously no way to say
  "no axis" from C# — which a chart used as a shape rather than as a reading needs. A route silhouette
  in a feed row was drawing tick labels.

## Running it on a real phone

`ikon app run --flutter-ios` boots a **simulator** only; there is no flag for a physical device, so
that leg is `flutter run` by hand. Two prerequisites, both at the Xcode GUI and neither scriptable:
pair the phone in Xcode's Devices window, and sign in with an Apple ID so a signing certificate
exists (`security find-identity -p codesigning` must report more than zero). A free Apple ID is
enough — it gives 7-day installs. Then set the Runner target's team in Xcode once.

The iOS bundle id is `com.ikonai.momentum`: a personal Apple team refuses an id another developer has
already registered, and the scaffold's `com.example.*` default is the most collided prefix there is.

Against the **deployed** app — the only way to test what the app is for, because a local dev server
means staying inside wifi range:

```
flutter run --release -d <device-id> \
  --dart-define=IKON_DEPLOYED=true \
  --dart-define=IKON_SPACE_ID=<SpaceId from ikon-config.development.toml> \
  --dart-define=IKON_SERVER_HOST=<domain>.dev.ikonai.app \
  --dart-define=IKON_AUTH_URL=https://auth.dev.ikonai.com
```

Those are the same defines `ikon app bundle --flutter-ios` injects (`FlutterFrontendBuilder.
BuildFlutterDefines`); `flutter run` is used instead because it installs straight onto the phone
without an export-options plist or a distribution profile.

**Deploy with `--platform-repo` until the platform changes above ship.** `LocationUpdate.
AltitudeMeters` and `AxisConfig.Hidden` are not in the published NuGet/npm packages, so a deploy
without it does not compile.

On the phone, iOS asks for location twice. The first prompt only offers **While Using**, which stops
recording the moment the screen locks; **Always** arrives as a second prompt after a while, or from
Settings → Momentum → Location. Pocket recording needs Always.

## Tests

`Ikon.App.Examples.Momentum.Test` — 22 tests over seeded simulated outings. Every one of them encodes
a failure that actually happened while building the app, which is the only reason they are worth
running: a walker's distance halved by a step floor set above a walking pace, a flat lakeside loop
reporting two hundred metres of climbing, a bike stalled at 0.9 m/s on every descent by a Newton
solver that lands on the wrong side of zero once gravity outweighs resistance, a car that never left
the first junction because its traffic light was never retired, a phantom 0.83 g corner from fitting a
circle to three noisy fixes, and a hard left at the end of every route from a look-ahead clamped onto
the point it started from.

Two conventions worth keeping:

- **Assert ranges a physicist would accept, not the number the code returns today.** A test pinned to
  the current value is a change detector, not a check that the measurement is right.
- **The run is pinned to the invariant culture** (`TestCulture`). The detectors format their
  measurements with the ambient culture, so a corner reads `0,41 g` on a Finnish machine and `0.41 g`
  elsewhere; without pinning, tests that read those strings pass in one place and fail in another.
  Worth noting the underlying oddity: those numbers are formatted with the *server's* locale, not the
  reader's.

The suite was checked by reintroducing two of the bugs above and confirming it failed.

## What actually broke the start sheet

Worth recording, because the first diagnosis was wrong and someone will otherwise repeat it.

Two separate faults produced one symptom — a dimmed screen with no sheet on it:

1. **A click handler on the sheet's parent.** The dismissing backdrop was the sheet's own parent, so
   every tap *inside* the sheet also dismissed it: choosing a kind shut the sheet before the choice
   could be acted on. Real, reproduced on the web, and fixed by making the backdrop a sibling.
2. **A stale app session after a deploy.** A running session keeps the old build, so the phone stayed
   attached to code that no longer existed — the UI drew and nothing responded. This is what produced
   "dark backdrop, no sheet, no buttons work", and restarting the app fixed it.

**It was not a Flutter renderer gap.** That was the first theory and it was wrong. A `fixed inset-0`
container holding an `absolute inset-0` backdrop beside a `w-full` flow child renders correctly on
Flutter — verified by `flutter_layout_parity_test.dart`'s "a flow child beside an absolute backdrop
lays out at full width", which was written to prove the gap and instead disproved it. An earlier
version of that repro *did* fail, but only because it put the overlay in a root with no page content,
which collapses the Stack to nothing — an artefact of the repro, not of the app.

Momentum still uses `view.Dialog` for the sheet, which remains the right component: it owns the
overlay, the dismissal and the stacking, and none of it has to be hand-rolled.

Unrelated but found alongside: the Flutter scaffold's `main.dart` ships
`scaffoldBackgroundColor: Color(0xFF0F172A)`, a slate blue. The Parallax view sits inside a
`SafeArea`, so that colour shows as bars above the status bar and below the home indicator in every
generated Flutter app whose theme is not slate. Momentum sets its own; the template arguably should
not pick a colour at all.

## Not built

On-device buffering across a network outage (fixes are pushed live, so a tunnel loses them), a social
graph beyond the log, segments and leaderboards, GPX import and export, HR or power-meter pairing,
route planning, and the true ongoing/Live-Activity lock screen described above.
