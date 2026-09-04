<!-- mined-from: Ikon.App.Patterns -->
# Tagged Model Output — Prose With Side-Channels In It

Tags are the right shape when the result is **prose that carries something alongside it**: a
visible answer plus reasoning to hide, a reply plus citations, a message plus a suggested action.
The moment the result is really a record, `Emerge.Run<T>` and structured JSON are the better tool —
tags are not a second-class way to get typed data.

`StructuredTagParser.Parse` returns the named blocks **and** `PlainText`: what was left after the
tagged blocks were lifted out. Rendering the raw response instead is how the reasoning you meant to
hide ends up on screen.

## When to use

Chain-of-thought you do not want shown, `<ask>`/`<option>` prompts, inline citations, a draft plus
a rationale. For anything with a fixed shape, use structured output.

## Notes

- **`using Ikon.AI.Emergence.Structured;` is required** — a nested namespace is not imported by its
  parent, so `GlobalUsings.cs` does not cover it.
- **A model may simply not emit the tag.** `GetTagContent` returns null then, so fall back to
  `PlainText` (or the raw response) rather than rendering nothing — an empty screen reads as a
  broken app, not as a model that ignored an instruction.
- Tag matching is **case-insensitive and tolerates attributes and formatting variations**, so
  `<Answer>` and `<answer lang="en">` both match. It does not need the model to be precise.
- `HasTag` answers "did the model comply" without pulling the content out — useful for a retry
  decision.
- `ParsedBlock` carries `StartIndex`/`EndIndex`, so a renderer can interleave prose and blocks in
  their original order rather than showing all the prose then all the blocks.
- `GetTagContent` returns the **first** occurrence. Use `Parse` when a tag can repeat.
- Streaming? `WithParsedTagsAsync` on the event stream does this incrementally, so a `<thinking>`
  block never flashes on screen before being hidden.

## Snippet

```csharp
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
```

## See also

- `quick-reply-options-from-llm` — `<ask>`/`<option>` pills built on the same parsing.
- `run-trace-and-cost` — what the run cost and which tools it called.
