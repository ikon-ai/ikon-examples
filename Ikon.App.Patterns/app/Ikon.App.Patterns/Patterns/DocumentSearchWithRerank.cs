// Ikon.AI.OCR is in GlobalUsings; Ikon.AI.Reranking and Ikon.AI.Retrieving are not — nested
// namespaces are not imported by their parent, so an app doing retrieval adds these itself.
using Ikon.AI.Reranking;
using Ikon.AI.Retrieving;

namespace Ikon.App.Patterns.Patterns;

// Pattern: document-search-with-rerank — see docs/patterns/document-search-with-rerank.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class DocumentSearchWithRerank : IPatternDemo
{
    public string Slug => "document-search-with-rerank";
    public string Title => "Document search: OCR, retrieve, rerank";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-document-search-with-rerank
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
            foreach (var hit in _hits)
            {
                col.Text(["text-sm"], key: hit, text: hit);
            }
        });
    }
    #endregion
}
