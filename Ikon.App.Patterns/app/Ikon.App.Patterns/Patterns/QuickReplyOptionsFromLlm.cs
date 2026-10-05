namespace Ikon.App.Patterns.Patterns;

// Pattern: quick-reply-options-from-llm — see docs/patterns/quick-reply-options-from-llm.md.
// The fields outside the region stand in for the app's orchestrator, the active-thread reactive and
// one finished assistant message carrying the <ask> markup. The orchestrator is an empty in-memory
// one with no thread under the demo id, so a pill press finds no thread and posts nothing: a real
// thread would re-engage a paid model.
internal sealed class QuickReplyOptionsFromLlm : IPatternDemo
{
    public string Slug => "quick-reply-options-from-llm";
    public string Title => "Quick-reply options from LLM";
    public string Category => "Chat";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Column(["bg-[#F7F5F2] rounded-2xl p-5 gap-1"], content: bubble => RenderAssistantMessage(bubble));
            col.Text(["text-xs text-zinc-400"],
                "In an app each pill posts its text as the user's reply and re-engages the paused thread; this demo has no live thread, so a press sends nothing");
        });
    }

    private readonly Orchestrator _orchestrator = new();
    private readonly ClientReactive<string?> _activeThreadId = new("demo-trip-planner");

    private readonly string content =
        "I found three direct flights from Helsinki to Lisbon in the week of 14 October, and two hotels near " +
        "Alfama that still have rooms for four nights. " +
        "<ask question=\"Which matters more for this trip?\">" +
        "<option>Cheapest total price</option>" +
        "<option>Shortest travel time</option>" +
        "<option>Walking distance to the old town</option>" +
        "<option>Free cancellation</option>" +
        "</ask>";

    #region example:pattern-quick-reply-options-from-llm
    // In RenderThreadMessage — assistant branch
    private void RenderAssistantMessage(IView view)
    {
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
                view.Row(["flex-wrap gap-2 mt-2"], content: rowView =>
                {
                    foreach (var option in options)
                    {
                        var capturedThreadId = _activeThreadId.Value!;
                        rowView.Button([
                            "bg-black/[0.04] hover:bg-black/[0.08] border border-black/[0.06] rounded-lg px-4 py-2",
                            "text-sm text-black/50 hover:text-black/70 transition-colors duration-200"],
                            onClick: async () => await HandleAskReplyAsync(capturedThreadId, option),
                            content: v => v.Text(text: option));
                    }
                });
            }
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

        if (thread is null || thread.Status.Value is ThreadStatus.Done or ThreadStatus.Failed or ThreadStatus.Archived)
        {
            return;
        }

        // Same channel as a typed reply: post the user turn, then re-engage the paused thread.
        await thread.PostAsync(new Message(Author.User, [new Content.Text(answer)]));
        await thread.ReactivateIfIdleAsync();
    }
    #endregion
}
