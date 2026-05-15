<!-- mined-from: Ikon.App.Monitor -->
# Dynamic Input List — Self-Growing Rows With Last-Empty-Always

Render a list of text fields where the user can type into the last empty row to spawn a new one and click the X to remove any other row. No "add row" button needed. Used for filters, group-bys, tags, recipients — anywhere you want N variable inputs with zero ceremony.

## When to use

A form needs an arbitrary-length list of short strings (filter clauses, email addresses, tag keywords). You want it to feel like a single field that keeps growing rather than a managed array.

## Snippet

```csharp
private static void RenderDynamicInputList(UIView view, Reactive<List<string>> items, string placeholder)
{
    var list = items.Value;
    for (int i = 0; i < list.Count; i++)
    {
        int index = i;
        view.Row(["gap-2 items-center"], content: view =>
        {
            view.TextField([Input.Default, "flex-1"],
                placeholder: placeholder,
                value: list[index],
                onValueChange: async v =>
                {
                    list[index] = v;
                    while (list.Count > 1
                           && string.IsNullOrWhiteSpace(list[^1])
                           && string.IsNullOrWhiteSpace(list[^2]))
                    {
                        list.RemoveAt(list.Count - 1);
                    }
                    if (!string.IsNullOrWhiteSpace(list[^1])) { list.Add(""); }
                    items.NotifyUpdate();
                    await Task.CompletedTask;
                });

            if (index < list.Count - 1)
            {
                view.Button([Button.GhostMd, Button.Icon, "shrink-0"],
                    onClick: async () =>
                    {
                        list.RemoveAt(index);
                        if (list.Count == 0 || !string.IsNullOrWhiteSpace(list[^1])) { list.Add(""); }
                        items.NotifyUpdate();
                        await Task.CompletedTask;
                    },
                    content: v => v.Icon([Icon.Default], name: "x"));
            }
            else
            {
                view.Box(["w-9 shrink-0"]); // spacer keeps the last row aligned with the others
            }
        });
    }
}

private static List<string> CollectNonEmpty(Reactive<List<string>> items) =>
    items.Value.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
```

## Notes

- Invariant: the last row is always blank. Typing into it auto-appends a new blank; clearing the second-to-last collapses any trailing empties.
- The remove button is omitted on the trailing empty row — replaced with a spacer Box of equal width so the column doesn't jump.
- `Reactive<List<string>>.NotifyUpdate()` is required because we're mutating the list in place rather than reassigning.
- Pair with `CollectNonEmpty` at submit time to drop the trailing empty + any other gaps.

## See also

- `inline-list-cell-edit`
- `multi-step-wizard`
