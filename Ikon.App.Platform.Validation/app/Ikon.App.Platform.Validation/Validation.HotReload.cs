public partial class Validation
{
    // Not a reactive, so each reload's instance shows a new one: a page still showing the old id was
    // never sent the new instance's UI
    private readonly string _hotReloadInstance = Guid.NewGuid().ToString("N")[..8];

    private readonly Reactive<int> _hotReloadSharedCount = new(0);
    private readonly ClientReactive<int> _hotReloadClientCount = new(0);

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
        });
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
