<!-- mined-from: AB2.BirdCard -->
# Parallel Extract + Reply — Two LLM Calls Per Turn

Each user answer kicks off two LLM calls in parallel: one extracts structured data (`TraitDelta`), the other generates the in-character reply. `Task.WhenAll` joins them, then both results are applied — the reply goes into chat, the extraction merges into accumulated state. Halves the per-turn latency vs. running them sequentially.

## When to use

Any conversational app where each turn produces both (a) an in-character reply and (b) structured signal you want to track (sentiment, traits, slot-filling, classification). The two are independent so they should never run sequentially.

## Snippet

```csharp
private async Task ProcessAnswersAsync(SessionState session, int clientId)
{
    var queue = GetOrCreateAnswerQueue(clientId);
    await foreach (var userText in queue.Reader.ReadAllAsync(_appCts.Token))
    {
        if (session.CurrentScreen.Value != Screen.Interview) break;
        session.IsProcessing.Value = true;
        StartProcessingTextCycle(session);
        try
        {
            var question = GetQuestion(session.QuestionIndex.Value);
            var extractionTask = ExtractTraitsAsync(question.Text, userText, _appCts.Token);
            var replyTask     = GenerateCharacterReplyAsync(session, userText, _appCts.Token);
            await Task.WhenAll(extractionTask, replyTask);

            var traitDelta = await extractionTask;
            var reply      = await replyTask;
            if (traitDelta != null) session.AccumulatedTraits.Apply(traitDelta);
            session.InterviewChat.AddModelMessage(reply);

            session.QuestionIndex.Value++;
            if (session.QuestionIndex.Value >= TotalQuestionCount)
            {
                await FinishInterviewAsync(session, _appCts.Token);
                break;
            }
            var next = GetQuestion(session.QuestionIndex.Value);
            session.InterviewChat.AddModelMessage(next.Text);
            session.CurrentQuestion.Value = next.Text;
        }
        finally
        {
            StopProcessingTextCycle(session);
            session.IsProcessing.Value = false;
        }
    }
}

private async Task<TraitDelta?> ExtractTraitsAsync(string question, string answer, CancellationToken ct) =>
    await _extractionChat.GenerateObjectAsync<TraitDelta>(
        parameters: [("ExtractTraits", true), ("CurrentQuestion", question), ("UserAnswer", answer)],
        cancellationToken: ct);

private async Task<string> GenerateCharacterReplyAsync(SessionState session, string userText, CancellationToken ct)
{
    session.InterviewChat.AddUserMessage(userText);
    return await session.InterviewChat.GenerateStringAsync(cancellationToken: ct);
}
```

## Notes

- Use a `Channel<string>` per client to serialize answers — the user can submit again before the previous turn finishes; the channel queues them.
- Use a separate `BasicChat` per role: an `_extractionChat` keeps tight, deterministic prompts; the `interviewChat` carries the conversation history.
- Wrap each call in its own `try/catch` returning a fallback so one failed call doesn't kill the turn.
- `Task.WhenAll` is the right join — both must finish before advancing the question.

## See also

- `batched-turn-window`
- `chat-with-tool-calls`
