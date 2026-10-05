namespace Ikon.App.Patterns.Patterns;

// Pattern: destructive-confirm-dialog — see docs/patterns/destructive-confirm-dialog.md.
// The seeded bot list and the in-memory delete outside the region stand in for the app's stored
// bots, so the id-driven dialog the doc extracts can be opened, cancelled and confirmed.
internal sealed class DestructiveConfirmDialog : IPatternDemo
{
    public string Slug => "destructive-confirm-dialog";
    public string Title => "Destructive confirm dialog";
    public string Category => "Interaction";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-2 max-w-md"], content: list =>
        {
            foreach (var bot in _bots.Value)
            {
                list.Row(["items-center gap-3 px-3 py-2 rounded-lg bg-slate-900 border border-slate-800"], content: row =>
                {
                    row.Text(["text-lg"], bot.Avatar);
                    row.Column(["flex-1"], content: info =>
                    {
                        info.Text(["text-sm text-slate-100"], bot.Name);
                        info.Text(["text-xs text-slate-500"], $"{bot.History.Count} saved version(s)");
                    });
                    RenderDeleteTrigger(row, bot);
                });
            }

            if (_bots.Value.Count == 0)
            {
                list.Text(["text-sm text-slate-500"], "All bots deleted");
            }

            list.Button([Button.OutlineSm, "self-start"], text: "Restore sample bots",
                onClick: async () =>
                {
                    _bots.Clear();
                    _bots.AddRange(SampleBots);
                });
        });

        RenderDeleteBotDialog(view);
    }

    private sealed record Bot(string Id, string Name, string Avatar, IReadOnlyList<string> History);

    private static readonly Bot[] SampleBots =
    [
        new("bot-sarge", "Sergeant Snark", "🪖", ["v1", "v2", "v3"]),
        new("bot-luna", "Luna the Oracle", "🔮", ["v1"]),
        new("bot-chef", "Chef Brisket", "🍖", ["v1", "v2"]),
    ];

    private readonly ReactiveList<Bot> _bots = new(SampleBots);

    private Task DeleteBotAsync(string botId)
    {
        _bots.RemoveAll(b => b.Id == botId);
        return Task.CompletedTask;
    }

    #region example:pattern-destructive-confirm-dialog
    private readonly ClientReactive<string?> _deleteBotId = new(null);

    // Trigger from any row
    private void RenderDeleteTrigger(IView actionRow, Bot bot)
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
