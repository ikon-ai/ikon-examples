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

// The database-list-live-refresh doc's private-rows example; it draws nothing in the gallery.
internal sealed class DatabaseListPrivateRows(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:pattern-database-list-private-rows
    private sealed record Trip(long Id, string OwnerId, string Destination, DateTime CreatedAt);

    // Written per user with SetFor, so a client reads its own user's trips and no one else's
    private readonly UserReactive<IReadOnlyList<Trip>> _myTrips = new([]);

    // Every trip, written only for staff user ids, in a reactive of its own
    private readonly UserReactive<IReadOnlyList<Trip>> _allTrips = new([]);
    private readonly UserReactive<bool> _isStaff = new(false);

    private readonly GlobalReactive<long> _tripsRevision = new(0, key: "trips-revision");

    // The revision each user's lists were last loaded at. A join and a bump can load one user at the
    // same time, and a load that read an older revision must not overwrite a newer one's rows.
    private readonly UserReactive<long> _loadedRevision = new(-1);
    private readonly Lock _loadLock = new();

    private ReactiveEffect? _reloadTrips;

    private ClientProfiles Profiles { get; } = new(app);

    /// <summary>Call from app.OnStarting.</summary>
    private async Task StartTripsAsync()
    {
        await using (var db = await app.DatabaseAsync())
        {
            await db.OpenAsync();
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS trips (
                    id bigserial PRIMARY KEY, owner_id text NOT NULL, destination text NOT NULL,
                    created_at timestamptz NOT NULL DEFAULT now())
                """);
        }

        // The effect reloads only the users connected when the revision changes, so a person who
        // arrives between bumps is loaded as they join
        app.OnClientJoined(async ctx =>
        {
            var profile = await Profiles.GetProfileAsync(ctx);
            _isStaff.SetFor(ctx.UserId, profile?.HasRole(UserRole.Moderator) == true);
            await LoadTripsAsync([ctx.UserId], _tripsRevision.Value);
        });

        _reloadTrips = new ReactiveEffect(async cancellationToken =>
        {
            var revision = _tripsRevision.Value;
            var userIds = app.Clients.Select(client => client.UserId).Where(id => id.Length > 0).Distinct().ToList();
            await LoadTripsAsync(userIds, revision, cancellationToken);
        }, _tripsRevision);
    }

    // One query loads every listed user's own trips and one the staff list, however many users are
    // connected. The revision is read before the queries, so the rows are at least that new.
    private async Task LoadTripsAsync(IReadOnlyList<string> userIds, long revision, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();

        var owned = (await db.QueryAsync<Trip>("""
            SELECT id AS Id, owner_id AS OwnerId, destination AS Destination, created_at AS CreatedAt
            FROM (SELECT *, row_number() OVER (PARTITION BY owner_id ORDER BY created_at DESC) AS n
                  FROM trips WHERE owner_id = ANY(@userIds)) newest
            WHERE n <= 100 ORDER BY created_at DESC
            """, new { userIds = userIds.ToArray() })).ToLookup(trip => trip.OwnerId);

        var staff = userIds.Where(userId => _isStaff.ValueFor(userId)).ToList();
        IReadOnlyList<Trip> all = staff.Count == 0 ? [] : (await db.QueryAsync<Trip>(
            "SELECT id AS Id, owner_id AS OwnerId, destination AS Destination, created_at AS CreatedAt FROM trips ORDER BY created_at DESC LIMIT 200")).ToList();

        cancellationToken.ThrowIfCancellationRequested();

        lock (_loadLock)
        {
            foreach (var userId in userIds)
            {
                if (_loadedRevision.ValueFor(userId) > revision)
                {
                    continue;
                }

                _loadedRevision.SetFor(userId, revision);
                _myTrips.SetFor(userId, owned[userId].ToList());

                if (staff.Contains(userId))
                {
                    _allTrips.SetFor(userId, all);
                }
            }
        }
    }

    // A staff handler, a [Cron] or an endpoint writes for someone else the same way: store, then bump
    private async Task AddTripAsync(string ownerId, string destination)
    {
        await using (var db = await app.DatabaseAsync())
        {
            await db.OpenAsync();
            await db.ExecuteAsync("INSERT INTO trips (owner_id, destination) VALUES (@ownerId, @destination)", new { ownerId, destination });
        }

        _tripsRevision.Update(revision => revision + 1);
    }

    private void Render(UIView view)
    {
        view.Column(["gap-3 w-full max-w-md"], content: col =>
        {
            col.Text([Text.H3], "My trips");

            foreach (var trip in _myTrips.Value)
            {
                col.Text([Text.Body], key: $"mine-{trip.Id}", text: trip.Destination);
            }

            if (_isStaff.Value)
            {
                col.Text([Text.H3], "All trips");

                foreach (var trip in _allTrips.Value)
                {
                    col.Text([Text.Body], key: $"all-{trip.Id}", text: $"{trip.Destination} ({trip.OwnerId})");
                }
            }
        });
    }
    #endregion
}
