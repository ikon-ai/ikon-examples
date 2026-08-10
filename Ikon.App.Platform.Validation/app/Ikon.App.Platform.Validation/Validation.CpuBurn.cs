using System.Diagnostics;

public partial class Validation
{
    // A run is capped, and the session has a total budget on top of it. This app is reachable by
    // anyone who has the URL and its container's core-seconds are metered and billed to us, so the
    // control disarms itself rather than relying on whoever pressed it to press stop. The budget is
    // per app session and never refills: the worst an abuser gets is one budget's worth per session.
    private const int CpuBurnRunSeconds = 30;
    private const int CpuBurnSessionBudgetSeconds = 300;

    private readonly Reactive<bool> _cpuBurnRunning = new(false);
    private readonly Reactive<int> _cpuBurnSpentSeconds = new(0);
    private readonly Reactive<string> _cpuBurnStatus = new("(idle)");

    private CancellationTokenSource? _cpuBurnCts;

    /// <summary>
    /// Pins one core for a fixed span so metered core-seconds can be checked against a number that
    /// is known rather than merely plausible: a run should meter about <see cref="CpuBurnRunSeconds"/>
    /// core-seconds, and `throttled_usec` should move if the container's CPU limit is below one core.
    /// </summary>
    private void RenderCpuBurnSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "CPU Burn");
            view.Text([Text.Caption, "mb-4"],
                $"Pins one core for {CpuBurnRunSeconds} seconds so metered core-seconds can be compared against a known quantity. " +
                $"Stops on its own, and the whole session is capped at {CpuBurnSessionBudgetSeconds} core-seconds.");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Status");
                    view.Text([Text.Body], _cpuBurnStatus.Value);
                });

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Budget");
                    view.Text([Text.Body], $"{_cpuBurnSpentSeconds.Value} of {CpuBurnSessionBudgetSeconds} core-seconds used");
                });

                view.Row([Layout.Row.Md], content: view =>
                {
                    bool exhausted = _cpuBurnSpentSeconds.Value + CpuBurnRunSeconds > CpuBurnSessionBudgetSeconds;

                    view.Button([Button.PrimaryMd],
                        text: $"Burn one core for {CpuBurnRunSeconds}s",
                        disabled: _cpuBurnRunning.Value || exhausted,
                        onClick: StartCpuBurnAsync);

                    view.Button([Button.ErrorMd],
                        text: "Stop",
                        disabled: !_cpuBurnRunning.Value,
                        onClick: async () => StopCpuBurn());
                });

                if (_cpuBurnSpentSeconds.Value + CpuBurnRunSeconds > CpuBurnSessionBudgetSeconds)
                {
                    view.Text([Text.Caption], "This session has spent its burn budget. Reconnect to get another one.");
                }
            });
        });
    }

    private async Task StartCpuBurnAsync()
    {
        if (_cpuBurnRunning.Value || _cpuBurnSpentSeconds.Value + CpuBurnRunSeconds > CpuBurnSessionBudgetSeconds)
        {
            return;
        }

        _cpuBurnCts?.Cancel();
        _cpuBurnCts = new CancellationTokenSource();
        var token = _cpuBurnCts.Token;

        _cpuBurnRunning.Value = true;
        _cpuBurnStatus.Value = $"Burning one core for {CpuBurnRunSeconds}s";

        _ = Task.Run(() => RunCpuBurnAsync(token));
    }

    private void StopCpuBurn()
    {
        _cpuBurnCts?.Cancel();
    }

    private async Task RunCpuBurnAsync(CancellationToken cancellationToken)
    {
        var processStopwatch = Stopwatch.StartNew();
        var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
        var deadline = TimeSpan.FromSeconds(CpuBurnRunSeconds);

        try
        {
            // A tight loop on one thread. Deliberately not parallel: one core for a known span is
            // the measurement, and more threads only make the expected figure depend on how many
            // cores the container was actually given.
            while (processStopwatch.Elapsed < deadline && !cancellationToken.IsCancellationRequested)
            {
                // Spin in bursts so the deadline and the cancellation are still checked promptly
                Thread.SpinWait(200_000);
            }
        }
        finally
        {
            var burned = Process.GetCurrentProcess().TotalProcessorTime - startCpu;
            var wallSeconds = (int)Math.Ceiling(processStopwatch.Elapsed.TotalSeconds);

            _cpuBurnSpentSeconds.Value += wallSeconds;
            _cpuBurnRunning.Value = false;
            _cpuBurnStatus.Value = $"Burned {burned.TotalSeconds:F1} core-seconds over {processStopwatch.Elapsed.TotalSeconds:F1}s of wall clock";

            await Task.CompletedTask;
        }
    }
}
