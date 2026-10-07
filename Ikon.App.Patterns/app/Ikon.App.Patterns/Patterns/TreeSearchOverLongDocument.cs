// Ikon.AI.Emergence.Tree is NOT in an app's GlobalUsings — a nested namespace is not imported by
// its parent, so this is one of the few `using Ikon.*` lines an app legitimately adds.
using Ikon.AI.Emergence.Tree;

namespace Ikon.App.Patterns.Patterns;

// Pattern: tree-search-over-long-document — see docs/patterns/tree-search-over-long-document.md.
// The example region below is the canonical body the doc extracts.
internal sealed class TreeSearchOverLongDocument : IPatternDemo
{
    public string Slug => "tree-search-over-long-document";
    public string Title => "Tree search over a long document";
    public string Category => "Web & data";

    // The gallery builds no index and calls no model: the demo starts with the hits a search of a
    // tenancy agreement for "can I end the lease early?" would return.
    private static readonly FoundSection[] SampleHits =
    [
        new("n-4-2", "Tenancy agreement > 4 Term > 4.2 Early termination",
            "Either party may end the tenancy before the end of the fixed term by giving at least two full calendar months' notice in writing. Notice given by the tenant takes effect on the last day of a rental period.",
            "Answers the question directly: early termination is allowed with two months' written notice", Page: 3),
        new("n-4-3", "Tenancy agreement > 4 Term > 4.3 Break fee",
            "Where the tenant ends the tenancy under clause 4.2 within the first twelve months, a break fee equal to one month's rent is payable on the termination date.",
            "The cost of ending early in the first year", Page: 3),
        new("n-9-1", "Tenancy agreement > 9 Deposit > 9.1 Return of deposit",
            "The deposit is returned within 21 days of the end of the tenancy, less any amounts owed under this agreement, including an unpaid break fee.",
            "What happens to the deposit when the lease ends early", Page: 7),
    ];

    public TreeSearchOverLongDocument() => _hits.ReplaceAll(SampleHits);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-2xl"], content: col =>
        {
            col.Text(["text-xs text-zinc-400"],
                "Sample hits for \"Can I end the lease early?\" over a tenancy agreement; no index is built and no model is called");
            Render(col);
        });
    }

    #region example:pattern-tree-search-over-long-document
    private readonly Reactive<TreeIndex?> _index = new(null);
    private readonly ReactiveList<FoundSection> _hits = new();
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _error = new(null);

    /// <summary>
    /// Index once, search many times. Building walks the whole document and costs a full pass, so
    /// it belongs behind an upload or a startup step -- never inside the search handler.
    /// </summary>
    private async Task IndexAsync(string document)
    {
        // The reader overload is the one for anything that arrives in pieces; StringContentReader
        // is the in-memory case, and IContentReader is the seam for a PDF or a database cursor.
        _index.Value = await TreeIndex.BuildAsync(
            LLMModel.Claude45Haiku,
            new StringContentReader(document),
            new TreeIndexOptions { GenerateSummaries = true, MaxDepth = 3 });
    }

    /// <summary>
    /// Search navigates the TREE, not the text: the navigator model reads the table of contents
    /// and walks toward the answer, so cost scales with tree depth rather than document length.
    /// That is the whole reason to build an index instead of stuffing the document into a prompt.
    /// </summary>
    private async Task SearchAsync(string question)
    {
        if (_index.Value is not { } index || _busy.Value)
        {
            return;
        }

        using var _ = _busy.AsToken();
        _error.Value = null;

        try
        {
            var found = await Emerge.TreeSearch(LLMModel.Claude46Sonnet, new KernelContext(), options =>
            {
                options.Index = index;
                options.Query = question;
                options.MaxResults = 5;

                // MaxSteps bounds the walk (default 10). A navigator that keeps deciding it is not
                // done yet pays for every step up to it.
                options.MaxSteps = 8;
            });

            _hits.ReplaceAll(found.Sections);
        }
        // A walk that stops without a result throws EmergenceStoppedException; a generation failure
        // that cannot be retried is rethrown as is. Either way the previous hits stay on screen.
        catch (Exception ex)
        {
            Log.Instance.Warning($"Tree search over '{index.Root.Title}' failed, user can search again: {ex.Message}");
            _error.Value = "The search didn't finish — try again.";
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            if (_error.Value is { } error)
            {
                col.Text(["text-error-primary text-sm"], text: error);
            }

            foreach (var section in _hits)
            {
                col.Card(["p-3"], key: section.NodeId, content: card =>
                {
                    card.Column(["gap-1"], content: lines =>
                    {
                        // Path is the breadcrumb through the tree -- what makes a hit citable.
                        lines.Text(["text-muted-foreground text-xs"], text: section.Path);
                        lines.Text(text: section.Content);
                        lines.Text(["text-muted-foreground text-xs italic"], text: section.Relevance);
                    });
                });
            }
        });
    }
    #endregion
}
