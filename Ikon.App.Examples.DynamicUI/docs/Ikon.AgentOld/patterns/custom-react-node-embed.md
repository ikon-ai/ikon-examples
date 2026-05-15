<!-- mined-from: BrainrotArena -->
# Custom React Node Embed — Drop a Real React Component into the C# Tree

The C# UI tree mostly draws itself, but sometimes you need a real React component (a Lua editor, a physics arena, a richtext editor). `view.AddNode(type: "custom.foo", props, key)` mounts the JSX component registered in `frontend-node` and wires events back through `editorBox.CreateAction<T>(...)`. The C# side stays declarative; the JS side stays a black box.

## When to use

You have functionality the platform's components can't express — a code editor, a canvas-based renderer, a 3rd-party widget. Mount it as a leaf node, pass props from reactive state, receive events through created action ids.

## Snippet

```csharp
col.Box(["flex-1 min-h-0"], content: editorBox =>
{
    editorBox.AddNode(
        type: "custom.lua-editor",
        key: $"editor:{active.Id}",
        props: new Dictionary<string, object?>
        {
            ["value"] = active.DraftCode,
            ["onValueChangeId"] = editorBox.CreateAction<string>(args =>
            {
                UpdateActiveDraftCode(args.Value ?? "");
                return Task.CompletedTask;
            }),
        });
});

// ... and a richer node with multiple action callbacks
arenaBox.AddNode(
    type: "custom.brainrot-arena",
    props: new Dictionary<string, object?>
    {
        ["code"] = active?.DraftCode ?? "",
        ["opponentCode"] = opponentCode,
        ["runId"] = _runId.Value,
        ["levelId"] = _levelId.Value,
        ["playerLabel"] = (active != null
            ? $"{DisplayName()}'s {active.Name}"
            : DisplayName()).ToUpperInvariant(),
        ["opponentLabel"] = opponentLabel,
        ["onResultId"] = arenaBox.CreateAction<string>(args =>
            HandlePlayerFightResultAsync(args.Value)),
        ["onConsoleId"] = arenaBox.CreateAction<string>(args =>
            HandleConsoleEventAsync(args.Value)),
    });
```

## Notes

- Pass a stable `key` when the node has identity (e.g. one editor per bot id) — otherwise React unmounts/remounts on every change and you lose internal state.
- Use `CreateAction<T>(handler)` not raw method references; the platform turns the returned id into a callable from JS.
- Props become the React component's props verbatim. Reactive state changes re-render with new props but DO NOT remount unless the key changes.
- The JS side lives under `frontend-node/` and is registered via the SDK's `customNodeResolver` — see `frontend-fundamentals` guide.
- Send small data through props (state ids, code strings); big binary blobs go through asset URLs.

## See also

- `chatbot-streaming` — usually you don't need a custom node; the platform handles streaming text bubbles natively
