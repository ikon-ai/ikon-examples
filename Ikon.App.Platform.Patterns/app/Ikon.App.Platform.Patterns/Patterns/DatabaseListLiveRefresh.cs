using Dapper;

namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: database-list-live-refresh — see docs/patterns/database-list-live-refresh.md.
// The doc quotes the example region below. The demo starts from sample notes; a running gallery
// creates the table and starts the effect at start, which then shows the table's rows. The render
// smoke test never starts the app and draws the sample notes.
internal sealed class DatabaseListLiveRefresh(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "database-list-live-refresh";
    public string Title => "Database list, live on every instance";
    public string Category => "Multi-user & games";
    public void RenderDemo(IView view) => Render((UIView)view);

    public static DatabaseListLiveRefresh WithSampleNotes(IApp<SessionIdentity, ClientParameters> app)
    {
        var demo = new DatabaseListLiveRefresh(app);
        demo._notes.Value =
        [
            new ClubNote(2, "Next meeting moved to Thursday at 18:00", new DateTime(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc)),
            new ClubNote(1, "Chapter 4 for next time", new DateTime(2026, 11, 9, 17, 5, 0, DateTimeKind.Utc)),
        ];
        app.OnStarting(demo.StartAsync);
        return demo;
    }

    private async Task StartAsync()
    {
        if (SnapshotCapture.IsCaptureProcess)
        {
            return;
        }

        try
        {
            await StartNotesAsync();
        }
        catch (Exception ex)
        {
            // The sample notes still draw; only posting needs the table
            Log.Instance.Warning($"Creating the database-list-live-refresh demo's club_notes table failed, so its sample notes show and posting fails: {ex.Message}");
        }
    }

    #region example:pattern-database-list-live-refresh
    private sealed record ClubNote(long Id, string Text, DateTime CreatedAt);

    // This instance's copy of the rows, which every client of the instance draws
    private readonly Reactive<IReadOnlyList<ClubNote>> _notes = new([]);

    // One number shared by every instance. Each write adds 1 after its row is stored.
    // A field, so it exists when the instance is built: a global reactive created later is not shared.
    private readonly GlobalReactive<long> _notesRevision = new(0, key: "club-notes-revision");

    private readonly ClientReactive<string> _draft = new("");

    private ReactiveEffect? _reloadNotes;

    /// <summary>Call from app.OnStarting.</summary>
    private async Task StartNotesAsync()
    {
        await using (var db = await app.DatabaseAsync())
        {
            await db.OpenAsync();
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS club_notes (
                    id bigserial PRIMARY KEY, text text NOT NULL, created_at timestamptz NOT NULL DEFAULT now())
                """);
        }

        // One effect for the instance, not one per client: it loads once now, then again each time the
        // revision changes, and every client re-renders from _notes
        _reloadNotes = new ReactiveEffect(async cancellationToken =>
        {
            await using var db = await app.DatabaseAsync();
            await db.OpenAsync(cancellationToken);
            var rows = await db.QueryAsync<ClubNote>(
                "SELECT id AS Id, text AS Text, created_at AS CreatedAt FROM club_notes ORDER BY created_at DESC LIMIT 100");
            _notes.Value = rows.ToList();
        }, _notesRevision);
    }

    private async Task PostNoteAsync(string text)
    {
        await using (var db = await app.DatabaseAsync())
        {
            await db.OpenAsync();
            await db.ExecuteAsync("INSERT INTO club_notes (text) VALUES (@text)", new { text });
        }

        // Update, not Value + 1: when two instances write at once, both counts are kept
        _notesRevision.Update(revision => revision + 1);
    }

    private void Render(UIView view)
    {
        view.Column(["gap-3 w-full max-w-md"], content: col =>
        {
            col.Row([Layout.Row.Form], content: row =>
            {
                row.TextField(["default", "flex-1"], label: "Note for the club", bind: _draft);
                row.Button([Button.PrimaryMd], text: "Post", disabled: string.IsNullOrWhiteSpace(_draft.Value), onClick: async () =>
                {
                    var text = _draft.Value.Trim();
                    _draft.Value = "";
                    await PostNoteAsync(text);
                });
            });

            foreach (var note in _notes.Value)
            {
                col.Column([Card.Default, "gap-1 p-3"], key: note.Id.ToString(), content: card =>
                {
                    card.Text([Text.Body], note.Text);
                    card.Text([Text.Caption], note.CreatedAt.ToString("d MMM HH:mm"));
                });
            }
        });
    }
    #endregion
}
