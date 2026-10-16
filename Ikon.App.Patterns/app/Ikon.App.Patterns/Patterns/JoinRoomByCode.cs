namespace Ikon.App.Patterns.Patterns;

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

    // One shared instance holds every room; each client's seat says which room it is in
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
