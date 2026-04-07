public partial class Validation
{
    private void RenderNavMenuSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Horizontal NavigationMenu
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Dropdown Menu - Horizontal");
                view.Text([Text.Caption, "mb-4"], "Default horizontal orientation with dropdown content");

                view.NavigationMenu(
                    ["relative z-10 flex flex-1 items-center justify-center"],
                    value: _navMenuValue.Value,
                    orientation: Orientation.Horizontal,
                    onValueChange: async v => _navMenuValue.Value = v,
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.List], content: view =>
                        {
                            // Menu item with content
                            view.NavigationMenuItem(["relative"], value: "getting-started", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.Trigger], content: view =>
                                {
                                    view.Text(text: "Getting Started");
                                    view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                });

                                view.NavigationMenuContent(
                                    [NavigationMenu.ContentPopover, "w-[400px]"],
                                    content: view =>
                                    {
                                        view.Column(["gap-1"], content: view =>
                                        {
                                            view.Text([Text.BodyStrong], "Getting Started");
                                            view.NavigationMenuLink(
                                                [NavigationMenu.Link],
                                                active: false,
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Introduction",
                                                content: view =>
                                                {
                                                    view.Text([Text.Body], "Introduction");
                                                    view.Text([Text.Caption], "Learn the basics of our platform");
                                                });
                                            view.NavigationMenuLink(
                                                [NavigationMenu.Link],
                                                active: false,
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Installation",
                                                content: view =>
                                                {
                                                    view.Text([Text.Body], "Installation");
                                                    view.Text([Text.Caption], "Step-by-step setup guide");
                                                });
                                            view.NavigationMenuLink(
                                                [NavigationMenu.Link],
                                                active: true,
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Quick Start (active)",
                                                content: view =>
                                                {
                                                    view.Text([Text.Body], "Quick Start (active)");
                                                    view.Text([Text.Caption], "Get up and running quickly");
                                                });
                                        });
                                    });
                            });

                            // Menu item with content
                            view.NavigationMenuItem(["relative"], value: "components", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.Trigger], content: view =>
                                {
                                    view.Text(text: "Components");
                                    view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                });

                                view.NavigationMenuContent(
                                    [NavigationMenu.ContentPopover, "w-[500px]"],
                                    content: view =>
                                    {
                                        view.Row(["gap-4"], content: view =>
                                        {
                                            view.Column(["flex-1 gap-1"], content: view =>
                                            {
                                                view.Text([Text.Caption], "Layout");
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Grid",
                                                    content: v => v.Text(text: "Grid"));
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Flex",
                                                    content: v => v.Text(text: "Flex"));
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Container",
                                                    content: v => v.Text(text: "Container"));
                                            });
                                            view.Column(["flex-1 gap-1"], content: view =>
                                            {
                                                view.Text([Text.Caption], "Inputs");
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Button",
                                                    content: v => v.Text(text: "Button"));
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: TextField",
                                                    content: v => v.Text(text: "TextField"));
                                                view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Checkbox",
                                                    content: v => v.Text(text: "Checkbox"));
                                            });
                                        });
                                    });
                            });

                            // Direct link (no dropdown)
                            view.NavigationMenuItem(content: view =>
                            {
                                view.NavigationMenuLink(
                                    [NavigationMenu.Trigger],
                                    onSelect: async () => _navMenuStatus.Value = "Navigated to Documentation",
                                    content: view => view.Text(text: "Documentation"));
                            });

                            // Disabled trigger
                            view.NavigationMenuItem(value: "disabled", content: view =>
                            {
                                view.NavigationMenuTrigger(
                                    [NavigationMenu.TriggerDisabled],
                                    disabled: true,
                                    content: view =>
                                    {
                                        view.Text(text: "Disabled");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIcon], name: "chevron-down");
                                    });
                            });
                        });

                        view.NavigationMenuIndicator([NavigationMenu.Indicator]);
                    });
            });

            // Vertical NavigationMenu
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Dropdown Menu - Vertical");
                view.Text([Text.Caption, "mb-4"], "Vertical orientation with side-opening dropdowns");

                view.NavigationMenu(
                    ["relative flex flex-col w-[250px]"],
                    orientation: Orientation.Vertical,
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.ListVertical], content: view =>
                        {
                            view.NavigationMenuItem(["relative"], value: "dashboard", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.TriggerVertical], content: view =>
                                {
                                    view.Row([Layout.Row.InlineCenter], content: view =>
                                    {
                                        view.Icon([Icon.Default], name: "layout-dashboard");
                                        view.Text(text: "Dashboard");
                                    });
                                    view.Icon([NavigationMenu.TriggerIconRotate90], name: "chevron-right");
                                });

                                view.NavigationMenuContent(
                                    [NavigationMenu.ContentPopoverSide, "w-[200px]"],
                                    content: view =>
                                    {
                                        view.Column(["gap-1"], content: view =>
                                        {
                                            view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Overview",
                                                content: v => v.Text(text: "Overview"));
                                            view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Analytics",
                                                content: v => v.Text(text: "Analytics"));
                                            view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Reports",
                                                content: v => v.Text(text: "Reports"));
                                        });
                                    });
                            });

                            view.NavigationMenuItem(["relative"], value: "settings", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.TriggerVertical], content: view =>
                                {
                                    view.Row([Layout.Row.InlineCenter], content: view =>
                                    {
                                        view.Icon([Icon.Default], name: "gear");
                                        view.Text(text: "Settings");
                                    });
                                    view.Icon([NavigationMenu.TriggerIconRotate90], name: "chevron-right");
                                });

                                view.NavigationMenuContent(
                                    [NavigationMenu.ContentPopoverSide, "w-[200px]"],
                                    content: view =>
                                    {
                                        view.Column(["gap-1"], content: view =>
                                        {
                                            view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Profile",
                                                content: v => v.Text(text: "Profile"));
                                            view.NavigationMenuLink([NavigationMenu.LinkCompact],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Preferences",
                                                content: v => v.Text(text: "Preferences"));
                                        });
                                    });
                            });

                            view.NavigationMenuItem(content: view =>
                            {
                                view.NavigationMenuLink(
                                    [NavigationMenu.TriggerVertical],
                                    onSelect: async () => _navMenuStatus.Value = "Selected: Help",
                                    content: view =>
                                    {
                                        view.Row([Layout.Row.InlineCenter], content: view =>
                                        {
                                            view.Icon([Icon.Default], name: "question-mark-circled");
                                            view.Text(text: "Help");
                                        });
                                    });
                            });
                        });
                    });
            });

            // NavigationMenu with delay settings
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Dropdown Menu - Custom Delays");
                view.Text([Text.Caption, "mb-4"], "Configure open/close delay durations");

                view.NavigationMenu(
                    ["relative"],
                    delayDuration: 200,
                    skipDelayDuration: 100,
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.List], content: view =>
                        {
                            view.NavigationMenuItem(["relative"], value: "fast", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.Trigger], content: view =>
                                {
                                    view.Text(text: "Quick Open (200ms)");
                                    view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                });

                                view.NavigationMenuContent(
                                    [NavigationMenu.ContentPopover, "w-[200px]"],
                                    content: view => view.Text([Text.Body], "Opens quickly with 200ms delay"));
                            });
                        });
                    });
            });

            // NavigationMenu with Viewport
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Dropdown Menu - Viewport");
                view.Text([Text.Caption, "mb-4"], "Using NavigationMenuViewport for animated content transitions");

                view.NavigationMenu(
                    ["relative flex items-center justify-center"],
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.List], content: view =>
                        {
                            view.NavigationMenuItem(["relative"], value: "viewport-1", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.Trigger], content: view =>
                                {
                                    view.Text(text: "Menu 1");
                                    view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                });

                                view.NavigationMenuContent(["w-[300px] p-4"], content: view =>
                                {
                                    view.Column(["gap-2"], content: view =>
                                    {
                                        view.Text([Text.BodyStrong], "Menu 1 Content");
                                        view.Text([Text.Body], "This content appears in the viewport");
                                    });
                                });
                            });

                            view.NavigationMenuItem(["relative"], value: "viewport-2", content: view =>
                            {
                                view.NavigationMenuTrigger([NavigationMenu.Trigger], content: view =>
                                {
                                    view.Text(text: "Menu 2");
                                    view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                });

                                view.NavigationMenuContent(["w-[400px] p-4"], content: view =>
                                {
                                    view.Column(["gap-2"], content: view =>
                                    {
                                        view.Text([Text.BodyStrong], "Menu 2 Content");
                                        view.Text([Text.Body], "Different width content in the same viewport");
                                    });
                                });
                            });
                        });

                        view.NavigationMenuViewport([NavigationMenu.Viewport]);
                    });
            });

            // Status display
            if (!string.IsNullOrEmpty(_navMenuStatus.Value))
            {
                view.Box([Card.Elevated, "p-4 mt-4"], content: view =>
                {
                    view.Text([Text.Caption], "Last Navigation Event:");
                    view.Text([Text.Body], _navMenuStatus.Value);
                });
            }

            // NavPanel + NavSection
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "NavPanel / NavSection");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "NavPanel is the sidebar container. NavSection groups items with a label inside the panel.");

                view.Row([Layout.Row.Lg, "items-start flex-wrap"], content: view =>
                {
                    // Filled — colored bg: use Subtle / ActiveSubtle
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "NavPanel.Filled");
                        view.Text([Text.Caption, "text-quaternary mb-2"], "→ NavItem.Subtle + ActiveSubtle");
                        view.Box([NavPanel.Filled, "w-52 rounded-lg"], content: view =>
                        {
                            view.Box([NavSection.Root], content: view =>
                            {
                                view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                                view.Box([NavItem.Md, NavItem.ActiveSubtle], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                                view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                            });
                        });
                    });

                    // Ghost — transparent, inherits white parent: use Default / Active
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "NavPanel.Ghost");
                        view.Text([Text.Caption, "text-quaternary mb-2"], "→ NavItem.Default + Active");
                        view.Box([NavPanel.Ghost, "w-52"], content: view =>
                        {
                            view.Box([NavSection.Root], content: view =>
                            {
                                view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                                view.Box([NavItem.Md, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                                view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                            });
                        });
                    });

                    // Border composable — bg-card uses Default, colored bg uses Subtle
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "NavPanel.Border (composable bg)");
                        view.Row([Layout.Row.Sm, "items-start"], content: view =>
                        {
                            // bg-card → white, use Default / Active
                            view.Box([NavPanel.Border, "bg-card", "w-36"], content: view =>
                            {
                                view.Text([Text.Caption, "px-3 pt-2 mb-1"], "bg-card");
                                view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Home"); });
                                view.Box([NavItem.Md, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Stats"); });
                            });
                            // bg-surface → gray, use Subtle / ActiveSubtle
                            view.Box([NavPanel.Border, "bg-surface", "w-36"], content: view =>
                            {
                                view.Text([Text.Caption, "px-3 pt-2 mb-1"], "bg-surface");
                                view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Home"); });
                                view.Box([NavItem.Md, NavItem.ActiveSubtle], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Stats"); });
                            });
                            // bg-secondary → gray, use Subtle / ActiveSubtle
                            view.Box([NavPanel.Border, "bg-secondary", "w-36"], content: view =>
                            {
                                view.Text([Text.Caption, "px-3 pt-2 mb-1"], "bg-secondary");
                                view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Home"); });
                                view.Box([NavItem.Md, NavItem.ActiveSubtle], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Stats"); });
                            });
                        });
                    });

                    // Divided
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "NavPanel.Divided");
                        view.Box(["flex flex-row"], content: view =>
                        {
                            view.Box([NavPanel.Divided, "w-52"], content: view =>
                            {
                                view.Box([NavSection.Root], content: view =>
                                {
                                    view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                                    view.Box([NavItem.Md, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                    view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                                    view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                                });
                            });
                            view.Box(["flex-1 p-4 bg-background rounded-r-lg min-w-24"], content: view =>
                            {
                                view.Text([Text.BodySm, "text-tertiary"], "Main content");
                            });
                        });
                    });
                });
            });

            // NavItem
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "NavItem");
                view.Text([Text.BodySm, "text-tertiary mb-6"], "Combine a size (Sm/Md/Lg) with a state. Default is always the same. Active state is an app-level choice — pick one and use it consistently.");

                // Sizes
                view.Text([Text.H3, "mb-3"], "Sizes");
                view.Row([Layout.Row.Lg, "items-start flex-wrap mb-6"], content: view =>
                {
                    foreach (var (sizeClass, label) in new[] {
                        (NavItem.Sm, "NavItem.Sm"),
                        (NavItem.Md, "NavItem.Md"),
                        (NavItem.Lg, "NavItem.Lg"),
                    })
                    {
                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Text([Text.Caption, "mb-2"], label);
                            view.Box([NavPanel.Ghost, "bg-card w-52 rounded-lg border border-secondary"], content: view =>
                            {
                                view.Box([sizeClass, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                                view.Box([sizeClass, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                view.Box([sizeClass, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                                view.Box([sizeClass, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                            });
                        });
                    }
                });

                // Background context — Default vs Subtle
                view.Text([Text.H3, "mb-3"], "Background Context");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "Use Default on white backgrounds. Use Subtle on gray/filled backgrounds so hover and active remain visible.");
                view.Row([Layout.Row.Lg, "items-start flex-wrap mb-6"], content: view =>
                {
                    // Default on white
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-1"], "Default + Active");
                        view.Text([Text.Caption, "text-quaternary mb-2"], "On bg-card (white)");
                        view.Box([NavPanel.Ghost, "bg-card w-52 rounded-lg border border-secondary"], content: view =>
                        {
                            view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                            view.Box([NavItem.Md, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                            view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                            view.Box([NavItem.Md, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                        });
                    });
                    // Subtle on surface
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text([Text.Caption, "mb-1"], "Subtle + ActiveSubtle");
                        view.Text([Text.Caption, "text-quaternary mb-2"], "On bg-surface (gray)");
                        view.Box([NavPanel.Filled, "w-52 rounded-lg"], content: view =>
                        {
                            view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                            view.Box([NavItem.Md, NavItem.ActiveSubtle], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                            view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                            view.Box([NavItem.Md, NavItem.Subtle], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                        });
                    });
                });

                // Active variants
                view.Text([Text.H3, "mb-3"], "Active State Variants");
                view.Row([Layout.Row.Lg, "items-start flex-wrap"], content: view =>
                {
                    foreach (var (activeClass, defaultClass, panelClass, bgClass, label, desc) in new[] {
                        (NavItem.Active,       NavItem.Default, NavPanel.Ghost, "bg-card",    "NavItem.Active",       "Neutral fill — no brand"),
                        (NavItem.ActiveBrand,  NavItem.Default, NavPanel.Ghost, "bg-card",    "NavItem.ActiveBrand",  "Brand tint — use on white panels only"),
                        (NavItem.ActiveAccent, NavItem.Default, NavPanel.Ghost, "bg-card",    "NavItem.ActiveAccent", "Left border bar — desktop/IDE"),
                        (NavItem.ActiveSubtle, NavItem.Subtle,  NavPanel.Filled, "",          "NavItem.ActiveSubtle", "Neutral active — gray/filled panels"),
                    })
                    {
                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], label);
                            view.Text([Text.Caption, "text-quaternary mb-2"], desc);
                            view.Box([panelClass, bgClass, "w-52 rounded-lg border border-secondary"], content: view =>
                            {
                                view.Box([NavItem.Md, defaultClass], content: view => { view.Icon([NavItem.Icon], name: "home"); view.Text([NavItem.Label], "Dashboard"); });
                                view.Box([NavItem.Md, activeClass], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                view.Box([NavItem.Md, defaultClass], content: view => { view.Icon([NavItem.Icon], name: "users"); view.Text([NavItem.Label], "Team"); });
                                view.Box([NavItem.Md, defaultClass], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                            });
                        });
                    }
                });
            });
        });
    }
}
