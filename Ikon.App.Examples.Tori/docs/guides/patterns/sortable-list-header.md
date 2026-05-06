<!-- mined-from: Influencer.Tiktok -->
# Sortable List Header — Click-Column-To-Sort With Active Highlight

A list header where each column label is a button. Clicking it sets a single `Reactive<ProfileSortOrder>` enum and re-sorts the list. The active column gets a subtle background highlight so the user always knows what's driving the order. Clicking the same column flips ascending/descending. The body is plain LINQ-sorted on each render — no separate sorted-data state.

## When to use

Any results table or scrollable list with > ~20 items and multiple useful sort axes (score, date, name, has-email). Sort dropdowns are slower; click-the-column is the standard table affordance.

## Snippet

```csharp
public enum ProfileSortOrder
{
    ScoreDesc, ScoreAsc, HandleAsc, HasEmail, FollowerCountDesc, QueryHitsDesc,
}

private readonly Reactive<ProfileSortOrder> _profileSortOrder = new(ProfileSortOrder.ScoreDesc);

private void RenderListHeader(UIView view)
{
    view.Box(["px-2 py-1.5 border-b border-muted bg-muted/30"], content: view =>
    {
        view.Row(["items-center gap-1 text-xs"], content: view =>
        {
            view.Text(["w-5 shrink-0 text-right"], "#");

            view.Button([
                $"text-left px-1 py-0.5 rounded hover:bg-accent/50 flex-1 min-w-0 " +
                $"{(_profileSortOrder.Value == ProfileSortOrder.HandleAsc ? "bg-accent/30" : "")}",
            ], onClick: async () => { _profileSortOrder.Value = ProfileSortOrder.HandleAsc; },
            content: v => v.Text([Text.Caption, "font-semibold truncate"], "Handle"));

            view.Button([
                $"text-right px-1 py-0.5 rounded hover:bg-accent/50 w-12 shrink-0 " +
                $"{(_profileSortOrder.Value == ProfileSortOrder.FollowerCountDesc ? "bg-accent/30" : "")}",
            ], onClick: async () => { _profileSortOrder.Value = ProfileSortOrder.FollowerCountDesc; },
            content: v => v.Text([Text.Caption, "font-semibold"], "Foll"));

            view.Button([
                $"text-right px-1 py-0.5 rounded hover:bg-accent/50 w-8 shrink-0 " +
                $"{(_profileSortOrder.Value == ProfileSortOrder.ScoreDesc ? "bg-accent/30" : "")}",
            ], onClick: async () =>
            {
                _profileSortOrder.Value = _profileSortOrder.Value == ProfileSortOrder.ScoreDesc
                    ? ProfileSortOrder.ScoreAsc
                    : ProfileSortOrder.ScoreDesc;
            }, content: v => v.Text([Text.Caption, "font-semibold"], "Score"));
        });
    });
}

private List<Card> SortedCards(List<Card> cards) => _profileSortOrder.Value switch
{
    ProfileSortOrder.ScoreDesc         => cards.OrderByDescending(c => c.Score).ToList(),
    ProfileSortOrder.ScoreAsc          => cards.OrderBy(c => c.Score).ToList(),
    ProfileSortOrder.HandleAsc         => cards.OrderBy(c => c.Handle).ToList(),
    ProfileSortOrder.HasEmail          => cards.OrderByDescending(c => c.Emails.Count > 0)
                                                .ThenByDescending(c => c.Score).ToList(),
    ProfileSortOrder.FollowerCountDesc => cards.OrderByDescending(c => ParseFollowerCount(c.FollowerCount)).ToList(),
    ProfileSortOrder.QueryHitsDesc     => cards.OrderByDescending(c => c.QueryHits).ToList(),
    _                                  => cards,
};
```

## Notes

- Single `Reactive<enum>` for sort order — not a tuple of (column, ascending). Encode the asc/desc variants directly so the switch expression is simple.
- The "Score" column is the only one that toggles direction on re-click — the others are single-direction (you almost never want HandleZA).
- Re-sort on every render with LINQ; don't cache. The list is already in memory and `OrderBy` is fast for typical UI sizes.
- Show count of filtered/total items in the header (`{sortedCards.Count} of {cards.Count}`) so the user knows when filters are hiding rows.

## See also

- `kanban-multi-column`
- `filter-button-group`
