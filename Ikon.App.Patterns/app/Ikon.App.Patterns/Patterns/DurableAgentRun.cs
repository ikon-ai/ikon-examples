namespace Ikon.App.Patterns.Patterns;

// Pattern: durable-agent-run — see docs/patterns/durable-agent-run.md.
internal sealed class DurableAgentRun : IPatternDemo
{
    public string Slug => "durable-agent-run";
    public string Title => "Durable agent run";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend AI pattern: agent runs kept in Postgres, each driven by whichever instance holds its lease, so a run interrupted by a restart or a deploy continues from its last saved step — see docs/patterns/durable-agent-run.md.");
}

// Abstract so neither the bundle scan nor the Patterns app registers its [Cron] job or creates its
// table: the gallery runs no agents.
file abstract class DurableAgentRunExample(IAppBase app)
{
    #region example:pattern-durable-agent-run
    private const string PersonaName = "researcher";
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(90);

    // The owner this instance writes into the lease of the run rows it drives. Every instance, and
    // every restart of one, is a new owner.
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private Ikon.Agent.IStorage _storage = null!;

    /// <summary>Call once from Main.</summary>
    private async Task StartAgentRunsAsync()
    {
        var storage = new PostgresStorage((await app.EnsureDefaultDatabaseAsync()).ConnectionString);
        await storage.EnsureSchemaAsync();
        _storage = storage;

        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS agent_runs (
                id text PRIMARY KEY,
                task text NOT NULL,
                status text NOT NULL DEFAULT 'running',
                lease_owner text,
                lease_until timestamptz)
            """);

        // Not awaited: OnStarting must return for the instance to finish starting
        app.OnStarting(async () =>
        {
            if (SnapshotCapture.IsCaptureProcess)
            {
                return;
            }

            _ = DriveUnfinishedRunsAsync();
        });
    }

    private async Task StartRunAsync(string task)
    {
        await using (var connection = await app.DatabaseAsync())
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("INSERT INTO agent_runs (id, task) VALUES (@id, @task)",
                new { id = Guid.NewGuid().ToString("N"), task });
        }

        await DriveUnfinishedRunsAsync();
    }

    // A cron tick starts the app's shared instance when none is running, so a run interrupted while
    // nobody was connected continues within five minutes. In a running instance, the tick picks up
    // the runs of an instance that stopped.
    [Cron("*/5 * * * *")]
    public Task ResumeAgentRunsAsync() => DriveUnfinishedRunsAsync();

    private async Task DriveUnfinishedRunsAsync()
    {
        IEnumerable<(string Id, string Task)> claimed;

        try
        {
            await using var connection = await app.DatabaseAsync();
            await connection.OpenAsync();
            // A lease that ran out belongs to an instance that stopped without releasing it
            claimed = await connection.QueryAsync<(string Id, string Task)>("""
                UPDATE agent_runs SET lease_owner = @owner, lease_until = now() + make_interval(secs => @seconds)
                WHERE status = 'running' AND (lease_until IS NULL OR lease_until < now())
                RETURNING id, task
                """, new { owner = _instanceId, seconds = Lease.TotalSeconds });
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Could not claim unfinished agent runs, the next cron tick tries again: {ex.Message}");
            return;
        }

        foreach (var (id, task) in claimed)
        {
            _ = DriveRunAsync(id, task);
        }
    }

    private async Task DriveRunAsync(string runId, string task)
    {
        await using var hold = await app.BackgroundWork.StartAsync(TimeSpan.FromMinutes(30), $"agent run {runId}");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(app.StoppingToken);
        var renewing = RenewLeaseAsync(runId, stop);
        string? outcome = null;

        try
        {
            // A new orchestrator scoped to this run's records loads the thread as the last instance
            // to drive it left it, so no instance drives an out-of-date copy.
            await using var orchestrator = new Orchestrator(Storages.Scoped(_storage, $"run:{runId}"));
            orchestrator.AddPersona(new Persona(
                Name: PersonaName,
                SystemPrompt: "Research the task with web search and finish with a short, sourced summary.",
                Skills: [Built.WebSearch],
                Reasoning: new Reasoning(Capability.Standard, ModelFamily.Claude),
                Budget: new Budget(BudgetAction.Stop, MaxCredits: 5)));
            await orchestrator.ResumeAsync(includeTerminal: true, stop.Token);

            var run = await orchestrator.CreateAppAsync("run", id: $"run:{runId}", ct: stop.Token);
            var plan = run.Plans.Value.FirstOrDefault()
                ?? await run.CreatePlanAsync("main", PersonaName, new Content.Text(task), ct: stop.Token);

            var result = await plan.Thread.DriveAsync(DriveMode.UntilQuiescent,
                onPass: async _ => await hold.ExtendAsync(TimeSpan.FromMinutes(30)), ct: stop.Token);
            outcome = result == DriveOutcome.Settled ? "done" : "failed";
        }
        catch (Exception ex) when (ex is OperationCanceledException || stop.IsCancellationRequested)
        {
            // The instance is stopping, or another instance took the run over. A stop can end the
            // drive with a cancellation before the stopping token fires, so a cancellation counts as
            // a stop too. The run stays 'running', and the next instance to take its lease continues
            // it from its last saved step
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Agent run {runId} failed: {ex.Message}");
            outcome = "failed";
        }
        finally
        {
            await stop.CancelAsync();
            await renewing;
            await ReleaseLeaseAsync(runId, outcome);
        }
    }

    private async Task RenewLeaseAsync(string runId, CancellationTokenSource stop)
    {
        using var timer = new PeriodicTimer(Lease / 3);

        try
        {
            while (await timer.WaitForNextTickAsync(stop.Token))
            {
                await using var connection = await app.DatabaseAsync();
                await connection.OpenAsync();
                var renewed = await connection.ExecuteAsync("""
                    UPDATE agent_runs SET lease_until = now() + make_interval(secs => @seconds)
                    WHERE id = @runId AND lease_owner = @owner
                    """, new { runId, owner = _instanceId, seconds = Lease.TotalSeconds });

                // Another instance took the lease after a renewal failed to arrive in time
                if (renewed == 0)
                {
                    await stop.CancelAsync();
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Without renewals the lease runs out and another instance takes the run, so this one stops driving it
            Log.Instance.Warning($"Agent run {runId} could not renew its lease and stops so that another instance can resume it: {ex.Message}");
            await stop.CancelAsync();
        }
        catch (OperationCanceledException)
        {
            // The run ended or the instance is stopping
        }
    }

    private async Task ReleaseLeaseAsync(string runId, string? outcome)
    {
        try
        {
            // Releasing at once lets the next instance take an interrupted run without waiting for the lease to run out
            await using var connection = await app.DatabaseAsync();
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                UPDATE agent_runs SET status = COALESCE(@outcome, status), lease_owner = NULL, lease_until = NULL
                WHERE id = @runId AND lease_owner = @owner
                """, new { runId, outcome, owner = _instanceId });
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Could not release the lease of agent run {runId}; it runs out in {Lease.TotalSeconds:0} s: {ex.Message}");
        }
    }
    #endregion
}
