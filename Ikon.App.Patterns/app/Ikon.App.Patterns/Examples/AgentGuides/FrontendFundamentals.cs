using System.Runtime.CompilerServices;

namespace Ikon.App.Patterns.Examples;

#region example:custom-component-extension
public static class MyComponentExtensions
{
    public static void MyComponent(
        this UIView view,
        string someProp,
        Func<MyClickEventArgs, Task>? onClick = null,
        string[]? style = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onClickId = null;

        if (onClick != null)
        {
            onClickId = view.CreateAction<MyClickEventArgs>(args => onClick(args.Value));
        }

        view.AddNode(
            "my-component",
            new Dictionary<string, object?>
            {
                ["someProp"] = someProp,
                ["onClickId"] = onClickId
            },
            style: style,
            file: file,
            line: line);
    }
}
#endregion

file static class CustomComponentExamples
{
    private sealed record Bot(string Id, string DraftCode);

    private static void UpdateActiveDraftCode(string code) => Log.Instance.Debug($"draft {code}");

    public static void Use(UIView view)
    {
        #region example:custom-component-use
        view.MyComponent("Hello from custom component",
            onClick: async args => { Log.Instance.Info("Clicked!"); },
            style: ["w-full rounded-lg"]);
        #endregion
    }

    public static void Stateful(UIView view)
    {
        var activeBot = new Bot("bot-1", "");

        #region example:custom-component-key
        view.AddNode(
            type: "custom.lua-editor",
            key: $"editor:{activeBot.Id}",  // remount when activeBot changes
            props: new Dictionary<string, object?>
            {
                ["value"] = activeBot.DraftCode,
                ["onValueChangeId"] = view.CreateAction<string>(args =>
                {
                    UpdateActiveDraftCode(args.Value ?? "");
                    return Task.CompletedTask;
                }),
            });
        #endregion
    }
}

file sealed class EmbeddedWidgetExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record Ticket(string Id, string Title, bool Open);

    private readonly ReactiveList<Ticket> _tickets = new(new Ticket[] { new("t-1", "Laptop for Aino", true), new("t-2", "Monitor for Mikko", false) });

    private static void RenderFullApp(UIView view) => view.Text([Text.H2], "IT assets");

    #region example:frontend-embedded-widget
    private UI UI { get; } = new(app, new IkonTheme());

    // Per client: the framed widget and the full app in a tab are separate clients, so each keeps its own filter
    private readonly ClientReactive<bool> _openOnly = new(false);

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            // The host page frames <app URL>/widget, and the path is known before the first render
            if (app.Navigation.CurrentPath?.StartsWith("/widget", StringComparison.OrdinalIgnoreCase) == true)
            {
                RenderWidget(view);
                return;
            }

            RenderFullApp(view);
        });
    }

    // No header or navigation, and sized by the frame: the list scrolls inside the iframe's height
    private void RenderWidget(UIView view)
    {
        view.Column(["h-dvh overflow-y-auto p-3 gap-2 bg-background"], content: view =>
        {
            view.Row(["gap-2"], content: view =>
            {
                view.Button([_openOnly.Value ? Button.OutlineSm : Button.PrimarySm], text: "All", onClick: async () => _openOnly.Value = false);
                view.Button([_openOnly.Value ? Button.PrimarySm : Button.OutlineSm], text: "Open", onClick: async () => _openOnly.Value = true);
            });

            foreach (var ticket in _tickets.Where(t => t.Open || !_openOnly.Value))
            {
                view.Text([Text.Body, "truncate"], ticket.Title, key: ticket.Id);
            }

            view.Link([Text.Link, "text-sm"], text: "Open the full app", href: "/", target: "_blank");
        });
    }
    #endregion
}
