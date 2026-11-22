namespace Ikon.App.Platform.Patterns.Patterns;

// Inside Ikon.App.*, `Reactive` would mean the Ikon.App.Reactive namespace. An app's code is in the
// global namespace, where `Reactive` means the class the doc uses
using Reactive = Ikon.Common.Core.Reactive.Reactive;

// Pattern: loader-backed-list — see docs/patterns/loader-backed-list.md.
// The example region below is the canonical body the doc extracts; the in-memory store outside it
// stands in for the app's database, with a short delay so the loading and previous states show.
internal sealed class LoaderBackedList : IPatternDemo
{
    public string Slug => "loader-backed-list";
    public string Title => "Loader-backed list";
    public string Category => "State";
    public void RenderDemo(IView view) => Render((UIView)view);

    private readonly NoteStore _store = new();

    public sealed record Note(Guid Id, string Text);

    private sealed class NoteStore
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<string, List<Note>> _folders = new()
        {
            ["Inbox"] = [new Note(Guid.NewGuid(), "Call the venue about Friday")],
            ["Archive"] = [new Note(Guid.NewGuid(), "Book the photographer")],
        };

        public async Task<IReadOnlyList<Note>> ListAsync(string folder, CancellationToken ct)
        {
            await Task.Delay(400, ct);

            lock (_lock)
            {
                return _folders.TryGetValue(folder, out var notes) ? [.. notes] : [];
            }
        }

        public async Task AddAsync(string folder, Note note)
        {
            await Task.Delay(400);

            lock (_lock)
            {
                if (!_folders.TryGetValue(folder, out var notes))
                {
                    _folders[folder] = notes = [];
                }

                notes.Add(note);
            }
        }

        public async Task DeleteAsync(string folder, Guid id)
        {
            await Task.Delay(400);

            lock (_lock)
            {
                _folders.GetValueOrDefault(folder)?.RemoveAll(n => n.Id == id);
            }
        }
    }

    #region example:pattern-loader-backed-list
    private static readonly string[] Folders = ["Inbox", "Archive"];

    private readonly ClientReactive<string> _folder = new("Inbox");
    private readonly ClientReactive<string> _draft = new("");
    private readonly ClientReactive<bool> _deleting = new(false);
    private readonly ClientReactive<string?> _error = new(null);

    // The load calls an instance member, so the loader cannot be a field initializer (CS0236).
    // Reactive.Once builds it on its first read, because `??=` would let two renders each build one (IKON060).
    // RefreshInterval reloads the folders a screen shows, so a note another user or a job adds appears.
    private Loader<string, IReadOnlyList<Note>>? _notes;

    private Loader<string, IReadOnlyList<Note>> Notes => Reactive.Once(ref _notes, () => new(
        (folder, ct) => _store.ListAsync(folder, ct),
        new LoaderOptions { RefreshInterval = TimeSpan.FromSeconds(30) }));

    private async Task AddAsync()
    {
        var note = new Note(Guid.NewGuid(), _draft.Value.Trim());
        var folder = _folder.Value;
        _draft.Value = "";
        _error.Value = null;

        try
        {
            // Shows the note at once. If the save fails, the note is taken out again and the exception is caught here
            await Notes.UpdateAsync(folder, notes => [.. notes, note], () => _store.AddAsync(folder, note));
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Adding note {note.Id} to {folder} failed, the draft is offered again: {ex.Message}");
            _error.Value = "The note was not saved. Try again.";
            _draft.Value = note.Text;
        }
    }

    private async Task DeleteAsync(string folder, Note note)
    {
        // Save first, then change the loaded list, so nothing reloads and the old list does not flash back
        if (await _deleting.RunAsync(_error, () => _store.DeleteAsync(folder, note.Id)))
        {
            Notes.Update(folder, notes => [.. notes.Where(n => n.Id != note.Id)]);
        }
    }

    private void Render(UIView view)
    {
        view.Column([Card.Elevated, "p-5 gap-3"], content: view =>
        {
            view.Row(["gap-2"], content: row =>
            {
                foreach (var folder in Folders)
                {
                    row.Button([folder == _folder.Value ? Button.PrimarySm : Button.OutlineSm], text: folder,
                        onClick: async () => _folder.Value = folder);
                }
            });

            // While another folder's first load runs, the list shown here stays on screen, dimmed
            Loaded<IReadOnlyList<Note>> notes = Notes.GetOrPrevious(_folder.Value, "notes");

            if (notes.Value is null)
            {
                if (notes.IsFailed)
                {
                    view.Alert("Could not load the notes.", tone: SemanticTone.Error);
                    view.Button([Button.OutlineSm], text: "Retry", onClick: async () => Notes.Invalidate(_folder.Value));
                }
                else
                {
                    view.Text([Text.Muted], "Loading notes…");
                }
            }
            else
            {
                view.Column([notes.IsPrevious ? "gap-2 opacity-60" : "gap-2"], content: list =>
                {
                    if (notes.Value.Count == 0)
                    {
                        list.Text([Text.Muted], "No notes in this folder");
                    }

                    foreach (var note in notes.Value)
                    {
                        list.Row(["items-center justify-between gap-2"], content: row =>
                        {
                            row.Text(text: note.Text);
                            row.Button([Button.OutlineErrorSm], text: "Delete", disabled: _deleting.Value,
                                onClick: () => DeleteAsync(_folder.Value, note));
                        });
                    }
                });
            }

            view.Row(["gap-2"], content: row =>
            {
                row.TextField(["default", "flex-1"], label: "New note", bind: _draft);
                row.Button([Button.PrimarySm], text: "Add", disabled: string.IsNullOrWhiteSpace(_draft.Value),
                    onClick: AddAsync);
            });

            if (_error.Value is string error)
            {
                view.Alert(error, tone: SemanticTone.Error);
            }
        });
    }
    #endregion
}
