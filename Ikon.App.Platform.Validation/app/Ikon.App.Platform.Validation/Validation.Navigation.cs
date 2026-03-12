public partial class Validation
{
    private void RenderNavigationSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Tabs (simplified API example)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tabs");
                view.Text([Text.Caption, "mb-4"], "Tabs using simplified API");

                view.Tabs(
                    value: _nestedTabValue.Value,
                    onValueChange: async v => _nestedTabValue.Value = v ?? "nested1",
                    listStyle: [Tabs.List, "mb-4"],
                    triggerStyle: [Tabs.Trigger],
                    contentStyle: [Tabs.Content],
                    tabs: [
                        new TabItem("nested1", "Account", v => v.Text([Text.Body], "Account settings content goes here.")),
                        new TabItem("nested2", "Password", v => v.Text([Text.Body], "Password settings content goes here.")),
                        new TabItem("nested3", "Settings", v => v.Text([Text.Body], "General settings content goes here."))
                    ]);
            });

            // Tabs - Vertical Orientation
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tabs - Vertical");
                view.Text([Text.Caption, "mb-4"], "Tabs with vertical orientation");

                view.Tabs(
                    rootStyle: [Layout.Row.Md],
                    value: _verticalTabValue.Value,
                    orientation: Orientation.Vertical,
                    onValueChange: async v => _verticalTabValue.Value = v ?? "vtab1",
                    listStyle: [Tabs.ListVertical],
                    triggerStyle: [Tabs.Trigger],
                    contentContainerStyle: ["flex-1"],
                    contentStyle: [Tabs.Content],
                    tabs: [
                        new TabItem("vtab1", "General", v =>
                        {
                            v.Text([Text.BodyStrong], "General Settings");
                            v.Text([Text.Body], "Configure your general preferences here.");
                        }),
                        new TabItem("vtab2", "Privacy", v =>
                        {
                            v.Text([Text.BodyStrong], "Privacy Settings");
                            v.Text([Text.Body], "Manage your privacy and security options.");
                        }),
                        new TabItem("vtab3", "Notifications", v =>
                        {
                            v.Text([Text.BodyStrong], "Notification Settings");
                            v.Text([Text.Body], "Control how you receive notifications.");
                        })
                    ]);
            });

            // Tabs - Manual Activation
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tabs - Manual Activation");
                view.Text([Text.Caption, "mb-4"], "Tabs that require Enter/Space to activate (keyboard navigation only focuses)");

                view.Tabs(
                    value: _manualTabValue.Value,
                    activationMode: ActivationMode.Manual,
                    onValueChange: async v => _manualTabValue.Value = v ?? "manual1",
                    listStyle: [Tabs.List, "mb-4"],
                    triggerStyle: [Tabs.Trigger],
                    contentStyle: [Tabs.Content],
                    tabs: [
                        new TabItem("manual1", "Tab 1", v => v.Text([Text.Body], "Tab 1 content - Use arrow keys to move focus, Enter/Space to select.")),
                        new TabItem("manual2", "Tab 2", v => v.Text([Text.Body], "Tab 2 content - Manual activation mode is useful for heavy content.")),
                        new TabItem("manual3", "Tab 3", v => v.Text([Text.Body], "Tab 3 content - Content only loads when explicitly selected."))
                    ]);
            });

            // Tabs with Disabled Trigger
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tabs - Disabled Items");
                view.Text([Text.Caption, "mb-4"], "Some tabs can be disabled");

                view.Tabs(
                    listStyle: [Tabs.List, "mb-4"],
                    triggerStyle: [Tabs.Trigger],
                    disabledTriggerStyle: [Tabs.TriggerDisabled],
                    contentStyle: [Tabs.Content],
                    tabs: [
                        new TabItem("enabled1", "Enabled", v => v.Text([Text.Body], "First enabled tab content.")),
                        new TabItem("disabled1", "Disabled", _ => { }, Disabled: true),
                        new TabItem("enabled2", "Enabled", v => v.Text([Text.Body], "Second enabled tab content.")),
                        new TabItem("disabled2", "Disabled", _ => { }, Disabled: true)
                    ]);
            });

            // Accordion Single
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Accordion (Single)");
                view.Text([Text.Caption, "mb-4"], "Only one item can be open at a time");

                view.AccordionSingle([Accordion.Root],
                    value: _accordionValue.Value,
                    collapsible: true,
                    onValueChange: async v => _accordionValue.Value = v ?? "", content: view =>
                    {
                        view.AccordionItem([Accordion.Item], value: "item-1", content: view =>
                        {
                            view.AccordionHeader([Accordion.Header], content: view =>
                            {
                                view.AccordionTrigger([Accordion.Trigger], content: view =>
                                {
                                    view.Text(text: "Is it accessible?");
                                    view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                                });
                            });
                            view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                            {
                                view.Text([Text.Body], "Yes. It adheres to the WAI-ARIA design pattern.");
                            });
                        });

                        view.AccordionItem([Accordion.Item], value: "item-2", content: view =>
                        {
                            view.AccordionHeader([Accordion.Header], content: view =>
                            {
                                view.AccordionTrigger([Accordion.Trigger], content: view =>
                                {
                                    view.Text(text: "Is it styled?");
                                    view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                                });
                            });
                            view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                            {
                                view.Text([Text.Body], "Yes. It comes with default styles using the Standard theme.");
                            });
                        });

                        view.AccordionItem([Accordion.Item], value: "item-3", content: view =>
                        {
                            view.AccordionHeader([Accordion.Header], content: view =>
                            {
                                view.AccordionTrigger([Accordion.Trigger], content: view =>
                                {
                                    view.Text(text: "Is it animated?");
                                    view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                                });
                            });
                            view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                            {
                                view.Text([Text.Body], "Yes. It uses CSS transitions for smooth animations.");
                            });
                        });
                    });
            });

            // Accordion Multiple
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Accordion (Multiple)");
                view.Text([Text.Caption, "mb-4"], "Multiple items can be open at the same time");

                view.AccordionMultiple([Accordion.Root],
                    value: _accordionMultipleValues.Value,
                    onValueChange: async v => _accordionMultipleValues.Value = v, content: view =>
                    {
                        view.AccordionItem([Accordion.Item], value: "multi-1", content: view =>
                        {
                            view.AccordionHeader([Accordion.Header], content: view =>
                            {
                                view.AccordionTrigger([Accordion.Trigger], content: view =>
                                {
                                    view.Text(text: "Section One");
                                    view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                                });
                            });
                            view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                            {
                                view.Text([Text.Body], "Content for section one.");
                            });
                        });

                        view.AccordionItem([Accordion.Item], value: "multi-2", content: view =>
                        {
                            view.AccordionHeader([Accordion.Header], content: view =>
                            {
                                view.AccordionTrigger([Accordion.Trigger], content: view =>
                                {
                                    view.Text(text: "Section Two");
                                    view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                                });
                            });
                            view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                            {
                                view.Text([Text.Body], "Content for section two.");
                            });
                        });
                    });
            });

            // Accordion with Disabled Items
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Accordion - Disabled Items");
                view.Text([Text.Caption, "mb-4"], "Some accordion items can be disabled");

                view.AccordionSingle([Accordion.Root], collapsible: true, content: view =>
                {
                    view.AccordionItem([Accordion.Item], value: "enabled-1", content: view =>
                    {
                        view.AccordionHeader([Accordion.Header], content: view =>
                        {
                            view.AccordionTrigger([Accordion.Trigger], content: view =>
                            {
                                view.Text(text: "Enabled Section 1");
                                view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                            });
                        });
                        view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                        {
                            view.Text([Text.Body], "This section is enabled and can be expanded.");
                        });
                    });

                    view.AccordionItem([Accordion.Item, "opacity-50"], value: "disabled-1", disabled: true, content: view =>
                    {
                        view.AccordionHeader([Accordion.Header], content: view =>
                        {
                            view.AccordionTrigger([Accordion.Trigger], content: view =>
                            {
                                view.Text(text: "Disabled Section");
                                view.Icon(["ml-auto"], name: "lock");
                            });
                        });
                        view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                        {
                            view.Text([Text.Body], "This content cannot be accessed.");
                        });
                    });

                    view.AccordionItem([Accordion.Item], value: "enabled-2", content: view =>
                    {
                        view.AccordionHeader([Accordion.Header], content: view =>
                        {
                            view.AccordionTrigger([Accordion.Trigger], content: view =>
                            {
                                view.Text(text: "Enabled Section 2");
                                view.Icon([Accordion.ChevronIcon], name: "chevron-down");
                            });
                        });
                        view.AccordionContent([Accordion.Content, "pb-4"], content: view =>
                        {
                            view.Text([Text.Body], "This section is also enabled.");
                        });
                    });
                });
            });

            // Collapsible
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Collapsible");
                view.Text([Text.Caption, "mb-4"], "A component that can be expanded or collapsed");

                view.Collapsible([Collapsible.Root], open: _collapsibleOpen.Value,
                    onOpenChange: async open => _collapsibleOpen.Value = open,
                    content: view =>
                    {
                        view.Row([Layout.Row.Sm, "items-center justify-between"], content: view =>
                        {
                            view.Text([Text.BodyStrong], "Collapsible Section");
                            view.CollapsibleTrigger(content: view =>
                            {
                                view.Button([Button.GhostMd, Button.Size.Icon],
                                    content: v => v.Icon(name: _collapsibleOpen.Value ? "chevron-up" : "chevron-down"));
                            });
                        });
                        view.CollapsibleContent([Collapsible.Content, "mt-2"], content: view =>
                        {
                            view.Box([Card.Elevated, "p-4"], content: view =>
                            {
                                view.Text([Text.Body], "This content is revealed when the collapsible is expanded.");
                            });
                        });
                    });
            });

            // Menubar
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Menubar");
                view.Text([Text.Caption, "mb-4"], "A horizontal menu bar with dropdown menus");

                view.Menubar([Menubar.Root], content: view =>
                {
                    view.MenubarMenu(content: view =>
                    {
                        view.MenubarTrigger([Menubar.Trigger], content: v => v.Text(text: "File"));
                        view.MenubarContent([Menubar.Content], content: view =>
                        {
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "New File"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Open..."));
                            view.MenubarSeparator([Menubar.Separator]);
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Save"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Save As..."));
                        });
                    });

                    view.MenubarMenu(content: view =>
                    {
                        view.MenubarTrigger([Menubar.Trigger], content: v => v.Text(text: "Edit"));
                        view.MenubarContent([Menubar.Content], content: view =>
                        {
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Undo"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Redo"));
                            view.MenubarSeparator([Menubar.Separator]);
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Cut"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Copy"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Paste"));
                        });
                    });

                    view.MenubarMenu(content: view =>
                    {
                        view.MenubarTrigger([Menubar.Trigger], content: v => v.Text(text: "View"));
                        view.MenubarContent([Menubar.Content], content: view =>
                        {
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Zoom In"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Zoom Out"));
                            view.MenubarSeparator([Menubar.Separator]);
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Full Screen"));
                        });
                    });
                });
            });

            // Menubar with Submenus
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Menubar - Submenus");
                view.Text([Text.Caption, "mb-4"], "Menubar with nested submenu items");

                view.Menubar([Menubar.Root], content: view =>
                {
                    view.MenubarMenu(content: view =>
                    {
                        view.MenubarTrigger([Menubar.Trigger], content: v => v.Text(text: "File"));
                        view.MenubarContent([Menubar.Content], content: view =>
                        {
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "New File"));
                            view.MenubarSub(content: view =>
                            {
                                view.MenubarSubTrigger([Menubar.Item], content: view =>
                                {
                                    view.Text(text: "Open Recent");
                                    view.Icon(["ml-auto h-4 w-4"], name: "chevron-right");
                                });
                                view.MenubarSubContent([Menubar.Content], content: view =>
                                {
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "project.json"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "styles.css"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "index.html"));
                                    view.MenubarSeparator([Menubar.Separator]);
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Clear Recent"));
                                });
                            });
                            view.MenubarSeparator([Menubar.Separator]);
                            view.MenubarSub(content: view =>
                            {
                                view.MenubarSubTrigger([Menubar.Item], content: view =>
                                {
                                    view.Text(text: "Export As");
                                    view.Icon(["ml-auto h-4 w-4"], name: "chevron-right");
                                });
                                view.MenubarSubContent([Menubar.Content], content: view =>
                                {
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "PDF"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "PNG"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "SVG"));
                                });
                            });
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Save"));
                        });
                    });

                    view.MenubarMenu(content: view =>
                    {
                        view.MenubarTrigger([Menubar.Trigger], content: v => v.Text(text: "Edit"));
                        view.MenubarContent([Menubar.Content], content: view =>
                        {
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Undo"));
                            view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Redo"));
                            view.MenubarSeparator([Menubar.Separator]);
                            view.MenubarSub(content: view =>
                            {
                                view.MenubarSubTrigger([Menubar.Item], content: view =>
                                {
                                    view.Text(text: "Find and Replace");
                                    view.Icon(["ml-auto h-4 w-4"], name: "chevron-right");
                                });
                                view.MenubarSubContent([Menubar.Content], content: view =>
                                {
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Find"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Replace"));
                                    view.MenubarItem([Menubar.Item], content: v => v.Text(text: "Find in Files"));
                                });
                            });
                        });
                    });
                });
            });

            // Toolbar
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Toolbar");
                view.Text([Text.Caption, "mb-4"], "A container for grouping buttons and controls");

                view.Toolbar([Toolbar.Root], content: view =>
                {
                    view.ToolbarToggleGroupSingle([Toolbar.ToggleGroup], content: view =>
                    {
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "left", content: v => v.Icon(name: "text-align-left"));
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "center", content: v => v.Icon(name: "text-align-center"));
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "right", content: v => v.Icon(name: "text-align-right"));
                    });

                    view.ToolbarSeparator([Toolbar.Separator]);

                    view.ToolbarToggleGroupMultiple([Toolbar.ToggleGroup], content: view =>
                    {
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "bold", content: v => v.Icon(name: "font-bold"));
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "italic", content: v => v.Icon(name: "font-italic"));
                        view.ToolbarToggleItem([Toolbar.ToggleItem], value: "underline", content: v => v.Icon(name: "underline"));
                    });

                    view.ToolbarSeparator([Toolbar.Separator]);

                    view.ToolbarButton([Toolbar.Button], content: v => v.Icon(name: "link-2"));
                });
            });

            // Breadcrumb styles (static)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Breadcrumb Styles");
                view.Text([Text.Caption, "mb-4"], "Breadcrumb navigation styles (static)");

                view.Box([Breadcrumb.Root], content: view =>
                {
                    view.Box([Breadcrumb.List], content: view =>
                    {
                        view.Box([Breadcrumb.Item], content: view =>
                        {
                            view.Text([Breadcrumb.Link], "Home");
                        });
                        view.Text([Breadcrumb.Separator], "/");
                        view.Box([Breadcrumb.Item], content: view =>
                        {
                            view.Text([Breadcrumb.Link], "Products");
                        });
                        view.Text([Breadcrumb.Separator], "/");
                        view.Box([Breadcrumb.Item], content: view =>
                        {
                            view.Text([Breadcrumb.Ellipsis], "...");
                        });
                        view.Text([Breadcrumb.Separator], "/");
                        view.Box([Breadcrumb.Item], content: view =>
                        {
                            view.Text([Breadcrumb.Page], "Current Page");
                        });
                    });
                });
            });

            // Breadcrumb - Interactive
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Breadcrumb - Interactive");
                view.Text([Text.Caption, "mb-4"], "Clickable breadcrumb navigation with state management");

                view.Text([Text.Muted, "mb-2"], $"Current path: {string.Join(" / ", _breadcrumbPath.Value)}");

                view.Box([Breadcrumb.Root], content: view =>
                {
                    view.Box([Breadcrumb.List], content: view =>
                    {
                        for (var i = 0; i < _breadcrumbPath.Value.Count; i++)
                        {
                            var index = i;
                            var segment = _breadcrumbPath.Value[i];
                            var isLast = i == _breadcrumbPath.Value.Count - 1;

                            if (i > 0)
                            {
                                view.Text([Breadcrumb.Separator], "/");
                            }

                            view.Box([Breadcrumb.Item], content: view =>
                            {
                                if (isLast)
                                {
                                    view.Text([Breadcrumb.Page], segment);
                                }
                                else
                                {
                                    view.Button([Breadcrumb.Link, "cursor-pointer hover:underline"],
                                        label: segment,
                                        onClick: async () =>
                                        {
                                            _breadcrumbPath.Value = _breadcrumbPath.Value.Take(index + 1).ToList();
                                        });
                                }
                            });
                        }
                    });
                });

                view.Row([Layout.Row.Sm, "mt-4"], content: view =>
                {
                    view.Button([Button.OutlineSm], label: "Add Segment", onClick: async () =>
                    {
                        var newSegment = $"Folder{_breadcrumbPath.Value.Count}";
                        _breadcrumbPath.Value = [.. _breadcrumbPath.Value, newSegment];
                    });
                    view.Button([Button.GhostMd], label: "Reset", onClick: async () =>
                    {
                        _breadcrumbPath.Value = ["Home", "Products", "Electronics"];
                    });
                });
            });

            // Pagination styles (static)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Pagination Styles");
                view.Text([Text.Caption, "mb-4"], "Pagination component styles (static)");

                view.Box([Pagination.Root], content: view =>
                {
                    view.Box([Pagination.List], content: view =>
                    {
                        view.Box([Pagination.Previous], content: v => v.Icon(name: "chevron-left"));
                        view.Box([Pagination.Item], content: v => v.Text(text: "1"));
                        view.Box([Pagination.Item], content: v => v.Text(text: "2"));
                        view.Box([Pagination.Active], content: v => v.Text(text: "3"));
                        view.Box([Pagination.Item], content: v => v.Text(text: "4"));
                        view.Box([Pagination.Ellipsis], content: v => v.Text(text: "..."));
                        view.Box([Pagination.Item], content: v => v.Text(text: "10"));
                        view.Box([Pagination.Next], content: v => v.Icon(name: "chevron-right"));
                    });
                });
            });

            // Pagination - Interactive
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Pagination - Interactive");
                view.Text([Text.Caption, "mb-4"], "Pagination with page change handlers");

                const int totalPages = 10;
                var currentPage = _paginationPage.Value;

                view.Text([Text.Muted, "mb-2"], $"Current page: {currentPage} of {totalPages}");

                view.Box([Pagination.Root], content: view =>
                {
                    view.Box([Pagination.List], content: view =>
                    {
                        view.Button([currentPage <= 1 ? Pagination.Disabled : Pagination.Previous],
                            disabled: currentPage <= 1,
                            onClick: async () => _paginationPage.Value = Math.Max(1, currentPage - 1),
                            content: v => v.Icon(name: "chevron-left"));

                        for (var page = 1; page <= totalPages; page++)
                        {
                            var p = page;

                            if (page == 1 || page == totalPages || Math.Abs(page - currentPage) <= 1)
                            {
                                view.Button([page == currentPage ? Pagination.Active : Pagination.Item],
                                    onClick: async () => _paginationPage.Value = p,
                                    content: v => v.Text(text: p.ToString()));
                            }
                            else if (page == 2 && currentPage > 4)
                            {
                                view.Box([Pagination.Ellipsis], content: v => v.Text(text: "..."));
                            }
                            else if (page == totalPages - 1 && currentPage < totalPages - 3)
                            {
                                view.Box([Pagination.Ellipsis], content: v => v.Text(text: "..."));
                            }
                        }

                        view.Button([currentPage >= totalPages ? Pagination.Disabled : Pagination.Next],
                            disabled: currentPage >= totalPages,
                            onClick: async () => _paginationPage.Value = Math.Min(totalPages, currentPage + 1),
                            content: v => v.Icon(name: "chevron-right"));
                    });
                });
            });
        });
    }
}
