namespace Ikon.App.Patterns.Patterns;

// Pattern: chatbot-streaming — see docs/patterns/chatbot-streaming.md.
// This example is self-contained: the example region carries the whole conversation state, the
// streaming send, and the transcript/input UI.
internal sealed class ChatbotStreaming : IPatternDemo
{
    public string Slug => "chatbot-streaming";
    public string Title => "Chatbot streaming";
    public string Category => "Chat";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-chatbot-streaming
    public sealed record ChatMessage(string Role, string Text);

    private readonly ReactiveList<ChatMessage> _transcript = new();
    private readonly ClientReactive<string> _draft = new("");
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string> _streaming = new("");
    private KernelContext _ctx = new();

    // The text arrives as an argument so the Send button and Enter share one path; the button
    // passes _draft.Value, and Enter passes the submitted text.
    private async Task SendAsync(string input)
    {
        var text = input.Trim();

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
            // event is the next chunk of the model's own output — append to
            // _streaming so the UI re-renders token-by-token. Completed<T>
            // fires once with the final result + the new KernelContext.
            //
            // Run<string> is what makes those chunks readable. Every other T
            // turns JSON mode on, and then the chunks ARE the JSON: the bubble
            // fills up with {"Reply":"He… instead of the reply.
            _ctx = _ctx.Add(new MessageBlock(MessageBlockRole.User, text));
            var sb = new System.Text.StringBuilder();
            await foreach (var ev in Emerge.Run<string>(LLMModel.Claude46Sonnet, _ctx, pass =>
            {
                pass.SystemPrompt = "You are a helpful assistant. Reply concisely.";
            }))
            {
                if (ev is ModelText<string> token)
                {
                    sb.Append(token.Text);
                    _streaming.Value = sb.ToString();
                }
                else if (ev is Retry<string>)
                {
                    // The generation starts over; text streamed before the Retry is superseded.
                    sb.Clear();
                    _streaming.Value = "";
                }
                else if (ev is Completed<string> done)
                {
                    _ctx = done.Context;

                    if (!string.IsNullOrWhiteSpace(done.Result))
                    {
                        _transcript.Add(new ChatMessage("Assistant", done.Result));
                    }

                    _streaming.Value = "";
                }
                else if (ev is Stopped<string>)
                {
                    // Enumerating a run hands back a stop as an event, not an exception.
                    _transcript.Add(new ChatMessage("System", "The assistant didn't answer — send it again."));
                    _streaming.Value = "";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Chat reply for client {ReactiveScope.ClientId} failed, user can send again: {ex.Message}");

            // Visible, and in the reader's language — never the exception text, which names a
            // provider and a socket to someone who wanted an answer.
            _transcript.Add(new ChatMessage("System", "Couldn't reach the assistant — send it again."));
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
                    // bg-brand-solid, not bg-primary: in the Ikon semantic set bg-primary is the
                    // PAGE surface, so a bubble painted with it disappears into the page.
                    view.Box([isUser ? "self-end bg-brand-solid text-primary-on-brand" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"], content: v =>
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
                onSubmit: SendAsync);
            view.Button(style: [Button.Default, _busy.Value ? "opacity-50" : ""],
                disabled: _busy.Value, onClick: () => SendAsync(_draft.Value),
                content: v => v.Text(text: _busy.Value ? "Thinking…" : "Send"));
        });
    }
    #endregion
}
