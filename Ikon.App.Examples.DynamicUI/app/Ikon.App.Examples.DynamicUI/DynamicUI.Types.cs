using Ikon.Common;

public class ChatResponse
{
    [Description("Your response to the user", example: "I'll create a counter with increment and decrement buttons for you.")]
    public string Message { get; set; } = string.Empty;

    [Description("Whether to generate UI code. Set to true if the user is requesting a UI component", example: true)]
    public bool ShouldGenerateUI { get; set; }

    [Description("Description of the UI to generate if ShouldGenerateUI is true", example: "A counter with a display showing the current count and two buttons: one to increment and one to decrement")]
    public string? UIDescription { get; set; }
}

public class UICodeResponse
{
    [Description("The C# code that creates the UI using the Parallax framework", example: """
var counter = state.GetOrCreate<Reactive<int>>("counter", () => new Reactive<int>(0));

view.Column([Layout.Column.Md, "p-4", "items-center"], content: view =>
{
    view.Text([Text.H2], "Counter");
    view.Text([Text.Display], counter.Value.ToString());
    view.Row([Layout.Row.Md], content: view =>
    {
        view.Button([Button.OutlineMd], label: "-", onClick: async () => counter.Value--);
        view.Button([Button.PrimaryMd], label: "+", onClick: async () => counter.Value++);
    });
});
""")]
    public string Code { get; set; } = string.Empty;
}
