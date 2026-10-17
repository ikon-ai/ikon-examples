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

    private static async Task<string> DocModerationByLabelAsync(string message)
    {
        #region example:moderation-by-label
        ClassificationLabel[] severe =
            [ClassificationLabel.SafetyHateSpeech, ClassificationLabel.SafetySelfHarm, ClassificationLabel.SafetySexualContent, ClassificationLabel.SafetyViolence];

        var moderation = await Classifier.ClassifyAsync(message);

        if (moderation.Details.Any(detail => detail.IsFlagged && severe.Contains(detail.Label)))
        {
            return "held for a moderator";
        }

        var harassment = moderation.Details.FirstOrDefault(detail => detail.Label == ClassificationLabel.SafetyHarassment);

        // A NaN score compares false, so an unscored label is judged by IsFlagged alone.
        if (harassment is not null && (harassment.IsFlagged || harassment.Score > 0.3))
        {
            var tone = await Emerge.AskAsync<ToneCheck>($"Is this message a personal attack, or only blunt? Message: {message}");

            return tone.IsPersonalAttack ? "held for a moderator" : "posted";
        }

        return "posted";
        #endregion
    }

    private async Task<string> DocWebScreenshotAsync(string competitorUrl)
    {
        #region example:web-screenshot
        var scraper = new WebScraper(WebScraperModel.Jina);
        var shot = await scraper.TakeScreenshotAsync(new ScreenshotConfig { Url = competitorUrl, Width = 1280, Height = 800 });
        var path = $"screenshots/{DateTime.UtcNow:yyyyMMdd-HHmm}.png";
        await app.Files.Data.WriteBytesAsync(path, shot.Data, shot.MimeType);
        #endregion
        return path;
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

public sealed record ToneCheck(bool IsPersonalAttack);
