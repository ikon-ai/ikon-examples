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

    await foreach (var ev in Emerge.MapReduce<TranscriptChunkSummary, TranscriptAnalysis>(
        LLMModel.Claude45Sonnet, ctx, mr =>
        {
            mr.Input = chunks;
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

- Two distinct types: `TChunk` for per-chunk output and `TFinal` for the reduce output — they're often shape-similar but typing them separately stops the reduce from accidentally producing chunk-level granularity.
- `MaxParallel = 3` is conservative — Anthropic / OpenAI rate limits will shape this. Bump up for long inputs once you've measured.
- Interpolate `map.JsonSchema` / `reduce.JsonSchema` into the prompt — Emerge fills in the schema string from the generic type, so prompt and parser stay in sync.
- Listen for `Completed<TFinal>` — there are intermediate events you can also UI-stream (per-chunk progress) but only `Completed` carries the merged result.
- Naive char-split is fine for transcripts; for prose prefer paragraph or sentence boundaries to avoid mid-clause cuts.

## See also

- `web-research` — for retrieval before summarization
- `chatbot-streaming` — when you want token-level streaming UI rather than batch summary
