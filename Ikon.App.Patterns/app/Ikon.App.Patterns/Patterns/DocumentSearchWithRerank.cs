// Ikon.AI.OCR is in GlobalUsings; Ikon.AI.Reranking and Ikon.AI.Retrieving are not — nested
// namespaces are not imported by their parent, so an app doing retrieval adds these itself.
using Ikon.AI.Reranking;
using Ikon.AI.Retrieving;

namespace Ikon.App.Patterns.Patterns;

// Pattern: document-search-with-rerank — see docs/patterns/document-search-with-rerank.md.
// The example region below is the canonical body the doc extracts.
internal sealed class DocumentSearchWithRerank : IPatternDemo
{
    public string Slug => "document-search-with-rerank";
    public string Title => "Document search: OCR, retrieve, rerank";
    public string Category => "Web & data";

    // The gallery's embedding model is a mock that maps each text to its own random vector, so a
    // passage is found only by a question embedded exactly; each sample passage is therefore
    // indexed under every sample question, and the reranker orders the three a question finds.
    private static readonly string[] SampleQuestions =
    [
        "How long is the refund window?",
        "Who signs off on a contract change?",
        "Where are scanned invoices kept?",
    ];

    private static readonly (string Name, string Text)[] SamplePassages =
    [
        ("refunds", "Refunds are accepted within 30 days of purchase with the original receipt."),
        ("contracts", "Any change to a signed contract needs written approval from the account owner."),
        ("invoices", "Scanned invoices are archived in the finance folder for seven years."),
    ];

    private readonly Reactive<string> _corpusState = new("empty");

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            if (_corpusState.Value == "ready")
            {
                col.Text(["text-xs text-white/70"], text: "Sample documents indexed. Ask one of: " + string.Join(" / ", SampleQuestions));
            }
            else
            {
                col.Button([Button.OutlineSm, "self-start"],
                    text: _corpusState.Value == "loading" ? "Indexing sample documents…" : "Index sample documents",
                    disabled: _corpusState.Value != "empty",
                    onClick: async () => await SeedCorpusAsync());
            }

            Render(col);
        });
    }

    private async Task SeedCorpusAsync()
    {
        if (_corpusState.Value != "empty")
        {
            return;
        }

        GalleryMocks.Require();
        _corpusState.Value = "loading";

        try
        {
            var directory = Path.Combine(Path.GetTempPath(), "ikon-patterns-document-search", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var vectors = await EmbeddingGenerator.EmbedAsync(SampleQuestions, EmbeddingModel.OpenAI3Small, EmbeddingType.Query);

            foreach (var (name, text) in SamplePassages)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, name + ".txt"), text);

                var items = new List<EmbeddingItem>();

                foreach (var vector in vectors)
                {
                    items.Add(await EmbeddingItem.CreateAsync(vector, name, EmbeddingModel.OpenAI3Small, EmbeddingType.Document, EmbeddingEncoding.Base64));
                }

                await File.WriteAllTextAsync(Path.Combine(directory, name + ".embeddings.json"), JsonSerializer.Serialize(items));
            }

            await _retriever.InitializeAsync(directory, EmbeddingModel.OpenAI3Small);
            await _retriever.WaitForLoadingToEndAsync();
            _corpusState.Value = "ready";
        }
        catch (Exception ex)
        {
            // The state is shared by every client, so a failed seed hands the button back rather
            // than leaving it disabled for everyone; pressing it again retries
            Log.Instance.Warning($"Seeding the document search demo corpus failed, the button is offered again: {ex.Message}");
            _corpusState.Value = "empty";
        }
    }

    #region example:pattern-document-search-with-rerank
    // Indexed once, at startup or behind an upload -- the expensive step never sits in the search handler.
    private readonly Retriever _retriever = new();
    private readonly ClientReactiveList<string> _hits = new();

    /// <summary>
    /// A scanned page is not text until OCR makes it so. The one-shot takes bytes; the config form
    /// is what accepts a URL or an AssetUri, which is the right source for anything large.
    /// </summary>
    private static async Task<string> ExtractAsync(AssetUri document)
    {
        using var ocr = new OCR(OCRModel.AzureDocumentIntelligence);

        // MaxPagesSupported is 0 when the model publishes no limit -- never read that as a zero
        // budget. Where there IS a limit, a longer document is split across requests with Pages.
        var result = await ocr.AnalyzeDocumentAsync(new OCRConfig
        {
            AssetUri = document,
            Pages = ocr.MaxPagesSupported > 0 ? $"1-{ocr.MaxPagesSupported}" : null,
        });

        return result.Text;
    }

    /// <summary>
    /// Retrieval is recall-first and rerank is precision-second: ask the index for MORE than you
    /// need cheaply, then let a rerank model order the shortlist properly. One stage alone is
    /// either imprecise or too slow to run wide.
    /// </summary>
    private async Task SearchAsync(Retriever retriever, string question)
    {
        var links = await retriever.SearchAsync(question, maxLinks: 25);
        var passages = new List<string>();

        foreach (var link in links)
        {
            if (await retriever.GetContentAsync(link) is { } content)
            {
                passages.Add(content.ToString() ?? "");
            }
        }

        if (passages.Count == 0)
        {
            _hits.Clear();
            return;
        }

        // RerankItem carries the ORIGINAL index, not the text -- the ordering is a permutation of
        // what you passed in, so keep the list to index back into.
        var ranked = await Reranker.RerankAsync(passages, question, topN: 5);
        _hits.ReplaceAll(ranked.Select(item => passages[item.Index]));
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            // _hits is per client, so SearchAsync runs from this client's own action callback,
            // where the client scope is active and survives the awaits. A caller with no scope
            // (a timer, an upload completion) captures ReactiveScope.ClientId first and wraps the
            // call in ReactiveScope.Use(new ClientScope(clientId)).
            col.TextField(placeholder: "Ask the documents",
                onSubmit: async question => await SearchAsync(_retriever, question));

            foreach (var hit in _hits)
            {
                col.Text(["text-sm"], key: hit, text: hit);
            }
        });
    }
    #endregion
}
