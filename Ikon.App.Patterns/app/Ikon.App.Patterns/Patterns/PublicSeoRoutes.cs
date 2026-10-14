namespace Ikon.App.Patterns.Patterns;

// Pattern: public-seo-routes — see docs/patterns/public-seo-routes.md.
// `app` is the App's primary-constructor handle; LoadPublishedArticlesAsync stands in for the app's
// database read. RenderDemo draws one sample article through the region's render method.
internal sealed class PublicSeoRoutes(IAppBase app) : IPatternDemo
{
    public string Slug => "public-seo-routes";
    public string Title => "Public SEO routes";
    public string Category => "Platform mechanics";

    public void RenderDemo(IView view)
    {
        var sample = new Article("tide-pools", "en", "Reading a tide pool", "What lives between the tides, and when to look.", "Low tide uncovers a few square metres of rock that hold more life than the beach around them.", "articles/tide-pools.jpg", 1200);
        RenderArticle(view, sample);
    }

    private sealed record Article(string Slug, string Language, string Title, string Summary, string Body, string ImagePath, int Views);

    private Task<IReadOnlyList<Article>> LoadPublishedArticlesAsync() => throw new NotImplementedException();

    private static Task SaveToReadingListAsync(string slug) => Task.CompletedTask;

    #region example:pattern-public-seo-routes
    private readonly ReactiveDictionary<string, Article> _articlesByPath = new();

    private static string PathOf(Article article) => $"/{article.Language}/articles/{article.Slug}";

    public void Main()
    {
        app.OnStarting(async () =>
        {
            foreach (var article in await LoadPublishedArticlesAsync())
            {
                _articlesByPath[PathOf(article)] = article;
            }
        });

        // Runs on the deploying machine during the capture. The first [BootSnapshot] MaxRoutes routes
        // are prerendered and the rest are only listed in sitemap.xml, so the most important come first.
        app.OnSnapshotRoutes(async () =>
            (await LoadPublishedArticlesAsync()).OrderByDescending(article => article.Views).Select(PathOf));

        app.OnClientJoined(async _ => await DescribePageAsync(app.Navigation.CurrentPath ?? "/"));
        app.Navigation.PathChangedAsync += async args => await DescribePageAsync(args.Path);
    }

    // The capture records these for the route's prerendered page; in a browser they set the tab
    // title and the share tags as the visitor moves between routes.
    private async Task DescribePageAsync(string path)
    {
        if (!_articlesByPath.TryGetValue(path, out var article))
        {
            await ClientFunctions.SetPageMetadataAsync(title: "Field notes", description: "Short guides to the shore and what lives there.", language: "en");
            return;
        }

        // Every language version lists the same set, itself included: search engines ignore an
        // alternate the other page does not name back.
        var alternates = _articlesByPath.Values
            .Where(version => version.Slug == article.Slug)
            .ToDictionary(version => version.Language, PathOf);
        alternates["x-default"] = alternates.GetValueOrDefault("en", path);

        await ClientFunctions.SetPageMetadataAsync(
            title: article.Title,
            description: article.Summary,
            imageUrl: await app.Files.Public.GetAbsoluteUrlAsync(article.ImagePath),
            language: article.Language,
            alternates: alternates);
    }

    private static void RenderArticle(IView view, Article article)
    {
        // Revealed: what a crawler indexes. Everything else ships as a skeleton in the snapshot.
        view.SnapshotReveal(content: view =>
        {
            view.Text([Text.H1], article.Title);
            view.Text([Text.BodyLg, "mt-2"], article.Summary);
            view.Markdown(article.Body);
        });

        // Hidden: a control that does nothing before the live connection exists.
        view.SnapshotHide(content: view => view.Button([Button.OutlineMd, "mt-4"], text: "Save to reading list", onClick: async () => await SaveToReadingListAsync(article.Slug)));
    }
    #endregion
}
