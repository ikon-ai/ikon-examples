namespace Ikon.AI
  class AIException : Exception
  class AITimeoutException : RetryableAIException
    ctor(TimeSpan configuredTimeout, string targetName)
    TimeSpan ConfiguredTimeout { get; }
    string TargetName { get; }
  // When a result's ResultKind is ResultKind.Url the payload lives behind a signed download link valid for roughly one hour; GetDataAsync returns the bytes either way, downloading transparently when needed.
  static class AssetOutputs
    static Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    static Task<byte[]> GetDataAsync(this IResultPayload result, CancellationToken cancellationToken = default)
  // The name selects the model in the category's string-based APIs (e.g. new LLM("my-model")). An empty ApiKey means the endpoint needs no authentication header.
  abstract class CustomModel
    string ApiKey { get; init; }
    // Defaults to Name when left unset.
    string ApiModelName { get; init; }
    // The full URL including the API path, e.g. http://localhost:8000/v1/chat/completions.
    required string EndpointUrl { get; init; }
    // Must not collide with a built-in model name and must not contain dots or whitespace.
    required string Name { get; init; }
  // Register a model at app startup, then select it by name anywhere a model name string is accepted:
  // CustomModels.Instance.Register(new CustomLLMModel
  // {
  //     Name = "my-model",
  //     EndpointUrl = "http://localhost:8000/v1/chat/completions",
  //     Api = CustomLLMApi.OpenAICompletions,
  //     ApiKey = "sk-...",
  //     ContextWindowSize = 32768,
  // });
  //
  // var reply = await Emerge.AskAsync("Hello", "my-model");
  // Custom models always execute in the local process — calls never go through the Ikon RPC mechanism. Usage is reported with a .user suffix and billed as a flat per-request fee instead of per-token provider pricing. The registry is async-local (like CredentialStorage): register models on the main flow at startup, before spawning parallel work, so every flow sees them. Registering the same name again replaces the previous registration; instances constructed before the replacement keep the configuration they were created with.
  sealed class CustomModels : AsyncLocalInstance<CustomModels>
    ctor()
    bool IsRegistered(string name)
    void Register(CustomLLMModel model)
    void Register(CustomEmbeddingModel model)
    void Register(CustomRerankModel model)
    void Register(CustomClassificationModel model)
    // Removes the name from every category it is registered in; true when at least one registration was removed.
    bool Unregister(string name)
  interface IResultPayload
    byte[]? Data { get; }
    ResultKind Kind { get; }
    string? Url { get; }
  // Transient (network blip, server restart, flaky link) and therefore retryable — the RPC layer retries with a forced reconnect, and exhausted attempts still surface as retryable.
  sealed class IkonServerConnectException : RetryableAIException
  // A reference clip for prompt-driven audio editing: the model preserves this clip's timing and structure while the prompt re-styles it. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record InputAudio
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string? MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output adheres to this reference, in [0, 1]; higher keeps the original melody/timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
    string? Url { get; init; }
  // Supply the image exactly one way: inline via Data (with MimeType), by Url, or by AssetUri — all consumers resolve the asset to a URL. Type, Strength, and MaskDilution apply only to image-editing/inpainting models; depth, segmentation, mesh, and video generation ignore them.
  sealed record InputImage
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    double? MaskDilution { get; init; }
    string? MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
  enum InputImageType
    Normal
    Mask
  // A reference clip for video generation: footage the model is shown rather than asked to invent, addressed from the prompt the way reference images are. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically). Providers impose their own length and size limits.
  sealed record InputVideo
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  static class ModelFailure
    static ModelFailureKind Classify(Exception exception)
  // Unlike the retryable/non-retryable split, which answers "should this call be tried again" while saying nothing about the cause, this answers "what does the failure say about the model" — which is what decides whether a human has to act.
  enum ModelFailureKind
    // Callers that gate on this should treat it as a real failure: an unrecognised error is far more likely to be a genuine defect than a benign one.
    Unknown
    // Transport error, timeout, throttling or a provider-side fault; says nothing about whether the model is healthy.
    Transient
    // The model has been removed, renamed or retired and the configuration has to be updated.
    Unavailable
    // Missing or rejected credentials, exhausted credits, or a quota that is not a transient rate limit. An operator has to act, but nothing is wrong with the model or the code.
    AccessDenied
    // No content, an unusable tool call, or output that failed validation. Non-deterministic by nature and often not reproducible on the next call.
    Quality
  enum ModelRegion
    Global
    Eu
    EuNorth
    EuWest
    EuCentral
    EuSouth
    Us
    UsEast
    UsWest
  class NonRetryableAIException : AIException
  // An image produced by an analysis model (depth map, segmentation mask, preview). Kind tells how it was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record OutputImage : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  class RegionNotSupportedException : NonRetryableAIException
  // With Auto the payload stays inline in-process; only when the result is returned from a remotely hosted AI function is it uploaded to a short-lived asset URL, and then only if it exceeds an internal size threshold (a few MB), keeping the protocol message small. Url always uploads, in any context. Check the result's ResultKind field to see which delivery was used.
  enum ResultDelivery
    Auto
    Url
  // Data guarantees the result's Data is non-null; Url guarantees its Url is non-null. Call result.GetDataAsync() (AssetOutputs.GetDataAsync) to get the bytes either way.
  enum ResultKind
    Data
    Url
  class RetryableAIException : AIException
