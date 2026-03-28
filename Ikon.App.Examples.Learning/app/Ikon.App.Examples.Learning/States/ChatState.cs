namespace Ikon.App.Examples.Learning.States;

public class ChatState(LearningApp outer) : ILearningState
{
    public Task EnterAsync()
    {
        // Clear exercise context for free chat mode
        outer.CurrentExercise = null;
        outer.CurrentArticle = null;

        outer.ChatMessages.Clear();
        outer.ChatMessagesVersion.Value++;

        var greeting = "Hei! Olen Aino, kieltenoppimisavustajasi. Voit jutella kanssani millä tahansa kielellä - ei vain suomeksi! Olen täällä auttamassa sinua harjoittelemaan.";
        outer.AddChatMessage(ChatRole.Assistant, greeting);
        outer.SpeakAsync(greeting).RunParallel();

        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public Task HandleUserMessageAsync(string userId, string text)
    {
        outer.AddChatMessage(ChatRole.User, text);
        return Task.CompletedTask;
    }

    public Task HandleAIMessageAsync(string message)
    {
        outer.AddChatMessage(ChatRole.Assistant, message);
        outer.SpeakAsync(message).RunParallel();
        return Task.CompletedTask;
    }

    private async Task TranslateMessage(ChatMessage message)
    {
        if (message.IsTranslating.Value || message.TranslationValue.Value != null)
        {
            message.ShowTranslation.Value = !message.ShowTranslation.Value;
            return;
        }

        message.IsTranslating.Value = true;

        try
        {
            var translation = await CreateTranslationShader.GenerateAsync(
                LLMModel.Gpt41Mini.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState!,
                message.Content.Value
            );

            message.TranslationValue.Value = translation;
            message.ShowTranslation.Value = true;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error translating message: {ex.Message}");
        }
        finally
        {
            message.IsTranslating.Value = false;
        }
    }

    public void Render(UIView contentView)
    {
        var translations = outer.Translations;
        var _ = outer.ChatMessagesVersion.Value;
        var theme = outer.SelectedTheme.Value;

        contentView.Column(["h-full flex flex-col gap-3 md:gap-4 px-3 md:px-0"], content: view =>
        {
            // Messages area - scrollable, takes remaining space
            view.Box(["flex-1 min-h-0 overflow-hidden"], content: messagesContainer =>
            {
                messagesContainer.ScrollArea(
                    autoScroll: true,
                    autoScrollKey: outer.ChatMessagesVersion.Value.ToString(),
                    rootStyle: [ScrollArea.Root, "h-full py-2 md:py-3"],
                    content: scrollView =>
                {
                    var messageCount = outer.ChatMessages.Count;
                    var messageIndex = 0;

                    foreach (var message in outer.ChatMessages)
                    {
                        messageIndex++;
                        var isLastMessage = messageIndex == messageCount;

                        if (string.IsNullOrEmpty(message.Content.Value))
                        {
                            continue;
                        }

                        var currentMessage = message;
                        var isUser = message.Role == ChatRole.User;

                        scrollView.Box([isUser ? "ml-auto" : "mr-auto", "max-w-[85%] md:max-w-[75%] mb-3",
                            "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-200ms"], content: bubbleWrapper =>
                        {
                            bubbleWrapper.Box([
                                isUser
                                    ? $"{LearningApp.Styles.GetAccentGradient(theme)} text-white rounded-2xl rounded-br-md px-5 py-4 shadow-md"
                                    : "bg-white/90 backdrop-blur-sm border border-white/50 text-[#1a1a1a] rounded-2xl rounded-bl-md px-5 py-4 shadow-sm"
                            ], content: msgView =>
                            {
                                msgView.Column(["gap-2"], content: col =>
                                {
                                    col.Text(["text-base md:text-lg leading-relaxed", isUser ? "text-white" : "text-[#1a1a1a]"], message.Content.Value);

                                    if (message.ShowTranslation.Value && message.TranslationValue.Value != null)
                                    {
                                        col.Box([isUser ? "bg-white/15 rounded-xl px-3 py-2 mt-2" : "bg-gray-100/60 rounded-xl px-3 py-2 mt-2"], content: transView =>
                                        {
                                            transView.Column(["gap-1"], content: transCol =>
                                            {
                                                transCol.Text(["text-sm", isUser ? "text-white/90" : "text-[#4b5563]"],
                                                    message.TranslationValue.Value.TranslatedMessage);

                                                if (!string.IsNullOrEmpty(message.TranslationValue.Value.TransliteratedMessage))
                                                {
                                                    transCol.Text(["text-xs italic", isUser ? "text-white/70" : "text-[#9ca3af]"],
                                                        message.TranslationValue.Value.TransliteratedMessage);
                                                }
                                            });
                                        });
                                    }

                                    // Action buttons - refined pill-style buttons
                                    col.Row(["gap-2 mt-2"], content: actionsRow =>
                                    {
                                        var isTranslating = message.IsTranslating.Value;
                                        var translateLabel = message.ShowTranslation.Value ? "🔽 Hide" : "🌐 Translate";
                                        var translateStyle = isUser
                                            ? "text-sm font-medium px-3 py-1.5 rounded-full bg-white/20 hover:bg-white/30 text-white/90 hover:text-white transition-all duration-200"
                                            : "text-sm font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200";

                                        actionsRow.Button([translateStyle],
                                            onClick: async () =>
                                            {
                                                await TranslateMessage(currentMessage);
                                            },
                                            content: btnView =>
                                            {
                                                if (isTranslating)
                                                {
                                                    btnView.Row(["items-center gap-1.5"], content: row =>
                                                    {
                                                        row.Icon([Icon.Default, $"w-3.5 h-3.5 {LearningApp.Styles.GetAccentText(theme)} animate-spin"], name: "loader");
                                                    });
                                                }
                                                else
                                                {
                                                    btnView.Text([], translateLabel);
                                                }
                                            });

                                        if (!isUser)
                                        {
                                            actionsRow.Button(["text-sm font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200"],
                                                label: "🔊 Play",
                                                onClick: async () =>
                                                {
                                                    await outer.SpeakAsync(currentMessage.Content.Value);
                                                });
                                        }
                                    });
                                });
                            });
                        });
                    }
                });
            });

            // Input area - polished with proper spacing
            view.Box([LearningApp.Styles.GlassCardStrong, "px-4 py-3 md:px-5 md:py-4 rounded-3xl shadow-lg"], content: inputCard =>
            {
                inputCard.Row(["gap-2 md:gap-3 items-center"], content: inputView =>
                {
                    // Microphone button
                    var micStyle = outer.IsRecording.Value
                        ? "w-12 h-12 md:w-14 md:h-14 rounded-full bg-red-500 text-white flex items-center justify-center animate-pulse shadow-lg text-xl"
                        : "w-12 h-12 md:w-14 md:h-14 rounded-full bg-white/80 hover:bg-white text-[#6b7280] hover:text-[#1a1a1a] flex items-center justify-center transition-all duration-200 border border-gray-200/50 shadow-sm text-xl";

                    inputView.CaptureButton(
                        [micStyle],
                        kind: MediaCaptureKind.Audio,
                        label: "🎤",
                        captureMode: MediaCaptureButtonMode.Hold,
                        audioOptions: new ClientAudioCaptureOptions
                        {
                            AutoGainControl = true,
                            NoiseSuppression = true,
                            EchoCancellation = true
                        },
                        onCaptureStart: async e =>
                        {
                            Log.Instance.Info("[Chat] Mic capture started - interrupting speech");
                            outer.IsRecording.Value = true;
                            outer.InterruptSpeaking();
                            await Task.CompletedTask;
                        },
                        onCaptureStop: async e =>
                        {
                            outer.IsRecording.Value = false;
                            await Task.CompletedTask;
                        });

                    inputView.TextField(["flex-1 bg-white/60 border border-gray-200/50 rounded-2xl outline-none px-4 py-3 md:px-5 md:py-3.5 text-[#1a1a1a] placeholder-[#9ca3af] text-[15px] focus:border-gray-300 focus:bg-white/80 transition-all duration-200"],
                        value: outer.InputText.Value,
                        placeholder: "Kirjoita viestisi...",
                        onValueChange: value =>
                        {
                            outer.InputText.Value = value;
                            return Task.CompletedTask;
                        },
                        onSubmit: async _ =>
                        {
                            var text = outer.InputText.Value;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                outer.InputText.Value = "";
                                outer.AddChatMessage(ChatRole.User, text);
                                await outer.ProcessUserMessageAsync(text);
                            }
                        });

                    inputView.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} hover:shadow-lg active:scale-95 text-white px-5 md:px-6 py-3 md:py-3.5 rounded-2xl font-semibold text-sm md:text-base transition-all duration-200 shadow-md"],
                        label: "Lähetä",
                        onClick: async () =>
                        {
                            var text = outer.InputText.Value;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                outer.InputText.Value = "";
                                outer.AddChatMessage(ChatRole.User, text);
                                await outer.ProcessUserMessageAsync(text);
                            }
                        });
                });
            });
        });
    }
}
