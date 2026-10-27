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
    private readonly UserReactive<string> _text = new("");
    private readonly ClientReactive<string> _query = new("");
    private readonly ClientReactive<bool> _suggestionsOpen = new(false);
    private readonly ClientReactive<bool> _checked = new(false);
    private readonly ClientReactive<bool> _enabled = new(false);
    private readonly ClientReactive<double> _slider = new(0);
    private readonly ClientReactive<string> _selected = new("");
    private readonly ClientReactive<string> _radio = new("");
    private readonly ClientReactive<bool> _isDragging = new(false);
    private readonly ClientReactive<string> _layout = new("list");
    private readonly ClientReactive<string> _date = new("");
    private readonly ClientReactive<string> _time = new("09:00");
    private readonly ClientReactive<string> _day = new("");
    private readonly List<string> _fullDays = ["2026-12-24", "2026-12-25"];

    private static Task HandleSubmit(string submitted) => Task.CompletedTask;
    private static Task RunSearchAsync(string query) => Task.CompletedTask;

    public void Render(UIView view, bool isLoading)
    {
        #region example:input-components
        view.Button([Button.PrimaryMd], text: "Click", onClick: async () => { /* ... */ });
        view.Button([Button.OutlineMd], text: "Secondary", disabled: isLoading, onClick: async () => { /* ... */ });
        view.Button([Button.GhostMd, Button.Icon], text: "Settings", onClick: async () => { /* ... */ },
            content: v => v.Icon([Icon.Default], name: "settings"));
        // icon: draws an icon next to the text: label. A content: lambda is drawn in place of the text, and text: then only names the button.
        view.Button([Button.OutlineMd], text: "Listen", icon: "headphones", onClick: async () => { /* ... */ });
        view.TextField(bind: _text, placeholder: "Enter text",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Enter submits; input auto-clears after submit
        view.TextArea(bind: _text, style: ["default", "min-h-[100px]"], placeholder: "Type a message...",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Ctrl+Enter submits; input auto-clears after submit
        // onSubmit's parameter is the submitted value. With bind:, the bound reactive (`_text.Value`)
        // holds that same value while onSubmit runs; a clearing field (the default here) writes "" to
        // it once onSubmit returns, unless the user typed again meanwhile.
        // Note: both TextField and TextArea clear on submit only when an onSubmit handler is set; a bound field
        // with no onSubmit keeps its value. Pass clearOnSubmit: true/false to override either way.
        // A search box keeps the query on screen after Enter, so it opts out. Beside a button in a row it
        // takes flex-1, placed after "default" so the field keeps its theme (["flex-1"] alone is a bare input, IKON025):
        view.TextField(["default", "flex-1"], bind: _query, placeholder: "Search", clearOnSubmit: false,
            onSubmit: async query => { await RunSearchAsync(query); });
        view.TextField(bind: _text, type: "number", min: "0", step: "1", label: "Quantity");  // a number field: the bound value is still a string, so int.TryParse(_text.Value, out var quantity)
        view.TextArea(bind: _text, placeholder: "Notes", maxLength: 500);  // maxLength: caps typing and pasting (TextField takes it too)
        // onFocus/onBlur (TextField and TextArea) fire once per real change of focus, autoFocus included;
        // a re-render or switching to another app fires neither. A combobox opens its list on focus:
        view.TextField(bind: _query, placeholder: "City", ariaLabel: "City",
            onFocus: () => { _suggestionsOpen.Value = true; }, onBlur: () => { _suggestionsOpen.Value = false; });
        // Checkbox / Switch / Slider auto-render their inner part (the check mark, the switch
        // thumb, the slider track+thumb) AND, like RadioGroup / Toggle, their default styling —
        // the bare call below is all you need (a RadioGroupItem's checked fill is the whole dot, so
        // it needs no RadioGroupIndicator either); you do NOT have to compose a
        // CheckboxIndicator / SwitchThumb / SliderTrack child or pass a [*.Default] style.
        // Pass a content: lambda only to put CUSTOM content inside (e.g. a different icon), or a
        // style: array only to override the default look. To render a checkbox with no check mark
        // at all, opt out explicitly with content: _ => { }.
        view.Checkbox(bind: _checked, label: "Enable feature");  // label: renders a clickable trailing label
        view.Switch(bind: _enabled, label: "Notifications");
        view.Slider(bind: _slider, min: 0, max: 100, step: 1, label: "Volume");  // scalar single-thumb; pass value: [a, b] lists for multi-thumb
        view.Select(bind: _selected, ariaLabel: "Plan", placeholder: "Choose...", triggerStyle: [Input.DefaultSm, "w-40"],
            options: [new SelectOption("a", "Option A"), new SelectOption("b", "Option B")]);
        view.RadioGroup(label: "Plan", bind: _radio,
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
        // ToggleGroupSingle holds one value, ToggleGroupMultiple a list. With no style a group takes the theme's Toggle.Group and each item Toggle.Default; ToggleGroup.Root and ToggleGroup.Item are the same tokens
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
            multiple: true,                     // without it, one file per pick; each file is a separate upload with its own onUploadComplete
            maxFileSize: 10 * 1024 * 1024);    // optional max size in bytes, per file

        // A phone photo is shrunk on the device before maxFileSize is checked: longest side 1600 px, as
        // UploadImageFormat.Webp (JPEG where the client cannot encode it). The file name and MIME type follow, and HEIC photos are converted.
        view.FileUpload(
            onUploadComplete: async args => { /* args.FileName is "IMG_0001.webp", args.MimeType "image/webp" */ },
            accept: ["image/*"],
            maxFileSize: 2 * 1024 * 1024,
            maxImageDimension: 1600, imageQuality: 0.8, imageFormat: UploadImageFormat.Webp);

        // directory: true picks or drops a whole folder; each file's args.RelativePath is its path inside it ("Photos/2024/a.jpg")
        view.FileUpload(
            directory: true,
            onUploadComplete: async args => { /* args.RelativePath, args.FileName, args.LocalTempFilePath */ });

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

        #region example:date-time-pickers
        // Dates are ISO yyyy-MM-dd strings and times 24-hour HH:mm, both in value: and in onValueChange.
        // label: shows above the picker and names it, so ikon browse fills it by that name.
        view.DatePicker(label: "Date", value: _date.Value, labelLocale: "en-GB",
            minDate: DateTime.UtcNow.ToString("yyyy-MM-dd"), onValueChange: async date => _date.Value = date);
        view.TimePicker(label: "Starts", value: _time.Value, minuteStep: 15,
            onValueChange: async time => _time.Value = time);
        // A month grid that stays open, for a page that shows which days are free
        view.Calendar(value: _day.Value, disabledDates: _fullDays, onValueChange: async day => _day.Value = day);
        #endregion
    }
}

file sealed class OverlaysExamples
{
    private readonly ClientReactive<bool> _open = new(false);
    private readonly ClientReactive<bool> _alertOpen = new(false);
    private readonly ClientReactive<bool> _popOpen = new(false);
    private readonly ClientReactive<bool> _renameOpen = new(false);
    private readonly ClientReactive<bool> _toastOpen = new(false);
    private readonly ClientReactive<bool> _menuOpen = new(false);
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

        // The same panel from the built-in parts: title and description draw the header, content goes
        // under them, footer holds the buttons, and setting a title also adds the X close button
        view.Dialog(open: _renameOpen.Value, onOpenChange: async o => _renameOpen.Value = o,
            trigger: view => view.Button([Button.OutlineMd], text: "Rename"),
            title: "Rename the board",
            description: "Everyone on the board sees the new name.",
            content: view => view.Text([Text.Body], "The fields go here"),
            footer: view => view.Button([Button.PrimaryMd], text: "Save", onClick: async () => _renameOpen.Value = false));

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

    public void RenderHoverOrTap(UIView view, string explanation)
    {
        #region example:hover-or-tap
        // A Popover opens on a click and on a tap, so one control explains itself on every device.
        view.Popover(contentStyle: [Popover.Content, "w-72"],
            trigger: tap => tap.Button([Button.GhostSm], text: "Stroke rate"),
            contentSlot: panel => panel.Text([Text.BodySm], explanation));
        #endregion
    }
}

file sealed class NavigationExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ClientReactive<string> _tab = new("home");
    // The path is one client's, so the page it selects is too.
    private readonly ClientReactive<string> _page = new("/");
    private readonly ClientReactive<string> _accordionValue = new("");
    private readonly ClientReactive<bool> _open = new(false);
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
            listStyle: [Tabs.NavList], triggerStyle: [Tabs.NavTriggerMd], contentStyle: [Tabs.Content],
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

        #region example:bottom-nav
        // The page scrolls above the menu; the outlet holds the page for the client's path
        view.Column(["h-[480px]"], content: shell =>
        {
            shell.ScrollArea(["flex-1 min-h-0"], content: body =>
                body.NavOutlet(path: _page.Value, content: page =>
                {
                    if (_page.Value == "/settings")
                    {
                        RenderSettings(page);
                    }
                    else
                    {
                        RenderHome(page);
                    }
                }));
            shell.NavBar(content: bar =>
            {
                bar.NavItem("/", label: "Home", icon: "house");
                bar.NavItem("/settings", label: "Settings", icon: "settings", badge: 2);
            });
        });
        #endregion
    }

    public void Route()
    {
        #region example:bottom-nav-routing
        // A press on an item, an in-app link and a back gesture all arrive here
        app.Navigation.PathChangedAsync += args =>
        {
            _page.Value = args.Path;
            return Task.CompletedTask;
        };
        #endregion
    }

    private static void DocFeedInTab(UIView view, IReadOnlyList<FeedSlide> slides)
    {
        #region example:feed-in-tab
        // The root and the panel are flex children down to the feed, so each has a definite height;
        // the absolute box gives the feed a height it cannot collapse from
        view.Tabs(
            defaultValue: "feed",
            rootStyle: ["flex h-dvh flex-col"],
            contentStyle: [Tabs.ContentFill],
            tabs: [
                new TabItem("feed", "Feed", panel => panel.Box(["relative min-h-0 flex-1"], content: frame =>
                    frame.Box(["absolute inset-0"], content: fill =>
                        fill.FeedScroller(slides, style: [FeedScroller.Root, "h-full"])))),
                new TabItem("about", "About", panel => panel.Text(text: "About this feed")),
            ]);
        #endregion
    }

    private static void DocBottomTabBar(UIView view)
    {
        #region example:bottom-tab-bar
        // Root and panel are a flex column with a definite height; the list container comes last in
        // the column (order-last) and scrolls sideways, so five tabs still fit a 390px phone;
        // md:max-w-md md:mx-auto keeps the bar from spanning a wide desktop window
        view.Tabs(
            defaultValue: "home",
            rootStyle: ["flex h-dvh flex-col"],
            contentStyle: [Tabs.ContentFill],
            listContainerStyle: ["order-last shrink-0 overflow-x-auto border-t border-secondary md:mx-auto md:w-full md:max-w-md"],
            listStyle: [Tabs.List, "w-full min-w-max"],
            triggerStyle: [Tabs.Trigger, "flex-1"],
            tabs: [
                new TabItem("home", "Home", panel => panel.ScrollArea(["h-full"], content: list => list.Text(text: "Home"))),
                new TabItem("search", "Search", panel => panel.ScrollArea(["h-full"], content: list => list.Text(text: "Search"))),
            ]);
        #endregion
    }
}

file sealed class EventCalendarExamples
{
    private sealed record CalendarEvent(DateOnly Day, string Title);

    private readonly ClientReactive<DateOnly> _month = new(new DateOnly(2026, 10, 1));
    private readonly ReactiveList<CalendarEvent> _events = new();

    public void Render(UIView view)
    {
        #region example:event-calendar
        // view.Calendar picks a date and draws nothing in the day. A month with events in its days
        // is a 7-column Grid: blank cells pad the first week (weeks start on Monday), one Box per day.
        var first = _month.Value;
        var padding = ((int)first.DayOfWeek + 6) % 7;

        view.Row(["items-center justify-between"], content: header =>
        {
            header.Button([Button.GhostSm], ariaLabel: "Previous month", text: "<", onClick: async () => _month.Value = first.AddMonths(-1));
            header.Text([Text.H4], first.ToString("MMMM yyyy"));
            header.Button([Button.GhostSm], ariaLabel: "Next month", text: ">", onClick: async () => _month.Value = first.AddMonths(1));
        });

        view.Grid(["grid-cols-7 gap-1"], content: grid =>
        {
            foreach (var name in new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" })
            {
                grid.Text([Text.Caption], name);
            }

            for (var blank = 0; blank < padding; blank++)
            {
                grid.Box(["min-h-20"], key: $"blank-{blank}");
            }

            for (var number = 1; number <= DateTime.DaysInMonth(first.Year, first.Month); number++)
            {
                var day = new DateOnly(first.Year, first.Month, number);
                var label = number.ToString();

                grid.Box([Card.Default, "flex min-h-20 min-w-0 flex-col gap-1 p-1"], key: day.ToString("yyyy-MM-dd"), content: cell =>
                {
                    cell.Text([Text.Caption], label);

                    foreach (var calendarEvent in _events)
                    {
                        if (calendarEvent.Day == day)
                        {
                            cell.Text([Text.BodySm, "truncate"], calendarEvent.Title);
                        }
                    }
                });
            }
        });
        #endregion
    }
}

file sealed class MenubarToolbarExamples
{
    private readonly ClientReactive<bool> _showGrid = new(true);
    private readonly ClientReactive<string> _align = new("left");
    private readonly ClientReactive<string> _status = new("Ready");

    private Task RunAsync(string command)
    {
        _status.Value = $"Ran {command}";
        return Task.CompletedTask;
    }

    public void Render(UIView view)
    {
        #region example:menubar-and-toolbar
        // Neither draws a themed look of its own: give each part its Menubar.*, Toolbar.* or DropdownMenu.* token
        view.Menubar([Menubar.Root], content: bar =>
        {
            bar.MenubarMenu(value: "file", content: menu =>
            {
                menu.MenubarTrigger([Menubar.Trigger], content: t => t.Text(text: "File"));
                menu.MenubarContent([Menubar.Content, "p-1"], content: items =>
                {
                    items.MenubarItem([Menubar.Item], onSelect: async () => await RunAsync("New report"), content: i => i.Text(text: "New report"));
                    items.MenubarSeparator([Menubar.Separator, "my-1"]);
                    items.MenubarItem([Menubar.Item], disabled: true, content: i => i.Text(text: "Export (no reports yet)"));
                });
            });

            bar.MenubarMenu(value: "view", content: menu =>
            {
                menu.MenubarTrigger([Menubar.Trigger], content: t => t.Text(text: "View"));
                menu.MenubarContent([Menubar.Content, "p-1"], content: items =>
                {
                    items.MenubarCheckboxItem([DropdownMenu.CheckboxItem],
                        isChecked: _showGrid.Value ? CheckedState.Checked : CheckedState.Unchecked,
                        onValueChange: async state => _showGrid.Value = state == CheckedState.Checked,
                        content: item =>
                        {
                            item.MenubarItemIndicator(["absolute left-2"], content: mark => mark.Icon([Icon.Xs], name: "check"));
                            item.Text(text: "Show grid");
                        });
                });
            });
        });

        // ToolbarButton takes onClick: and no ariaLabel:, so its content names it
        view.Toolbar([Toolbar.Root, "mt-2 items-center"], content: bar =>
        {
            bar.ToolbarButton([Toolbar.Button], onClick: async () => await RunAsync("Undo"), content: b => b.Text(text: "Undo"));
            bar.ToolbarSeparator([Toolbar.Separator, "h-5"]);
            bar.ToolbarToggleGroupSingle([Toolbar.ToggleGroup], value: _align.Value, onValueChange: async v => _align.Value = v, content: group =>
            {
                group.ToolbarToggleItem([Toolbar.ToggleItem], value: "left", content: i => i.Text(text: "Left"));
                group.ToolbarToggleItem([Toolbar.ToggleItem], value: "center", content: i => i.Text(text: "Center"));
            });
        });

        view.Text([Text.Caption], _status.Value);
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

    public void RenderSavedRichText(UIView view)
    {
        #region example:rich-text-display
        // Saved editor HTML shown to readers: the same component, read-only and without its toolbar.
        // view.Markdown would show the tags as text.
        view.RichTextEditor(value: _pageHtml.Value, readOnly: true, showToolbar: false, ariaLabel: "Onboarding page");
        #endregion
    }

    public void RenderCodeWithPreview(UIView view)
    {
        #region example:code-editor-live-preview
        // CodeEditor is a monospace textarea with line numbers; language: only labels it, nothing is highlighted
        view.Row(["gap-4 h-80"], content: view =>
        {
            view.CodeEditor(["default", "flex-1 min-w-0"], value: _shaderSource.Value, language: "glsl", ariaLabel: "Shader source",
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
        // The canvas writes "uniform float uStrength;" for each uniforms: entry, so the GLSL reads it and declares nothing.
        // A new value reaches the running shader without a recompile
        view.ShadertoyCanvas(["w-full h-64 rounded-lg"], shaderSource: """
            void mainImage(out vec4 color, in vec2 fragCoord)
            {
                vec2 uv = fragCoord / iResolution.xy;
                color = vec4(uv * uStrength, 0.5 + 0.5 * sin(iTime), 1.0);
            }
            """,
            uniforms: new Dictionary<string, ShaderUniform> { ["uStrength"] = ShaderUniform.Float((float)_strength.Value) });
        view.Slider(min: 0, max: 1, step: 0.05, bind: _strength, label: "Strength", showValue: true);
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
    private sealed record SoundRow(string Name, string Code);

    private static readonly IReadOnlyList<SoundRow> DocSounds =
        [.. Enumerable.Range(1, 1000).Select(number => new SoundRow($"Rifle shot {number}", $"SFX-{number:00000}"))];

    private void DocVirtualList(UIView view) => RenderSounds(view, DocSounds);

    #region example:virtual-list
    // A field, so each client keeps its own first visible row
    private readonly ClientReactive<int> _soundScrollIndex = new(0);

    private void RenderSounds(UIView view, IReadOnlyList<SoundRow> sounds)
    {
        // The list scrolls inside a parent of fixed height; itemHeight is each row's height in pixels
        view.Box(["h-[420px]"], content: view =>
            view.VirtualList(itemCount: sounds.Count, itemHeight: 56, scrollIndex: _soundScrollIndex,
                onRenderItem: (row, index) =>
                {
                    var sound = sounds[index];

                    row.Row([Layout.Row.SpaceBetween, "h-14 px-3 border-b border-secondary"], content: view =>
                    {
                        view.Text([Text.Body], sound.Name);
                        view.Text([Text.Caption], sound.Code);
                    });
                }));
    }
    #endregion

    private void DocSortableList(UIView view)
    {
        #region example:sortable-list
        view.SortableList(
            items: _items.Value,
            onReorder: async args => _items.ReplaceAll(args.NewOrder),
            itemContent: (v, id) => v.Text([Text.Body], id));
        #endregion
    }

    private void DocSortableShelf(UIView view)
    {
        #region example:sortable-shelf
        // Two rows that scroll sideways; a held touch picks an item up, so a swipe still scrolls.
        // A tap opens the item, a double tap favourites it.
        view.SortableList(
            items: _items.Value,
            rows: 2,
            longPressDelay: 400,
            onReorder: async args => _items.ReplaceAll(args.NewOrder),
            itemStyle: ["w-40"],
            itemContent: (v, id) => v.Box(["flex flex-col gap-1"], ariaLabel: id,
                onClick: () => _openedItem.Value = id,
                onDoubleTap: () =>
                {
                    if (!_favourites.Remove(id))
                    {
                        _favourites.Add(id);
                    }
                },
                content: card =>
                {
                    card.Text([Text.Body], id);
                    card.Text([Text.Caption], _favourites.Contains(id) ? "Favourite" : _openedItem.Value == id ? "Open" : "");
                }));
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
        // Element classes win over Markdown's own spacing and link look, no !important needed
        view.Markdown("One paragraph.\n\nAnother, with a [link](/docs).", paragraphStyle: ["my-2"], linkStyle: ["text-brand-secondary"]);
        // Shrinks from Text.Display down to 16px to stay on one line in its width, then ends in an ellipsis
        view.Text([Text.Display, "w-full"], "A title too long for a phone", fit: TextFit.Shrink, minFontSize: 16);

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
        view.Image(["w-8 h-8 text-brand-secondary"], src: imageUrl, tint: true);  // Monochrome art painted in the text colour, alpha kept
        view.Image(["w-full aspect-video object-cover rounded-lg"], src: imageUrl, alt: "Cover",
            blurhash: "LEHV6nWB2yk8pyo0adR*.7kCMdnj",   // Drawn until the picture loads, which then fades in
            fallback: f => f.Box(["w-full h-full flex items-center justify-center bg-secondary text-3xl"], content: b => b.Text("🚲")));  // When the source fails to load
        view.Skeleton(["w-2/3"], size: SkeletonSize.Lg);   // Loading placeholder; style is added to the pulse
        view.Box([Skeleton.Title]);                        // Or a token: Skeleton.Title, Text, TextSm, Avatar, Card, Button, Input
        view.Badge("Ready", tone: SemanticTone.Success, size: BadgeSize.Sm);  // Size is a parameter: there is no Badge.Sm; tones are Neutral, Brand, Success, Warning, Error, Info (no Danger)
        view.Badge([Badge.BrandSm], "New");                // Or a token that names tone and size
        view.Alert("Card declined", tone: SemanticTone.Error, description: "Try another card.");  // The first argument is the title; an error is Error, not Danger
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

    private sealed record PlanRow(string Name, int Seats, decimal Price);

    private static Task OpenPlan(PlanRow plan) => Task.CompletedTask;

    private static void DocContentGrid(UIView view, IReadOnlyList<PlanRow> plans)
    {
        #region example:content-grid
        // Width is a CSS track size; a column without one takes Flex shares of the space left
        view.ContentGrid(
            [
                new ContentGridColumn("Plan", Flex: 1),
                new ContentGridColumn("Seats", Width: "6rem", Align: ColumnAlign.Right),
                new ContentGridColumn("Price", Width: "8rem", Align: ColumnAlign.Right),
                new ContentGridColumn(null, Width: "7rem"),
            ],
            style: [ContentGrid.Bordered],
            content: grid =>
            {
                // One child per cell, row after row; Align right-aligns the cells' text as well
                foreach (var plan in plans)
                {
                    grid.Text([ContentGrid.Cell], plan.Name);
                    grid.Text([ContentGrid.Cell, "tabular-nums"], plan.Seats.ToString());
                    grid.Text([ContentGrid.Cell, "tabular-nums"], $"{plan.Price:N0} €");
                    grid.Box([ContentGrid.Cell], content: cell =>
                        cell.Button([Button.OutlineSm], text: "Open", onClick: async () => await OpenPlan(plan)));
                }
            });
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

file sealed class SortableTableExamples
{
    private sealed record Contact(string Name, string City, int Orders);

    private static readonly Contact[] Contacts = [new("Aino", "Oulu", 4), new("Eero", "Turku", 9)];

    #region example:sortable-table
    private readonly ClientReactive<string> _sortKey = new("name");
    private readonly ClientReactive<bool> _descending = new(false);

    private void RenderTable(UIView view)
    {
        var sortKey = _sortKey.Value;
        var descending = _descending.Value;
        var rows = (sortKey switch
        {
            "city" => Contacts.OrderBy(c => c.City),
            "orders" => Contacts.OrderBy(c => c.Orders),
            _ => Contacts.OrderBy(c => c.Name),
        }).ToList();

        if (descending)
        {
            rows.Reverse();
        }

        // A header sorts by its button: pressing the active column flips the direction.
        void sortHead(UIView head, string key, string label) => head.TableHead(content: cell =>
            cell.Button([Button.GhostSm], text: sortKey == key ? $"{label} {(descending ? "▼" : "▲")}" : label,
                onClick: async () =>
                {
                    _descending.Value = sortKey == key && !descending;
                    _sortKey.Value = key;
                }));

        view.Table(content: table =>
        {
            table.TableHeader(content: header => header.TableRow(content: row =>
            {
                sortHead(row, "name", "Name");
                sortHead(row, "city", "City");
                sortHead(row, "orders", "Orders");
            }));
            table.TableBody(content: body =>
            {
                foreach (var contact in rows)
                {
                    body.TableRow(key: contact.Name, content: row =>
                    {
                        row.TableCell(contact.Name);
                        row.TableCell(contact.City);
                        row.TableCell(contact.Orders.ToString());
                    });
                }
            });
        });
    }
    #endregion
}

file sealed class BoxObserverExamples
{
    private sealed record Message(string Id, string Text);

    private readonly ClientReactive<int> _columns = new(2);
    private readonly ReactiveList<string> _cards = new(["Revenue", "Orders", "Customers", "Returns"]);
    private readonly ReactiveList<Message> _messages = new([new("m1", "Welcome to the team"), new("m2", "Your report is ready")]);
    private readonly ReactiveList<string> _readIds = new();

    public void Render(UIView view)
    {
        #region example:box-resize-and-visibility
        // Three columns once the panel itself is 720 px wide, whatever the screen width. Storing the
        // column count, not the width, re-renders only when the count changes.
        view.Box(["grid gap-4", _columns.Value == 3 ? "grid-cols-3" : "grid-cols-2"],
            onResize: async e => _columns.Value = e.Width >= 720 ? 3 : 2,
            content: grid =>
            {
                foreach (var card in _cards)
                {
                    grid.Text([Text.Body], card);
                }
            });

        // Mark a message read once half of it has been on screen.
        foreach (var message in _messages)
        {
            view.Box(["p-3"], key: message.Id,
                onVisibilityChange: async e =>
                {
                    if (e.Visible && !_readIds.Contains(message.Id))
                    {
                        _readIds.Add(message.Id);
                    }
                },
                visibilityOnce: true,
                content: row => row.Text([Text.Body], message.Text));
        }
        #endregion
    }
}
