namespace Ikon.App.Patterns.Patterns;

// Pattern: live-profile-extraction-side-panel — see docs/patterns/live-profile-extraction-side-panel.md.
// The docsnippet region runs a background extraction after each chat turn and merges only non-empty
// fields into the reactive profile; the stubs outside it stand in for the chat log and the profile
// record the side panel renders.
internal sealed class LiveProfileExtractionSidePanel : IPatternDemo
{
    public string Slug => "live-profile-extraction-side-panel";
    public string Title => "Live profile extraction side panel";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: after each chat turn a background extraction merges only non-empty fields into a reactive customer profile. See the source and docs/patterns/live-profile-extraction-side-panel.md.");

    private sealed record ChatTurn(string Role, string Content);
    private sealed record ProfileExtraction(string Name, string Budget, string Stage);

    private sealed class CustomerProfile
    {
        public string Name { get; set; } = "";
        public string Budget { get; set; } = "";
        public string Stage { get; set; } = "";
    }

    private readonly ReactiveList<ChatTurn> _messages = new();
    private readonly Reactive<CustomerProfile> _profile = new(new CustomerProfile());

    #region docsnippet:pattern-live-profile-extraction-side-panel
    private async Task ExtractProfileAsync()
    {
        var recent = _messages.TakeLast(6)
            .Select(m => $"{(m.Role == "user" ? "Customer" : "Agent")}: {m.Content}")
            .ToList();
        if (recent.Count == 0) { return; }

        var current = _profile.Value;
        var conversation = string.Join("\n", recent);

        var result = await Emerge.Run<ProfileExtraction>(
            LLMModel.Claude45Haiku, pass =>
            {
                pass.SystemPrompt = "Extract customer info. Return only fields that are mentioned. " +
                                    "Empty strings for unknown fields.";
                pass.Command = $"""
                    Current profile:
                    {JsonSerializer.Serialize(current)}

                    Recent conversation:
                    {conversation}

                    Return JSON:
                    {pass.JsonSchema}
                    """;
                pass.Temperature = 0;
            });

        var p = _profile.Value;
        if (!string.IsNullOrEmpty(result.Name)) { p.Name = result.Name; }
        if (!string.IsNullOrEmpty(result.Budget)) { p.Budget = result.Budget; }
        if (!string.IsNullOrEmpty(result.Stage)) { p.Stage = result.Stage; }
        // ... merge each field only if non-empty
        _profile.NotifyUpdate();
    }

    private void OnCompleted()
    {
        // Fired after each Completed event:
        _ = ExtractProfileAsync();
    }
    #endregion
}
