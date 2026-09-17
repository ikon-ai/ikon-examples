namespace Ikon.Pipeline.Items
  interface IItem<out T>
    Task<bool> IsObjectAsync<TObject>()
    // processId: Identifier associated with the processor run.
    T WithProcessId(Guid processId)
  // Immutable, lightweight pointer: it carries a content hash, not the bytes (which live in the content cache). Produce modified copies via the With* methods rather than mutating. The hash is computed once at creation: Create hashes content, MIME type, parent hashes and tags; CreateInitial hashes content and MIME type only, so initial items differing solely in tags share a hash. With* copies keep the hash whatever they change. MIME type is auto-detected from the content when not supplied and sets the output file extension.
  readonly struct Item : IItem<Item>
    // Do not construct directly — always create items via the static Create, CreateInitial, or CreateFromObject methods.
    ctor()
    string GroupId { get; init; }
    string Hash { get; init; }
    // For internal use.
    string? InitialPath { get; init; }
    bool IsDefault { get; }
    ItemMetadata? Metadata { get; init; }
    string MimeType { get; init; }
    // Used as the filename when outputting; the extension comes from the MIME type.
    string Name { get; init; }
    IReadOnlyList<string> ParentHashes { get; init; }
    Guid ProcessId { get; init; }
    IReadOnlyList<string>? Tags { get; init; }
    // Called from processors during the run; the parent items feed the new item's hash. To seed inputs before Run, use CreateInitial.
    // parents: Parent items used to compute the new item's hash.
    // name: Name of the new item.
    // content: Content stream.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Content stream.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: UTF-8 string content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: UTF-8 string content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: Binary content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Binary content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: Local file containing the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Local file containing the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes content to JSON. Use inside the pipeline; before Run use CreateInitialFromObject<T>.
    // parents: Parent items.
    // name: Name of the new item.
    // content: Object to serialize.
    // tags: Optional tags.
    // metadata: Optional metadata.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateFromObject<T>(List<Item> parents, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Object to serialize.
    // tags: Optional tags.
    // metadata: Optional metadata.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateFromObject<T>(Item parent, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // For seeding input items after the pipeline is initialized but before Run. Inside a running pipeline use Create instead.
    // name: Name of the item.
    // content: Stream containing the item content.
    // mimeTypeOverride: Optional MIME type to use instead of auto detection.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: UTF-8 string content.
    // mimeTypeOverride: Optional MIME type override.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: Binary content.
    // mimeTypeOverride: Optional MIME type override.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: Object to serialize.
    // metadata: Optional metadata for the item.
    // tags: Optional tags associated with the item.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateInitialFromObject<T>(string name, T content, ItemMetadata? metadata = null, List<string>? tags = null, JsonSerializerOptions? jsonSerializerOptions = null)
    Task<byte[]> GetContentAsBytes()
    Task<TObject> GetContentAsObject<TObject>()
    Task<Stream> GetContentAsStream()
    Task<string> GetContentAsString()
    string GetGroupId()
    Task<string> GetGroupIdAsync()
    Task<LocalFile> GetLocalFile()
    string GetOriginalName()
    Task<string> GetOriginalNameAsync()
    string GetOriginalPath()
    Task<string> GetOriginalPathAsync()
    string GetPageId()
    Task<string> GetPageIdAsync()
    Task<List<Item>> GetParents()
    string GetProcessId()
    Task<string> GetProcessIdAsync()
    bool HasTags(params string[] tags)
    Task<bool> HasTagsAsync(params string[] tags)
    bool IsAudio()
    Task<bool> IsAudioAsync()
    bool IsBinary()
    Task<bool> IsBinaryAsync()
    bool IsCsv()
    Task<bool> IsCsvAsync()
    bool IsImage()
    Task<bool> IsImageAsync()
    bool IsJson()
    Task<bool> IsJsonAsync()
    bool IsMicrosoftExcel()
    Task<bool> IsMicrosoftExcelAsync()
    bool IsMicrosoftPowerpoint()
    Task<bool> IsMicrosoftPowerpointAsync()
    bool IsMicrosoftWord()
    Task<bool> IsMicrosoftWordAsync()
    // This is an exact object-type-name match against the item's MIME type, not an is-assignable check: it returns false for a base class or interface of the stored type even though GetContentAsObject<TObject> would deserialize such an item successfully. Do not use it to guard GetContentAsObject<TObject> against a base/interface TObject.
    bool IsObject<TObject>()
    bool IsObject()
    Task<bool> IsObjectAsync<TObject>()
    Task<bool> IsObjectAsync()
    bool IsPdf()
    Task<bool> IsPdfAsync()
    bool IsText()
    Task<bool> IsTextAsync()
    bool IsVideo()
    Task<bool> IsVideoAsync()
    bool IsXml()
    Task<bool> IsXmlAsync()
    // name: Optional new name.
    // mimeType: Optional MIME type override.
    // processId: Optional process identifier.
    // groupId: Optional group identifier.
    // tags: Optional tag collection.
    // metadata: Optional metadata override.
    Item With(string? name = null, string? mimeType = null, Guid? processId = null, string? groupId = null, List<string>? tags = null, ItemMetadata? metadata = null)
    Item WithProcessId(Guid processId)
    const string ObjectMimeTypePrefix
  static class ItemExtensions
    // Returns null when nothing matches — unlike FirstOrDefault, which yields a default Item struct that null checks cannot detect.
    static Item? FirstOrNull(this IEnumerable<Item> items, Func<Item, bool> predicate)
    // Returns null when the collection is empty — unlike FirstOrDefault, which yields a default Item struct that null checks cannot detect.
    static Item? FirstOrNull(this IEnumerable<Item> items)
  // When outputting an item that has metadata, the metadata is written alongside the item with a .meta.json extension. Immutable by design; use the With method to create modified copies.
  readonly struct ItemMetadata
    // Do not use. Use the constructor which takes a parent ItemMetadata instead.
    ctor()
    // Inherits values from the provided parent metadata where a parameter is not supplied, except PreviousItemName and NextItemName: those are sequence links of the new item and are always taken from the parameters (null when omitted), never from the parent.
    ctor(ItemMetadata? parent, string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)
    DateTime? CreatedAt { get; init; }
    string? CustomJson { get; init; }
    string? DocumentTitle { get; init; }
    string? DocumentType { get; init; }
    string? NextItemName { get; init; }
    string? OriginalName { get; init; }
    string? OriginalPath { get; init; }
    int? PageCount { get; init; }
    int? PageNumber { get; init; }
    IReadOnlyList<int>? PageNumbers { get; init; }
    string? PreviousItemName { get; init; }
    IReadOnlyDictionary<string, string>? Properties { get; init; }
    IReadOnlyList<string>? TitleHierarchy { get; init; }
    DateTime? UpdatedAt { get; init; }
    ItemMetadata With(string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)
