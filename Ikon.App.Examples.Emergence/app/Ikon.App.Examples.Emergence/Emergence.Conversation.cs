using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class ChatReply
    {
        public string Reply { get; set; } = "";
    }

    private sealed record ChatTurn(string Role, string Text);

    // A single KernelContext carried across turns is what gives the model its memory of the conversation.
    private KernelContext _chatContext = new();
    private readonly ReactiveList<ChatTurn> _chatMessages = new();
    private readonly Reactive<string> _chatInput = new("");
    private readonly Reactive<bool> _chatBusy = new(false);

    private void RenderConversationExample(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Conversation (context reuse)");
                    view.Text([Text.Body, "text-muted-foreground mb-2"],
                        "A multi-turn chatbot. Each turn reuses the KernelContext returned by the previous Emerge.Run call, " +
                        "so the model remembers the whole conversation automatically.");

                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How it works:");
                        view.Text([Text.Caption, "text-blue-300 whitespace-pre-wrap"],
                            "The first turn starts with a fresh KernelContext. Every reply returns an updated context which is\n" +
                            "passed into the next turn — no manual history bookkeeping. Try a follow-up like \"and why?\".");
                    });
                });
            });

            view.Box([Card.Default, "p-4 flex flex-col min-h-[420px] max-h-[560px]"], content: view =>
            {
                view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                {
                    view.Text([Text.H3], "Chat");
                    view.Button([Button.OutlineMd], text: "Reset", onClick: async () =>
                    {
                        _chatContext = new KernelContext();
                        _chatMessages.Value = [];
                    });
                });

                view.ScrollArea(["flex-1 min-h-0"], autoScrollKey: _chatMessages.Value.Count.ToString(), content: view =>
                {
                    if (_chatMessages.Value.Count == 0)
                    {
                        view.Text([Text.Caption, "text-muted-foreground"], "Say hello to start the conversation...");
                    }
                    else
                    {
                        view.Column([Layout.Column.Sm], content: view =>
                        {
                            foreach (var (turn, index) in _chatMessages.Value.Select((t, i) => (t, i)))
                            {
                                var isUser = turn.Role == "user";
                                var bubble = turn.Role switch
                                {
                                    "user" => "bg-primary/20 self-end",
                                    "assistant" => "bg-muted self-start",
                                    _ => "bg-red-500/10 self-start"
                                };

                                view.Box([$"rounded-lg px-3 py-2 max-w-[80%] {bubble}"], key: $"msg-{index}", content: view =>
                                {
                                    view.Text([Text.Caption, "font-semibold text-muted-foreground mb-0.5"], isUser ? "You" : (turn.Role == "assistant" ? "Assistant" : "System"));
                                    view.Text([Text.Body, "whitespace-pre-wrap break-words"], turn.Text);
                                });
                            }
                        });
                    }
                });

                view.Row([Layout.Row.Sm, "mt-2 shrink-0 items-center"], content: view =>
                {
                    view.TextField(
                        bind: _chatInput,
                        style: [Input.Default, "flex-1"],
                        label: "Message",
                        placeholder: "Type a message and press Enter...",
                        onSubmit: async _ => await SendChatAsync(),
                        clearOnSubmit: true);

                    view.Button([Button.PrimaryMd], text: _chatBusy.Value ? "..." : "Send", disabled: _chatBusy.Value, onClick: async () => await SendChatAsync());
                });
            });
        });
    }

    private async Task SendChatAsync()
    {
        var text = _chatInput.Value.Trim();

        if (text.Length == 0 || _chatBusy.Value)
        {
            return;
        }

        _chatInput.Value = "";
        _chatMessages.Add(new ChatTurn("user", text));
        _chatBusy.Value = true;

        try
        {
            var (reply, context) = await Emerge.Run<ChatReply>(LLMModel.Claude45Haiku, _chatContext, pass =>
            {
                pass.SystemPrompt = "You are a friendly, concise assistant. Keep replies to a few sentences.";
                pass.Command = text;
            }).FinalAsync();

            _chatContext = context;
            _chatMessages.Add(new ChatTurn("assistant", reply?.Reply ?? "(no reply)"));
        }
        catch (Exception ex)
        {
            _chatMessages.Add(new ChatTurn("system", $"Error: {ex.Message}"));
        }
        finally
        {
            _chatBusy.Value = false;
        }
    }
}
