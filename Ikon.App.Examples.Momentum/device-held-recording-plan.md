# Device-held recording

Plan, written 2026-08-25. **Implemented the same day** — see the "device-held recording" section of
`platform-dotnet/Ikon.App.Examples.Momentum/momentum-tracker-app-spec.md` for the as-built, which is what to trust. Kept for
the reasoning behind the shape.

## The idea

Split what the phone sends into two paths that want opposite things:

| | Live stream | Device archive |
|---|---|---|
| Feeds | live screen, coach, lock screen, auto-pause | the saved activity, and the gait corpus |
| Fidelity | decimated, lossy | complete |
| Latency | seconds | none — it arrives at the end |
| A network outage | leaves a hole, and that is fine | buffered on disk |
| A server restart | server resumes from its own flushed track | untouched |
| Transport | the batched function calls we already have | one file, uploaded at finish, deleted on ack |

Today there is one path doing both jobs, and it is bad at each. It costs the rider cellular data all
ride to deliver samples nobody needs in real time, and it still loses the ride's true record whenever
the network drops, because a fix that fails to arrive is simply gone.

## Why this is worth doing

Three things fall out at once, and the third is the one that matters:

1. **The cellular bill drops.** The live path only has to be good enough for cadence and a speed
   readout. At 25 Hz accelerometer-only it is about 5 MB an hour instead of 21 with the gyroscope.
2. **The corpus stops being lossy.** It is written to disk at full rate with every sensor asked for,
   because the wire is no longer in the way. Chunks dropped during an outage — which the current
   design counts and shrugs at — stop happening.
3. **A ride survives an outage.** This is the real prize. `platform-dotnet/Ikon.App.Examples.Momentum/momentum-tracker-app-spec.md` records
   on-device fix buffering as a known gap: the server-side recording survives a *server* restart but
   not a *network* one, so a tunnel, a dead spot or a flat cell puts a hole in the rider's track that
   nothing can repair. A phone that holds its own fixes repairs it.

## The one design decision

The tempting reading is "the device is authoritative". That is the wrong shape, because everything
the rider looks at mid-ride — the live numbers, auto-pause, the coach, the lock-screen banner — is
computed server-side from the live stream, and restart survival depends on the server's own
progressive flush. Inverting authority mid-ride puts all of that at risk to fix a problem that only
shows up at the end.

**So the archive repairs rather than replaces.** The server-side recording stays exactly as it is,
holes and all. When the outing finishes, the uploaded archive is re-run through `TrackRecorder` from
raw fixes and the resulting track replaces the progressively-flushed one. Every failure of the new
path — upload fails, file lost, phone dies — degrades to precisely today's behaviour, which is a
saved activity that is as good as the network was.

That is also why the archive stores **raw fixes rather than processed points**: the server's recorder
is the processor, it is deterministic, and re-running it over the complete set gives a strictly
better track than the gappy live one. The detectors already run as a full pass at finish, so the
repaired track slots in ahead of them with nothing else moved.

## Shape

**On the phone.** One file per outing, appended as samples arrive, in the chunk format
`MotionCorpus` already defines for motion and a parallel compact record for fixes. Appending to a
local file is cheap and survives the app being killed.

**Upload.** At finish. Retained until the server acknowledges, then deleted — so a failed upload is
simply retried the next time the app connects, and "cleared after upload" is what makes the phone's
disk bounded rather than a growing archive of every ride ever taken. An upload that never succeeds
costs one ride's file, not the device.

**Platform seam.** `onUploadStart` can return an `AssetUri`, so the bytes stream into asset storage
without passing through app memory — the right behaviour for a multi-megabyte file. But the handler
is internal, owned by `Ikon.Parallax.UI` and reached through a view context, so a background upload
with no rendered `view.FileUpload` needs a headless registration path. That is the one genuinely new
platform surface. *(As built: the handler became a per-app singleton shared by both paths, and
`app.Uploads` is the public registration API on top of it.)*

**Applies to every sport.** Nothing here is horse-specific. A run, a ride and a drive all benefit
from a track that survives a tunnel; the gait corpus is just the reason the question came up.

## Steps

1. Headless upload registration — an app-level way to register an upload action id and its
   `AssetUri` destination without rendering a component.
2. Local recording in the Dart SDK: a `record` option on location and motion tracking that appends to
   a per-outing file.
3. Decimate the live motion stream, now that fidelity is the archive's job.
4. Upload at finish, acknowledge, delete. Retry on next connect if it failed.
5. Momentum: on receiving an archive, re-run `TrackRecorder` over its raw fixes and replace the
   track before the detectors run.
6. Retire the counted-gap machinery in `MotionCorpus` once the archive is the source, or keep it as
   the fallback for clients that do not record locally.

Steps 1–2 are the load-bearing ones; the rest is downhill from them.

## What this does not fix

A phone that dies mid-ride still loses whatever it had not uploaded, and the server keeps only what
the live stream delivered. That is the same trade every recorder makes, and the live path having
already saved an approximate track is a better floor than most.
