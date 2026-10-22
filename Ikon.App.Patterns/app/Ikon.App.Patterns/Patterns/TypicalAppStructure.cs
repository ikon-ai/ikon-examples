namespace Ikon.App.Patterns.Patterns;

// Pattern: typical-app-structure — see docs/patterns/typical-app-structure.md.
// The skeleton is the whole teaching, so it lives inside the region as a nested example class. Two
// pieces cannot be live lines here: the program entry (`return await App.Run(args);`), a top-level
// statement one per app, and the `[App]` attribute itself, which the runtime discovers exactly once
// per assembly — a live one here would compete with this app's real [App] type. Both are shown as
// comments instead.
internal sealed class TypicalAppStructure : IPatternDemo
{
    public string Slug => "typical-app-structure";
    public string Title => "Typical app structure";
    public string Category => "Layout";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title, "The canonical app skeleton — session identity, client parameters, UI root, and header/main regions — see docs/patterns/typical-app-structure.md.");

    #region example:pattern-typical-app-structure
    // Program entry, the top-level first line of the app class's own file (one per app, no Program.cs):  return await App.Run(args);

    public record SessionIdentity(string? UserId);
    public record ClientParameters(string Name);

    // The app class carries the attribute:  [App]
    public partial class MyApp(IApp<SessionIdentity, ClientParameters> app)
    {
        private UI UI { get; } = new(app, new IkonTheme());

        private readonly Reactive<int> _count = new(0);

        public async Task Main()
        {
            UI.Root(["h-screen flex flex-col"], content: view =>
            {
                // ── Header region ───────────────────────────────────────────────
                // Header, tabs and content share one left edge: the bar is full width, its inner row
                // takes Layout.PageWidth, and the page below uses the same max-w-5xl column.
                view.Box(["shrink-0 w-full border-b border-secondary"], content: view =>
                {
                    // flex-wrap: on a phone the actions move under the title rather than squeezing their labels onto two lines.
                    view.Row([Layout.PageWidth, "py-4 items-center justify-between flex-wrap gap-3"], content: view =>
                    {
                        view.Text([Text.H3, "tracking-wide"], text: "MyApp");
                        view.Button([Button.OutlineSm], text: "Reset count", onClick: () => _count.Value = 0);
                    });
                });

                // ── Main region ─────────────────────────────────────────────────
                // Tabs.NavListPage draws the rail full width and starts the first label at the title's left edge.
                view.Tabs(
                    defaultValue: "home",
                    style: ["flex-1 min-h-0 flex flex-col"],
                    listStyle: [Tabs.NavListPage],
                    contentStyle: [Tabs.ContentFill],
                    triggerStyle: [Tabs.NavTriggerMd],
                    tabs:
                    [
                        new TabItem("home", "Home", view =>
                        {
                            view.Column([Layout.Page, "gap-4 py-8"], content: view =>
                            {
                                // The title with its actions beside it from sm up, stacked on a phone
                                view.PageHeader("Welcome", subtitle: $"count: {_count.Value}",
                                    actions: view => view.Button([Button.PrimaryMd], text: "Increment", onClick: () => _count.Value++));
                            });
                        }),
                        new TabItem("about", "About", view => view.Column([Layout.Page], content: view => view.Text([Text.Body], text: "About this app"))),
                    ]);
            });
        }
    }
    #endregion
}
