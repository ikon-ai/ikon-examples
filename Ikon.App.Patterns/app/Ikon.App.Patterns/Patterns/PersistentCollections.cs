namespace Ikon.App.Patterns.Patterns;

// Pattern: persistent-collections — see docs/patterns/persistent-collections.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class PersistentCollections : IPatternDemo
{
    public string Slug => "persistent-collections";
    public string Title => "Persistent reactive collections";
    public string Category => "State";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-persistent-collections
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
    #endregion
}
