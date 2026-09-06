namespace Ikon.Parallax.Components.Standard
  // Declare one as an app field per form (new FormState<PresetDraft>(() => new())); the reactives take their identity from the declaring field, not from this class. All state is client-scoped, so each client edits its own draft, and members must be called where a client scope is active (UI render or event handlers). Show resets busy, dirty and errors; SubmitAsync runs validation, then the handler under the busy flag, and closes on success.
  sealed class FormState<T>
    // newDraft: Produces the draft used by Show when no draft is supplied.
    ctor(Func<T> newDraft)
    T Draft { get; }
    string? Error { get; }
    bool HasErrors { get; }
    bool IsBusy { get; }
    bool IsDirty { get; }
    bool IsOpen { get; }
    void ClearErrors()
    void Close()
    // The mutator runs under the reactive's per-client lock, so concurrent field handlers cannot lose each other's edits. With a record draft, form.Edit(d => d with { Name = value }).
    void Edit(Func<T, T> mutate)
    string? ErrorFor(string field)
    void Fail(string message)
    void Fail(string field, string message)
    void Show()
    void Show(T draft)
    // Returns false without calling the handler while a submit is already in flight or when the validator returns any error. A FormException from the handler becomes a field or form error; any other exception becomes a form-level error carrying its message and is logged at warning. The form closes only when the handler completes and closeOnSuccess is true.
    // validate: Returns the field errors for the draft; an empty sequence means valid.
    Task<bool> SubmitAsync(Func<T, Task> onSubmit, Func<T, IEnumerable<FormFieldError>>? validate = null, bool closeOnSuccess = true)
  static class FormStateExtensions
    // contentStyle: Merged on top of Theming.Dialog.Content.
    static void FormDialog<T>(this UIView view, FormState<T> form, Action<UIView> content, string? title = null, string? description = null, string[]? contentStyle = null, string[]? overlayStyle = null, string? key = null)
    static void FormError<T>(this UIView view, FormState<T> form, string[]? style = null)
    // name: Field name; matches FormFieldError.Field and FormException.Field.
    static void FormField<T>(this UIView view, FormState<T> form, string name, Action<UIView> content, string[]? style = null, string? key = null)
    // validate: Returns the field errors for the draft; an empty sequence means valid.
    static void FormSubmit<T>(this UIView view, FormState<T> form, string text, Func<T, Task> onSubmit, Func<T, IEnumerable<FormFieldError>>? validate = null, bool closeOnSuccess = true, string[]? style = null, string? key = null)
  enum HourFormat
    Hour24
    Hour12
  // The size: form of the Theming.Icon.Xs..Xl tokens. The style-array form (view.Icon([Icon.Lg], ...)) stays valid and, being a caller class, wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
    Xl2
    Xl3
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  static class ImageExtensions
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // delayMs: Delay in milliseconds before showing the fallback.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // src: URL of the image to display.
    // alt: Alternative text description for accessibility.
    // onLoadingStatusChange: Invoked when the image loading status changes.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // src: URL or path to the image source.
    // assetUri: Asset URI to resolve the image source from. Takes precedence over src.
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // data: Binary image data.
    // mimeType: MIME type of the image (e.g., "image/png", "image/jpeg").
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  static class InputExtensions
    // value: A controlled value with no onValueChange renders the field read-only.
    // autoSubmit: When true, onAutoSubmit fires once all characters are entered.
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // index: Zero-based index of this slot in the OTP field.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // autoComplete: Browser autocomplete hint (e.g., "current-password", "new-password").
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // autoResize: When true, grows to fit content up to maxRows (default 6), then scrolls.
    // submitOnEnter: When true, Enter submits and Shift+Enter inserts a newline; default false (Ctrl/Cmd+Enter submits).
    // onSubmit: Receives the submitted value; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true when onSubmit/onSubmitWithContext is set.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires after the write-back.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // A controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) is read-only — the rule every input component shares.
    // onSubmit: Receives the submitted value on Enter; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true only when onSubmit is set; without one Enter does not empty a bound field.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires.
    // multiline: Delegates to TextArea (MUI-style spelling); rows: alone implies it.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Values match the browser KeyboardEvent.key specification; raw strings also work for keys not listed here.
  static class Key
    const string Alt
    const string ArrowDown
    const string ArrowLeft
    const string ArrowRight
    const string ArrowUp
    const string Backspace
    const string Control
    const string Delete
    const string End
    const string Enter
    const string Escape
    const string F1
    const string F10
    const string F11
    const string F12
    const string F2
    const string F3
    const string F4
    const string F5
    const string F6
    const string F7
    const string F8
    const string F9
    const string Home
    const string Meta
    const string PageDown
    const string PageUp
    const string Shift
    const string Space
    const string Tab
  // Property semantics match the browser KeyboardEvent.
  sealed record KeyboardEventArgs
    ctor(string Key, string Code, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool Repeat)
    bool AltKey { get; init; }
    string Code { get; init; }
    bool CtrlKey { get; init; }
    string Key { get; init; }
    bool MetaKey { get; init; }
    bool Repeat { get; init; }
    bool ShiftKey { get; init; }
  static class KeyboardExtensions
    // keys: Only forward events for these key names (Key constants); null forwards all keys.
    // global: Default true: listens at document level; false listens only on the wrapper element.
    // requireCtrlOrMeta: When true, the client drops events without Ctrl or Cmd held — the filter every ⌘X-style shortcut needs. Filtering only in the server callback is not enough: preventDefault applies client-side to every matched key, so a bare-key listener with it swallows that letter in every text field of the app.
    // preventDefault: Prevents the default browser behavior for matched keys; pair with requireCtrlOrMeta for modifier shortcuts.
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? requireCtrlOrMeta = null, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  static class LayoutExtensions
    // ratio: The width-to-height ratio to maintain (e.g., 16.0/9.0 for widescreen).
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1.0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // dir: Text direction for descendants.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Divider(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb; rootStyle rarely needed.
    // threshold: Distance from end (in pixels) to trigger onNearEnd. Default 200.
    // debounceMs: Debounce time in ms to prevent rapid callback firing. Default 100.
    // loading: When true, shows loading indicator and prevents duplicate callbacks.
    // hasMore: When false, disables the onNearEnd callback (end of data reached).
    // direction: Whether to detect scroll near end going Down (append) or Up (prepend).
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill. Style slots: rootStyle → Progress.Root, indicatorStyle → Progress.Indicator.
    // indeterminate: When true, displays an indeterminate progress animation.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resize is handled entirely on the client — only the final size reaches the server via onResized.
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200.0, double minSize = 100.0, double maxSize = 500.0, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb, cornerStyle (when both scrollbars show); rootStyle rarely needed.
    // scrollHideDelay: Delay in milliseconds before hiding scrollbars when type is Scroll or Hover.
    // autoScroll: When true, automatically scrolls to the bottom when content changes (chat-style).
    // autoScrollKey: Anything whose value changes when the content does — auto-scroll re-fires on change. Pass the collection itself (any reactive contributes its change version), a count, or a composite string. Required when autoScroll is true.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // orientation: Whether the separator is horizontal or vertical.
    // decorative: When true, the separator is purely visual and not announced by screen readers.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record LocationActionEvent : ActionEvent
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  enum MediaCaptureButtonMode
    Hold
    Toggle
  // ClientContext identifies the initiating user and is populated for all capture kinds; prefer ClientSessionId/UserId over tracking streamId-to-client mappings yourself.
  sealed record MediaCaptureEvent
    ctor(string StreamId, MediaCaptureKind Kind)
    Context? ClientContext { get; init; }
    int? ClientSessionId { get; }
    MediaCaptureKind Kind { get; init; }
    string StreamId { get; init; }
    string? UserId { get; }
  enum MediaCaptureKind
    Audio
    Camera
    Screen
