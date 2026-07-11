<!-- mined-from: Ikon.App.Threads -->
# LLM Title + Goal Generator — Polish User Input Into a Spec

When the user types a freeform prompt to start a thread/job/ticket, run a tiny `Quick`-tier LLM call to produce both a 3-8 word display title and a clean 1-2 sentence goal. Cheap, deterministic (`Temperature = 0`), one shot. Falls back to a string truncation if the call fails.

## When to use

Anywhere you let users start work with a sentence and want a tidy display title plus a normalized "this is what we're trying to do" string. Threads, tasks, support tickets, projects — wherever the raw input is too messy to display.

## Snippet

```csharp
private static async Task<(string Title, string Goal)> GenerateThreadTitleAsync(
    string userInput, List<string>? skillPacks = null)
{
    try
    {
        var model = new DefaultModelResolver().Resolve(Capability.Quick, ModelFamily.Claude);
        var skillContext = skillPacks is { Count: > 0 }
            ? $"\n\nActive skill packs:\n{string.Join("\n", skillPacks.Select(s => $"- {s}"))}"
            : "";

        var result = await Emerge.Run<ThreadTitleResult>(
            model, pass =>
            {
                pass.SystemPrompt = "Generate a short thread title and clear goal from the user's input. " +
                                    "Title: 3-8 words, no quotes, no periods. " +
                                    "Goal: 1-2 sentences that clearly state what should be accomplished, " +
                                    "fixing typos and adding context.";
                pass.Command = $"User input:\n\n{userInput[..Math.Min(500, userInput.Length)]}{skillContext}\n\n" +
                               $"Return JSON:\n{pass.JsonSchema}";
                pass.Temperature = 0;
                pass.MaxOutputTokens = 200;
                pass.MaxIterations = 1;
            }).ResultAsync();

        var title = result.Title?.Trim();
        var goal = result.Goal?.Trim();

        return (
            !string.IsNullOrWhiteSpace(title) ? title : TruncateTitle(userInput),
            !string.IsNullOrWhiteSpace(goal) ? goal : userInput
        );
    }
    catch
    {
        return (TruncateTitle(userInput), userInput);
    }
}

private static string TruncateTitle(string input)
{
    var firstLine = input.Split('\n', 2)[0].Trim();
    return firstLine.Length <= 60 ? firstLine : firstLine[..57] + "...";
}

public record ThreadTitleResult(string Title, string Goal);
```

## Notes

- `Capability.Quick` resolves to whichever fast cheap model is current (Haiku-tier). Don't hardcode model enums for utility calls like this.
- `MaxIterations = 1` and `Temperature = 0` — title/goal generation must be deterministic and cheap.
- Always have a non-LLM fallback (the `TruncateTitle` path). Never let a flaky utility call block a user clicking "Start".
- The 500-char truncation prevents huge pastes from blowing context.

## See also

- `onboarding-name-capture`
- `command-verb-input-with-hints`
