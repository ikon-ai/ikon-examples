using System.Text.Json;
using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    private readonly Reactive<DriveItem?> _spActiveItem = new(null);
    private readonly Reactive<string> _spPanelView = new("");
    private readonly ReactiveList<DriveItemVersion> _spFileVersions = new();
    private readonly ReactiveList<DriveItemGrant> _spFileGrants = new();
    private readonly Reactive<IReadOnlyDictionary<string, JsonElement>?> _spFileFields = new(null);
    private readonly Reactive<string> _spFieldName = new("");
    private readonly Reactive<string> _spFieldValue = new("");
    private readonly Reactive<SharingLink?> _spCreatedLink = new(null);
    private readonly Reactive<byte[]?> _spThumbnail = new(null);
    private readonly Reactive<string> _spCopyTarget = new("");
    private readonly Reactive<string> _spSharingUrl = new("");
    private readonly Reactive<bool> _spPermanentDeleteOpen = new(false);

    // Only what this tab created is offered for removal, so a test against a shared site cannot
    // withdraw someone's real access.
    private readonly HashSet<string> _spLinksCreatedHere = [];

    private void SpToggleActiveItem(DriveItem item)
    {
        _spActiveItem.Value = _spActiveItem.Value?.Id == item.Id ? null : item;
        _spPanelView.Value = "";
        _spThumbnail.Value = null;
    }

    private void RenderSharePointItemPanel(UIView view, DriveItem item)
    {
        view.Column(["pl-6 py-3 border-b border-secondary bg-secondary/40 rounded-md"], content: view =>
        {
            view.Row([Layout.Row.Sm, "flex-wrap mb-2"], content: view =>
            {
                if (!item.IsFolder)
                {
                    view.Button([Button.OutlineSm], text: "Versions", disabled: _spBusy.Value, props: TestId("sp-file-versions"),
                        onClick: async () => await SpRunAsync("list versions", () => LoadSharePointFileVersionsAsync(item)));
                    view.Button([Button.OutlineSm], text: "Check out", disabled: _spBusy.Value, props: TestId("sp-file-checkout"),
                        onClick: async () => await SpRunAsync("check out", () => CheckOutSharePointFileAsync(item)));
                    view.Button([Button.OutlineSm], text: "Check in", disabled: _spBusy.Value, props: TestId("sp-file-checkin"),
                        onClick: async () => await SpRunAsync("check in", () => CheckInSharePointFileAsync(item)));
                    view.Button([Button.GhostSm], text: "Discard check-out", disabled: _spBusy.Value,
                        onClick: async () => await SpRunAsync("discard the check-out", () => DiscardSharePointCheckOutAsync(item)));
                    view.Button([Button.OutlineSm], text: "Thumbnail", disabled: _spBusy.Value, props: TestId("sp-file-thumbnail"),
                        onClick: async () => await SpRunAsync("get the thumbnail", () => LoadSharePointThumbnailAsync(item)));
                }

                view.Button([Button.OutlineSm], text: "Properties", disabled: _spBusy.Value, props: TestId("sp-file-properties"),
                    onClick: async () => await SpRunAsync("read the properties", () => LoadSharePointFileFieldsAsync(item)));
                view.Button([Button.OutlineSm], text: "Share", disabled: _spBusy.Value, props: TestId("sp-file-share"),
                    onClick: async () => await SpRunAsync("create a sharing link", () => CreateSharePointLinkAsync(item)));
                view.Button([Button.OutlineSm], text: "Permissions", disabled: _spBusy.Value, props: TestId("sp-file-permissions"),
                    onClick: async () => await SpRunAsync("list permissions", () => LoadSharePointFileGrantsAsync(item)));

                if (SpRestAvailable)
                {
                    view.Button([Button.OutlineSm], text: "Resolved permissions", disabled: _spBusy.Value, props: TestId("sp-file-resolve"),
                        onClick: async () => await SpRunAsync("resolve permissions", () => ResolveSharePointFileGrantsAsync(item)));
                    view.Button([Button.GhostSm], text: "Toggle own permissions", disabled: _spBusy.Value, props: new Dictionary<string, object>
                    {
                        ["data-testid"] = "sp-file-inheritance",
                        ["title"] = "Breaks the file's permission inheritance (copying its library's), or restores it when broken",
                    }, onClick: async () => await SpRunAsync("change permission inheritance", () => ToggleSharePointItemInheritanceAsync(item)));
                }

                view.Button([Button.GhostErrorSm], text: "Delete permanently", disabled: _spBusy.Value, props: TestId("sp-file-permanent-delete"),
                    onClick: async () => _spPermanentDeleteOpen.Value = true);
            });

            view.AlertDialog(
                open: _spPermanentDeleteOpen.Value,
                onOpenChange: async open => _spPermanentDeleteOpen.Value = open,
                title: $"Delete {item.Name} for good?",
                description: "This skips the recycle bin and cannot be undone.",
                cancelLabel: "Keep it",
                actionLabel: "Delete permanently",
                onAction: async () =>
                {
                    _spPermanentDeleteOpen.Value = false;
                    await SpRunAsync($"permanently delete {item.Name}", () => PermanentlyDeleteSharePointItemAsync(item));
                });

            if (_spRoleTarget.Value == item.Name && _spRoleAssignments.Value is { } roles)
            {
                view.Text([Text.Caption, "mb-2"], $"Permissions: {(roles.HasUniqueRoleAssignments ? "its own" : "inherited")} · {string.Join("; ", roles.Assignments.Select(a => $"{a.Principal.Title}: {string.Join(", ", a.Roles.Select(r => r.Name))}"))}",
                    props: TestId("sp-file-roles"));
            }

            view.Row([Layout.Row.Sm, "items-center flex-wrap mb-2"], content: view =>
            {
                var options = _spLibraries.Select(l => new SelectOption(l.Id, l.Name)).ToList();
                view.Select(value: _spCopyTarget.Value.Length > 0 ? _spCopyTarget.Value : _spLibrary.Value?.Id ?? "", options: options, ariaLabel: "Copy to library",
                    props: TestId("sp-file-copy-target"), onValueChange: async v => _spCopyTarget.Value = v ?? "");
                view.Button([Button.OutlineSm], text: "Copy", disabled: _spBusy.Value, props: TestId("sp-file-copy"),
                    onClick: async () => await SpRunAsync($"copy {item.Name}", () => CopySharePointItemAsync(item)));
                view.Text([Text.Caption], "Same library: a renamed copy in this folder; another library: a copy at its root");
            });

            switch (_spPanelView.Value)
            {
                case "versions":
                    foreach (var version in _spFileVersions)
                    {
                        view.Row([Layout.Row.Sm, "items-center"], key: version.Id, content: view =>
                        {
                            view.Text([Text.Caption, "flex-1"], $"{version.Id} · {version.LastModified:yyyy-MM-dd HH:mm} · {version.LastModifiedBy} · {SpSize(version.Size)}", props: TestId("sp-file-version"));
                            view.Button([Button.GhostSm], text: "Restore", disabled: _spBusy.Value,
                                onClick: async () => await SpRunAsync($"restore version {version.Id}", () => RestoreSharePointFileVersionAsync(item, version)));
                        });
                    }

                    break;

                case "properties" when _spFileFields.Value is { } fields:
                    foreach (var (name, value) in fields.Where(f => !f.Key.StartsWith('_') && f.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False).Take(20))
                    {
                        view.Text([Text.Caption], $"{name}: {value}", key: name, props: TestId("sp-file-field"));
                    }

                    view.Row([Layout.Row.Sm, "items-end flex-wrap mt-2"], content: view =>
                    {
                        view.TextField([Input.Default, "w-48"], value: _spFieldName.Value, placeholder: "Column internal name", props: TestId("sp-field-name"),
                            onValueChange: async v => _spFieldName.Value = v ?? "");
                        view.TextField([Input.Default, "w-48"], value: _spFieldValue.Value, placeholder: "New value", props: TestId("sp-field-value"),
                            onValueChange: async v => _spFieldValue.Value = v ?? "");
                        view.Button([Button.OutlineSm], text: "Save", disabled: _spBusy.Value || _spFieldName.Value.Trim().Length == 0, props: TestId("sp-field-save"),
                            onClick: async () => await SpRunAsync("update the property", () => UpdateSharePointFileFieldAsync(item)));
                    });

                    break;

                case "link" when _spCreatedLink.Value is { } link:
                    view.Row([Layout.Row.Sm, "items-center flex-wrap"], content: view =>
                    {
                        view.Link([Text.Caption, "break-all"], href: link.WebUrl, text: link.WebUrl, target: "_blank", props: TestId("sp-file-link"));
                        view.Button([Button.GhostErrorSm], text: "Remove link", disabled: _spBusy.Value, props: TestId("sp-file-link-remove"),
                            onClick: async () => await SpRunAsync("remove the link", () => RemoveSharePointPermissionAsync(item, link.PermissionId)));
                    });

                    break;

                case "permissions":
                    foreach (var grant in _spFileGrants)
                    {
                        view.Row([Layout.Row.Sm, "items-center"], key: grant.PermissionId + grant.PrincipalId, content: view =>
                        {
                            var who = grant.Kind == GrantPrincipalKind.Link ? $"{grant.LinkScope} link" : grant.DisplayName ?? grant.Email ?? grant.LoginName ?? grant.PrincipalId ?? "?";
                            view.Text([Text.Caption, "flex-1"], $"{who} ({grant.Kind}) · {string.Join(", ", grant.Roles)}{(grant.Inherited ? " · inherited" : "")}", props: TestId("sp-file-grant"));

                            if (_spLinksCreatedHere.Contains(grant.PermissionId))
                            {
                                view.Button([Button.GhostErrorSm], text: "Remove", disabled: _spBusy.Value,
                                    onClick: async () => await SpRunAsync("remove the permission", () => RemoveSharePointPermissionAsync(item, grant.PermissionId)));
                            }
                        });
                    }

                    break;

                case "thumbnail" when _spThumbnail.Value is { } image:
                    view.Image(["max-h-48 rounded-md"], data: image, mimeType: "image/jpeg", alt: $"Thumbnail of {item.Name}");
                    break;
            }
        });
    }

    private void RenderSharePointOpenByLink(UIView view)
    {
        view.Row([Layout.Row.Md, "items-end flex-wrap mt-4"], content: view =>
        {
            view.TextField([Input.Default, "flex-1 min-w-64"], value: _spSharingUrl.Value, placeholder: "Paste a sharing link or a file's SharePoint URL",
                props: TestId("sp-open-link-input"), onValueChange: async v => _spSharingUrl.Value = v ?? "");
            view.Button([Button.OutlineMd], text: "Open by link", disabled: _spBusy.Value || _spSharingUrl.Value.Trim().Length == 0, props: TestId("sp-open-link"),
                onClick: async () => await SpRunAsync("open by link", OpenSharePointByLinkAsync));
        });
    }

    private async Task<string> LoadSharePointFileVersionsAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        _spFileVersions.ReplaceAll(await oneDrive.ListVersionsAsync(item.DriveId, item.Id));
        _spPanelView.Value = "versions";
        return $"PASS {item.Name} has {_spFileVersions.Count} versions";
    }

    private async Task<string> RestoreSharePointFileVersionAsync(DriveItem item, DriveItemVersion version)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.RestoreVersionAsync(item.DriveId, item.Id, version.Id);
        await LoadSharePointFileVersionsAsync(item);
        return $"PASS restored {item.Name} to version {version.Id}; it now has {_spFileVersions.Count} versions";
    }

    private async Task<string> CheckOutSharePointFileAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.CheckOutAsync(item.DriveId, item.Id);
        var after = await oneDrive.GetItemAsync(item.DriveId, item.Id);
        return after.CheckedOut ? $"PASS checked out {item.Name}" : $"FAIL check out {item.Name}: Graph accepted it but the file does not read as checked out";
    }

    private async Task<string> CheckInSharePointFileAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.CheckInAsync(item.DriveId, item.Id, "Checked in from the Validation app");
        var after = await oneDrive.GetItemAsync(item.DriveId, item.Id);
        return after.CheckedOut ? $"FAIL check in {item.Name}: the file still reads as checked out" : $"PASS checked in {item.Name}";
    }

    private async Task<string> DiscardSharePointCheckOutAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.DiscardCheckOutAsync(item.DriveId, item.Id);
        return $"PASS discarded the check-out of {item.Name}";
    }

    private async Task<string> LoadSharePointThumbnailAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await using var image = await oneDrive.GetThumbnailAsync(item.DriveId, item.Id);
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer);
        _spThumbnail.Value = buffer.ToArray();
        _spPanelView.Value = "thumbnail";
        return $"PASS thumbnail of {item.Name}: {SpSize(buffer.Length)}";
    }

    private async Task<string> LoadSharePointFileFieldsAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        _spFileFields.Value = await oneDrive.GetItemFieldsAsync(item.DriveId, item.Id);
        _spPanelView.Value = "properties";
        return $"PASS {item.Name} has {_spFileFields.Value.Count} library fields";
    }

    private async Task<string> UpdateSharePointFileFieldAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        var name = _spFieldName.Value.Trim();
        var updated = await oneDrive.UpdateItemFieldsAsync(item.DriveId, item.Id, new Dictionary<string, object?> { [name] = _spFieldValue.Value });
        _spFileFields.Value = updated;
        return updated.TryGetValue(name, out var value) ? $"PASS {name} is now {value}" : $"FAIL update {name}: the answer has no such field";
    }

    private async Task<string> CreateSharePointLinkAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        var link = await oneDrive.CreateSharingLinkAsync(item.DriveId, item.Id, SharingLinkType.View, SharingLinkScope.Organization, DateTimeOffset.UtcNow.AddDays(1));
        _spLinksCreatedHere.Add(link.PermissionId);
        _spCreatedLink.Value = link;
        _spPanelView.Value = "link";
        return $"PASS created an organization view link to {item.Name}, expiring {link.Expires:yyyy-MM-dd HH:mm} UTC";
    }

    private async Task<string> RemoveSharePointPermissionAsync(DriveItem item, string permissionId)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.RemovePermissionAsync(item.DriveId, item.Id, permissionId);
        _spLinksCreatedHere.Remove(permissionId);
        _spCreatedLink.Value = null;
        await LoadSharePointFileGrantsAsync(item);
        return $"PASS removed the permission; {item.Name} now has {_spFileGrants.Count} grants";
    }

    private async Task<string> LoadSharePointFileGrantsAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        _spFileGrants.ReplaceAll(await oneDrive.ListPermissionsAsync(item.DriveId, item.Id));
        _spPanelView.Value = "permissions";
        return $"PASS {item.Name} has {_spFileGrants.Count} grants, {_spFileGrants.Count(g => g.Kind is GrantPrincipalKind.SiteGroup or GrantPrincipalKind.SiteUser or GrantPrincipalKind.Unknown)} unresolved";
    }

    private async Task<string> CopySharePointItemAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        var target = _spCopyTarget.Value.Length > 0 ? _spCopyTarget.Value : item.DriveId;
        var sameDrive = target == item.DriveId;
        var copy = await oneDrive.CopyAsync(item.DriveId, item.Id, target, sameDrive ? item.ParentId : null, conflict: UploadConflict.Rename);

        if (sameDrive)
        {
            await LoadSharePointFolderAsync();
        }

        return $"PASS copied {item.Name} to {copy.Name}{(sameDrive ? "" : $" in {_spLibraries.FirstOrDefault(l => l.Id == target)?.Name ?? target}")}";
    }

    private async Task<string> OpenSharePointByLinkAsync()
    {
        var (_, oneDrive) = SpClients();
        var item = await oneDrive.ResolveSharingUrlAsync(_spSharingUrl.Value.Trim());
        _spActiveItem.Value = item;
        _spPanelView.Value = "";

        if (!_spItems.Any(i => i.Id == item.Id))
        {
            _spItems.Insert(0, item);
        }

        return $"PASS the link opens {item.Name} in drive {item.DriveId}{(item.SharePointIds?.ListItemId is { } listItem ? $", list item {listItem}" : "")}";
    }
}
