using System.Runtime.CompilerServices;

public partial class Validation
{
    private readonly Reactive<bool> _emergeDirectProcessing = new(false);
    private readonly Reactive<string?> _emergeDirectResult = new(null);
    private readonly Reactive<string?> _emergeDirectError = new(null);

    // Walks the inner-exception chain because Emerge wraps provider failures in
    // EmergenceStoppedException; the three typed refusals each need a different reaction from an app,
    // so each is named rather than folded into a generic message.
    private static (string Kind, string Message) DescribeAIFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            switch (current)
            {
                case ContentRefusedException refused:
                {
                    var reason = string.IsNullOrEmpty(refused.FinishReason) ? "none given" : refused.FinishReason;
                    return ("Content refused", $"The provider refused the content (finish reason: {reason})");
                }

                case ReasoningBurnException:
                {
                    return ("Reasoning burn", $"The model spent its whole output budget reasoning and produced no answer: {current.Message}");
                }

                case AIRegionPolicyViolationException policy:
                {
                    return ("Region policy", $"The organisation's {policy.Policy} region policy refused model {policy.ModelName}");
                }
            }
        }

        if (exception is EmergenceStoppedException stopped)
        {
            return ("Stopped", $"Emergence stopped: {stopped.Message}");
        }

        return ("Error", exception.Message);
    }

    private void RenderEmergeDirectChecks(UIView view)
    {
        view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
        {
            view.Button(
                [Button.OutlineMd],
                text: "Run Emerge API checks",
                props: TestId("ai-emerge-direct-run"),
                disabled: _emergeDirectProcessing.Value,
                onClick: RunEmergeDirectChecksAsync);

            if (_emergeDirectProcessing.Value)
            {
                view.Box([Icon.Spinner]);
            }
        });

        if (!string.IsNullOrEmpty(_emergeDirectError.Value))
        {
            view.Box([Alert.Error], props: TestId("ai-emerge-direct-error"), content: view =>
            {
                view.Text([Alert.Description, "whitespace-pre-wrap"], _emergeDirectError.Value);
            });
        }

        if (!string.IsNullOrEmpty(_emergeDirectResult.Value))
        {
            view.Box([Alert.Success], props: TestId("ai-emerge-direct-result"), content: view =>
            {
                view.Text([Alert.Description, "whitespace-pre-wrap"], _emergeDirectResult.Value);
            });
        }
    }

    private async Task RunEmergeDirectChecksAsync()
    {
        _emergeDirectProcessing.Value = true;
        _emergeDirectError.Value = null;
        _emergeDirectResult.Value = null;

        try
        {
            var model = LLMModel.Default;
            var lines = new List<string>();
            var failures = new List<string>();

            var context = new KernelContext()
                .Add(new Instruction(InstructionType.Context, "Answer with a single lowercase word and nothing else."))
                .Add(new MessageBlock(MessageBlockRole.User, "What colour is a clear daytime sky?"));

            var estimatedTokens = context.EstimateInputTokens();

            if (estimatedTokens <= 0)
            {
                failures.Add($"EstimateInputTokens returned {estimatedTokens} for a non-empty context");
            }

            var generated = (await Emerge.Generate(model, context).AsStringAsync()).Trim();

            if (string.IsNullOrWhiteSpace(generated))
            {
                failures.Add("Emerge.Generate returned no text");
            }

            lines.Add($"Generate ({model}): \"{Truncate(generated, 60)}\", ~{estimatedTokens} input tokens estimated");

            var forwardedEvents = 0;
            ModelStream countingStream = (streamContext, ct) => CountEventsAsync(Emerge.Generate(model, streamContext, ct: ct), () => forwardedEvents++, ct);

            var streamed = await Emerge.Run<string>(model, pass =>
            {
                pass.Command = "Reply with exactly the word: pong";
                pass.MaxOutputTokens = 20;
            }, countingStream);

            if (forwardedEvents == 0)
            {
                failures.Add("the ModelStream delegate was never pulled");
            }

            lines.Add($"Run through ModelStream: \"{Truncate(streamed.Trim(), 60)}\", {forwardedEvents} LLMEvents forwarded");

            var scripted = await Emerge.Run<string>(model, pass => pass.Command = "ignored", Emerge.Scripted(["scripted-ok"]));

            if (scripted.Trim() != "scripted-ok")
            {
                failures.Add($"Emerge.Scripted replayed \"{scripted}\" instead of \"scripted-ok\"");
            }

            lines.Add($"Scripted: \"{scripted.Trim()}\"");

            if (failures.Count > 0)
            {
                _emergeDirectError.Value = "FAIL: " + string.Join("; ", failures) + "\n" + string.Join("\n", lines);
                return;
            }

            _emergeDirectResult.Value = "PASS\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            var (kind, message) = DescribeAIFailure(ex);
            _emergeDirectError.Value = $"FAIL: {kind}: {message}";
        }
        finally
        {
            _emergeDirectProcessing.Value = false;
        }
    }

    private static async IAsyncEnumerable<LLMEvent> CountEventsAsync(IAsyncEnumerable<LLMEvent> source, System.Action onEvent,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var llmEvent in source.WithCancellation(ct))
        {
            onEvent();
            yield return llmEvent;
        }
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "…";
}
