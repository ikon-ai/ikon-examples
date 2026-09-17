namespace Ikon.Parallax.Components.Standard
  static class ComposerExtensions
    // A complete input bar — attach button, drag-and-drop, paste, auto-growing text, optional push-to-talk — so apps do not rebuild it. Stateless: pass the draft in value and the pending files in attachments, and store what the callbacks hand back. onSubmit receives the submitted text, empty when the Send button was clicked rather than Enter pressed: take it when it is non-empty and fall back to the draft, because a surface switch can clear the draft between the keystroke and the handler. The mic renders only when both capture callbacks are wired; transcription is the app's job. Per-slot style parameters restyle every part, and label parameters localize every string.
    // seedSelectionIds: Ids from a prior FilePicker selection, uploaded on mount (see FileUploadZone).
    // onAttachmentAdded: A file was picked, dropped, or pasted and finished uploading; its temp path is in the args.
    // onCaptureStop: Push-to-talk released — transcribe the capture and append it to the draft.
    static void Composer(this UIView view, string[]? style = null, string? value = null, string? placeholder = null, bool busy = false, IReadOnlyList<ComposerAttachment>? attachments = null, string[]? accept = null, long? maxFileSize = null, int? maxRows = null, bool? autoFocus = null, string[]? seedSelectionIds = null, string[]? fieldStyle = null, string[]? chipStyle = null, string[]? attachButtonStyle = null, string[]? sendButtonStyle = null, string[]? micStyle = null, string[]? activeStyle = null, string attachLabel = "Attach files", string sendLabel = "Send", string holdToTalkLabel = "Hold to talk", string releaseLabel = "Release to send", string? key = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<FileUploadCompleteArgs, Task>? onAttachmentAdded = null, Func<FileUploadErrorArgs, Task>? onAttachmentError = null, Func<int, Task>? onAttachmentRemoved = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null)
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
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
    // tooltip: Hover text rendered with the themed OverlayExtensions.Tooltip; it also becomes the accessible name when nothing else names the control. Do not use a title prop instead.
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
    // The item's heading element; renders content as is. Put the AccordionTrigger inside it yourself — none is added automatically.
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
    // The mirror of an upload that streams into asset storage via onUploadStart: here the browser reads back out of it, over a signed URL, without the bytes passing through the app. Use AssetClass.CloudFile so the URL is private and temporal, and give the object an expiresAt if it was staged only for this download. Ignored when Data, Url or UrlProvider is set.
    Func<Task<AssetUri>>? AssetProvider { get; init; }
    byte[]? Data { get; init; }
    string? DataActionId { get; init; }
    string? Filename { get; init; }
    string? MimeType { get; init; }
    // Regular or data URL. When Data is set, auto-generated as a data URL using MimeType, falling back to "application/octet-stream" when MimeType is unset.
    string Url { get; init; }
    // Use instead of Data for anything expensive to produce or re-derived as the user works. Data ships its payload to every client that renders the button; this callback runs on the server on the click, and only the address it returns crosses the transport — the browser fetches the file itself. Return an expiring signed URL from private asset storage. Returning null or empty cancels the download. Ignored when Data or Url is set.
    Func<Task<string?>>? UrlProvider { get; init; }
