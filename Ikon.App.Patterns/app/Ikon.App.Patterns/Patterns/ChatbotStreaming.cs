namespace Ikon.App.Patterns.Patterns;

// Pattern: chatbot-streaming — see docs/patterns/chatbot-streaming.md.
// This example is self-contained: the docsnippet region carries the whole conversation state, the
// streaming send, and the transcript/input UI.
internal sealed class ChatbotStreaming : IPatternDemo
{
    public string Slug => "chatbot-streaming";
    public string Title => "Chatbot streaming";
    public string Category => "Chat";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-chatbot-streaming
    public sealed record ChatMessage(string Role, string Text);
    public sealed record ChatReply(string Reply);

    private readonly ReactiveList<ChatMessage> _transcript = new();
    private readonly Reactive<string> _draft = new("");
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string> _streaming = new("");
    private KernelContext _ctx = new();

    private async Task SendAsync()
    {
        var text = _draft.Value.Trim();

        if (string.IsNullOrEmpty(text) || _busy.Value)
        {
            return;
        }

        _transcript.Add(new ChatMessage("You", text));
        _draft.Value = "";
        _streaming.Value = "";
        using var _ = _busy.AsToken();

        try
        {
            // Streaming: iterate Emerge.Run's event stream. Each ModelText<T>
            // event is the next token chunk — append to _streaming so the UI
            // re-renders token-by-token. Completed<T> fires once with the
            // final typed result + the new KernelContext for the next turn.
            var sb = new System.Text.StringBuilder();
            await foreach (var ev in Emerge.Run<ChatReply>(LLMModel.Claude46Sonnet, _ctx, pass =>
            {
                pass.SystemPrompt = "You are a helpful assistant. Reply concisely.";
                pass.Command = $"User said: {text}\n\nReturn JSON:\n{pass.JsonSchema}";
            }))
            {
                if (ev is ModelText<ChatReply> token)
                {
                    sb.Append(token.Text);
                    _streaming.Value = sb.ToString();
                }
                else if (ev is Completed<ChatReply> done)
                {
                    _ctx = done.Context;

                    if (done.Result != null)
                    {
                        _transcript.Add(new ChatMessage("Assistant", done.Result.Reply));
                    }

                    _streaming.Value = "";
                }
            }
        }
        catch (Exception ex)
        {
            _transcript.Add(new ChatMessage("System", $"Error: {ex.Message}"));
            _streaming.Value = "";
        }
    }

    private void Render(IView view)
    {
        view.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["p-4"], content: view =>
        {
            view.Column(["gap-3"], content: view =>
            {
                foreach (var msg in _transcript)
                {
                    var isUser = msg.Role == "You";
                    view.Box([isUser ? "self-end bg-primary" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"], content: v =>
                        v.Text(text: msg.Text));
                }

                // In-flight streaming bubble: reading _streaming.Value here
                // registers a dependency, so this re-renders on every ModelText
                // event. Empties when Completed fires above.
                if (!string.IsNullOrEmpty(_streaming.Value))
                {
                    view.Box(["self-start bg-surface rounded-lg p-3 max-w-[80%] opacity-80"], content: v =>
                        v.Text(text: _streaming.Value));
                }
            });
        });
        view.Row(["p-4 gap-2 border-t"], content: view =>
        {
            view.TextField([Input.Default, "flex-1"], value: _draft.Value, placeholder: "Type a message…",
                onValueChange: async v => _draft.Value = v,
                onSubmit: async _ => await SendAsync());
            view.Button(style: [Button.Default, _busy.Value ? "opacity-50" : ""],
                disabled: _busy.Value, onClick: SendAsync,
                content: v => v.Text(text: _busy.Value ? "Thinking…" : "Send"));
        });
    }
    #endregion
}
