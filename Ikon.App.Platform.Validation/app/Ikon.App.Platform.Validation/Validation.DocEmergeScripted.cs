// Generated holder for the fences of emergence-guide.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
file static class DocEmergeScripted
{
    private sealed record MyType(string Text);

    private static readonly IReadOnlyList<string> responses = [];
    private static readonly string task = "";

    public static async Task EmsTestingWithMockLlm()
    {
        #region docsnippet:ems-testing-with-mock-llm
        var result = await Emerge.Run<MyType>(
            LLMModel.Claude45Sonnet,
            pass => { pass.Command = task; },
            Emerge.Scripted(responses)  // Replays the given texts in order; no provider call
        );
        #endregion
    }
}
