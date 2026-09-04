namespace Ikon.App.Patterns.Patterns;

// Pattern: offline-recording-archive — see docs/patterns/offline-recording-archive.md.
// The docsnippet region is the repair loop: record on the device, take the archive when it lands,
// re-derive the track from complete data. app.Recordings throws on a host with no device attached,
// so the service calls stay out of the render path the gallery drives.
internal sealed class OfflineRecordingArchive(IAppBase app) : IPatternDemo
{
    public string Slug => "offline-recording-archive";
    public string Title => "Offline recording archive";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => RenderTrackQuality(view);

    private sealed record Outing(string Id, int FixCount, bool Repaired);

    private readonly ReactiveList<Outing> _outings = Seeded();

    private static ReactiveList<Outing> Seeded()
    {
        var outings = new ReactiveList<Outing>();
        outings.AddRange([new Outing("morning-run", 1284, true), new Outing("river-loop", 412, false)]);
        return outings;
    }

    #region docsnippet:pattern-offline-recording-archive
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
    #endregion
}
