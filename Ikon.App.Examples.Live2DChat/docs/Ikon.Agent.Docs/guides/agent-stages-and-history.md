# Agent Stages and History

## Agent Stages and History

### Stage Machines

A long run rarely wants one prompt and one tool set throughout. An `IStageMachine<TState>` — where
`TState` is your own enum — gives each stage its own skills and instructions, and decides when to
move on.

`SkillsFor(thread, state)` returns the `SkillSet` for a stage: a `Name`, the `Skills`, optional
extra `Instructions`, and a `ReasoningOverride`. That override is the useful part — the runner
resolves the pass's model and temperature from it instead of the persona's own `Reasoning`, so
drafting stages can sit on a cheap fast tier while verdict stages stay on a strong one, per pass,
without splitting the persona in two.

`TryTransitionAsync(thread, current, evt, ct)` returns a `StageTransitionResult<TState>` — the
`NextStage`, an optional `StatusOverride` applied *before* the pass's automatic go-idle so it wins
over the default, and `Notes` attached to the just-completed pass for run analysis. Return null to
stay put with no override.

Three optional hooks matter for cost and for resumption:

- `ContextExclusions(thread, state)` names thread-message indexes to keep OUT of the LLM context —
  superseded content the machine knows is dead weight. **Compute it once per stage entry and return
  the same frozen set for every pass in that stage**, or the changing prefix defeats prompt caching.
- `OnPassStartingAsync` runs before every pass and ahead of context assembly, which is the hook for
  once-per-run seeding that must land in the cached prefix. Implementations dedupe with their own
  per-run state.
- `TryResumeWithoutPassAsync` advances a reactivated thread with no LLM pass at all — a user
  approval routing straight to the next stage. Return a transition to apply it and skip the pass,
  or null to fall through to a normal pass.

Stage machines are registered per orchestrator and shared by every thread on it, which is why every
method takes the `thread`: that parameter is what lets an implementation hold per-run state without
leaking it between runs.

### What a Pass Recorded

A pass record is the complete, self-contained account of one turn — the model and sampling, the
exact system prompt and context given to the model, the assistant reply, and every tool call with
full arguments and results. `StageBefore`/`StageAfter` carry the FSM stage on stage-machine runs and
are null otherwise, and non-text content is omitted.

Its parts: a `PassMessage` is one context entry (`Role` — `"user"` or the agent name — and `Text`),
and it is the model's *input*, not the reply. A `PassToolCall` is `CallId`, `Name`,
`ParametersJson` and `Result`. A `ContextSnapshot` is a system prompt plus history, with an `Empty`
for the starting case. `StreamedContent` counts what a pass streamed, split into text, reasoning and
tool-argument characters — the number to watch when a run feels expensive but its turn count looks
reasonable.

### The Journal

Every settled event is appended to a per-thread journal as a `JournalEntry` — a monotonic
`Sequence`, the thread id, a timestamp, a `Kind` and the `ThreadEvent` itself. `JournalCodec` is the
codec: `IsJournalable(evt)` excludes the high-frequency streaming events (`TextDelta`,
`TokenUsageUpdated`, `Progress`) as live-only, `Kind` and `Encode` write, and `Decode` **throws on
an unknown kind rather than silently dropping a slice of history**. The encoded payload deliberately
omits the bulk — binary message and artifact bodies, and a pass's system prompt and context window —
because the message, artifact and run-log stores are the system of record for those.

### Snapshots and Checkpoints

The three snapshot records are the flat, serializable views. `AppSnapshot` is id, name, brief,
status and timestamps. `PlanSnapshot` adds the app id, the `Sections` map and the rolled-up `Score`.
`ThreadSnapshot` is id, plan id, agent name, an optional parent id, status, the current `Stage` and
`StageMachineName`, `Usage`, and timestamps.

A `ThreadCheckpoint` bundles a `Label` and `CapturedAt` with a thread snapshot, its plan snapshot,
its messages and its artifacts — everything needed to reconstruct a run for review or a bug report.
`Checkpoints.WriteAsync(checkpoint, path)` creates the parent directory and overwrites; `ReadAsync`
brings it back.
