namespace Ikon.App.Patterns.Patterns;

// Pattern: custom-react-node-embed — see docs/patterns/custom-react-node-embed.md.
// The stubs outside the region stand in for the app's real draft state and event handlers so the
// docsnippet body — the AddNode / CreateAction wiring the doc extracts — compiles on its own.
internal sealed class CustomReactNodeEmbed : IPatternDemo
{
    public string Slug => "custom-react-node-embed";
    public string Title => "Custom React node embed";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Embeds custom React components via view.AddNode, wiring client-side callbacks through CreateAction. See the source and docs/patterns/custom-react-node-embed.md.");

    private sealed record BotDraft(string Id, string Name, string DraftCode);

    private readonly UIView col = null!;
    private readonly UIView arenaBox = null!;
    private readonly BotDraft active = null!;
    private readonly string opponentCode = "";
    private readonly string opponentLabel = "";
    private readonly Reactive<int> _runId = new(0);
    private readonly Reactive<string> _levelId = new("");

    private string DisplayName() => throw new NotImplementedException();
    private void UpdateActiveDraftCode(string code) => throw new NotImplementedException();
    private Task HandlePlayerFightResultAsync(string? payload) => throw new NotImplementedException();
    private Task HandleConsoleEventAsync(string? payload) => throw new NotImplementedException();

    #region docsnippet:pattern-custom-react-node-embed
    private void Render(IView view)
    {
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
    }
    #endregion
}
