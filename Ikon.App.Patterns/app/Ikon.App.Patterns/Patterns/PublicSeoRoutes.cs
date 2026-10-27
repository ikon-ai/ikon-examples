using System.Text.Json;

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
        var sample = new Article("tide-pools", "en", "Reading a tide pool", "What lives between the tides, and when to look.", "Low tide uncovers a few square metres of rock that hold more life than the beach around them.", "articles/tide-pools.jpg", new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
        RenderArticle(view, sample);
    }

    private sealed record Article(string Slug, string Language, string Title, string Summary, string Body, string ImagePath, DateTimeOffset UpdatedAt);

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

        app.OnClientJoined(async _ => await DescribePageAsync(app.Navigation.CurrentPath ?? "/"));
        app.Navigation.PathChangedAsync += async args => await DescribePageAsync(args.Path);
    }

    // Called by whatever writes articles: an editor's save, an import job. The platform captures the
    // page as a guest sees it now and serves it to crawlers, with this lastmod in sitemap.xml.
    private async Task OnArticleSavedAsync(Article article)
    {
        _articlesByPath[PathOf(article)] = article;
        await app.PublicRoutes.PublishAsync(PathOf(article), lastModified: article.UpdatedAt);
    }

    private async Task OnArticleWithdrawnAsync(Article article)
    {
        _articlesByPath.Remove(PathOf(article));
        await app.PublicRoutes.UnpublishAsync(PathOf(article));
    }

    // The capture records these for the route's crawler page; in a browser they set the tab title and
    // the share tags as the visitor moves between routes.
    private async Task DescribePageAsync(string path)
    {
        if (!_articlesByPath.TryGetValue(path, out var article))
        {
            // Any other path shows the not-found screen, which search engines must not list
            bool home = path == "/";
            await ClientFunctions.SetPageMetadataAsync(title: home ? "Field notes" : "Not found", description: "Short guides to the shore and what lives there.", language: "en", noIndex: !home);
            return;
        }

        // Every language version lists the same set, itself included: search engines ignore an
        // alternate the other page does not name back.
        var alternates = _articlesByPath.Values
            .Where(version => version.Slug == article.Slug)
            .ToDictionary(version => version.Language, PathOf);
        alternates["x-default"] = alternates.GetValueOrDefault("en", path);

        string imageUrl = await app.Files.Public.GetAbsoluteUrlAsync(article.ImagePath);

        // schema.org structured data: search engines show it as a rich result
        string jsonLd = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = article.Title,
            ["description"] = article.Summary,
            ["image"] = imageUrl,
            ["dateModified"] = article.UpdatedAt.ToString("O"),
            ["inLanguage"] = article.Language,
        });

        await ClientFunctions.SetPageMetadataAsync(
            title: article.Title,
            description: article.Summary,
            imageUrl: imageUrl,
            language: article.Language,
            alternates: alternates,
            jsonLd: jsonLd,
            type: "article");
    }

    private static void RenderArticle(IView view, Article article)
    {
        // Revealed: what a crawler indexes. Everything else ships as a skeleton in the snapshot.
        view.SnapshotReveal(content: view =>
        {
            view.Heading(article.Title, style: [Text.H1]);
            view.Text([Text.BodyLg, "mt-2"], article.Summary);
            view.Markdown(article.Body);
        });

        // Hidden: a control that does nothing before the live connection exists.
        view.SnapshotHide(content: view => view.Button([Button.OutlineMd, "mt-4"], text: "Save to reading list", onClick: async () => await SaveToReadingListAsync(article.Slug)));
    }
    #endregion
}
