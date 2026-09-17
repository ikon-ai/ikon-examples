namespace Ikon.Pipeline
  sealed class PipelineStatus
    ctor()
    int DuplicateItemCount { get; set; }
    TimeSpan Duration { get; set; }
    int ErrorLogCount { get; set; }
    bool HasCompleted { get; set; }
    bool HasFaulted { get; set; }
    // Inputs that were found but never reached the pipeline (unreadable file, corrupt archive, state error). Counts against the failure threshold together with ProcessFailureCount and OutputFailureCount.
    int InputFailureCount { get; set; }
    int InputItemCacheHits { get; set; }
    int InputItemCacheMiss { get; }
    int InputItemCount { get; set; }
    int InvalidItemCount { get; set; }
    // Output items that could not be written to a configured output. Counts against the failure threshold together with ProcessFailureCount and InputFailureCount.
    int OutputFailureCount { get; set; }
    int OutputItemCacheHits { get; set; }
    int OutputItemCacheMiss { get; }
    int OutputItemCount { get; set; }
    int ProcessFailureCount { get; set; }
    int ProcessRetryCount { get; set; }
    int ProcessedItemCacheHits { get; set; }
    int ProcessedItemCacheMiss { get; }
    int ProcessedItemCount { get; set; }
    string ProcessingId { get; set; }
    DateTime StartTime { get; set; }
    // Every item lost anywhere in the run: InputFailureCount + ProcessFailureCount + OutputFailureCount. This is the number the failure threshold is compared against.
    int TotalFailureCount { get; }
    Dictionary<string, double> Usages { get; set; }
    int WarningLogCount { get; set; }
    bool WasCancelled { get; set; }
  sealed class ProcessorAttribute : Attribute
    ctor(string? id = null, int version = 1, int maxParallelism = 0, int maxRetries = -1, bool isRemote = false, bool skipCache = false, bool allowDuplicates = true, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Defaults to true (duplicates preserved). Set to false to enable deduplication based on content hash and group id.
    bool AllowDuplicates { get; set; }
    string? Id { get; set; }
    bool IsRemote { get; set; }
    int MaxParallelism { get; set; }
    // Maximum retries on failure. The default of -1 means "not set" and falls back to the pipeline defaults; an explicit 0 disables retries for this processor.
    int MaxRetries { get; set; }
    // When left null (not set) the pipeline defaults are used; an explicitly-empty array means retry nothing, matching the pipeline-level default semantics.
    Type[]? RetryableExceptionTypes { get; set; }
    bool SkipCache { get; set; }
    ProcessorTags[] Tags { get; set; }
    // Feeds the processor cache hash; bump the version to invalidate previously cached outputs.
    int Version { get; set; }
  enum ProcessorTags
    Gpu
