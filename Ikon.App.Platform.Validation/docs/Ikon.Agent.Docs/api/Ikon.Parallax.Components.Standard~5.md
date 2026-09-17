namespace Ikon.Parallax.Components.Standard
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
  static class MediaExtensions
    // url: URL of the audio source.
    // controls: When true, displays audio playback controls.
    // autoplay: When true, audio starts playing automatically.
    // loop: When true, audio loops continuously.
    // muted: When true, audio is muted.
    // preload: Specifies if/how the audio should be loaded when the page loads ("none", "metadata", or "auto").
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Pure presentation: renders the same loop whatever the microphone is doing (per-frame amplitude would cost a server round trip per frame). To reveal it only while capturing, give the row containing the mic button and the wave the group class and style the wave's container with Theming.MicButton.WhileCapturing — the reveal keys on the client-stamped data-ikon-capture-active attribute and lands on press, with no server involvement.
    // style: Crosswind/Tailwind utility classes merged over Theming.AudioWave.Root.
    // bars: How many bars to draw.
    // barStyle: Style for each bar. Defaults to Theming.AudioWave.Bar.
    static void AudioWave(this UIView view, string[]? style = null, int bars = 7, string[]? barStyle = null, string? key = null)
    // text: Visible button text; becomes the aria-label when content is provided.
    // holdReleaseDelayMs: In Hold mode, keeps capturing this many milliseconds after release — speech users often release slightly before finishing.
    // permissionText: Shown instead of the button's own face while the browser has not granted the microphone/camera yet; pressing then only asks for the permission. Defaults to "Enable microphone"/"Enable camera"; pass an empty string to keep the normal face and let the press ask silently.
    // permissionDeniedText: Shown when the permission was refused or no device exists. Defaults to "Microphone blocked"/"Camera blocked".
    // onPermissionChanged: Fires when the user answers the permission dialog.
    // content: When provided, enables icon mode: content is displayed and text becomes the aria-label.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, string? permissionText = null, string? permissionDeniedText = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Func<MediaPermissionEvent, Task>? onPermissionChanged = null, Action<UIView>? content = null)
    // Tap to open the microphone, tap again to close — the segment between is one utterance. Prefer it over PushToTalkButton for long utterances or busy hands; never offer both for one microphone. It owns the permission the same way: until the browser grants one the button renders as "Enable microphone" and a press only asks, and disabled is never the way to express that state. After Audio.UseSpeechRecognition(...), subscribe to Audio.SpeechRecognizedAsync for the transcription when the mic closes.
    // permissionText: Wording of the enable-microphone state; empty keeps the button's normal face.
    // permissionDeniedText: Wording shown when the microphone is blocked or missing.
    // onPermissionChanged: Fires when the user answers the permission dialog.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void MicToggleButton(this UIView view, string[]? style = null, string? text = "🎤", ClientAudioCaptureOptions? audioOptions = null, string? permissionText = null, string? permissionDeniedText = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Func<MediaPermissionEvent, Task>? onPermissionChanged = null, Action<UIView>? content = null)
    // Hold to talk, release to send. The button owns the microphone permission: until the browser grants one it renders as an "Enable microphone" affordance and a press only asks, never also starting a capture — a permission dialog takes focus, which the page reads as a release, so a hold that doubles as the ask is cancelled before the user answers it. Never use disabled for permission state; a disabled button cannot ask. After Audio.UseSpeechRecognition(...), transcriptions arrive on Audio.SpeechRecognizedAsync.
    // holdReleaseDelayMs: Milliseconds to keep capturing after release; 500 by default.
    // permissionText: Wording of the enable-microphone state; empty keeps the button's normal face.
    // permissionDeniedText: Wording shown when the microphone is blocked or missing.
    // onPermissionChanged: Fires when the user answers the dialog — offer typing instead when it comes back denied.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, string? permissionText = null, string? permissionDeniedText = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Func<MediaPermissionEvent, Task>? onPermissionChanged = null, Action<UIView>? content = null)
    // streamId: Identifier of the video stream to display.
    // width: Width of the canvas in pixels.
    // height: Height of the canvas in pixels.
    // onTap: Called with the tap position normalized to the rendered frame (0..1).
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, Func<VideoTapArgs, Task>? onTap = null, string? styleId = null, string? key = null)
    // url: URL of the video source.
    // controls: When true, displays video playback controls.
    // autoplay: When true, video starts playing automatically.
    // loop: When true, video loops continuously.
    // muted: When true, video is muted.
    // playsInline: When true, plays inline on mobile devices instead of fullscreen.
    // poster: URL of the poster image shown before playback.
    // width: Width of the video player in pixels.
    // height: Height of the video player in pixels.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Raised when the user answers the permission dialog, so an app can react to a refusal instead of leaving a mic button that silently does nothing — offer typing instead, or explain where the browser's site settings are.
  sealed record MediaPermissionEvent
    ctor(MediaPermissionState State, MediaCaptureKind Kind)
    MediaCaptureKind Kind { get; init; }
    MediaPermissionState State { get; init; }
  enum MediaPermissionState
    Granted
    Prompt
    Denied
    Unavailable
