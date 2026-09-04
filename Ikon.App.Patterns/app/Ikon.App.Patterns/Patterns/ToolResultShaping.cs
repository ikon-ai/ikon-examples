namespace Ikon.App.Patterns.Patterns;

// Pattern: tool-result-shaping — see docs/patterns/tool-result-shaping.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ToolResultShaping : IPatternDemo
{
    public string Slug => "tool-result-shaping";
    public string Title => "Shaping what a tool tells the model";
    public string Category => "Conversational AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Server-side pattern with no UI: what a tool hands back to the model, including media "
        + "and framing text. See the source and docs/patterns/tool-result-shaping.md.");

    private sealed record Invoice(string Id, decimal Total);

    private static Task<Invoice[]> LookupAsync(string customer) => throw new NotImplementedException();
    private static Task<byte[]> RenderChartAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-tool-result-shaping
    /// <summary>
    /// A tool can return the value alone, or a FunctionResult that FRAMES it. The prefix and
    /// suffix are written into the model's transcript around the result, which is where you put
    /// what the data cannot say about itself -- that it is truncated, stale, or empty for a
    /// reason.
    /// </summary>
    [Function("Look up a customer's invoices")]
    public static async Task<FunctionResult> FindInvoices(string customer)
    {
        var invoices = await LookupAsync(customer);

        if (invoices.Length == 0)
        {
            // An empty array reads to the model as "no invoices exist". Saying WHY it is empty is
            // the difference between the model reporting a fact and inventing one.
            return new FunctionResult(invoices,
                modelMessagePrefix: $"No invoices found for '{customer}'. The name may be spelled differently.");
        }

        return new FunctionResult(
            invoices.Take(20).ToArray(),
            modelMessagePrefix: invoices.Length > 20
                ? $"Showing the 20 most recent of {invoices.Length} invoices."
                : null,
            modelMessageSuffix: "Amounts are in EUR.");
    }

    /// <summary>
    /// FunctionMediaResult hands the model an image alongside text. Only providers that support
    /// media in tool results inline it; everything else falls back to ToString, which SUMMARIZES
    /// the media rather than emitting the bytes -- so this degrades instead of flooding a
    /// transcript with base64.
    /// </summary>
    [Function("Chart this month's revenue")]
    public static async Task<FunctionMediaResult> ChartRevenue()
    {
        var png = await RenderChartAsync();
        return new FunctionMediaResult("Revenue by week, this month.", new BinaryDataContainer(png, "image/png"));
    }

    /// <summary>
    /// Returning Emerge.EndRun from a tool body ends the run after the current tool batch instead
    /// of looping back to the model -- for a tool that IS the answer, rather than one feeding it.
    /// </summary>
    [Function("Cancel the operation")]
    public static EndRun<string> Cancel() => Emerge.EndRun("Cancelled at the user's request.");
    #endregion
}
