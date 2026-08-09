<!-- mined-from: Transcript -->
# MapReduce Long-Document Summary — Chunk → Summarize → Combine

A long document (transcript, paper, report) is split into fixed-size character chunks, each chunk summarized in parallel via `Emerge.MapReduce`, and the per-chunk summaries combined in a single reduce step into one cohesive summary plus action items. Returns structured JSON via the schema interpolated into the prompt.

## When to use

Inputs that won't fit in a single LLM call, or that would (but slowly) — meeting transcripts, long PDFs, multi-hour audio, codebases. The map-reduce shape is also faster than a sequential walk because chunks summarize concurrently. Avoid for inputs under ~4k characters — one direct call is simpler and more coherent.

## Snippet

```csharp
public sealed class TranscriptChunkSummary
{
    public string Summary { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = [];
}

public sealed class TranscriptAnalysis
{
    public string Summary { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = [];
}

private async Task<TranscriptAnalysis> SummarizeTranscriptAsync(string transcriptText, CancellationToken ct)
{
    var ctx = new KernelContext();
    var chunks = SplitTranscript(transcriptText, 4000);
    TranscriptAnalysis? analysis = null;

    await foreach (var ev in Emerge.MapReduce<string, TranscriptChunkSummary, TranscriptAnalysis>(
        LLMModel.Claude45Sonnet, ctx, mr =>
        {
            mr.Chunks = chunks;
            mr.MaxParallel = 3;

            mr.Map(map =>
            {
                map.Command = $"""
                    Summarize the following transcript chunk and extract any action items.
                    Return JSON:
                    {map.JsonSchema}
                    """;
            });

            mr.Reduce(reduce =>
            {
                reduce.Command = $"""
                    Combine the chunk summaries into a concise overall summary.
                    Provide a clear list of actionable follow-ups based on the full transcript.
                    Return JSON:
                    {reduce.JsonSchema}
                    """;
            });
        }).WithCancellation(ct))
    {
        if (ev is Completed<TranscriptAnalysis> completed)
        {
            analysis = completed.Result;
        }
    }

    return analysis ?? new TranscriptAnalysis();
}

private static IReadOnlyList<string> SplitTranscript(string text, int chunkSize)
{
    if (string.IsNullOrWhiteSpace(text)) return [];

    var chunks = new List<string>();
    var start = 0;
    while (start < text.Length)
    {
        var length = Math.Min(chunkSize, text.Length - start);
        chunks.Add(text.Substring(start, length));
        start += length;
    }
    return chunks;
}
```

## Notes

- Three type arguments, in flow order: `TInput` for a chunk of input (a `string` here), `TMapped` for the per-chunk model output, `TResult` for the reduce output. `mr.Chunks` is the list to map over — one LLM call per element. `mr.Input` + `mr.Split` is the alternative lane when the chunking belongs to the pattern rather than the caller.
- `MaxParallel = 3` is conservative — Anthropic / OpenAI rate limits will shape this. Bump up for long inputs once you've measured.
- Interpolate `map.JsonSchema` / `reduce.JsonSchema` into the prompt — Emerge fills in the schema string from the generic type, so prompt and parser stay in sync.
- Listen for `Completed<TFinal>` — there are intermediate events you can also UI-stream (per-chunk progress) but only `Completed` carries the merged result.
- Naive char-split is fine for transcripts; for prose prefer paragraph or sentence boundaries to avoid mid-clause cuts.

## See also

- `web-research` — for retrieval before summarization
- `chatbot-streaming` — when you want token-level streaming UI rather than batch summary
