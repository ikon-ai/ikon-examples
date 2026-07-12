<!-- mined-from: BrainrotArena -->
# Undo/Redo Cursor History — Linear Timeline + Drafted Edits

Each editable entity holds its own version list, plus a `Cursor` index pointing at the current saved version, plus a separate `DraftCode` field for unsaved edits. Save commits the draft as a new version (truncating any "redo tail" past the cursor). Undo/Redo move the cursor; the draft snaps to the version's text. Publish marks one of the versions as the "live" one separately from the cursor.

## When to use

Code/text editors inside an app, prompt history, drawing layers — anywhere users want non-destructive iteration with a clear undo path that survives reconnects.

## Snippet

```csharp
public record BotVersion(string Code, long AtTicks);

public record Bot(
    string Id, string Author, string Name,
    string DraftCode,                // unsaved live editor buffer
    List<BotVersion> History,        // committed versions
    int Cursor,                      // index in History; -1 = nothing saved yet
    int PublishedIndex);             // separate from Cursor; what other clients see

private const int MaxHistoryVersions = 50;

// The bots live in a ReactiveList — item edits go through one whole-list transform.
private readonly PersistentReactiveList<Bot> _bots = new();

private void ReplaceBot(string id, Func<Bot, Bot> update) =>
    _bots.Update(bots => bots.Select(b => b.Id == id ? update(b) : b));   // one notification

private Task SaveActiveBotAsync()
{
    var active = ResolveActiveBot();
    if (active == null || !HasUnsavedChanges(active)) return Task.CompletedTask;

    // Truncate any "redo" tail past the current cursor before appending
    var keep = active.Cursor < 0
        ? new List<BotVersion>()
        : active.History.Take(active.Cursor + 1).ToList();
    keep.Add(new BotVersion(active.DraftCode, DateTime.UtcNow.Ticks));

    if (keep.Count > MaxHistoryVersions)
        keep = keep.Skip(keep.Count - MaxHistoryVersions).ToList();

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
    if (active == null || active.Cursor <= 0) return Task.CompletedTask;
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
    if (bot.Cursor < 0) return !string.IsNullOrWhiteSpace(bot.DraftCode);
    return bot.History[bot.Cursor].Code != bot.DraftCode;
}

// Disable buttons based on cursor position
actions.Button([Button.GhostSm], disabled: bot.Cursor <= 0,
    onClick: UndoActiveBotAsync, content: v => v.Text(text: "↶ Undo"));
actions.Button([Button.GhostSm], disabled: bot.Cursor < 0 || bot.Cursor >= bot.History.Count - 1,
    onClick: RedoActiveBotAsync, content: v => v.Text(text: "↷ Redo"));
```

## Notes

- `DraftCode` is the live buffer; `History[Cursor].Code` is the last saved checkpoint. `HasUnsavedChanges` compares them — show "● unsaved" when they differ.
- Save *truncates the redo tail* (`History.Take(Cursor + 1)`) before appending — standard linear history semantics.
- `PublishedIndex` is decoupled from `Cursor` so the author can keep editing past the version other people are using.
- Cap `History` (50 here) to bound memory; trim from the start when over.
- Hold `_bots` in a `PersistentReactiveList<Bot>` so the whole timeline survives restarts — same one-notification-per-mutation contract as `ReactiveList<T>`, never `PersistentReactive<List<Bot>>`.
- Item-level edits go through `_bots.Update(bots => bots.Select(b => b.Id == id ? update(b) : b))` — one atomic whole-list transform, one notification. `_bots.Value` is an `IReadOnlyList<Bot>` snapshot, so there is nothing to rebuild and reassign by hand.

## See also

- `persistent-user-preferences` — different shape: single value, not a timeline
- `shared-list-ai-cleanup` — same `_bots.Update(...)` whole-list transform pattern
