namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static async Task<int> DocWebSearchOneShotAsync()
    {
        var count = 0;

        #region example:web-search-one-shot
        var results = await WebSearcher.SearchAsync("latest AI news", maxResults: 5);  // Google by default
        foreach (var result in results) { /* result.Title, result.Url, result.Content */ }
        #endregion

        foreach (var result in results)
        {
            count += result.Title.Length;
        }

        return count;
    }

    private static async Task<int> DocEmbeddingsOneShotAsync()
    {
        #region example:embeddings-one-shot
        var embeddings = await EmbeddingGenerator.EmbedAsync(["Hello world", "Goodbye"]);  // OpenAI3Small (cheap+fast) by default
        // embeddings[0] is float[] vector
        #endregion

        return embeddings.Count;
    }

    private static async Task DocOtherDataServicesAsync(
        string userText, byte[] documentBytes, byte[] docxBytes, IReadOnlyList<string> documents, string query)
    {
        #region example:other-data-services
        var page = await WebScraper.ScrapeAsync("https://example.com");          // page.Content is Markdown
        var moderation = await Classifier.ClassifyAsync(userText);               // moderation.IsFlagged
        var ocr = await OCR.AnalyzeAsync(documentBytes);                         // ocr.Text
        var pdf = await FileConverter.ConvertToPdfAsync(docxBytes, "report.docx");
        var ranked = await Reranker.RerankAsync(documents, query);               // ranked[0].Index into documents
        #endregion

        _ = (page, moderation, ocr, pdf, ranked);
    }

    private static async Task DocWebSearchConfigAsync()
    {
        #region example:web-search-config
        using var searcher = new WebSearcher(WebSearcherModel.Google);
        var results = await searcher.SearchPagesAsync(new SearchConfig { Query = "latest AI news", InSiteUrl = "https://example.com" });
        #endregion

        _ = results;
    }
}
