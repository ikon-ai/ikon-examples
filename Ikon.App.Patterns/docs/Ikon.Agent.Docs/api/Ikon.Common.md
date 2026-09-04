namespace Ikon.Common
  class AsyncLocalInstances
    bool AsyncLocalModeInitialized { get; }
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
    void Remove(object owner)
    void Restore(object owner)
    bool TryRemove(object owner)
    bool TryRestore(object owner)
    static readonly AsyncLocalInstances Instance
  // Read-only configuration handed to the app at startup and exposed through IAppBase.Databases: look a database up by Name or Type and open it (see IAppBase.Database or AppDatabaseConnection.Create). An app never constructs one — databases are created with ikon app db create (or the Portal) and provisioned by the backend.
  sealed record DatabaseConnectionInfo
    ctor()
    // Ready-to-use ADO.NET connection string, pointing at the app's own database through the connection pooler. It carries credentials — never log it or surface it to a client.
    string ConnectionString { get; init; }
    // The lookup key when an app has more than one database, as given to ikon app db create --name.
    string Name { get; init; }
    // "postgres" is the only engine the platform provisions today, and AppDatabaseConnection.Create throws NotSupportedException for anything else. Match on it rather than assuming.
    string Type { get; init; }
  // Derives from DescriptionAttribute so that every reader of the BCL attribute — Tool.Of lambda parameters, function registration — also picks this one up, and an app that has global using Ikon.Common; can write [Description] anywhere the BCL one is accepted. Adding using System.ComponentModel; next to it makes the bare name ambiguous (CS0104); qualify one of them.
  class DescriptionAttribute : DescriptionAttribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    object? Example { get; }
    // Not honoured by any schema generator: whether a property is required is derived from its nullability, and the OpenAI dialect lists every property as required regardless. Kept for source compatibility.
    RequiredStatus IsRequired { get; }
    int MinArrayItems { get; }
  enum EndpointProtocol
    Tcp
    Tls
    Udp
  sealed class IkonLoggerProvider : ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException.
    static void RunParallel(this Task task, Action<Exception>? onException = null)
  static class MimeTypes
    // Registers a mime type for a file extension. The extension is normalized (leading dot stripped, lower-cased) so it matches what the lookups use, and the write is locked against the concurrent readers. Argument order is (extension, mimeType), matching the rest of the type.
    static void AddOrUpdate(string extension, string mimeType)
    // Returns the file extension registered for a mime type. When several extensions map to the same mime type, the first one in registration (insertion) order is returned. When no extension matches, the default extension (DefaultExtension, "bin") is returned.
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
    // The negation of IsText: everything that is not text/* or ending in /json or /xml counts as binary — images, audio, video, and unknown or empty types included. Broader than application/octet-stream and does not imply that specific mime type.
    static bool IsBinary(string mimeType)
    static bool IsCsv(string mimeType)
    static bool IsImage(string mimeType)
    static bool IsJson(string mimeType)
    static bool IsMarkdown(string mimeType)
    static bool IsMicrosoftExcel(string mimeType)
    static bool IsMicrosoftPowerpoint(string mimeType)
    static bool IsMicrosoftWord(string mimeType)
    static bool IsNotes(string mimeType)
    static bool IsPdf(string mimeType)
    // Returns true when the mime type is textual: any text/* type, or one ending in /json or /xml. Everything else (images, audio, video, unknown types) is not text.
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
    // type: The category keyword — not a mime string. Recognized keywords are: text, markdown, video, image, audio, json, binary, csv, zip, xml, pdf, word, excel, powerpoint, notes, and any. "any" always returns true; an unrecognized keyword returns false.
    static bool TypeMatchesMimetype(string type, string mimeType)
    const string ApplicationExcel
    const string ApplicationJavascript
    const string ApplicationJson
    const string ApplicationMsword
    const string ApplicationOctetStream
    const string ApplicationPdf
    const string ApplicationSql
    const string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    const string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    const string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    const string ApplicationXml
    const string ApplicationZip
    const string AudioMpeg
    const string AudioXWav
    const string Binary
    const string DefaultExtension
    const string DefaultMimeType
    const string ImageAvif
    const string ImageBmp
    const string ImageGif
    const string ImageHeif
    const string ImageJpeg
    const string ImagePng
    const string ImageSvg
    const string ImageSvgXml
    const string ImageTiff
    const string ImageWebp
    const string TextCss
    const string TextCsv
    const string TextHtml
    const string TextJavascript
    const string TextMarkdown
    const string TextPlain
    const string TextXml
    const string VideoMp4
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  // Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
  sealed class RelayEndpoint : IAsyncDisposable
    int LocalPort { get; }
    EndpointProtocol Protocol { get; }
    string PublicHost { get; }
    int PublicPort { get; }
    ValueTask DisposeAsync()
  enum RequiredStatus
    Default
    Required
    Optional
  class Resources : AsyncLocalInstance<Resources>
    ctor()
    Task<byte[]> ReadAsBytesAsync(string resourcePath)
    Task<Stream> ReadAsStreamAsync(string resourcePath)
    Task<string> ReadAsStringAsync(string resourcePath)
  // Across every overload, retries counts attempts beyond the first: the delegate runs once, then up to retries more times, for at most retries + 1 total invocations (e.g. retries = 5 allows up to 6 calls). When no retryableExceptions filter is supplied, only transient exceptions are retried: IOException, HttpRequestException and TimeoutException. Non-transient exceptions (bugs, validation failures) surface immediately instead of being retried. Pass an explicit filter to override this default — e.g. [typeof(Exception)] to retry every exception. maxDelay caps the backoff per exception, for a failure whose next attempt is an independent draw rather than a wait for something to recover; return TimeSpan.MaxValue to leave the ladder uncapped. It never changes how many attempts are made. maxRetries lowers retries per exception, for a failure where more attempts buy nothing however long the wait; Int32.MaxValue leaves it at retries. It never raises the budget.
  static class Retrier
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
  static class StringDistance
    // Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b. Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory.
    static int Levenshtein(string? a, string? b)
