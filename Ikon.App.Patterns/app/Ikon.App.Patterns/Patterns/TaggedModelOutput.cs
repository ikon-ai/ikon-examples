// Ikon.AI.Emergence.Structured is NOT in an app's GlobalUsings — a nested namespace is not
// imported by its parent, so an app parsing tagged output adds this itself.
using Ikon.AI.Emergence.Structured;

namespace Ikon.App.Patterns.Patterns;

// Pattern: tagged-model-output — see docs/patterns/tagged-model-output.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class TaggedModelOutput : IPatternDemo
{
    public string Slug => "tagged-model-output";
    public string Title => "Tagged model output";
    public string Category => "Conversational AI";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-tagged-model-output
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

            // HasTag answers "did the model comply" without pulling the content out.
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
