namespace Ikon.App.Patterns.Patterns;

// Pattern: inline-list-cell-edit — see docs/patterns/inline-list-cell-edit.md.
// The example region renders one editable card per list item; the members outside it stand in for
// the item model, the row actions and the save the fields fire on every keystroke. The demo keeps
// the project in memory and shows when it last saved; Generate stays disabled, because in the app it
// starts a paid video generation. The states start without thumbnails, because the render smoke test
// has no upload handler for the Replace control; a button adds sample ones.
internal sealed class InlineListCellEdit : IPatternDemo
{
    public string Slug => "inline-list-cell-edit";
    public string Title => "Inline list cell edit";
    public string Category => "Data";

    private sealed class CharacterState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public byte[]? ImageData { get; set; }
        public string? ImageMime { get; set; }
    }

    private readonly ReactiveList<CharacterState> _states = new(
    [
        new() { Id = "idle", Name = "Idle breathing" },
        new() { Id = "wave", Name = "Waving hello" },
        new() { Id = "laugh", Name = "Laughing" },
    ]);

    private readonly Reactive<string> _lastSaved = new("Not saved yet");
    private readonly string statusLabel = "Generate loop";
    private bool canGenerate => false;

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-md"], content: col =>
        {
            col.Row(["gap-3 items-center"], content: row =>
            {
                row.Button([Button.OutlineSm], text: "Add sample thumbnails", onClick: async () =>
                {
                    string[][] palette = [["#0EA5E9", "#0C4A6E"], ["#F59E0B", "#78350F"], ["#10B981", "#064E3B"]];

                    for (var i = 0; i < _states.Count; i++)
                    {
                        var state = _states[i];
                        var colors = palette[i % palette.Length];
                        state.ImageData = SampleThumbnail(colors[0], colors[1]);
                        state.ImageMime = "image/svg+xml";
                        _states[i] = state;
                    }
                });
                row.Text(["text-xs text-zinc-400"], _lastSaved.Value);
            });
            Render(col);
        });
    }

    private static byte[] SampleThumbnail(string light, string dark) => System.Text.Encoding.UTF8.GetBytes(
        $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64'><rect width='64' height='64' fill='{dark}'/>" +
        $"<circle cx='32' cy='26' r='12' fill='{light}'/><rect x='14' y='42' width='36' height='22' rx='11' fill='{light}'/></svg>");

    private async Task HandleImageUpload(string stateId, FileUploadCompleteArgs args)
    {
        if (args.LocalTempFilePath is not { } path)
        {
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path);
        var found = false;

        // Looked up by id under the list's lock: a remove clicked meanwhile shifts indices, and a
        // second upload to the same state must not interleave its bytes with this one's mime type.
        _states.Mutate(states =>
        {
            var state = states.Find(s => s.Id == stateId);

            if (state == null)
            {
                return;
            }

            state.ImageData = bytes;
            state.ImageMime = args.MimeType;
            found = true;
        });

        if (!found)
        {
            return;
        }

        await SaveProjectAsync();
    }

    private Task GenerateLoopVideoAsync(string stateId) => Task.CompletedTask;

    private void RemoveState(string stateId)
    {
        _states.RemoveAll(s => s.Id == stateId);
        _ = SaveProjectAsync();
    }

    private Task SaveProjectAsync()
    {
        _lastSaved.Value = $"Saved {_states.Count} states at {DateTime.Now:HH:mm:ss}";
        return Task.CompletedTask;
    }

    #region example:pattern-inline-list-cell-edit
    private void Render(IView view)
    {
        // _states is a ReactiveList<CharacterState>
        for (var i = 0; i < _states.Count; i++)
        {
            var index = i;
            var state = _states[i];
            var stateId = state.Id;
            view.Column([Card.Default, "p-3", Layout.Column.Sm], content: view =>
            {
                // Name field — edits in place, saves on every keystroke (debounced inside SaveProjectAsync)
                view.TextField(
                    [Input.Default, "font-medium"],
                    placeholder: "State name",
                    value: state.Name,
                    onValueChange: async value =>
                    {
                        state.Name = value;
                        _states[index] = state;
                        _ = SaveProjectAsync();
                    });

                // Image upload / thumbnail
                if (state.ImageData != null && state.ImageMime != null)
                {
                    view.Row(["gap-2 items-center"], content: view =>
                    {
                        view.Image(
                            style: ["w-16 h-16 object-cover", Tokens.Radius.Md],
                            data: state.ImageData,
                            mimeType: state.ImageMime,
                            alt: state.Name);

                        view.FileUpload(
                            accept: ["image/*"],
                            multiple: false,
                            maxFileSize: 20_000_000,
                            onUploadComplete: async args => await HandleImageUpload(stateId, args),
                            content: v => v.Text(["text-xs cursor-pointer text-primary underline"], "Replace"));
                    });
                }

                // Action row — generate button uses the captured stateId from the closure
                view.Row(["gap-2"], content: view =>
                {
                    view.Button([Button.SecondaryMd, "flex-1 text-sm"], statusLabel,
                        disabled: !canGenerate,
                        onClick: async () => { _ = GenerateLoopVideoAsync(stateId); });

                    view.Button([Button.GhostMd, Button.Icon, "text-destructive"],
                        onClick: async () => { RemoveState(stateId); },
                        content: v => v.Icon([Icon.Default], name: "trash-2"));
                });
            });
        }
    }
    #endregion
}
