namespace Ikon.App.Patterns.Patterns;

// Pattern: qr-check-in — see docs/patterns/qr-check-in.md.
// Create wires the join handler into the Patterns app and adds a sample shift, so opening the
// link under its QR code checks that client in; the demo's button does the same for this client
// without a second device.
internal sealed class QrCheckIn(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "qr-check-in";
    public string Title => "QR check-in at the door";
    public string Category => "Accounts";

    public static QrCheckIn Create(IApp<SessionIdentity, ClientParameters> app)
    {
        var demo = new QrCheckIn(app);
        demo.AddShift("Food bank, evening shift", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3));
        app.OnClientJoined(async (ctx, parameters) => await demo.CheckInFromLinkAsync(ctx, parameters.Checkin));
        return demo;
    }

    public void RenderDemo(IView view)
    {
        RenderCheckIn((UIView)view);

        if (_shifts.FirstOrDefault() is { } shift)
        {
            view.Button([Button.OutlineMd, "self-start mt-4"], text: "Check in on this device", onClick: async () =>
            {
                if (app.CurrentClientContext is { } ctx)
                {
                    await CheckInFromLinkAsync(ctx, shift.CheckInCode);
                }
            });
        }
    }

    #region example:pattern-qr-check-in
    private sealed record Shift(string Id, string Title, DateTimeOffset Starts, DateTimeOffset Ends, string CheckInCode);

    private sealed record CheckIn(string ShiftId, string UserId, string Name, DateTimeOffset At);

    private ClientProfiles Profiles { get; } = new(app);

    // A real app keeps shifts and check-ins in its database
    private readonly ReactiveList<Shift> _shifts = new();
    private readonly ReactiveList<CheckIn> _checkIns = new();

    private readonly ClientReactive<string?> _checkInResult = new(null);
    private readonly ClientReactive<bool> _signInToCheckIn = new(false);

    private static readonly TimeSpan OpensBeforeStart = TimeSpan.FromMinutes(30);

    // Each shift gets a code no one can guess, so the QR at the door is the way in
    private void AddShift(string title, DateTimeOffset starts, DateTimeOffset ends) =>
        _shifts.Add(new Shift(Guid.NewGuid().ToString("N"), title, starts, ends, Guid.NewGuid().ToString("N")[..10]));

    // Call once from Main:
    // app.OnClientJoined(async (ctx, parameters) => await CheckInFromLinkAsync(ctx, parameters.Checkin));
    private async Task CheckInFromLinkAsync(Context ctx, string? code)
    {
        // The deploy's snapshot client opens the app too
        if (string.IsNullOrEmpty(code) || ctx.IsSnapshot)
        {
            return;
        }

        // The join handler runs in the joining client's scope, so these are that phone's values
        if (ctx.IsAnonymous)
        {
            _signInToCheckIn.Value = true;
            return;
        }

        _signInToCheckIn.Value = false;
        var shift = _shifts.FirstOrDefault(s => s.CheckInCode == code);
        var now = DateTimeOffset.UtcNow;

        if (shift is null)
        {
            _checkInResult.Value = "This check-in code isn't valid. Ask the coordinator at the door.";
            return;
        }

        if (now < shift.Starts - OpensBeforeStart)
        {
            _checkInResult.Value = $"Check-in for {shift.Title} opens 30 minutes before it starts.";
            return;
        }

        if (now > shift.Ends)
        {
            _checkInResult.Value = $"{shift.Title} has ended.";
            return;
        }

        // A reload keeps ?checkin= in the address and joins again, so a check-in is recorded once
        if (!_checkIns.Any(c => c.ShiftId == shift.Id && c.UserId == ctx.UserId))
        {
            var profile = await Profiles.GetProfileAsync(ctx);
            var name = profile?.VisibleName is { Length: > 0 } visible ? visible : "A volunteer";
            _checkIns.Add(new CheckIn(shift.Id, ctx.UserId, name, now));
        }

        _checkInResult.Value = $"You're checked in for {shift.Title}. Thank you!";
    }

    private void RenderCheckIn(UIView view)
    {
        view.Column(["gap-4 w-full max-w-xl"], content: col =>
        {
            if (_signInToCheckIn.Value)
            {
                // The sign-in keeps the page's address, ?checkin= included, so the join after it checks in
                col.Button([Button.PrimaryMd, "self-start"], text: "Sign in to check in",
                    onClick: async () => await ClientFunctions.LoginShowAsync("Sign in to check in for your shift"));
            }

            if (_checkInResult.Value is { } result)
            {
                col.Text([Text.Body, "font-medium"], result);
            }

            foreach (var shift in _shifts)
            {
                col.Column([Card.Default, "gap-3 p-4"], key: shift.Id, content: card =>
                {
                    card.Text([Text.H3], shift.Title);

                    // The coordinator prints this or shows it on a tablet by the door. A phone's own
                    // camera opens the link; the volunteer needs no scanner in the app.
                    var link = app.JoinUrl(new { checkin = shift.CheckInCode });
                    card.QR(["w-48 h-48 bg-white p-2 rounded-lg"], value: link);
                    card.Text([Text.Caption, "break-all"], link);

                    var arrived = _checkIns.Where(c => c.ShiftId == shift.Id).ToList();
                    card.Text([Text.Caption], $"{Plural.Of(arrived.Count, "volunteer")} checked in");

                    foreach (var checkIn in arrived)
                    {
                        card.Text([Text.Body], checkIn.Name, key: checkIn.UserId);
                    }
                });
            }
        });
    }
    #endregion
}
