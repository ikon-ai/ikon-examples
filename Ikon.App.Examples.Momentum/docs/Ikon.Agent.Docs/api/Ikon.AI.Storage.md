namespace Ikon.AI.Storage
  struct KeywordSearchResult
    ctor(string link, float score)
    string Link
    float Score
  enum Metric
    DotProduct
    CosineSimilarity
    EuclideanDistance
  struct Result<T>
    ctor(int key, float score, T value)
    int Key
    float Score
    T Value
  class VectorDatabase
    ctor(VectorStoreConfig? config = null)
    Task CreateCollectionAsync(string collectionName, EmbeddingModel model)
    Task<int> GetDataItemCountAsync(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)
  enum VectorStoreBackend
    InMemory
    PgVector
  // The default (or a null config) keeps the in-memory store, so existing callers are unaffected; pass one with VectorStoreBackend.PgVector to persist and scale.
  sealed class VectorStoreConfig
    ctor()
    VectorStoreBackend Backend { get; init; }
    // Each operation opens and disposes its own connection, so the call belongs inside the factory: () => DatabaseConnection.Postgres(...).DbConnection. Required when Backend is VectorStoreBackend.PgVector.
    Func<DbConnection>? ConnectionFactory { get; init; }
    string TablePrefix { get; init; }
