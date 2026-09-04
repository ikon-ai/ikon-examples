<!-- mined-from: Ikon.App.Patterns -->
# Composer Input Bar — The Chat Input You Do Not Rebuild

`view.Composer` is a complete input bar: attach button, drag-and-drop, paste, auto-growing text,
attachment chips, send, and an optional push-to-talk mic. Apps rebuild this out of a `TextField`
and a `Button` and lose most of it — the paste handling, the drop target, the row growth, the chip
removal.

It is **stateless**. It renders the draft and the chips it is handed, and reports what changed;
storing both is the app's job. That is also why the attachment list and whatever the app keeps
alongside it must stay index-aligned: `onAttachmentRemoved` hands back an index into the list the
app passed.

## When to use

Any chat, comment box, or prompt input — anywhere a person types a message and may attach
something. For a single-line search or filter field, `TextField` is the right size.

## Notes

- **Bind the draft to USER scope.** A reload mints a new client session, so a half-typed message in
  a `ClientReactive` vanishes with it. `UserReactive<string>` survives reloads and follows the user
  across tabs.
- **Prefer `onSubmit`'s text over re-reading the draft** — a surface switch can clear the draft
  between the keystroke and the handler.
- `busy:` disables send while work is in flight; pair it with the same flag that guards the send
  itself.
- `accept:` is a **string array** (`["image/*", ".pdf"]`), never the HTML attribute string.
- The mic renders only when **both** capture callbacks (`onCaptureStart`, `onCaptureStop`) are
  wired. Transcription is the app's job — see `voice-loop`.
- `onAttachmentAdded` gives `FileUploadCompleteArgs`, so the destination follows the ordinary
  upload rules: return an `AssetUri` from an upload start to stream into asset storage, or take
  `LocalTempFilePath` for something consumed once. Exactly one of the two is non-null.
- `onAttachmentError` carries `FileName` and `ErrorMessage`. A failed upload that is not surfaced
  leaves a chip the send will not carry.
- Every slot has a style parameter (`fieldStyle`, `chipStyle`, `sendButtonStyle`, `micStyle`, …)
  and every string a label parameter, so restyling and localizing need no fork.

## Snippet

```csharp
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
```

## See also

- `file-upload-with-progress` — the upload path behind the attachments.
- `message-action-row` — per-message actions on the transcript above it.
