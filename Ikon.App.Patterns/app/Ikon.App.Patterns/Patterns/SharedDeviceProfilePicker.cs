namespace Ikon.App.Patterns.Patterns;

// Pattern: shared-device-profile-picker — see docs/patterns/shared-device-profile-picker.md.
// The demo draws the picker over the family's starting children, and the parent area asking for a
// first PIN.
internal sealed class SharedDeviceProfilePicker : IPatternDemo
{
    public string Slug => "shared-device-profile-picker";
    public string Title => "Profile picker on a shared device";
    public string Category => "Accounts";

    public void RenderDemo(IView view) => Render((UIView)view);

    #region example:pattern-shared-device-profile-picker
    private sealed record Child(string Id, string Name, string Emoji);

    // The parent's account is signed in on the tablet, so the family is that user: User-scoped
    // state is the same on every device the parent signs in on. The children are not accounts.
    private readonly PersistentUserReactiveList<Child> _children = new([new Child("c1", "Ella", "🦊"), new Child("c2", "Leo", "🐢")]);
    private readonly PersistentUserReactive<string?> _parentPin = new((string?)null);

    // Per user, so reloading the page does not reset the count
    private readonly UserReactive<int> _wrongPins = new(0);
    private readonly UserReactive<DateTime> _pinLockedUntil = new(DateTime.MinValue);

    // Who is holding the tablet is per device, never per account
    private readonly ClientReactive<string?> _activeChildId = new((string?)null);
    private readonly ClientReactive<DateTime> _parentUnlockedUntil = new(DateTime.MinValue);
    // Per client, so digits typed on the tablet never show on another device of the same account
    private readonly ClientReactive<string> _pinEntry = new("");

    private bool ParentUnlocked => _parentUnlockedUntil.Value > DateTime.UtcNow;

    // Stored as salt:hash, never the PIN itself
    private static string HashPin(string pin, byte[] salt) =>
        Convert.ToHexString(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(pin, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32));

    private void SetPin(string pin)
    {
        _pinEntry.Value = "";

        if (pin.Length < 4)
        {
            return;
        }

        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        _parentPin.Value = $"{Convert.ToHexString(salt)}:{HashPin(pin, salt)}";
        _parentUnlockedUntil.Value = DateTime.UtcNow.AddMinutes(10);
    }

    private void TryUnlock()
    {
        var entered = _pinEntry.Value;
        _pinEntry.Value = "";

        // A four-digit PIN has 10,000 values, so wrong guesses lock the parent area for a while
        if (DateTime.UtcNow < _pinLockedUntil.Value || _parentPin.Value?.Split(':') is not [var salt, var hash])
        {
            return;
        }

        if (HashPin(entered, Convert.FromHexString(salt)) != hash)
        {
            _wrongPins.Value++;

            if (_wrongPins.Value >= 5)
            {
                _wrongPins.Value = 0;
                _pinLockedUntil.Value = DateTime.UtcNow.AddMinutes(5);
            }

            return;
        }

        _wrongPins.Value = 0;
        _parentUnlockedUntil.Value = DateTime.UtcNow.AddMinutes(10);
    }

    private void AddChild(string name, string emoji)
    {
        // Every parent action checks the unlock itself; the area being drawn is not the check
        if (!ParentUnlocked)
        {
            return;
        }

        _children.Add(new Child(Guid.NewGuid().ToString("N"), name, emoji));
    }

    private void Render(UIView view)
    {
        if (_children.FirstOrDefault(child => child.Id == _activeChildId.Value) is { } active)
        {
            view.Row([Layout.Row.SpaceBetween], content: row =>
            {
                row.Text([Text.H2], $"{active.Emoji} {active.Name}");
                row.Button([Button.GhostSm], text: "Switch", onClick: async () => _activeChildId.Value = null);
            });
            return;
        }

        view.Column(["gap-6"], content: col =>
        {
            RenderPicker(col);
            RenderParentArea(col);
        });
    }

    private void RenderPicker(UIView view)
    {
        view.Column(["gap-4 items-center"], content: col =>
        {
            col.Text([Text.H2], "Who's playing?");
            col.Row(["gap-4 flex-wrap justify-center"], content: row =>
            {
                foreach (var child in _children)
                {
                    row.Button([Button.OutlineMd, "h-28 w-28 flex-col gap-1"], key: child.Id,
                        onClick: async () => _activeChildId.Value = child.Id,
                        content: button =>
                        {
                            button.Text(["text-4xl"], child.Emoji);
                            button.Text([Text.Label], child.Name);
                        });
                }
            });
        });
    }

    private void RenderParentArea(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-72 self-center"], content: col =>
        {
            col.Text([Text.H3], "Parents");

            if (_parentPin.Value is null)
            {
                // The parent signed the tablet in, so the parent is the one who sets the PIN first
                col.TextField(type: "password", label: "Choose a parent PIN", bind: _pinEntry, onSubmit: async pin => SetPin(pin));
                return;
            }

            if (!ParentUnlocked)
            {
                col.TextField(type: "password", label: "Parent PIN", bind: _pinEntry, onSubmit: async _ => TryUnlock());
                return;
            }

            col.Button([Button.OutlineMd], text: "Add a child", onClick: async () => AddChild("New child", "🐣"));
            col.Button([Button.GhostSm], text: "Lock", onClick: async () => _parentUnlockedUntil.Value = DateTime.MinValue);
        });
    }
    #endregion
}
