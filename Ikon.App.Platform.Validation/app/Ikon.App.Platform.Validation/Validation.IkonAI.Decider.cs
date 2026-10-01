public partial class Validation
{
    private const string DeciderSampleTicket = "I was charged twice for my subscription this month and I want my money back right now. This is the third time this has happened!";

    private readonly Reactive<string> _deciderState = new(DeciderSampleTicket);
    private readonly Reactive<bool> _deciderProcessing = new(false);
    private readonly Reactive<string?> _deciderResult = new(null);
    private readonly Reactive<string?> _deciderError = new(null);

    private void RenderDeciderCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-decider-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Decider");

            view.Box([FormField.Root], content: view =>
            {
                view.Text([FormField.Label], "State (support ticket)");
                view.TextArea(
                    [Textarea.Default],
                    value: _deciderState.Value,
                    onValueChange: async v => _deciderState.Value = v ?? "");
            });

            view.Row([Layout.Row.Md, "mt-4 items-center flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "Decide",
                    props: TestId("ai-decider-run"),
                    disabled: _deciderProcessing.Value || string.IsNullOrWhiteSpace(_deciderState.Value),
                    onClick: RunDeciderAsync);

                if (_deciderProcessing.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (!string.IsNullOrEmpty(_deciderError.Value))
            {
                view.Box([Alert.Error, "mt-4"], props: TestId("ai-decider-error"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _deciderError.Value);
                });
            }

            if (!string.IsNullOrEmpty(_deciderResult.Value))
            {
                view.Box([Alert.Success, "mt-4"], props: TestId("ai-decider-result"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _deciderResult.Value);
                });
            }
        });
    }

    private async Task RunDeciderAsync()
    {
        _deciderProcessing.Value = true;
        _deciderError.Value = null;
        _deciderResult.Value = null;

        try
        {
            var failures = new List<string>();
            var lines = new List<string>();

            using var decider = new Decider(DecisionModel.Jev);
            decider.Timeout = TimeSpan.FromSeconds(45);

            var capabilities = Decider.GetCapabilities(DecisionModel.Jev);
            lines.Add($"MaxInputTokens: instance {decider.MaxInputTokens}, capabilities {capabilities.MaxInputTokens}");

            var questions = new Dictionary<string, DecisionQuestion>
            {
                ["department"] = DecisionQuestion.Choice("Which team should handle this ticket?", "billing", "technical", "sales"),
                ["frustration"] = DecisionQuestion.Score("How frustrated is the customer?", "calm", "annoyed", "furious"),
                ["refund"] = DecisionQuestion.Noul("Is the customer asking for a refund?", whenTrue: "a refund is requested", whenFalse: "no refund is requested")
            };

            var triage = await decider.DecideAsync(_deciderState.Value, questions);

            if (string.IsNullOrEmpty(triage.Model))
            {
                failures.Add("DecisionResult.Model is empty");
            }

            if (triage.Answers.Count != questions.Count)
            {
                failures.Add($"{triage.Answers.Count} answers for {questions.Count} questions");
            }

            foreach (var (name, question) in questions)
            {
                if (!triage.Answers.TryGetValue(name, out var answer))
                {
                    failures.Add($"no answer for '{name}'");
                    continue;
                }

                if (answer.Type != question.Type)
                {
                    failures.Add($"'{name}' asked as {question.Type}, answered as {answer.Type}");
                }
            }

            var department = triage["department"];
            var departmentChoice = department.AsChoice();

            if (!questions["department"].Options.ContainsKey(departmentChoice))
            {
                failures.Add($"Choice '{departmentChoice}' is not one of the options");
            }

            CheckProbabilities("department", department, failures);
            lines.Add($"Choice department = {departmentChoice} (confidence {department.Confidence:F2}; {FormatProbabilities(department)})");

            var frustration = triage["frustration"];
            var frustrationScore = frustration.AsScore();

            if (frustrationScore < 0 || frustrationScore > questions["frustration"].Levels.Count - 1)
            {
                failures.Add($"Score {frustrationScore} is outside 0..{questions["frustration"].Levels.Count - 1}");
            }

            CheckProbabilities("frustration", frustration, failures);
            var legend = string.Join(", ", frustration.Legend.Select(kv => $"{kv.Key}={kv.Value}"));
            lines.Add($"Score frustration = {frustrationScore:F3} (legend {legend})");

            var refund = triage["refund"];
            var refundOdds = refund.AsNoul();

            if (refundOdds < 0 || refundOdds > 1)
            {
                failures.Add($"Noul {refundOdds} is outside 0..1");
            }

            if (refund.Probabilities.Count != 0)
            {
                failures.Add("a Noul answer carried per-option probabilities");
            }

            lines.Add($"Noul refund = {refundOdds:F3}");

            try
            {
                refund.AsChoice();
                failures.Add("AsChoice on a Noul answer did not throw");
            }
            catch (InvalidOperationException)
            {
                // The throw is the contract being checked: reading a Noul answer as a Choice must refuse
            }

            var order = new DeciderSampleOrder("ORD-1042", "Tampere", ["hockey stick", "puck"], Express: true);
            var warehouse = await decider.DecideAsync(DecisionState.From(order),
                DecisionQuestion.Choice("Which warehouse ships this order? Ship from the one in the destination city.", "helsinki", "tampere"));
            lines.Add($"Structured state (DecisionState.From) warehouse = {warehouse.AsChoice()}");

            var jsonState = DecisionState.FromJson("""{"temperatureCelsius": -25, "windMs": 12}""");

            if (!jsonState.IsJson)
            {
                failures.Add("DecisionState.FromJson did not mark the state as JSON");
            }

            var route = await Decider.ChooseAsync(DecisionState.FromText(_deciderState.Value), "Route this ticket.", "human-agent", "self-service-faq");
            lines.Add($"Decider.ChooseAsync route = {route}");
            lines.Add($"Answered by {triage.Model}");

            if (failures.Count > 0)
            {
                _deciderError.Value = "FAIL: " + string.Join("; ", failures) + "\n" + string.Join("\n", lines);
                return;
            }

            _deciderResult.Value = "PASS\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            var (kind, message) = DescribeAIFailure(ex);
            _deciderError.Value = $"FAIL: {kind}: {message}";
        }
        finally
        {
            _deciderProcessing.Value = false;
        }
    }

    private static void CheckProbabilities(string name, DecisionAnswer answer, List<string> failures)
    {
        if (answer.Probabilities.Count == 0)
        {
            failures.Add($"'{name}' carries no probabilities");
            return;
        }

        var sum = answer.Probabilities.Values.Sum();

        if (Math.Abs(sum - 1.0) > 0.02)
        {
            failures.Add($"'{name}' probabilities sum to {sum:F3}, not 1");
        }
    }

    private static string FormatProbabilities(DecisionAnswer answer)
        => string.Join(", ", answer.Probabilities.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:F2}"));
}

internal sealed record DeciderSampleOrder(string OrderId, string DestinationCity, List<string> Items, bool Express);
