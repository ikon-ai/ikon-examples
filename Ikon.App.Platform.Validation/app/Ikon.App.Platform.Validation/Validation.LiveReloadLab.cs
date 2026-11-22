public partial class Validation
{
    private const string LiveReloadLabTab = "live-reload-lab";

    // Set by the live reload rig's --gc-pressure: the lab then runs compacting collections without a pause
    private const string LiveReloadLabGCPressureVariable = "IKON_LIVE_RELOAD_LAB_GC_PRESSURE";
    private static readonly TimeSpan LiveReloadLabPumpInterval = TimeSpan.FromMilliseconds(250);

    private readonly Reactive<int> _labShared = new(0);
    private readonly ClientReactive<int> _labClient = new(0);
    private readonly Reactive<string> _labPressed = new("not pressed");
    private readonly Reactive<string> _labAsync = new("");
    private readonly Reactive<string> _labBackground = new("");
    private readonly Reactive<string> _labLoop = new("");
    private readonly Reactive<string> _labEffect = new("");
    private readonly Reactive<string> _labThread = new("");
    private readonly Reactive<string> _labSuspended = new("waiting");
    private readonly Reactive<string> _labMemory = new("not measured");

    // Let go by the Resume button, so the rig decides when the method that waits on it goes on
    private readonly SemaphoreSlim _labGate = new(0);

    // Not a reactive, which a restart would carry over: each instance writes it once as it starts
    private string _labStartup = "";

    // Made once per instance, so an edit applied to the running app does not construct them again
    private readonly LabThing _labThing = new();
    private readonly LabPrimary _labPrimary = new("primary-name", 3);
    private readonly LabBox<string> _labBox = new("boxed");

    // Hot reload exists only in a local run, and the lab's background work would cost every deployed instance
    private bool LiveReloadLabShown => app.GlobalState.ServerRunType == ServerRunType.Local;

    private IEnumerable<TabItem> LiveReloadLabTabs() =>
        LiveReloadLabShown ? [new TabItem(LiveReloadLabTab, "Live Reload Lab", RenderLiveReloadLabSection)] : [];

    private void InitLiveReloadLab()
    {
        if (!LiveReloadLabShown)
        {
            return;
        }

        var stop = new CancellationTokenSource();
        var effect = new ReactiveEffect(() => _labEffect.Value = LiveReloadLab.Effect(_labShared.Value), _labShared);

        app.OnStopping(async () =>
        {
            await stop.CancelAsync();
            effect.Dispose();
        });

        _labStartup = "startup-1";

        if (Environment.GetEnvironmentVariable(LiveReloadLabGCPressureVariable) == "1")
        {
            StartLiveReloadLabGCPressure(stop.Token);
        }

        _ = PumpLiveReloadLabAsync(stop.Token);
        _ = RunLiveReloadLabSuspendedAsync(stop.Token);
        _ = RunLiveReloadLabLoopAsync(stop.Token);
        _ = Task.Factory.StartNew(() => RunLiveReloadLabThread(stop.Token), stop.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    // A method that is entered once and loops without awaiting, so no edit to it ever starts to run
    private void RunLiveReloadLabThread(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            SetLab(_labThread, "thread-1");
            stop.WaitHandle.WaitOne(LiveReloadLabPumpInterval);
        }
    }

    // Code that runs on a timer and not in a render: an edit to what it calls shows at its next round
    private async Task PumpLiveReloadLabAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            string background;
            string asyncText;

            try
            {
                background = LiveReloadLab.Background();
                asyncText = await LiveReloadLab.AsyncText();
            }
            catch (Exception ex)
            {
                // Shown on the page, where the rig reads it as a wrong value
                background = asyncText = $"FAIL {ex.GetType().Name}: {ex.Message}";
            }

            SetLab(_labBackground, background);
            SetLab(_labAsync, asyncText);

            try
            {
                await Task.Delay(LiveReloadLabPumpInterval, stop);
            }
            catch (OperationCanceledException)
            {
                // The instance is stopping, which ends the loop
                return;
            }
        }
    }

    // A field that a live update adds to a loaded type is kept outside its object, and the runtime loses a
    // write to it when a collection moves the object at the wrong moment. Two threads that compact the
    // youngest generation in a loop make that moment come within seconds, so an edit that is applied live and
    // adds such a field shows as a wrong value or ends the process while the rig watches
    private static void StartLiveReloadLabGCPressure(CancellationToken stop)
    {
        for (int i = 0; i < 2; i++)
        {
            var collector = new Thread(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true, compacting: true);
                    Thread.Sleep(1);
                }
            })
            {
                IsBackground = true,
                Name = "Live reload lab GC pressure",
            };

            collector.Start();
        }
    }

    // What the process holds once everything unreachable is gone, so that the rig can tell memory that live
    // edits keep from memory the collector had not yet had a reason to free. The managed numbers are the heap
    // after full blocking compacting collections; private bytes minus the committed heap is what the runtime
    // holds outside it, where applied deltas and replaced method bodies live. A build that a restart
    // replaced has unloaded when its assembly is gone from the list, which takes more than one collection:
    // the count of loaded copies of the app's assembly says how many builds are still in memory, and the
    // names that are loaded more than once say what else stayed with them
    private static string MeasureLiveReloadLabMemory()
    {
        for (int i = 0; i < 4; i++)
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        long managed = GC.GetTotalMemory(forceFullCollection: true);
        long committed = GC.GetGCMemoryInfo().TotalCommittedBytes;
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        string appName = typeof(Validation).Assembly.GetName().Name ?? "";
        int appBuilds = assemblies.Count(assembly => assembly.GetName().Name == appName);
        int collectible = assemblies.Count(assembly => assembly.IsCollectible);
        int dynamic = assemblies.Count(assembly => assembly.IsDynamic);
        var repeated = assemblies.GroupBy(assembly => assembly.GetName().Name ?? "?").Where(group => group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}*{group.Count()}");

        // The tick count makes two measurements differ, so the rig sees that the second one has arrived
        return $"at={Environment.TickCount64} managed={managed} committed={committed} private={process.PrivateMemorySize64} workingSet={process.WorkingSet64} "
            + $"assemblies={assemblies.Length} appBuilds={appBuilds} collectible={collectible} dynamic={dynamic} repeated:{string.Join(",", repeated)}";
    }

    // A method that waits at one await until the Resume button is pressed, so an edit lands while a call
    // is suspended in it, and the call then resumes in whatever code the edit left
    private async Task RunLiveReloadLabSuspendedAsync(CancellationToken stop)
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                string label = "suspended-1";
                await _labGate.WaitAsync(stop);
                // rig:suspended
                SetLab(_labSuspended, label + " resumed");
            }
        }
        catch (OperationCanceledException)
        {
            // The instance is stopping, which ends the loop
        }
        catch (Exception ex)
        {
            // Shown on the page, where the rig reads what a resumed call ran into
            SetLab(_labSuspended, $"FAIL {ex.GetType().Name}: {ex.Message}");
        }
    }

    // A method that never returns while the instance runs, so every edit to it lands while it is awaiting
    private async Task RunLiveReloadLabLoopAsync(CancellationToken stop)
    {
        int round = 0;

        while (!stop.IsCancellationRequested)
        {
            round++;
            SetLab(_labLoop, round > 0 ? "loop-1" : "loop-none");

            try
            {
                await Task.Delay(LiveReloadLabPumpInterval, stop);
            }
            catch (OperationCanceledException)
            {
                // The instance is stopping, which ends the loop
                return;
            }
        }
    }

    // Written only when it changed, so the pump re-renders a page only when an edit changed what it shows
    private static void SetLab(Reactive<string> target, string value)
    {
        if (target.Value != value)
        {
            target.Value = value;
        }
    }

    private void RenderLiveReloadLabSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Live Reload Lab");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], "State");
                    LabRow(view, "instance", _hotReloadInstance);
                    LabRow(view, "shared", _labShared.Value.ToString());
                    LabRow(view, "client", _labClient.Value.ToString());
                    LabRow(view, "pressed", _labPressed.Value);
                    LabRow(view, "effect", _labEffect.Value);

                    view.Button([Button.PrimaryMd],
                        text: "Press",
                        props: TestId("lab-press"),
                        onClick: async () =>
                        {
                            _labShared.Value++;
                            _labClient.Value++;
                            _labPressed.Value = "pressed-1";
                        });
                });
            });

            // Reads a reactive value, so its subtree is one a render reuses while that value is unchanged
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], $"Members (shared count {_labShared.Value})");
                    foreach (var (name, value) in LiveReloadLab.Rows())
                    {
                        LabRow(view, name, value);
                    }

                    LabRow(view, "main-file", LiveReloadLabMainFile());
                    LabRow(view, "app-lambda", LabText(() => $"app-lambda-1 in a {app.GlobalState.ServerRunType} run"));
                    LabRow(view, "generic-type", _labBox.Describe());
                    LabRow(view, "primary", _labPrimary.Describe());
                    LabRow(view, "ctor-kept", _labThing.Text);
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], $"Render method (shared count {_labShared.Value})");
                    LabRow(view, "render", "render-1");
                    view.Box(["h-6 w-24 rounded-md bg-success-500"], props: TestId("lab-style"));
                    // rig:nodes
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], "Background work");
                    LabRow(view, "background", _labBackground.Value);
                    LabRow(view, "async", _labAsync.Value);
                    LabRow(view, "loop", _labLoop.Value);
                    LabRow(view, "thread", _labThread.Value);
                    LabRow(view, "suspended", _labSuspended.Value);

                    view.Button([Button.PrimaryMd],
                        text: "Resume",
                        props: TestId("lab-resume"),
                        onClick: async () => _labGate.Release());
                    LabRow(view, "startup", _labStartup);
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], "Memory");
                    LabRow(view, "memory", _labMemory.Value);

                    view.Button([Button.PrimaryMd],
                        text: "Measure",
                        props: TestId("lab-measure"),
                        onClick: async () => _labMemory.Value = MeasureLiveReloadLabMemory());
                });
            });

            // A container of its own, so a body that throws replaces only this card's content
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.H3], $"Throwing (shared count {_labShared.Value})");
                    LabRow(view, "throwing", LiveReloadLab.Throwing());
                });
            });

            RenderHotReloadCellCard(view);
        });
    }

    private static string LabText(Func<string> text) => text();

    private static void LabRow(UIView view, string name, string value)
    {
        view.Row([Layout.Row.Md], content: view =>
        {
            view.Text([Text.Caption, "w-40"], name);
            view.Text([Text.Body], value, props: TestId($"lab-{name}"));
        });
    }
}
