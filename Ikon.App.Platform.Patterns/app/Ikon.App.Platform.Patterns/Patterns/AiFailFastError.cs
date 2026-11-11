namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: ai-fail-fast-error — see docs/patterns/ai-fail-fast-error.md.
// The example region below is the canonical body the doc extracts.
internal sealed class AiFailFastError : IPatternDemo
{
    public string Slug => "ai-fail-fast-error";
    public string Title => "Fail fast and show the AI error";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-ai-fail-fast-error
    public sealed record ReplyDraft
    {
        public string Subject { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
    }

    private readonly ClientReactive<ReplyDraft?> _draft = new(null);
    private readonly ClientReactive<bool> _busy = new(false);
    private readonly ClientReactive<string?> _error = new(null);

    private async Task DraftReplyAsync(string customerMessage)
    {
        if (_busy.Value)
        {
            return;
        }

        _error.Value = null;
        using var busy = _busy.AsToken();

        try
        {
            _draft.Value = await Emerge.Run<ReplyDraft>(LLMModel.Claude55Sonnet, pass =>
            {
                pass.Command = $"Draft a short, friendly reply to this customer message:\n{customerMessage}";
                pass.MaxRetries = 0;
            });
        }
        catch (Exception ex) when (ex is AIException or EmergenceStoppedException)
        {
            // One attempt, one model: the person is waiting and decides whether to try again
            Log.Instance.Warning($"Reply draft failed ({ModelFailure.Classify(ex)}): {ex.Message}");
            _error.Value = MessageFor(ex);
        }
    }

    // What the screen says. The exception's own text goes to the log; only the platform's denials
    // carry a sentence written for a person.
    private static string MessageFor(Exception ex) => ex switch
    {
        AIPlatformDenialException denial => denial.Message,
        ProviderChargedException => "The draft was made but could not be used. Check your costs before trying again.",
        ProviderJobAbandonedException => "The draft took too long and was stopped. Try again later.",
        InvalidRequestException => "This request can't be made with this model.",
        _ => ModelFailure.Classify(ex) switch
        {
            ModelFailureKind.Transient => "The AI service is busy. Try again in a minute.",
            ModelFailureKind.Quality => "The draft came back unusable. Try again or reword the message.",
            ModelFailureKind.NotAllowed => "Your organisation does not allow this model.",
            ModelFailureKind.Unavailable or ModelFailureKind.AccessDenied or ModelFailureKind.CreditExhausted => "Drafting is unavailable right now.",
            _ => "Something went wrong while drafting.",
        },
    };

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                [Button.PrimaryMd],
                disabled: _busy.Value,
                onClick: async () => await DraftReplyAsync("My order arrived without the charger."),
                content: v => v.Text(text: _busy.Value ? "Drafting…" : "Draft reply"));

            if (_error.Value is string error)
            {
                col.Alert(error, tone: SemanticTone.Error);
            }

            if (_draft.Value is { } draft)
            {
                col.Text(["font-semibold"], text: draft.Subject);
                col.Text(text: draft.Body);
            }
            else
            {
                col.Text(["text-muted-foreground text-sm"], text: "Press Draft reply to write an answer to the customer.");
            }
        });
    }
    #endregion
}
