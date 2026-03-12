public partial class Validation
{
    private void RenderOverlaysSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Dialog
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Dialog");
                view.Text([Text.Caption, "mb-4"], "A modal dialog that interrupts the user with important content");

                view.Dialog(
                    open: _dialogOpen.Value,
                    onOpenChange: async open => _dialogOpen.Value = open ?? false,
                    overlayStyle: [Dialog.Overlay],
                    contentStyle: [Dialog.Content],
                    trigger: view => view.Button([Button.OutlineMd], label: "Open Dialog"),
                    content: view =>
                    {
                        view.Box([Dialog.Header], content: view =>
                        {
                            view.Text([Dialog.Title], "Dialog Title");
                            view.Text([Dialog.Description], "This is the dialog description. It provides additional context.");
                        });
                        view.Text([Text.Body, "my-4"], "Dialog content goes here. You can put any content inside.");
                        view.Box([Dialog.Footer], content: view =>
                        {
                            view.Button([Button.OutlineMd], label: "Cancel", onClick: async () => _dialogOpen.Value = false);
                            view.Button([Button.PrimaryMd], label: "Confirm", onClick: async () => _dialogOpen.Value = false);
                        });
                    });
            });

            // Alert Dialog
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Alert Dialog");
                view.Text([Text.Caption, "mb-4"], "A modal dialog for important confirmations that requires user action");

                view.AlertDialog(
                    open: _alertDialogOpen.Value,
                    onOpenChange: async open => _alertDialogOpen.Value = open ?? false,
                    overlayStyle: [AlertDialog.Overlay],
                    contentStyle: [AlertDialog.Content],
                    trigger: view => view.Button([Button.DangerMd], label: "Delete Item"),
                    title: "Are you sure?",
                    titleStyle: [AlertDialog.Title],
                    description: "This action cannot be undone. This will permanently delete the item.",
                    descriptionStyle: [AlertDialog.Description],
                    footerStyle: [AlertDialog.Footer],
                    cancelLabel: "Cancel",
                    cancelStyle: [AlertDialog.Cancel],
                    actionLabel: "Delete",
                    actionStyle: [Button.DangerMd]);
            });

            // Popover
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Popover");
                view.Text([Text.Caption, "mb-4"], "A floating panel that appears on click");

                view.Popover(
                    open: _popoverOpen.Value,
                    onOpenChange: async open => _popoverOpen.Value = open ?? false,
                    contentStyle: [Popover.Content],
                    trigger: view => view.Button([Button.OutlineMd], label: "Open Popover"),
                    content: view =>
                    {
                        view.Column([Layout.Column.Sm], content: view =>
                        {
                            view.Text([Text.BodyStrong], "Popover Content");
                            view.Text([Text.Caption], "This is the popover content area.");
                            view.TextField([Input.Default], placeholder: "Enter something...");
                        });
                    });
            });

            // Tooltip
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tooltip");
                view.Text([Text.Caption, "mb-4"], "A popup that displays information on hover");

                view.Row([Layout.Row.Md], content: view =>
                {
                    view.Tooltip(
                        contentStyle: [Tooltip.Content],
                        trigger: view => view.Button([Button.OutlineMd], label: "Hover me"),
                        content: view => view.Text(text: "This is a tooltip!"));

                    view.Tooltip(
                        contentStyle: [Tooltip.Content],
                        trigger: view => view.Button([Button.GhostMd, Button.Size.Icon], content: v => v.Icon(name: "info-circled")),
                        content: view => view.Text(text: "More information here"));
                });
            });

            // Hover Card
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Hover Card");
                view.Text([Text.Caption, "mb-4"], "A card that appears on hover, useful for previews");

                view.HoverCard(
                    contentStyle: [HoverCard.Content],
                    trigger: view => view.Text([Text.Link], "@username"),
                    contentSlot: view =>
                    {
                        view.Column([Layout.Column.Sm], content: view =>
                        {
                            view.Row([Layout.Row.Sm], content: view =>
                            {
                                view.Box(["w-10 h-10 rounded-full bg-tertiary"]);
                                view.Column([Layout.Column.Xs], content: view =>
                                {
                                    view.Text([Text.BodyStrong], "User Name");
                                    view.Text([Text.Caption], "@username");
                                });
                            });
                            view.Text([Text.Body], "This is the user bio. It can contain more information about the user.");
                        });
                    });
            });

            // Toast
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Toast");
                view.Text([Text.Caption, "mb-4"], "Brief notifications that appear temporarily");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Button([Button.OutlineMd], label: "Show Toast", onClick: async () => _toastOpen.Value = true);

                    view.Text([Text.Caption, "mt-4"], "Static preview:");
                    view.Box([Toast.Base, Tokens.Width.Toast], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "flex-1"], content: view =>
                        {
                            view.Text([Toast.Title], "Toast Title");
                            view.Text([Toast.Description], "This is a toast notification message.");
                        });
                        view.Button([Toast.Action], label: "Undo");
                    });
                });

                view.Toast(
                    open: _toastOpen.Value,
                    onOpenChange: async open => _toastOpen.Value = open ?? false,
                    durationMs: 5000,
                    title: "Notification",
                    description: "Your action was completed successfully!",
                    showClose: true,
                    toastStyle: [Toast.Base],
                    viewportStyle: [Toast.ViewportBottomCenter],
                    titleStyle: [Toast.Title],
                    descriptionStyle: [Toast.Description],
                    closeStyle: [Toast.Close]);
            });

            // Sheet styles (static preview)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Sheet Styles");
                view.Text([Text.Caption, "mb-4"], "Sheet side panel styles (static preview)");

                view.Box([Card.Elevated, "p-4 max-w-sm"], content: view =>
                {
                    view.Box([Sheet.Header], content: view =>
                    {
                        view.Text([Sheet.Title], "Sheet Title");
                        view.Text([Sheet.Description], "Sheet description goes here.");
                    });
                    view.Text([Text.Body, "py-4"], "Sheet content area");
                    view.Box([Sheet.Footer], content: view =>
                    {
                        view.Button([Button.OutlineMd], label: "Cancel");
                        view.Button([Button.PrimaryMd], label: "Save");
                    });
                });
            });

            // Drawer styles (static preview)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Drawer Styles");
                view.Text([Text.Caption, "mb-4"], "Drawer bottom sheet styles (static preview)");

                view.Box([Card.Elevated, "rounded-t-xl overflow-hidden"], content: view =>
                {
                    view.Box([Layout.Center, "py-2"], content: view =>
                    {
                        view.Box([Drawer.Handle]);
                    });
                    view.Box([Drawer.Header], content: view =>
                    {
                        view.Text([Drawer.Title], "Drawer Title");
                        view.Text([Drawer.Description], "Drawer description");
                    });
                    view.Box(["p-4"], content: view =>
                    {
                        view.Text([Text.Body], "Drawer content area");
                    });
                    view.Box([Drawer.Footer], content: view =>
                    {
                        view.Button([Button.PrimaryMd, "w-full"], label: "Continue");
                    });
                });
            });
        });
    }
}
