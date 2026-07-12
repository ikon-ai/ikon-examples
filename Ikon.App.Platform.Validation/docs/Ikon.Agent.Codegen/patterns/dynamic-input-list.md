<!-- mined-from: Ikon.App.Monitor -->
# Dynamic Input List — Self-Growing Rows With Last-Empty-Always

Render a list of text fields where the user can type into the last empty row to spawn a new one and click the X to remove any other row. No "add row" button needed. Used for filters, group-bys, tags, recipients — anywhere you want N variable inputs with zero ceremony.

## When to use

A form needs an arbitrary-length list of short strings (filter clauses, email addresses, tag keywords). You want it to feel like a single field that keeps growing rather than a managed array.

## Snippet

```csharp
private static void RenderDynamicInputList(UIView view, ReactiveList<string> items, string placeholder)
{
    var count = items.Count;
    for (int i = 0; i < count; i++)
    {
        int index = i;
        view.Row(["gap-2 items-center"], content: view =>
        {
            view.TextField([Input.Default, "flex-1"],
                placeholder: placeholder,
                value: items[index],
                onValueChange: async v =>
                {
                    items.Update(current =>
                    {
                        var list = new List<string>(current);
                        list[index] = v;
                        while (list.Count > 1
                               && string.IsNullOrWhiteSpace(list[^1])
                               && string.IsNullOrWhiteSpace(list[^2]))
                        {
                            list.RemoveAt(list.Count - 1);
                        }
                        if (!string.IsNullOrWhiteSpace(list[^1])) { list.Add(""); }
                        return list;
                    });
                    await Task.CompletedTask;
                });

            if (index < count - 1)
            {
                view.Button([Button.GhostMd, Button.Icon, "shrink-0"],
                    onClick: async () =>
                    {
                        items.Update(current =>
                        {
                            var list = new List<string>(current);
                            list.RemoveAt(index);
                            if (list.Count == 0 || !string.IsNullOrWhiteSpace(list[^1])) { list.Add(""); }
                            return list;
                        });
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

private static List<string> CollectNonEmpty(ReactiveList<string> items) =>
    items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
```

## Notes

- Invariant: the last row is always blank. Typing into it auto-appends a new blank; clearing the second-to-last collapses any trailing empties.
- The remove button is omitted on the trailing empty row — replaced with a spacer Box of equal width so the column doesn't jump.
- `items` is a `ReactiveList<string>` — each edit is one `items.Update(list => …)` so the whole transform (set value, collapse trailing empties, ensure last blank) lands as a single notification; mutating `.Value` in place does not compile and there is no `NotifyUpdate`.
- Pair with `CollectNonEmpty` at submit time to drop the trailing empty + any other gaps.

## See also

- `inline-list-cell-edit`
- `multi-step-wizard`
