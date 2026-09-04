<!-- mined-from: Ikon.App.Patterns -->
# Document Search — OCR, Retrieve, Rerank

Three stages, each doing what the next cannot. **OCR** turns a scanned page into text.
**`Retriever`** finds candidates cheaply and wide. **`Reranker`** orders the shortlist properly.

Running retrieval alone is imprecise; running a rerank model over everything is too slow to be
wide. So retrieval is **recall-first** — ask for more than you need — and rerank is
**precision-second** over the shortlist.

## When to use

Question-answering over documents a user uploaded, a knowledge base, a contract archive. When the
document is one long structured thing and the answer is a few sections of it,
`tree-search-over-long-document` navigates rather than embeds and is usually cheaper.

## Notes

- **`using Ikon.AI.Reranking;` and `using Ikon.AI.Retrieving;` are required** — nested namespaces
  are not imported by their parent. `Ikon.AI.OCR` *is* in `GlobalUsings.cs`.
- `DocumentType` has one value, `General`. There is no `Pdf` member — the format is inferred.
- **`MaxPagesSupported` and `MaxDocumentSizeBytes` are `0` when the model publishes no limit.**
  Never read that as a zero budget. Where there is a limit, a longer document is split across
  requests with `OCRConfig.Pages`.
- The size limit is only checked for `Data`; the size behind a `Url` or `AssetUri` is not known
  before the request, so a large document can still be refused by the provider.
- **`RerankItem` carries the original `Index` and a `Score`, not the text.** The result is a
  permutation of what you passed in — keep the list to index back into, or the ordering is
  meaningless.
- `topN: 0` returns everything reordered rather than nothing.
- `Retriever.InitializeAsync` takes either a data directory or a list of `AssetUri`s, and builds
  the embedding index — that is the expensive step, so it belongs at startup or behind an upload,
  never in the search handler.
- `AnalyzeDocumentStreamingAsync` yields page by page, for showing progress on a long document
  rather than waiting for all of it.

## Snippet

```csharp
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
```

## See also

- `tree-search-over-long-document` — navigating one document instead of embedding a corpus.
- `web-research` — the same shape over live search results.
