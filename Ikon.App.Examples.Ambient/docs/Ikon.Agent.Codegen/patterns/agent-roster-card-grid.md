<!-- mined-from: Architect -->
# Agent Roster Card Grid — Toggle Specialists Onto The Team

A responsive grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`) of agent personas, each card showing a circular icon avatar (background tinted when active), name, role, specialty, and a Switch on the right edge that flips membership. The active set lives in a `Reactive<List<string>>` of agent ids; the card looks at `Contains` to decide its ring/tint.

## When to use

Any "pick a subset from a fixed roster" surface — choosing AI personas/agents, picking team members, selecting plugins/integrations to enable, multi-select feature flags. Especially good when each member has a meaningful description that should be visible at choice time, not behind a tooltip.

## Snippet

```csharp
private void RenderAgentsTab(UIView view)
{
    view.Box(["mt-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"], content: view =>
    {
        foreach (var agent in AvailableAgents)
        {
            RenderAgentCard(view, agent);
        }
    });
}

private void RenderAgentCard(UIView view, Agent agent)
{
    var isActive = _activeAgentIds.Value.Contains(agent.Id);

    view.Box([Card.Default, "p-5 cursor-pointer", isActive ? "ring-2 ring-primary bg-primary/5" : ""], content: view =>
    {
        view.Column([Layout.Column.Md], content: view =>
        {
            view.Row(["items-start justify-between"], content: view =>
            {
                view.Row(["items-center gap-3"], content: view =>
                {
                    view.Box(["w-10 h-10 rounded-full flex items-center justify-center",
                        isActive ? "bg-primary text-primary-foreground" : "bg-primary/10"], content: view =>
                    {
                        view.Icon([Icon.Default, isActive ? "text-primary-foreground" : "text-primary"], name: agent.IconName);
                    });

                    view.Column(["gap-1"], content: view =>
                    {
                        view.Text([Text.BodyStrong], agent.Name);
                        view.Text([Text.Caption, "text-primary"], agent.Role);
                    });
                });

                view.Switch([Switch.Default],
                    value: isActive,
                    onValueChange: async v =>
                    {
                        if (v && !_activeAgentIds.Value.Contains(agent.Id))
                        {
                            _activeAgentIds.Add(agent.Id);
                        }
                        else if (!v)
                        {
                            _activeAgentIds.Remove(agent.Id);
                        }
                    });
            });

            view.Text([Text.Caption, "text-muted-foreground"], agent.Specialty);
            view.Text([Text.Small, "text-muted-foreground italic mt-1"], agent.Persona);
        });
    });
}
```

## Notes

- Use `ring-2 ring-primary bg-primary/5` for the active state — a ring reads as "selected" without consuming a border slot already used by the card.
- The agent icon doubles as a status indicator: filled background when active, tinted background when not.
- Use the platform's `Reactive<List<T>>.Add(item)` / `.Remove(item)` extensions — they mutate AND fire `Changed` in one call. Calling `_x.Value.Add(item)` directly does not trigger a UI update (the list reference is unchanged); the extensions handle that for you.
- Pair with a sticky "N specialists ready — Start Discussion" CTA that only renders when `Count > 0`. Gives the user feedback that their selection had effect.

## See also

- `state-machine-cards-and-transitions` — similar card-list shape with item-level actions instead of toggle
- `multi-agent-parallel-discussion` — the back-end that consumes this selection
