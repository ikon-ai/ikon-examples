namespace Ikon.AI.WebSearching
  interface IWebSearcher : IDisposable, IWebSearcherInfo
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  sealed record SearchConfig
    ctor()
    // Two-letter ISO code, e.g. "us". Amazon cannot apply it and throws when it is set.
    string CountryCode { get; init; }
    // Host or URL prefix the hits must belong to. Amazon and YouTube cannot restrict by site and throw when it is set.
    string InSiteUrl { get; init; }
    // Two-letter ISO code, e.g. "en". Amazon and Bing cannot apply it and throw when it is set.
    string Language { get; init; }
    // SerpApi models return at most 100 per search and reject a higher value.
    int MaxResults { get; init; }
    WebSearcherOutputFormat OutputFormat { get; init; }
    string Query { get; init; }
    // For the whole search request.
    TimeSpan Timeout { get; init; }
  sealed record SearchResult
    ctor()
    string Content { get; init; }
    // Non-empty when the hit was found but its page could not be fetched; Content is then empty. Spider only.
    string Error { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class WebSearcher : IWebSearcher
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebSearcher per call. Defaults to WebSearcherModel.Google; override via model. Each SearchResult exposes .Url/.Title/.Content. Use the constructor + SearchPagesAsync for site-restricted search, country/language targeting, or other SearchConfig fields, or SearchImagesAsync (with an image-capable model) for image search.
    static Task<List<SearchResult>> SearchAsync(string query, WebSearcherModel model = Google, int maxResults = 10, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get; init; }
  enum WebSearcherModel
    Spider
    Jina
    Google
    GoogleImages
    Amazon
    Bing
    BingImages
    Youtube
    // extension methods: WebSearcherModelExtensions{DisplayName}
  static class WebSearcherModelExtensions
    static string DisplayName(this WebSearcherModel model)
  enum WebSearcherOutputFormat
    Text
    Markdown
    Html
