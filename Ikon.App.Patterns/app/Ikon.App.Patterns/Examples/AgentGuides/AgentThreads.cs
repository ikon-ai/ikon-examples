using Ikon.Agent.Skills;

namespace Ikon.App.Patterns.Examples;

// The agent-threads guide sections.

public static class AgentThreadExamples
{
    #region example:agent-subagent-call
    public static Task<string> SummariseAsync(AgentThread parent, string document, CancellationToken ct)
    {
        return AgentCall.RunSubAgentAsync<string>(
            parent,
            instructions: "Summarise the document in three sentences.",
            skills: [],
            inputs: new Content.Text(document),
            extract: thread =>
            {
                // With no skills the sub-agent has no tools, so its answer is its last message.
                var reply = thread.Messages.Value.LastOrDefault(message => message.Author.Kind == AuthorKind.Agent);
                return Task.FromResult(reply?.GetText());
            },
            maxPasses: 6,
            ct: ct);
    }
    #endregion

    #region example:agent-thread-options
    public static Task<AgentPlan> StartReviewAsync(AgentApp app, CancellationToken ct)
    {
        // StageMachineName must already be registered on the orchestrator, and InitialStage may not
        // be supplied without it — either mistake throws InvalidOperationException at creation.
        var options = new ThreadOptions(StageMachineName: "review", InitialStage: "Drafting");

        return app.CreatePlanAsync("Quarterly review", "reviewer", new Content.Text("Review Q3"), options, ct);
    }
    #endregion

    #region example:agent-user-decision
    public static async Task<string?> PendingQuestionAsync(AgentThread thread)
    {
        // Answering leaves the prompt stored, so only a waiting thread has a question pending
        if (thread.Status.Value != ThreadStatus.WaitingForInput)
        {
            return null;
        }

        var prompt = await UserDecisionProtocol.TryReadPromptAsync(thread);

        return prompt is null ? null : $"{prompt.Question} ({string.Join(" / ", prompt.Options)})";
    }
    #endregion
}
