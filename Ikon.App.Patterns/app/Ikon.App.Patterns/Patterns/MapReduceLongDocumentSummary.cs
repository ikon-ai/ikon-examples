namespace Ikon.App.Patterns.Patterns;

// Pattern: mapreduce-long-document-summary — see docs/patterns/mapreduce-long-document-summary.md.
// The docsnippet region splits a long document into fixed-size chunks, summarizes each in parallel,
// and reduces the per-chunk summaries into one cohesive result. The example is self-contained.
internal sealed class MapReduceLongDocumentSummary : IPatternDemo
{
    public string Slug => "mapreduce-long-document-summary";
    public string Title => "MapReduce long-document summary";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: splits a long document into fixed-size chunks, summarizes each in parallel, and reduces them into one cohesive summary. See the source and docs/patterns/mapreduce-long-document-summary.md.");

    #region docsnippet:pattern-mapreduce-long-document-summary
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
        if (string.IsNullOrWhiteSpace(text)) { return []; }

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
    #endregion
}
