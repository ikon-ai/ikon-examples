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
        // Composer is the input bar, which clears itself on Enter or Send
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
                }));
        #endregion
    }
}

file sealed class InputsExamples
{
    private readonly Reactive<string> _text = new("");
    private readonly Reactive<bool> _checked = new(false);
    private readonly Reactive<bool> _enabled = new(false);
    private readonly Reactive<double> _slider = new(0);
    private readonly Reactive<string> _selected = new("");
    private readonly Reactive<string> _radio = new("");
    private readonly Reactive<bool> _isDragging = new(false);

    private static Task HandleSubmit(string submitted) => Task.CompletedTask;

    public void Render(UIView view, bool isLoading)
    {
        #region example:input-components
        view.Button([Button.PrimaryMd], text: "Click", onClick: async () => { /* ... */ });
        view.Button([Button.OutlineMd], text: "Secondary", disabled: isLoading, onClick: async () => { /* ... */ });
        view.Button([Button.GhostMd, Button.Icon], onClick: async () => { /* ... */ },
            content: v => v.Icon([Icon.Default], name: "settings"));
        view.TextField(bind: _text, placeholder: "Enter text",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Enter submits; input auto-clears after submit
        view.TextArea(bind: _text, style: ["min-h-[100px]"], placeholder: "Type a message...",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Ctrl+Enter submits; input auto-clears after submit
        // onSubmit's parameter is the submitted value. With bind:, the bound reactive (`_text.Value`)
        // holds that same value while onSubmit runs; a clearing field (the default here) writes "" to
        // it once onSubmit returns, unless the user typed again meanwhile.
        // Note: both TextField and TextArea clear on submit only when an onSubmit handler is set; a bound field
        // with no onSubmit keeps its value. Pass clearOnSubmit: true/false to override either way.
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

    public void Render(UIView view)
    {
        #region example:overlay-components
        // Dialog
        view.Dialog(open: _open.Value, onOpenChange: async o => _open.Value = o,
            overlayStyle: [Dialog.Overlay], contentStyle: [Dialog.Content],
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
