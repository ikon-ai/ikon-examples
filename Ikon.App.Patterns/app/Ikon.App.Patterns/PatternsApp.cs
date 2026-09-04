using Ikon.App.Patterns;

return await App.Run(args);

public record SessionIdentity(string? UserId);
public record ClientParameters(string Name = "Ikon");

[App]
public class PatternsApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme { Mode = ThemeMode.Fixed });

    private readonly ClientReactive<string> _selectedSlug = new("");

    public async Task Main()
    {
        var demos = PatternGallery.Create(app)
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Title)
            .ToList();

        UI.Root([Page.Default], content: view =>
        {
            view.Row(["h-screen w-full bg-[#0A0A0A] text-[#EDE7DC]"], content: shell =>
            {
                RenderSidebar(shell, demos);
                RenderSelected(shell, demos);
            });
        });
    }

    private void RenderSidebar(IView view, IReadOnlyList<IPatternDemo> demos)
    {
        view.ScrollArea(
            scrollbars: ScrollAreaScrollbars.Vertical,
            type: ScrollAreaType.Auto,
            rootStyle: ["w-[280px] flex-shrink-0 border-r border-white/10 bg-[#0E0E0E]"],
            viewportStyle: ["p-3"],
            content: rail =>
            {
                rail.Text(["text-xs uppercase tracking-wider text-[#7A756E] px-2 py-2"],
                    $"Patterns ({demos.Count})");

                string? currentCategory = null;
                foreach (var demo in demos)
                {
                    if (demo.Category != currentCategory)
                    {
                        currentCategory = demo.Category;
                        rail.Text(["text-[11px] font-semibold uppercase tracking-wide text-[#D6A85C] px-2 pt-3 pb-1"],
                            currentCategory);
                    }

                    bool selected = _selectedSlug.Value == demo.Slug;
                    string style = selected
                        ? "w-full text-left text-sm text-[#EDE7DC] bg-white/10 rounded-md px-3 py-2 cursor-pointer transition-colors"
                        : "w-full text-left text-sm text-[#A8A29E] rounded-md px-3 py-2 cursor-pointer hover:text-[#EDE7DC] hover:bg-white/5 transition-colors";
                    string slug = demo.Slug;
                    rail.Button([style, "border-none justify-start"],
                        text: demo.Title,
                        onClick: async () => { _selectedSlug.Value = slug; });
                }
            });
    }

    private void RenderSelected(IView view, IReadOnlyList<IPatternDemo> demos)
    {
        var selected = demos.FirstOrDefault(d => d.Slug == _selectedSlug.Value) ?? demos.FirstOrDefault();

        view.ScrollArea(
            scrollbars: ScrollAreaScrollbars.Vertical,
            type: ScrollAreaType.Auto,
            rootStyle: ["flex-1 min-w-0"],
            viewportStyle: ["p-8"],
            content: pane =>
            {
                if (selected is null)
                {
                    pane.Text(["text-sm text-[#A8A29E]"], "No patterns registered.");
                    return;
                }

                pane.Column(["gap-1 mb-6"], content: header =>
                {
                    header.Text(["text-2xl font-semibold text-[#EDE7DC]"], selected.Title);
                    header.Text(["text-xs font-mono text-[#7A756E]"],
                        $"{selected.Category} · {selected.Slug}");
                });

                selected.RenderDemo(pane);
            });
    }
}
