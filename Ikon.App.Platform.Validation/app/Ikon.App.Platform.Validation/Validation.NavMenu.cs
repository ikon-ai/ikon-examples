public partial class Validation
{
    private void RenderNavMenuSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Horizontal NavigationMenu
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "NavigationMenu - Horizontal");
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
                                view.NavigationMenuTrigger(
                                    ["group inline-flex h-9 w-max items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent data-[state=open]:bg-accent"],
                                    content: view =>
                                    {
                                        view.Text(text: "Getting Started");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                    });

                                view.NavigationMenuContent(
                                    ["absolute top-full left-0 w-[400px] p-4 bg-popover border rounded-md shadow-lg"],
                                    content: view =>
                                    {
                                        view.Column(["gap-3"], content: view =>
                                        {
                                            view.Text([Text.BodyStrong], "Getting Started");
                                            view.NavigationMenuLink(
                                                ["block p-3 rounded-md hover:bg-accent"],
                                                active: false,
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Introduction",
                                                content: view =>
                                                {
                                                    view.Text([Text.Body], "Introduction");
                                                    view.Text([Text.Caption], "Learn the basics of our platform");
                                                });
                                            view.NavigationMenuLink(
                                                ["block p-3 rounded-md hover:bg-accent"],
                                                active: false,
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Installation",
                                                content: view =>
                                                {
                                                    view.Text([Text.Body], "Installation");
                                                    view.Text([Text.Caption], "Step-by-step setup guide");
                                                });
                                            view.NavigationMenuLink(
                                                ["block p-3 rounded-md hover:bg-accent"],
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
                                view.NavigationMenuTrigger(
                                    ["group inline-flex h-9 w-max items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent data-[state=open]:bg-accent"],
                                    content: view =>
                                    {
                                        view.Text(text: "Components");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                    });

                                view.NavigationMenuContent(
                                    ["absolute top-full left-0 w-[500px] p-4 bg-popover border rounded-md shadow-lg"],
                                    content: view =>
                                    {
                                        view.Row(["gap-4"], content: view =>
                                        {
                                            view.Column(["flex-1 gap-2"], content: view =>
                                            {
                                                view.Text([Text.Caption], "Layout");
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Grid",
                                                    content: v => v.Text(text: "Grid"));
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Flex",
                                                    content: v => v.Text(text: "Flex"));
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Container",
                                                    content: v => v.Text(text: "Container"));
                                            });
                                            view.Column(["flex-1 gap-2"], content: view =>
                                            {
                                                view.Text([Text.Caption], "Inputs");
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: Button",
                                                    content: v => v.Text(text: "Button"));
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
                                                    onSelect: async () => _navMenuStatus.Value = "Selected: TextField",
                                                    content: v => v.Text(text: "TextField"));
                                                view.NavigationMenuLink(
                                                    ["block p-2 rounded-md hover:bg-accent"],
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
                                    ["group inline-flex h-9 w-max items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent box-border"],
                                    onSelect: async () => _navMenuStatus.Value = "Navigated to Documentation",
                                    content: view =>
                                    {
                                        view.Text(text: "Documentation");
                                    });
                            });

                            // Disabled trigger
                            view.NavigationMenuItem(value: "disabled", content: view =>
                            {
                                view.NavigationMenuTrigger(
                                    ["inline-flex h-9 w-max items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium opacity-50 cursor-not-allowed"],
                                    disabled: true,
                                    content: view =>
                                    {
                                        view.Text(text: "Disabled");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIcon], name: "chevron-down");
                                    });
                            });
                        });

                        view.NavigationMenuIndicator(
                            ["flex h-2 items-end justify-center overflow-hidden data-[state=visible]:animate-in data-[state=hidden]:animate-out"]);
                    });
            });

            // Vertical NavigationMenu
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "NavigationMenu - Vertical");
                view.Text([Text.Caption, "mb-4"], "Vertical orientation for sidebar navigation");

                view.NavigationMenu(
                    ["relative flex flex-col w-[250px]"],
                    orientation: Orientation.Vertical,
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.ListVertical], content: view =>
                        {
                            view.NavigationMenuItem(["relative"], value: "dashboard", content: view =>
                            {
                                view.NavigationMenuTrigger(
                                    ["group flex w-full items-center justify-between rounded-md bg-muted text-foreground px-3 py-2 text-sm font-medium transition-colors hover:bg-accent data-[state=open]:bg-accent"],
                                    content: view =>
                                    {
                                        view.Row([Layout.Row.InlineCenter], content: view =>
                                        {
                                            view.Icon([Icon.Default], name: "layout-dashboard");
                                            view.Text(text: "Dashboard");
                                        });
                                        view.Icon([NavigationMenu.TriggerIconRotate90], name: "chevron-right");
                                    });

                                view.NavigationMenuContent(
                                    ["absolute left-[260px] top-0 z-50 w-[200px] p-2 bg-popover border rounded-md shadow-lg"],
                                    content: view =>
                                    {
                                        view.Column(["gap-1"], content: view =>
                                        {
                                            view.NavigationMenuLink(
                                                ["block p-2 rounded-md hover:bg-accent"],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Overview",
                                                content: v => v.Text(text: "Overview"));
                                            view.NavigationMenuLink(
                                                ["block p-2 rounded-md hover:bg-accent"],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Analytics",
                                                content: v => v.Text(text: "Analytics"));
                                            view.NavigationMenuLink(
                                                ["block p-2 rounded-md hover:bg-accent"],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Reports",
                                                content: v => v.Text(text: "Reports"));
                                        });
                                    });
                            });

                            view.NavigationMenuItem(["relative"], value: "settings", content: view =>
                            {
                                view.NavigationMenuTrigger(
                                    ["group flex w-full items-center justify-between rounded-md bg-muted text-foreground px-3 py-2 text-sm font-medium transition-colors hover:bg-accent data-[state=open]:bg-accent"],
                                    content: view =>
                                    {
                                        view.Row([Layout.Row.InlineCenter], content: view =>
                                        {
                                            view.Icon([Icon.Default], name: "gear");
                                            view.Text(text: "Settings");
                                        });
                                        view.Icon([NavigationMenu.TriggerIconRotate90], name: "chevron-right");
                                    });

                                view.NavigationMenuContent(
                                    ["absolute left-[260px] top-0 z-50 w-[200px] p-2 bg-popover border rounded-md shadow-lg"],
                                    content: view =>
                                    {
                                        view.Column(["gap-1"], content: view =>
                                        {
                                            view.NavigationMenuLink(
                                                ["block p-2 rounded-md hover:bg-accent"],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Profile",
                                                content: v => v.Text(text: "Profile"));
                                            view.NavigationMenuLink(
                                                ["block p-2 rounded-md hover:bg-accent"],
                                                onSelect: async () => _navMenuStatus.Value = "Selected: Preferences",
                                                content: v => v.Text(text: "Preferences"));
                                        });
                                    });
                            });

                            view.NavigationMenuItem(content: view =>
                            {
                                view.NavigationMenuLink(
                                    ["group flex w-full items-center justify-between rounded-md bg-muted text-foreground px-3 py-2 text-sm font-medium transition-colors hover:bg-accent box-border"],
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
                view.Text([Text.H2, "mb-4"], "NavigationMenu with Custom Delays");
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
                                view.NavigationMenuTrigger(
                                    ["group inline-flex h-9 items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent data-[state=open]:bg-accent"],
                                    content: view =>
                                    {
                                        view.Text(text: "Quick Open (200ms)");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                    });

                                view.NavigationMenuContent(
                                    ["absolute top-full left-0 w-[200px] p-3 bg-popover border rounded-md shadow-lg"],
                                    content: view =>
                                    {
                                        view.Text([Text.Body], "Opens quickly with 200ms delay");
                                    });
                            });
                        });
                    });
            });

            // NavigationMenu with Viewport
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "NavigationMenu with Viewport");
                view.Text([Text.Caption, "mb-4"], "Using NavigationMenuViewport for animated content transitions");

                view.NavigationMenu(
                    ["relative flex items-center justify-center"],
                    content: view =>
                    {
                        view.NavigationMenuList([NavigationMenu.List], content: view =>
                        {
                            view.NavigationMenuItem(["relative"], value: "viewport-1", content: view =>
                            {
                                view.NavigationMenuTrigger(
                                    ["group inline-flex h-9 items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent"],
                                    content: view =>
                                    {
                                        view.Text(text: "Menu 1");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                    });

                                view.NavigationMenuContent(
                                    ["w-[300px] p-4"],
                                    content: view =>
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
                                view.NavigationMenuTrigger(
                                    ["group inline-flex h-9 items-center justify-center rounded-md bg-muted text-foreground px-4 py-2 text-sm font-medium transition-colors hover:bg-accent"],
                                    content: view =>
                                    {
                                        view.Text(text: "Menu 2");
                                        view.Icon(["ml-1", NavigationMenu.TriggerIconRotate180], name: "chevron-down");
                                    });

                                view.NavigationMenuContent(
                                    ["w-[400px] p-4"],
                                    content: view =>
                                    {
                                        view.Column(["gap-2"], content: view =>
                                        {
                                            view.Text([Text.BodyStrong], "Menu 2 Content");
                                            view.Text([Text.Body], "Different width content in the same viewport");
                                        });
                                    });
                            });
                        });

                        view.NavigationMenuViewport(
                            ["absolute top-full left-0 mt-2 origin-top-center bg-popover border rounded-md shadow-lg overflow-hidden transition-all data-[state=open]:animate-in data-[state=closed]:animate-out"]);
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
        });
    }
}
