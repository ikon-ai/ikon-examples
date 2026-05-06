<!-- mined-from: BrainrotArena -->
# Destructive Confirm Dialog — Driven by an Id, Not a Flag

Destructive actions (delete, kick, abort) need a confirmation step. The clean shape: store the *target id* in a `ClientReactive<string?>` — the dialog's open state is derived (`id != null`). One reactive does double duty: "do I show the dialog?" AND "what am I confirming?". No flag-and-id pair to keep in sync.

## When to use

Delete/destroy/abort buttons in lists where each row can trigger the dialog. Avoids the bug where `_dialogOpen=true` lingers after the target was already removed.

## Snippet

```csharp
private readonly ClientReactive<string?> _deleteBotId = new(null);

// Trigger from any row
actionRow.Button(
    [Button.GhostSm, "text-[10px] py-0.5 px-1.5 text-rose-300/80"],
    onClick: () =>
    {
        _deleteBotId.Value = bot.Id;
        return Task.CompletedTask;
    },
    content: v => v.Text(text: "×"));

// Render the dialog — its open state is derived from the id
private void RenderDeleteBotDialog(UIView view)
{
    var deleteId = _deleteBotId.Value;
    var bot = deleteId != null ? _bots.Value.FirstOrDefault(b => b.Id == deleteId) : null;
    var isOpen = bot != null;

    view.Dialog(
        open: isOpen,
        modal: true,
        onOpenChange: async open =>
        {
            if (open != true) _deleteBotId.Value = null;
        },
        overlayStyle: [Dialog.Overlay],
        contentStyle: [Dialog.Content, "max-w-md w-full rounded-2xl p-6 gap-3 flex flex-col bg-slate-950 border border-rose-300/30"],
        content: dlg =>
        {
            if (bot == null) return;

            dlg.Text([Text.H3, "text-slate-100"], $"Delete {bot.Avatar} {bot.Name}?");
            dlg.Text(["text-sm text-slate-400"],
                $"All {bot.History.Count} saved version(s) will be removed. This can't be undone.");

            dlg.Row(["justify-end gap-2 mt-2"], content: r =>
            {
                r.Button(
                    [Button.OutlineSm],
                    onClick: () => { _deleteBotId.Value = null; return Task.CompletedTask; },
                    content: v => v.Text(text: "Cancel"));

                r.Button(
                    [Button.PrimarySm, "bg-rose-500 hover:bg-rose-400 border-rose-400"],
                    onClick: () => DeleteBotAsync(bot.Id),
                    content: v => v.Text(text: "Delete"));
            });
        });
}
```

## Notes

- Look up the entity inside the render — this way if the entity is gone, `bot == null` ⇒ dialog hidden naturally.
- The destructive action button uses `bg-rose-500 hover:bg-rose-400` overlaid on `Button.PrimarySm` — semantic color in danger position.
- Always reset the id in `onOpenChange` for ESC/backdrop dismissal.
- Prefer `ClientReactive` (per-client) so two users clicking delete on different rows don't interfere.

## See also

- `busy-flag-loading` — for destructive actions that take time, gate the button on a separate busy flag
