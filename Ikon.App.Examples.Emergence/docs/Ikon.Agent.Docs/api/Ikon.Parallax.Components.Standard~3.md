namespace Ikon.Parallax.Components.Standard
  static class DragAndDropExtensions
    // onDragEnd: Invoked when the drag operation ends (dropped or cancelled).
    // activationDistance: Pixels of pointer movement before a drag activates; a pointerdown below the threshold is delivered as a normal click (inner Button.onClick fires). Null: drag activates immediately.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // activeDragId: The ID of the currently dragged item. When set, the overlay only renders its content after the server has sent content matching this drag ID, preventing stale content from a previous drag.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // hideOnDrag: When true, hides the original element during drag. Use with DragOverlay.
    // data: Custom data attached to this draggable, available in drag event arguments.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // data: Custom data attached to this droppable, available in drag event arguments.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // items: List of item identifiers in the current sort order.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item stays free for inner clickable elements. Place inside a SortableItem (or a SortableList itemContent); outside one it renders as a plain container.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // items: List of item identifiers in the current sort order.
    // onReorder: Invoked with the new order after a drag. The only write-back — persist args.NewOrder here, or reorders show on the client but never reach the app.
    // itemContent: Renders each item's content, receiving the item id; omitted, each item renders a drag-handle icon plus the id as text.
    // activationDistance: Pixels of pointer movement before a drag activates; a pointerdown below the threshold is delivered as a normal click (inner Button.onClick fires). Null: drag activates immediately.
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record DragCancelArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record DragEndArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragMoveArgs
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  sealed record DragOverArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragStartArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record EscapeKeyDownArgs
    ctor()
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    // expandedIds: Node ids that start expanded.
    ctor(params string[] expandedIds)
    void Clear()
    void Collapse(string id)
    void Expand(string id)
    bool IsExpanded(string id)
    void Set(string id, bool expanded)
    void Toggle(string id)
  // Hint used by FeedSlide to preload the slide's primary media asset.
  enum FeedMediaKind
    None
    Image
    // Preloads the video's metadata only, not the full payload.
    Video
    // Preloads the full video payload. Use sparingly — costs bandwidth.
    VideoFull
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    // slides: Slides rendered in order; grow the list and use onScrollNearEnd to page more in.
    // preloadAhead: Slides after the active one to keep mounted and preload media for. Default 2.
    // preloadBehind: Slides before the active one to keep mounted. Default 1.
    // autoPlay: Autoplay videos on the active slide. Default true.
    // muted: Controlled mute state for all media. Default true (browsers require muted autoplay).
    // scrollEndThreshold: Slides from the end at which onScrollNearEnd fires. Default 2.
    // style: Outermost viewport container; default token FeedScroller.Root.
    // slideStyle: Applied to every slide; default token FeedScroller.Slide.
    // onActiveChange: Invoked with the new active slide index.
    // onScrollNearEnd: Fires within scrollEndThreshold slides of the end, with the active slide index — use it to append the next page.
    // onMuteChange: Invoked when the user toggles mute on an in-slide control.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<int, Task>? onActiveChange = null, Func<int, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // index: Zero-based index of this slide.
    // style: Style classes for the slide container.
    // mediaKind: Kind of media to preload for this slide.
    // mediaUrl: URL of the media asset.
    // mediaPoster: Optional poster image URL for video slides.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record FeedSlide
    // Content: Builder invoked to render the slide. Only slides inside the render window are realized.
    // Key: Stable key used for diffing and preload identity. Defaults to slide index.
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
    FeedMediaKind MediaKind { get; init; }
    string? MediaPoster { get; init; }
    string? MediaUrl { get; init; }
  static class FilePickerExtensions
    // Only reports picked-file metadata to the server — the bytes stay on the client and are not uploaded until a FileUploadExtensions.FileUpload with a matching seedSelectionIds prop is mounted.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // maxFileSize: Maximum file size in bytes (enforced client-side before emitting selection).
    // onFileSelected: Invoked once per picked file, with client-generated SelectionId and metadata.
    // onValidationError: Invoked when a picked file is rejected client-side (e.g. over maxFileSize). Surface Reason to the user — without this the rejection is silent.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePickerExtensions.FilePicker. The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed record FilePickerSelectedArgs
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. over maxFileSize). Surface Reason to the user — without a handler the rejection is silent.
  sealed record FilePickerValidationErrorArgs
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  static class FileUploadExtensions
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // maxFileSize: Maximum file size in bytes.
    // onUploadPreStart: First accept/reject hook, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate.
    // onUploadStart: Second hook, after the file hash is computed and before any chunks arrive; same return contract as onUploadPreStart.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // seedSelectionIds: Ids from a prior FilePickerExtensions.FilePicker selection; on first mount the client uploads the cached File handles through the normal pipeline, reusing each SelectionId as the UploadId.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container; the first positional style array is its alias), activeStyle (while a file is dragged over). The MIME filter is the named accept: parameter.
    // maxFileSize: Maximum file size in bytes.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // onUploadPreStart: First accept/reject hook, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadStart: Second hook, after the file hash is computed and before any chunks arrive; same return contract as onUploadPreStart.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // seedSelectionIds: Ids from a prior FilePickerExtensions.FilePicker selection; on mount the client uploads the cached File handles through the normal pipeline, reusing each SelectionId as the UploadId.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  static class FocusHintExtensions
    // targetViewId: View ID to receive focus. Defaults to the current view.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  sealed record FocusHintProps
    ctor()
    TimeSpan? Cooldown { get; init; }
    bool FocusOnly { get; init; }
    FocusPriority Priority { get; init; }
    int Ranking { get; init; }
  sealed record FocusOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Maps to ARIA live region politeness, and decides whether the hint scrolls: Polite defers to a reader who has scrolled away and shows the new-content indicator instead, Assertive scrolls the container to the target every time.
  enum FocusPriority
    Polite
    Assertive
  // Thrown inside the handler passed to FormState<T>.SubmitAsync: a null Field shows the message at form level, a field name shows it under that field. Any other exception type from the handler is shown at form level with its message.
  sealed class FormException : Exception
    // The form-level form: with no field name the message shows above the form rather than under a field.
    ctor(string message)
    ctor(string field, string message)
    string? Field { get; }
