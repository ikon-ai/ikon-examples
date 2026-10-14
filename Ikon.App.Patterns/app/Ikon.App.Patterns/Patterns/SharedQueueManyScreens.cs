namespace Ikon.App.Patterns.Patterns;

// Pattern: shared-queue-many-screens — see docs/patterns/shared-queue-many-screens.md.
// The demo draws the three screens side by side from a seeded queue: in an app each device opens
// its own path, and Render picks the screen from it. The staff screen's button calls CallNext,
// which does nothing here, because the gallery's client holds no staff role.
internal sealed class SharedQueueManyScreens(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "shared-queue-many-screens";
    public string Title => "One queue, three screens";
    public string Category => "Multi-user & games";

    private Audio Audio { get; } = new(app);

    public static SharedQueueManyScreens WithSampleQueue(IApp<SessionIdentity, ClientParameters> app)
    {
        var demo = new SharedQueueManyScreens(app);
        demo._queue.AddRange([new Ticket(1, "Aino", Called: true), new Ticket(2, "Bruno", Called: false), new Ticket(3, "Chidi", Called: false)]);
        demo._lastNumber = 3;
        return demo;
    }

    public void RenderDemo(IView view)
    {
        var ui = (UIView)view;
        ui.Row(["gap-4 flex-wrap items-start"], content: row =>
        {
            RenderKiosk(row);
            RenderStaff(row);
            RenderDisplay(row);
        });
    }

    #region example:pattern-shared-queue-many-screens
    private enum Screen { Kiosk, Staff, Display }

    private sealed record Ticket(int Number, string Name, bool Called);

    // With SessionIdentity() declared without fields, every kiosk, staff device and waiting-room
    // display joins the same instance, and this one list is the queue all of them show.
    private readonly ReactiveList<Ticket> _queue = new();
    private int _lastNumber;

    // Each device opens its own path (/, /staff, /display), so the screen is a per-client value,
    // decided on the client's first frame.
    private readonly ClientReactive<Screen> _screen = ClientReactive.Create(_ => app.Navigation.CurrentPath?.Trim('/') switch
    {
        "staff" => Screen.Staff,
        "display" => Screen.Display,
        _ => Screen.Kiosk,
    });

    private readonly ClientReactive<bool> _isStaff = new(false);
    private readonly ClientReactive<bool> _displayStarted = new(false);
    private readonly ClientReactive<string> _name = new("");
    private readonly ClientReactive<int?> _myNumber = new((int?)null);

    private ClientProfiles Profiles { get; } = new(app);

    /// <summary>Call once from Main.</summary>
    private void WireStaffRole()
    {
        // A join handler runs in the joining client's scope, so this sets the flag for that device.
        // A device sees a role given later only after it reconnects.
        app.OnClientJoined(async ctx =>
        {
            var profile = await Profiles.GetProfileAsync(ctx);
            _isStaff.Value = profile?.HasRole("staff") == true;
        });
    }

    private void CheckIn()
    {
        var name = _name.Value.Trim();

        if (name.Length == 0)
        {
            return;
        }

        var ticket = new Ticket(Interlocked.Increment(ref _lastNumber), name, Called: false);
        _queue.Add(ticket);
        _myNumber.Value = ticket.Number;
        _name.Value = "";
    }

    private void CallNext()
    {
        // Only staff see the button, but this check is the one that counts: a handler can be
        // reached without the button
        if (!_isStaff.Value)
        {
            return;
        }

        // The ticket is picked inside the update, which runs under the list's lock, so two staff
        // pressing at once call two tickets. The called ticket replaces the one called before it,
        // so the list holds the waiting tickets and the number the displays show.
        Ticket? next = null;
        _queue.Update(list =>
        {
            next = list.FirstOrDefault(ticket => !ticket.Called);
            return next is { } picked
                ? list.Where(ticket => !ticket.Called).Select(ticket => ticket.Number == picked.Number ? ticket with { Called = true } : ticket)
                : list;
        });

        if (next is null)
        {
            return;
        }

        // Only the displays speak; the kiosk and the staff devices stay quiet
        var displays = app.Clients.Ids.Where(id => _screen.ValueFor(id) == Screen.Display).ToList();
        Audio.Speak(MediaTargets.To(displays), $"Number {next.Number}, please come to the desk");
    }

    private void Render(UIView view)
    {
        switch (_screen.Value)
        {
            case Screen.Staff when !_isStaff.Value:
                view.Text([Text.Body], "This screen is for staff. Sign in with a staff account.");
                break;
            case Screen.Staff:
                RenderStaff(view);
                break;
            case Screen.Display:
                RenderDisplay(view);
                break;
            default:
                RenderKiosk(view);
                break;
        }
    }

    private void RenderKiosk(UIView view)
    {
        view.Column([Card.Default, "gap-3 w-72"], content: col =>
        {
            if (_myNumber.Value is { } number)
            {
                col.Text([Text.H2], $"Your number is {number}");
                col.Button([Button.OutlineMd], text: "Next person", onClick: async () => _myNumber.Value = null);
                return;
            }

            col.TextField(label: "Your name", bind: _name);
            col.Button([Button.PrimaryMd], text: "Check in", onClick: async () => CheckIn());
        });
    }

    private void RenderStaff(UIView view)
    {
        var waiting = _queue.Where(ticket => !ticket.Called).ToList();

        view.Column([Card.Default, "gap-3 w-72"], content: col =>
        {
            col.Text([Text.H3], $"{waiting.Count} waiting");

            foreach (var ticket in waiting)
            {
                col.Text([Text.Body], $"{ticket.Number} · {ticket.Name}", key: $"ticket-{ticket.Number}");
            }

            col.Button([Button.PrimaryMd], text: "Call next", disabled: waiting.Count == 0, onClick: async () => CallNext());
        });
    }

    private void RenderDisplay(UIView view)
    {
        // Numbers only: the waiting room never sees a name
        var serving = _queue.FirstOrDefault(ticket => ticket.Called);

        view.Column([Card.Default, "items-center gap-2 w-72"], content: col =>
        {
            col.Text([Text.Caption], "Now serving");
            col.Text([Text.Display], serving?.Number.ToString() ?? "–");

            // A browser plays sound only after someone has tapped the page, so the display is
            // started with one tap when it is set up
            if (!_displayStarted.Value)
            {
                col.Button([Button.OutlineMd], text: "Start display", onClick: async () => _displayStarted.Value = true);
            }
        });
    }
    #endregion
}
