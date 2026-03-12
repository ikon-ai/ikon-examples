public partial class Validation
{
    private void RenderButtonsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Primary buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Primary Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimarySm], label: "Primary Sm");
                    view.Button([Button.PrimaryMd], label: "Primary Md");
                    view.Button([Button.PrimaryLg], label: "Primary Lg");
                    view.Button([Button.PrimaryMd], label: "Disabled", disabled: true);
                });
            });

            // Secondary button
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Secondary Button");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.SecondaryMd], label: "Secondary Md");
                    view.Button([Button.SecondaryMd], label: "Disabled", disabled: true);
                });
            });

            // Outline buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Outline Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.OutlineSm], label: "Outline Sm");
                    view.Button([Button.OutlineMd], label: "Outline Md");
                    view.Button([Button.OutlineMd], label: "Disabled", disabled: true);
                });
            });

            // Ghost button
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Ghost Button");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.GhostMd], label: "Ghost Md");
                    view.Button([Button.GhostMd], label: "Disabled", disabled: true);
                });
            });

            // Danger button
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Danger Button");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.DangerMd], label: "Danger Md");
                    view.Button([Button.DangerMd], label: "Disabled", disabled: true);
                });
            });

            // Link button
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Link Button");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.LinkMd], label: "Link Md");
                    view.Button([Button.LinkMd], label: "Disabled", disabled: true);
                });
            });

            // Loading buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Loading Buttons");
                view.Text([Text.Caption, "mb-4"], "Buttons with loading state using spinner indicators");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd, State.Loading], content: view =>
                        {
                            view.Box([Icon.Spinner, "mr-2"]);
                            view.Text(text: "Loading...");
                        });
                        view.Button([Button.SecondaryMd, State.Loading], content: view =>
                        {
                            view.Box([Icon.Spinner, "mr-2"]);
                            view.Text(text: "Processing");
                        });
                        view.Button([Button.OutlineMd, State.Loading], content: view =>
                        {
                            view.Box([Icon.Spinner, "mr-2"]);
                            view.Text(text: "Saving...");
                        });
                    });

                    view.Text([Text.Caption, "mt-4"], "Spinner sizes");
                    view.Row([Layout.Row.Md, "flex-wrap items-center"], content: view =>
                    {
                        view.Box([Icon.SpinnerSm]);
                        view.Text([Text.Caption], "SpinnerSm");
                        view.Box([Icon.Spinner]);
                        view.Text([Text.Caption], "Spinner");
                        view.Box([Icon.SpinnerLg]);
                        view.Text([Text.Caption], "SpinnerLg");
                    });

                    view.Text([Text.Caption, "mt-4"], "Icon button loading");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd, Button.Size.Icon, State.Loading], label: "Loading", content: v => v.Box([Icon.Spinner]));
                        view.Button([Button.SecondaryMd, Button.Size.Icon, State.Loading], label: "Loading", content: v => v.Box([Icon.Spinner]));
                        view.Button([Button.OutlineMd, Button.Size.Icon, State.Loading], label: "Loading", content: v => v.Box([Icon.Spinner]));
                    });
                });
            });

            // Icon buttons with real icons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Icon Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, Button.Size.Icon], label: "Heart", content: v => v.Icon([Icon.Default], name: "heart"));
                    view.Button([Button.SecondaryMd, Button.Size.Icon], label: "Star", content: v => v.Icon([Icon.Default], name: "star"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Settings", content: v => v.Icon([Icon.Default], name: "settings"));
                    view.Button([Button.GhostMd, Button.Size.Icon], label: "Search", content: v => v.Icon([Icon.Default], name: "search"));
                    view.Button([Button.DangerMd, Button.Size.Icon], label: "Delete", content: v => v.Icon([Icon.Default], name: "trash-2"));
                });
                view.Row([Layout.Row.Md, "flex-wrap", "mt-4"], content: view =>
                {
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Add", content: v => v.Icon([Icon.Default], name: "plus"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Remove", content: v => v.Icon([Icon.Default], name: "minus"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Confirm", content: v => v.Icon([Icon.Default], name: "check"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Close", content: v => v.Icon([Icon.Default], name: "x"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Expand", content: v => v.Icon([Icon.Default], name: "chevron-down"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Collapse", content: v => v.Icon([Icon.Default], name: "chevron-up"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Menu", content: v => v.Icon([Icon.Default], name: "menu"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "More options", content: v => v.Icon([Icon.Default], name: "more-horizontal"));
                });
            });

            // Emoji buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Emoji Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, Button.Size.Icon], label: "Heart", content: v => v.Text(text: "❤️"));
                    view.Button([Button.SecondaryMd, Button.Size.Icon], label: "Star", content: v => v.Text(text: "⭐"));
                    view.Button([Button.OutlineMd, Button.Size.Icon], label: "Settings", content: v => v.Text(text: "⚙️"));
                    view.Button([Button.GhostMd, Button.Size.Icon], label: "Search", content: v => v.Text(text: "🔍"));
                    view.Button([Button.DangerMd, Button.Size.Icon], label: "Delete", content: v => v.Text(text: "🗑️"));
                });
            });

            // Toggle buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Toggle Buttons");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Toggle([Toggle.DefaultSm],
                            pressed: _togglePressed.Value, onPressedChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Sm"));
                        view.Toggle([Toggle.DefaultMd],
                            pressed: _togglePressed.Value, onPressedChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Md"));
                        view.Toggle([Toggle.DefaultLg],
                            pressed: _togglePressed.Value, onPressedChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Lg"));
                    });
                });
            });

            // Toggle groups
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Toggle Groups");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Single selection (alignment)");
                    view.ToggleGroupSingle([Layout.Row.Sm],
                        value: _toggleGroupSingleValue.Value,
                        onValueChange: async value => _toggleGroupSingleValue.Value = value ?? "",
                        content: view =>
                        {
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "left", content: v => v.Text(text: "⬅️"));
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "center", content: v => v.Text(text: "↔️"));
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "right", content: v => v.Text(text: "➡️"));
                        });

                    view.Text([Text.Caption], "Multiple selection (formatting)");
                    view.ToggleGroupMultiple([Layout.Row.Sm],
                        value: _toggleGroupValues.Value,
                        onValueChange: async values => _toggleGroupValues.Value = values,
                        content: view =>
                        {
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "bold", content: v => v.Text(["font-bold"], "B"));
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "italic", content: v => v.Text(["italic"], "I"));
                            view.ToggleGroupItem([Toggle.DefaultMd], value: "underline", content: v => v.Text(["underline"], "U"));
                        });
                });
            });
        });
    }
}
