namespace Ikon.App.Patterns.Patterns;

// Pattern: disclosure-surfaces — see docs/patterns/disclosure-surfaces.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class DisclosureSurfaces : IPatternDemo
{
    public string Slug => "disclosure-surfaces";
    public string Title => "Disclosure surfaces: accordion, collapsible, hover card";
    public string Category => "Layout & navigation";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Faq(string Id, string Question, string Answer);

    #region docsnippet:pattern-disclosure-surfaces
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
    #endregion
}
