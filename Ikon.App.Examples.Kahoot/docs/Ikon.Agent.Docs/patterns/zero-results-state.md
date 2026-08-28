# Zero-Results State — Three Empty Screens, Not One

An empty list has three completely different causes, and generated apps routinely ship one screen for all three. The result is the worst version of each: a user who filtered everything out is told "Add your first recipe", so they think the app lost their data.

Keep them apart:

| Cause | What the user needs | What it must NOT say |
|---|---|---|
| **Nothing yet** — the store really is empty | An invitation and the primary action | — |
| **Nothing matches** — filters or search are active | The way back: clear the filter | "Add your first…" — their data still exists |
| **Nothing loaded** — the fetch or generation failed | What failed, in one human sentence, plus retry | A cheerful empty state; this is an error |

## When to use

Any surface that renders a collection: lists, tables, boards, galleries, search results, feeds. The filtered case appears the moment the surface gains a search box or a filter — see `record-list-toolbar`.

## Snippet

```csharp
private readonly Reactive<string> _search = new("");
private readonly Reactive<string?> _loadError = new(null);

private void RenderCollection(IView view)
{
    var visible = Visible();

    if (visible.Count > 0)
    {
        foreach (var recipe in visible) { RenderRecipeCard(view, recipe); }
        return;
    }

    // 1. FAILED — checked first. An error dressed as an empty state hides the problem.
    if (_loadError.Value is { } message)
    {
        RenderEmpty(view, "cloud-off", "Couldn't load your recipes", message,
            actionLabel: "Try again", action: async () => await ReloadAsync());
        return;
    }

    // 2. FILTERED — their data is still there; the way out is clearing the filter.
    if (_search.Value.Length > 0)
    {
        RenderEmpty(view, "search-x",
            "No recipes match",
            $"Nothing matches \"{_search.Value}\". Your {_recipes.Count} saved recipes are still here.",
            actionLabel: "Clear search", action: async () => ClearFilters());
        return;
    }

    // 3. GENUINELY EMPTY — the only case that invites creation.
    RenderEmpty(view, "chef-hat",
        "No recipes yet",
        "Save your first recipe and it will show up here.",
        actionLabel: "Add a recipe", action: async () => OpenComposer());
}

private static void RenderEmpty(
    IView view, string icon, string title, string description, string actionLabel, Func<Task> action)
{
    view.Column([EmptyState.Root], content: view =>
    {
        view.Box([EmptyState.IconWrap], content: v => v.Icon([EmptyState.IconSize], name: icon));
        view.Text([EmptyState.Title], text: title);
        view.Text([EmptyState.Description], text: description);
        view.Row([EmptyState.Actions], content: v =>
            v.Button([Button.PrimaryMd], text: actionLabel, onClick: action));
    });
}
```

## Notes

- **Order matters.** Check failure first, then filtered, then genuinely empty. Reversing it means a network error renders as a friendly invitation to start over.
- The filtered message should name what was filtered and say the data survives — `$"Your {_recipes.Count} saved recipes are still here."` is the sentence that stops the support ticket.
- The action in each state is different by design: retry, clear, create. An empty state whose only content is a sentence leaves the user with nowhere to go.
- `EmptyState.Root` / `IconWrap` / `IconSize` / `Title` / `Description` / `Actions` are theme constants — the whole layout is already designed, so this never needs hand-rolled spacing. Use `EmptyState.RootFull` when the collection owns the viewport and the state should centre in it.
- Real Lucide icon names, never emoji. `search-x`, `inbox`, `cloud-off`, `file-question` all read as deliberate; a pictograph in this slot is the fastest visual tell of generated work.
- A designed empty state is also the **first** thing a new user sees, so it is worth as much care as the populated view. It is not, however, a substitute for seeding demo content — a cold-booted app should usually show the product working (see the plan's DEMO CONTENT section); the empty state is what remains after the user clears everything.
- Loading is a fourth, separate state — render a `view.Spinner()` or skeletons while the fetch is in flight, never the "nothing yet" screen, which flashes as a lie.

## See also

- `record-list-toolbar` — the search and filters that produce the filtered case.
- `busy-flag-loading` — the loading state that must not be confused with an empty one.
- `depth-and-atmosphere` — giving the empty state enough presence to look intentional.
