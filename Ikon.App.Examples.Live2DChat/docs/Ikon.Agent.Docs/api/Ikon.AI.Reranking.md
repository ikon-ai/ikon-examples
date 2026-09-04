namespace Ikon.AI.Reranking
  enum CustomRerankApi
    Cohere
    Jina
    Voyage
    Together
  sealed class CustomRerankModel : CustomModel
    ctor()
    required CustomRerankApi Api { get; init; }
  interface IReranker : IDisposable
    // Returns items ordered most relevant first; RerankItem.Index is the document's position in RerankerConfig.Documents.
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
  sealed record RerankItem
    ctor()
    int Index { get; init; }
    double Score { get; init; }
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    JinaReranker35
    VoyageRerank25
    VoyageRerank25Lite
    // Not directly usable — select custom models (see CustomModels) by their registered name string.
    Custom
  static class RerankModelExtensions
    static string DisplayName(this RerankModel model)
  sealed class Reranker : IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Reranker per call. Defaults to RerankModel.CohereRerank4Fast; override via model. Pass topN to cap returned items (0 returns all). Each RerankItem carries the document's original .Index and relevance .Score, ordered most relevant first. Use the constructor + RerankAsync for a custom timeout or reusing one instance across many queries.
    static Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, RerankModel model = CohereRerank4Fast, int topN = 0, CancellationToken cancellationToken = default)
  sealed record RerankerConfig
    ctor()
    List<string> Documents { get; init; }
    string Query { get; init; }
    // Scaled up internally with the document count.
    TimeSpan Timeout { get; init; }
    // Caps how many items are returned; 0 returns all.
    int TopN { get; init; }
