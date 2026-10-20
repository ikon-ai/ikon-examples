namespace Ikon.App.Patterns.Patterns;

#region example:pattern-shared-text-editor-extension
// One change to the text: remove `Remove` characters at `Start`, then insert `Insert` there.
// Offsets count UTF-16 units, which is what a JavaScript string counts too.
public sealed record TextEdit(string Id, int BaseRevision, int Start, int Remove, string Insert);

public static class TextEdits
{
    // Moves an edit made against some text over an `earlier` one made against the same text, so it
    // lands on the same characters. The same function is in text-edits.ts, and the two must agree.
    public static TextEdit Transform(TextEdit edit, TextEdit earlier)
    {
        var shift = earlier.Insert.Length - earlier.Remove;

        if (edit.Start >= earlier.Start + earlier.Remove)
        {
            return edit with { Start = edit.Start + shift };
        }

        if (edit.Start + edit.Remove <= earlier.Start)
        {
            return edit;
        }

        // The two changes overlap: the earlier one wins what it replaced, and the new text is kept after it
        return edit with { Start = earlier.Start + earlier.Insert.Length, Remove = 0 };
    }
}

public static class SharedTextExtensions
{
    public static void SharedText(this UIView view, string text, int revision, IReadOnlyList<string> appliedIds,
        Func<TextEdit, Task> onEdit, string[]? style = null, string? key = null)
    {
        view.AddNode("custom.shared-text", new Dictionary<string, object?>
        {
            ["text"] = text,
            ["revision"] = revision,
            ["applied"] = appliedIds.ToList(),
            // ikon browse cannot read the editor, so observe prints this line for it
            ["content"] = $"Shared text editor, revision {revision}: {text}",
            ["onEditId"] = view.CreateAction<TextEdit>(args => onEdit(args.Value)),
        }, style: style, key: key);
    }
}
#endregion

// Pattern: shared-text-editor — see docs/patterns/shared-text-editor.md.
// The demo shows its note: the custom.shared-text node needs a React component this gallery's
// frontend does not register.
internal sealed class SharedTextEditor : IPatternDemo
{
    public string Slug => "shared-text-editor";
    public string Title => "Shared text editor";
    public string Category => "Custom frontend";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "One document that everyone who opens the app edits at once: the text is reactive state in C#, a custom textarea sends each change as a small splice, and the server orders the splices. See docs/patterns/shared-text-editor.md.");

    #region example:pattern-shared-text-editor
    // Reactive<T> is shared by every client of the instance, so the app uses SessionIdentity()
    private readonly Reactive<string> _document = new("Meeting notes\n");
    private readonly Reactive<int> _revision = new(0);
    private readonly Reactive<string[]> _appliedIds = new([]);

    // The recent edits in the order the server applied them. The edit at index n moved the document to
    // revision _historyOffset + n + 1; older ones are dropped so the list does not grow for ever.
    private readonly List<TextEdit> _history = [];
    private int _historyOffset;
    private readonly object _historyLock = new();

    private Task ApplyEditAsync(TextEdit edit)
    {
        lock (_historyLock)
        {
            // An edit made against a revision older than the kept history cannot be moved. Its sender
            // hears nothing, gives up after a few seconds and takes the server's text
            if (edit.BaseRevision < _historyOffset)
            {
                return Task.CompletedTask;
            }

            // The sender had revision BaseRevision, so move its edit over everything applied since
            var current = edit;

            foreach (var earlier in _history.Skip(Math.Clamp(edit.BaseRevision - _historyOffset, 0, _history.Count)))
            {
                current = TextEdits.Transform(current, earlier);
            }

            var text = _document.Value;
            var start = Math.Clamp(current.Start, 0, text.Length);
            var remove = Math.Clamp(current.Remove, 0, text.Length - start);

            _history.Add(current with { Start = start, Remove = remove });
            _document.Value = string.Concat(text.AsSpan(0, start), current.Insert, text.AsSpan(start + remove));

            if (_history.Count > 1000)
            {
                _history.RemoveRange(0, 500);
                _historyOffset += 500;
            }

            _revision.Value = _historyOffset + _history.Count;

            // The sender recognises its own edit by its id, and a few ids stay in case updates are merged
            _appliedIds.Value = [.. _appliedIds.Value.Append(edit.Id).TakeLast(32)];
        }

        return Task.CompletedTask;
    }

    private void RenderEditor(UIView view)
    {
        view.Column([Card.Default, "p-4 gap-3 max-w-3xl"], content: view =>
        {
            view.Text([Text.H3], "Shared notes");
            view.SharedText(_document.Value, _revision.Value, _appliedIds.Value, ApplyEditAsync,
                style: ["h-80 w-full rounded-lg border border-border bg-background p-3 font-mono"]);
            view.Text([Text.Caption], $"Revision {_revision.Value}. Everyone who opens this page edits the same text.");
        });
    }
    #endregion
}
