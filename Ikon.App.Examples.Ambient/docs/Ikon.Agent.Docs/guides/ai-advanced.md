# AI Advanced

## AI Advanced Features

> **For LLM calls, chatbots, and conversation history, always use `Emerge.Run<T>()` from the emergence guide.** This section covers low-level infrastructure that most developers don't need directly.

### AI Database

Database utilities for AI context — SQL connectivity and schema extraction (`Ikon.AI.Database`: Trino/Postgres/Sqlite/BigQuery connections, text-to-SQL tooling), plus vector stores and semantic search infrastructure (`Ikon.AI.Storage`).

Refer to generated API docs for full details.

---

# Ikon.AI Public API
namespace Ikon.AI.Database
  class DatabaseColumnInfo
    ctor()
    string ColumnName { get; set; }
    string DataType { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string? ForeignKeyColumnName { get; set; }
    string? ForeignKeyTableName { get; set; }
    bool? IsForeignKey { get; set; }
    bool? IsPrimaryKey { get; set; }
    List<string>? Values { get; set; }
  // For app code prefer the typed factories (Trino, Postgres, Sqlite, BigQuery), passing the password from app.Secrets. CreateAsync instead reads every connection field from environment variables or space secrets, for shared pipelines.
  class DatabaseConnection : IDisposable
    string BigQueryDataset { get; set; }
    string BigQueryProjectId { get; set; }
    DatabaseType DatabaseType { get; set; }
    DbConnection DbConnection { get; set; }
    static DatabaseConnection BigQuery(string projectId, string dataset)
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
    // Disposes the owned DbConnection — a pooled connection returns to its pool. Wrap per-request use in using; without it every construction leaks a live connection until the pool is exhausted.
    void Dispose()
    static DatabaseConnection Postgres(string host, int port, string database, string user, string password)
    static DatabaseConnection Sqlite(string path)
    static DatabaseConnection Trino(string host, int port, string catalog, string user, string password)
  class DatabaseConnection.Config
    ctor()
    string? EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret? SpaceSecret { get; set; }
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get; set; }
    string SpaceId { get; set; }
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get; set; }
    List<string>? ExampleQuestions { get; set; }
    string? SqlCteCommand { get; set; }
    List<DatabaseTableInfo> Tables { get; set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
  // Configuration for database info extraction.
  class DatabaseInfoExtractor.Config
    ctor()
    // Regex patterns matched against the three-part schema.table.column name.
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    // When empty the default depends on the database type (e.g. public for PostgreSQL).
    List<string>? Schemas { get; set; }
    // Regex patterns for table names to exclude.
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    // Regex patterns matched against schema.table (or just table); an empty/null list includes all.
    List<string>? TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
  class DatabaseTableInfo
    ctor()
    List<DatabaseColumnInfo> Columns { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string TableName { get; set; }
  enum DatabaseType
    Unknown
    PostgreSql
    Sqlite
    BigQuery
    Trino
  sealed class ResultCell
    ctor(string column, object? value)
    string Column { get; }
    object? Value { get; }
  sealed class ResultRow
    ctor(IReadOnlyList<ResultCell> cells)
    IReadOnlyList<ResultCell> Cells { get; }
    // Value of the named column, or null. Null is returned both for a genuine SQL NULL and for a column that is not present — use TryGetValue to tell the two apart.
    object? this[string column] { get; }
    // Looks up a column by name. Returns false only when no such column exists; a column present but holding SQL NULL returns true with value set to null.
    bool TryGetValue(string column, out object? value)
  sealed class ResultSet
    ctor(IReadOnlyList<string> columns, IReadOnlyList<ResultRow> rows, int limitedRowCount, int totalRowCount, CultureInfo culture)
    IReadOnlyList<string> Columns { get; }
    int LimitedRowCount { get; }
    IReadOnlyList<ResultRow> Rows { get; }
    int TotalRowCount { get; }
    static Task<ResultSet> Create(DbDataReader reader, int maxRows, CultureInfo? culture = null, List<string>? columnNames = null)
    string ToCsv()
    string ToJson()
    string ToMarkdown()
  static class SqlValidator
    // Best-effort guard that rejects LLM-authored SQL carrying a write/side-effect keyword or a table outside allowedTables. It is a keyword blocklist plus a FROM/JOIN allowlist, NOT a dialect-aware parser, so it does not prove the statement is side-effect free. Where the query runs against real data, back it with a read-only transaction or role.
    static void ValidateReadOnly(string sql, IReadOnlySet<string> allowedTables)

namespace Ikon.AI.Storage
  class KeywordIndex
    ctor()
    Task AddAsync(string word, string link)
    static KeywordIndex Deserialize(Stream stream)
    Task InitializeAsync()
    void RemoveTooCommonTerms(double threshold = 0.5, int minDocumentCount = 5)
    List<KeywordSearchResult> Search(string words)
    void Serialize(Stream stream)
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
    // Brute-force in-process store. The default — no external dependency.
    InMemory
    // Postgres + pgvector, with an HNSW index. Scales past what an in-RAM linear scan can.
    PgVector
  // Chooses the backing store for a VectorDatabase. The default (or a null config) keeps the in-memory store, so existing callers are unaffected; pass one with VectorStoreBackend.PgVector to persist and scale.
  sealed class VectorStoreConfig
    ctor()
    VectorStoreBackend Backend { get; init; }
    // Opens a fresh connection for a pgvector operation (each op opens and disposes its own, as PgVectorCorpus does), so the call belongs inside the factory: () => DatabaseConnection.Postgres(...).DbConnection. Required when Backend is VectorStoreBackend.PgVector.
    Func<DbConnection>? ConnectionFactory { get; init; }
    // Table-name prefix, so several vector databases can share one Postgres database.
    string TablePrefix { get; init; }
