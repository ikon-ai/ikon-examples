namespace Ikon.AI.WebScraping
  sealed record Cookie
    ctor()
    string Domain { get; init; }
    double ExpirationDate { get; init; }
    bool HostOnly { get; init; }
    bool HttpOnly { get; init; }
    int Id { get; init; }
    string Name { get; init; }
    string Path { get; init; }
    string SameSite { get; init; }
    bool Secure { get; init; }
    bool Session { get; init; }
    string StoreId { get; init; }
    string Value { get; init; }
  sealed record DownloadFileConfig
    ctor()
    string CountryCode { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
  sealed record DownloadFileResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    // Every page the crawl reached, failed ones with PageResult.Error set; throws when none could be fetched.
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed record MultiPageScrapeConfig
    ctor()
    bool AddGivenUrlsToWhitelist { get; init; }
    // Spider whitelists Urls so nothing else consumes MaxPages; a URL the crawl never returns comes back with PageResult.Error set.
    bool AllowOnlyGivenUrls { get; init; }
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    int DelayMs { get; init; }
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    // Local browser scrapers only; the remote scrapers throw on false.
    bool Headless { get; init; }
    bool IgnoreRobotsTxt { get; init; }
    // No scraper implements this; setting it throws.
    bool IncludeLinkedFiles { get; init; }
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    bool LoadResources { get; init; }
    string Locale { get; init; }
    int MaxDepth { get; init; }
    // The crawl stops here; Spider logs a warning when the site may have more pages.
    int MaxPages { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    // Spider reruns the crawl up to three times for Urls still missing, then throws.
    bool RerunIfGivenUrlsMissing { get; init; }
    // Per page. Local browser scrapers only; Spider bounds a page fetch itself.
    TimeSpan SinglePageTimeout { get; init; }
    // For the whole crawl.
    TimeSpan Timeout { get; init; }
    List<string> UrlBlacklist { get; init; }
    List<string> UrlWhitelist { get; init; }
    List<string> Urls { get; init; }
    bool UseReadability { get; init; }
    bool UseSitemap { get; init; }
    bool UseSitemapOnly { get; init; }
    bool UseStreaming { get; init; }
    // Extra wait after page load. Local browser scrapers only; Spider throws when set.
    TimeSpan WaitAfter { get; init; }
  sealed record PageResult
    ctor()
    string Content { get; init; }
    // Non-empty when a crawl reached the page but could not fetch it; Content is then empty. A crawl whose every page failed throws instead.
    string Error { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed record ScreenshotConfig
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    bool FullPage { get; init; }
    // Local browser scrapers only; the remote scrapers throw on false.
    bool Headless { get; init; }
    int Height { get; init; }
    string JavaScript { get; init; }
    string Locale { get; init; }
    string PlaywrightScript { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    // LocalNodriver only (TikTok captcha solver); the remote scrapers throw when set.
    bool UseCaptchaSolver { get; init; }
    TimeSpan WaitAfter { get; init; }
    int Width { get; init; }
  sealed record ScreenshotResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
  sealed record SinglePageScrapeConfig
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    // Nodriver throws when set.
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    // Local browser scrapers only; the remote scrapers throw on false.
    bool Headless { get; init; }
    // No scraper implements this; setting it throws.
    bool IncludeLinkedFiles { get; init; }
    // Nodriver throws when set.
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    // Jina throws when set; Nodriver's browser always loads resources.
    bool LoadResources { get; init; }
    string Locale { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    // For the whole fetch.
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    // LocalNodriver only (TikTok captcha solver); the remote scrapers throw when set.
    bool UseCaptchaSolver { get; init; }
    // Jina always extracts and throws on false; Nodriver never extracts.
    bool UseReadability { get; init; }
    // Extra wait after page load. Browser scrapers only; Spider and Jina fetch without one.
    TimeSpan WaitAfter { get; init; }
  sealed class WebScraper : IWebScraper
    ctor(string modelName)
    ctor(WebScraperModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(WebScraperModel model, IReadOnlyList<ModelRegion>? regions)
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
    void Dispose()
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<PageResult> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebScraper per call. Defaults to WebScraperModel.Jina; override via model. Returns the page as Markdown in .Content plus .Title/.Url. Use the constructor + ScrapeSinglePageAsync for output format/cookies/JS or other fields, or ScrapeMultiplePagesAsync/TakeScreenshotAsync/DownloadFileAsync for crawling, screenshots, and downloads.
    static Task<PageResult> ScrapeAsync(string url, WebScraperModel model = Jina, CancellationToken cancellationToken = default)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsFileDownload { get; init; }
    bool SupportsMultiPageScraping { get; init; }
    bool SupportsScreenshotting { get; init; }
    bool SupportsSinglePageScraping { get; init; }
  enum WebScraperModel
    Spider
    Jina
    LocalPuppeteer
    LocalNodriver
    LocalPlaywright
    // extension methods: WebScraperModelExtensions{DisplayName}
  static class WebScraperModelExtensions
    static string DisplayName(this WebScraperModel model)
  enum WebScraperOutputFormat
    Text
    Markdown
    Html
