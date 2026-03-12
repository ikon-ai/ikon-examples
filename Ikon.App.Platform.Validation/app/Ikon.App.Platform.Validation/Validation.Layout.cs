public partial class Validation
{
    private void RenderLayoutSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
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
                        view.Button([Button.SecondaryMd], label: "Reset", onClick: async () =>
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
                                view.Button([Button.SecondaryMd], label: "Second");
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
                                view.Button([Button.SecondaryMd], label: "Second");
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
}
