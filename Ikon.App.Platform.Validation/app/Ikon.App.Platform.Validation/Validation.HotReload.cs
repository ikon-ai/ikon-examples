public partial class Validation
{
    // Not a reactive, so each reload's instance shows a new one: a page still showing the old id was
    // never sent the new instance's UI
    private readonly string _hotReloadInstance = Guid.NewGuid().ToString("N")[..8];

    private const string HotReloadCellWorkspace = "hot-reload";

    private readonly Reactive<int> _hotReloadSharedCount = new(0);
    private readonly ClientReactive<int> _hotReloadClientCount = new(0);

    // Each line names the instance that wrote it, so a page can tell this instance's line from the one
    // the reload carried over from the instance before
    private readonly Reactive<string> _hotReloadCellStart = new("Running…");
    private readonly ClientReactive<string?> _hotReloadCellError = new(null);

    private void RenderHotReloadSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Hot Reload");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Body], $"Build: {HotReloadProbe.Build}", props: TestId("hot-reload-build"));
                    view.Text([Text.Body], $"Instance: {_hotReloadInstance}", props: TestId("hot-reload-instance"));
                    view.Text([Text.Body], $"Resource: {ReadHotReloadProbeResource()}", props: TestId("hot-reload-resource"));
                    view.Text([Text.Body], $"Shared count: {_hotReloadSharedCount.Value}", props: TestId("hot-reload-shared-count"));
                    view.Text([Text.Body], $"Client count: {_hotReloadClientCount.Value}", props: TestId("hot-reload-client-count"));

                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            text: "Count",
                            props: TestId("hot-reload-count"),
                            onClick: async () =>
                            {
                                _hotReloadSharedCount.Value++;
                                _hotReloadClientCount.Value++;
                            });

                        view.Box([HotReloadProbe.Accent, "h-10 w-32 rounded-md"], props: TestId("hot-reload-accent"));
                    });
                });
            });

            RenderHotReloadCellCard(view);
        });
    }

    // A reload stops the run's cells, so the cell this page mirrors starts again from the new build and
    // the page's mirror has to subscribe to it again
    private void RenderHotReloadCellCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Column([Layout.Column.Sm], content: view =>
            {
                view.Text([Text.H3], "Cell");
                view.Text([Text.Body], $"Cell at start: {_hotReloadCellStart.Value}", props: TestId("hot-reload-cell-start"));

                ILabCell cell;

                try
                {
                    cell = ConnectLabCell(HotReloadCellWorkspace);
                }
                catch (Exception ex)
                {
                    view.Text([Text.Body], $"Error: {ex.Message}", props: TestId("hot-reload-cell-error"));
                    return;
                }

                var status = cell.Counter is MirrorReactive<int> mirror ? mirror.Status.ToString() : "in this process";
                view.Text([Text.Body], $"Cell mirror: {status}", props: TestId("hot-reload-cell-status"));
                view.Text([Text.Body], $"Cell count: {cell.Counter.Value}", props: TestId("hot-reload-cell-count"));
                view.Text([Text.Body], $"Open cell connections: {Cells.Instance.OpenConnectionCount}", props: TestId("hot-reload-cell-connections"));

                view.Button([Button.PrimaryMd],
                    text: "Count in the cell",
                    props: TestId("hot-reload-cell-increment"),
                    onClick: async () => FireCellCommand(_hotReloadCellError, () => cell.IncrementAsync(1)));

                if (_hotReloadCellError.Value != null)
                {
                    view.Text([Text.Body], $"Error: {_hotReloadCellError.Value}", props: TestId("hot-reload-cell-error"));
                }
            });
        });
    }

    // Only a local run hot-reloads. Each new instance calls its cell as it starts. A reload that stops
    // the cells after the new instance has connected to them loses that call, and the card then shows it.
    private void StartHotReloadCellProbe()
    {
        if (app.GlobalState.ServerRunType != ServerRunType.Local)
        {
            return;
        }

        _ = ProbeHotReloadCellAsync();
    }

    private async Task ProbeHotReloadCellAsync()
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            var info = await ConnectLabCell(HotReloadCellWorkspace).DescribeAsync().WaitAsync(TimeSpan.FromSeconds(60.0));
            _hotReloadCellStart.Value = $"PASS {_hotReloadInstance}: build {info.Build} in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:0} ms";
        }
        catch (Exception ex)
        {
            _hotReloadCellStart.Value = $"FAIL {_hotReloadInstance}: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string ReadHotReloadProbeResource()
    {
        using var stream = typeof(Validation).Assembly.GetManifestResourceStream("hot-reload-probe.txt");

        if (stream == null)
        {
            return "missing";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
