using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // Structured result type for the AskAsync<T> example.
    public sealed class TicketClassification
    {
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public double Confidence { get; set; }
        public string SuggestedTeam { get; set; } = "";
    }

    private readonly Reactive<string> _selectedAskExample = new("string");
    private readonly Reactive<string> _askPrompt = new("Summarize the theory of relativity for a curious 12-year-old, in two sentences.");
    private readonly Reactive<string> _askTicket = new("My laptop screen flickers and goes black randomly, and I have a client demo in an hour!");

    private void RenderAskSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Ask — One-Shot Shortcut");
                view.Text([Text.Body, "text-muted-foreground"],
                    "The simplest entry point: a single LLM call with no KernelContext, no tools, no streaming. " +
                    "Defaults to the fast, cheap Claude 4.5 Haiku — ideal for short transformations, classification, and summaries.");
            });

            RenderExampleSelector(view, _selectedAskExample,
                ("string", "AskAsync (string)"),
                ("structured", "AskAsync<T> (structured)"));

            switch (_selectedAskExample.Value)
            {
                case "string":
                    RenderAskStringExample(view);
                    break;
                case "structured":
                    RenderAskStructuredExample(view);
                    break;
            }
        });
    }

    private void RenderAskStringExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "ask-string",
            title: "AskAsync (plain string)",
            description: "Send a prompt, get a plain-text answer back. No schema, no tools.",
            howItWorks:
                "Calls Emerge.AskAsync(prompt) which returns a string directly.\n" +
                "Equivalent to a Run<string>(...).FinalAsync() one-liner, but with zero ceremony.\n" +
                "Uses Claude 4.5 Haiku by default; pass a model to override.",
            runLabel: "Ask",
            resultLabel: "Answer",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Prompt");
                    c.TextArea(
                        [Textarea.Default, "w-full h-20 font-mono text-xs"],
                        value: _askPrompt.Value,
                        placeholder: "Ask anything...",
                        onValueChange: async v => _askPrompt.Value = v ?? "");
                });
            },
            runAction: RunAskStringExample);
    }

    private void RenderAskStructuredExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "ask-structured",
            title: "AskAsync<T> (structured JSON)",
            description: "Send a prompt, get a typed object back — the model is asked for JSON matching T's schema.",
            howItWorks:
                "Calls Emerge.AskAsync<TicketClassification>(prompt) which returns a typed instance.\n" +
                "The library generates the JSON schema from T automatically.\n" +
                "Throws if the model returns nothing or invalid JSON, so it's best for well-scoped extraction/classification.",
            runLabel: "Classify",
            resultLabel: "Classification",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Support Ticket");
                    c.TextArea(
                        [Textarea.Default, "w-full h-20 font-mono text-xs"],
                        value: _askTicket.Value,
                        placeholder: "Describe a support issue...",
                        onValueChange: async v => _askTicket.Value = v ?? "");
                });
            },
            runAction: RunAskStructuredExample);
    }

    private async Task RunAskStringExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Asking...";

        try
        {
            state.Log("Calling Emerge.AskAsync(prompt) — one-shot, no context", LogLevel.Info);

            var answer = await Emerge.AskAsync(_askPrompt.Value, _cts!.Token);

            state.ResultJson.Value = answer;
            state.CurrentStage.Value = "Complete";
            state.Log($"Received {answer.Length} chars", LogLevel.Result);
        }
        catch (OperationCanceledException)
        {
            state.Log("Cancelled", LogLevel.Error);
        }
        catch (Exception ex)
        {
            state.Log($"Error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            state.IsRunning.Value = false;
        }
    }

    private async Task RunAskStructuredExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Classifying...";

        try
        {
            state.Log("Calling Emerge.AskAsync<TicketClassification>(prompt)", LogLevel.Info);

            var command = $"""
                Classify this support ticket. Determine the category, priority (low/medium/high/urgent),
                a confidence score between 0 and 1, and which team should handle it.

                Ticket: "{_askTicket.Value}"
                """;

            var result = await Emerge.AskAsync<TicketClassification>(command, _cts!.Token);

            state.SetResult(result);
            state.CurrentStage.Value = $"Complete — {result.Category} / {result.Priority}";
            state.Log($"Classified as {result.Category} (priority {result.Priority}, confidence {result.Confidence:F2})", LogLevel.Result);
        }
        catch (OperationCanceledException)
        {
            state.Log("Cancelled", LogLevel.Error);
        }
        catch (Exception ex)
        {
            state.Log($"Error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            state.IsRunning.Value = false;
        }
    }
}
