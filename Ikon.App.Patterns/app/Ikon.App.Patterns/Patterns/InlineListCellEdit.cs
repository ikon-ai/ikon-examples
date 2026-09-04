namespace Ikon.App.Patterns.Patterns;

// Pattern: inline-list-cell-edit — see docs/patterns/inline-list-cell-edit.md.
// The docsnippet region renders one editable card per list item; the stubs outside it stand in for
// the item model, the row actions and the debounced save the fields fire on every keystroke.
internal sealed class InlineListCellEdit : IPatternDemo
{
    public string Slug => "inline-list-cell-edit";
    public string Title => "Inline list cell edit";
    public string Category => "Data";
    public void RenderDemo(IView view) => Render(view);

    private sealed class CharacterState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public byte[]? ImageData { get; set; }
        public string? ImageMime { get; set; }
    }

    private readonly ReactiveList<CharacterState> _states = new();
    private readonly string statusLabel = "Generate";
    private bool canGenerate => false;

    private Task HandleImageUpload(string stateId, FileUploadCompleteArgs args) => throw new NotImplementedException();
    private Task GenerateLoopVideoAsync(string stateId) => throw new NotImplementedException();
    private void RemoveState(string stateId) => throw new NotImplementedException();
    private Task SaveProjectAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-inline-list-cell-edit
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
