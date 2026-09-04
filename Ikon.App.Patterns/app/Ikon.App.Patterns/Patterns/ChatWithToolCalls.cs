namespace Ikon.App.Patterns.Patterns;

// Pattern: chat-with-tool-calls — see docs/patterns/chat-with-tool-calls.md.
// The stubs outside the region stand in for the chat entry/session model, the tool registration and
// the persistence the app owns; the docsnippet region is the canonical body the doc extracts.
internal sealed class ChatWithToolCalls : IPatternDemo
{
    public string Slug => "chat-with-tool-calls";
    public string Title => "Chat with tool calls";
    public string Category => "Chat";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: streams an Emerge run with registered tools, distinguishing planning narration from the final answer and persisting the messages the run added. See the source and docs/patterns/chat-with-tool-calls.md.");

    private enum ChatMessageRole { User, Model }

    private sealed class ChatMessageEntry
    {
        public ChatMessageRole Role { get; set; }
        public Reactive<bool> IsProcessing { get; } = new(false);
        public Reactive<string> Content { get; } = new("");
    }

    private sealed record ChatResponse(string Response);

    private sealed class ChatSession
    {
        public IReadOnlyList<MessageBlock> Messages { get; } = new List<MessageBlock>();
        public void AppendUserMessage(string message) => throw new NotImplementedException();
        public KernelContext BuildKernelContext() => throw new NotImplementedException();
        public void Replace(IReadOnlyList<MessageBlock> messages) => throw new NotImplementedException();
    }

    private readonly Reactive<bool> _chatIsProcessing = new(false);
    private readonly ReactiveList<ChatMessageEntry> _chatMessages = new();
    private readonly LLMModel model = default;
    private readonly string systemPrompt = "";

    private Task<ChatSession> GetOrHydrateChatSessionAsync(Guid caseId) => throw new NotImplementedException();

    private void RegisterChatTools(EmergePass<ChatResponse> pass, Guid caseId) => throw new NotImplementedException();

    private Task PersistMessageBlockAsync(Guid caseId, MessageBlock message, string? userId) => throw new NotImplementedException();

    #region docsnippet:pattern-chat-with-tool-calls
    private async Task SendChatMessageAsync(string userMessage, Guid caseId)
    {
        _chatIsProcessing.Value = true;

        try
        {
            var session = await GetOrHydrateChatSessionAsync(caseId);
            session.AppendUserMessage(userMessage);

            var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Model };
            assistantEntry.IsProcessing.Value = true;
            _chatMessages.Add(assistantEntry);

            var responseText = new StringBuilder();
            var afterToolCall = false;
            var preRunCount = session.Messages.Count;

            await foreach (var ev in Emerge.Run<ChatResponse>(model, session.BuildKernelContext(), pass =>
            {
                pass.SystemPrompt = systemPrompt;
                pass.Command = userMessage;
                pass.Temperature = 0.3;
                pass.MaxIterations = 15;
                RegisterChatTools(pass, caseId);
            }))
            {
                switch (ev)
                {
                    case ModelText<ChatResponse> text:
                    {
                        // Model text before a tool call is planning narration; the real answer comes after.
                        if (afterToolCall)
                        {
                            responseText.Clear();
                            afterToolCall = false;
                        }

                        responseText.Append(text.Text);
                        assistantEntry.Content.Value = responseText.ToString();
                        break;
                    }

                    case ToolCallPlanned<ChatResponse>:
                    {
                        assistantEntry.IsProcessing.Value = true;
                        break;
                    }

                    case ToolCallResult<ChatResponse>:
                    {
                        afterToolCall = true;
                        assistantEntry.IsProcessing.Value = false;
                        break;
                    }

                    case Completed<ChatResponse> completed:
                    {
                        assistantEntry.IsProcessing.Value = false;

                        if (completed.Result != null && !string.IsNullOrEmpty(completed.Result.Response))
                        {
                            assistantEntry.Content.Value = completed.Result.Response;
                        }

                        var finalMessages = completed.Context.Messages;
                        session.Replace(finalMessages);

                        // Persist only the messages this run added so tool memory survives a refresh.
                        for (int i = preRunCount; i < finalMessages.Count; i++)
                        {
                            await PersistMessageBlockAsync(caseId, finalMessages[i], userId: null);
                        }

                        break;
                    }
                }
            }
        }
        finally
        {
            _chatIsProcessing.Value = false;
        }
    }
    #endregion
}
