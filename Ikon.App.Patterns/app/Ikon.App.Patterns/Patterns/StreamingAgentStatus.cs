namespace Ikon.App.Patterns.Patterns;

// Pattern: streaming-agent-status — see docs/patterns/streaming-agent-status.md.
// The orchestrator and selected-thread reactive stand in for the caller's real agent wiring; the
// live block reads the running thread's own reactives, so touching them inside the lambda subscribes.
// The demo drives a real AgentThread with a scripted model stream in place of a provider, so a run
// costs nothing and still goes through the runner that sets the activity, the tool-call timeline
// and the usage the block reads.
internal sealed class StreamingAgentStatus : IPatternDemo
{
    public string Slug => "streaming-agent-status";
    public string Title => "Streaming agent status";
    public string Category => "AI";

    private const string DemoPersona = "trip-planner";

    private readonly Orchestrator _orchestrator;
    private readonly Reactive<string?> _selectedThreadId = new((string?)null);
    private readonly Reactive<bool> _demoRunning = new(false);
    private int _demoRuns;
    private int _scriptStep;

    public StreamingAgentStatus()
    {
        _orchestrator = new Orchestrator(llm: ScriptedModelAsync)
            .AddPersona(new Persona(
                Name: DemoPersona,
                SystemPrompt: "",
                Skills: [new DemoTravelSkill()],
                Reasoning: new Reasoning()));
    }

    public void RenderDemo(IView view)
    {
        view.Column(["gap-2 max-w-xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"],
                text: _demoRunning.Value ? "Agent running…" : "Run the trip-planner agent",
                disabled: _demoRunning.Value,
                onClick: async () =>
                {
                    _demoRunning.Value = true;
                    _ = RunDemoAsync();
                });
            col.Text(["text-xs text-zinc-400"], "A scripted model stands in for the provider: two lookups succeed, the hotel hold fails, then the agent answers");
            Render(col);
        });
    }

    private sealed class DemoTravelSkill : Skill
    {
        public override string Name => "travel";

        public override IEnumerable<Tool> Tools() =>
        [
            Tool.Of<string, string>("search_flights", "Search direct flights for a route", async route =>
            {
                await Task.Delay(2500);
                return $"3 direct flights {route}, from 189 EUR";
            }),
            Tool.Of<string, string>("check_calendar", "Read the traveller's calendar for a week", async week =>
            {
                await Task.Delay(3200);
                return $"Free {week} except Thursday afternoon";
            }),
            Tool.Of<string, string>("hold_hotel_room", "Put a hotel room on hold", async hotel =>
            {
                await Task.Delay(2000);
                throw new InvalidOperationException($"{hotel} has no rooms left for 14-18 Oct");
            }),
        ];
    }

    private async Task RunDemoAsync()
    {
        try
        {
            Interlocked.Exchange(ref _scriptStep, 0);
            var thread = await _orchestrator.CreateThreadAsync(
                DemoPersona,
                new Content.Text("Plan a four-night trip from Helsinki to Lisbon in the week of 14 October"),
                planName: $"run-{Interlocked.Increment(ref _demoRuns)}");
            _selectedThreadId.Value = thread.Id;
            await thread.DriveAsync(DriveMode.UntilQuiescent);
        }
        finally
        {
            _demoRunning.Value = false;
        }
    }

    private async IAsyncEnumerable<LLMEvent> ScriptedModelAsync(KernelContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var step = Interlocked.Increment(ref _scriptStep);
        await Task.Delay(1200, ct);
        yield return new LLMEvent.Usage(InputTokens: 1400 + step * 650, CachedInputTokens: 0, CacheCreationInputTokens: 0, OutputTokens: 90 + step * 40);

        switch (step)
        {
            case 1:
                yield return DemoCall(context, "search_flights", "HEL-LIS", step * 10 + 1);
                yield return DemoCall(context, "check_calendar", "week 42", step * 10 + 2);
                break;
            case 2:
                yield return DemoCall(context, "hold_hotel_room", "Casa do Largo", step * 10 + 1);
                break;
            default:
                foreach (var chunk in new[] { "Flights on the 14th and 18th fit your calendar. ", "Casa do Largo is full, ", "so I would book Memmo Alfama instead." })
                {
                    await Task.Delay(400, ct);
                    yield return new LLMEvent.TextDelta(chunk);
                }

                break;
        }
    }

    private static LLMEvent DemoCall(KernelContext context, string tool, string argument, int index)
    {
        var function = context.Functions[tool];
        var json = JsonSerializer.Serialize(new Dictionary<string, string> { [function.Parameters[0].Name] = argument });
        return new LLMEvent.ToolCallRequested(new FunctionCall(function, [argument], json, $"call-{index}", $"hash-{index}"));
    }

    private void Render(IView view)
    {
        #region example:pattern-streaming-agent-status
        var thread = _selectedThreadId.Value is { } id ? _orchestrator.GetThread(id) : null;

        if (thread is not null && thread.Status.Value == ThreadStatus.Active)
        {
            var activity = thread.Activity.Value;
            var usage = thread.Usage.Value;

            view.Box(["py-4 mt-3 px-5 rounded-xl bg-gradient-to-r from-muted/40 to-muted/20 shadow-sm"], content: view =>
            {
                view.Row(["items-center gap-3 mb-1"], content: view =>
                {
                    view.Text(["text-xs font-bold text-sky-600 dark:text-sky-400"], text: thread.AgentName);
                    view.Box(["flex-1"]);

                    var activityText = activity.Kind switch
                    {
                        ActivityKind.Thinking => "thinking",
                        ActivityKind.Streaming => "streaming",
                        ActivityKind.RunningTool => activity.Tool ?? "tool",
                        _ => "idle"
                    };
                    view.Text(["text-xs px-1 py-0.5 rounded font-mono text-muted-foreground bg-muted"], text: activityText);
                });

                if (thread.Stage.Value is { Length: > 0 } stage)
                {
                    view.Text(["text-xs text-muted-foreground/70 mb-1"], text: stage);
                }

                // ResultText is null until the call completes, so it is what says "in flight".
                // IsError is null on a finished entry too, when it was journaled before the flag
                // existed -- that is "unknown", and it must not read as a spinner or a check.
                foreach (var call in thread.ToolCallTimeline.Value)
                {
                    view.Row(["items-center gap-1.5 py-0.5"], key: $"{call.PrecedingAgentMessages}-{call.ToolName}", content: view =>
                    {
                        if (call.ResultText is null)
                        {
                            view.Spinner(["text-sky-400"], size: SpinnerSize.Sm);
                        }
                        else if (call.IsError == true)
                        {
                            view.Icon(["w-3 h-3 text-red-400"], name: "x");
                        }
                        else if (call.IsError == false)
                        {
                            view.Icon(["w-3 h-3 text-emerald-400"], name: "check");
                        }
                        else
                        {
                            view.Icon(["w-3 h-3 text-muted-foreground/50"], name: "minus");
                        }

                        view.Text(["text-xs text-muted-foreground font-mono"], text: call.ToolName);

                        if (!string.IsNullOrEmpty(call.ResultText))
                        {
                            view.Text(["text-xs text-muted-foreground/50 truncate"], text: call.ResultText);
                        }
                    });
                }

                view.Text(["text-xs text-muted-foreground/50 mt-1"],
                    text: $"turn {usage.Turns + 1} · {usage.InputTokens + usage.OutputTokens} tokens · {usage.WallTime.TotalSeconds:F0}s");
            });
        }
        #endregion
    }
}
