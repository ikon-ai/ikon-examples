<!-- mined-from: ParallaxDesigner -->
# Quick Edit Find-Replace — Cheap LLM Router For Trivial Changes

Before sending a UI/code edit request to a slow expensive model, route it through a fast cheap model (Claude Haiku) with strict instructions: "if this can be done as substring find/replace, return the pairs; otherwise set IsSimpleEdit=false." The C# side validates each `Find` is in the source, applies the replacements, and only falls through to the heavy model if the router returns `IsSimpleEdit=false` or any `Find` doesn't match.

## When to use

Apps where most user edits are cosmetic (colors, labels, spacing, icon names, prop tweaks) but occasionally structural. Without a router, every edit pays the heavy-model latency. With it, ~80% of edits return in under a second.

## Snippet

```csharp
private async Task<QuickEditResponse?> TryQuickEditAsync(string userMessage)
{
    var ctx = new KernelContext()
        .Add(new Instruction(InstructionType.Context, $"""
            You are a fast UI code editor. Return IsSimpleEdit=true with find-and-replace pairs for ANY change
            expressible as exact substring replacements without restructuring:
            - Colors, spacing, padding, font size/weight, border radius, opacity
            - Text content: labels, headings, placeholder text, button labels
            - Component variants: Button.PrimaryMd→Button.OutlineMd, Text.H2→Text.H3
            - Icon names: name: "check"→name: "x"
            - Numeric values, single-prop flips
            Each Find must be an EXACT substring of the existing code. Each Replace is the substitution.
            Set IsSimpleEdit=false only for changes that add/remove components or new state/handlers.
            """))
        .Add(new MessageBlock(MessageBlockRole.User,
            $"## Existing Code\n```csharp\n{_lastGeneratedCode.Value}\n```\n\n## User Request\n{userMessage}"));

    try
    {
        var result = await Emerge.Run<QuickEditResponse>(LLMModel.Claude45Haiku, ctx, pass =>
        {
            pass.Temperature = 0.1f;
            pass.MaxOutputTokens = 1000;
            pass.Command = $"Return find/replace pairs if simple, else IsSimpleEdit=false.\n\n{pass.JsonSchema}";
        }).ResultAsync();

        if (result.IsSimpleEdit && result.Replacements.Count > 0)
        {
            var code = _lastGeneratedCode.Value;
            foreach (var r in result.Replacements)
            {
                if (!code.Contains(r.Find)) return null;
                code = code.Replace(r.Find, r.Replace);
            }
            _lastGeneratedCode.Value = code;
            return result;
        }
    }
    catch { }
    return null;
}

// Caller: try cheap, fall through to expensive
var quick = await TryQuickEditAsync(userMessage);
if (quick == null)
{
    await RegenerateWithSonnetAsync(userMessage);
}
```

## Notes

- Validate every `Find` is in the source *before* applying any of them — a single bogus pair should abort the whole quick-edit and fall through, not produce a partial garbled file.
- Keep the prompt's category list explicit — the model is otherwise overly cautious and bails to `IsSimpleEdit=false` on cases it can handle.
- Temperature 0.1 — find/replace pairs are not creative work.
- Don't show the user "fast path / slow path" distinction; both should feel like the same edit affordance.

## See also

- `ai-prefill-form-from-description`
- `plan-then-code-iteration`
