using System.Text.Json;
using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    // Only lists this tab made are offered for deletion, so a test against a shared site cannot
    // remove someone's real list.
    private const string SpTestListPrefix = "Ikon validation";

    private readonly ReactiveList<ListColumn> _spListColumns = new();
    private readonly Reactive<bool> _spListColumnsShown = new(false);
    private readonly ReactiveList<SharePointContentType> _spListContentTypes = new();
    private readonly Reactive<bool> _spListContentTypesShown = new(false);
    private readonly Reactive<string> _spContentTypeToAdd = new("");
    private readonly Reactive<string> _spNewItemStatus = new("");
    private readonly Reactive<string> _spNewItemOwner = new("");
    private readonly Reactive<ListItem?> _spVersionsItem = new(null);
    private readonly ReactiveList<ListItemVersion> _spItemVersions = new();
    private readonly ReactiveDictionary<string, string> _spListDeltaLinks = new();
    private readonly Reactive<string?> _spListChangesSummary = new(null);
    private readonly Reactive<bool> _spDeleteListOpen = new(false);
    private readonly Reactive<ListItem?> _spAttachmentsItem = new(null);
    private readonly ReactiveList<ListItemAttachment> _spAttachments = new();

    private void RenderSharePointListsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Lists");

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                foreach (var list in _spLists.Where(l => !l.Hidden && l.Template != "documentLibrary"))
                {
                    var selected = _spList.Value?.Id == list.Id;
                    view.Button([selected ? Button.PrimarySm : Button.OutlineSm], key: list.Id, text: list.DisplayName,
                        props: TestId($"sp-list-{list.Name}"),
                        onClick: async () => await SpRunAsync($"open {list.DisplayName}", () => OpenSharePointListAsync(list)));
                }

                view.Button([Button.GhostSm], text: "Create test list", disabled: _spBusy.Value, props: TestId("sp-list-create"),
                    onClick: async () => await SpRunAsync("create a test list", CreateSharePointTestListAsync));
            });

            if (_spList.Value is not { } current)
            {
                view.Text([Text.Caption], "Pick a list to read its items, or create a test list with a choice, date, person and number column");
                return;
            }

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Columns", disabled: _spBusy.Value, props: TestId("sp-list-columns"),
                    onClick: async () => await SpRunAsync("list columns", LoadSharePointListColumnsAsync));
                view.Button([Button.OutlineSm], text: "Content types", disabled: _spBusy.Value, props: TestId("sp-list-content-types"),
                    onClick: async () => await SpRunAsync("list content types", LoadSharePointListContentTypesAsync));
                view.Button([Button.OutlineSm], text: "Check for changes", disabled: _spBusy.Value, props: TestId("sp-list-changes"),
                    onClick: async () => await SpRunAsync("check the list for changes", () => ReadSharePointListChangesAsync(fromNow: false)));
                view.Button([Button.GhostSm], text: "Start from now", disabled: _spBusy.Value, props: TestId("sp-list-changes-now"),
                    onClick: async () => await SpRunAsync("start list changes from now", () => ReadSharePointListChangesAsync(fromNow: true)));

                var deletable = current.DisplayName.StartsWith(SpTestListPrefix, StringComparison.Ordinal);
                view.Button([Button.GhostErrorSm], text: "Delete list", disabled: _spBusy.Value || !deletable, props: new Dictionary<string, object>
                {
                    ["data-testid"] = "sp-list-delete",
                    ["title"] = deletable ? "Moves the list to the site's recycle bin" : "Only lists this tab created can be deleted here",
                }, onClick: async () => _spDeleteListOpen.Value = true);
            });

            view.AlertDialog(
                open: _spDeleteListOpen.Value,
                onOpenChange: async open => _spDeleteListOpen.Value = open,
                title: $"Delete {current.DisplayName}?",
                description: "The list and its items move to the site's recycle bin.",
                cancelLabel: "Keep it",
                actionLabel: "Delete list",
                onAction: async () =>
                {
                    _spDeleteListOpen.Value = false;
                    await SpRunAsync("delete the list", DeleteSharePointListAsync);
                });

            if (_spListChangesSummary.Value is { } changes)
            {
                view.Text([Text.Body, "mb-3"], changes, props: TestId("sp-list-changes-summary"));
            }

            RenderSharePointListColumns(view);
            RenderSharePointListContentTypes(view);
            RenderSharePointNewItem(view);

            if (_spListItems.Count == 0)
            {
                view.Text([Text.Caption], $"{current.DisplayName} has no items");
            }

            foreach (var item in _spListItems)
            {
                view.Row([Layout.Row.Md, "items-center py-2 border-b border-secondary"], key: item.Id, content: view =>
                {
                    view.Column(["flex-1 min-w-0"], content: view =>
                    {
                        view.Text([Text.Body, "truncate"], SpField(item, "Title") ?? $"Item {item.Id}", props: TestId("sp-list-item"));
                        view.Text([Text.Caption, "truncate"], SpFieldSummary(item));
                    });

                    view.Button([Button.GhostSm], text: "Retitle", disabled: _spBusy.Value || _spNewItemTitle.Value.Trim().Length == 0,
                        props: new Dictionary<string, object> { ["title"] = "Sets the title to the text in the new-item box, only if nobody changed the item meanwhile" },
                        onClick: async () => await SpRunAsync("retitle the item", () => RetitleSharePointListItemAsync(item)));
                    view.Button([Button.GhostSm], text: "Versions", disabled: _spBusy.Value,
                        onClick: async () => await SpRunAsync("list item versions", () => LoadSharePointItemVersionsAsync(item)));

                    if (SpRestAvailable)
                    {
                        view.Button([Button.GhostSm], text: "Attachments", disabled: _spBusy.Value, props: TestId("sp-item-attachments"),
                            onClick: async () => await SpRunAsync("list attachments", () => LoadSharePointAttachmentsAsync(item)));
                    }
                    view.Button([Button.GhostErrorSm], text: "Delete", disabled: _spBusy.Value,
                        props: new Dictionary<string, object> { ["aria-label"] = $"Delete item {item.Id}", ["title"] = "Moves it to the site's recycle bin" },
                        onClick: async () => await SpRunAsync("delete the item", () => DeleteSharePointListItemAsync(item)));
                });

                if (_spAttachmentsItem.Value?.Id == item.Id)
                {
                    view.Column(["pl-6 py-2"], content: view =>
                    {
                        foreach (var attachment in _spAttachments)
                        {
                            view.Row([Layout.Row.Sm, "items-center"], key: attachment.FileName, content: view =>
                            {
                                view.Text([Text.Caption, "flex-1"], attachment.FileName, props: TestId("sp-item-attachment"));
                                view.Button([Button.GhostSm], text: "Read", disabled: _spBusy.Value,
                                    onClick: async () => await SpRunAsync($"read {attachment.FileName}", () => ReadSharePointAttachmentAsync(item, attachment)));
                                view.Button([Button.GhostErrorSm], text: "Delete", disabled: _spBusy.Value,
                                    onClick: async () => await SpRunAsync($"delete {attachment.FileName}", () => DeleteSharePointAttachmentAsync(item, attachment)));
                            });
                        }

                        view.Button([Button.OutlineSm, "self-start mt-1"], text: "Attach a note", disabled: _spBusy.Value, props: TestId("sp-item-attach"),
                            onClick: async () => await SpRunAsync("attach a note", () => AttachSharePointNoteAsync(item)));
                    });
                }

                if (_spVersionsItem.Value?.Id == item.Id)
                {
                    view.Column(["pl-6 py-2"], content: view =>
                    {
                        foreach (var version in _spItemVersions)
                        {
                            view.Row([Layout.Row.Sm, "items-center"], key: version.Id, content: view =>
                            {
                                view.Text([Text.Caption, "flex-1"], $"{version.Id} · {version.LastModified:yyyy-MM-dd HH:mm} · {version.LastModifiedBy}", props: TestId("sp-item-version"));
                                view.Button([Button.GhostSm], text: "Restore", disabled: _spBusy.Value,
                                    onClick: async () => await SpRunAsync($"restore version {version.Id}", () => RestoreSharePointItemVersionAsync(item, version)));
                            });
                        }
                    });
                }
            }
        });
    }

    private void RenderSharePointListColumns(UIView view)
    {
        if (!_spListColumnsShown.Value)
        {
            return;
        }

        view.Column(["mb-4"], content: view =>
        {
            view.Text([Text.Label, "mb-1"], "Columns");

            foreach (var column in _spListColumns.Where(c => !c.Hidden && !c.ReadOnly))
            {
                view.Row([Layout.Row.Sm, "items-center"], key: column.Name, content: view =>
                {
                    var detail = column.Choices is { } choices ? $" [{string.Join(", ", choices)}]" : column.LookupListId is { } lookup ? $" → {lookup}" : "";
                    view.Text([Text.Caption, "flex-1"], $"{column.DisplayName} ({column.Name}) · {column.Type}{detail}{(column.Indexed ? " · indexed" : "")}{(column.Required ? " · required" : "")}",
                        props: TestId("sp-list-column"));

                    if (!column.Indexed && column.Id is not null)
                    {
                        view.Button([Button.GhostSm], text: "Index", disabled: _spBusy.Value,
                            onClick: async () => await SpRunAsync($"index {column.Name}", () => IndexSharePointColumnAsync(column)));
                    }
                });
            }
        });
    }

    private void RenderSharePointListContentTypes(UIView view)
    {
        if (!_spListContentTypesShown.Value)
        {
            return;
        }

        view.Column(["mb-4"], content: view =>
        {
            view.Text([Text.Label, "mb-1"], "Content types");

            foreach (var type in _spListContentTypes)
            {
                view.Text([Text.Caption], $"{type.Name} · {type.Id}{(type.Hidden ? " · hidden" : "")}", key: type.Id, props: TestId("sp-list-content-type"));
            }

            view.Row([Layout.Row.Sm, "items-end flex-wrap mt-2"], content: view =>
            {
                view.TextField([Input.Default, "flex-1 min-w-48"], value: _spContentTypeToAdd.Value, placeholder: "Site content type id, e.g. 0x0101 for Document",
                    props: TestId("sp-content-type-input"), onValueChange: async v => _spContentTypeToAdd.Value = v ?? "");
                view.Button([Button.OutlineSm], text: "Add to list", disabled: _spBusy.Value || _spContentTypeToAdd.Value.Trim().Length == 0, props: TestId("sp-content-type-add"),
                    onClick: async () => await SpRunAsync("add the content type", AddSharePointContentTypeAsync));
            });
        });
    }

    private void RenderSharePointNewItem(UIView view)
    {
        var status = _spListColumns.FirstOrDefault(c => c is { Name: "Status", Type: "choice" });
        var owner = _spListColumns.FirstOrDefault(c => c is { Name: "Owner", Type: "personOrGroup" });

        view.Row([Layout.Row.Md, "items-end flex-wrap mb-3"], content: view =>
        {
            view.TextField([Input.Default, "flex-1 min-w-48"],
                value: _spNewItemTitle.Value,
                placeholder: "Title of a new item",
                props: TestId("sp-item-input"),
                onValueChange: async v => _spNewItemTitle.Value = v ?? "");

            if (status?.Choices is { Count: > 0 } choices)
            {
                view.Select(value: _spNewItemStatus.Value.Length > 0 ? _spNewItemStatus.Value : choices[0],
                    options: choices.Select(c => new SelectOption(c, c)).ToList(), ariaLabel: "Status",
                    props: TestId("sp-item-status"), onValueChange: async v => _spNewItemStatus.Value = v ?? "");
            }

            if (owner is not null)
            {
                view.TextField([Input.Default, "w-56"], value: _spNewItemOwner.Value, placeholder: "Owner's email (optional)",
                    props: TestId("sp-item-owner"), onValueChange: async v => _spNewItemOwner.Value = v ?? "");
            }

            view.Button([Button.OutlineMd], text: "Add item", disabled: _spBusy.Value || _spNewItemTitle.Value.Trim().Length == 0, props: TestId("sp-item-add"),
                onClick: async () => await SpRunAsync("add the item", AddSharePointListItemAsync));
        });
    }

    private async Task<string> OpenSharePointListAsync(SharePointList list)
    {
        _spList.Value = list;
        _spListColumnsShown.Value = false;
        _spListContentTypesShown.Value = false;
        _spVersionsItem.Value = null;
        _spListChangesSummary.Value = null;

        // The new-item form offers a choice and a person input when the list has those columns.
        var (sharePoint, _) = SpClients();
        _spListColumns.ReplaceAll(await sharePoint.ListColumnsAsync(_spSite.Value!.Id, list.Id));

        return await LoadSharePointListAsync();
    }

    private async Task<string> LoadSharePointListAsync()
    {
        var list = _spList.Value!;
        var (sharePoint, _) = SpClients();
        IReadOnlyList<ListItem> items;
        var capped = "";

        try
        {
            items = await sharePoint.ListItemsAsync(_spSite.Value!.Id, list.Id, maxPages: SpListPages);
        }
        catch (ConnectorPageCapException<ListItem> cap)
        {
            items = cap.Items;
            capped = $" (the first {cap.Items.Count}; the list holds more)";
        }

        _spListItems.ReplaceAll(items);
        return $"PASS read {items.Count} items from {list.DisplayName}{capped}";
    }

    private async Task<string> CreateSharePointTestListAsync()
    {
        var (sharePoint, _) = SpClients();
        var siteId = _spSite.Value!.Id;
        var name = $"{SpTestListPrefix} {DateTime.UtcNow:yyyy-MM-dd HH.mm.ss}";

        var list = await sharePoint.CreateListAsync(siteId, name, columns:
        [
            new ColumnSpec("Status", ColumnKind.Choice, Choices: ["Open", "In progress", "Done"], Indexed: true),
            new ColumnSpec("Due", ColumnKind.DateOnly, DisplayName: "Due date"),
            new ColumnSpec("Owner", ColumnKind.PersonOrGroup),
            new ColumnSpec("Score", ColumnKind.Number),
        ]);

        _spLists.ReplaceAll(await sharePoint.ListListsAsync(siteId));
        await OpenSharePointListAsync(list);
        return $"PASS created {name} with Status (indexed choice), Due date, Owner and Score columns";
    }

    private async Task<string> DeleteSharePointListAsync()
    {
        var (sharePoint, _) = SpClients();
        var list = _spList.Value!;
        await sharePoint.DeleteListAsync(_spSite.Value!.Id, list.Id);

        _spLists.RemoveAll(l => l.Id == list.Id);
        _spList.Value = null;
        _spListItems.Clear();
        return $"PASS moved {list.DisplayName} to the recycle bin";
    }

    private async Task<string> LoadSharePointListColumnsAsync()
    {
        var (sharePoint, _) = SpClients();
        _spListColumns.ReplaceAll(await sharePoint.ListColumnsAsync(_spSite.Value!.Id, _spList.Value!.Id));
        _spListColumnsShown.Value = true;
        return $"PASS {_spList.Value!.DisplayName} has {_spListColumns.Count} columns, {_spListColumns.Count(c => c.Indexed)} indexed";
    }

    private async Task<string> IndexSharePointColumnAsync(ListColumn column)
    {
        var (sharePoint, _) = SpClients();
        var updated = await sharePoint.UpdateColumnAsync(_spSite.Value!.Id, _spList.Value!.Id, column.Id!, new Dictionary<string, object?> { ["indexed"] = true });
        _spListColumns.ReplaceAll(_spListColumns.Select(c => c.Name == column.Name ? updated : c).ToList());
        return updated.Indexed ? $"PASS indexed {column.Name}" : $"FAIL index {column.Name}: Graph answered without the column indexed";
    }

    private async Task<string> LoadSharePointListContentTypesAsync()
    {
        var (sharePoint, _) = SpClients();
        _spListContentTypes.ReplaceAll(await sharePoint.ListContentTypesAsync(_spSite.Value!.Id, _spList.Value!.Id));
        _spListContentTypesShown.Value = true;
        return $"PASS {_spList.Value!.DisplayName} has {_spListContentTypes.Count} content types";
    }

    private async Task<string> AddSharePointContentTypeAsync()
    {
        var (sharePoint, _) = SpClients();
        var added = await sharePoint.AddContentTypeToListAsync(_spSite.Value!.Id, _spList.Value!.Id, _spContentTypeToAdd.Value.Trim());
        _spContentTypeToAdd.Value = "";
        await LoadSharePointListContentTypesAsync();
        return $"PASS added {added.Name} to {_spList.Value!.DisplayName} as {added.Id}";
    }

    private async Task<string> AddSharePointListItemAsync()
    {
        var title = _spNewItemTitle.Value.Trim();
        var (sharePoint, _) = SpClients();
        var siteId = _spSite.Value!.Id;
        var fields = new Dictionary<string, object?> { ["Title"] = title };
        var notes = new List<string>();

        if (_spListColumns.FirstOrDefault(c => c is { Name: "Status", Type: "choice" })?.Choices is { Count: > 0 } choices)
        {
            fields["Status"] = _spNewItemStatus.Value.Length > 0 ? _spNewItemStatus.Value : choices[0];
            notes.Add($"Status {fields["Status"]}");
        }

        if (_spNewItemOwner.Value.Trim() is { Length: > 0 } email && _spListColumns.Any(c => c is { Name: "Owner", Type: "personOrGroup" }))
        {
            var userId = await sharePoint.EnsureSiteUserAsync(siteId, email);
            fields.SetLookup("Owner", userId);
            notes.Add($"Owner site user {userId}");
        }

        var item = await sharePoint.CreateItemAsync(siteId, _spList.Value!.Id, fields);

        _spNewItemTitle.Value = "";
        await LoadSharePointListAsync();
        return $"PASS added item {item.Id} “{title}”{(notes.Count > 0 ? $" with {string.Join(", ", notes)}" : "")}";
    }

    private async Task<string> RetitleSharePointListItemAsync(ListItem item)
    {
        var (sharePoint, _) = SpClients();
        var title = _spNewItemTitle.Value.Trim();

        try
        {
            await sharePoint.UpdateItemFieldsAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, new Dictionary<string, object?> { ["Title"] = title }, item.ETag);
        }
        catch (ConnectorException ex) when (ex.StatusCode == 412)
        {
            await LoadSharePointListAsync();
            return $"PASS the item changed since it was read, so the update was refused (412); reloaded it — retitle again";
        }

        _spNewItemTitle.Value = "";
        await LoadSharePointListAsync();
        return $"PASS retitled item {item.Id} to “{title}”";
    }

    private async Task<string> LoadSharePointItemVersionsAsync(ListItem item)
    {
        var (sharePoint, _) = SpClients();
        _spItemVersions.ReplaceAll(await sharePoint.ListItemVersionsAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id));
        _spVersionsItem.Value = item;
        return $"PASS item {item.Id} has {_spItemVersions.Count} versions";
    }

    private async Task<string> RestoreSharePointItemVersionAsync(ListItem item, ListItemVersion version)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.RestoreItemVersionAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, version.Id);
        await LoadSharePointListAsync();
        await LoadSharePointItemVersionsAsync(item);
        return $"PASS restored item {item.Id} to version {version.Id}";
    }

    private async Task<string> DeleteSharePointListItemAsync(ListItem item)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.DeleteItemAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, item.ETag);

        _spListItems.RemoveAll(i => i.Id == item.Id);
        return $"PASS moved item {item.Id} to the recycle bin";
    }

    private async Task<string> ReadSharePointListChangesAsync(bool fromNow)
    {
        var list = _spList.Value!;
        var (sharePoint, _) = SpClients();
        var stored = fromNow ? null : _spListDeltaLinks.TryGetValue(list.Id, out var link) ? link : null;
        ListItemDelta delta;

        try
        {
            delta = await sharePoint.ItemsDeltaAsync(_spSite.Value!.Id, list.Id, stored, fromNow, maxPages: 20);
        }
        catch (ConnectorException ex) when (ex.StatusCode == 410)
        {
            stored = null;
            delta = await sharePoint.ItemsDeltaAsync(_spSite.Value!.Id, list.Id, maxPages: 20);
        }

        _spListDeltaLinks[list.Id] = delta.DeltaLink;
        var removed = delta.Items.Count(i => i.Deleted);

        _spListChangesSummary.Value = fromNow
            ? "Watching from now on: change an item in SharePoint, then Check for changes"
            : stored is null
                ? $"Read the whole list: {delta.Items.Count} items. Change something and check again."
                : $"{delta.Items.Count} changed since the last check ({removed} removed): {string.Join(", ", delta.Items.Take(10).Select(i => i.Deleted ? $"#{i.Id} removed" : SpField(i, "Title") ?? $"#{i.Id}"))}";

        return $"PASS {(fromNow ? "started list changes from now" : "read the list changes")}: {delta.Items.Count} items";
    }

    private async Task<string> LoadSharePointAttachmentsAsync(ListItem item)
    {
        var (sharePoint, _) = SpClients();
        _spAttachments.ReplaceAll(await sharePoint.ListAttachmentsAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id));
        _spAttachmentsItem.Value = item;
        return $"PASS item {item.Id} has {_spAttachments.Count} attachments";
    }

    private async Task<string> AttachSharePointNoteAsync(ListItem item)
    {
        var (sharePoint, _) = SpClients();
        var name = $"note {DateTime.UtcNow:HH.mm.ss}.txt";
        var added = await sharePoint.AddAttachmentAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, name, System.Text.Encoding.UTF8.GetBytes($"Attached from the Validation app at {DateTime.UtcNow:O}"));
        await LoadSharePointAttachmentsAsync(item);
        return $"PASS attached {added.FileName} to item {item.Id}";
    }

    private async Task<string> ReadSharePointAttachmentAsync(ListItem item, ListItemAttachment attachment)
    {
        var (sharePoint, _) = SpClients();
        await using var content = await sharePoint.DownloadAttachmentAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, attachment.FileName);
        var text = await new StreamReader(content).ReadToEndAsync();
        return $"PASS {attachment.FileName}: {(text.Length > 120 ? text[..120] + "…" : text)}";
    }

    private async Task<string> DeleteSharePointAttachmentAsync(ListItem item, ListItemAttachment attachment)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.DeleteAttachmentAsync(_spSite.Value!.Id, _spList.Value!.Id, item.Id, attachment.FileName);
        _spAttachments.RemoveAll(a => a.FileName == attachment.FileName);
        return $"PASS deleted {attachment.FileName}";
    }

    private static string SpFieldSummary(ListItem item)
    {
        return string.Join(" · ", item.Fields
            .Where(f => f.Key is not ("Title" or "ContentType" or "Attachments" or "Edit" or "LinkTitle" or "LinkTitleNoMenu" or "ItemChildCount" or "FolderChildCount" or "AppAuthorLookupId" or "AppEditorLookupId")
                && !f.Key.StartsWith('_') && f.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            .Take(5)
            .Select(f => $"{f.Key}: {f.Value}"));
    }
}
