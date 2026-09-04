# Agent Threads

## Agent Threads

`Emerge.Run<T>` is one LLM call. `Ikon.Agent` is what you reach for when the work is a *run* — many
turns, tools, artifacts that accumulate, sub-agents, and state that has to survive a restart.

Four nested things: an **Orchestrator** holds the registered personas and stage machines and owns
persistence; an **AgentApp** is one workspace; an **AgentPlan** is one piece of work inside it, with
a 1:1 working **AgentThread** that actually runs. Construct the orchestrator synchronously and call
`ResumeAsync` once at startup to re-hydrate persisted apps, plans and threads before driving
anything.

`AgentApp` exposes `Name` and `Brief` as reactives (bind them straight into a view), `Plans` as the
active subset in creation order and `AllPlans` as a point-in-time snapshot including archived ones,
and a `Status` that is only ever `Active` or `Archived` — apps have none of the richer state machine
threads do. `CreatePlanAsync` makes a plan *and* its working thread, running the named persona
seeded with your task. `ArchiveAsync` cascades to every owned plan and is reversible with
`RestoreAsync`; `RemovePlanAsync` deletes from durable storage and is not.

A plan's `Sections` are `PlanSection` records (`Content` plus an optional `Score`), and the plan's
own `Score` is the minimum of the sections that have one, or null when none do.

### Creating a Thread

```csharp
public static Task<AgentPlan> StartReviewAsync(AgentApp app, CancellationToken ct)
{
    // StageMachineName must already be registered on the orchestrator, and InitialStage may not
    // be supplied without it — either mistake throws InvalidOperationException at creation.
    var options = new ThreadOptions(StageMachineName: "review", InitialStage: "Drafting");

    return app.CreatePlanAsync("Quarterly review", "reviewer", new Content.Text("Review Q3"), options, ct);
}
```

`ThreadOptions` carries the three creation-time choices, and two of them throw rather than warn: a
`StageMachineName` that is not registered on the orchestrator throws `InvalidOperationException` at
creation, and so does an `InitialStage` supplied without one. `InitialStatus` defaults to `Active`;
a thread created explicitly as `Pending` must have its first turn driven through
`EnsureActiveAsync`/`DriveAsync` rather than `ReactivateIfIdleAsync`. A `SpawnSpec` is the same
three things (persona name, seed task, options) as a value you can pass around, and `WaitMode`
(`All` or `Any`) says whether waiting on several threads means all of them or the first.

### Sub-Agents

`AgentCall.RunSubAgentAsync` spawns a child under the current thread and drives it until your
`extract` returns non-null:

```csharp
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
```

The inline persona it builds is registered for the call and evicted afterwards, so nothing needs
`Orchestrator.AddPersona` — but it is in-memory only, so a sub-agent that must survive a process
restart needs a name-registered persona and the `AgentCall.RunAsync` overload. Each call nests one
level deeper in the spawn tree, so bound recursion with `Budget.MaxDepth`. Fanning out in parallel
through `Task.WhenAll` is safe: each call gets a distinct inline persona name.

`AgentCallSpec<T>` is the typed form — `SeedTask`, a `PrimeAsync` that posts inputs the seed task
does not carry (called once, before any pass), an `ExtractResult` called after each pass, a
`MaxPasses` ceiling, and an `OnComplete` that fires once on the way out **on both the success and
the failure path** with an `AgentCallRecord`: persona, result type, whether it succeeded, the child
thread id, turns, the four token counts, wall time and start. The call throws
`InvalidOperationException` if no result appears within `MaxPasses`; the persona's own budget may
stop it sooner.

### Artifacts

An `Artifact` is a named, typed piece of work product on a thread — `Parts` are `Content`, and
`Source` is an `ArtifactSource` of `Agent`, `User` or `System`, so the origin of a file is never
guesswork. `AttachmentInput` (bytes, mime type, optional name) is what a host hands in when a person
attaches a file.

Images cost context, so a pass may show only so many: `PassMediaBudget.TryClaim(name, out
alreadyShown)` returns false both when the per-pass ceiling (`MaxImagesPerPass`, 3) is spent and
when that artifact was already shown this pass — `alreadyShown` is what distinguishes "scroll up" from
"no headroom", and `HasHeadroom` asks in advance.

### Budgets

`BudgetRemaining` is the live view of what a run has left: input, cached-input, cache-creation and
output tokens, wall time and turns, each null when no cap is set, plus the `Effective` budget —
the merged host and per-persona cap the runtime is actually enforcing. When one runs out,
`BudgetAction` decides what happens: `Stop`, `Continue`, or `AskUser`.

### Asking the User

An agent that needs a person's decision writes a `UserDecisionPrompt` — a `Question`, the `Options`,
and a `Kind`. The host reads it with `UserDecisionProtocol.TryReadPromptAsync`, renders the choice,
and posts the answer back as a message built with `BuildResponse(choice)`; the agent picks it up on
its next pass. `TryReadResponse` reads a `UserDecisionResponse` off an incoming message.

```csharp
public static async Task<string?> PendingQuestionAsync(AgentThread thread)
{
    var prompt = await UserDecisionProtocol.TryReadPromptAsync(thread);

    return prompt is null ? null : $"{prompt.Question} ({string.Join(" / ", prompt.Options)})";
}
```

`RuntimeMessages.NudgePayloadKind` marks the other kind of injected message: a user-role corrective
the runner posts to recover a drive. It steers the agent and is never part of the user's
conversation, so filter it out of anything you render as chat.

`ModelResolver` is why the agent layer never names a concrete model — `Resolve(reasoning)` and
`Resolve(capability, family)` turn intent into an `LLMModel` per pass. `AgentScopes.AgentRun` is the
log scope a run is stamped with, which is also what makes its cost attributable through `app.Costs`.
