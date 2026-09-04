namespace Ikon.AI.Embeddings
  enum CustomEmbeddingApi
    OpenAI
    Cohere
    Mistral
    Google
    Jina
    Voyage
  sealed class CustomEmbeddingModel : CustomModel
    ctor()
    required CustomEmbeddingApi Api { get; init; }
    required int EmbeddingVectorSize { get; init; }
    // Larger batches are split automatically. Defaults to 96.
    int MaxInputCount { get; init; }
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an EmbeddingGenerator per call. Defaults to EmbeddingModel.OpenAI3Small and EmbeddingType.Generic; override the model via model, and pass an explicit EmbeddingType when embedding documents vs. queries for asymmetric retrieval. Returns one float[] per input, in input order. Use the constructor + GenerateEmbeddingsAsync for per-request batch caps or a custom timeout.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities : IEmbeddingGeneratorInfo
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  sealed record EmbeddingGeneratorConfig
    ctor()
    List<string> Inputs { get; init; }
    // Per-request batch cap; larger input lists are split into batches of this size. 0 means the model's maximum.
    int MaxInputCount { get; init; }
    // Per-request; scaled up internally with the batch size.
    TimeSpan Timeout { get; init; }
    EmbeddingType Type { get; init; }
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get; init; }
    string Embedding { get; init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get; init; }
    EmbeddingModel Model { get; init; }
    EmbeddingType Type { get; init; }
    static Task<EmbeddingItem> CreateAsync(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = default)
    static Task<EmbeddingItem> CreateAsync(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
  enum EmbeddingModel
    OpenAIAda2
    OpenAI3Small
    OpenAI3Large
    CohereEmbed4
    MistralEmbed
    CodestralEmbed
    GeminiEmbedding1
    GeminiEmbedding2
    GoogleTextEmbedding5
    GoogleTextMultilingualEmbedding2
    JinaEmbeddings3
    JinaEmbeddings4
    JinaEmbeddings5TextSmall
    JinaEmbeddings5TextNano
    JinaEmbeddings5OmniSmall
    JinaEmbeddings5OmniNano
    Voyage35
    Voyage35Lite
    Voyage4
    Voyage4Lite
    Voyage4Large
    VoyageCode3
    VoyageCode4
    // Not directly usable — select custom models (see CustomModels) by their registered name string.
    Custom
  static class EmbeddingModelExtensions
    static string DisplayName(this EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable, IEmbeddingGeneratorInfo
    // Returns one vector per input, in input order.
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IEmbeddingGeneratorInfo
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
  static class VectorMath
    // embeddings: List of embeddings (each as a float array)
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // throws ArgumentException: The vectors differ in length, or either vector has zero magnitude (e.g. a blank or failed embedding), for which cosine similarity is undefined. Guard degenerate vectors before calling when scoring in a loop.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Neighbors are ranked by Euclidean distance.
    // embeddings: List of embeddings (each as a float array)
    // k: Number of neighbors to retrieve for each embedding
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    static float GetMagnitude(ReadOnlySpan<float> vector)
  readonly struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }
