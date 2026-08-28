# Nav and Menu Rows — Navigation Is Not a Row of Buttons

The three surfaces a user navigates by — page tabs, a sidebar rail, a menu row — and the token each one takes. They share a shape: a row that hugs its label, rests transparent, tints on hover, and marks the current one with **weight plus a subtle ground**, never a filled button. Getting this wrong is the single most recognisable generated-app tell: a header where every destination is a solid pill, so nothing reads as "where I am" and everything reads as "press me".

## When to use

Any app with more than one screen, section, or view. Reach for it the moment you are about to write `view.Button([Button.PrimaryMd], text: "Overview")` — that is the mistake this pattern exists to prevent.

Choosing between the three: **tabs** switch peer panels inside one context (Overview / Activity / Files); a **sidebar rail** carries 4–7 top-level destinations in an operational app (see `collapsible-sidebar-nav` for the collapse behaviour); a **menu row** holds lower-frequency actions behind a trigger.

## Snippet

```csharp
private readonly ClientReactive<string> _section = new("overview");
private readonly Reactive<bool> _menuOpen = new(false);
private readonly Reactive<string> _density = new("Comfortable");

private void RenderShell(IView view)
{
    // Page navigation between peer panels: content-width tabs on a shared rail. Not a
    // segmented control — Tabs.List/Tabs.Trigger would fill each label with the brand colour
    // and the row would read as three buttons.
    view.Tabs(
        value: _section.Value,
        onValueChange: async value => _section.Value = value,
        listStyle: [Tabs.NavList],
        triggerStyle: [Tabs.NavTriggerMd],
        tabs: Sections.Select(s =>
            new TabItem(s.Key, s.Label, v => RenderSectionBody(v, s.Label))));

    // The same destinations as a sidebar rail. NavItem carries the whole row: transparent at
    // rest, tinted on hover, and the active row differs in WEIGHT as well as colour.
    view.Column([NavPanel.Border, "w-56"], content: view =>
    {
        foreach (var s in Sections)
        {
            bool active = _section.Value == s.Key;

            view.Button([NavItem.Md, active ? NavItem.Active : NavItem.Default],
                onClick: async () => _section.Value = s.Key,
                content: v =>
                {
                    v.Icon([NavItem.Icon], name: s.Icon);
                    v.Text([NavItem.Label], text: s.Label);
                });
        }
    });

    // A menu row is a Button wearing Menu.Item — a full-width transparent row that highlights
    // on hover. Menu.Content must reach contentStyle: or the panel renders transparent.
    view.DropdownMenu(
        open: _menuOpen.Value,
        onOpenChange: async open => _menuOpen.Value = open,
        contentStyle: [Menu.Content],
        trigger: v => v.Button([Button.OutlineMd], text: "Density"),
        content: v =>
        {
            v.Text([Menu.Label], text: "Row density");

            string[] options = ["Comfortable", "Compact"];

            foreach (var option in options)
            {
                v.Button([Menu.Item], text: option, onClick: async () =>
                {
                    _density.Value = option;
                    _menuOpen.Value = false;
                });
            }
        });

    RenderDensityToggle(view);
}

// A segmented control IS the right answer when the choices are parallel values of ONE setting,
// and that is the only place the filled treatment belongs.
private void RenderDensityToggle(IView view) =>
    view.Tabs(
        value: _density.Value,
        onValueChange: async value => _density.Value = value,
        listStyle: [Tabs.List],
        triggerStyle: [Tabs.Trigger],
        tabs:
        [
            new TabItem("Comfortable", "Comfortable", _ => { }),
            new TabItem("Compact", "Compact", _ => { }),
        ]);
```

## Notes

- **Four control types, four shapes.** A navigation tab is content-width and bound to its neighbours by proximity; a button is content plus padding with a visible silhouette of its own; a segmented control is equal-width because its options are parallel values of one setting; an icon-only button is square. Give two of them the same treatment and the surface stops telling the user what kind of thing each one is.
- **Tabs vs segmented control is decided by MEANING, not by count or width.** Day / Week / Month and List / Grid are one setting's values — segmented, equal-width, filled active. Overview / Activity / Files are peer panels — navigation, content-width, underlined active. Two or three items does not make navigation a segmented control, and a wide container is not a reason to stretch tabs.
- **`Tabs.NavList` draws the shared rail** as its own `border-b`; each trigger pulls itself onto that rail with `-mb-px` and paints a 2px `border-b-2` when active. Neither adds row height, so the row does not jump by two pixels when the selection moves.
- **The active tab changes weight, not just colour.** `data-[state=active]:font-semibold` beside the indicator — colour alone fails for the ~8% of men with a colour vision deficiency, and it is the same rule that governs status dots.
- **Sizes are roles, not free numbers.** `NavTriggerSm` (32px row) for dense toolbars and narrow panes, `NavTriggerMd` (36px) for ordinary page navigation, `NavTriggerLg` (40px) where navigation is deliberately prominent. Pick from context, then check the shortest label still reads as part of the group.
- **A filter facet is not navigation.** A sidebar of Project / Platform / Type is a filter panel — style it as one (see `record-list-toolbar`). Rendering each selected facet as a full-width high-emphasis nav row gives the app two competing navigation systems, neither of which is the real one.
- Hover may change colour or ground; it must not change geometry. Bold-on-hover reflows the label and the row twitches under the pointer — save the weight change for the active state, where it is stable.

## See also

- `collapsible-sidebar-nav` — the icon rail that toggles between wide and narrow, with per-item badges.
- `board-move-without-drag` — the same `Menu.Item` rows inside a per-card move menu.
- `record-list-toolbar` — the filter/sort strip that must not be mistaken for navigation.
- `command-palette-jump` — keyboard-driven jumping across the same destinations.
