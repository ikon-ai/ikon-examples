using Ikon.Pipelines.Public.Processors.Pdf;

namespace Ikon.App.Patterns.Patterns;

// Pattern: mapreduce-long-document-summary — see docs/patterns/mapreduce-long-document-summary.md.
// The example region splits a long document into fixed-size chunks, summarizes each in parallel,
// and reduces the per-chunk summaries into one cohesive result. The example is self-contained.
internal sealed class MapReduceLongDocumentSummary : IPatternDemo
{
    public string Slug => "mapreduce-long-document-summary";
    public string Title => "MapReduce long-document summary";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: splits a long document into fixed-size chunks, summarizes each in parallel, and reduces them into one cohesive summary. See the source and docs/patterns/mapreduce-long-document-summary.md.");

    #region example:pattern-mapreduce-long-document-summary
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
            LLMModel.Claude55Sonnet, ctx, mr =>
            {
                mr.Chunks = chunks;
                mr.MaxParallel = 3;

                mr.Map(map =>
                {
                    map.Command = "Summarize the following transcript chunk and extract any action items.";
                });

                mr.Reduce(reduce =>
                {
                    reduce.Command = """
                        Combine the chunk summaries into a concise overall summary.
                        Provide a clear list of actionable follow-ups based on the full transcript.
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

    #region example:pattern-mapreduce-pdf-parts
    public sealed record PartSummary(string Pages, string? Summary);

    // The model reads a long PDF in parts of a few pages each. A part that fails is named in the result
    // and the other parts are still summarised.
    private static async Task<string> SummarizePdfAsync(byte[] pdf, int pagesPerPart, CancellationToken ct)
    {
        var model = LLMModel.Claude55Sonnet;

        if (!Emerge.GetCapabilities(model).SupportsInputPdf)
        {
            throw new InvalidOperationException($"{model} does not read PDFs; send page images instead");
        }

        var parts = new List<(string Pages, byte[] Pdf)>();

        using (var document = PdfDocument.Load(pdf))
        {
            for (var first = 0; first < document.PageCount; first += pagesPerPart)
            {
                var count = Math.Min(pagesPerPart, document.PageCount - first);
                using var part = new MemoryStream();
                document.CopyPages(first, count, part);
                parts.Add(($"{first + 1}-{first + count}", part.ToArray()));
            }
        }

        using var limit = new SemaphoreSlim(3);

        var summaries = await Task.WhenAll(parts.Select(async part =>
        {
            await limit.WaitAsync(ct);

            try
            {
                var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User,
                    [new TextPart($"Pages {part.Pages} of a longer document."), new PdfPart(part.Pdf, "application/pdf")]));

                var summary = await Emerge.Run<string>(model, context,
                    pass => pass.Command = "Summarise these pages in five sentences.", ct);
                return new PartSummary(part.Pages, summary);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && SpendLimitExceededException.Find(ex) is null)
            {
                Log.Instance.Warning($"PDF part {part.Pages} was not summarised: {ex.Message}");
                return new PartSummary(part.Pages, null);
            }
            finally
            {
                limit.Release();
            }
        }));

        var read = summaries.Where(s => s.Summary is not null).Select(s => $"Pages {s.Pages}: {s.Summary}");
        var skipped = summaries.Where(s => s.Summary is null).Select(s => s.Pages).ToList();

        var overview = await Emerge.AskAsync($"Combine these part summaries into one overview:\n{string.Join("\n", read)}", model, ct);

        return skipped.Count == 0 ? overview : $"{overview}\n\nNot read: pages {string.Join(", ", skipped)}";
    }
    #endregion
}
