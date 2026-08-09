<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Contact Start Screen — Pick A Customer Or Skip Into Anonymous

Open the app on a list of contacts (CRM customers, supports tickets, prior chats). Each row is a clickable card with name + email + phone + truncated notes. A free-text filter narrows the list; a "Start without customer →" button opens an anonymous session. Selection seeds a per-session profile so the agent has full context from message one.

## When to use

Sales/support/coaching apps where most sessions are tied to an existing record but you also need a "blank" option. Replaces an empty chat input with no context as the entry point.

## Snippet

```csharp
private void RenderStartScreen(UIView view)
{
    view.Column(["h-screen flex flex-col bg-[#09090b]"], content: col =>
    {
        col.Box(["px-8 pt-8 pb-4"], content: header =>
        {
            header.Column(["max-w-4xl mx-auto w-full gap-2"], content: c =>
            {
                c.Text(["text-xs font-medium tracking-[0.2em] uppercase text-white/20"], "Parrotz AI");
                c.Text(["text-2xl font-light text-white/80"], "Testing Rig");
                c.Text(["text-sm text-white/30 mt-1"], "Pick a customer or start without one.");
            });
        });

        col.Box(["px-8 pb-4"], content: f => f.Row(["max-w-4xl mx-auto w-full"], content: row =>
            row.TextField(["flex-1 bg-white/5 border border-white/10 rounded-lg px-4 py-2"],
                placeholder: "Filter customers...",
                value: _customerFilter.Value,
                onValueChange: async v => _customerFilter.Value = v)));

        col.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["px-8 py-2"], content: scroll =>
        {
            var filter = _customerFilter.Value ?? "";
            var list = string.IsNullOrEmpty(filter)
                ? _customers.Value
                : _customers.Value.Where(c =>
                    c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            scroll.Column(["gap-2 max-w-4xl mx-auto w-full"], content: ls =>
            {
                foreach (var customer in list)
                {
                    ls.Button(["bg-white/[0.03] hover:bg-white/[0.07] border border-white/[0.06] rounded-lg p-4 text-left w-full"],
                        onClick: async () => StartChat(customer),
                        content: v => v.Column(["gap-1"], content: col2 =>
                        {
                            col2.Text(["text-base font-medium text-white/70"], customer.Name);
                            col2.Row(["gap-4"], content: r =>
                            {
                                if (!string.IsNullOrEmpty(customer.Email))
                                    r.Text(["text-xs text-white/25"], customer.Email);
                                if (!string.IsNullOrEmpty(customer.Phone))
                                    r.Text(["text-xs text-white/25"], customer.Phone);
                            });
                            if (!string.IsNullOrEmpty(customer.Notes))
                                col2.Text(["text-xs text-white/15 mt-1 line-clamp-2"], customer.Notes);
                        }));
                }
            });
        });

        col.Box(["px-8 py-6"], content: f => f.Row(["max-w-4xl mx-auto w-full"], content: row =>
            row.Button(["bg-transparent border border-white/10 rounded-lg px-6 py-3 text-sm"],
                onClick: async () => StartChat(null),
                content: v => v.Text(text: "Start without customer →"))));
    });
}
```

## Notes

- Filter on name/email/phone with `Contains(StringComparison.OrdinalIgnoreCase)` rather than ToLower-and-compare; cleaner and allocation-free.
- Skip rows with no name AND no email at the data layer, not the renderer — junk records shouldn't be a UI concern.
- The "start without" button at the bottom is a deliberate de-emphasis with `border-white/10` — the path of least resistance is picking a real contact.

## See also

- `search-filter-grouped-results`
- `shared-list-ai-cleanup`
- `kpi-card-grid`
