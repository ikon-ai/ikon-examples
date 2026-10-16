namespace Ikon.App.Patterns.Patterns;

// Pattern: cluster-similar-items — see docs/patterns/cluster-similar-items.md.
// The gallery's embeddings are the mock's, which give every different text an unrelated vector, so
// the seeded themes below use made-up vectors and a new item joins a theme only as an exact repeat.
internal sealed class ClusterSimilarItems : IPatternDemo
{
    public string Slug => "cluster-similar-items";
    public string Title => "Group similar feedback";
    public string Category => "AI";

    public ClusterSimilarItems()
    {
        var size = EmbeddingGenerator.GetCapabilities(Model).EmbeddingVectorSize;
        var random = new Random(7);

        foreach (var texts in new[]
        {
            new[] { "Dark mode please", "A dark theme for night use", "The white screen hurts my eyes at night" },
            new[] { "CSV export fails on large files", "Export times out with 50k rows" },
            new[] { "Slack alerts would be great" },
        })
        {
            var theme = Enumerable.Range(0, size).Select(_ => (float)random.NextDouble() - 0.5f).ToArray();
            var items = texts.Select(text => new Feedback(Guid.NewGuid().ToString("N"), text, theme.Select(v => v + ((float)random.NextDouble() - 0.5f) * 0.3f).ToArray())).ToList();
            _themes.Add(new Theme(Guid.NewGuid().ToString("N"), texts[0], items, VectorMath.CalculateAverageEmbedding(items.Select(i => i.Vector).ToList())));
        }
    }

    public void RenderDemo(IView view) => RenderThemes((UIView)view);

    #region example:pattern-cluster-similar-items
    private sealed record Feedback(string Id, string Text, float[] Vector);

    private sealed record Theme(string Id, string Name, IReadOnlyList<Feedback> Items, float[] Centroid);

    private sealed record ThemeName(int Number, string Name);

    private const EmbeddingModel Model = EmbeddingModel.OpenAI3Small;

    // Cosine similarity at or above which an item joins a theme. On this model, short rewordings of
    // one request scored 0.41-0.68 and different requests 0.07-0.32; measure again for another model.
    private const float SameTheme = 0.45f;

    // A real app keeps the items, their vectors and their theme in its database
    private readonly ReactiveList<Theme> _themes = new();
    // Per user, so a reload keeps a half-typed item
    private readonly UserReactive<string> _draft = new("");
    private readonly Reactive<bool> _naming = new(false);

    private async Task AddFeedbackAsync(string text)
    {
        // One call embeds a whole batch, so an import embeds its rows together
        var vector = (await EmbeddingGenerator.EmbedAsync([text], Model, EmbeddingType.Clustering))[0];
        var item = new Feedback(Guid.NewGuid().ToString("N"), text, vector);

        // Inside Update, so two items arriving at once both see the other's theme
        _themes.Update(themes =>
        {
            var best = -1;
            var bestScore = SameTheme;

            for (var i = 0; i < themes.Count; i++)
            {
                var score = VectorMath.CalculateCosineSimilarity(vector, themes[i].Centroid);

                if (score >= bestScore)
                {
                    (best, bestScore) = (i, score);
                }
            }

            if (best < 0)
            {
                themes.Add(new Theme(Guid.NewGuid().ToString("N"), text, [item], vector));
                return;
            }

            // The centre moves with each member, so a theme drifts toward what its items share
            var items = themes[best].Items.Append(item).ToList();
            themes[best] = themes[best] with { Items = items, Centroid = VectorMath.CalculateAverageEmbedding(items.Select(f => f.Vector).ToList()) };
        });
    }

    // One model call names every theme from its items; the grouping itself needs no model
    private async Task NameThemesAsync()
    {
        using var busy = _naming.AsToken();
        var themes = _themes.Value;
        var listing = string.Join("\n", themes.Select((t, i) => $"{i + 1}. {string.Join(" | ", t.Items.Take(8).Select(f => f.Text))}"));

        var names = await Emerge.Run<List<ThemeName>>(LLMModel.Gpt5Mini, pass =>
        {
            pass.Command = $"Name each numbered group of user feedback in two to five words, as a product team would.\n\n{listing}";
        });

        // Matched by id, since an item can join or start a theme while the model answers
        var byId = names.Where(n => n.Number >= 1 && n.Number <= themes.Count).ToDictionary(n => themes[n.Number - 1].Id, n => n.Name);
        _themes.ReplaceWhere(t => byId.ContainsKey(t.Id), t => t with { Name = byId[t.Id] });
    }

    private void RenderThemes(UIView view)
    {
        view.Column(["gap-4 w-full max-w-xl"], content: col =>
        {
            col.Row(["gap-2 items-end"], content: row =>
            {
                row.TextField(["default", "flex-1"], label: "New feedback", bind: _draft);
                row.Button([Button.PrimaryMd], text: "Add", disabled: string.IsNullOrWhiteSpace(_draft.Value), onClick: async () =>
                {
                    var text = _draft.Value.Trim();
                    _draft.Value = "";
                    await AddFeedbackAsync(text);
                });
            });

            col.Button([Button.OutlineSm, "self-start"], text: _naming.Value ? "Naming…" : "Name the themes", disabled: _naming.Value || _themes.Count == 0, onClick: NameThemesAsync);

            foreach (var theme in _themes.OrderByDescending(t => t.Items.Count))
            {
                col.Column([Card.Default, "gap-1 p-4"], key: theme.Id, content: card =>
                {
                    card.Text([Text.H3], theme.Name);
                    card.Text([Text.Caption], theme.Items.Count == 1 ? "1 item" : $"{theme.Items.Count} items");

                    foreach (var item in theme.Items)
                    {
                        card.Text([Text.Body], item.Text, key: item.Id);
                    }
                });
            }
        });
    }
    #endregion
}
