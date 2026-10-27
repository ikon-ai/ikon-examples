public partial class Validation
{
    // How long the Slow item's handler holds the press, so the battery can see the menu and the page's
    // skeleton move before the server answers.
    private static readonly TimeSpan AppNavSlowPageDelay = TimeSpan.FromMilliseconds(800);

    private const string AppNavPath = "/app-nav";

    private readonly ClientReactive<string> _appNavPage = ClientReactive.Create(_ => AppNavPageFor(app.Navigation.CurrentPath));
    private readonly ClientReactive<string> _appNavResult = new("Not switched yet");
    private readonly ClientReactive<string> _appNavCheckboxResult = new("Not ticked yet");

    private static string AppNavPathOf(string page) => page == "home" ? AppNavPath : $"{AppNavPath}/{page}";

    private static string AppNavPageFor(string? path) => path switch
    {
        AppNavPath + "/inbox" => "inbox",
        AppNavPath + "/slow" => "slow",
        _ => "home",
    };

    // Called for every path the client moves to, the App Nav tab's own pages included.
    private void RouteAppNav(string path)
    {
        if (path != AppNavPath && !path.StartsWith(AppNavPath + "/", StringComparison.Ordinal))
        {
            return;
        }

        var page = AppNavPageFor(path);
        _appNavPage.Value = page;
        _appNavResult.Value = $"PASS page {page} arrived";
    }

    private void RenderAppNavSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "NavBar and NavOutlet");
                view.Text([Text.BodySm, "text-tertiary mb-4"],
                    "The pressed item turns selected and the page area shows the page's skeleton before the server answers. The Slow item's handler waits 800 ms before the app routes.");

                view.Column(["h-[420px] rounded-lg border border-secondary overflow-hidden"], content: phone =>
                {
                    phone.ScrollArea(["flex-1 min-h-0"], content: body =>
                        body.NavOutlet(path: AppNavPathOf(_appNavPage.Value), content: page => RenderAppNavPage(page, _appNavPage.Value)));

                    phone.NavBar(ariaLabel: "Validation pages", content: bar =>
                    {
                        bar.NavItem(AppNavPath, label: "Home", icon: "house", props: TestId("app-nav-item-home"));
                        bar.NavItem(AppNavPath + "/inbox", label: "Inbox", icon: "inbox", badge: 3, props: TestId("app-nav-item-inbox"));
                        bar.NavItem(AppNavPath + "/slow", label: "Slow", icon: "hourglass", props: TestId("app-nav-item-slow"),
                            onClick: async () => await Task.Delay(AppNavSlowPageDelay));
                    });
                });

                view.Row(["mt-3 gap-2"], content: row =>
                    row.Button([Button.OutlineSm], "Open Inbox", path: AppNavPath + "/inbox", props: TestId("app-nav-button-inbox")));

                view.Text([Text.BodySm, "mt-3"], _appNavResult.Value, props: TestId("app-nav-result"));
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Uncontrolled checkbox");
                view.Text([Text.BodySm, "text-tertiary mb-4"],
                    "A checkbox given only a default ticks on the client, and the server hears about the change.");
                view.Checkbox(defaultValue: false, label: "Tick me",
                    props: TestId("app-nav-checkbox"),
                    onValueChange: async ticked => _appNavCheckboxResult.Value = ticked ? "PASS the server heard the tick" : "Unticked");
                view.Text([Text.BodySm, "mt-3"], _appNavCheckboxResult.Value, props: TestId("app-nav-checkbox-result"));
            });
        });
    }

    private static void RenderAppNavPage(UIView view, string page)
    {
        view.Column([Layout.Column.Md, "p-4"], content: content =>
        {
            content.Text([Text.H3], page switch { "inbox" => "Inbox", "slow" => "Slow page", _ => "Home" }, props: TestId($"app-nav-page-{page}"));
            for (var line = 1; line <= 12; line++)
            {
                content.Text([Text.Body], $"{page} line {line}");
            }
        });
    }
}
