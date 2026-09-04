<!-- mined-from: Ikon.App.Patterns -->
# Sheet And Drawer — Edge-Anchored Overlays

Both slide in from an edge and both own their header, but they answer different questions:

| | Comes from | Reads as | Use for |
|---|---|---|---|
| `Sheet` | any `Side` you pick | a panel beside the content | filters, details, settings, an inspector |
| `Drawer` | the bottom, with a drag handle | the touch idiom | action sheets, pickers, thumb-reachable confirms |

`Dialog` is the third option and is centred, interrupting, and for something that must be answered
before continuing. `overlay-selection` covers choosing between all of them.

## When to use

Secondary content that belongs to the page rather than replacing it. If the user must respond
before doing anything else, that is a `Dialog`; if it is a brief message, a toast.

## Notes

- **`title:` and `description:` are parameters, not content.** Both components render their own
  header — putting a heading inside `content:` duplicates it and loses the accessible name.
- `footer:` is a separate slot, which is where the confirm button belongs so it stays put while the
  body scrolls.
- **`modal: false` leaves the page behind interactive** — right for an inspector the user consults
  while working, wrong for anything that must be dealt with.
- `Sheet`'s `side:` defaults to `Side.Right`. Left is conventionally navigation; right is
  conventionally detail.
- `Drawer`'s `showHandle` is what tells a user it can be dragged away; `Sheet`'s `showClose` is the
  equivalent affordance for a pointer.
- Both take `open:` + `onOpenChange:` for controlled use, or `defaultOpen:` for uncontrolled. A
  `trigger:` slot renders the thing that opens it, so the open state need not be lifted at all when
  nothing else touches it.

## Snippet

```csharp
private readonly ClientReactive<bool> _filtersOpen = new(false);
private readonly ClientReactive<bool> _actionsOpen = new(false);

private void Render(IView view)
{
    view.Row(["gap-2"], content: row =>
    {
        // Sheet slides from an EDGE you choose and is the desktop shape: filters, details, a
        // settings panel beside the content it belongs to. Both Sheet and Drawer own their
        // header, so title:/description: are parameters, not something to render inside.
        row.Sheet(
            open: _filtersOpen.Value,
            onOpenChange: async open => _filtersOpen.Value = open,
            side: Side.Right,
            title: "Filters",
            description: "Narrow the list",
            trigger: t => t.Button(content: v => v.Text(text: "Filters")),
            content: panel => panel.Text(text: "…filter controls…"),
            footer: f => f.Button([Button.PrimaryMd],
                onClick: () => _filtersOpen.Value = false,
                content: v => v.Text(text: "Apply")));

        // Drawer comes from the BOTTOM with a drag handle, which is the touch idiom: an
        // action sheet, a picker, a confirm the thumb can reach. showHandle is what tells a
        // user it can be dragged away.
        row.Drawer(
            open: _actionsOpen.Value,
            onOpenChange: async open => _actionsOpen.Value = open,
            title: "Actions",
            showHandle: true,
            trigger: t => t.Button(content: v => v.Text(text: "Actions")),
            content: panel => panel.Column(["gap-2"], content: list =>
            {
                list.Button(onClick: () => _actionsOpen.Value = false, content: v => v.Text(text: "Share"));
                list.Button(onClick: () => _actionsOpen.Value = false, content: v => v.Text(text: "Duplicate"));
            }));

        // modal: false lets the page behind stay interactive -- right for an inspector the
        // user consults while working, wrong for anything that must be answered.
    });
}
```

## See also

- `overlay-selection` — choosing modal vs drawer vs popover vs tooltip vs toast by task depth.
- `slide-in-side-panel` — building one by hand when the component does not fit.
- `disclosure-surfaces` — hiding content in place rather than over it.
