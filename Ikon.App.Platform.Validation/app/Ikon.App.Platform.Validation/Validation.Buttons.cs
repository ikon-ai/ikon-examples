public partial class Validation
{
    private void RenderButtonsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Solid
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Solid");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "Always brand-600 background and white text — identical in light and dark mode. Use when the button must look the same regardless of theme.");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.SolidSm],  text: "Solid Sm");
                    view.Button([Button.SolidMd],  text: "Solid Md");
                    view.Button([Button.SolidLg],  text: "Solid Lg");
                    view.Button([Button.SolidMd],  text: "Disabled", disabled: true);
                });
            });

            // Primary
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Primary");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "Adapts to theme — deep brand background with white text in light mode; light pastel brand background with dark text in dark mode.");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimarySm],  text: "Primary Sm");
                    view.Button([Button.PrimaryMd],  text: "Primary Md");
                    view.Button([Button.PrimaryLg],  text: "Primary Lg");
                    view.Button([Button.PrimaryMd],  text: "Disabled", disabled: true);
                });
            });

            // Neutral
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Neutral");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.NeutralSm], text: "Neutral Sm");
                    view.Button([Button.NeutralMd], text: "Neutral Md");
                    view.Button([Button.NeutralLg], text: "Neutral Lg");
                    view.Button([Button.NeutralMd], text: "Disabled", disabled: true);
                });
            });

            // Outline
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Outline");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.OutlineSm], text: "Outline Sm");
                    view.Button([Button.OutlineMd], text: "Outline Md");
                    view.Button([Button.OutlineLg], text: "Outline Lg");
                    view.Button([Button.OutlineMd], text: "Disabled", disabled: true);
                });
            });

            // Ghost
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Ghost");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.GhostSm], text: "Ghost Sm");
                    view.Button([Button.GhostMd], text: "Ghost Md");
                    view.Button([Button.GhostLg], text: "Ghost Lg");
                    view.Button([Button.GhostMd], text: "Disabled", disabled: true);
                });
            });

            // Error
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Error");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.ErrorSm], text: "Error Sm");
                    view.Button([Button.ErrorMd], text: "Error Md");
                    view.Button([Button.ErrorLg], text: "Error Lg");
                    view.Button([Button.ErrorMd], text: "Disabled", disabled: true);
                });
            });

            // Success
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Success");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.SuccessSm], text: "Success Sm");
                    view.Button([Button.SuccessMd], text: "Success Md");
                    view.Button([Button.SuccessLg], text: "Success Lg");
                    view.Button([Button.SuccessMd], text: "Disabled", disabled: true);
                });
            });

            // Warning
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Warning");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.WarningSm], text: "Warning Sm");
                    view.Button([Button.WarningMd], text: "Warning Md");
                    view.Button([Button.WarningLg], text: "Warning Lg");
                    view.Button([Button.WarningMd], text: "Disabled", disabled: true);
                });
            });

            // Info
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Info");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.InfoSm], text: "Info Sm");
                    view.Button([Button.InfoMd], text: "Info Md");
                    view.Button([Button.InfoLg], text: "Info Lg");
                    view.Button([Button.InfoMd], text: "Disabled", disabled: true);
                });
            });

            // Link
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Link");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.LinkSm], text: "Link Sm");
                    view.Button([Button.LinkMd], text: "Link Md");
                    view.Button([Button.LinkLg], text: "Link Lg");
                    view.Button([Button.LinkMd], text: "Disabled", disabled: true);
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
                        view.Button([Button.NeutralMd, State.Loading], content: view =>
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
                        view.Button([Button.PrimaryMd, Button.Icon, State.Loading], text: "Loading", content: v => v.Box([Icon.Spinner]));
                        view.Button([Button.NeutralMd, Button.Icon, State.Loading], text: "Loading", content: v => v.Box([Icon.Spinner]));
                        view.Button([Button.OutlineMd, Button.Icon, State.Loading], text: "Loading", content: v => v.Box([Icon.Spinner]));
                    });
                });
            });

            // Icon buttons with real icons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Icon Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, Button.Icon], text: "Heart", content: v => v.Icon([Icon.Default], name: "heart"));
                    view.Button([Button.NeutralMd, Button.Icon], text: "Star", content: v => v.Icon([Icon.Default], name: "star"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Settings", content: v => v.Icon([Icon.Default], name: "settings"));
                    view.Button([Button.GhostMd, Button.Icon], text: "Search", content: v => v.Icon([Icon.Default], name: "search"));
                    view.Button([Button.ErrorMd, Button.Icon], text: "Delete", content: v => v.Icon([Icon.Default], name: "trash-2"));
                });
                view.Row([Layout.Row.Md, "flex-wrap", "mt-4"], content: view =>
                {
                    view.Button([Button.OutlineMd, Button.Icon], text: "Add", content: v => v.Icon([Icon.Default], name: "plus"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Remove", content: v => v.Icon([Icon.Default], name: "minus"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Confirm", content: v => v.Icon([Icon.Default], name: "check"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Close", content: v => v.Icon([Icon.Default], name: "x"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Expand", content: v => v.Icon([Icon.Default], name: "chevron-down"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Collapse", content: v => v.Icon([Icon.Default], name: "chevron-up"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Menu", content: v => v.Icon([Icon.Default], name: "menu"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "More options", content: v => v.Icon([Icon.Default], name: "more-horizontal"));
                });
            });

            // Emoji buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Emoji Buttons");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, Button.Icon], text: "Heart", content: v => v.Text(text: "❤️"));
                    view.Button([Button.NeutralMd, Button.Icon], text: "Star", content: v => v.Text(text: "⭐"));
                    view.Button([Button.OutlineMd, Button.Icon], text: "Settings", content: v => v.Text(text: "⚙️"));
                    view.Button([Button.GhostMd, Button.Icon], text: "Search", content: v => v.Text(text: "🔍"));
                    view.Button([Button.ErrorMd, Button.Icon], text: "Delete", content: v => v.Text(text: "🗑️"));
                });
            });

            // Buttons with icons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Buttons with Icons");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Label], "Icon Left");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd, Button.IconLeft], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "plus");
                            v.Text(text: "Create New");
                        });
                        view.Button([Button.NeutralMd, Button.IconLeft], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "settings");
                            v.Text(text: "Settings");
                        });
                        view.Button([Button.OutlineMd, Button.IconLeft], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "search");
                            v.Text(text: "Search");
                        });
                        view.Button([Button.GhostMd, Button.IconLeft], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "download");
                            v.Text(text: "Download");
                        });
                        view.Button([Button.ErrorMd, Button.IconLeft], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "trash-2");
                            v.Text(text: "Delete");
                        });
                    });
                    view.Text([Text.Label], "Icon Right");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd, Button.IconRight], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "arrow-right");
                            v.Text(text: "Continue");
                        });
                        view.Button([Button.NeutralMd, Button.IconRight], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "external-link");
                            v.Text(text: "Open");
                        });
                        view.Button([Button.OutlineMd, Button.IconRight], content: v =>
                        {
                            v.Icon([Icon.Xs], name: "chevron-down");
                            v.Text(text: "More");
                        });
                    });
                });
            });

            // Toggle buttons
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Toggle Buttons");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Label], "Text");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Toggle([Toggle.DefaultSm],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Sm"));
                        view.Toggle([Toggle.DefaultMd],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Md"));
                        view.Toggle([Toggle.DefaultLg],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "Lg"));
                    });
                    view.Text([Text.Label], "Icon");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Toggle([Toggle.IconDefaultSm],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "✱"));
                        view.Toggle([Toggle.IconDefaultMd],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "✱"));
                        view.Toggle([Toggle.IconDefaultLg],
                            value: _togglePressed.Value, onValueChange: async pressed => _togglePressed.Value = pressed, content: v => v.Text(text: "✱"));
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
