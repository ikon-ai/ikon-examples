<!-- mined-from: Ikon.App.Patterns -->
# Skeleton While Loading — Standing In For The Shape, Not The Wait

A skeleton is not a spinner with square corners. It stands in for the **shape** of what is
arriving, so it mirrors the real row — same container, same gaps, a circle where the avatar goes.
That is what keeps the layout from jumping when the content lands, which is the whole reason to
prefer it over a spinner for content that has a known structure.

## When to use

A list, a card grid, a profile, a table — anything whose layout you already know before the data
arrives. Use a `Spinner` instead when the shape is unknown or the wait is a single action
completing (a button doing work), and `Progress` when there is a real fraction to show.

## Notes

- **Loading and empty are different states.** A skeleton that never resolves because the list came
  back empty is the worst of both — branch on loaded-and-empty separately and give it a designed
  screen (see `zero-results-state`).
- Render a small fixed number of placeholder rows. Enough to read as "content is coming"; not so
  many that they promise a page which may turn out to be short.
- `SkeletonShape` is `Rectangle`, `Circle` or `Square`; `SkeletonSize` runs `Xs`…`Xl`. Width comes
  from the style array (`["w-1/3"]`) — varying it across lines is what makes a text block read as
  text rather than as bars.
- The boot snapshot is a public asset painted to everyone before the live connection, so by
  default it replaces every content leaf with a skeleton and per-user content can never leak. The
  `SnapshotExtensions` wrappers override that for specific regions by branching on
  `UIView.IsSnapshot`, so the app keeps one `UI.Root` definition.

## Snippet

```csharp
private readonly ClientReactive<bool> _loading = new(true);
private readonly ClientReactiveList<Article> _articles = new();

/// <summary>
/// A skeleton stands in for the SHAPE of what is coming, so it mirrors the real row rather
/// than being a generic grey bar. Same container, same gaps, same circle for the avatar --
/// that is what stops the layout jumping when the content lands.
/// </summary>
private static void RenderPlaceholderRow(IView view)
{
    view.Row(["gap-3 items-center"], content: row =>
    {
        row.Skeleton(shape: SkeletonShape.Circle, size: SkeletonSize.Md);

        row.Column(["gap-2 flex-1"], content: col =>
        {
            col.Skeleton(["w-1/3"], size: SkeletonSize.Sm);
            col.Skeleton(["w-full"], size: SkeletonSize.Sm);
        });
    });
}

private void Render(IView view)
{
    view.Column(["gap-4"], content: col =>
    {
        if (_loading.Value)
        {
            // A fixed small number of placeholder rows: enough to read as "content is coming",
            // not so many that they promise a page that may turn out to be empty.
            for (var i = 0; i < 3; i++)
            {
                RenderPlaceholderRow(col);
            }

            return;
        }

        // Loaded and empty is a DIFFERENT state from loading, and needs its own designed
        // screen -- a skeleton that never resolves is the worst of both.
        if (_articles.Count == 0)
        {
            col.Text(["text-muted-foreground"], text: "Nothing here yet");
            return;
        }

        foreach (var article in _articles)
        {
            col.Column(["gap-1"], key: article.Id, content: item =>
            {
                item.Text([Text.H3], text: article.Title);
                item.Text(["text-muted-foreground"], text: article.Summary);
            });
        }
    });
}
```

## See also

- `zero-results-state` — the three different empty screens, including loaded-and-empty.
- `busy-flag-loading` — the flag that drives the branch.
