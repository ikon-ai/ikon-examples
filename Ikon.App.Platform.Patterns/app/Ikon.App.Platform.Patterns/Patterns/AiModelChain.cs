namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: ai-model-chain — see docs/patterns/ai-model-chain.md.
// The example region below is the canonical body the doc extracts.
internal sealed class AiModelChain : IPatternDemo
{
    public string Slug => "ai-model-chain";
    public string Title => "A chain of models from different vendors";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-ai-model-chain
    // Models this app has tested on this task, each from a different vendor, in the order to try them
    private static readonly LLMModel[] SummaryChain = [LLMModel.Claude55Sonnet, LLMModel.Gemini38Flash, LLMModel.Gpt56Sol];

    private readonly ClientReactive<string?> _summary = new(null);
    private readonly ClientReactive<LLMModel?> _answeredBy = new(null);
    private readonly ClientReactive<bool> _busy = new(false);
    private readonly ClientReactive<string?> _error = new(null);

    private async Task SummariseAsync(string text)
    {
        if (_busy.Value)
        {
            return;
        }

        _error.Value = null;
        using var busy = _busy.AsToken();

        try
        {
            var (summary, model) = await RunOnChainAsync(model => Emerge.AskAsync($"Summarise in two sentences:\n{text}", model));
            _summary.Value = summary;
            _answeredBy.Value = model;
        }
        catch (Exception ex) when (ex is AIException or EmergenceStoppedException)
        {
            Log.Instance.Warning($"Summary failed on every model of the chain: {ex.Message}");
            _error.Value = "Couldn't summarise the text. Try again later.";
        }
    }

    // Tries each model in turn; the last model's failure is the caller's. Returns the model that
    // answered, so what is stored or shown names what actually ran.
    private static async Task<(T Result, LLMModel Model)> RunOnChainAsync<T>(Func<LLMModel, Task<T>> runAsync)
    {
        for (int i = 0; ; i++)
        {
            var model = SummaryChain[i];

            try
            {
                return (await runAsync(model), model);
            }
            catch (Exception ex) when (i < SummaryChain.Length - 1 && IsWorthAnotherModel(ex))
            {
                Log.Instance.Warning($"Summary could not run on {model} ({ModelFailure.Classify(ex)}), trying {SummaryChain[i + 1]}: {ex.Message}");
            }
        }
    }

    // Only a failure that says nothing about the request moves on. A request the model refused, an
    // answer that broke the contract (Quality) and an unknown failure would fail the same way on the
    // next model, and a charged or abandoned job has already been paid for.
    private static bool IsWorthAnotherModel(Exception ex)
    {
        if (ex is OperationCanceledException or ProviderChargedException or ProviderJobAbandonedException or InvalidRequestException)
        {
            return false;
        }

        return ModelFailure.Classify(ex)
            is ModelFailureKind.Transient
            or ModelFailureKind.Unavailable
            or ModelFailureKind.AccessDenied
            or ModelFailureKind.CreditExhausted
            or ModelFailureKind.NotAllowed;
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                [Button.PrimaryMd],
                disabled: _busy.Value,
                onClick: async () => await SummariseAsync("The harbour opens at six. Boats leave every hour until the evening ferry at nine."),
                content: v => v.Text(text: _busy.Value ? "Summarising…" : "Summarise"));

            if (_error.Value is string error)
            {
                col.Alert(error, tone: SemanticTone.Error);
            }

            if (_summary.Value is string summary)
            {
                col.Text(text: summary);
                col.Text(["text-muted-foreground text-xs"], text: $"Answered by {_answeredBy.Value?.DisplayName()}");
            }
            else
            {
                col.Text(["text-muted-foreground text-sm"], text: $"Tries {string.Join(", then ", SummaryChain.Select(model => model.DisplayName()))}.");
            }
        });
    }
    #endregion
}
