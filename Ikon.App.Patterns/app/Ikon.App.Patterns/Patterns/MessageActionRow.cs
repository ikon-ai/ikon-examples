namespace Ikon.App.Patterns.Patterns;

// Pattern: message-action-row — see docs/patterns/message-action-row.md.
// The docsnippet region is one transcript row plus its hover/touch-revealed action row; the stubs
// outside it stand in for the message model, the transcript, the signed-in user and the edit flow.
internal sealed class MessageActionRow : IPatternDemo
{
    public string Slug => "message-action-row";
    public string Title => "Message action row";
    public string Category => "Chat";

    public void RenderDemo(IView view)
    {
        foreach (var message in _messages)
        {
            RenderMessage(view, message);
        }
    }

    private sealed record ChatMessage(string Id, string AuthorId, string Text, DateTime SentAt);

    private readonly ReactiveList<ChatMessage> _messages = new();

    public MessageActionRow()
    {
        _messages.AddRange(
        [
            new ChatMessage("1", "them", "Shipping the retro board today.", DateTime.UtcNow.AddMinutes(-9)),
            new ChatMessage("2", "me", "Nice — I'll take the column drag.", DateTime.UtcNow.AddMinutes(-4)),
        ]);
    }

    private static string CurrentUserId => "me";

    private void BeginEdit(string messageId) => _editing.Value = messageId;

    #region docsnippet:pattern-message-action-row
    private readonly UserReactive<string> _replyingTo = new("");
    private readonly Reactive<string?> _editing = new(null);
    private readonly Reactive<string?> _confirmingDelete = new(null);

    private void RenderMessage(IView view, ChatMessage message)
    {
        var mine = message.AuthorId == CurrentUserId;

        // `group` on the row is what the children's `group-hover:` reads.
        view.Row(["group relative flex gap-3 rounded-lg px-3 py-2 hover:bg-muted/50"], content: view =>
        {
            view.Column([Layout.Column.Xs, "flex-1 min-w-0"], content: v =>
            {
                v.Text([Text.Caption], text: message.SentAt.ToLocalTime().ToString("HH:mm"));
                v.Text([Text.Body, "whitespace-pre-wrap break-words"], text: message.Text);
            });

            // Hidden until hovered on a mouse — but ALWAYS visible on touch (pointer-coarse:) and
            // reachable by keyboard (focus-within:). Without those two, this row does not exist
            // on a phone.
            view.Row([
                "absolute right-2 top-1 items-center gap-0.5 rounded-md border border-secondary bg-card p-0.5 shadow-sm",
                "opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 pointer-coarse:opacity-100",
            ], content: v =>
            {
                RenderAction(v, "reply", "Reply", async () => _replyingTo.Value = message.Id);

                v.ActionButton([Button.GhostMd, Button.IconSm],
                    action: ActionKind.CopyToClipboard,
                    options: new CopyToClipboardActionOptions { Text = message.Text },
                    props: new Dictionary<string, object> { ["aria-label"] = "Copy message" },
                    content: inner => inner.Icon([Icon.Sm], name: "copy"));

                // Author-only actions. Rendering these on someone else's message is a permission
                // bug — omit them, never disable-and-hope.
                if (mine)
                {
                    RenderAction(v, "pencil", "Edit", async () => BeginEdit(message.Id));
                    RenderAction(v, "trash-2", "Delete", async () => _confirmingDelete.Value = message.Id);
                }
            });
        });
    }

    private static void RenderAction(IView view, string icon, string label, Func<Task> onClick)
    {
        view.Button([Button.GhostMd, Button.IconSm], onClick: onClick,
            props: new Dictionary<string, object> { ["aria-label"] = label },
            content: v => v.Icon([Icon.Sm], name: icon));
    }
    #endregion
}
