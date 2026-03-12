public partial class Validation
{
    private void RenderChatCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Chat");
            view.Text([Text.Caption, "mb-4"], "Emergence-powered AI assistant with auto-scrolling chat messages and configurable model/region selection.");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md], content: view =>
                {
                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Model");
                        view.Select(
                            value: _chatModel.Value,
                            options: GetModelOptions<LLMModel>(),
                            onValueChange: async v => _chatModel.Value = v ?? _chatModel.Value);
                    });

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Region");
                        view.Select(
                            value: _chatRegion.Value,
                            options: GetModelOptions<ModelRegion>(),
                            onValueChange: async v => _chatRegion.Value = v ?? _chatRegion.Value);
                    });
                });

                view.ScrollArea(
                    autoScroll: _chatMessages.Value.Count > 0,
                    autoScrollKey: _chatMessages.Value.Count.ToString(),
                    rootStyle: [ScrollArea.Root, "h-96 border border-secondary rounded-md p-4"],
                    content: scrollView =>
                {
                    if (_chatMessages.Value.Count == 0)
                    {
                        scrollView.Box(["flex items-center justify-center h-full"], content: emptyView =>
                        {
                            emptyView.Text([Text.Caption, "text-muted-foreground"], "No messages yet. Send a message to start chatting!");
                        });
                    }
                    else
                    {
                        scrollView.Column([Layout.Column.Md], content: col =>
                        {
                            foreach (var message in _chatMessages.Value)
                            {
                                RenderChatMessage(col, message);
                            }
                        });
                    }
                });

                view.Row([Layout.Row.Md, "mt-4"], content: row =>
                {
                    row.TextField([Input.Default, "flex-1"],
                        value: _chatInputText.Value,
                        placeholder: "Type a message...",
                        onValueChange: value =>
                        {
                            _chatInputText.Value = value ?? "";
                            return Task.CompletedTask;
                        },
                        onSubmit: async () =>
                        {
                            var text = _chatInputText.Value.Trim();

                            if (!string.IsNullOrEmpty(text) && !_chatIsProcessing.Value)
                            {
                                _chatInputText.Value = "";
                                await SendChatMessageAsync(text);
                            }
                        });

                    row.Button([Button.PrimaryMd],
                        label: _chatIsProcessing.Value ? "Sending..." : "Send",
                        disabled: _chatIsProcessing.Value || string.IsNullOrWhiteSpace(_chatInputText.Value),
                        onClick: async () =>
                        {
                            var text = _chatInputText.Value.Trim();

                            if (!string.IsNullOrEmpty(text) && !_chatIsProcessing.Value)
                            {
                                _chatInputText.Value = "";
                                await SendChatMessageAsync(text);
                            }
                        });
                });

                view.Row([Layout.Row.Md, "mt-4"], content: row =>
                {
                    row.Button([Button.SecondaryMd],
                        label: "Add Test Message",
                        onClick: AddTestMessage);

                    row.Button([Button.OutlineMd],
                        label: "Clear Chat",
                        onClick: ClearChatMessages);
                });
            });
        });
    }

    private void RenderChatMessage(UIView view, ChatMessageEntry message)
    {
        var isUser = message.Role == ChatMessageRole.User;
        var alignmentClass = isUser ? "ml-auto" : "mr-auto";
        var bubbleStyle = isUser
            ? "bg-blue-600 text-white rounded-2xl rounded-br-md px-4 py-3"
            : "bg-zinc-200 dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 rounded-2xl rounded-bl-md px-4 py-3";
        var labelStyle = isUser ? "text-xs text-blue-200 mb-1" : "text-xs text-zinc-500 dark:text-zinc-400 mb-1";
        var label = isUser ? "You" : "Assistant";

        view.Box([alignmentClass, "max-w-[80%]"], content: wrapper =>
        {
            wrapper.Box([bubbleStyle], content: bubble =>
            {
                bubble.Column(content: col =>
                {
                    col.Text([labelStyle], label);
                    col.Text([Text.Body], message.Content.Value);
                });
            });
        });
    }

    private async Task SendChatMessageAsync(string userMessage)
    {
        _chatIsProcessing.Value = true;

        try
        {
            var userEntry = new ChatMessageEntry { Role = ChatMessageRole.User };
            userEntry.Content.Value = userMessage;
            _chatMessages.Value.Add(userEntry);
            _chatMessages.NotifyUpdate();

            var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
            assistantEntry.Content.Value = "";
            _chatMessages.Value.Add(assistantEntry);
            _chatMessages.NotifyUpdate();

            var responseText = new StringBuilder();
            var ctx = new KernelContext();

            // Build conversation history from all previous messages (skip the empty assistant placeholder)
            foreach (var msg in _chatMessages.Value)
            {
                if (msg == assistantEntry)
                {
                    continue;
                }

                var role = msg.Role == ChatMessageRole.User ? MessageBlockRole.User : MessageBlockRole.Model;
                ctx = ctx.Add(new MessageBlock(role, msg.Content.Value));
            }

            var model = Enum.Parse<LLMModel>(_chatModel.Value);
            var region = Enum.Parse<ModelRegion>(_chatRegion.Value);

            await foreach (var ev in Emerge.Run<ChatReply>(model, ctx, pass =>
            {
                pass.Command = userMessage;
                pass.SystemPrompt = "You are a helpful assistant. Keep responses concise and friendly.";
                pass.MaxOutputTokens = 500;
                pass.Regions = [region];
            }))
            {
                switch (ev)
                {
                    case ModelText<ChatReply> text:
                        responseText.Append(text.Text);
                        assistantEntry.Content.Value = responseText.ToString();
                        break;

                    case Completed<ChatReply> completed:
                        assistantEntry.Content.Value = completed.Result.Response;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            var errorEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
            errorEntry.Content.Value = $"Error: {ex.Message}";
            _chatMessages.Value.Add(errorEntry);
            _chatMessages.NotifyUpdate();
        }
        finally
        {
            _chatIsProcessing.Value = false;
        }
    }

    private Task AddTestMessage()
    {
        var userEntry = new ChatMessageEntry { Role = ChatMessageRole.User };
        userEntry.Content.Value = $"Test message #{_chatMessages.Value.Count + 1}";
        _chatMessages.Value.Add(userEntry);

        var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
        assistantEntry.Content.Value = $"This is a test response to message #{_chatMessages.Value.Count}.";
        _chatMessages.Value.Add(assistantEntry);

        _chatMessages.NotifyUpdate();
        return Task.CompletedTask;
    }

    private Task ClearChatMessages()
    {
        _chatMessages.Value.Clear();
        _chatMessages.NotifyUpdate();
        return Task.CompletedTask;
    }
}

internal enum ChatMessageRole
{
    User,
    Assistant
}

internal sealed class ChatMessageEntry
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public ChatMessageRole Role { get; init; }
    public Reactive<string> Content { get; } = new("");
}

internal sealed class ChatReply
{
    public string Response { get; set; } = "";
}
