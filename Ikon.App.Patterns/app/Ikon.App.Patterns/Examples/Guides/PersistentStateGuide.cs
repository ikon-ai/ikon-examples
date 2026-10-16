namespace Ikon.App.Patterns.Protocol;

file sealed class PersistentStateExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record MyState(string Title = "");

    private sealed record Prefs(bool DarkMode = false);

    private sealed record Camera(string Id);

    private sealed record Recipe(string Name, string[] Ingredients);

    #region example:persistent-default
    // Preferences, drafts and small app state:
    private readonly PersistentSessionReactive<MyState> _state = new(new MyState());
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
    private readonly PersistentSessionReactive<Prefs> _prefs = new(new Prefs());

    // byte[] payloads stay on asset storage automatically — no backend parameter needed
    private readonly PersistentSessionReactive<byte[]> _snapshot = new([]);

    // Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
    // not the raw bytes, so it is no image src; serve media through app.Files.Public
    private readonly PersistentSessionReactive<byte[]> _logo
        = new([], backend: PersistenceBackend.Public);

    // Explicitly target a postgres DB of the app's own
    private readonly PersistentSessionReactive<long> _counter
        = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
    #endregion

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
            var baseline = new PersistentSessionReactive<byte[]>([]);  // id is the loop position
        }

        // RIGHT — explicit stable key derived from the dynamic identity.
        foreach (var camera in cameras)
        {
            var baseline = new PersistentSessionReactive<byte[]>(
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
