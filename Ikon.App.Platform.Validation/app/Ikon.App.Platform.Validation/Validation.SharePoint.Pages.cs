using System.Net;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    // Only pages this tab created are offered for deletion, so a test against a shared site cannot
    // remove a real page.
    private const string SpTestPagePrefix = "ikon-validation-";

    private readonly ReactiveList<SitePageInfo> _spPages = new();
    private readonly Reactive<bool> _spPagesShown = new(false);
    private readonly Reactive<string?> _spPageText = new(null);
    private readonly Reactive<string> _spNewPageTitle = new("");
    private readonly Reactive<string> _spNewPageText = new("");
    private readonly Reactive<string> _spSearchKql = new("");
    private readonly Reactive<string> _spSearchKind = new("files");
    private readonly Reactive<SharePointSearchResults?> _spSearchResults = new(null);

    private void RenderSharePointPagesCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Pages");

            view.Row([Layout.Row.Sm, "items-end flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "List pages", disabled: _spBusy.Value, props: TestId("sp-pages"),
                    onClick: async () => await SpRunAsync("list pages", LoadSharePointPagesAsync));
                view.TextField([Input.Default, "w-48"], value: _spNewPageTitle.Value, placeholder: "New page title", props: TestId("sp-page-title"),
                    onValueChange: async v => _spNewPageTitle.Value = v ?? "");
                view.TextField([Input.Default, "flex-1 min-w-48"], value: _spNewPageText.Value, placeholder: "A paragraph for it", props: TestId("sp-page-text"),
                    onValueChange: async v => _spNewPageText.Value = v ?? "");
                view.Button([Button.OutlineSm], text: "Create page", disabled: _spBusy.Value || _spNewPageTitle.Value.Trim().Length == 0, props: TestId("sp-page-create"),
                    onClick: async () => await SpRunAsync("create a page", CreateSharePointPageAsync));
            });

            if (_spPagesShown.Value)
            {
                foreach (var page in _spPages)
                {
                    view.Row([Layout.Row.Sm, "items-center py-1 border-b border-secondary"], key: page.Id, content: view =>
                    {
                        view.Text([Text.Caption, "flex-1 truncate"], $"{page.Title} · {page.Name} · {page.PublishingLevel ?? "?"}", props: TestId("sp-page"));
                        view.Button([Button.GhostSm], text: "Open", disabled: _spBusy.Value,
                            onClick: async () => await SpRunAsync($"read {page.Name}", () => ReadSharePointPageAsync(page)));
                        view.Button([Button.GhostSm], text: "Publish", disabled: _spBusy.Value || page.PublishingLevel == "published",
                            onClick: async () => await SpRunAsync($"publish {page.Name}", () => PublishSharePointPageAsync(page)));

                        if (page.Name.StartsWith(SpTestPagePrefix, StringComparison.Ordinal))
                        {
                            view.Button([Button.GhostErrorSm], text: "Delete", disabled: _spBusy.Value,
                                onClick: async () => await SpRunAsync($"delete {page.Name}", () => DeleteSharePointPageAsync(page)));
                        }
                    });
                }
            }

            if (_spPageText.Value is { } text)
            {
                view.Box(["bg-secondary rounded-md p-4 mt-3 max-h-72 overflow-auto"], content: view =>
                {
                    view.Text([Text.Body, "whitespace-pre-wrap text-sm"], text, props: TestId("sp-page-content"));
                });
            }
        });
    }

    private void RenderSharePointSearchCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Microsoft Search");
            view.Text([Text.Caption, "mb-3"], "Searches the whole tenant through Microsoft's index (Files.Read.All or Sites.Read.All); fresh uploads take minutes to show up");

            view.Row([Layout.Row.Md, "items-end flex-wrap mb-3"], content: view =>
            {
                view.TextField([Input.Default, "flex-1 min-w-64"], value: _spSearchKql.Value, placeholder: "KQL, e.g. budget filetype:xlsx", props: TestId("sp-kql-input"),
                    onValueChange: async v => _spSearchKql.Value = v ?? "");
                view.Select(value: _spSearchKind.Value, ariaLabel: "What to search",
                    options: [new SelectOption("files", "Files"), new SelectOption("items", "List items"), new SelectOption("sites", "Sites"), new SelectOption("lists", "Lists")],
                    props: TestId("sp-kql-kind"), onValueChange: async v => _spSearchKind.Value = v ?? "files");
                view.Button([Button.OutlineMd], text: "Search", disabled: _spBusy.Value || _spSearchKql.Value.Trim().Length == 0, props: TestId("sp-kql-search"),
                    onClick: async () => await SpRunAsync("search with Microsoft Search", SearchSharePointContentAsync));
            });

            if (_spSearchResults.Value is { } results)
            {
                view.Text([Text.Caption, "mb-2"], $"{results.Total} matches{(results.MoreResultsAvailable ? ", more available" : "")}");

                foreach (var hit in results.Hits)
                {
                    view.Column(["py-1 border-b border-secondary"], key: $"{hit.Rank}-{hit.Id}", content: view =>
                    {
                        view.Text([Text.Body, "truncate"], $"{hit.Kind} · {hit.Name ?? hit.WebUrl ?? hit.Id}", props: TestId("sp-kql-hit"));
                        var summary = WebUtility.HtmlDecode(hit.Summary.Replace("<c0>", "«").Replace("</c0>", "»").Replace("<ddd/>", "…"));
                        view.Text([Text.Caption, "truncate"], summary.Length > 0 ? summary : hit.WebUrl ?? "");
                    });
                }
            }
        });
    }

    private async Task<string> LoadSharePointPagesAsync()
    {
        var (sharePoint, _) = SpClients();
        _spPages.ReplaceAll(await sharePoint.ListPagesAsync(_spSite.Value!.Id));
        _spPagesShown.Value = true;
        return $"PASS {_spSite.Value!.DisplayName} has {_spPages.Count} pages, {_spPages.Count(p => p.PublishingLevel == "published")} published";
    }

    private async Task<string> CreateSharePointPageAsync()
    {
        var (sharePoint, _) = SpClients();
        var title = _spNewPageTitle.Value.Trim();
        var paragraph = _spNewPageText.Value.Trim();
        var html = $"<p>{WebUtility.HtmlEncode(paragraph.Length > 0 ? paragraph : "Created from the Ikon Validation app.")}</p>";
        var page = await sharePoint.CreatePageAsync(_spSite.Value!.Id, $"{SpTestPagePrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss}", title, [PageSection.OneColumn(PageWebPart.Text(html))]);

        _spNewPageTitle.Value = "";
        _spNewPageText.Value = "";
        await LoadSharePointPagesAsync();
        return $"PASS created {page.Name} as a {page.PublishingLevel ?? "draft"}: publish it to make it visible";
    }

    private async Task<string> ReadSharePointPageAsync(SitePageInfo page)
    {
        var (sharePoint, _) = SpClients();
        _spPageText.Value = await sharePoint.GetPageTextAsync(_spSite.Value!.Id, page.Id);
        return $"PASS read {page.Name}: {_spPageText.Value.Length} characters of text";
    }

    private async Task<string> PublishSharePointPageAsync(SitePageInfo page)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.PublishPageAsync(_spSite.Value!.Id, page.Id);
        await LoadSharePointPagesAsync();
        return $"PASS published {page.Name}";
    }

    private async Task<string> DeleteSharePointPageAsync(SitePageInfo page)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.DeletePageAsync(_spSite.Value!.Id, page.Id);
        _spPages.RemoveAll(p => p.Id == page.Id);
        return $"PASS moved {page.Name} to the recycle bin";
    }

    private async Task<string> SearchSharePointContentAsync()
    {
        var (sharePoint, _) = SpClients();
        SearchEntityType[] kinds = _spSearchKind.Value switch
        {
            "items" => [SearchEntityType.ListItem],
            "sites" => [SearchEntityType.Site],
            "lists" => [SearchEntityType.List],
            _ => [SearchEntityType.DriveItem],
        };

        _spSearchResults.Value = await sharePoint.SearchContentAsync(new SharePointSearchQuery(_spSearchKql.Value.Trim(), kinds, Size: 25));
        return $"PASS Microsoft Search found {_spSearchResults.Value.Total} matches";
    }
}
