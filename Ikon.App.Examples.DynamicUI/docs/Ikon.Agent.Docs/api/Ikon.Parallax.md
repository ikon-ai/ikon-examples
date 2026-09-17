namespace Ikon.Parallax
  sealed class ActionArgs<T>
    ctor()
    Context ClientContext { get; init; }
    T Value { get; init; }
  sealed class ActionFailedEventArgs : EventArgs
    ctor(Exception exception, Context clientContext, Guid actionId, string callSite)
    Guid ActionId { get; }
    string CallSite { get; }
    Context ClientContext { get; }
    Exception Exception { get; }
  // Collapses the busy/status ceremony of an async handler to await _busy.RunAsync(_status, LoadAsync). For the busy flag alone (no status reactive), use _busy.AsToken() from Ikon.Common.Core.Reactive instead.
  static class ReactiveBusyExtensions
    // Clears status, raises busy for the duration of the work (via ReactiveBoolExtensions.AsToken, so it always returns to false), and routes a failure's message into status instead of throwing. Cancellation (OperationCanceledException) propagates to the caller. Returns whether the work completed, so callers can add their own failure handling on top.
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  // Per-client theme state created by UI.UseTheme. Holds each client's active theme and switches it: Current is bindable in views, and ToggleAsync can be bound directly to a button's onClick.
  sealed class ThemeControl
    ClientReactive<Theme> Current { get; }
    // Current is set even when the push fails (a client without the SetTheme function, or one whose function registry is not populated yet): the miss is logged as a warning and the client keeps rendering its previous theme until the next push.
    Task SetAsync(Theme theme)
    Task ToggleAsync()
  class UI
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs per-frame timing breakdowns to the app log.
    bool EnableProfiling { get; set; }
    // Default true. A subtree that reads only non-reactive data will not refresh until one of its reactive dependencies changes; set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point. This overload BLOCKS the calling thread until the initial render completes (it drives the async render with GetAwaiter().GetResult()). Calling it from the app's Main — an async Task that runs on the thread pool with no synchronization context — is the normal form; only a host with a single-threaded synchronization context (a UI thread, a test harness) must await RootAsync instead to avoid deadlocking it.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point.
    Task RootAsync(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Call once in Main, before clients join. With followClient true (the default) a joining client keeps its own saved theme and clients without one get defaultTheme; false forces defaultTheme on every client. Bind the returned Current in views and ToggleAsync to a button's onClick.
    // defaultTheme: The theme applied to clients that have none of their own (or to all clients when followClient is false).
    // followClient: When true, respects a joining client's own saved theme; when false, forces defaultTheme on every join.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
    // Runs inside the failed handler's client scope, so a ClientReactive written here reaches the person who clicked. The exception is not rethrown and the client is told the action failed either way; a subscriber that throws is logged and does not stop that report.
    event AsyncEventHandler<ActionFailedEventArgs>? ActionFailedAsync
  class UIView
    string DefaultIconLibrary { get; }
    // True only while capturing the build-time boot snapshot — a public asset shown to everyone before the live UI connects (always false on the live render). Gate per-user or sensitive content on this, preferably via the SnapshotReveal/SnapshotHide/SnapshotOnly wrappers.
    bool IsSnapshot { get; }
    // The boot-snapshot variant id this capture render was asked for (the client's Context.SnapshotVariant): the app's [BootSnapshot] seed rules name variant skeletons, and the capture client passes each id here so the app can branch to the matching skeleton. Empty on route captures (render the real page) and on every live render.
    string SnapshotVariant { get; }
    // Prop values are compared with the previous render's by reference before anything else, so a value mutated in place between renders (a List the app appends to, an object without value equality) diffs as unchanged and the client keeps showing the old content. Treat prop values as immutable: build a new value when the content changes.
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // The returned string is an opaque reference to use as an image src (e.g. on an Image component), not a data URL. The data buffer is copied on registration, so the caller may reuse or mutate it immediately after the call. data must be non-empty — an empty buffer has no valid reference and throws ArgumentException.
    string RegisterPayload(byte[] data, string mimeType)
    // extension methods, using Ikon.Parallax.Components.Charts: ChartExtensions{BarChart, LineChart, PieChart}
    // extension methods, using Ikon.Parallax.Components.DataTable: DataTableExtensions{DataTable}
    // extension methods, using Ikon.Parallax.Components.ImageEditor: ImageEditorExtensions{ImageEditorCanvas}
    // extension methods, using Ikon.Parallax.Components.Standard: AccessibilityExtensions{AccessibleIcon, VisuallyHidden}, AlertExtensions{Alert}, BadgeExtensions{Badge}, BreadcrumbExtensions{Breadcrumb}, CalendarExtensions{Calendar, DatePicker}, CardExtensions{Card, EmptyState, StatCard}, CarouselExtensions{Carousel, Slide}, ChatLogExtensions{ChatLog}, CodeEditorExtensions{CodeEditor}, ColorPickerExtensions{ColorPicker}, ComposerExtensions{Composer}, ContainerExtensions{Box, Column, Flex, Grid, Layer, Row, Spinner, Stack}, ContentGridExtensions{ContentGrid}, CoreExtensions{ActionButton, Button, Heading, Icon, Link, Markdown, Text, Toggle, ToggleGroupItem, ToggleGroupMultiple, ToggleGroupSingle}, DisclosureExtensions{AccordionContent, AccordionHeader, AccordionItem, AccordionMultiple, AccordionSingle, AccordionTrigger, Collapsible, CollapsibleContent, CollapsibleTrigger}, DragAndDropExtensions{DndContext, DragOverlay, Draggable, Droppable, SortableContext, SortableHandle, SortableItem, SortableList}, FeedScrollerExtensions{FeedScroller, FeedSlide}, FilePickerExtensions{FilePicker}, FileUploadExtensions{FileUpload, FileUploadZone}, FocusHintExtensions{FocusHint}, FormExtensions{Checkbox, CheckboxIndicator, Form, FormControl, FormField, FormLabel, FormMessage, FormSubmit, Label, RadioGroup, RadioGroupIndicator, RadioGroupItem, Slider, SliderRange, SliderThumb, SliderTrack, Switch, SwitchThumb, TriStateCheckbox}, FormStateExtensions{FormDialog, FormError, FormField, FormSubmit}, ImageExtensions{Avatar, AvatarFallback, AvatarImage, Image}, InputExtensions{OtpField, OtpFieldInput, PasswordToggleField, PasswordToggleFieldIcon, PasswordToggleFieldInput, PasswordToggleFieldToggle, TextArea, TextField}, KeyboardExtensions{KeyboardListener}, LayoutExtensions{AspectRatio, DirectionProvider, Divider, InfiniteScrollView, Progress, ResizableSplit, ScrollArea, Separator}, MediaExtensions{AudioUrlPlayer, AudioWave, CaptureButton, MicToggleButton, PushToTalkButton, VideoStreamCanvas, VideoUrlPlayer}, NavigationExtensions{Menubar, MenubarCheckboxItem, MenubarContent, MenubarItem, MenubarItemIndicator, MenubarMenu, MenubarRadioGroup, MenubarRadioItem, MenubarSeparator, MenubarSub, MenubarSubContent, MenubarSubTrigger, MenubarTrigger, NavigationMenu, NavigationMenuContent, NavigationMenuIndicator, NavigationMenuItem, NavigationMenuLink, NavigationMenuList, NavigationMenuTrigger, NavigationMenuViewport, Toolbar, ToolbarButton, ToolbarLink, ToolbarSeparator, ToolbarToggleGroupMultiple, ToolbarToggleGroupSingle, ToolbarToggleItem}, OverlayExtensions{AlertDialog, Dialog, HoverCard, Popover, Toast, Tooltip}, OverlayMenuExtensions{Combobox, CommandPalette, DropdownMenu, Kbd}, PaginationExtensions{Paginate}, PanZoomExtensions{PanZoom}, QrCodeExtensions{QR}, RichTextEditorExtensions{RichTextEditor}, RoutingExtensions{Routed, Set}, ScrollColumnExtensions{ScrollColumn}, SelectExtensions{Select}, ShadertoyExtensions{ShadertoyCanvas}, SheetExtensions{Drawer, Sheet}, SkeletonExtensions{Skeleton}, SnapshotExtensions{SnapshotHide, SnapshotOnly, SnapshotReveal}, TableExtensions{Table, TableBody, TableCell, TableHead, TableHeader, TableRow}, TabsExtensions{Tabs}, TimePickerExtensions{TimePicker}, ToastsExtensions{ToastHost}, TreeViewExtensions{TreeView}, VirtualListExtensions{VirtualGrid, VirtualList}
  sealed class UIViewNode
    // Treat as immutable: the node is shared by reference into the subtree cache, and the differ relies on the child list being the pristine as-built content, so mutating it corrupts diffing and the cache. The mutable backing list is builder-internal.
    IReadOnlyList<UIViewNode> Children { get; }
    string? ContentFingerprint { get; }
    bool HasExplicitKey { get; }
    string Id { get; }
    int IdHash { get; }
    // Debug-only, process-global switch: when true, EVERY node built by ANY view on ANY thread and for ANY client emits a source file/line marker that is serialized into the wire payload, inflating all UI updates. Despite reading like a per-instance toggle it is static mutable state with no thread-safety, so flip it only for local debugging (the runtime sets it from the app's DebugMode) and never leave it on in production.
    static bool IncludeSourceMarkers { get; set; }
    // Backed by the compact shape-interned PropsMap on server-built trees; treat as immutable.
    IReadOnlyDictionary<string, object?> Props { get; }
    string? SourceMarker { get; }
    string? StableHint { get; }
    IReadOnlyList<string> StyleIds { get; }
    string Type { get; }
