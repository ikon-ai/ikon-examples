namespace Ikon.App.Patterns.Patterns;

// Pattern: llm-vision-cache — see docs/patterns/llm-vision-cache.md.
// The docsnippet region describes a record's photos once with a cheap vision model and persists the
// result; the stubs outside it stand in for the persistence layer the cache reads through.
internal sealed class LlmVisionCache : IPatternDemo
{
    public string Slug => "llm-vision-cache";
    public string Title => "LLM vision cache";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: describes a record's photos once with a cheap vision model and persists the result, coalescing concurrent callers. See the source and docs/patterns/llm-vision-cache.md.");

    private static Task<VisualDescription?> ReadAsync(string id) => throw new NotImplementedException();
    private static Task WriteAsync(string id, VisualDescription description) => throw new NotImplementedException();

    #region docsnippet:pattern-llm-vision-cache
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

            var result = await Emerge.Run<VisualDescription>(LLMModel.Claude45Haiku, ctx, pass =>
            {
                pass.SystemPrompt = "Describe used-item photos so a sales agent can comment naturally. " +
                                    "Be concrete and honest. Only describe what's visible. 1 sentence per field.";
                pass.Command = $"Return JSON: {pass.JsonSchema}";
                pass.Temperature = 0.2;
                pass.MaxOutputTokens = 500;
            });

            await WriteAsync(id, result);
            _inflight.TryRemove(id, out _);
            return result;
        }
    }
    #endregion
}
