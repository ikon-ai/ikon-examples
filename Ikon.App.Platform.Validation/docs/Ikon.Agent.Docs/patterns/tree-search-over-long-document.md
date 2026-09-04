<!-- mined-from: Ikon.App.Patterns -->
# Tree Search Over A Long Document — Navigate The Index, Not The Text

Build a `TreeIndex` once, then let a navigator model **walk** it. The navigator reads the table of
contents and moves toward the answer, so cost scales with the tree's depth rather than the
document's length — which is the entire reason to index instead of stuffing the document into a
prompt.

Indexing costs a full pass over the content, so it belongs behind an upload or a startup step.
Putting it inside the search handler pays for the whole document on every question.

## When to use

A long document a user asks questions about — a manual, a contract, a spec, a transcript, a
codebase. When the answer is a *summary of everything*, `mapreduce-long-document-summary` is the
right shape instead; tree search is for finding the few sections that matter.

## Notes

- **`using Ikon.AI.Emergence.Tree;` is required.** A nested namespace is not imported by its
  parent, so `GlobalUsings.cs` does not cover it — this is one of the few `using Ikon.*` lines an
  app legitimately adds (`Ikon.App.Cells` is the other).
- `IContentReader` is the seam for content that arrives in pieces — a PDF, a database cursor, a
  paged API. `StringContentReader` is the in-memory case.
- `TreeIndexOptions.GenerateSummaries` is what makes navigation work: without per-node summaries
  the navigator is choosing between bare titles.
- **Set `MaxSteps`.** It bounds the walk; without it a navigator that keeps deciding it is not done
  pays for every step it takes.
- `FoundSection.Path` is the breadcrumb through the tree — what makes a hit citable rather than an
  anonymous quote. `Relevance` is the navigator's own reason for returning it.
- A run that stops without a result throws `EmergenceStoppedException` (not `AIException`) — catch
  that type, and keep the previous hits rather than blanking the screen.
- `TreeNode.AddChild` also sets the child's `Parent` and `Depth`; nodes pushed onto `Children`
  directly get those links only when the tree enters a `TreeIndex`, or on `RebuildIndex()`.

## Snippet

```csharp
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
```

## See also

- `mapreduce-long-document-summary` — when the answer needs all of the document, not part of it.
- `web-research` — the same shape over search results instead of one document.
