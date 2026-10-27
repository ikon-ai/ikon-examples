namespace Ikon.App.Patterns.Protocol;

file sealed class PersistentStateExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record MyState(string Title = "");

    private sealed record Prefs(bool DarkMode = false);

    private sealed record Camera(string Id);

    private sealed record Recipe(string Name, string[] Ingredients);

    #region example:persistent-default
    // Small state of this instance, kept across its restarts and deploys:
    private readonly PersistentReactive<MyState> _state = new(new MyState());
    #endregion

    #region example:persistent-starting-values
    // Every new user starts with two example recipes; a returning user sees their own list.
    private readonly PersistentUserReactiveList<Recipe> _recipes = new(
    [
        new Recipe("Scrambled eggs", ["2 eggs", "butter"]),
        new Recipe("Toast", ["bread"]),
    ]);
    #endregion

    #region example:persistent-backends
    // Default — structured state lands in the app's built-in postgres database
    private readonly PersistentReactive<Prefs> _prefs = new(new Prefs());

    // byte[] payloads stay on asset storage automatically — no backend parameter needed
    private readonly PersistentReactive<byte[]> _snapshot = new([]);

    // Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
    // not the raw bytes, so it is no image src; serve media through app.Files.Public
    private readonly PersistentReactive<byte[]> _logo
        = new([], backend: PersistenceBackend.Public);

    // Explicitly target a postgres DB of the app's own
    private readonly PersistentReactive<long> _counter
        = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
    #endregion

    #region example:persistent-global-values
    // One value for every instance of the app, kept across deploys: a banner each room shows.
    private readonly PersistentGlobalReactive<string> _announcement = new("", key: "announcement");

    // One value per user, the same in every instance (every room) the user opens.
    private readonly PersistentGlobalUserReactive<string> _nickname = new("", key: "nickname");
    #endregion

    public string Greeting(string userId) => $"{_announcement.Value} {_nickname.ValueFor(userId)}";

    private void ReadWrite(MyState next)
    {
        #region example:persistent-read-write
        // Read and write like any reactive:
        _state.Value = next;
        var current = _state.Value;
        #endregion

        Log.Instance.Debug($"{current} {_prefs} {_snapshot} {_counter}");
    }

    public void PublicUrl()
    {
        #region example:persistent-public-url
        var url = _logo.PublicUrl;  // null until a value is stored; loaded before Main() once one is
        #endregion

        Log.Instance.Debug($"{url}");
    }

    private void DynamicKeys(IReadOnlyList<Camera> cameras)
    {
        #region example:persistent-dynamic-keys
        // WRONG — without a key, each iteration's stable id follows its position in the loop, not the
        // camera: reorder the cameras or run the method again and every id shifts.
        foreach (var camera in cameras)
        {
            var baseline = new PersistentReactive<byte[]>([]);  // id is the loop position
        }

        // RIGHT — explicit stable key derived from the dynamic identity.
        foreach (var camera in cameras)
        {
            var baseline = new PersistentReactive<byte[]>(
                [],
                key: $"baseline:{camera.Id}");
        }
        #endregion
    }

    public async Task EraseAsync(string userId)
    {
        #region example:persistent-erase-user
        await app.EraseUserStateAsync(userId);
        #endregion
    }
}
