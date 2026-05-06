<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Vision-Annotation Cache — Describe Once, Quote Forever

Run an LLM vision pass over a small set of images for a record (a car, a product, a property), persist the structured description in a `cloud-json` table, and from then on inject the description into chat prompts as if you'd seen the photos yourself. Cheap LLM, one-shot, never re-billed.

## When to use

You have records with image URLs and a chat agent that wants to "remember" what's in those photos across many conversations. You don't want to send images every turn, and you want the in-text references ("the midnight-blue one") to be consistent.

## Snippet

```csharp
public record VisualDescription(string Overview, string ExteriorColor,
    string Condition, string InteriorNotes, string NotableFeatures);

public class VisualCache(IAppBase app)
{
    private const string DbName = "visuals";
    private readonly ConcurrentDictionary<string, Task<VisualDescription?>> _inflight = new();

    public Task<VisualDescription?> GetOrCreateAsync(string id, string label, IReadOnlyList<string> urls)
        => _inflight.GetOrAdd(id, k => GetOrCreateCoreAsync(k, label, urls));

    private async Task<VisualDescription?> GetOrCreateCoreAsync(string id, string label, IReadOnlyList<string> urls)
    {
        var existing = await ReadAsync(id);
        if (existing != null) { return existing; }
        if (urls.Count == 0) { return null; }

        var parts = new List<IMessagePart> { new TextPart($"Photos of {label}:") };
        foreach (var url in urls.Take(3)) { parts.Add(new ImageUrlPart(url)); }
        var ctx = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, parts.ToArray()));

        var (result, _) = await Emerge.Run<VisualDescription>(LLMModel.Claude45Haiku, ctx, pass =>
        {
            pass.SystemPrompt = "Describe used-item photos so a sales agent can comment naturally. " +
                                "Be concrete and honest. Only describe what's visible. 1 sentence per field.";
            pass.Command = $"Return JSON: {pass.JsonSchema}";
            pass.Temperature = 0.2;
            pass.MaxOutputTokens = 500;
        }).FinalAsync();

        if (result != null) { await WriteAsync(id, result); }
        _inflight.TryRemove(id, out _);
        return result;
    }
}
```

## Notes

- The `_inflight` dict deduplicates concurrent first-touches of the same id (chat usually pings the same record from many turns).
- Cheap model (Haiku) is correct here — visual descriptions don't need flagship reasoning.
- Cache hits cost nothing; emit only on cold miss + non-empty url list.
- Inject the description into the chat system prompt as "you've already shown these photos" so the agent doesn't re-send them.

## See also

- `clickable-reference-card-in-chat`
- `web-research`
