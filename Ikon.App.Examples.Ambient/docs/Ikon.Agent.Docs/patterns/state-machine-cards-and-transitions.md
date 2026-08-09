<!-- mined-from: Anima -->
# State Machine UI — Cards For States, Rows For Transitions

A two-tab editor: one tab manages a list of named states (each with image + generated loop video), the other tab lists every directed transition between states. Transitions auto-derive from the cross-product of states with images, and each row exposes a per-status status glyph and a generate-this-one button.

## When to use

You're building a tool whose user model is "I have N states and M transitions between them" — character animators, game state designers, workflow editors, conversation flow builders. The shape generalizes any time the user holds a set of nodes and inspects/generates the directed pairs between them.

## Snippet

```csharp
private void RenderStatesTab(UIView view)
{
    view.Column([Layout.Column.Md, "pt-4"], content: view =>
    {
        view.Button([Button.PrimaryMd, "w-full"], "+ Add State",
            onClick: async () => { AddState(); });

        // _states is a ReactiveList<CharacterState>; loop by index so handlers can write the item back.
        for (var i = 0; i < _states.Count; i++)
        {
            var index = i;
            var state = _states[i];
            var stateId = state.Id;
            view.Column([Card.Default, "p-3", Layout.Column.Sm], content: view =>
            {
                view.TextField([Input.Default, "font-medium"],
                    placeholder: "State name",
                    value: state.Name,
                    onValueChange: async value =>
                    {
                        state.Name = value;
                        _states[index] = state;   // indexer setter fires the one notification
                    });

                // ... image upload / thumbnail ...

                var statusLabel = state.LoopStatus switch
                {
                    GenerationStatus.Generating => "Generating...",
                    GenerationStatus.Complete => "Regenerate Loop",
                    GenerationStatus.Failed => "Retry Loop",
                    _ => "Generate Loop"
                };

                view.Button([Button.SecondaryMd, "flex-1 text-sm"], statusLabel,
                    disabled: !canGenerate,
                    onClick: async () => { _ = GenerateLoopVideoAsync(stateId); });
            });
        }
    });
}

private void RenderTransitionsTab(UIView view)
{
    foreach (var transition in _transitions)
    {
        var sourceState = _states.First(s => s.Id == transition.SourceStateId);
        var targetState = _states.First(s => s.Id == transition.TargetStateId);
        var transitionId = transition.Id;

        view.Row([Card.Default, "p-3 items-center gap-2"], content: view =>
        {
            view.Text(["text-sm flex-1 truncate"], sourceState.Name + " → " + targetState.Name);

            var statusLabel = transition.Status switch
            {
                GenerationStatus.Generating => "...",
                GenerationStatus.Complete => "✓",
                GenerationStatus.Failed => "!",
                _ => ""
            };

            view.Button([Button.SecondaryMd, "text-xs"],
                transition.Status == GenerationStatus.Generating ? "Generating..." : "Generate",
                disabled: transition.Status == GenerationStatus.Generating,
                onClick: async () => { _ = GenerateTransitionVideoAsync(transitionId); });
        });
    }
}
```

## Notes

- Per-item status enum (`Idle / Generating / Complete / Failed`) drives both the button label and an inline glyph — single source of truth, no parallel busy flags.
- Capture `var stateId = state.Id;` and `var index = i;` before the `onClick` lambda so the closure doesn't bind to the loop variable.
- States and transitions are `ReactiveList<T>` fields. A field write inside an item is invisible to the list, so after mutating an item in place assign it back through the indexer (`_states[index] = state`) — one notification. Never rebuild the list by hand: `_states.Value` is an `IReadOnlyList<T>` snapshot, so `.Value.Add` / `.Value.Find` / `.Value[i] =` do not compile. Read with `foreach (var t in _transitions)`, `_states.First(…)`, `_states.Count`.
- Provide a "Generate All" bulk-action button at the top of each tab; it kicks off the per-item loops in parallel.
- Auto-create the transitions cross-product whenever a state with an image is added; users won't manually wire them.

## See also

- `agent-roster-card-grid` — the agent-card grid that toggles members in/out of an active set
