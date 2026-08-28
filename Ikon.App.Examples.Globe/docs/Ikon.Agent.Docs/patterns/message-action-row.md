# Message Action Row — Per-Message Actions That Work on Touch

Every messaging surface people have used lets them act on a single message: reply, copy, edit, delete. A chat app without them reads as a prototype, and it is the first thing a user asks for after seeing Version 1.

The row reveals on hover, which is the part that goes wrong: `hover:` never fires on a touch device, so a hover-only action row is *unreachable* on every phone. Pair it with `focus-within:` and `pointer-coarse:` and it works everywhere.

## When to use

Any per-item transcript: chat, comments, a feed, an activity log, an AI conversation. The same shape is right for any list row whose secondary actions should stay out of the way until wanted.

Include only the actions the product really supports and the current user is really allowed. Edit and Delete belong to the author; a shared transcript with an Edit button on someone else's message is a permission bug, not a nicety.

## Snippet

```csharp
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
```

## Notes

- **The three-class reveal is the pattern.** `group-hover:opacity-100` alone ships an app whose message actions are impossible to reach on a phone — and phones are where chat apps get used. `pointer-coarse:opacity-100` shows the row permanently on touch; `group-focus-within:opacity-100` makes it keyboard-reachable.
- `opacity-0` still leaves the row in the layout and in the accessibility tree, so it never shifts content when it appears — and the app validator can find it. Do not use `hidden`.
- Every icon-only button needs `aria-label` through `props`. Unnamed icon buttons are invisible to assistive tech *and* to the validator, which then reports the actions as missing.
- Copy is a declarative `view.ActionButton` with `ActionKind.CopyToClipboard` — there is no `ClientFunctions.CopyToClipboardAsync`.
- Delete opens a confirm, it does not delete. Route `_confirmingDelete` into an `AlertDialog`; see `destructive-confirm-dialog`.
- Distinguish "delete for me" from "delete for everyone" only if the product actually implements both. One Delete that does the truthful thing beats two that pretend.
- Reply should visibly change the composer (a quoted strip above the input the user can dismiss), otherwise setting `_replyingTo` is a dead control.
- Add Forward, Pin or React only where the product supports them. Every extra icon costs the row its calm, and a Pin that pins nowhere is worse than no Pin.
- **Hover is a shortcut, never the only path.** Desktop may reveal the row on hover or focus, but every action in it stays reachable by keyboard, and on touch by long-press or an explicit More. A right-click context menu is the same: an accelerator, not the only door.
- **Selecting a message provides context; it never acts.** Selection may feed an AI turn, but on its own it must not send, delete, forward or otherwise commit anything.
- **A failed send keeps its content.** Text and attachments survive the failure, so Retry means press-again rather than type-it-all-again — and delivery states (Sending / Sent / Delivered / Read / Failed) change colour and glyph, not geometry, or the transcript twitches every time one lands.
- **Presence is shown only where presence data is real.** An Online dot the app cannot substantiate is a fabricated fact, and last-seen is privacy-sensitive besides. For a group, a member count beats implying everyone is present.

## See also

- `overlay-selection` — choosing the confirm shape Delete opens.
- `destructive-confirm-dialog` — the id-driven confirm this row's Delete feeds.
- `role-tagged-transcript-feed` — per-speaker styling for the surrounding transcript.
- `copy-and-share-action-row` — the floating variant for a whole result rather than one message.
