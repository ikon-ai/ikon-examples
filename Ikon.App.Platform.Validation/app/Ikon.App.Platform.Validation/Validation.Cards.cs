public partial class Validation
{
    private void RenderCardsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Surface tokens
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Surface Tokens");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "3 levels: page → panel → card. bg-neutral-muted never flips in dark mode.");
                view.Box(["bg-background rounded-lg p-4 border border-secondary"], content: view =>
                {
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Box(["bg-background border border-secondary rounded-lg p-4 flex-1 min-w-32"], content: view =>
                        {
                            view.Text([Text.Label, "mb-1"], "bg-background");
                            view.Text([Text.Caption], "Page — outermost layer");
                        });
                        view.Box(["bg-surface border border-secondary rounded-lg p-4 flex-1 min-w-32"], content: view =>
                        {
                            view.Text([Text.Label, "mb-1"], "bg-surface");
                            view.Text([Text.Caption], "Panel — mid layer");
                        });
                        view.Box(["bg-card border border-secondary rounded-lg p-4 flex-1 min-w-32"], content: view =>
                        {
                            view.Text([Text.Label, "mb-1"], "bg-card");
                            view.Text([Text.Caption], "Card — top layer");
                        });
                        view.Box(["bg-neutral-muted border border-secondary rounded-lg p-4 flex-1 min-w-32"], content: view =>
                        {
                            view.Text([Text.Label, "mb-1 text-white"], "bg-neutral-muted");
                            view.Text([Text.Caption, "text-white/60"], "Always dark — code, data");
                        });
                    });
                });
            });

            // Card variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Card Variants");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Card.Default, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Default");
                        view.Text([Text.Caption], "No shadow — page sections, content areas, table rows");
                    });

                    view.Box([Card.Subtle, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Subtle");
                        view.Text([Text.Caption], "shadow-sm — standard content cards, list items");
                    });

                    view.Box([Card.Elevated, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Elevated");
                        view.Text([Text.Caption], "shadow-md — popovers, dropdowns, tooltips");
                    });

                    view.Box([Card.Strong, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Strong");
                        view.Text([Text.Caption], "shadow-lg — floating panels, command palette, sheets");
                    });

                    view.Box([Card.Outline, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Outline");
                        view.Text([Text.Caption], "No shadow, thicker border — selection states, highlighted items");
                    });

                    view.Box(["bg-surface rounded-lg p-2"], content: view =>
                    {
                        view.Box([Card.Flat, "p-4"], content: view =>
                        {
                            view.Text([Text.BodyStrong], "Card.Flat");
                            view.Text([Text.Caption], "No border, no shadow — relies on bg-card contrast against bg-surface.");
                        });
                    });

                    view.Box([Card.Ghost, "p-4 border border-dashed border-quaternary"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Ghost");
                        view.Text([Text.Caption], "Transparent — structural grouping, drag targets (dashed border for visibility only)");
                    });

                    view.Box([Card.Interactive, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Interactive");
                        view.Text([Text.Caption], "Shadow lift on hover — content cards, tiles, feature items");
                    });

                    view.Box([Card.InteractiveFill, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.InteractiveFill");
                        view.Text([Text.Caption], "Background fill on hover — list items, settings rows, compact cards");
                    });

                    view.Box([Card.Selected, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Selected");
                        view.Text([Text.Caption], "Light brand tint bg + brand border — active/selected state; pair with Card.Interactive or InteractiveFill");
                    });

                });
            });

            // Selectable card list demo
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Selectable Cards");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "Toggle between Card.Interactive (unselected) and Card.Selected (active). Click a card to select it.");

                view.Box(["grid grid-cols-3 gap-3"], content: view =>
                {
                    var items = new[]
                    {
                        ("card-1", "Design System",   "Tokens, components, and patterns",  "layout"),
                        ("card-2", "Navigation",      "Menus, sidebars, and breadcrumbs",  "compass"),
                        ("card-3", "Data & Charts",   "Tables, stat cards, and graphs",    "bar-chart-2"),
                        ("card-4", "Forms & Inputs",  "Text fields, selects, and toggles", "edit-3"),
                        ("card-5", "Overlays",        "Dialogs, popovers, and toasts",     "layers"),
                        ("card-6", "Layout",          "Split panels, grids, and spacing",  "grid"),
                    };

                    foreach (var (id, title, description, icon) in items)
                    {
                        var isSelected = _selectedCardId.Value == id;
                        view.Box([isSelected ? Card.Selected : Card.Interactive, "p-4"],
                            onClick: async () => { _selectedCardId.Value = id; },
                            content: view =>
                            {
                                view.Row(["items-center gap-3 mb-2"], content: view =>
                                {
                                    view.Icon(["w-5 h-5", isSelected ? "text-brand-primary" : "text-tertiary"], name: icon);
                                    view.Text([Text.BodyStrong], title);
                                });
                                view.Text([Text.Caption], description);
                            });
                    }
                });
            });

            // Glass cards
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Glass Cards");
                view.Text([Text.Caption, "mb-4"], "Requires a colourful background to see the blur and transparency effect");
                view.Box(["rounded-lg p-6 bg-gradient-to-br from-violet-600 via-blue-600 to-pink-600 relative overflow-hidden"], content: view =>
                {
                    view.Box(["absolute -top-10 -left-10 w-40 h-40 bg-yellow-400 rounded-full opacity-60"]);
                    view.Box(["absolute top-20 right-10 w-32 h-32 bg-cyan-400 rounded-full opacity-60"]);
                    view.Box(["absolute -bottom-5 left-1/3 w-28 h-28 bg-rose-400 rounded-full opacity-60"]);
                    view.Box(["absolute top-5 left-1/2 w-20 h-20 bg-green-400 rounded-full opacity-50"]);
                    view.Box(["absolute bottom-10 right-1/4 w-24 h-24 bg-orange-400 rounded-full opacity-50"]);

                    view.Row([Layout.Row.Md, "flex-wrap relative z-10"], content: view =>
                    {
                        view.Box([Card.Glass, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Card.Glass");
                            view.Text([Text.Caption, "text-white/70"], "Glassmorphism with backdrop blur");
                        });

                        view.Box([Card.GlassSubtle, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Card.GlassSubtle");
                            view.Text([Text.Caption, "text-white/70"], "Subtle glass with less blur");
                        });

                        view.Box([Card.Glass, Interaction.HoverCard, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Glass + Hover");
                            view.Text([Text.Caption, "text-white/70"], "Hover to see lift and glow");
                        });
                    });
                });
            });

            // Card with slots
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Card with Slots");
                view.Box([Card.Default], content: view =>
                {
                    view.Box([Card.Header], content: view =>
                    {
                        view.Text([Text.H3], "Card Title");
                        view.Text([Text.Caption], "Card description goes here");
                    });
                    view.Box([Card.Content], content: view =>
                    {
                        view.Text([Text.Body], "This is the card content area. It can contain any content you need.");
                    });
                    view.Box([Card.Footer], content: view =>
                    {
                        view.Button([Button.OutlineMd], label: "Cancel");
                        view.Button([Button.PrimaryMd], label: "Submit");
                    });
                });
            });


            // Badge variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Badge");
                view.Column([Layout.Column.Lg], content: view =>
                {
                    // Size
                    view.Text([Text.H3], "Size");
                    view.Row([Layout.Row.Sm, "flex-wrap items-center"], content: view =>
                    {
                        view.Box([Badge.GreySm], content: v => v.Text(text: "Small"));
                        view.Box([Badge.GreyMd], content: v => v.Text(text: "Medium"));
                        view.Box([Badge.GreyLg], content: v => v.Text(text: "Large"));
                    });

                    // Style
                    view.Text([Text.H3], "Style");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.BrandMd], content: v => v.Text(text: "Neutral"));
                        view.Box([Badge.OutlineBrandMd], content: v => v.Text(text: "Outline"));
                    });

                    // Colour
                    view.Text([Text.H3], "Colour");
                    view.Text([Text.Caption], "Neutral");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.GreyMd], content: v => v.Text(text: "Grey"));
                        view.Box([Badge.BrandMd], content: v => v.Text(text: "Brand"));
                        view.Box([Badge.ErrorMd], content: v => v.Text(text: "Error"));
                        view.Box([Badge.SuccessMd], content: v => v.Text(text: "Success"));
                        view.Box([Badge.WarningMd], content: v => v.Text(text: "Warning"));
                        view.Box([Badge.InfoMd], content: v => v.Text(text: "Info"));
                    });
                    view.Text([Text.Caption], "Outline");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.OutlineGreyMd], content: v => v.Text(text: "Grey"));
                        view.Box([Badge.OutlineBrandMd], content: v => v.Text(text: "Brand"));
                        view.Box([Badge.OutlineErrorMd], content: v => v.Text(text: "Error"));
                        view.Box([Badge.OutlineSuccessMd], content: v => v.Text(text: "Success"));
                        view.Box([Badge.OutlineWarningMd], content: v => v.Text(text: "Warning"));
                        view.Box([Badge.OutlineInfoMd], content: v => v.Text(text: "Info"));
                    });

                    // Icon
                    view.Text([Text.H3], "With Icon");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.SuccessMd, Badge.IconLeft], content: view =>
                        {
                            view.Icon([Icon.Xs], name: "circle-check");
                            view.Text(text: "Success");
                        });
                        view.Box([Badge.ErrorMd, Badge.IconLeft], content: view =>
                        {
                            view.Icon([Icon.Xs], name: "circle-x");
                            view.Text(text: "Error");
                        });
                        view.Box([Badge.WarningMd, Badge.IconLeft], content: view =>
                        {
                            view.Icon([Icon.Xs], name: "triangle-alert");
                            view.Text(text: "Warning");
                        });
                        view.Box([Badge.InfoMd, Badge.IconLeft], content: view =>
                        {
                            view.Icon([Icon.Xs], name: "info");
                            view.Text(text: "Info");
                        });
                        view.Box([Badge.GreyMd, Badge.IconRight], content: view =>
                        {
                            view.Icon([Icon.Xs], name: "x");
                            view.Text(text: "Dismiss");
                        });
                    });
                });
            });

            // Image card interactions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Image Card");
                view.Text([Text.Caption, "mb-4"], "Hover over each card to see the interaction");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    // Zoom + Dim
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Box([ImageCard.Root, "w-48 h-48"], content: view =>
                        {
                            view.Image([ImageCard.Image, ImageCard.Hover.Zoom], src: "https://picsum.photos/seed/a/192/192", alt: "Zoom");
                            view.Box([ImageCard.Overlay.Dim]);
                        });
                        view.Text([Text.Caption], "Zoom + Dim");
                    });

                    // Reveal caption
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Box([ImageCard.Root, "w-48 h-48"], content: view =>
                        {
                            view.Image([ImageCard.Image, ImageCard.Hover.Zoom], src: "https://picsum.photos/seed/b/192/192", alt: "Reveal");
                            view.Box([ImageCard.Overlay.Reveal], content: view =>
                            {
                                view.Text([ImageCard.Title], "Image Title");
                                view.Text([ImageCard.Caption], "Caption slides up on hover");
                            });
                        });
                        view.Text([Text.Caption], "Reveal");
                    });

                    // Center overlay
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Box([ImageCard.Root, "w-48 h-48"], content: view =>
                        {
                            view.Image([ImageCard.Image], src: "https://picsum.photos/seed/c/192/192", alt: "Center");
                            view.Box([ImageCard.Overlay.Center], content: view =>
                            {
                                view.Icon(["text-white w-10 h-10"], name: "play");
                            });
                        });
                        view.Text([Text.Caption], "Center overlay");
                    });

                    // Dim only
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Box([ImageCard.Root, "w-48 h-48"], content: view =>
                        {
                            view.Image([ImageCard.Image, ImageCard.Hover.Dim], src: "https://picsum.photos/seed/d/192/192", alt: "Dim");
                        });
                        view.Text([Text.Caption], "Dim only");
                    });
                });
            });

            // Alert variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Alert Variants");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Alert.Default], content: view =>
                    {
                        view.Text([Alert.Title], "Default Alert");
                        view.Text([Alert.Description], "This is a default alert message.");
                    });

                    view.Box([Alert.Error], content: view =>
                    {
                        view.Text([Alert.Title], "Error Alert");
                        view.Text([Alert.Description], "This is a danger/error alert message.");
                    });

                    view.Box([Alert.Success], content: view =>
                    {
                        view.Text([Alert.Title], "Success Alert");
                        view.Text([Alert.Description], "This is a success alert message.");
                    });

                    view.Box([Alert.Warning], content: view =>
                    {
                        view.Text([Alert.Title], "Warning Alert");
                        view.Text([Alert.Description], "This is a warning alert message.");
                    });

                    view.Box([Alert.Info], content: view =>
                    {
                        view.Text([Alert.Title], "Info Alert");
                        view.Text([Alert.Description], "This is an info alert message.");
                    });
                });
            });

            // Empty States
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Empty State");
                view.Text([Text.BodySm, "text-tertiary mb-6"], "Use when a list, table, or section has no content to show.");

                view.Column([Layout.Column.Lg], content: view =>
                {
                    // Default
                    view.Text([Text.H3, "mb-2"], "Default");
                    view.Box([Card.Subtle, "overflow-hidden"], content: view =>
                    {
                        view.Box([EmptyState.Root], content: view =>
                        {
                            view.Box([EmptyState.IconWrap], content: view =>
                                view.Icon([EmptyState.IconSize], name: "inbox"));
                            view.Text([EmptyState.Title], "No messages yet");
                            view.Text([EmptyState.Description], "When you receive messages they will appear here.");
                            view.Row([EmptyState.Actions], content: view =>
                            {
                                view.Button([Button.PrimaryMd], label: "Get started", onClick: async () => { });
                            });
                        });
                    });

                    // Compact
                    view.Text([Text.H3, "mb-2"], "Compact");
                    view.Box([Card.Subtle, "overflow-hidden"], content: view =>
                    {
                        view.Box([EmptyState.RootSm], content: view =>
                        {
                            view.Box([EmptyState.IconWrapSm], content: view =>
                                view.Icon([EmptyState.IconSizeSm], name: "search"));
                            view.Text([EmptyState.Title], "No results found");
                            view.Text([EmptyState.Description], "Try adjusting your search or filters.");
                        });
                    });

                    // With two actions
                    view.Text([Text.H3, "mb-2"], "With Two Actions");
                    view.Box([Card.Subtle, "overflow-hidden"], content: view =>
                    {
                        view.Box([EmptyState.Root], content: view =>
                        {
                            view.Box([EmptyState.IconWrap], content: view =>
                                view.Icon([EmptyState.IconSize], name: "folder-open"));
                            view.Text([EmptyState.Title], "No files uploaded");
                            view.Text([EmptyState.Description], "Upload your first file to get started, or import from another source.");
                            view.Row([EmptyState.Actions], content: view =>
                            {
                                view.Button([Button.PrimaryMd], label: "Upload file", onClick: async () => { });
                                view.Button([Button.OutlineMd], label: "Import", onClick: async () => { });
                            });
                        });
                    });

                    // With illustration / image
                    view.Text([Text.H3, "mb-2"], "With Illustration or Image");
                    view.Text([Text.Caption, "mb-3"], "Use IllustrationWrap instead of IconWrap — no background box, just sizing.");
                    view.Box([Card.Subtle, "overflow-hidden"], content: view =>
                    {
                        view.Box([EmptyState.Root], content: view =>
                        {
                            view.Box([EmptyState.IllustrationWrap], content: view =>
                                view.Image([EmptyState.IllustrationSize],
                                    src: "https://picsum.photos/seed/empty/160/160",
                                    alt: "Empty state illustration"));
                            view.Text([EmptyState.Title], "No data available");
                            view.Text([EmptyState.Description], "Connect a data source to start seeing your metrics here.");
                            view.Row([EmptyState.Actions], content: view =>
                            {
                                view.Button([Button.PrimaryMd], label: "Connect source", onClick: async () => { });
                            });
                        });
                    });

                    // No icon
                    view.Text([Text.H3, "mb-2"], "Without Icon");
                    view.Box([Card.Subtle, "overflow-hidden"], content: view =>
                    {
                        view.Box([EmptyState.RootSm], content: view =>
                        {
                            view.Text([EmptyState.Title], "Nothing here yet");
                            view.Text([EmptyState.Description], "Add your first item to get started.");
                            view.Row([EmptyState.Actions], content: view =>
                            {
                                view.Button([Button.NeutralSm], label: "Add item", onClick: async () => { });
                            });
                        });
                    });
                });
            });

            // Image onClick test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Image onClick");
                view.Text([Text.Body, "mb-4"], "Click the image below");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Image(["w-32 h-32 rounded-lg border border-gray-300 object-cover"],
                        src: "https://picsum.photos/128/128",
                        alt: "Clickable sample image",
                        onClick: async () =>
                        {
                            _imageClickCount.Value++;
                            _imageClickStatus.Value = $"Clicked {_imageClickCount.Value} time(s) at {DateTime.Now:HH:mm:ss}";
                        });
                    view.Text([Text.Caption, "mt-2"], $"Status: {_imageClickStatus.Value}");
                });
            });
        });
    }
}
