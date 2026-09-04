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

    #region docsnippet:pattern-typical-app-structure
    // Program entry, a top-level statement in Program.cs (one per app):  return await App.Run(args);

    public sealed record SessionIdentity(string? UserId);
    public sealed record ClientParameters(string ClientName);

    // The app class carries the attribute:  [App]
    public class MyApp(IApp<SessionIdentity, ClientParameters> app)
    {
        private UI UI { get; } = new(app, new IkonTheme());

        private readonly Reactive<int> _count = new(0);

        public Task Main()
        {
            UI.Root(["h-screen flex flex-col"], content: view =>
            {
                // ── Header region ───────────────────────────────────────────────
                view.Row(["flex-shrink-0 px-6 py-4 border-b items-center justify-between"], content: view =>
                {
                    view.Text([Text.H3, "tracking-wide"], text: "MyApp");
                });

                // ── Main region ─────────────────────────────────────────────────
                view.Column([Layout.Page, "flex-1 min-h-0 gap-4 py-8"], content: view =>
                {
                    view.Text([Text.H1], text: "Welcome");
                    view.Text([Text.Body, "text-muted-foreground"], text: $"count: {_count.Value}");
                    view.Button(
                        style: [Button.Default, "transition-colors duration-150 hover:opacity-90"],
                        onClick: () => _count.Value++,
                        content: v => v.Text(text: "Increment"));
                });
            });
            return Task.CompletedTask;
        }
    }
    #endregion
}
