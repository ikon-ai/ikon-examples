namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: join-room-by-code — see docs/patterns/join-room-by-code.md.
// `app` is the App's primary-constructor handle; WireRooms is what the app's Main() calls once.
internal sealed class JoinRoomByCode(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "join-room-by-code";
    public string Title => "Join a room by code";
    public string Category => "Multi-user & games";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-join-room-by-code
    public sealed record Room(string Code, int HostId, bool Started);

    public sealed record Seat(string Code, string Name);

    private const int MaxPlayers = 8;

    // No 0/O or 1/I, so a code read off a screen is typed right
    private const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    // One shared instance holds every room; each client's seat says which room it is in.
    // A room object that holds a Reactive or ReactiveList does not belong in this dictionary: capture
    // refuses it and a hot reload resets every room (keep those in a ConcurrentDictionary by code)
    private readonly ReactiveDictionary<string, Room> _rooms = new();
    private readonly ReactiveDictionary<int, Seat> _seats = new();
    private readonly Lock _joinLock = new();

    private readonly ClientReactive<string> _codeInput = new("");
    private readonly ClientReactive<string> _nameInput = new("");
    private readonly ClientReactive<string?> _joinError = new(null);

    /// <summary>
    /// Called once from Main(). Closing a tab gives up its seat, and closing the host's tab closes the room.
    /// </summary>
    private void WireRooms()
    {
        app.OnClientLeft(async ctx => Leave(ctx.ClientSessionId));
    }

    private void HostRoom(int clientId, string name)
    {
        string code;

        do
        {
            code = string.Concat(Enumerable.Range(0, 4).Select(_ => CodeLetters[Random.Shared.Next(CodeLetters.Length)]));
        }
        while (!_rooms.TryAdd(code, new Room(code, clientId, Started: false)));

        _seats[clientId] = new Seat(code, name);
    }

    private void Join(int clientId, string typedCode, string name)
    {
        var code = typedCode.Trim().ToUpperInvariant();

        // Two people taking the last seat at once must not both get it
        lock (_joinLock)
        {
            var seated = _seats.Values.Where(seat => seat.Code == code).ToList();

            _joinError.Value =
                !_rooms.TryGetValue(code, out var room) ? $"No room has the code {code}." :
                room.Started ? "That game has already started." :
                seated.Count >= MaxPlayers ? "That room is full." :
                seated.Any(seat => string.Equals(seat.Name, name, StringComparison.OrdinalIgnoreCase)) ? $"Someone in that room is already called {name}." :
                null;

            if (_joinError.Value is null)
            {
                _seats[clientId] = new Seat(code, name);
            }
        }
    }

    private void Leave(int clientId)
    {
        if (!_seats.TryGetValue(clientId, out var seat))
        {
            return;
        }

        _seats.Remove(clientId);

        if (_rooms.TryGetValue(seat.Code, out var room) && room.HostId == clientId)
        {
            _rooms.Remove(seat.Code);

            foreach (var player in _seats.Where(entry => entry.Value.Code == seat.Code).Select(entry => entry.Key).ToList())
            {
                _seats.Remove(player);
            }
        }
    }

    private void Render(IView view)
    {
        var me = ReactiveScope.ClientId;

        view.Column([Card.Default, "gap-4 p-4 w-full max-w-md"], content: col =>
        {
            if (_seats.TryGetValue(me, out var seat) && _rooms.TryGetValue(seat.Code, out var room))
            {
                col.Text([Text.Caption], "Room code");
                col.Text([Text.Display, "tracking-widest"], room.Code);

                foreach (var player in _seats.Values.Where(other => other.Code == room.Code))
                {
                    col.Text([Text.Body], key: $"player-{player.Name}", text: player.Name);
                }

                if (room.HostId == me && !room.Started)
                {
                    col.Button([Button.PrimaryMd], text: "Start",
                        onClick: async () => _rooms[room.Code] = room with { Started = true });
                }

                col.Button([Button.OutlineMd], text: "Leave", onClick: async () => Leave(me));
                return;
            }

            col.TextField(label: "Your name", bind: _nameInput, placeholder: "Name the others see");
            var hasName = !string.IsNullOrWhiteSpace(_nameInput.Value);

            col.Button([Button.PrimaryMd], text: "Host a room", disabled: !hasName,
                onClick: async () => HostRoom(me, _nameInput.Value.Trim()));

            col.Separator(["default", "my-2"]);
            col.TextField(label: "Room code", bind: _codeInput, placeholder: "ABCD");

            if (_joinError.Value is { } error)
            {
                col.Alert(error, tone: SemanticTone.Error);
            }

            col.Button([Button.OutlineMd], text: "Join", disabled: !hasName || _codeInput.Value.Trim().Length != 4,
                onClick: async () => Join(me, _codeInput.Value, _nameInput.Value.Trim()));

            if (!hasName)
            {
                col.Text([Text.Caption], "Enter your name to host or join.");
            }
            else if (_codeInput.Value.Trim().Length != 4)
            {
                col.Text([Text.Caption], "Room codes have four characters.");
            }
        });
    }
    #endregion
}

// The join-room-by-code doc's "keep the seat across a reload" variant; it draws nothing of its own.
internal sealed class JoinRoomByCodeAcrossReloads(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:pattern-join-room-by-code-across-reloads
    public sealed record Room(string Code, string HostUserId, bool Started);

    public sealed record Seat(string Code, string Name);

    private static readonly TimeSpan ReloadGrace = TimeSpan.FromSeconds(30);

    private readonly ReactiveDictionary<string, Room> _rooms = new();
    private readonly ReactiveDictionary<string, Seat> _seats = new();
    private readonly Lock _joinLock = new();

    // The latest leave of each seated user with no client left. A grace timer frees the seat only
    // if its leave is still the latest, so the timer of an earlier reload cannot free it.
    private readonly Dictionary<string, long> _awayLeaves = new();
    private long _leaveCount;

    /// <summary>
    /// Called once from Main(). Seats are keyed by user id, which a guest keeps across a reload in
    /// the same browser, so the reloaded page is the same player under a new client id.
    /// </summary>
    private void WireRooms()
    {
        app.OnClientJoined(async ctx =>
        {
            lock (_joinLock)
            {
                _awayLeaves.Remove(ctx.UserId);
            }
        });

        app.OnClientLeft(async ctx =>
        {
            var userId = ctx.UserId;
            long leave;

            lock (_joinLock)
            {
                // The leaving client is already gone from app.Clients, so a client of the same user
                // still there is another tab, or the reloaded page that joined before this leave arrived
                if (!_seats.ContainsKey(userId) || app.Clients.Any(client => client.UserId == userId))
                {
                    return;
                }

                leave = ++_leaveCount;
                _awayLeaves[userId] = leave;
            }

            // A closed page leaves at once, so the leave of a reload usually arrives before the
            // reloaded page joins. The seat is kept for ReloadGrace and freed only if the user has
            // not come back by then.
            Task.Run(async () =>
            {
                await Task.Delay(ReloadGrace, app.StoppingToken);
                FreeIfStillAway(userId, leave);
            }).ObserveInBackground();
        });
    }

    private void FreeIfStillAway(string userId, long leave)
    {
        lock (_joinLock)
        {
            if (!_awayLeaves.TryGetValue(userId, out var latest) || latest != leave)
            {
                return;
            }

            _awayLeaves.Remove(userId);

            if (_seats.TryGetValue(userId, out var seat))
            {
                _seats.Remove(userId);
                CloseIfHost(userId, seat.Code);
            }
        }
    }

    // Under _joinLock. A room ends with its host's seat, and its players lose theirs.
    private void CloseIfHost(string userId, string code)
    {
        if (!_rooms.TryGetValue(code, out var room) || room.HostUserId != userId)
        {
            return;
        }

        _rooms.Remove(code);

        foreach (var player in _seats.Where(entry => entry.Value.Code == code).Select(entry => entry.Key).ToList())
        {
            _seats.Remove(player);
        }
    }

    private string? Join(string userId, string typedCode, string name)
    {
        var code = typedCode.Trim().ToUpperInvariant();

        lock (_joinLock)
        {
            _seats.TryGetValue(userId, out var mine);

            if (mine?.Code == code)
            {
                return null;
            }

            var seated = _seats.Values.Where(seat => seat.Code == code).ToList();
            var error =
                !_rooms.TryGetValue(code, out var room) ? $"No room has the code {code}." :
                room.Started ? "That game has already started." :
                seated.Count >= 8 ? "That room is full." :
                seated.Any(seat => string.Equals(seat.Name, name, StringComparison.OrdinalIgnoreCase)) ? $"Someone in that room is already called {name}." :
                null;

            if (error is null)
            {
                // A host who sits down elsewhere closes the room it hosted, or nobody ever would
                if (mine is not null)
                {
                    CloseIfHost(userId, mine.Code);
                }

                _seats[userId] = new Seat(code, name);
            }

            return error;
        }
    }

    // The render reads the seat by ReactiveScope.UserId, so every tab of the player shows the room
    private Seat? MySeat() => _seats.TryGetValue(ReactiveScope.UserId, out var seat) ? seat : null;
    #endregion
}
