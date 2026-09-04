namespace Ikon.Parallax.Components.Standard
  static class ContainerExtensions
    // onClick: Accepts sync (() => …) and async (async () => …) lambdas alike. A clickable Box automatically carries button semantics — role="button", tabIndex=0, Enter/Space activation. Override either through props, and give an icon-only Box an ["aria-label"].
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex flex-col base class, which cannot be removed or replaced.
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex base class, which cannot be removed or replaced.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed grid base class, which cannot be removed or replaced.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned; place inside a Stack container.
    // style: Crosswind utility classes; absolute is prepended when the array lacks it, and a null style defaults to absolute inset-0.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex flex-row base class, which cannot be removed or replaced.
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind/Tailwind utility classes appended to the base spinner styling (e.g. a colour or margin).
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Layers children on top of each other; give each layer a Layer child.
    // style: Crosswind utility classes appended to the fixed relative base class, which cannot be removed or replaced.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  record ContentGridColumn
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  static class ContentGridExtensions
    static void ContentGrid(this UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null)
  sealed record CopyToClipboardActionOptions : ActionOptions
    ctor()
    required string Text { get; init; }
  static class CoreExtensions
    // text: Visible button text. When content is provided it instead becomes the accessible aria-label.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // text: Visible button text. When content is provided it instead becomes the accessible aria-label.
    // href: URL to navigate to when clicked; renders the button as an anchor element.
    // icon: Lucide icon name rendered alongside the text; content (when provided) wins over it.
    // tooltip: Hover text rendered with the themed Tooltip; it also becomes the accessible name when nothing else names the control. Do not use a title prop instead.
    // tooltipRootStyle: Styles for the tooltip wrapper, the element that sits in the parent's layout — responsive and positioning classes go here, not on the button. Defaults to inline-flex shrink-0.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null, Delegate? onPressStart = null, Delegate? onPressEnd = null)
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null, Delegate? onPressStart = null, Delegate? onPressEnd = null)
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // size: Merged as the icon's base sizing, so a w-*/h-* class in style still wins. Omit it to leave sizing entirely to style.
    // library: Defaults to the view's default icon library.
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // style: Crosswind utility classes; defaults to the theme's Button.Link styling.
    // href: Required. A same-origin path navigates in place without a document reload (surfacing as app.Navigation.PathChangedAsync), so the connection survives it and no onClick is needed to navigate.
    // rel: When target is "_blank" and rel is null, defaults to "noopener noreferrer". Pass "external" to force a full document load for a same-origin link.
    // onClick: Fires alongside navigation; for side effects only — href already handles the destination.
    // content: Custom child content; text then becomes the aria-label.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // style: Crosswind/Tailwind utility classes for styling. With no array the body renders with Markdown.Default — heading scale, list markers, table rules, blockquote bar and a self-scrolling fenced-code box. Pass "default" as the first class to keep those and add your own on top; any other array replaces them.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // defaultValue: Initial pressed state when not controlling value.
    // label: Trailing text label; wraps the toggle and the text in a <label>, so clicking the text toggles the control and the text is the toggle's accessible name.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // defaultValue: Initial selection when not controlling value.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // defaultValue: Initial selection when not controlling value.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  enum Dir
    Ltr
    Rtl
  static class DisclosureExtensions
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // collapsible: Allows the open item to be closed again, leaving none open.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // defaultOpen: Initial open state for uncontrolled mode.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record DownloadFileActionOptions : ActionOptions
    ctor()
    byte[]? Data { get; init; }
    string? Filename { get; init; }
    string? MimeType { get; init; }
    // Regular or data URL. When Data is set, auto-generated as a data URL using MimeType, falling back to "application/octet-stream" when MimeType is unset.
    string Url { get; init; }
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
