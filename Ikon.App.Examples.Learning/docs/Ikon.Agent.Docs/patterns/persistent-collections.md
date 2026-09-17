<!-- mined-from: Ikon.App.Patterns -->
# Persistent Reactive Collections — The Type Is The Scope
<!-- checked-against: 8e18e5ce5e24c506 -->
Collection state uses the reactive **collection** types, and which one you pick decides both who
sees it and whether it survives a restart. Wrapping an ordinary mutable collection —
`Reactive<List<T>>`, `PersistentReactive<Dictionary<K,V>>` — is build error **IKON002** in an app
project, because a mutation through `.Value` notifies nobody. The `ReactiveCollectionExtensions`
helpers (`Add`, `Remove`, `Mutate`, …) mutate and notify in one call for library code that already
carries such a wrapper; they are not a reason to declare a new one.

| Prefix | Who sees it | Survives restart |
|---|---|---|
| `Reactive*` | everyone | no |
| `Client*` | one client session | no |
| `User*` | one user, across devices | no |
| `Persistent*` | everyone | yes |
| `PersistentSession*` | one session | yes |
| `PersistentUser*` | one user, across devices | yes |

Each prefix has `List<T>`, `Dictionary<TKey,TValue>` and `HashSet<T>` forms with the same contract:
tracked reads, one notification per mutation, copy-on-write snapshots.

## When to use

Any collection the app keeps: a set of tags, a per-user reading list, votes by poll, saved runs.
Reach for `Persistent*` when losing it on restart would be a bug, and for the plain forms when it
is genuinely session-lived.

## Notes

- **Mutate on the reactive itself** — `_dict[key] = v`, `.Add`, `.Remove`, `.Contains`,
  `.RemoveAll`, `.ReplaceAll`, `.Update(...)`. `.Value.Add` does not compile: `Value` reads are
  read-only views. `.Value = newList` replaces the whole content, the same as `ReplaceAll`.
- **Enumeration, `Count` and the indexer are tracked reads**, so a subtree that reads them
  re-renders on change. There is no wrapper component and nothing to subscribe to.
- **Starting items go to the constructor** — `= new();` starts empty, `= new([...])` is what every
  user, session or app sees until it has state of its own. There is no "seed if empty" step.
- **`User*` and `PersistentUser*` resolve against the active `UserScope`** — `UI.Root()`, an action
  callback, or a `ReactiveScope.Use(new UserScope(...))` block — and every read or mutation throws
  where none is active: `Main()`, the constructor, `Task.Run` loops, timers and endpoint handlers.
  Capture `ReactiveScope.UserId` where the scope exists and use the `…For(userId, …)` accessors
  (`AddFor`, `SetFor`, `UpdateFor`, `ValueFor`) from there.
- `PersistenceBackend`, `postgresDatabase` and `key` are constructor arguments when the default
  backend is not what you want; `PublicUrl` is set for the public-asset backend.

## Snippet

```csharp
// Collection state uses the reactive COLLECTION types. Wrapping a mutable collection --
// PersistentReactive<Dictionary<K,V>>, Reactive<List<T>> -- is build error IKON002 in an app
// project, because a mutation through .Value notifies nobody. The ReactiveCollectionExtensions
// helpers exist for legacy code that already has such a wrapper, not for new declarations.
//
// Scope is the first choice, and it is three-way:
//   Persistent*            shared by everyone, survives restart
//   PersistentSession*     per session, survives restart
//   PersistentUser*        per user, follows them across devices
private readonly PersistentReactiveHashSet<string> _publishedTags = new();
private readonly PersistentUserReactiveDictionary<string, int> _votesByPoll = new();
// Constructor items are what every user starts with until they have state of their own.
private readonly PersistentUserReactiveList<string> _readingList = new(["Getting started"]);

private void CastVote(string pollId, int option)
{
    // Mutate ON the reactive: the indexer, Add, Remove and Contains are all tracked, and each
    // mutation notifies once. There is no .Value.Add -- Value reads are read-only views.
    // The User* forms resolve against the UserScope this callback runs in; from Main(), the
    // constructor, Task.Run, a timer or an endpoint handler there is none and this line throws.
    _votesByPoll[pollId] = option;
}

private void RecordVoteLater(string pollId, int option)
{
    // Background work carries no user scope: capture the id here and use the ...For accessor.
    var userId = ReactiveScope.UserId;
    _ = Task.Run(() => _votesByPoll.SetFor(userId, pollId, option));
}

private void ToggleTag(string tag)
{
    if (!_publishedTags.Remove(tag))
    {
        _publishedTags.Add(tag);
    }
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        // Enumerating and reading Count are tracked reads, so this subtree re-renders when the
        // set changes -- no wrapper component and nothing to subscribe to.
        col.Text([Text.H3], text: $"Tags ({_publishedTags.Count})");

        col.Row(["gap-2 flex-wrap"], content: row =>
        {
            foreach (var tag in _publishedTags.Order(StringComparer.Ordinal))
            {
                row.Button(key: tag, onClick: () => ToggleTag(tag), content: v => v.Text(text: tag));
            }
        });

        col.Text([Text.H3], text: "Reading list");

        foreach (var title in _readingList)
        {
            col.Text(key: title, text: title);
        }

        col.Text(["text-muted-foreground text-sm"],
            text: $"You have voted in {_votesByPoll.Count} polls");

        col.Button(onClick: () => CastVote("colours", 1), content: v => v.Text(text: "Vote"));
        col.Button(onClick: () => RecordVoteLater("colours", 2), content: v => v.Text(text: "Vote later"));
    });
}
```

## See also

- `persistent-user-preferences` — the single-value case and when to use it.
- `shared-list-ai-cleanup` — a shared collection with an AI transform over it.
