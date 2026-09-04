<!-- mined-from: Ikon.App.Patterns -->
# Disclosure Surfaces — Accordion, Collapsible, Hover Card

Three ways to hide content until it is wanted, and they are not interchangeable. Picking by shape
rather than by habit is most of the work:

| Surface | Use when |
|---|---|
| `AccordionSingle` | several sections, **one open at a time** — an FAQ, where the reader compares answers one by one |
| `AccordionMultiple` | several sections a reader wants open **together** — a filter panel, a settings page |
| `Collapsible` | **one** region with a trigger — there is nothing for it to be "one of" |
| `HoverCard` | a **preview** on hover and focus that holds real content — a link, an avatar, a citation |

## When to use

Long pages with optional depth: FAQs, advanced settings, per-row detail, inline definitions. For a
short label on hover use a tooltip; for something the user must dismiss use a dialog or a drawer
(see `overlay-selection`).

## Notes

- **`AccordionItem`'s `value:` is its identity** — it is what the group's `value` matches against,
  so it is not optional. `AccordionSingle` takes a `string`; `AccordionMultiple` takes a
  `IReadOnlyList<string>`, and passing one where the other is expected is the usual compile error
  here.
- `collapsible: true` on `AccordionSingle` lets the open item close again, leaving none open.
  Without it one is always open.
- The trigger goes inside an `AccordionHeader` — that pairing is what gives the control its
  heading semantics.
- **`HoverCard` is not a tooltip.** It holds real content, so it needs `openDelay`/`closeDelay` to
  stop it flickering as the pointer crosses it. And `hover:` does not exist on touch: never put
  anything reachable *only* in a hover card.
- `forceMount:` on `AccordionContent`/`CollapsibleContent` renders the body even while closed —
  for animating it, or when something inside must stay mounted.
- `Separator` is `decorative: true` by default, which keeps it out of the accessibility tree. Set
  it false only when the rule genuinely separates two groups a screen reader should hear about.

## Snippet

```csharp
private static readonly Faq[] Faqs =
[
    new("shipping", "When does it ship?", "Within two working days."),
    new("returns", "Can I return it?", "Yes, within 30 days."),
];

private readonly ClientReactive<string> _openFaq = new("shipping");
private readonly ClientReactive<bool> _advancedOpen = new(false);

private void Render(IView view)
{
    view.Column(["gap-4"], content: col =>
    {
        // AccordionSingle keeps ONE item open and is the right shape for an FAQ: the reader
        // compares answers one at a time. AccordionMultiple takes a LIST value instead, for
        // sections a reader wants open together. collapsible: lets the open one close again.
        col.AccordionSingle(
            value: _openFaq.Value,
            collapsible: true,
            onValueChange: async id => _openFaq.Value = id,
            content: accordion =>
            {
                foreach (var faq in Faqs)
                {
                    // The item's value: is its identity -- that is what the group's value
                    // matches against, so it is not optional.
                    accordion.AccordionItem(value: faq.Id, key: faq.Id, content: item =>
                    {
                        item.AccordionHeader(content: header =>
                            header.AccordionTrigger(content: t => t.Text(text: faq.Question)));

                        item.AccordionContent(content: body => body.Text(text: faq.Answer));
                    });
                }
            });

        col.Separator();

        // Collapsible is the single-region case: one trigger, one body, no group and no
        // identity to match. Reach for it when there is nothing to be "one of".
        col.Collapsible(
            open: _advancedOpen.Value,
            onOpenChange: async open => _advancedOpen.Value = open,
            content: disclosure =>
            {
                disclosure.CollapsibleTrigger(content: t => t.Text(text: "Advanced settings"));
                disclosure.CollapsibleContent(content: body => body.Text(text: "Nothing to see yet."));
            });

        // HoverCard previews on hover and focus and is NOT a tooltip: it holds real content,
        // so it needs open/close delays to stop it flickering as the pointer crosses it.
        // Never put anything reachable only here -- hover does not exist on touch.
        col.HoverCard(
            openDelay: 300,
            closeDelay: 150,
            trigger: t => t.Link(["underline"], text: "shipping policy"),
            contentSlot: card => card.Text(text: "Two working days, tracked, insured."));
    });
}
```

## See also

- `overlay-selection` — choosing modal vs drawer vs popover vs tooltip vs toast by task depth.
- `collapsible-sidebar-nav` — the navigation-specific case.
