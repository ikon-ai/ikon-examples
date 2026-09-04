namespace Ikon.App.Patterns.Patterns;

// Pattern: skeleton-while-loading — see docs/patterns/skeleton-while-loading.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class SkeletonWhileLoading : IPatternDemo
{
    public string Slug => "skeleton-while-loading";
    public string Title => "Skeleton while loading";
    public string Category => "Status & feedback";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Article(string Id, string Title, string Summary);

    #region docsnippet:pattern-skeleton-while-loading
    private readonly ClientReactive<bool> _loading = new(true);
    private readonly ClientReactiveList<Article> _articles = new();

    /// <summary>
    /// A skeleton stands in for the SHAPE of what is coming, so it mirrors the real row rather
    /// than being a generic grey bar. Same container, same gaps, same circle for the avatar --
    /// that is what stops the layout jumping when the content lands.
    /// </summary>
    private static void RenderPlaceholderRow(IView view)
    {
        view.Row(["gap-3 items-center"], content: row =>
        {
            row.Skeleton(shape: SkeletonShape.Circle, size: SkeletonSize.Md);

            row.Column(["gap-2 flex-1"], content: col =>
            {
                col.Skeleton(["w-1/3"], size: SkeletonSize.Sm);
                col.Skeleton(["w-full"], size: SkeletonSize.Sm);
            });
        });
    }

    private void Render(IView view)
    {
        view.Column(["gap-4"], content: col =>
        {
            if (_loading.Value)
            {
                // A fixed small number of placeholder rows: enough to read as "content is coming",
                // not so many that they promise a page that may turn out to be empty.
                for (var i = 0; i < 3; i++)
                {
                    RenderPlaceholderRow(col);
                }

                return;
            }

            // Loaded and empty is a DIFFERENT state from loading, and needs its own designed
            // screen -- a skeleton that never resolves is the worst of both.
            if (_articles.Count == 0)
            {
                col.Text(["text-muted-foreground"], text: "Nothing here yet");
                return;
            }

            foreach (var article in _articles)
            {
                col.Column(["gap-1"], key: article.Id, content: item =>
                {
                    item.Text([Text.H3], text: article.Title);
                    item.Text(["text-muted-foreground"], text: article.Summary);
                });
            }
        });
    }
    #endregion
}
