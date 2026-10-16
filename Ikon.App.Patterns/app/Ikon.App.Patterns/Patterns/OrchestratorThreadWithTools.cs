namespace Ikon.App.Patterns.Patterns;

// Pattern: orchestrator-thread-with-tools — see docs/patterns/orchestrator-thread-with-tools.md.
// OivaApp outside the region stands in for the host app the skill's tools call back into; the implicit
// conversion lets the example's `new BuildingSkill(this)` hand this pattern class to the skill.
internal sealed class OrchestratorThreadWithTools : IPatternDemo
{
    public string Slug => "orchestrator-thread-with-tools";
    public string Title => "Orchestrator thread with tools";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend AI pattern: an Orchestrator persona drives an AgentThread whose skill exposes tools the model calls to ground every claim. See the source and docs/patterns/orchestrator-thread-with-tools.md.");

    public sealed class OivaApp
    {
        public static implicit operator OivaApp(OrchestratorThreadWithTools snippet) => throw new NotImplementedException();

        public Task<string> SearchBuildingsAsync(string query) => throw new NotImplementedException();
        public string SelectBuilding(string id) => throw new NotImplementedException();
    }

    #region example:pattern-orchestrator-thread-with-tools
    private const string PersonaName = "oiva";

    // Marks the seed so the chat leaves it out: the person did not write it
    private const string SeedPayloadKind = "oiva/seed";

    private Orchestrator _mind = null!;
    private AgentThread _thread = null!;

    private async Task InitializeMindAsync()
    {
        _mind = new Orchestrator();
        _mind.AddPersona(new Persona(
            Name: PersonaName,
            SystemPrompt: "You are Oiva — a building maintenance assistant. "
                + "Use the tools to ground every claim. Reply briefly.",
            Skills: [new BuildingSkill(this)],
            Reasoning: new Reasoning(Capability.Standard, ModelFamily.Claude, Temperature: 0.4, MaxOutputTokensPerPass: 8000)));
        await _mind.ResumeAsync();

        _thread = await _mind.CreateThreadAsync(
            PersonaName,
            new Message(Author.User, [new Content.Text("Help the user inspect and maintain their buildings.")], SeedPayloadKind),
            appName: "Oiva",
            planName: "session");
    }

    public sealed class BuildingSkill(OivaApp app) : Skill
    {
        public override string Name => "buildings";

        public override string Instructions =>
            "Search first, then answer from tool results only — never invent building data.";

        public override IEnumerable<Tool> Tools() =>
        [
            // TResult is what the tool returns once awaited, so an async method returning
            // Task<string> is Tool.Of<string, string>, the same as a sync one returning string.
            Tool.Of<string, string>("search_building",
                "Search buildings by name/address/decade. Returns matches.",
                app.SearchBuildingsAsync),

            Tool.Of<string, string>("select_building",
                "Switch the UI to the given building id.",
                app.SelectBuilding),
        ];
    }

    private async Task PostUserMessageAsync(string text)
    {
        await _thread.PostAsync(new Message(Author.User, [new Content.Text(text)]));
        await _thread.ReactivateIfIdleAsync();
        await _thread.DriveAsync(DriveMode.UntilQuiescent);
    }

    private void RenderChat(UIView view)
    {
        var turns = _thread.Messages.Value
            .Where(m => m.PayloadKind is null && (m.Author == Author.User || m.Author == Author.Agent(PersonaName)))
            .ToList();

        foreach (var (message, index) in turns.Select((m, i) => (m, i)))
        {
            var isUser = message.Author == Author.User;
            view.Box(["rounded-xl p-3", isUser ? "bg-foreground/10 self-end" : "bg-card self-start"],
                key: index.ToString(),
                content: v => v.Text(["whitespace-pre-wrap text-sm"], message.GetText()));
        }

        foreach (var call in _thread.ToolCallTimeline.Value.Where(c => c.ResultText is null))
        {
            view.Text([Text.Caption], $"-> {call.ToolName}…");
        }

        if (_thread.Busy.Value)
        {
            view.Spinner();
        }
    }
    #endregion
}
