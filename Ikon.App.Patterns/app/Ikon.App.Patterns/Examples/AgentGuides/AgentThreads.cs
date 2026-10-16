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

    #region example:agent-subagent-fan-out
    // A coordinator's tool hands each course to its own helper agent at once, and a reactive the UI
    // renders shows each helper's state while the tool runs.
    public sealed class ChefsSkill(Reactive<IReadOnlyDictionary<string, string>> progress) : Skill
    {
        private readonly Lock _progressLock = new();

        public override string Name => "chefs";

        public override IEnumerable<Tool> Tools() =>
        [
            Tool.OfContext<string[], string>("ask_chefs",
                "Gives each course to its own chef and returns their dishes. Call it once with every course.",
                async (ctx, courses) =>
                {
                    progress.Value = courses.ToDictionary(course => course, _ => "working");

                    var dishes = await Task.WhenAll(courses.Select(async course =>
                    {
                        try
                        {
                            var dish = await AgentCall.RunSubAgentAsync<string>(
                                ctx.Thread,
                                instructions: $"You are the {course} chef. Suggest one dish in two sentences.",
                                skills: [],
                                inputs: new Content.Text(course),
                                extract: chef => Task.FromResult(chef.FinalAnswer.Value),
                                name: "chef",
                                maxPasses: 3,
                                ct: ctx.Cancellation);
                            SetState(course, "done");
                            return $"{course}: {dish}";
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            // The other chefs' dishes still reach the coordinator, which says this course is missing.
                            Log.Instance.Warning($"The {course} chef failed: {ex.Message}");
                            SetState(course, "failed");
                            return $"{course}: the chef could not answer";
                        }
                    }));

                    return string.Join("\n", dishes);
                }),
        ];

        // The helpers finish on different threads, so the copy-and-replace is done under a lock.
        private void SetState(string course, string state)
        {
            lock (_progressLock)
            {
                progress.Value = new Dictionary<string, string>(progress.Value) { [course] = state };
            }
        }
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

    public enum DispatchStage { Intake, Routing, Confirmation }

    #region example:agent-stage-machine-attach
    public static Task<AgentThread> StartDispatchAsync(Orchestrator orchestrator, IStageMachine<DispatchStage> stages, CancellationToken ct)
    {
        orchestrator.RegisterStageMachine(stages);

        return orchestrator.CreateThreadAsync(
            "dispatcher", new Content.Text("Take the next parcel."), appName: "Desk", planName: "depot",
            options: new ThreadOptions(StageMachineName: stages.Name, InitialStage: nameof(DispatchStage.Intake)),
            ct: ct);
    }
    #endregion

    #region example:agent-stage-chat-tools
    public enum ConciergeStage { Understanding, Booking }

    public sealed class ConciergeStages(Skill lookups, Skill bookings) : IStageMachine<ConciergeStage>
    {
        public string Name => "concierge";

        public ConciergeStage InitialState => ConciergeStage.Understanding;

        public SkillSet SkillsFor(AgentThread thread, ConciergeStage state) => state switch
        {
            ConciergeStage.Understanding => new SkillSet("understanding", [lookups, new StageTools(
                Tool.MoveTo(ConciergeStage.Booking, "begin_booking", "Call when the guest picks a room and wants it booked."))]),
            _ => new SkillSet("booking", [bookings, new StageTools(
                Tool.MoveTo(ConciergeStage.Understanding, "finish_booking", "Call after the booking tool returned an id.", continueInNewStage: false))]),
        };

        public Task<StageTransitionResult<ConciergeStage>?> TryTransitionAsync(AgentThread thread, ConciergeStage current, ThreadEvent evt, CancellationToken ct) =>
            Task.FromResult<StageTransitionResult<ConciergeStage>?>(null);

        private sealed class StageTools(params Tool[] tools) : Skill
        {
            public override string Name => "stage";

            public override IEnumerable<Tool> Tools() => tools;
        }
    }
    #endregion

    #region example:agent-thread-rollback
    // Rolls the assistant back to just before message `keep` (0-based): the returned thread has the
    // history up to there, and the current one is archived. Use the returned thread from now on.
    public static async Task<AgentThread> RollBackAsync(Orchestrator orchestrator, AgentThread current, int keep, CancellationToken ct)
    {
        var now = await current.CheckpointAsync($"rollback-{keep}", ct);
        var earlier = now with { Messages = [.. now.Messages.Take(keep)] };

        var restored = await orchestrator.RestoreCheckpointAsync(earlier, ct);
        await current.TransitionAsync(ThreadTransition.Archive);
        return restored;
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

    #region example:agent-plan-section-tool
    // The agent writes its plan's named sections through these tools, and the UI renders
    // thread.Plan.Sections, a reactive, as they change.
    public sealed class PlanSectionsSkill : Skill
    {
        public override string Name => "plan-sections";

        public override IEnumerable<Tool> Tools() =>
        [
            Tool.OfContext<string, string, string>("write_section",
                "Writes one named section of the plan, replacing its text.",
                async (ctx, name, content) =>
                {
                    await ctx.Thread.Plan.UpdateContentAsync(name, content);
                    return $"Saved the {name} section";
                }),
            Tool.OfContext<string, string>("read_section",
                "Returns the text of one named section of the plan, or an empty string.",
                (ctx, name) => Task.FromResult(ctx.Thread.Plan.ReadSection(name)?.Content ?? "")),
        ];
    }
    #endregion
}
