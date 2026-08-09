<!-- mined-from: Anima -->
# Inline List-Cell Edit — Type-To-Save Card Fields

Each list item renders its editable string field directly as a `TextField` inside the card (no edit/save mode). The `onValueChange` mutates the item in-place, then assigns it back through the `ReactiveList` indexer to fire the change, then triggers a debounced save. The user never clicks an "Edit" button.

## When to use

Anywhere editing should feel like Notion or a spreadsheet — single short fields per row, no validation friction, no submit step. State editors, kanban card titles, settings rows, contact lists. Avoid for long-form prose or fields that need validation before save (use a form pattern instead).

## Snippet

```csharp
// _states is a ReactiveList<CharacterState>
for (var i = 0; i < _states.Count; i++)
{
    var index = i;
    var state = _states[i];
    var stateId = state.Id;
    view.Column([Card.Default, "p-3", Layout.Column.Sm], content: view =>
    {
        // Name field — edits in place, saves on every keystroke (debounced inside SaveProjectAsync)
        view.TextField(
            [Input.Default, "font-medium"],
            placeholder: "State name",
            value: state.Name,
            onValueChange: async value =>
            {
                state.Name = value;
                _states[index] = state;
                _ = SaveProjectAsync();
            });

        // Image upload / thumbnail
        if (state.ImageData != null && state.ImageMime != null)
        {
            view.Row(["gap-2 items-center"], content: view =>
            {
                view.Image(
                    style: ["w-16 h-16 object-cover", Tokens.Radius.Md],
                    data: state.ImageData,
                    mimeType: state.ImageMime,
                    alt: state.Name);

                view.FileUpload(
                    accept: ["image/*"],
                    multiple: false,
                    maxFileSize: 20_000_000,
                    onUploadComplete: async args => await HandleImageUpload(stateId, args),
                    content: v => v.Text(["text-xs cursor-pointer text-primary underline"], "Replace"));
            });
        }

        // Action row — generate button uses the captured stateId from the closure
        view.Row(["gap-2"], content: view =>
        {
            view.Button([Button.SecondaryMd, "flex-1 text-sm"], statusLabel,
                disabled: !canGenerate,
                onClick: async () => { _ = GenerateLoopVideoAsync(stateId); });

            view.Button([Button.GhostMd, Button.Icon, "text-destructive"],
                onClick: async () => { RemoveState(stateId); },
                content: v => v.Icon([Icon.Default], name: "trash-2"));
        });
    });
}
```

## Notes

- The `state.Name = value` mutation is *required* — it is what propagates the new field to consumers that hold the same item by reference.
- A `ReactiveList<T>` notifies on ITS mutators, not on a field write inside an item. After a per-item mutation, assign the item back through the indexer (`_states[index] = state`) — one notification, and the row re-renders. Never rebuild the list by hand: `_states.Value` is an `IReadOnlyList<T>` snapshot, so `.Value.Add`/`.Value[i] =` do not compile.
- Loop by index (`for (var i = 0; i < _states.Count; i++)`) so the handler has an `index` to write back through; `Count` and `[i]` are tracked reads. Capture `var stateId = state.Id;` and `var index = i;` outside the lambdas so the closures capture values, not the loop variable.
- Use `_ = SaveProjectAsync();` (fire-and-forget) on every keystroke; debounce inside the save function or use a periodic timer instead. Don't `await` it — that re-renders the field and breaks focus.
- Skip an explicit "Edit" button entirely. Modes are friction.

## See also

- `agent-roster-card-grid` — for boolean per-item state instead of text
