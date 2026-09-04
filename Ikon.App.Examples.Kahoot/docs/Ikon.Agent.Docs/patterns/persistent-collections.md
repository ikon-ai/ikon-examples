<!-- mined-from: Ikon.App.Patterns -->
# Persistent Reactive Collections — The Type Is The Scope

Collection state uses the reactive **collection** types, and which one you pick decides both who
sees it and whether it survives a restart. Wrapping an ordinary mutable collection —
`Reactive<List<T>>`, `PersistentReactive<Dictionary<K,V>>` — is build error **IKON002**, because a
mutation inside the wrapper notifies nobody.

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
- The lists start empty — declare `= new();` with no argument, or pass initial entries to seed.
- **Seed per-user collections in `OnClientJoined`**, when that user's store is empty. Touching a
  user-scoped field in `OnStarting` crashes the boot, because no user scope exists yet. Shared
  state seeds in `OnStarting`.
- `PersistenceBackend`, `postgresDatabase` and `key` are constructor arguments when the default
  backend is not what you want; `PublicUrl` is set for the public-asset backend.

## Snippet

```csharp
// Collection state uses the reactive COLLECTION types. Wrapping a mutable collection --
// PersistentReactive<Dictionary<K,V>>, Reactive<List<T>> -- is build error IKON002, because a
// mutation inside the wrapper notifies nobody.
//
// Scope is the first choice, and it is three-way:
//   Persistent*            shared by everyone, survives restart
//   PersistentSession*     per session, survives restart
//   PersistentUser*        per user, follows them across devices
private readonly PersistentReactiveHashSet<string> _publishedTags = new();
private readonly PersistentUserReactiveDictionary<string, int> _votesByPoll = new();
private readonly PersistentUserReactiveList<string> _readingList = new();

private void CastVote(string pollId, int option)
{
    // Mutate ON the reactive: the indexer, Add, Remove and Contains are all tracked, and each
    // mutation notifies once. There is no .Value.Add -- Value reads are read-only views.
    _votesByPoll[pollId] = option;
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

        // A per-user collection is seeded in OnClientJoined when that user's store is empty --
        // never in OnStarting, where no user scope exists yet and touching one crashes the boot.
        col.Button(onClick: () => CastVote("colours", 1), content: v => v.Text(text: "Vote"));
    });
}
```

## See also

- `persistent-user-preferences` — the single-value case and when to use it.
- `shared-list-ai-cleanup` — a shared collection with an AI transform over it.
