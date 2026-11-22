using Ikon.Agent.Skills;
using Ikon.Agent.Storage.Postgres;
using Ikon.Connect.App;

namespace Ikon.App.Platform.Patterns.Examples;

// The agent-threads guide sections.

public static class AgentThreadExamples
{
    #region example:agent-durable-storage
    // Threads, plans and artifacts kept in the app's built-in database, so they survive a restart,
    // a deploy and a hot reload. Build it once, in OnStarting, then call ResumeAsync.
    public static async Task<Orchestrator> CreateDurableOrchestratorAsync(IAppBase app)
    {
        var storage = new PostgresStorage((await app.EnsureDefaultDatabaseAsync()).ConnectionString);
        await storage.EnsureSchemaAsync();
        return new Orchestrator(storage);
    }
    #endregion

    #region example:agent-brain
    // Claude Code on this computer answers the thread, through the persona's own tools; on a deployed
    // app, pick a computer a person connected from app.Computers.For(userId) instead
    public static async Task AnswerWithClaudeCodeAsync(IAppBase app, AgentThread thread)
    {
        if (app.Computers.Local is { } computer && computer.CanRun("claude-code"))
        {
            await thread.UseBrainAsync(Brain.ClaudeCode(computer));
        }

        await thread.DriveAsync(DriveMode.UntilQuiescent);
    }

    // A coder: Claude Code with all of its own tools, working in a checkout on that computer
    public static Brain CoderOn(Computer computer, string checkout) =>
        Brain.ClaudeCode(computer) with { WorkingDirectory = checkout, OwnTools = OwnTools.All };

    // An operator: acts through the app's own MCP server, reads files freely, asks a person before
    // anything else, and the page shows each step and each message as the agent writes it. Build it
    // once per thread: a new object with new delegates restarts the agent's process.
    public static ComputerBrain OperatorOn(Computer computer, string appMcpUrl, Func<ToolPermission, CancellationToken, Task<bool>> askPerson, Action<string> showStep) =>
        Brain.ClaudeCode(computer) with
        {
            OwnTools = OwnTools.All,
            McpServers = [new SessionMcpServer("app", appMcpUrl)],
            Approve = async (permission, ct) => permission.Tool is "Read" or "Grep" or "Glob" || await askPerson(permission, ct)
                ? ToolAnswer.Allowed
                : ToolAnswer.Refused("The person declined this; ask them in your reply what to do instead."),
            Watch = update =>
            {
                if (update is SessionToolUse use)
                {
                    showStep($"Using {use.Tool}");
                }
            },
            PostEveryMessage = true,
        };
    #endregion

    #region example:agent-computer-session
    // Claude Code works on a task in a folder on the computer, and the app shows what it does. The task's
    // id names the session, so after a restart Open follows the same one again.
    public static async Task<string?> StartCodingTaskAsync(IAppBase app, Computer computer, string taskId, string folder, string brief, Action<string> show)
    {
        ComputerSession? session = null;
        session = app.Computers.Open(computer.Id, taskId, update =>
        {
            switch (update)
            {
                case SessionText text:
                    show(text.Text);
                    break;

                case SessionToolUse use:
                    show($"Using {use.Tool}");
                    break;

                case SessionRequested { Request: ToolPermission permission }:
                    // Reading is fine; anything else waits for a person in a real app
                    _ = session!.AnswerAsync(permission, allow: permission.Tool is "Read" or "Grep" or "Glob");
                    break;

                case SessionTurnEnded ended:
                    show($"Done (${ended.CostUsd ?? 0:0.00}): {ended.Reply}");
                    break;
            }
        });

        var brain = Brain.ClaudeCode(computer) with { WorkingDirectory = folder, OwnTools = OwnTools.All };
        return await session.StartAsync(brain, new SessionStartInfo(brief) { Conversation = taskId });
    }
    #endregion

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
            // maxPasses already bounds the passes; a Stop budget also ends a child that runs too long.
            budget: new Budget(BudgetAction.Stop, MaxTurns: 6, MaxWallTime: TimeSpan.FromMinutes(2)),
            ct: ct);
    }
    #endregion

    #region example:agent-subagent-json
    public sealed record Finding(string Label, double X, double Y);

    public static Task<List<Finding>> FindDamageAsync(AgentThread parent, Content.Image photo, CancellationToken ct)
    {
        return AgentCall.RunSubAgentAsync<List<Finding>>(
            parent,
            instructions: "List the damage you see. Answer with only a JSON array: " +
                "[{\"label\": \"short name\", \"x\": 0.5, \"y\": 0.5}], x and y from 0 to 1 from the top-left corner.",
            skills: [],
            inputs: photo,
            maxPasses: 3,
            budget: new Budget(BudgetAction.Stop, MaxTurns: 3, MaxWallTime: TimeSpan.FromMinutes(2)),
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

    #region example:agent-stage-drive-to-done
    // A pass that answers in text and calls no MoveTo tool ends the drive as Settled before the last
    // stage, so post a short user turn and drive again, a few times at most.
    public static async Task<DriveOutcome> DriveToDoneAsync(AgentThread thread, CancellationToken ct)
    {
        for (var nudges = 0; ; nudges++)
        {
            var outcome = await thread.DriveAsync(DriveMode.UntilStable, ct: ct);

            // Done, a wait for the person's input, a failure or the safety limit: not a stall to nudge
            var stalled = outcome == DriveOutcome.Settled
                && thread.Stage.Value != "Done"
                && thread.Status.Value is ThreadStatus.Active or ThreadStatus.Idle;

            if (!stalled || nudges == 3)
            {
                return outcome;
            }

            await thread.PostAsync(new Message(Author.User, [new Content.Text("Continue with the next step.")]), ct);
        }
    }
    #endregion

    #region example:agent-pass-budget
    // MaxTurns counts passes, so with UntilStable it stops a pipeline that keeps calling tools: the
    // thread fails with "budget exceeded: MaxTurns" and DriveAsync returns DriveOutcome.Failed.
    public static async Task<DriveOutcome> RunPipelineAsync(Orchestrator orchestrator, AgentThread thread, CancellationToken ct)
    {
        orchestrator.AddPersona(new Persona("pipeline", "Work through the steps, then call done.", [Built.Termination],
            new Reasoning(), Budget: new Budget(BudgetAction.Stop, MaxTurns: 8)));

        return await thread.DriveAsync(DriveMode.UntilStable, ct: ct);
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

    #region example:agent-stage-transition
    // The body of IStageMachine.TryTransitionAsync. It runs after each completed pass and decides from
    // the app's own state (here two flags a tool set), because the event is always AgentCompleted.
    public static Task<StageTransitionResult<DispatchStage>?> DecideDispatchStageAsync(DispatchStage current, bool intakeDone, bool routeApproved)
    {
        StageTransitionResult<DispatchStage>? result = current switch
        {
            DispatchStage.Intake when intakeDone => new(DispatchStage.Routing),
            DispatchStage.Routing when routeApproved => new(DispatchStage.Confirmation, ThreadTransition.WaitForInput, Notes: "Route approved"),
            _ => null,
        };

        return Task.FromResult(result);
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
    // Call in a handler (a render cannot await) and keep the result in a reactive the render reads.
    public static async Task<UserDecisionPrompt?> PendingPromptAsync(AgentThread thread)
    {
        // Answering leaves the prompt stored, so only a waiting thread has a question pending
        if (thread.Status.Value != ThreadStatus.WaitingForInput)
        {
            return null;
        }

        return await UserDecisionProtocol.TryReadPromptAsync(thread);
    }

    public static void RenderPrompt(UIView view, AgentThread thread, UserDecisionPrompt prompt, ClientReactive<string> typedAnswer)
    {
        view.Text([Text.Body], text: prompt.Question);

        // A clarification has no options: without a text field the thread waits for ever
        if (prompt.Kind == "clarification")
        {
            view.TextField(label: "Your answer", bind: typedAnswer);
            view.Button([Button.PrimaryMd], text: "Send",
                onClick: async () => await AnswerAsync(thread, typedAnswer.Value));
            return;
        }

        for (var i = 0; i < prompt.Options.Count; i++)
        {
            var choice = prompt.Options[i];
            view.Button([Button.SecondaryMd], text: prompt.LabelAt(i),
                onClick: async () => await AnswerAsync(thread, choice));
        }
    }

    private static async Task AnswerAsync(AgentThread thread, string answer)
    {
        await thread.PostAsync(UserDecisionProtocol.BuildResponse(answer));
        await thread.DriveAsync(DriveMode.UntilStable);
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

    #region example:agent-artifact-tool
    // Tool.OfContext passes the tool's ToolContext as the first parameter, ctx, ahead of the model's arguments
    public static Tool SaveShortlistTool() =>
        Tool.OfContext<string, string[], string>("save_shortlist",
            "Saves the shortlist for one topic, replacing an earlier one.",
            async (ctx, topic, items) =>
            {
                await ctx.WriteArtifactAsync($"shortlist-{topic}", items);
                return $"Saved {items.Length} items for {topic}";
            });
    #endregion
}
