// Ikon.AI.Emergence.Structured is NOT in an app's GlobalUsings — a nested namespace is not
// imported by its parent, so an app parsing tagged output adds this itself.
using Ikon.AI.Emergence.Structured;

namespace Ikon.App.Patterns.Patterns;

// Pattern: tagged-model-output — see docs/patterns/tagged-model-output.md.
// The example region below is the canonical body the doc extracts. AskAsync would call a paid
// model, so the demo feeds two recorded replies through the same parse instead: one that uses both
// tags, and one where the model ignored them and the answer falls back to the untagged text.
internal sealed class TaggedModelOutput : IPatternDemo
{
    public string Slug => "tagged-model-output";
    public string Title => "Tagged model output";
    public string Category => "Conversational AI";

    private const string DemoTaggedReply =
        "<thinking>The recipe serves 4 and uses 300 g of flour. For 6 servings scale by 6/4 = 1.5, " +
        "so 300 g × 1.5 = 450 g. Butter: 120 g × 1.5 = 180 g.</thinking>" +
        "<answer>For **6 servings** use **450 g flour** and **180 g butter**; keep the baking time the same.</answer>";

    private const string DemoUntaggedReply =
        "Use 450 g of flour and 180 g of butter for six servings. The baking time does not change.";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Text(["text-sm text-zinc-400"], "Question: How much flour and butter for 6 servings instead of 4?");
            col.Row(["gap-2"], content: row =>
            {
                row.Button([Button.OutlineSm], text: "Reply with tags", onClick: async () => ApplyRecordedReply(DemoTaggedReply));
                row.Button([Button.OutlineSm], text: "Reply without tags", onClick: async () => ApplyRecordedReply(DemoUntaggedReply));
            });
            Render(col);
        });
    }

    private void ApplyRecordedReply(string raw)
    {
        var parsed = StructuredTagParser.Parse(raw, "thinking", "answer");
        _reasoning.Value = parsed.Blocks.FirstOrDefault(b => b.TagName == "thinking")?.Content;
        _answer.Value = StructuredTagParser.GetTagContent(raw, "answer")
            ?? (parsed.PlainText.Length > 0 ? parsed.PlainText : raw);
    }

    #region example:pattern-tagged-model-output
    private readonly ClientReactive<string?> _answer = new(null);
    private readonly ClientReactive<string?> _reasoning = new(null);

    /// <summary>
    /// Tags are the shape for output that is PROSE with side-channels in it -- a visible answer
    /// plus reasoning to hide, or a citation block. Structured JSON (Emerge.Run&lt;T&gt;) is the
    /// better tool the moment the result is really a record.
    /// </summary>
    private async Task AskAsync(string question)
    {
        var raw = await Emerge.AskAsync(
            $"""
             Answer the question. Put your working in <thinking> tags and the reply in <answer>.

             {question}
             """);

        // Parse returns the named blocks AND the PlainText -- what was left after the tagged
        // blocks were lifted out. Rendering raw output instead leaks the reasoning to the user.
        var parsed = StructuredTagParser.Parse(raw, "thinking", "answer");

        _reasoning.Value = parsed.Blocks.FirstOrDefault(b => b.TagName == "thinking")?.Content;

        // A model may simply not emit a tag, so the answer falls back to the untagged remainder
        // rather than rendering nothing.
        _answer.Value = StructuredTagParser.GetTagContent(raw, "answer")
            ?? (parsed.PlainText.Length > 0 ? parsed.PlainText : raw);
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            if (_answer.Value is { } answer)
            {
                col.Markdown(answer);
            }

            // The model may have skipped <thinking>, so the disclosure only shows when there was any.
            if (_reasoning.Value is { } reasoning)
            {
                col.Collapsible(content: disclosure =>
                {
                    disclosure.CollapsibleTrigger(content: t => t.Text(text: "Show working"));
                    disclosure.CollapsibleContent(content: body =>
                        body.Text(["text-muted-foreground text-sm"], text: reasoning));
                });
            }
        });
    }
    #endregion
}
