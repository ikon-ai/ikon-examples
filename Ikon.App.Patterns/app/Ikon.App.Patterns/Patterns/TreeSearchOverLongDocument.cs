// Ikon.AI.Emergence.Tree is NOT in an app's GlobalUsings — a nested namespace is not imported by
// its parent, so this is one of the few `using Ikon.*` lines an app legitimately adds.
using Ikon.AI.Emergence.Tree;

namespace Ikon.App.Patterns.Patterns;

// Pattern: tree-search-over-long-document — see docs/patterns/tree-search-over-long-document.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class TreeSearchOverLongDocument : IPatternDemo
{
    public string Slug => "tree-search-over-long-document";
    public string Title => "Tree search over a long document";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-tree-search-over-long-document
    private readonly Reactive<TreeIndex?> _index = new(null);
    private readonly ReactiveList<FoundSection> _hits = new();
    private readonly Reactive<bool> _busy = new(false);

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

        try
        {
            var found = await Emerge.TreeSearch(LLMModel.Claude46Sonnet, new KernelContext(), options =>
            {
                options.Index = index;
                options.Query = question;
                options.MaxResults = 5;

                // MaxSteps bounds the walk. Without it a navigator that keeps deciding it is not
                // done yet pays for every step it takes.
                options.MaxSteps = 8;
            });

            _hits.ReplaceAll(found.Sections);
        }
        catch (EmergenceStoppedException)
        {
            // The walk stopped without a result; the previous hits stay on screen.
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            foreach (var section in _hits)
            {
                col.Card(["p-3"], key: section.NodeId, content: card =>
                {
                    // Path is the breadcrumb through the tree -- what makes a hit citable.
                    card.Text(["text-muted-foreground text-xs"], text: section.Path);
                    card.Text(text: section.Content);
                    card.Text(["text-muted-foreground text-xs italic"], text: section.Relevance);
                });
            }
        });
    }
    #endregion
}
