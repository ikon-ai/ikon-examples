public partial class Validation
{
    private readonly Reactive<string> _chatReasoning = new(ChatReasoningDefault);
    private readonly Reactive<string?> _chatError = new(null);
    private readonly Reactive<string?> _chatErrorKind = new(null);
    private readonly Reactive<string?> _chatTokenEstimate = new(null);
    private readonly ClientReactive<string> _composerDraft = new("");
    private readonly ClientReactiveList<ComposerAttachment> _composerAttachments = new();
    private readonly ClientReactive<string> _composerEcho = new("Echo: (nothing sent yet)");

    private const string ChatReasoningDefault = "Default";

    private static readonly List<SelectOption> ChatReasoningOptions =
    [
        new(ChatReasoningDefault, "Model default (no dial)"),
        new("Effort:Low", "Effort: Low"),
        new("Effort:Medium", "Effort: Medium"),
        new("Effort:High", "Effort: High"),
        new("Budget:1024", "Token budget: 1024"),
        new("Budget:4096", "Token budget: 4096")
    ];

    private static string DescribeAcceptedReasoningDial(string modelName)
    {
        if (!Enum.TryParse<LLMModel>(modelName, out var model))
        {
            return "Accepted reasoning dial: unknown model";
        }

        try
        {
            var capabilities = Emerge.GetCapabilities(model);
            var dial = capabilities.AcceptedReasoningDial;
            var hint = dial switch
            {
                ReasoningDial.Effort => "pick an Effort level",
                ReasoningDial.TokenBudget => "pick a Token budget",
                _ => "leave the dial at the model default"
            };

            return $"Accepted reasoning dial: {dial} ({hint}; the other dial is refused at the request) · context {capabilities.ContextWindowSize} tokens";
        }
        catch (Exception ex)
        {
            return $"Accepted reasoning dial: unavailable ({ex.Message})";
        }
    }

    private static void ApplyChatReasoning<T>(EmergePass<T> pass, string choice)
    {
        var parts = choice.Split(':');

        if (parts.Length != 2)
        {
            return;
        }

        if (parts[0] == "Effort")
        {
            pass.ReasoningEffort = Enum.Parse<ReasoningEffort>(parts[1]);
        }
        else if (parts[0] == "Budget")
        {
            var budget = int.Parse(parts[1]);
            pass.ReasoningTokenBudget = budget;
            // Anthropic refuses a thinking budget that is not below the output cap
            pass.MaxOutputTokens = budget + 500;
        }
    }

    private void RenderChatCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Chat");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
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

                    view.Box([FormField.Root, "flex-1"], content: view =>
                    {
                        view.Text([FormField.Label], "Reasoning");
                        view.Select(
                            value: _chatReasoning.Value,
                            options: ChatReasoningOptions,
                            onValueChange: async v => _chatReasoning.Value = v ?? ChatReasoningDefault);
                    });
                });

                view.Text([Text.Caption], DescribeAcceptedReasoningDial(_chatModel.Value), props: TestId("ai-chat-reasoning-dial"));

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
                            emptyView.Text([Text.Caption, "text-muted-foreground"], "No messages");
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

                view.Row([Layout.Row.Md, "mt-4 flex-wrap"], content: row =>
                {
                    row.TextField([Input.Default, "flex-1"],
                        value: _chatInputText.Value,
                        placeholder: "Type a message...",
                        onValueChange: value =>
                        {
                            _chatInputText.Value = value ?? "";
                            return Task.CompletedTask;
                        },
                        onSubmit: async submitted =>
                        {
                            // Read the submitted value directly: clearOnSubmit
                            // fires a trailing onValueChange("") that races with
                            // onSubmit, so _chatInputText.Value can already be
                            // empty by the time we observe it here.
                            var text = (submitted ?? "").Trim();

                            if (!string.IsNullOrEmpty(text) && !_chatIsProcessing.Value)
                            {
                                _chatInputText.Value = "";
                                await SendChatMessageAsync(text);
                            }
                        });

                    row.Button([Button.PrimaryMd],
                        text: _chatIsProcessing.Value ? "Sending..." : "Send",
                        props: TestId("ai-chat-send"),
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

                view.AiDisclosure(AiDisclosureKind.Interaction);

                view.Row([Layout.Row.Md, "mt-4 flex-wrap"], content: row =>
                {
                    row.Button([Button.PrimaryMd],
                        text: "Add Test Message",
                        onClick: AddTestMessage);

                    row.Button([Button.ErrorMd],
                        text: "Clear Chat",
                        onClick: ClearChatMessages);
                });

                RenderEmergeDirectChecks(view);

                if (!string.IsNullOrEmpty(_chatTokenEstimate.Value))
                {
                    view.Text([Text.Caption], _chatTokenEstimate.Value, props: TestId("ai-chat-tokens"));
                }

                if (!string.IsNullOrEmpty(_chatError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], props: TestId("ai-chat-error"), content: view =>
                    {
                        view.Text([Alert.Title], _chatErrorKind.Value ?? "Error");
                        view.Text([Alert.Description], _chatError.Value);
                    });
                }
            });
        });
    }

    private void RenderChatMessage(UIView view, ChatMessageEntry message)
    {
        var isUser = message.Role == ChatMessageRole.User;
        var alignmentClass = isUser ? "ml-auto" : "mr-auto";
        var bubbleStyle = isUser
            ? "bg-brand-solid text-primary-on-brand rounded-2xl rounded-br-md px-4 py-3"
            : "bg-tertiary text-primary rounded-2xl rounded-bl-md px-4 py-3";
        var labelStyle = isUser ? "text-xs text-primary-on-brand/80 mb-1" : "text-xs text-quaternary mb-1";
        var label = isUser ? "You" : "Assistant";

        view.Box([alignmentClass, "max-w-[80%]"], content: wrapper =>
        {
            wrapper.Box([bubbleStyle], content: bubble =>
            {
                bubble.Column(content: col =>
                {
                    col.Text([labelStyle], label);
                    col.Text([Text.Body, isUser ? "text-primary-on-brand" : "text-primary"], message.Content.Value, props: isUser ? null : TestId("ai-chat-reply"));
                });
            });
        });
    }

    private async Task SendChatMessageAsync(string userMessage)
    {
        _chatIsProcessing.Value = true;
        _chatError.Value = null;
        _chatErrorKind.Value = null;

        try
        {
            var userEntry = new ChatMessageEntry { Role = ChatMessageRole.User };
            userEntry.Content.Value = userMessage;
            _chatMessages.Add(userEntry);

            var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
            assistantEntry.Content.Value = "";
            _chatMessages.Add(assistantEntry);

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
            var reasoning = _chatReasoning.Value;
            _chatTokenEstimate.Value = $"Estimated input tokens (KernelContext.EstimateInputTokens): {ctx.EstimateInputTokens()}";

            await foreach (var ev in Emerge.Run<ChatReply>(model, ctx, pass =>
            {
                pass.Command = userMessage;
                pass.SystemPrompt = "You are a helpful assistant. Keep responses concise and friendly.";
                pass.MaxOutputTokens = 500;
                pass.Regions = [region];
                ApplyChatReasoning(pass, reasoning);
            }))
            {
                switch (ev)
                {
                    case ModelText<ChatReply> text:
                        responseText.Append(text.Text);
                        assistantEntry.Content.Value = responseText.ToString();
                        break;

                    case Completed<ChatReply> { Result: { } result }:
                        assistantEntry.Content.Value = result.Response;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            var (kind, message) = DescribeAIFailure(ex);
            _chatErrorKind.Value = kind;
            _chatError.Value = message;

            var errorEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
            errorEntry.Content.Value = $"Error: {message}";
            _chatMessages.Add(errorEntry);
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
        _chatMessages.Add(userEntry);

        var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
        assistantEntry.Content.Value = $"This is a test response to message #{_chatMessages.Value.Count}.";
        _chatMessages.Add(assistantEntry);

        return Task.CompletedTask;
    }

    private Task ClearChatMessages()
    {
        _chatMessages.Clear();
        return Task.CompletedTask;
    }

    // No AI behind it: the composer's own contract (draft, submit, attachments) is what is under
    // test, so the send is echoed back verbatim.
    private void RenderComposerCard(UIView view)
    {
        view.Feature("inputs/composer", content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Composer");
                view.Text([Text.BodySm, "text-tertiary mb-4"], "view.Composer: attach button, drag-and-drop and paste, auto-growing text and a Send that appears once there is something to send. Enter or Send echoes the text back below.");

                view.Box(["w-full"], props: TestId("composer-root"), content: box =>
                {
                    box.Composer(
                        value: _composerDraft.Value,
                        placeholder: "Type a message to echo",
                        attachments: _composerAttachments.Value,
                        maxFileSize: 1024 * 1024,
                        sendLabel: "Send echo",
                        onValueChange: async text => _composerDraft.Value = text,
                        onSubmit: async submitted =>
                        {
                            var text = string.IsNullOrWhiteSpace(submitted) ? _composerDraft.Value : submitted;
                            var attached = 0;

                            // Counted and cleared in one step: an attachment upload completing in between
                            // would otherwise be dropped without being counted in the echo.
                            _composerAttachments.Update(items =>
                            {
                                attached = items.Count;
                                return [];
                            });
                            _composerDraft.Value = "";
                            _composerEcho.Value = attached > 0
                                ? $"Echo: {text.Trim()} (+{attached} attachment{(attached == 1 ? "" : "s")})"
                                : $"Echo: {text.Trim()}";
                        },
                        onAttachmentAdded: async args =>
                        {
                            _composerAttachments.Add(new ComposerAttachment(args.FileName, args.MimeType, args.Size));

                            if (args.LocalTempFilePath is { } path)
                            {
                                try
                                {
                                    File.Delete(path);
                                }
                                catch (IOException)
                                {
                                    // The platform clears its upload temp directory when the app stops
                                }
                            }
                        },
                        onAttachmentRemoved: async index =>
                        {
                            if (index >= 0 && index < _composerAttachments.Count)
                            {
                                _composerAttachments.RemoveAt(index);
                            }
                        });
                });

                view.Text([Text.Body, "mt-3"], _composerEcho.Value, props: TestId("composer-echo"));
                view.Text([Text.Caption], $"Pending attachments: {_composerAttachments.Count}", props: TestId("composer-attachments"));
            });
        });
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
