// Ikon.AI.OCR is in GlobalUsings; Ikon.AI.Reranking and Ikon.AI.Retrieving are not — nested
// namespaces are not imported by their parent, so an app doing retrieval adds these itself.
using Ikon.AI.Reranking;
using Ikon.AI.Retrieving;
using System.Globalization;
using Dapper;
using Ikon.Pipelines.Public.Processors.Pdf;

namespace Ikon.App.Patterns.Patterns;

// Pattern: document-search-with-rerank — see docs/patterns/document-search-with-rerank.md.
// The example region below is the canonical body the doc extracts.
internal sealed class DocumentSearchWithRerank(IAppBase app) : IPatternDemo
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

            // A real library answer needs PDFs in the app's database, so this sample shows how an answer renders.
            col.Text([Text.Label, "mt-4"], "Document library answer (sample)");
            RenderAnswer((UIView)col, new CitedAnswer(
                "Acme renews automatically for another year unless notice is given 90 days before the end [1]. Borealis ends on its end date [2].",
                [new Passage("acme-services.pdf", 7, ""), new Passage("borealis-licence.pdf", 3, "")]));
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

    #region example:pattern-document-search-library
    private sealed record Passage(string FileName, int Page, string Text);
    private sealed record CitedAnswer(string Text, IReadOnlyList<Passage> Sources);

    // Run once from app.OnStarting. The table has one row per page. vector(1536) is the vector length
    // of OpenAI3Small, the EmbedAsync default, and the HNSW index keeps the nearest-page search fast
    // as the table grows.
    private async Task CreatePassagesTableAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS passages (
                id bigserial PRIMARY KEY,
                file_name text NOT NULL,
                page int NOT NULL,
                text text NOT NULL,
                embedding vector(1536) NOT NULL);
            CREATE INDEX IF NOT EXISTS passages_by_embedding ON passages USING hnsw (embedding vector_cosine_ops);
            """);
    }

    // pgvector reads a vector from its text form, [0.1,0.2,...]
    private static string ToVector(float[] vector) =>
        "[" + string.Join(",", vector.Select(value => value.ToString(CultureInfo.InvariantCulture))) + "]";

    // Stores one uploaded PDF. A page's text comes from its text layer, or from OCR for a scanned page
    // that has none. Each page gets one embedding and one row, so an answer can cite its page.
    private async Task IngestAsync(string fileName, byte[] pdf)
    {
        var pages = new List<Passage>();

        using (var document = PdfDocument.Load(pdf))
        {
            for (var i = 0; i < document.PageCount; i++)
            {
                using var page = document.GetPage(i);
                var text = page.GetText();

                if (string.IsNullOrWhiteSpace(text))
                {
                    using var single = new MemoryStream();
                    page.CreateCopy(single);
                    text = (await OCR.AnalyzeAsync(single.ToArray())).Text;
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    pages.Add(new Passage(fileName, i + 1, text.Trim()));
                }
            }
        }

        var vectors = await EmbeddingGenerator.EmbedAsync(pages.Select(page => page.Text).ToList(),
            type: EmbeddingType.Document, timeout: TimeSpan.FromSeconds(60));

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();
        await db.ExecuteAsync("DELETE FROM passages WHERE file_name = @fileName", new { fileName }, transaction);

        for (var i = 0; i < pages.Count; i++)
        {
            await db.ExecuteAsync("""
                INSERT INTO passages (file_name, page, text, embedding)
                VALUES (@FileName, @Page, @Text, @embedding::vector)
                """, new { pages[i].FileName, pages[i].Page, pages[i].Text, embedding = ToVector(vectors[i]) }, transaction);
        }

        await transaction.CommitAsync();
    }

    // The database finds the nearest pages, the reranker keeps the most relevant ones, and the answer
    // cites them by number, so each claim maps to a file and a page.
    private async Task<CitedAnswer> AskLibraryAsync(string question)
    {
        var query = (await EmbeddingGenerator.EmbedAsync([question], type: EmbeddingType.Query))[0];

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        var candidates = (await db.QueryAsync<Passage>("""
            SELECT file_name AS FileName, page AS Page, text AS Text FROM passages
            ORDER BY embedding <=> @query::vector LIMIT 40
            """, new { query = ToVector(query) })).ToList();

        if (candidates.Count == 0)
        {
            return new CitedAnswer("There are no documents yet.", []);
        }

        var ranked = await Reranker.RerankAsync(candidates.Select(candidate => candidate.Text).ToList(), question, topN: 5);
        var sources = ranked.Select(item => candidates[item.Index]).ToList();
        var numbered = string.Join("\n\n", sources.Select((source, i) => $"[{i + 1}] {source.FileName}, page {source.Page}:\n{source.Text}"));

        var text = await Emerge.AskAsync($"""
            Answer the question from these passages only. After each claim, put the number of the
            passage it comes from in brackets, like [2]. If the passages do not answer it, say so.

            {numbered}

            Question: {question}
            """);
        return new CitedAnswer(text, sources);
    }

    private static void RenderAnswer(UIView view, CitedAnswer answer)
    {
        view.Column(["gap-2"], content: col =>
        {
            // The model writes Markdown (lists, bold), which view.Text would show as raw symbols
            col.Markdown(answer.Text);

            for (var i = 0; i < answer.Sources.Count; i++)
            {
                var source = answer.Sources[i];
                col.Text([Text.Caption], key: $"{source.FileName}:{source.Page}", text: $"[{i + 1}] {source.FileName}, page {source.Page}");
            }
        });
    }
    #endregion

    #region example:pattern-document-search-shipped
    // Run from app.OnStarting, once the passages table exists. The handbook ships in the app's data/
    // folder. Its sections are embedded once per version of its text, so a restart, a new instance
    // or a deploy of the same text embeds nothing.
    private async Task IndexHandbookAsync()
    {
        const string fileName = "handbook.md";
        var text = await app.Files.Data.ReadTextAsync(fileName);
        var version = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text)));

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("CREATE TABLE IF NOT EXISTS indexed_documents (file_name text PRIMARY KEY, version text NOT NULL)");
        await using var transaction = await db.BeginTransactionAsync();

        // Instances that start together wait here, and all but the first find the text indexed
        await db.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext(@fileName))", new { fileName }, transaction);
        var indexed = await db.ExecuteScalarAsync<string?>(
            "SELECT version FROM indexed_documents WHERE file_name = @fileName", new { fileName }, transaction);

        if (indexed == version)
        {
            return;
        }

        // One passage per "## " section, numbered in the page column so an answer cites the section
        var sections = ("\n" + text).Split("\n## ")
            .Select((section, index) => index == 0 ? section.Trim() : "## " + section.Trim())
            .Where(section => section.Length > 0)
            .Select((section, index) => new Passage(fileName, index + 1, section))
            .ToList();

        var vectors = await EmbeddingGenerator.EmbedAsync(sections.Select(section => section.Text).ToList(),
            type: EmbeddingType.Document, timeout: TimeSpan.FromSeconds(60));

        await db.ExecuteAsync("DELETE FROM passages WHERE file_name = @fileName", new { fileName }, transaction);

        for (var i = 0; i < sections.Count; i++)
        {
            await db.ExecuteAsync("""
                INSERT INTO passages (file_name, page, text, embedding)
                VALUES (@FileName, @Page, @Text, @embedding::vector)
                """, new { sections[i].FileName, sections[i].Page, sections[i].Text, embedding = ToVector(vectors[i]) }, transaction);
        }

        await db.ExecuteAsync("""
            INSERT INTO indexed_documents (file_name, version) VALUES (@fileName, @version)
            ON CONFLICT (file_name) DO UPDATE SET version = EXCLUDED.version
            """, new { fileName, version }, transaction);
        await transaction.CommitAsync();
    }
    #endregion
}
