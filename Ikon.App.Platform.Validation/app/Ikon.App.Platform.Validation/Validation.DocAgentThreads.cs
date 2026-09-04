using Ikon.Agent;
using Ikon.Agent.Skills;
using Ikon.AI.Emergence;

namespace Ikon.App.Platform.Validation.Docs;

// The agent-threads guide sections.

public static class AgentThreadDocs
{
    #region docsnippet:agent-subagent-call
    public static Task<string> SummariseAsync(AgentThread parent, string document, CancellationToken ct)
    {
        return AgentCall.RunSubAgentAsync<string>(
            parent,
            instructions: "Summarise the document in three sentences. Save the summary as an artifact named 'summary'.",
            skills: [],
            inputs: new Content.Text(document),
            extract: async thread =>
            {
                return await thread.GetArtifactAsync("summary") is { } summary
                    ? string.Concat(summary.Parts.OfType<Content.Text>().Select(part => part.Value))
                    : null;
            },
            maxPasses: 6,
            ct: ct);
    }
    #endregion

    #region docsnippet:agent-thread-options
    public static Task<AgentPlan> StartReviewAsync(AgentApp app, CancellationToken ct)
    {
        // StageMachineName must already be registered on the orchestrator, and InitialStage may not
        // be supplied without it — either mistake throws InvalidOperationException at creation.
        var options = new ThreadOptions(StageMachineName: "review", InitialStage: "Drafting");

        return app.CreatePlanAsync("Quarterly review", "reviewer", new Content.Text("Review Q3"), options, ct);
    }
    #endregion

    #region docsnippet:agent-user-decision
    public static async Task<string?> PendingQuestionAsync(AgentThread thread)
    {
        var prompt = await UserDecisionProtocol.TryReadPromptAsync(thread);

        return prompt is null ? null : $"{prompt.Question} ({string.Join(" / ", prompt.Options)})";
    }
    #endregion
}
