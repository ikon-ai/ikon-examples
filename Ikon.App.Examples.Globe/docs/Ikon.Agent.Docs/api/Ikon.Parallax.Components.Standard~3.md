namespace Ikon.Parallax.Components.Standard
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
    // onUploadPreStart: First accept/reject hook, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate.
    // onUploadStart: Second hook, after the file hash is computed and before any chunks arrive; same return contract as onUploadPreStart.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // seedSelectionIds: Ids from a prior FilePickerExtensions.FilePicker selection; on first mount the client uploads the cached File handles through the normal pipeline, reusing each SelectionId as the UploadId.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container; the first positional style array is its alias), activeStyle (while a file is dragged over). The MIME filter is the named accept: parameter.
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
  // Maps to ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  // Thrown inside the handler passed to FormState<T>.SubmitAsync: a null Field shows the message at form level, a field name shows it under that field. Any other exception type from the handler is shown at form level with its message.
  sealed class FormException : Exception
    ctor(string field, string message)
    string? Field { get; }
  static class FormExtensions
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the checkbox in a <label> — clicking the text toggles the control and the text becomes its accessible name. Prefer this over placing your own Text beside a bare Checkbox, which associates nothing.
    // bind: Two-way binds the checkbox to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // forceMount: When true, forces the indicator to render even when the checkbox is unchecked.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // onClearServerErrors: Invoked when server-side validation errors should be cleared.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // name: The name of the form field, used for validation and form submission.
    // serverInvalid: When true, indicates the field has a server-side validation error.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // match: The validation condition that must be met for this message to display.
    // forceMatch: When true, forces the message to display regardless of the match condition.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // htmlFor: The id of the element this label is associated with.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: Orientation used for keyboard navigation.
    // label: Group-level label rendered above the radio group (same field ergonomics as TextField).
    // bind: Two-way binds the group to a Reactive<T> — reads bind.Value and writes it back on every selection. When set, value: is ignored and onValueChange still fires after the write-back.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // forceMount: When true, forces the indicator to render even when the radio is not selected.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // value: The unique value for this radio item within the group.
    // required: When true, indicates this radio item must be selected before the form can be submitted.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // onValueChange: Fires continuously while dragging.
    // onValueCommit: Fires once when dragging ends.
    // content: The default content's thumb carries aria-readonly for a read-only slider (controlled value: with no write-back); custom thumbs should set it too.
    // label: Also the accessible name of the thumbs, where role="slider" lives — a name on the root names nothing; multi-thumb thumbs are numbered from it.
    // bind: Two-way binds a single-thumb slider to a Reactive<T>, writing back as the user drags; value: is ignored and onValueChange still fires. Multi-thumb ranges use the value: list form.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the switch in a <label> — clicking the text toggles it and the text becomes the switch's accessible name; without this or ariaLabel it is announced as an unlabelled control.
    // bind: Two-way binds the switch to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // formValue: HTML form value submitted when checked.
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  sealed record FormFieldError
    // Field: The name of the field, as passed to FormStateExtensions.FormField<T>.
    // Message: Human-readable text rendered under the field.
    ctor(string Field, string Message)
    string Field { get; init; }
    string Message { get; init; }
  enum FormMessageMatch
    ValueMissing
    TypeMismatch
    TooShort
    TooLong
    PatternMismatch
    RangeUnderflow
    RangeOverflow
    StepMismatch
    BadInput
    CustomError
