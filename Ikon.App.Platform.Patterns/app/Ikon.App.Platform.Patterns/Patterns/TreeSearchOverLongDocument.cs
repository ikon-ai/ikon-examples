namespace Ikon.App.Platform.Patterns.Patterns;

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
            LLMModel.Default,
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

        using var busy = _busy.AsToken();
        _error.Value = null;

        try
        {
            var found = await Emerge.TreeSearch(LLMModel.Claude55Sonnet, new KernelContext(), options =>
            {
                options.Index = index;
                options.Query = question;
                options.MaxResults = 5;

                // MaxSteps bounds the walk (default 10). A navigator that keeps deciding it is not
                // done yet pays for every step up to it.
                options.MaxSteps = 8;
            });

            // Empty Sections alone cannot tell a document with no answer from a navigator that never
            // read it; only the second is a failed search.
            if (found.StoppedReason == "NoToolCall")
            {
                _error.Value = "The search didn't finish — try again.";
                return;
            }

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
                col.Card([Card.Default], key: section.NodeId, contentStyle: ["p-3"], content: card =>
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

    #region example:pattern-tree-index-from-headings
    /// <summary>
    /// A manual that already has headings needs no model to index it. One node per heading holds the
    /// text under it, so a hit is that section rather than the whole document, and a long manual costs
    /// nothing to index. The summary is the section's first line, which is what the navigator reads.
    /// </summary>
    internal static TreeIndex IndexFromHeadings(string title, string markdown)
    {
        var root = new TreeNode("root", title);
        TreeNode? chapter = null;
        var current = root;
        var text = new System.Text.StringBuilder();
        var lines = markdown.Split('\n');

        // Text above the first heading, or under a skipped title line, stays readable as the root's own
        void Close()
        {
            current.Content = text.ToString().Trim();
            current.Summary = current.Content.Split('\n')[0] is var first && first.Length > 160 ? first[..160] : first;
            text.Clear();
        }

        for (var i = TitleLineCount(title, lines); i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                Close();
                chapter = new TreeNode("", line[2..].Trim());
                root.AddChild(chapter);
                current = chapter;
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Close();
                current = new TreeNode("", line[3..].Trim());
                (chapter ?? root).AddChild(current);
            }
            else
            {
                text.AppendLine(line);
            }
        }

        Close();
        NumberChildren(root, "");
        return new TreeIndex(root);
    }

    // "3 Gearbox", "3. Gearbox" and "3.2 Oil checks" keep the document's own numbers as ids, so a node
    // id matches what the manual and its readers call the section. Siblings keep them only when every
    // one has its own distinct number, and are counted otherwise: a counted id beside a document's own
    // could repeat it, and a TreeIndex keeps only the last node of an id.
    private static void NumberChildren(TreeNode parent, string parentNumber)
    {
        var own = parent.Children.Select(child => HeadingNumber(child.Title, parentNumber)).ToList();
        var keepOwn = own.All(number => number is not null) && own.Distinct().Count() == own.Count;

        for (var i = 0; i < parent.Children.Count; i++)
        {
            var child = parent.Children[i];
            child.Id = keepOwn ? own[i]! : parentNumber.Length == 0 ? $"{i + 1}" : $"{parentNumber}.{i + 1}";
            NumberChildren(child, child.Id);
        }
    }

    // A section number has one part more than its parent's and starts with it ("3.2" under "3"). A year
    // ("# 2024 Service manual") or a measure ("## 10 mm bolts") is part of the heading's text.
    private static string? HeadingNumber(string heading, string parentNumber)
    {
        var match = System.Text.RegularExpressions.Regex.Match(heading,
            @"^(\d{1,3}(?:\.\d{1,3})*)\.?\s+(?!(?:mm|cm|m|km|mg|g|kg|ml|v|kv|w|kw|nm|hz|bar|psi|ft|lbs?|%|°c)(?:\s|$))\S",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        var number = match.Groups[1].Value;
        var depth = parentNumber.Length == 0 ? 1 : parentNumber.Split('.').Length + 1;

        if (number.Split('.').Length != depth || (parentNumber.Length > 0 && !number.StartsWith(parentNumber + ".", StringComparison.Ordinal)))
        {
            return null;
        }

        return number;
    }

    // A first "# " line that names the document is not chapter 1: indexing it as one would shift
    // every counted chapter id by one, and the navigator would cite the wrong section.
    private static int TitleLineCount(string title, string[] lines)
    {
        var first = Array.FindIndex(lines, line => line.Trim().Length > 0);

        if (first < 0 || !lines[first].StartsWith("# ", StringComparison.Ordinal))
        {
            return 0;
        }

        var heading = lines[first][2..].Trim();

        if (string.Equals(heading, title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return first + 1;
        }

        if (HeadingNumber(heading, "") is not null)
        {
            return 0;
        }

        var laterChapters = lines.Skip(first + 1).Where(line => line.StartsWith("# ", StringComparison.Ordinal)).ToList();

        if (laterChapters.Count > 0)
        {
            // Unnumbered above numbered chapters is a title; above unnumbered ones it is chapter 1.
            return laterChapters.All(line => HeadingNumber(line[2..].Trim(), "") is not null) ? first + 1 : 0;
        }

        // The only "# " line, directly over "## " headings, titles a document whose chapters are "## ".
        var next = Array.FindIndex(lines, first + 1, line => line.Trim().Length > 0);
        return next >= 0 && lines[next].StartsWith("## ", StringComparison.Ordinal) ? first + 1 : 0;
    }
    #endregion
}
