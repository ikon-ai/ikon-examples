public partial class Validation
{
    private void RenderLayoutSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Crosswind -> Flutter style mapping checks
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Crosswind to Flutter Mapping");
                view.Text([Text.Caption, "mb-4"], "Validates object-fit, flex-1, space-y, align-self, and dashed/dotted borders on the Flutter renderer.");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong], "object-fit (cover / contain / fill)");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        foreach (var (fit, label) in new[] { ("object-cover", "cover"), ("object-contain", "contain"), ("object-fill", "fill") })
                        {
                            view.Column([Layout.Column.Xs, "items-center"], content: view =>
                            {
                                view.Image(["w-28 h-16 rounded-md border border-secondary", fit],
                                    src: "https://picsum.photos/seed/objectfit/300/150", alt: label);
                                view.Text([Text.Caption], label);
                            });
                        }
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "flex-1 (children grow to fill the row)");
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Box(["flex-1 bg-surface p-3 rounded-md border border-secondary"], content: v => v.Text([Text.Caption], "flex-1"));
                        view.Box(["flex-1 bg-surface p-3 rounded-md border border-secondary"], content: v => v.Text([Text.Caption], "flex-1"));
                        view.Box(["bg-surface p-3 rounded-md border border-secondary"], content: v => v.Text([Text.Caption], "auto width"));
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "space-y-3 (gap between stacked children)");
                    view.Box(["space-y-3 bg-surface p-3 rounded-md border border-secondary"], content: view =>
                    {
                        view.Box(["bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "Item 1"));
                        view.Box(["bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "Item 2"));
                        view.Box(["bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "Item 3"));
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "align-self (start / center / end in a row)");
                    view.Row([Layout.Row.Md, "h-24 bg-surface p-2 rounded-md border border-secondary"], content: view =>
                    {
                        view.Box(["self-start bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "start"));
                        view.Box(["self-center bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "center"));
                        view.Box(["self-end bg-card p-2 rounded"], content: v => v.Text([Text.Caption], "end"));
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "border-style (dashed / dotted / solid)");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Box(["border-2 border-dashed border-secondary rounded-md p-4"], content: v => v.Text([Text.Caption], "dashed"));
                        view.Box(["border-2 border-dotted border-secondary rounded-md p-4"], content: v => v.Text([Text.Caption], "dotted"));
                        view.Box(["border-2 border-solid border-secondary rounded-md p-4"], content: v => v.Text([Text.Caption], "solid"));
                    });
                });
            });

            // Progress
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Progress");
                view.Text([Text.Caption, "mb-4"], "Progress bar with auto-rendered indicator");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Default variant");
                    view.Progress(value: _progressValue.Value, max: 100, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Success variant");
                    view.Progress(value: 80, max: 100, variant: Progress.Variant.Success, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Warning variant");
                    view.Progress(value: 50, max: 100, variant: Progress.Variant.Warning, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Error variant");
                    view.Progress(value: 30, max: 100, variant: Progress.Variant.Error, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Indeterminate");
                    view.Progress(indeterminate: true, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Indeterminate with success variant");
                    view.Progress(indeterminate: true, variant: Progress.Variant.Success, rootStyle: [Progress.Root]);

                    view.Text([Text.Caption], "Custom size (Size.Sm)");
                    view.Progress(value: 70, max: 100, variant: Progress.Variant.Default, rootStyle: [Progress.Base, Progress.Size.Sm]);
                });
            });

            // Avatar - sizes
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Avatar - Sizes");
                view.Row([Layout.Row.Md, "items-end flex-wrap"], content: view =>
                {
                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Xs, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "XS"));
                        });
                        view.Text([Text.Caption], "Xs");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Sm, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "SM"));
                        });
                        view.Text([Text.Caption], "Sm");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Root], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "MD"));
                        });
                        view.Text([Text.Caption], "Md");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Lg, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "LG"));
                        });
                        view.Text([Text.Caption], "Lg");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Xl, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "XL"));
                        });
                        view.Text([Text.Caption], "Xl");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Xl2, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Text(text: "2XL"));
                        });
                        view.Text([Text.Caption], "2Xl");
                    });
                });
            });

            // Avatar - shapes
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Avatar - Shapes");
                view.Row([Layout.Row.Lg], content: view =>
                {
                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Lg, Avatar.Shape.Circle], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Icon(name: "person"));
                        });
                        view.Text([Text.Caption], "Circle");
                    });

                    view.Column([Layout.Column.Xs, "items-center"], content: view =>
                    {
                        view.Avatar([Avatar.Base, Avatar.Size.Lg, Avatar.Shape.Square], content: view =>
                        {
                            view.AvatarFallback( [Avatar.Fallback], content: v => v.Icon(name: "person"));
                        });
                        view.Text([Text.Caption], "Square");
                    });
                });
            });

            // Separator
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Separator");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Horizontal (default)");
                    view.Separator( [Separator.Horizontal]);

                    view.Text([Text.Caption], "Horizontal subtle");
                    view.Separator( [Separator.Horizontal, Separator.Variant.Subtle]);

                    view.Text([Text.Caption], "Horizontal strong");
                    view.Separator( [Separator.Horizontal, Separator.Variant.Strong]);

                    view.Text([Text.Caption], "Vertical (in row)");
                    view.Row([Layout.Row.Md, "h-8 items-center"], content: view =>
                    {
                        view.Text([Text.Body], "Left");
                        view.Separator( [Separator.Vertical], orientation: Orientation.Vertical);
                        view.Text([Text.Body], "Right");
                    });
                });
            });

            // AspectRatio
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "AspectRatio");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "w-32"], content: view =>
                        {
                            view.AspectRatio( [AspectRatio.Base, AspectRatio.Ratio.Square], ratio: 1, content: view =>
                            {
                                view.Box([AspectRatio.PlaceholderContent], content: v => v.Text(text: "1:1"));
                            });
                            view.Text([Text.Caption], "Square");
                        });

                        view.Column([Layout.Column.Xs, "w-48"], content: view =>
                        {
                            view.AspectRatio( [AspectRatio.Base, AspectRatio.Ratio.Video], ratio: 16.0 / 9.0, content: view =>
                            {
                                view.Box([AspectRatio.PlaceholderContent], content: v => v.Text(text: "16:9"));
                            });
                            view.Text([Text.Caption], "Video");
                        });

                        view.Column([Layout.Column.Xs, "w-32"], content: view =>
                        {
                            view.AspectRatio( [AspectRatio.Base, AspectRatio.Ratio.Portrait], ratio: 3.0 / 4.0, content: view =>
                            {
                                view.Box([AspectRatio.PlaceholderContent], content: v => v.Text(text: "3:4"));
                            });
                            view.Text([Text.Caption], "Portrait");
                        });
                    });
                });
            });

            // ScrollArea
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "ScrollArea");
                view.Text([Text.Caption, "mb-4"], "Custom scrollable area with auto-rendered viewport and scrollbars");

                view.Column([Layout.Column.Lg], content: view =>
                {
                    view.Text([Text.BodyStrong], "Vertical scrolling (default)");
                    view.ScrollArea(scrollbars: ScrollAreaScrollbars.Vertical, rootStyle: [ScrollArea.Bordered, "h-32 w-full"], content: view =>
                    {
                        view.Column(["p-4"], content: view =>
                        {
                            for (var i = 1; i <= 15; i++)
                            {
                                var index = i;
                                view.Text([Text.Body], $"Vertical item {index}");
                            }
                        });
                    });

                    view.Text([Text.BodyStrong], "Horizontal scrolling");
                    view.ScrollArea(scrollbars: ScrollAreaScrollbars.Horizontal, rootStyle: [ScrollArea.Bordered, "w-full"], content: view =>
                    {
                        view.Row(["p-4 w-max gap-4"], content: view =>
                        {
                            for (var i = 1; i <= 10; i++)
                            {
                                var index = i;
                                view.Box([Card.Elevated, "p-4 min-w-[120px]"], content: v =>
                                {
                                    v.Text([Text.BodyStrong], $"Card {index}");
                                });
                            }
                        });
                    });

                    view.Text([Text.BodyStrong], "Both directions");
                    view.ScrollArea(scrollbars: ScrollAreaScrollbars.Both, rootStyle: [ScrollArea.Bordered, "h-48 w-full"], content: view =>
                    {
                        view.Box(["p-4 w-[800px]"], content: view =>
                        {
                            for (var i = 1; i <= 20; i++)
                            {
                                var index = i;
                                view.Text([Text.Body, "whitespace-nowrap"], $"Row {index}: This is a long line of text that extends beyond the container width to demonstrate horizontal scrolling along with vertical scrolling.");
                            }
                        });
                    });
                });
            });

            // InfiniteScrollView
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "InfiniteScrollView");
                view.Text([Text.Caption, "mb-4"], "Lazy-loading scroll area that fires callbacks when user scrolls near the end");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "items-center mb-4"], content: view =>
                    {
                        view.Text([Text.Body], $"Items loaded: {_infiniteScrollItems.Value.Count}");
                        view.Button([Button.NeutralMd], label: "Reset", onClick: async () =>
                        {
                            _infiniteScrollItems.Value = [];
                            _infiniteScrollPage = 0;
                            _infiniteScrollHasMore.Value = true;
                        });
                    });

                    view.InfiniteScrollView(
                        rootStyle: [ScrollArea.Bordered, "h-64 w-full"],
                        threshold: 100,
                        debounceMs: 150,
                        loading: _infiniteScrollLoading.Value,
                        hasMore: _infiniteScrollHasMore.Value,
                        onNearEnd: async args =>
                        {
                            if (_infiniteScrollLoading.Value)
                            {
                                return;
                            }

                            _infiniteScrollLoading.Value = true;

                            await Task.Delay(500);

                            _infiniteScrollPage++;
                            var newItems = new List<string>();
                            var startIndex = (_infiniteScrollPage - 1) * 10 + 1;

                            for (var i = 0; i < 10; i++)
                            {
                                newItems.Add($"Item {startIndex + i} (loaded in batch {_infiniteScrollPage})");
                            }

                            _infiniteScrollItems.Value = [.._infiniteScrollItems.Value, ..newItems];

                            if (_infiniteScrollPage >= 5)
                            {
                                _infiniteScrollHasMore.Value = false;
                            }

                            _infiniteScrollLoading.Value = false;
                        },
                        loadingIndicator: v =>
                        {
                            v.Row(["flex justify-center py-4 gap-2"], content: row =>
                            {
                                row.Box([Skeleton.AvatarSm]);
                                row.Text([Text.Caption], "Loading more items...");
                            });
                        },
                        content: v =>
                        {
                            v.Column(["p-4"], content: col =>
                            {
                                foreach (var item in _infiniteScrollItems.Value)
                                {
                                    col.Box([Card.Elevated, "p-3 mb-2"], key: item, content: box =>
                                    {
                                        box.Text([Text.Body], item);
                                    });
                                }

                                if (!_infiniteScrollHasMore.Value)
                                {
                                    col.Text([Text.Caption, "text-center py-4 text-muted-foreground"], "No more items to load");
                                }
                            });
                        });

                    view.Text([Text.Caption, "mt-2"], _infiniteScrollHasMore.Value
                        ? "Scroll to the bottom to load more items..."
                        : "All 50 items loaded!");
                });
            });

            // Auto-scroll test (Polite vs Assertive)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Auto-Scroll Test");
                view.Text([Text.Caption, "mb-4"], "Items added every 3 seconds. Compare Polite vs Assertive scroll behaviors.");

                view.Row([Layout.Row.Md, "items-center mb-4"], content: view =>
                {
                    var isRunning = _autoScrollCts != null;
                    view.Button([isRunning ? Button.NeutralMd : Button.PrimaryMd],
                        label: isRunning ? "Stop" : "Start",
                        onClick: async () =>
                        {
                            if (_autoScrollCts != null)
                            {
                                _autoScrollCts.Cancel();
                                _autoScrollCts = null;
                            }
                            else
                            {
                                _autoScrollCts = new CancellationTokenSource();
                                _ = RunAutoScrollTestAsync(_autoScrollCts.Token);
                            }
                        });
                    view.Button([Button.OutlineMd], label: "Clear", onClick: async () =>
                    {
                        _autoScrollPoliteItems.Value = [];
                        _autoScrollAssertiveItems.Value = [];
                    });
                    view.Text([Text.Caption], $"Items: {_autoScrollPoliteItems.Value.Count}");
                });

                view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
                {
                    view.Column([Layout.Column.Md, "flex-1 min-w-0"], content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Polite (smart auto-scroll)");
                        view.Text([Text.Caption, "mb-2"], "Scroll up → shows ↓ indicator. Click it to jump back.");
                        view.ScrollArea(
                            rootStyle: [ScrollArea.Bordered, "h-48 w-full"],
                            autoScroll: true,
                            autoScrollKey: _autoScrollPoliteItems.Value.Count.ToString(),
                            content: view =>
                            {
                                view.Column(["p-4"], content: view =>
                                {
                                    foreach (var item in _autoScrollPoliteItems.Value)
                                    {
                                        view.Box([Card.Elevated, "p-2 mb-1"], key: item, content: v =>
                                        {
                                            v.Text([Text.Body], item);
                                        });
                                    }
                                });
                            });
                    });

                    view.Column([Layout.Column.Md, "flex-1 min-w-0"], content: view =>
                    {
                        view.Text([Text.BodyStrong, "mb-2"], "Assertive (forced scroll)");
                        view.Text([Text.Caption, "mb-2"], "Always scrolls to bottom, even if you scrolled away.");
                        view.ScrollArea(
                            rootStyle: [ScrollArea.Bordered, "h-48 w-full"],
                            content: view =>
                            {
                                view.Column(["p-4"], content: view =>
                                {
                                    foreach (var item in _autoScrollAssertiveItems.Value)
                                    {
                                        view.Box([Card.Elevated, "p-2 mb-1"], key: item, content: v =>
                                        {
                                            v.Text([Text.Body], item);
                                        });
                                    }

                                    view.Box(["h-1"], content: anchor =>
                                    {
                                        anchor.FocusHint(new FocusHintProps
                                            {
                                                Priority = FocusPriority.Assertive,
                                                Cooldown = TimeSpan.FromMilliseconds(50)
                                            },
                                            key: $"assertive-scroll-{_autoScrollAssertiveItems.Value.Count}");
                                    });
                                });
                            });
                    });
                });
            });

            // Image component
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Image");
                view.Text([Text.Caption, "mb-4"], "Display images from URLs or binary data");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong], "URL-based Image");
                    view.Row([Layout.Row.Md, "flex-wrap items-end"], content: view =>
                    {
                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-24 h-24 rounded-md object-cover"],
                                src: "https://picsum.photos/seed/small/200/200",
                                alt: "Small placeholder image");
                            view.Text([Text.Caption], "Small (96px)");
                        });

                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-32 h-32 rounded-md object-cover"],
                                src: "https://picsum.photos/seed/medium/200/200",
                                alt: "Medium placeholder image");
                            view.Text([Text.Caption], "Medium (128px)");
                        });

                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-48 h-48 rounded-lg object-cover"],
                                src: "https://picsum.photos/seed/large/300/300",
                                alt: "Large placeholder image");
                            view.Text([Text.Caption], "Large (192px)");
                        });
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "Image Shapes");
                    view.Row([Layout.Row.Md, "flex-wrap items-end"], content: view =>
                    {
                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-24 h-24 rounded-full object-cover"],
                                src: "https://picsum.photos/seed/circle/200/200",
                                alt: "Circular image");
                            view.Text([Text.Caption], "Circle");
                        });

                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-24 h-24 rounded-none object-cover"],
                                src: "https://picsum.photos/seed/square/200/200",
                                alt: "Square image");
                            view.Text([Text.Caption], "Square");
                        });

                        view.Column([Layout.Column.Xs], content: view =>
                        {
                            view.Image(["w-24 h-24 rounded-xl object-cover"],
                                src: "https://picsum.photos/seed/rounded/200/200",
                                alt: "Rounded image");
                            view.Text([Text.Caption], "Rounded XL");
                        });
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "Aspect Ratio with Image");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "w-48"], content: view =>
                        {
                            view.AspectRatio([AspectRatio.Base], ratio: 16.0 / 9.0, content: view =>
                            {
                                view.Image(["w-full h-full object-cover rounded-md"],
                                    src: "https://picsum.photos/seed/video/400/225",
                                    alt: "16:9 landscape image");
                            });
                            view.Text([Text.Caption], "16:9");
                        });

                        view.Column([Layout.Column.Xs, "w-32"], content: view =>
                        {
                            view.AspectRatio([AspectRatio.Base], ratio: 1, content: view =>
                            {
                                view.Image(["w-full h-full object-cover rounded-md"],
                                    src: "https://picsum.photos/seed/square2/200/200",
                                    alt: "1:1 square image");
                            });
                            view.Text([Text.Caption], "1:1");
                        });

                        view.Column([Layout.Column.Xs, "w-24"], content: view =>
                        {
                            view.AspectRatio([AspectRatio.Base], ratio: 3.0 / 4.0, content: view =>
                            {
                                view.Image(["w-full h-full object-cover rounded-md"],
                                    src: "https://picsum.photos/seed/portrait/150/200",
                                    alt: "3:4 portrait image");
                            });
                            view.Text([Text.Caption], "3:4");
                        });
                    });
                });
            });

            // DirectionProvider
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "DirectionProvider");
                view.Text([Text.Caption, "mb-4"], "Provides text direction context (LTR/RTL) to descendants");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong], "Left-to-Right (LTR) - Default");
                    view.DirectionProvider(dir: Dir.Ltr, content: view =>
                    {
                        view.Box(["border border-secondary rounded-md p-4"], content: view =>
                        {
                            view.Text([Text.Body], "This text flows left-to-right.");
                            view.Row(["gap-2 mt-2"], content: view =>
                            {
                                view.Button([Button.PrimaryMd], label: "First");
                                view.Button([Button.NeutralMd], label: "Second");
                                view.Button([Button.OutlineMd], label: "Third");
                            });
                        });
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "Right-to-Left (RTL)");
                    view.DirectionProvider(dir: Dir.Rtl, content: view =>
                    {
                        view.Box(["border border-secondary rounded-md p-4"], content: view =>
                        {
                            view.Text([Text.Body], "This text flows right-to-left. Components respect RTL direction.");
                            view.Row(["gap-2 mt-2"], content: view =>
                            {
                                view.Button([Button.PrimaryMd], label: "First");
                                view.Button([Button.NeutralMd], label: "Second");
                                view.Button([Button.OutlineMd], label: "Third");
                            });
                        });
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "RTL Example with Icons");
                    view.DirectionProvider(dir: Dir.Rtl, content: view =>
                    {
                        view.Box(["border border-secondary rounded-md p-4"], content: view =>
                        {
                            view.Row(["gap-4 items-center"], content: view =>
                            {
                                view.Icon([Icon.Default], name: "arrow-right");
                                view.Text([Text.Body], "Arrow points in text direction");
                            });
                            view.Row(["gap-4 items-center mt-2"], content: view =>
                            {
                                view.Icon([Icon.Default], name: "text-align-left");
                                view.Text([Text.Body], "Alignment follows direction");
                            });
                        });
                    });

                    view.Text([Text.BodyStrong, "mt-4"], "Nested Direction Contexts");
                    view.DirectionProvider(dir: Dir.Rtl, content: view =>
                    {
                        view.Box(["border border-blue-500 rounded-md p-4"], content: view =>
                        {
                            view.Text([Text.Caption, "text-blue-500"], "RTL Context");
                            view.Text([Text.Body], "Parent is RTL");

                            view.DirectionProvider(["mt-2"], dir: Dir.Ltr, content: view =>
                            {
                                view.Box(["border border-green-500 rounded-md p-4"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-green-500"], "LTR Context (nested)");
                                    view.Text([Text.Body], "This child overrides to LTR");
                                });
                            });
                        });
                    });
                });
            });

            // Skeleton loading states
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Skeleton (Loading States)");
                view.Column([Layout.Column.Lg], content: view =>
                {
                    view.Text([Text.Caption], "Text skeletons");
                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Box([Skeleton.TextLg]);
                        view.Box([Skeleton.Text]);
                        view.Box([Skeleton.TextSm]);
                    });

                    view.Text([Text.Caption], "Avatar skeletons");
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Box([Skeleton.AvatarSm]);
                        view.Box([Skeleton.Avatar]);
                        view.Box([Skeleton.AvatarLg]);
                    });

                    view.Text([Text.Caption], "Component skeletons");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Box([Skeleton.Button]);
                        view.Box([Skeleton.Input]);
                    });

                    view.Text([Text.Caption], "Card skeleton");
                    view.Box([Skeleton.Card]);
                });
            });

            // Split Layout
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Split Layout");
                view.Text([Text.BodySm, "text-tertiary mb-6"], "Structural patterns for sidebar, master-detail, and three-column layouts.");

                view.Column([Layout.Column.Xl], content: view =>
                {
                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Sidebar + Main");
                        view.Text([Text.Caption, "mb-3"], "Classic nav layout.");
                        view.Box(["h-40 rounded-lg overflow-hidden border border-secondary flex flex-row min-h-0"], content: view =>
                        {
                            view.Box([Layout.Split.Sidebar, "bg-surface border-r border-secondary p-2"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-3"], "Sidebar");
                                view.Column(["gap-0.5"], content: view =>
                                {
                                    view.Box([NavItem.Sm, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "layout-dashboard"); view.Text([NavItem.Label], "Dashboard"); });
                                    view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                    view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                                });
                            });
                            view.Box([Layout.Split.Main, "bg-card p-4"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Main content");
                                view.Text([Text.Small, "text-quaternary"], "flex-1 min-w-0");
                            });
                        });
                    });

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Main + Detail");
                        view.Text([Text.Caption, "mb-3"], "Master-detail / inspector layout.");
                        view.Box(["h-40 rounded-lg overflow-hidden border border-secondary flex flex-row min-h-0"], content: view =>
                        {
                            view.Box([Layout.Split.Main, "bg-card border-r border-secondary p-4"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Main content");
                                view.Text([Text.Small, "text-quaternary"], "flex-1 min-w-0");
                            });
                            view.Box([Layout.Split.Detail, "bg-surface p-3"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Detail panel");
                                view.Text([Text.Small, "text-quaternary"], "w-72 shrink-0");
                            });
                        });
                    });

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Three-Column");
                        view.Text([Text.Caption, "mb-3"], "Full IDE / dashboard layout.");
                        view.Box(["h-40 rounded-lg overflow-hidden border border-secondary flex flex-row min-h-0"], content: view =>
                        {
                            view.Box([Layout.Split.SidebarSm, "bg-surface border-r border-secondary p-2"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Sidebar");
                                view.Text([Text.Small, "text-quaternary"], "w-48");
                            });
                            view.Box([Layout.Split.Main, "bg-card border-r border-secondary p-4"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Main");
                                view.Text([Text.Small, "text-quaternary"], "flex-1");
                            });
                            view.Box([Layout.Split.Detail, "bg-surface p-3"], content: view =>
                            {
                                view.Text([Text.Caption, "text-tertiary mb-1"], "Detail");
                                view.Text([Text.Small, "text-quaternary"], "w-72");
                            });
                        });
                    });

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Collapsible Sidebar");
                        view.Text([Text.Caption, "mb-3"], "Toggle button sits at the boundary.");
                        view.Box(["h-40 rounded-lg overflow-hidden border border-secondary flex flex-row min-h-0"], content: view =>
                        {
                            if (_splitSidebarOpen.Value)
                            {
                                view.Box([Layout.Split.Sidebar, "bg-surface border-r border-secondary p-2"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-tertiary mb-3"], "Sidebar");
                                    view.Column(["gap-0.5"], content: view =>
                                    {
                                        view.Box([NavItem.Sm, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "layout-dashboard"); view.Text([NavItem.Label], "Dashboard"); });
                                        view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                        view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                                    });
                                });
                            }

                            view.Box([Layout.Split.Main, "bg-card p-2"], content: view =>
                            {
                                view.Button([Button.GhostSm, Button.Icon], onClick: async () =>
                                {
                                    _splitSidebarOpen.Value = !_splitSidebarOpen.Value;
                                }, content: v => v.Icon([Icon.Xs], name: _splitSidebarOpen.Value ? "panel-left-close" : "panel-left-open"));
                            });
                        });
                    });
                });
            });

            // Resizable Split
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Resizable Split");
                view.Text([Text.BodySm, "text-tertiary mb-6"], "Drag the handle to resize. The resize is handled entirely on the client.");

                view.Column([Layout.Column.Xl], content: view =>
                {
                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Horizontal");
                        view.Text([Text.Caption, "mb-3"], $"Sidebar width: {_resizableSplitSize.Value:0}px");
                        view.Box(["h-48 rounded-lg overflow-hidden border border-secondary"], content: view =>
                        {
                            view.ResizableSplit(
                                orientation: Orientation.Horizontal,
                                initialSize: 224,
                                minSize: 120,
                                maxSize: 400,
                                onResized: async size => _resizableSplitSize.Value = size,
                                style: [ResizableSplit.Root, "h-full"],
                                first: view =>
                                {
                                    view.Box(["bg-surface p-3 h-full"], content: view =>
                                    {
                                        view.Text([Text.Caption, "text-tertiary mb-2"], "Sidebar");
                                        view.Column(["gap-0.5"], content: view =>
                                        {
                                            view.Box([NavItem.Sm, NavItem.Active], content: view => { view.Icon([NavItem.Icon], name: "layout-dashboard"); view.Text([NavItem.Label], "Dashboard"); });
                                            view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "bar-chart-2"); view.Text([NavItem.Label], "Analytics"); });
                                            view.Box([NavItem.Sm, NavItem.Default], content: view => { view.Icon([NavItem.Icon], name: "settings"); view.Text([NavItem.Label], "Settings"); });
                                        });
                                    });
                                },
                                second: view =>
                                {
                                    view.Box(["bg-card p-4 h-full"], content: view =>
                                    {
                                        view.Text([Text.Caption, "text-tertiary"], "Main content area");
                                    });
                                });
                        });
                    });

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Vertical");
                        view.Text([Text.Caption, "mb-3"], "Top/bottom split with vertical handle.");
                        view.Box(["h-64 rounded-lg overflow-hidden border border-secondary"], content: view =>
                        {
                            view.ResizableSplit(
                                orientation: Orientation.Vertical,
                                initialSize: 120,
                                minSize: 60,
                                maxSize: 200,
                                style: [ResizableSplit.Root, "h-full"],
                                first: view =>
                                {
                                    view.Box(["bg-surface p-3 h-full"], content: view =>
                                    {
                                        view.Text([Text.Caption, "text-tertiary"], "Top pane");
                                    });
                                },
                                second: view =>
                                {
                                    view.Box(["bg-card p-3 h-full"], content: view =>
                                    {
                                        view.Text([Text.Caption, "text-tertiary"], "Bottom pane");
                                    });
                                });
                        });
                    });

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.H3], "Nested");
                        view.Text([Text.Caption, "mb-3"], $"Outer {(int)_resizableNestedOuterSize.Value}px, inner {(int)_resizableNestedInnerSize.Value}px");
                        view.Box(["h-96 rounded-lg overflow-hidden border border-secondary"], content: view =>
                        {
                            view.ResizableSplit(
                                orientation: Orientation.Horizontal,
                                initialSize: _resizableNestedOuterSize.Value,
                                minSize: 200,
                                maxSize: 600,
                                onResized: async w => _resizableNestedOuterSize.Value = w,
                                style: [ResizableSplit.Root, "h-full"],
                                first: view =>
                                {
                                    view.Box(["bg-surface p-3 h-full"], content: view =>
                                    {
                                        view.Text([Text.Caption, "text-tertiary"], "Outer first");
                                    });
                                },
                                second: outer =>
                                {
                                    outer.ResizableSplit(
                                        orientation: Orientation.Vertical,
                                        initialSize: _resizableNestedInnerSize.Value,
                                        minSize: 60,
                                        maxSize: 320,
                                        onResized: async h => _resizableNestedInnerSize.Value = h,
                                        style: [ResizableSplit.Root, "h-full"],
                                        first: view =>
                                        {
                                            view.Box(["bg-card p-3 h-full"], content: view =>
                                            {
                                                view.Text([Text.Caption, "text-tertiary"], "Inner top");
                                            });
                                        },
                                        second: view =>
                                        {
                                            view.Box(["bg-surface p-3 h-full"], content: view =>
                                            {
                                                view.Text([Text.Caption, "text-tertiary"], "Inner bottom");
                                            });
                                        });
                                });
                        });
                    });
                });
            });

            // Box onClick test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Box onClick");
                view.Text([Text.Body, "mb-4"], "Click the colored box below");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box(["w-32 h-32 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center"],
                        onClick: async () =>
                        {
                            _boxClickCount.Value++;
                            _boxClickStatus.Value = $"Clicked {_boxClickCount.Value} time(s) at {DateTime.Now:HH:mm:ss}";
                        },
                        content: view =>
                        {
                            view.Text(["text-white font-medium"], "Click me");
                        });
                    view.Text([Text.Caption, "mt-2"], $"Status: {_boxClickStatus.Value}");
                });
            });
        });
    }

    private async Task RunAutoScrollTestAsync(CancellationToken ct)
    {
        var counter = _autoScrollPoliteItems.Value.Count;

        AddAutoScrollItem(ref counter);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            AddAutoScrollItem(ref counter);
        }
    }

    private void AddAutoScrollItem(ref int counter)
    {
        counter++;
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var text = $"Item {counter} — {timestamp}";
        _autoScrollPoliteItems.Value = [.._autoScrollPoliteItems.Value, text];
        _autoScrollAssertiveItems.Value = [.._autoScrollAssertiveItems.Value, text];
    }
}
