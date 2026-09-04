namespace Ikon.App.Patterns.Patterns;

// Pattern: composer-input-bar — see docs/patterns/composer-input-bar.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ComposerInputBar : IPatternDemo
{
    public string Slug => "composer-input-bar";
    public string Title => "Composer input bar";
    public string Category => "Conversational AI";
    // Composer embeds a FileUploadZone, which registers upload callbacks against a live app — so
    // the gallery shows a note rather than the component. The region below still compiles and is
    // still the code the doc extracts.
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Input bar whose demo needs app context to render: attach, drag-and-drop, paste, "
        + "auto-growing text and send, with the draft and chips stored by the app. See the source "
        + "and docs/patterns/composer-input-bar.md.");

    private Task SendAsync(string text, IReadOnlyList<string> files) => throw new NotImplementedException();

    #region docsnippet:pattern-composer-input-bar
    // The draft is USER-scoped, not client-scoped: a reload mints a new client session, so a
    // half-typed message in a ClientReactive vanishes with it.
    private readonly UserReactive<string> _draft = new("");
    private readonly ClientReactiveList<ComposerAttachment> _attachments = new();
    private readonly ClientReactiveList<string> _paths = new();
    private readonly ClientReactive<bool> _busy = new(false);

    /// <summary>
    /// Composer is STATELESS: it renders the draft and chips it is handed and reports what
    /// changed. Storing both is the app's job, and is why the two lists stay index-aligned --
    /// onAttachmentRemoved hands back an index into the list the app passed.
    /// </summary>
    private void Render(IView view)
    {
        view.Composer(
            value: _draft.Value,
            placeholder: "Message",
            busy: _busy.Value,
            attachments: _attachments,
            accept: ["image/*", ".pdf"],
            maxRows: 6,
            onValueChange: async text => _draft.Value = text,

            // Prefer the SUBMITTED text over re-reading the draft: a surface switch can clear the
            // draft between the keystroke and the handler.
            onSubmit: async text =>
            {
                if (_busy.Value)
                {
                    return;
                }

                using var _ = _busy.AsToken();
                await SendAsync(text, _paths.ToList());
                _draft.Value = "";
                _attachments.Clear();
                _paths.Clear();
            },

            onAttachmentAdded: async args =>
            {
                if (args.LocalTempFilePath is { } path)
                {
                    _attachments.Add(new ComposerAttachment(Path.GetFileName(path), "application/octet-stream", 0));
                    _paths.Add(path);
                }
            },

            onAttachmentRemoved: async index =>
            {
                _attachments.RemoveAt(index);
                _paths.RemoveAt(index);
            },

            // An upload that fails silently leaves a chip the send will not carry -- say so.
            onAttachmentError: async args =>
                Log.Instance.Warning($"Attachment {args.FileName} failed: {args.ErrorMessage}"));
    }
    #endregion
}
