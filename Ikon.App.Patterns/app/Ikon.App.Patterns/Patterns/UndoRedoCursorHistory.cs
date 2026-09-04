namespace Ikon.App.Patterns.Patterns;

// Pattern: undo-redo-cursor-history — see docs/patterns/undo-redo-cursor-history.md.
// ResolveActiveBot/RedoActiveBotAsync stand in for the app's selection + redo wiring; the docsnippet
// region is the canonical timeline model, save/undo transforms, and cursor-gated action buttons.
internal sealed class UndoRedoCursorHistory : IPatternDemo
{
    public string Slug => "undo-redo-cursor-history";
    public string Title => "Undo-redo cursor history";
    public string Category => "State";
    public void RenderDemo(IView view) => RenderUndoRedoActions(view, _sampleBot);

    private readonly Bot _sampleBot = new(
        Id: "sample", Author: "demo", Name: "Sample bot",
        DraftCode: "print('hi')",
        History: [new BotVersion("print('hi')", 0)],
        Cursor: 0, PublishedIndex: 0, UpdatedAtTicks: 0);

    private Bot? ResolveActiveBot() => throw new NotImplementedException();

    private Task RedoActiveBotAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-undo-redo-cursor-history
    public record BotVersion(string Code, long AtTicks);

    public record Bot(
        string Id, string Author, string Name,
        string DraftCode,                // unsaved live editor buffer
        List<BotVersion> History,        // committed versions
        int Cursor,                      // index in History; -1 = nothing saved yet
        int PublishedIndex,              // separate from Cursor; what other clients see
        long UpdatedAtTicks);            // last save, for "edited 2m ago" and conflict checks

    private const int MaxHistoryVersions = 50;

    // The bots live in a ReactiveList — item edits go through one whole-list transform.
    private readonly PersistentReactiveList<Bot> _bots = new();

    private void ReplaceBot(string id, Func<Bot, Bot> update) =>
        _bots.Update(bots => bots.Select(b => b.Id == id ? update(b) : b));   // one notification

    private Task SaveActiveBotAsync()
    {
        var active = ResolveActiveBot();

        if (active == null || !HasUnsavedChanges(active))
        {
            return Task.CompletedTask;
        }

        // Truncate any "redo" tail past the current cursor before appending
        var keep = active.Cursor < 0
            ? new List<BotVersion>()
            : active.History.Take(active.Cursor + 1).ToList();
        keep.Add(new BotVersion(active.DraftCode, DateTime.UtcNow.Ticks));

        if (keep.Count > MaxHistoryVersions)
        {
            keep = keep.Skip(keep.Count - MaxHistoryVersions).ToList();
        }

        var newCursor = keep.Count - 1;
        var newPub = active.PublishedIndex < 0 ? -1 : Math.Min(active.PublishedIndex, newCursor);

        ReplaceBot(active.Id, b => b with
        {
            History = keep, Cursor = newCursor, PublishedIndex = newPub,
            UpdatedAtTicks = DateTime.UtcNow.Ticks,
        });
        return Task.CompletedTask;
    }

    private Task UndoActiveBotAsync()
    {
        var active = ResolveActiveBot();

        if (active == null || active.Cursor <= 0)
        {
            return Task.CompletedTask;
        }

        var newCursor = active.Cursor - 1;
        ReplaceBot(active.Id, b => b with
        {
            Cursor = newCursor,
            DraftCode = b.History[newCursor].Code,
        });
        return Task.CompletedTask;
    }

    private static bool HasUnsavedChanges(Bot bot)
    {
        if (bot.Cursor < 0)
        {
            return !string.IsNullOrWhiteSpace(bot.DraftCode);
        }

        return bot.History[bot.Cursor].Code != bot.DraftCode;
    }

    // Disable buttons based on cursor position
    private void RenderUndoRedoActions(IView actions, Bot bot)
    {
        actions.Button([Button.GhostSm], disabled: bot.Cursor <= 0,
            onClick: UndoActiveBotAsync, content: v => v.Text(text: "↶ Undo"));
        actions.Button([Button.GhostSm], disabled: bot.Cursor < 0 || bot.Cursor >= bot.History.Count - 1,
            onClick: RedoActiveBotAsync, content: v => v.Text(text: "↷ Redo"));
    }
    #endregion
}
