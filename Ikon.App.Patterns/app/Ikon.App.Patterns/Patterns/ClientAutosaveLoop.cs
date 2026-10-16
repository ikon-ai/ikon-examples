namespace Ikon.App.Patterns.Patterns;

// Pattern: client-autosave-loop — see docs/patterns/client-autosave-loop.md.
// The stub outside the region stands in for the app's database write.
internal sealed class ClientAutosaveLoop : IPatternDemo
{
    public ClientAutosaveLoop() => StartAutosave();

    public string Slug => "client-autosave-loop";
    public string Title => "Client autosave";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => Render(view);

    private static Task SaveDraftAsync(string userId, string title, string body, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

    #region example:pattern-client-autosave-loop
    private readonly ClientReactive<string> _draftTitle = new("");
    private readonly ClientReactive<string> _draftBody = new("");
    private readonly ClientReactive<string> _saveStatus = new("");
    private ClientReactiveEffect? _autosave;

    /// <summary>
    /// Called once from Main(). The effect runs per client, in that client's scope, whenever that
    /// client changes one of its dependencies. A change while a run is waiting
    /// cancels it and starts another, so the save happens once, a moment after the last keystroke.
    /// </summary>
    private void StartAutosave()
    {
        _autosave = new ClientReactiveEffect(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);

            if (string.IsNullOrWhiteSpace(_draftTitle.Value))
            {
                _saveStatus.Value = "Not saved yet: the title is required";
                return;
            }

            _saveStatus.Value = "Saving…";
            await SaveDraftAsync(ReactiveScope.UserId, _draftTitle.Value, _draftBody.Value, cancellationToken);
            _saveStatus.Value = $"Saved at {DateTime.UtcNow:HH:mm:ss} UTC";
        }, _draftTitle, _draftBody);
    }

    private void Render(IView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-xl"], content: col =>
        {
            col.TextField(label: "Title", bind: _draftTitle, placeholder: "Workshop name");
            col.TextArea(label: "Notes", bind: _draftBody, placeholder: "What the workshop covers");
            col.Text([Text.Caption], _saveStatus.Value is { Length: > 0 } status ? status : "Changes save automatically.");
        });
    }
    #endregion
}
