namespace Ikon.App.Patterns.Patterns;

// Pattern: destructive-confirm-dialog — see docs/patterns/destructive-confirm-dialog.md.
// The stubs outside the region stand in for the bot list, the row a trigger fires from, and the
// delete operation so the id-driven dialog body the doc extracts compiles on its own.
internal sealed class DestructiveConfirmDialog : IPatternDemo
{
    public string Slug => "destructive-confirm-dialog";
    public string Title => "Destructive confirm dialog";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => RenderDeleteBotDialog(view);

    private sealed record Bot(string Id, string Name, string Avatar, IReadOnlyList<string> History);
    private readonly ReactiveList<Bot> _bots = new();
    private readonly IView actionRow = null!;
    private readonly Bot bot = null!;
    private Task DeleteBotAsync(string botId) => throw new NotImplementedException();

    #region docsnippet:pattern-destructive-confirm-dialog
    private readonly ClientReactive<string?> _deleteBotId = new(null);

    // Trigger from any row
    private void RenderDeleteTrigger(IView view)
    {
        actionRow.Button(
            [Button.GhostSm, "text-[10px] py-0.5 px-1.5 text-rose-300/80"],
            onClick: () =>
            {
                _deleteBotId.Value = bot.Id;
                return Task.CompletedTask;
            },
            content: v => v.Text(text: "×"));
    }

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
    #endregion
}
