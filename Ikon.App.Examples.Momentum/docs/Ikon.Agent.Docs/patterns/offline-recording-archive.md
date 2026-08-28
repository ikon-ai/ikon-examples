# Offline Recording Archive — the Track That Survives a Tunnel

`app.Recordings` has the device write its own fixes and motion samples to local storage and upload the file when the activity ends. This is the difference between a tracker that works and one that needs good signal: a fix that fails to send in a tunnel or on a flat cell is *gone*, and no server-side durability recovers it, because it never arrived.

It **pairs with** the live stream rather than replacing it. The live stream drives the screen and may be decimated and gappy; the archive lands at the end and repairs the record. Keep the server-side recording as it is and let the archive correct it — then a failed upload, or a client too old to record, degrades to the live track rather than to nothing.

## When to use

Anything recorded outdoors or in motion where the record matters after the fact: runs, rides, deliveries, surveys, field inspections. Any capture where losing the middle of it is worse than showing it late.

## Snippet

```csharp
/// Subscribe in OnStarting. The upload may arrive DAYS later, from a session that never rendered
/// anything — a phone that finally found signal — so nothing about this can hang off a client.
private void Wire()
{
    app.Recordings.OnArchive(Repair);
}

private async Task StartAsync(int sessionId, string outingId)
{
    // Fixes are what survives a tunnel; motion is recorded at the FULL rate even when the live
    // stream is decimated, so the analysis afterwards sees everything the device felt.
    await app.Recordings.StartAsync(sessionId, outingId, new RecordingOptions(
        Fixes: true,
        Motion: true,
        MaxBytes: 128L * 1024 * 1024));
}

private async Task StopAsync(int sessionId, string outingId)
{
    await app.Recordings.StopAsync(sessionId, outingId);
}

/// A client reconnecting with work it could not send at the time. Ask on every join — the device
/// keeps each file until the server acknowledges it, so this is how a stranded outing gets home.
private async Task OnClientJoinedAsync(int sessionId)
{
    await app.Recordings.RequestPendingAsync(sessionId);
}

/// The archive REPLACES the live track rather than appending to it. The live stream was gappy by
/// definition — that is what this exists to fix — so re-derive from the complete fix list.
private void Repair(RecordingArchive archive)
{
    for (var i = 0; i < _outings.Count; i++)
    {
        if (_outings[i].Id != archive.ArchiveId) { continue; }

        // RecordedFix is raw on purpose: re-run the app's own smoothing and auto-pause over the
        // whole set. Keep archive.Asset if the raw bytes are worth re-analysing on a later build.
        _outings[i] = _outings[i] with { FixCount = archive.Fixes.Count, Repaired = true };
        return;
    }
}

private void RenderTrackQuality(IView view)
{
    view.Column([Layout.Column.Sm, "max-w-sm"], content: view =>
    {
        foreach (var outing in _outings)
        {
            view.Row([Card.Default, "items-center justify-between gap-3 p-3"], content: v =>
            {
                v.Text([Text.Body], text: $"{outing.FixCount} fixes");
                v.Text([outing.Repaired ? Badge.SuccessSm : Badge.NeutralSm],
                    text: outing.Repaired ? "Repaired from device" : "Live track only");
            });
        }
    });
}
```

## Notes

- **Wire `OnArchive` in `OnStarting`, never per client.** The upload may arrive days later from a session that never rendered anything — a phone that finally found signal. Nothing about this can hang off a live client.
- Call `RequestPendingAsync` on every client join. The device keeps each file until the server acknowledges it, so this is the call that brings a stranded recording home.
- **The archive replaces, it does not append.** The live track was gappy by definition; re-derive from the complete fix list instead of merging, or you keep exactly the holes this exists to remove.
- `RecordedFix` is raw on purpose — no smoothing, no auto-pause, no elevation fill. The app's own recorder is the processor, and re-running it over a complete set beats one assembled live from whatever the network delivered. Storing the processed result would bake in the gaps.
- `RecordingOptions.Motion` records at the **full** rate asked of `app.Motion`, independent of `MotionOptions.LiveHertz`. Decimate the live stream for the screen and still get everything for the analysis.
- `MaxBytes` is a refusal, not a target: a device out of space must fail the recording, not the phone.
- `RecordingArchive.Asset` is where the raw bytes live. Keep it when the recording itself is worth having — a corpus to train on, or a re-analysis a later build will want to run — and let it go otherwise.
- `RecordingRecordKind` distinguishes fixes from motion inside the encoded file; `RecordingArchiveCodec` decodes it if you ever need the bytes directly. Normally the decoded `Fixes` and `Motion` lists are all you touch.
- Uploads land through `app.Uploads` (a `UploadService`) under the fixed id `RecordingArchiveService.UploadActionId` — fixed rather than generated because the uploading session may be a different one from any that rendered.

## See also

- `device-motion-stream` — the live half, and where `LiveHertz` decimation belongs.
- `file-upload-with-progress` — the person-picks-a-file path through the same transport.
