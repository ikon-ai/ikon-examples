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
