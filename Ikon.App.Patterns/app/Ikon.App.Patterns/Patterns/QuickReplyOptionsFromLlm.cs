namespace Ikon.App.Patterns.Patterns;

// Pattern: quick-reply-options-from-llm — see docs/patterns/quick-reply-options-from-llm.md.
// The stubs outside the region stand in for the app's orchestrator, the active-thread reactive, the
// assistant message text, and the render surface the snippet writes into.
internal sealed class QuickReplyOptionsFromLlm : IPatternDemo
{
    public string Slug => "quick-reply-options-from-llm";
    public string Title => "Quick-reply options from LLM";
    public string Category => "Chat";
    public void RenderDemo(IView view) => RenderAssistantMessage(view);

    private readonly Orchestrator _orchestrator = null!;
    private readonly ClientReactive<string?> _activeThreadId = new(null);
    private readonly string content = "";

    #region docsnippet:pattern-quick-reply-options-from-llm
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

        if (thread is null)
        {
            return;
        }

        // Same channel as a typed reply: post the user turn, then re-engage the paused thread.
        await thread.PostAsync(new Message(Author.User, [new Content.Text(answer)]));
        await thread.ReactivateIfIdleAsync();
    }
    #endregion
}
