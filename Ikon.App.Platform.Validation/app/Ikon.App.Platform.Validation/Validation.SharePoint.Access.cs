using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    private readonly ReactiveList<SharePointPrincipal> _spSiteGroups = new();
    private readonly Reactive<SharePointPrincipal?> _spOpenGroup = new(null);
    private readonly ReactiveList<SharePointPrincipal> _spGroupMembers = new();
    private readonly Reactive<SharePointRoleAssignments?> _spRoleAssignments = new(null);
    private readonly Reactive<string?> _spRoleTarget = new(null);
    private readonly ReactiveList<RecycleBinItem> _spRecycleBin = new();
    private readonly Reactive<bool> _spRecycleBinShown = new(false);

    // SharePoint's own REST API refuses app-only tokens obtained with a secret.
    private bool SpRestAvailable => SpHasCertificate() || _spUseDelegated.Value;

    private void RenderSharePointAccessCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Access");

            if (!SpRestAvailable)
            {
                view.Text([Text.Body, "text-warning-primary"], $"SKIP: site groups and role assignments use SharePoint's own REST API, which needs a certificate ({SpCertificateSecret}) or a delegated sign-in",
                    props: TestId("sp-access-skip"));
                return;
            }

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Site groups", disabled: _spBusy.Value, props: TestId("sp-site-groups"),
                    onClick: async () => await SpRunAsync("list site groups", LoadSharePointSiteGroupsAsync));
                view.Button([Button.OutlineSm], text: "Site role assignments", disabled: _spBusy.Value, props: TestId("sp-site-roles"),
                    onClick: async () => await SpRunAsync("list site role assignments", () => LoadSharePointRoleAssignmentsAsync(SharePointTarget.Site(_spSite.Value!.Id), _spSite.Value!.DisplayName)));

                if (_spList.Value is { } list)
                {
                    view.Button([Button.OutlineSm], text: $"Roles on {list.DisplayName}", disabled: _spBusy.Value, props: TestId("sp-list-roles"),
                        onClick: async () => await SpRunAsync("list the list's role assignments", () => LoadSharePointRoleAssignmentsAsync(SharePointTarget.List(_spSite.Value!.Id, list.Id), list.DisplayName)));
                }
            });

            foreach (var group in _spSiteGroups)
            {
                view.Row([Layout.Row.Sm, "items-center"], key: group.Id.ToString(), content: view =>
                {
                    view.Text([Text.Caption, "flex-1"], $"{group.Title} (#{group.Id})", props: TestId("sp-site-group"));
                    view.Button([Button.GhostSm], text: "Members", disabled: _spBusy.Value,
                        onClick: async () => await SpRunAsync($"list the members of {group.Title}", () => LoadSharePointGroupMembersAsync(group)));
                });

                if (_spOpenGroup.Value?.Id == group.Id)
                {
                    view.Column(["pl-6 mb-2"], content: view =>
                    {
                        foreach (var member in _spGroupMembers)
                        {
                            view.Text([Text.Caption], $"{member.Title} · {member.Kind}{(member.EntraId is { } entra ? $" · Entra {entra}" : "")}{(member.UserPrincipalName is { } upn ? $" · {upn}" : "")}",
                                key: member.Id.ToString(), props: TestId("sp-group-member"));
                        }
                    });
                }
            }

            if (_spRoleAssignments.Value is { } assignments)
            {
                view.Text([Text.Label, "mt-3 mb-1"], $"Role assignments on {_spRoleTarget.Value} — {(assignments.HasUniqueRoleAssignments ? "its own" : "inherited")}");

                foreach (var assignment in assignments.Assignments)
                {
                    view.Text([Text.Caption], $"{assignment.Principal.Title} ({assignment.Principal.Kind}) · {string.Join(", ", assignment.Roles.Select(r => r.Name))}",
                        key: assignment.Principal.Id.ToString(), props: TestId("sp-role-assignment"));
                }
            }
        });
    }

    private void RenderSharePointRecycleBinCard(UIView view)
    {
        if (!SpRestAvailable)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Recycle bin");
            view.Button([Button.OutlineSm, "mb-3"], text: "Show recently deleted", disabled: _spBusy.Value, props: TestId("sp-recycle-bin"),
                onClick: async () => await SpRunAsync("list the recycle bin", LoadSharePointRecycleBinAsync));

            if (!_spRecycleBinShown.Value)
            {
                return;
            }

            if (_spRecycleBin.Count == 0)
            {
                view.Text([Text.Caption], "The site's recycle bin is empty");
            }

            foreach (var item in _spRecycleBin.OrderByDescending(i => i.DeletedAt).Take(25))
            {
                view.Row([Layout.Row.Sm, "items-center"], key: item.Id, content: view =>
                {
                    view.Text([Text.Caption, "flex-1 truncate"], $"{item.LeafName} · {item.DirName} · deleted {item.DeletedAt:yyyy-MM-dd HH:mm} by {item.DeletedBy}", props: TestId("sp-recycle-item"));
                    view.Button([Button.GhostSm], text: "Restore", disabled: _spBusy.Value,
                        onClick: async () => await SpRunAsync($"restore {item.LeafName}", () => RestoreSharePointRecycleBinItemAsync(item)));
                });
            }
        });
    }

    private async Task<string> LoadSharePointSiteGroupsAsync()
    {
        var (sharePoint, _) = SpClients();
        _spSiteGroups.ReplaceAll(await sharePoint.ListSiteGroupsAsync(_spSite.Value!.Id));
        _spOpenGroup.Value = null;
        return $"PASS {_spSite.Value!.DisplayName} has {_spSiteGroups.Count} site groups";
    }

    private async Task<string> LoadSharePointGroupMembersAsync(SharePointPrincipal group)
    {
        var (sharePoint, _) = SpClients();
        _spGroupMembers.ReplaceAll(await sharePoint.ListSiteGroupMembersAsync(_spSite.Value!.Id, group.Id));
        _spOpenGroup.Value = group;
        var unresolved = _spGroupMembers.Count(m => m.Kind == SharePointPrincipalKind.Unknown);
        return $"PASS {group.Title} has {_spGroupMembers.Count} members{(unresolved > 0 ? $", {unresolved} of a kind the connector does not know" : "")}";
    }

    private async Task<string> LoadSharePointRoleAssignmentsAsync(SharePointTarget target, string name)
    {
        var (sharePoint, _) = SpClients();
        _spRoleAssignments.Value = await sharePoint.ListRoleAssignmentsAsync(target);
        _spRoleTarget.Value = name;
        return $"PASS {name} has {_spRoleAssignments.Value.Assignments.Count} role assignments, {(_spRoleAssignments.Value.HasUniqueRoleAssignments ? "its own" : "inherited")}";
    }

    private async Task<string> LoadSharePointRecycleBinAsync()
    {
        var (sharePoint, _) = SpClients();
        _spRecycleBin.ReplaceAll(await sharePoint.ListRecycleBinAsync(_spSite.Value!.Id));
        _spRecycleBinShown.Value = true;
        return $"PASS the recycle bin holds {_spRecycleBin.Count} items";
    }

    private async Task<string> RestoreSharePointRecycleBinItemAsync(RecycleBinItem item)
    {
        var (sharePoint, _) = SpClients();
        await sharePoint.RestoreFromRecycleBinAsync(_spSite.Value!.Id, item.Id);
        _spRecycleBin.RemoveAll(i => i.Id == item.Id);

        if (_spLibrary.Value != null)
        {
            await LoadSharePointFolderAsync();
        }

        return $"PASS restored {item.LeafName} to {item.DirName}";
    }

    private async Task<string> ResolveSharePointFileGrantsAsync(DriveItem item)
    {
        var (sharePoint, oneDrive) = SpClients();
        var grants = await oneDrive.ListPermissionsAsync(item.DriveId, item.Id);
        var resolved = await sharePoint.ResolveGrantsAsync(_spSite.Value!.Id, grants);
        _spFileGrants.ReplaceAll(resolved);
        _spPanelView.Value = "permissions";

        static int Unresolved(IEnumerable<DriveItemGrant> list) => list.Count(g => g.Kind is GrantPrincipalKind.SiteGroup or GrantPrincipalKind.SiteUser or GrantPrincipalKind.Unknown);
        return $"PASS resolved {grants.Count} grants into {resolved.Count}: {Unresolved(grants)} unresolved before, {Unresolved(resolved)} after";
    }

    private async Task<string> ToggleSharePointItemInheritanceAsync(DriveItem item)
    {
        if (item.SharePointIds is not { ListId: { } listId, ListItemId: { } listItemId })
        {
            return "FAIL change inheritance: the file came back without its list item ids";
        }

        var (sharePoint, _) = SpClients();
        var target = SharePointTarget.Item(_spSite.Value!.Id, listId, listItemId);
        var before = await sharePoint.ListRoleAssignmentsAsync(target);

        if (before.HasUniqueRoleAssignments)
        {
            await sharePoint.ResetInheritanceAsync(target);
        }
        else
        {
            await sharePoint.BreakInheritanceAsync(target);
        }

        _spRoleAssignments.Value = await sharePoint.ListRoleAssignmentsAsync(target);
        _spRoleTarget.Value = item.Name;
        return $"PASS {item.Name} {(before.HasUniqueRoleAssignments ? "inherits its library's permissions again" : "now has permissions of its own, copied from its library")}";
    }

    private async Task<string> PermanentlyDeleteSharePointItemAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.PermanentDeleteAsync(item.DriveId, item.Id);
        _spItems.RemoveAll(i => i.Id == item.Id);
        _spActiveItem.Value = null;
        return $"PASS deleted {item.Name} for good";
    }
}
