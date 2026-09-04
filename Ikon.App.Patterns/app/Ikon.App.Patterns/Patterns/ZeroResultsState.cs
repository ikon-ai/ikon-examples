namespace Ikon.App.Patterns.Patterns;

// Pattern: zero-results-state — see docs/patterns/zero-results-state.md.
// The docsnippet region is the three-way branch plus the shared empty-state renderer; the stubs
// outside it stand in for the record model, the store, the derived query and the three actions each
// state offers. The demo renders the genuinely-empty case, which is what a cold-booted app shows.
internal sealed class ZeroResultsState : IPatternDemo
{
    public string Slug => "zero-results-state";
    public string Title => "Zero results state";
    public string Category => "Feedback";
    public void RenderDemo(IView view) => RenderCollection(view);

    private sealed record Recipe(string Id, string Title);

    private readonly ReactiveList<Recipe> _recipes = new();

    private IReadOnlyList<Recipe> Visible() =>
        _search.Value.Length == 0
            ? _recipes
            : _recipes.Where(r => r.Title.Contains(_search.Value, StringComparison.OrdinalIgnoreCase)).ToList();

    private void ClearFilters() => _search.Value = "";

    private Task ReloadAsync()
    {
        _loadError.Value = null;
        return Task.CompletedTask;
    }

    private void OpenComposer() => _recipes.Add(new Recipe(Guid.NewGuid().ToString(), "New recipe"));

    private static void RenderRecipeCard(IView view, Recipe recipe) =>
        view.Box([Card.Default, "p-3"], content: v => v.Text([Text.Body], text: recipe.Title));

    #region docsnippet:pattern-zero-results-state
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
    #endregion
}
