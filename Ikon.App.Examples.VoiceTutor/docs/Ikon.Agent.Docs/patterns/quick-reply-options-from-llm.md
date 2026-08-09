<!-- mined-from: NoBrainer -->
# Quick-Reply Option Buttons — Parsed From LLM `<ask>` Tag

The agent's message can include an `<ask question="..."><option>A</option><option>B</option></ask>` block. The renderer parses it into (text-before, question, options[]), shows the prose, then the question, then a wrapping row of clickable option pills. Clicking a pill posts that text as the user's reply and reactivates the thread.

## When to use

Anywhere you want the LLM to drive a *bounded* clarification turn (multiple-choice follow-ups, choose-among-suggestions, confirmation prompts). Better UX than a free-text reply when the agent already knows the candidate answers; users tap once.

## Snippet

```csharp
// In RenderThreadMessage — assistant branch
var hasAsk = content.Contains("<ask ");

if (hasAsk)
{
    var (textBefore, question, options) = ParseAskContent(content);

    if (!string.IsNullOrEmpty(textBefore))
        view.Text(["text-sm text-black/45 leading-relaxed font-light"], textBefore);

    if (!string.IsNullOrEmpty(question))
        view.Text(["text-sm text-black/55 font-medium mt-1"], question);

    if (options.Count > 0)
    {
        view.Row(["flex-wrap gap-2 mt-2"], content: view =>
        {
            foreach (var option in options)
            {
                var capturedThreadId = _activeThreadId.Value!;
                view.Button([
                    "bg-black/[0.04] hover:bg-black/[0.08] border border-black/[0.06] rounded-lg px-4 py-2",
                    "text-sm text-black/50 hover:text-black/70 transition-colors duration-200"],
                    onClick: async () => await HandleAskReplyAsync(capturedThreadId, option),
                    content: v => v.Text(text: option));
            }
        });
    }
}

private static (string TextBefore, string Question, List<string> Options) ParseAskContent(string content)
{
    var askIdx = content.IndexOf("<ask ", StringComparison.Ordinal);
    var textBefore = askIdx > 0 ? content[..askIdx].TrimEnd() : "";

    var question = "";
    var qStart = content.IndexOf("question=\"", StringComparison.Ordinal);
    if (qStart >= 0)
    {
        qStart += "question=\"".Length;
        var qEnd = content.IndexOf('"', qStart);
        if (qEnd > qStart)
            question = content[qStart..qEnd]
                .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");
    }

    var options = new List<string>();
    var optStart = 0;
    while ((optStart = content.IndexOf("<option>", optStart, StringComparison.Ordinal)) >= 0)
    {
        optStart += "<option>".Length;
        var optEnd = content.IndexOf("</option>", optStart, StringComparison.Ordinal);
        if (optEnd > optStart)
        {
            options.Add(content[optStart..optEnd].Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"));
            optStart = optEnd;
        }
        else break;
    }

    return (textBefore, question, options);
}

private async Task HandleAskReplyAsync(string threadId, string answer)
{
    var thread = _orchestrator.GetThread(threadId);

    if (thread is null)
    {
        return;
    }

    // Same channel as a typed reply: post the user turn, then re-engage the paused thread.
    await thread.PostAsync(new Message(Author.User, [new Content.Text(answer)]));
    await thread.ReactivateIfIdleAsync();
}
```

## Notes

- Snapshot `_activeThreadId.Value` into a local before the lambda. The handler runs after the click, and reading the reactive inside it would pick up whichever thread is active *then*, not the one this row was rendered for.
- Parser uses raw string scanning instead of XML — the LLM emits inline tags inside prose and full XML parsing rejects it. Decoding `&amp; &lt; &gt; &quot;` covers what the model tends to emit.
- The text *before* the `<ask>` is preserved as a separate paragraph — the LLM often writes a sentence and then asks; both belong on screen.
- Posting the answer goes through the same channel as a typed reply so the rest of the agent loop (transition, thinking indicator, response) needs no special-casing.
- The agent API here is the real one: `_orchestrator.GetThread(id)` → `AgentThread?`, `thread.PostAsync(new Message(Author.User, [new Content.Text(text)]))` (a `Message` carries its text in `Parts` as `Content.Text`), and `thread.ReactivateIfIdleAsync()` (sugar for `TransitionAsync(ThreadTransition.Reactivate)`, a no-op if the thread is already active). There is no `MessageAuthor` type — the author is `Ikon.Agent.Author` with `Author.User` / `Author.Agent(name)`.

## See also

- `chatbot-streaming` — for free-form turns
- `chat-with-tool-calls` — when the LLM emits structured commands rather than questions
