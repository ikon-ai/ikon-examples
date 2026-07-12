<!-- mined-from: Ikon.App.Bump -->
# Pairwise LLM Matchmaker — Yes/No With Conversation Starters

When you need an LLM to evaluate whether two records (people, items, jobs) genuinely belong together, return both a verdict and the artefacts a UI needs: a topic, a rationale, and 2-3 ready-to-send opener strings. One JSON shape, one call, no follow-ups.

## When to use

You have two profile-like objects on the server and want a structured "is this a good match?" decision plus the words a user would actually use to act on it. Single-pass, low-latency, JSON-only.

## Snippet

```csharp
private async Task<MatchPitch?> GeneratePitchAsync(UserProfile self, UserProfile other)
{
    try
    {
        var context = new KernelContext();
        context = context.Add(new MessageBlock(MessageBlockRole.User,
            BuildMatchmakerPrompt(self, other)));

        var result = await Emerge.Run<MatchPitch>(LLMModel.Claude46Sonnet, context, pass =>
        {
            pass.SystemPrompt = """
                You are a matchmaker for a one-to-one app.
                - Decide whether they would genuinely benefit from meeting.
                  It is better to decline than to force a weak match.
                - If yes, propose ONE specific topic — crisp, one sentence.
                - Explain briefly (1-2 sentences) why this topic fits these two.
                - Offer 2 or 3 concrete opening messages a person could send.
                Always respond in the same language the two people wrote in.
                """;
            pass.Command = "Return the structured pitch. If pairing is weak, set GoodMatch=false.";
            pass.Temperature = 0.7;
            pass.UseJson = true;
            pass.MaxIterations = 1;
        }).ResultAsync();

        return result;
    }
    catch (Exception ex)
    {
        Log.Instance.Warning($"Matchmaker LLM failed: {ex.Message}");
        return null;
    }
}

public record MatchPitch(bool GoodMatch, string Narrative, string ProposedTopic,
    string TopicRationale, List<string> OpeningPrompts);
```

## Notes

- `MaxIterations = 1` keeps it cheap; one call returns everything.
- Decline path matters — empower the LLM to say "no" by giving it a `GoodMatch=false` exit.
- The opener strings are the highest-leverage output: they unblock the user's first move.
- Pair with a small `await Task.Delay(weight)` race so the UI never feels twitchy on a fast model.

## See also

- `parallel-extract-and-reply`
- `quick-reply-options-from-llm`
