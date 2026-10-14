using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

// The validation app runs a real drag-and-drop board of its own, down to the same field names, so
// the version the docs teach lives here rather than colliding with it.
file sealed class DragDropExamples
{
    private sealed record DragItem(string Id, string Title);

    private static readonly DragItem[] SeedItems = [new("card-1", "Draft the brief"), new("card-2", "Review the copy"), new("card-3", "Book the venue")];

    private static IEnumerable<DragItem> GetColumnItems(string columnId) => SeedItems;

    private static DragItem GetItem(string id) => SeedItems.FirstOrDefault(item => item.Id == id) ?? new(id, id);

    private static Task HandleDrop(string activeId, string overId) => Task.CompletedTask;

    private void DocDragAndDropState() => Log.Instance.Debug($"{_activeDragId} {_dragOverColumnId}");

    #region example:drag-and-drop-state
    // Per-client drag tracking (lightweight, only IDs): a drag is one client's gesture
    private readonly ClientReactive<string?> _activeDragId = new(null);
    private readonly ClientReactive<string?> _dragOverColumnId = new(null);
    #endregion

    private void DocDragAndDrop(UIView view)
    {
        #region example:drag-and-drop
        // DndContext wraps the entire drag area
        view.DndContext(
            collisionDetection: CollisionDetection.RectIntersection,
            onDragStart: async args => { _activeDragId.Value = args.ActiveId; },
            onDragOver: async args => { _dragOverColumnId.Value = args.OverId; },
            onDragEnd: async args =>
            {
                _activeDragId.Value = null;
                _dragOverColumnId.Value = null;
                if (args.OverId != null) { await HandleDrop(args.ActiveId, args.OverId); }
            },
            onDragCancel: async () => { _activeDragId.Value = null; _dragOverColumnId.Value = null; },
            content: view =>
            {
                // Each column is a Droppable
                view.Droppable(["min-h-[100px]"], id: "column-1", content: v =>
                {
                    foreach (var item in GetColumnItems("column-1"))
                    {
                        // Each card is a Draggable with hideOnDrag
                        v.Draggable(["p-2 cursor-grab"], id: item.Id, hideOnDrag: true,
                            content: card => card.Text(text: item.Title));
                    }
                });

                // DragOverlay renders the floating drag preview
                view.DragOverlay(["shadow-lg opacity-90"], dropAnimation: true,
                    activeDragId: _activeDragId.Value,
                    content: v =>
                    {
                        if (_activeDragId.Value != null) { v.Text(text: GetItem(_activeDragId.Value).Title); }
                    });
            });
        #endregion
        view.Text([Text.Caption, "mt-2"], $"Dragging: {_activeDragId.Value ?? "nothing"} · over: {_dragOverColumnId.Value ?? "no column"}");
    }

}

public sealed record MyClickEventArgs(string Id);

file sealed class ChatLayoutExamples
{
    private sealed record Message(string Author, string Text);

    private readonly ReactiveList<Message> _messages = new();

    private readonly UserReactive<string> _draft = new("");

    public void Render(UIView view)
    {
        #region example:chat-layout
        // ChatLog pins the header and the footer and auto-scrolls the messages between them;
        // Composer is the input bar; Enter clears the draft, but after a Send click only onSubmit does
        view.ChatLog(["h-screen p-4 gap-4"],
            autoScrollKey: _messages,
            header: view => view.Text([Text.H2], "Chat"),
            content: view =>
            {
                foreach (var msg in _messages)
                {
                    view.Box(["py-2"], content: view =>
                    {
                        view.Text([Text.Caption, "text-muted-foreground"], msg.Author);
                        view.Text([Text.Body], msg.Text);
                    });
                }
            },
            footer: view => view.Composer(
                value: _draft.Value,
                placeholder: "Type a message...",
                onValueChange: async text => _draft.Value = text,
                onSubmit: async submitted =>
                {
                    if (!string.IsNullOrWhiteSpace(submitted))
                    {
                        _messages.Add(new Message("User", submitted));
                    }

                    _draft.Value = "";
                }));
        #endregion
    }
}

file sealed class InputsExamples
{
    private readonly Reactive<string> _text = new("");
    private readonly Reactive<string> _query = new("");
    private readonly Reactive<bool> _checked = new(false);
    private readonly Reactive<bool> _enabled = new(false);
    private readonly Reactive<double> _slider = new(0);
    private readonly Reactive<string> _selected = new("");
    private readonly Reactive<string> _radio = new("");
    private readonly Reactive<bool> _isDragging = new(false);
    private readonly Reactive<string> _layout = new("list");

    private static Task HandleSubmit(string submitted) => Task.CompletedTask;
    private static Task RunSearchAsync(string query) => Task.CompletedTask;

    public void Render(UIView view, bool isLoading)
    {
        #region example:input-components
        view.Button([Button.PrimaryMd], text: "Click", onClick: async () => { /* ... */ });
        view.Button([Button.OutlineMd], text: "Secondary", disabled: isLoading, onClick: async () => { /* ... */ });
        view.Button([Button.GhostMd, Button.Icon], onClick: async () => { /* ... */ },
            content: v => v.Icon([Icon.Default], name: "settings"));
        view.TextField(bind: _text, placeholder: "Enter text",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Enter submits; input auto-clears after submit
        view.TextArea(bind: _text, style: ["default", "min-h-[100px]"], placeholder: "Type a message...",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Ctrl+Enter submits; input auto-clears after submit
        // onSubmit's parameter is the submitted value. With bind:, the bound reactive (`_text.Value`)
        // holds that same value while onSubmit runs; a clearing field (the default here) writes "" to
        // it once onSubmit returns, unless the user typed again meanwhile.
        // Note: both TextField and TextArea clear on submit only when an onSubmit handler is set; a bound field
        // with no onSubmit keeps its value. Pass clearOnSubmit: true/false to override either way.
        // A search box keeps the query on screen after Enter, so it opts out:
        view.TextField(bind: _query, placeholder: "Search", clearOnSubmit: false,
            onSubmit: async query => { await RunSearchAsync(query); });
        view.TextArea(bind: _text, maxLength: 500);  // maxLength: caps typing and pasting (TextField takes it too)
        // Checkbox / Switch / Slider auto-render their inner part (the check mark, the switch
        // thumb, the slider track+thumb) AND, like RadioGroup / Toggle, their default styling —
        // the bare call below is all you need (a RadioGroupItem's checked fill is the whole dot, so
        // it needs no RadioGroupIndicator either); you do NOT have to compose a
        // CheckboxIndicator / SwitchThumb / SliderTrack child or pass a [*.Default] style.
        // Pass a content: lambda only to put CUSTOM content inside (e.g. a different icon), or a
        // style: array only to override the default look. To render a checkbox with no check mark
        // at all, opt out explicitly with content: _ => { }.
        view.Checkbox(bind: _checked, label: "Enable feature");  // label: renders a clickable trailing label
        view.Switch(bind: _enabled);
        view.Slider(bind: _slider, min: 0, max: 100, step: 1);   // scalar single-thumb; pass value: [a, b] lists for multi-thumb
        view.Select(bind: _selected, placeholder: "Choose...",
            options: [new SelectOption("a", "Option A"), new SelectOption("b", "Option B")]);
        view.RadioGroup(bind: _radio,
            content: view =>
            {
                // The item is just the radio circle (text in its content overflows it) — wrap item
                // and text in a Label, which names the radio and makes the text clickable
                view.Label([Label.Default, "flex items-center gap-2 cursor-pointer"], content: view =>
                {
                    view.RadioGroupItem(value: "opt1");
                    view.Text(text: "Option 1");
                });
                view.Label([Label.Default, "flex items-center gap-2 cursor-pointer"], content: view =>
                {
                    view.RadioGroupItem(value: "opt2");
                    view.Text(text: "Option 2");
                });
            });
        // A toggle group has no theme styling of its own. Its tokens are ToggleGroup.Root (the group) and ToggleGroup.Item
        view.ToggleGroupSingle([ToggleGroup.Root, "w-full"], value: _layout.Value, ariaLabel: "Layout",
            onValueChange: async v => { _layout.Value = v; },
            content: group =>
            {
                group.ToggleGroupItem([ToggleGroup.Item, "flex-1"], value: "list", content: v => v.Text(text: "List"));
                group.ToggleGroupItem([ToggleGroup.Item, "flex-1"], value: "grid", content: v => v.Text(text: "Grid"));
            });
        view.FileUpload(
            onUploadComplete: async args => { /* args.UploadId, args.FileName, args.MimeType, args.Size, args.LocalTempFilePath, args.AssetUri */ },
            onUploadProgress: async args => { /* args.ProgressPercentage (0-100), args.BytesUploaded, args.Size */ },
            accept: [".jpg", ".png", ".pdf"],  // optional file type filter
            maxFileSize: 10 * 1024 * 1024);    // optional max size in bytes

        // FileUploadZone wraps any content with drag-drop + paste upload capability
        view.FileUploadZone(
            accept: ["video/*"],
            onUploadComplete: async args => { /* args.FileName, args.MimeType, args.Size, args.LocalTempFilePath, args.AssetUri */ },
            onUploadProgress: async args => { /* args.ProgressPercentage, args.BytesUploaded */ },
            onDragActiveChange: async isDragging => { _isDragging.Value = isDragging; },
            zoneStyle: [FileUpload.Zone.Base],
            activeStyle: [FileUpload.Zone.Active],
            content: v => v.Text([Text.Caption], "Drop files here"));
        #endregion
    }
}

file sealed class OverlaysExamples
{
    private readonly Reactive<bool> _open = new(false);
    private readonly Reactive<bool> _alertOpen = new(false);
    private readonly Reactive<bool> _popOpen = new(false);
    private readonly Reactive<bool> _toastOpen = new(false);
    private readonly Reactive<bool> _menuOpen = new(false);
    private readonly Reactive<string> _sort = new("Newest");

    public void Render(UIView view)
    {
        #region example:overlay-components
        // Dialog
        view.Dialog([Dialog.Content, "sm:max-w-xl"], open: _open.Value, onOpenChange: async o => _open.Value = o,
            overlayStyle: [Dialog.Overlay], showClose: true,
            trigger: view => view.Button([Button.OutlineMd], text: "Open"),
            contentSlot: view =>
            {
                view.Box([Dialog.Header], content: view =>
                {
                    view.Text([Dialog.Title], "Title");
                    view.Text([Dialog.Description], "Description");
                });
                view.Text([Text.Body, "my-4"], "Content");
                view.Box([Dialog.Footer], content: view =>
                {
                    view.Button([Button.OutlineMd], text: "Cancel", onClick: async () => _open.Value = false);
                    view.Button([Button.PrimaryMd], text: "Confirm", onClick: async () => _open.Value = false);
                });
            });

        // AlertDialog
        view.AlertDialog(open: _alertOpen.Value, onOpenChange: async o => _alertOpen.Value = o,
            overlayStyle: [AlertDialog.Overlay], contentStyle: [AlertDialog.Content],
            trigger: view => view.Button([Button.ErrorMd], text: "Delete"),
            title: "Are you sure?", titleStyle: [AlertDialog.Title],
            description: "This action cannot be undone.", descriptionStyle: [AlertDialog.Description],
            footerStyle: [AlertDialog.Footer], cancelLabel: "Cancel", cancelStyle: [AlertDialog.Cancel],
            actionLabel: "Delete", actionStyle: [Button.ErrorMd]);

        // Popover, Tooltip, HoverCard
        view.Popover(open: _popOpen.Value, onOpenChange: async o => _popOpen.Value = o,
            contentStyle: [Popover.Content],
            trigger: view => view.Button([Button.OutlineMd], text: "Open"),
            contentSlot: view => { /* ... */ });
        view.Tooltip(contentStyle: [Tooltip.Content],
            trigger: view => view.Button([Button.OutlineMd], text: "Hover me"),
            contentSlot: view => view.Text(text: "Tooltip text"));
        view.HoverCard(contentStyle: [HoverCard.Content],
            trigger: view => view.Text([Text.Link], "@user"),
            contentSlot: view => { /* ... */ });

        // DropdownMenu: each row is a Button styled with Menu.Item
        view.DropdownMenu(open: _menuOpen.Value, onOpenChange: async o => _menuOpen.Value = o,
            contentStyle: [Menu.Content],
            trigger: view => view.Button([Button.OutlineMd], text: $"Sort: {_sort.Value}"),
            content: view =>
            {
                foreach (var option in new[] { "Newest", "Oldest", "Name" })
                {
                    view.Button([Menu.Item], text: option, onClick: async () =>
                    {
                        _sort.Value = option;
                        _menuOpen.Value = false;
                    });
                }
            });

        // Toast
        view.Toast(open: _toastOpen.Value, onOpenChange: async o => _toastOpen.Value = o,
            viewportStyle: [Toast.ViewportBottomCenter], toastStyle: [Toast.Base],
            title: "Success", titleStyle: [Toast.Title],
            description: "Action completed", descriptionStyle: [Toast.Description],
            durationMs: 3000, showClose: true, closeStyle: [Toast.Close]);
        #endregion
    }
}

file sealed class NavigationExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<string> _tab = new("home");
    private readonly Reactive<string> _accordionValue = new("");
    private readonly Reactive<bool> _open = new(false);
    private readonly Reactive<bool> _hasMore = new(true);
    private readonly Reactive<bool> _loading = new(false);
    private readonly ReactiveList<string> _items = new();

    private static void RenderHome(UIView view) => view.Text(text: "Home: recent activity and shortcuts");

    private static void RenderSettings(UIView view) => view.Text(text: "Settings: profile, notifications and language");

    private static Task LoadMoreItems() => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region example:navigation-components
        // Tabs with routing
        view.Tabs(value: _tab.Value, onValueChange: async v =>
            {
                _tab.Value = v;
                await app.Navigation.SetPathAsync($"/{v}");
            },
            listContainerStyle: [Card.Default, "p-2 mb-4"],
            listStyle: [Tabs.List], triggerStyle: [Tabs.Trigger], contentStyle: [Tabs.Content],
            tabs: [
                new TabItem("home", "Home", RenderHome),
                new TabItem("settings", "Settings", RenderSettings),
            ]);

        // Accordion (single open item at a time)
        view.AccordionSingle(value: _accordionValue.Value,
            onValueChange: async v => _accordionValue.Value = v,
            content: view =>
            {
                view.AccordionItem(value: "item1", content: view =>
                {
                    view.AccordionHeader(content: view =>
                    {
                        view.AccordionTrigger(content: view => view.Text(text: "Section 1"));
                    });
                    view.AccordionContent(content: view => view.Text(text: "Content 1"));
                });
            });

        // Collapsible
        view.Collapsible(open: _open.Value, onOpenChange: async o => _open.Value = o,
            content: view =>
            {
                // The trigger renders a <button> itself: style it and give it text, never a nested view.Button.
                view.CollapsibleTrigger([Button.OutlineSm], content: view => view.Text(text: "Toggle"));
                view.CollapsibleContent(content: view => { /* ... */ });
            });

        // InfiniteScrollView
        view.InfiniteScrollView(
            rootStyle: ["h-[400px]"],
            hasMore: _hasMore.Value,
            loading: _loading.Value,
            onNearEnd: async args => { await LoadMoreItems(); },
            content: view => { foreach (var item in _items) { view.Text(text: item); } });
        #endregion
    }
}

file sealed class EditorsExamples
{
    private const string StarterShader = """
        void mainImage(out vec4 color, in vec2 fragCoord)
        {
            vec2 uv = fragCoord / iResolution.xy;
            color = vec4(uv, 0.5 + 0.5 * sin(iTime), 1.0);
        }
        """;

    private readonly UserReactive<string> _pageHtml = new("<h2>Onboarding</h2><p>Start with the team handbook.</p>");
    private readonly UserReactive<string> _shaderSource = new(StarterShader);
    private readonly ClientReactive<string?> _compileError = new(null);
    private readonly ClientReactive<double> _strength = new(0.8);
    private readonly ClientReactive<int> _clearPad = new(0);
    private readonly ClientReactive<int> _savePad = new(0);
    private readonly ClientReactive<string?> _drawing = new(null);
    private readonly ClientReactive<string?> _drawingMask = new(null);

    public void RenderRichText(UIView view)
    {
        #region example:rich-text-editor
        // The value is an HTML string; tools: picks the toolbar buttons (null shows the default set)
        view.RichTextEditor(value: _pageHtml.Value, placeholder: "Write the page…",
            tools: [RichTextTool.Bold, RichTextTool.Italic, RichTextTool.Heading2, RichTextTool.BulletList, RichTextTool.Link, RichTextTool.Undo, RichTextTool.Redo],
            onValueChange: async html => _pageHtml.Value = html);
        #endregion
    }

    public void RenderCodeWithPreview(UIView view)
    {
        #region example:code-editor-live-preview
        // CodeEditor is a monospace textarea with line numbers; language: only labels it, nothing is highlighted
        view.Row(["gap-4 h-80"], content: view =>
        {
            view.CodeEditor(["default", "flex-1 min-w-0"], value: _shaderSource.Value, language: "glsl",
                onValueChange: async source => { _shaderSource.Value = source; _compileError.Value = null; });

            view.Column(["flex-1 min-w-0 gap-2"], content: view =>
            {
                // An empty shaderSource throws, so the canvas renders only once there is code to compile.
                // A source that fails to compile reports its log and leaves the last good shader running
                if (!string.IsNullOrWhiteSpace(_shaderSource.Value))
                {
                    view.ShadertoyCanvas(["w-full flex-1 min-h-0 rounded-lg"], shaderSource: _shaderSource.Value,
                        onCompileError: async log => _compileError.Value = log);
                }

                if (_compileError.Value is { } error)
                {
                    view.Text([Text.Caption, "font-mono whitespace-pre-wrap text-error-600"], error);
                }
            });
        });
        #endregion
    }

    public void RenderShaderUniforms(UIView view)
    {
        #region example:shadertoy-uniforms
        // Each uniforms: entry is declared for the shader (uniform float uStrength;), so the GLSL only reads it.
        // A new value reaches the running shader without a recompile
        view.ShadertoyCanvas(["w-full h-64 rounded-lg"], shaderSource: """
            void mainImage(out vec4 color, in vec2 fragCoord)
            {
                vec2 uv = fragCoord / iResolution.xy;
                color = vec4(uv * uStrength, 0.5 + 0.5 * sin(iTime), 1.0);
            }
            """,
            uniforms: new Dictionary<string, ShaderUniform> { ["uStrength"] = ShaderUniform.Float((float)_strength.Value) });
        view.Slider(min: 0, max: 1, step: 0.05, bind: _strength, ariaLabel: "Strength");
        #endregion
    }

    public void RenderDrawingPad(UIView view)
    {
        #region example:image-editor-drawing-pad
        // No src: a blank pad the size of its container. background: makes it opaque, as a vision model expects.
        // saveMask: adds MaskData (white where the strokes are) to onSave
        view.ImageEditorCanvas(["w-full aspect-[2/1] rounded-lg border"], background: "#ffffff",
            brushColor: "#111827", brushWidth: 6, saveMask: true,
            triggerClear: _clearPad.Value, triggerSave: _savePad.Value,
            onSave: async args => { _drawing.Value = args.ImageData; _drawingMask.Value = args.MaskData; });

        view.Row(["gap-2"], content: row =>
        {
            row.Button([Button.OutlineSm], text: "Clear", onClick: async () => _clearPad.Value++);
            row.Button([Button.PrimarySm], text: "Check", onClick: async () => _savePad.Value++);
        });
        #endregion
    }
}

internal sealed partial class AgentGuideExamples
{

    private void DocSortableList(UIView view)
    {
        #region example:sortable-list
        view.SortableList(
            items: _items.Value,
            onReorder: async args => _items.ReplaceAll(args.NewOrder),
            itemContent: (v, id) => v.Text([Text.Body], id));
        #endregion
    }

    private static void DocTextAndContent(UIView view)
    {
        #region example:text-and-content
        view.Text([Text.Display], "Large Title");
        view.Text([Text.H2], "Section Heading");
        view.Text([Text.Body], "Body text");
        view.Text([Text.Caption, "text-muted-foreground"], "Small caption");
        view.Heading([Text.H3], text: "Heading component");   // or positional: view.Heading("Title")
        view.Markdown("**Bold** and `code`");

        // Text is an inline span, so Texts nested in one Text flow as one sentence
        view.Text([Text.Body], content: sentence =>
        {
            sentence.Text("Ship it ");
            sentence.Text(["font-semibold text-brand-secondary"], text: "today");
            sentence.Text(", not next week.");
        });
        #endregion
    }

    private static void DocDisplayComponents(UIView view, string imageUrl, byte[] bytes)
    {
        #region example:display-components
        view.Icon([Icon.Default], name: "check");          // Lucide icon names
        view.Box([Icon.Spinner, "w-4 h-4"]);              // CSS-only spinning loader (use Box, not Icon)
        view.Icon([Icon.Xs, "animate-spin"], name: "loader-2");  // Spinning Lucide icon (Icon.Spinner would also draw its bordered ring)
        view.Image(["max-w-full h-auto"], src: imageUrl);   // From URL
        view.Image(["rounded-lg"], data: bytes, mimeType: MimeTypes.ImageJpeg);  // From bytes
        view.Skeleton(["w-2/3"], size: SkeletonSize.Lg);   // Loading placeholder; style is added to the pulse
        view.Box([Skeleton.Title]);                        // Or a token: Skeleton.Title, Text, TextSm, Avatar, Card, Button, Input
        #endregion
    }

    private static void DocLayoutComponents(UIView view)
    {
        #region example:layout-components
        view.Box([Card.Default, "p-6"], content: view => { /* ... */ });
        view.Row([Layout.Row.Md, "flex-wrap"], content: view => { /* ... */ });
        view.Column([Layout.Column.Lg], content: view => { /* ... */ });
        view.Flex(["flex gap-4"], content: view => { /* ... */ });
        view.ScrollArea(rootStyle: ["h-[400px]"], content: view => { /* ... */ });
        view.Separator(["default", "my-4"]);
        view.AspectRatio(["w-full"], ratio: 16.0 / 9.0, content: view => { /* ... */ });
        #endregion
    }

    private static void DocActionButton(UIView view)
    {
        #region example:action-button
        view.ActionButton([Button.PrimaryMd],
            action: ActionKind.CopyToClipboard,
            options: new CopyToClipboardActionOptions { Text = "Copied text!" },
            onActionComplete: async e => { /* e.Success */ },
            content: v => { v.Icon([Icon.Default, "mr-2"], name: "clipboard-copy"); v.Text(text: "Copy"); });
        #endregion
    }

    private void DocCaptureButtons(UIView view)
    {
        #region example:capture-buttons
        // Audio capture (microphone)
        view.CaptureButton([Button.OutlineMd, Button.Icon],
            kind: MediaCaptureKind.Audio,
            captureMode: MediaCaptureButtonMode.Toggle,  // or Hold
            audioOptions: new ClientAudioCaptureOptions { /* ... */ },
            onCaptureStart: async args => { _streamId.Value = args.StreamId; },
            onCaptureStop: async args => { _streamId.Value = null; },
            content: v => v.Icon([Icon.Default], name: "mic"));

        // Video capture (camera). Capture media always routes to the app on the server,
        // never to the other clients — the app shows it with Video.Play onto a surface.
        view.CaptureButton([Button.OutlineMd, Button.Icon],
            kind: MediaCaptureKind.Camera,
            captureMode: MediaCaptureButtonMode.Toggle,
            videoOptions: new ClientVideoCaptureOptions { Framerate = 10, Width = 1280, Height = 720 },
            onCaptureStart: async args => { /* args.StreamId is the Id of the VideoInput that Video.InputStartedAsync hands the app */ },
            onCaptureStop: async args => { /* cleanup */ },
            content: v => v.Icon([Icon.Default], name: "video"));
        #endregion
    }
}
