using System.Diagnostics;

public partial class Validation
{
    private readonly Reactive<bool> _profilingRunning = new(false);
    private readonly Reactive<int> _profilingUpdatesPerSecond = new(30);
    private readonly Reactive<long> _profilingCounter = new(0);
    private readonly Reactive<string> _profilingSummary = new("");
    private readonly Reactive<bool> _profilingSubtreeCaching = new(true);
    private readonly Reactive<bool> _profilingSubtreeRendering = new(true);

    private CancellationTokenSource? _profilingCts;

    // Wraps a heavy tab's content so it is skipped while profiling runs, isolating that
    // tab's per-frame render cost. All tabs render server-side on every frame, so skipping
    // the expensive ones (Memory's live process/GC introspection, Payments, Charts, Icons,
    // Ikon.AI) is the cleanest way to measure their contribution to render time. The skip is
    // automatic — a forgotten checkbox used to silently inflate every measurement.
    private Action<UIView> ProfilingSkippable(Action<UIView> render) => view =>
    {
        if (_profilingRunning.Value)
        {
            view.Text([Text.Caption], "(skipped while profiling is running)");
            return;
        }

        render(view);
    };

    private void RenderProfilingSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            RenderCpuBurnSection(view);

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "UI Profiling");
                view.Text([Text.Caption, "mb-4"], "Automated rapid UI updates to profile render performance");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                    {
                        view.Text([Text.Body], "Updates per second:");
                        view.Slider([Slider.Root, "flex-1"],
                            value: [(double)_profilingUpdatesPerSecond.Value],
                            min: 1,
                            max: 120,
                            step: 1,
                            disabled: _profilingRunning.Value,
                            onValueChange: async v => { if (v.Count > 0) _profilingUpdatesPerSecond.Value = (int)v[0]; },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                        view.Text([Text.Body, "w-12"], $"{_profilingUpdatesPerSecond.Value}");
                    });

                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        if (!_profilingRunning.Value)
                        {
                            view.Button([Button.PrimaryMd], text: "Start Profiling", onClick: StartProfilingAsync);
                        }
                        else
                        {
                            view.Button([Button.ErrorMd], text: "Stop Profiling", onClick: StopProfilingAsync);
                        }

                        view.Button([Button.OutlineMd], text: "Reset Stats", onClick: ResetProfilingStatsAsync);
                    });

                    if (_profilingRunning.Value)
                    {
                        view.Text([Text.Caption], "Expensive tabs (Memory, Payments, Charts, Icons, Ikon.AI) are disabled while profiling runs");
                    }

                    view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            value: _profilingSubtreeCaching.Value,
                            onValueChange: async v =>
                            {
                                _profilingSubtreeCaching.Value = v;
                                UI.EnableSubtreeCaching = v;
                                ResetProfilingStatsAfterModeChange();
                            },
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "check")));
                        view.Text([Text.Body], "Subtree caching");
                    });

                    view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            value: _profilingSubtreeRendering.Value,
                            onValueChange: async v =>
                            {
                                _profilingSubtreeRendering.Value = v;
                                UI.EnableSubtreeRendering = v;
                                // The per-container handle graph is built and torn down by a full
                                // root render; without one, the previous mode keeps running.
                                app.ReactiveGlobalState.Clients.NotifyUpdate();
                                ResetProfilingStatsAfterModeChange();
                            },
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "check")));
                        view.Text([Text.Body], "Subtree rendering (partial updates)");
                    });
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                // Profiler stats are not reactive; tie this subtree to the profiling tick and the
                // mode toggles so it refreshes on every update AND immediately on a mode change,
                // and read the stats HERE so each rebuild sees fresh values instead of a stale
                // closure from the enclosing render.
                _ = _profilingCounter.Value;
                _ = _profilingSubtreeRendering.Value;
                _ = _profilingSubtreeCaching.Value;
                var history = Profiler.History;
                var totalStats = history?.GetTotalStats() ?? default;

                view.Text([Text.H3, "mb-4"], "Live Metrics");

                view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
                {
                    RenderMetricCard(view, "Updates", $"{history?.SampleCount ?? 0}");
                    RenderMetricCard(view, "Avg (ms)", $"{totalStats.Avg:F2}");
                    RenderMetricCard(view, "Min (ms)", totalStats.Min > 0 ? $"{totalStats.Min:F2}" : "-");
                    RenderMetricCard(view, "Max (ms)", $"{totalStats.Max:F2}");
                    RenderMetricCard(view, "P95 (ms)", $"{totalStats.P95:F2}");
                    RenderMetricCard(view, "P99 (ms)", $"{totalStats.P99:F2}");
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                _ = _profilingCounter.Value;
                _ = _profilingSubtreeRendering.Value;
                _ = _profilingSubtreeCaching.Value;
                view.Text([Text.H3, "mb-4"], "Detailed Phase Breakdown");
                RenderPhaseBreakdownTable(view, Profiler.History);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Profiling Content");
                view.Text([Text.Caption, "mb-2"], "This area updates rapidly during profiling");
                view.Box(["border border-secondary rounded p-4"], content: view =>
                {
                    view.Text([Text.H1], $"Counter: {_profilingCounter.Value}");
                    view.Text([Text.Body], $"Running: {_profilingRunning.Value}");
                    view.Text([Text.Caption], $"Target: {_profilingUpdatesPerSecond.Value} updates/sec");
                });
            });
        });
    }

    private static void RenderPhaseBreakdownTable(UIView view, ProfileHistory? history)
    {
        if (history == null || history.SampleCount == 0)
        {
            view.Text([Text.Caption], "(no data yet - start profiling)");
            return;
        }

        view.Box(["overflow-x-auto"], content: view =>
        {
            view.Box(["grid grid-cols-6 gap-2 text-sm font-mono"], content: view =>
            {
                view.Text([Text.Caption, "font-bold"], "Phase");
                view.Text([Text.Caption, "font-bold text-right"], "Avg");
                view.Text([Text.Caption, "font-bold text-right"], "Min");
                view.Text([Text.Caption, "font-bold text-right"], "Max");
                view.Text([Text.Caption, "font-bold text-right"], "P95");
                view.Text([Text.Caption, "font-bold text-right"], "P99");

                var totalStats = history.GetTotalStats();
                view.Text([Text.Body, "font-bold"], "Total");
                view.Text([Text.Body, "text-right"], $"{totalStats.Avg:F2}");
                view.Text([Text.Body, "text-right"], $"{totalStats.Min:F2}");
                view.Text([Text.Body, "text-right"], $"{totalStats.Max:F2}");
                view.Text([Text.Body, "text-right"], $"{totalStats.P95:F2}");
                view.Text([Text.Body, "text-right"], $"{totalStats.P99:F2}");

                foreach (var name in history.Names)
                {
                    var stats = history.GetStats(name);
                    view.Text([Text.Caption], name);
                    view.Text([Text.Caption, "text-right"], $"{stats.Avg:F2}");
                    view.Text([Text.Caption, "text-right"], $"{stats.Min:F2}");
                    view.Text([Text.Caption, "text-right"], $"{stats.Max:F2}");
                    view.Text([Text.Caption, "text-right"], $"{stats.P95:F2}");
                    view.Text([Text.Caption, "text-right"], $"{stats.P99:F2}");
                }
            });
        });
    }

    private static void RenderMetricCard(UIView view, string label, string value)
    {
        view.Box([Card.Default, "p-4 min-w-[120px]"], content: view =>
        {
            view.Text([Text.Caption], label);
            view.Text([Text.H2], value);
        });
    }

    // A render-mode change invalidates accumulated samples: mixing both modes' timings makes the
    // stats meaningless and leaves the old mode's phase rows lingering in the breakdown table.
    // Recording pauses through the transition (the mode switch itself still renders frames under
    // the OLD mode's handle graph) and the reset lands once the switch has settled.
    private void ResetProfilingStatsAfterModeChange()
    {
        Profiler.PauseHistory();

        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            Profiler.ResetHistory();
            _profilingCounter.Value = 0;

            if (_profilingRunning.Peek)
            {
                Profiler.ResumeHistory();
            }
        });
    }

    private async Task StartProfilingAsync()
    {
        if (_profilingRunning.Value)
        {
            return;
        }

        UI.EnableProfiling = true;
        Profiler.EnableHistory(1000);
        Profiler.ResumeHistory();
        _profilingRunning.Value = true;
        _profilingCounter.Value = 0;
        _profilingCts = new CancellationTokenSource();

        _ = RunProfilingLoopAsync(_profilingCts.Token);
    }

    private async Task StopProfilingAsync()
    {
        Profiler.PauseHistory();
        _profilingCts?.Cancel();
        _profilingCts = null;
        _profilingRunning.Value = false;
        UI.EnableProfiling = false;
    }

    private async Task ResetProfilingStatsAsync()
    {
        Profiler.ResetHistory();
        _profilingCounter.Value = 0;
    }

    private async Task RunProfilingLoopAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            var interval = 1000.0 / _profilingUpdatesPerSecond.Value;
            var nextTick = sw.Elapsed.TotalMilliseconds + interval;

            _profilingCounter.Value++;

            var remaining = nextTick - sw.Elapsed.TotalMilliseconds;

            if (remaining > 0)
            {
                try
                {
                    await Task.Delay((int)remaining, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
