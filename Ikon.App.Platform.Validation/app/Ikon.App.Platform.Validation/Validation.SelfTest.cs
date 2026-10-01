public partial class Validation
{
    public sealed record SelfTestResult(string Id, bool Passed, string Detail, long ElapsedMs);

    private sealed class SelfTestFailure(string message) : Exception(message);

    private sealed record SelfTestCheck(string Id, Func<Task<string>> Run);

    private static readonly TimeSpan SelfTestCheckTimeout = TimeSpan.FromSeconds(10);

    // Opening the tab reruns the battery once results are this old, so a long-lived instance the
    // validator reopens hourly proves the current process rather than replaying a stale verdict.
    private static readonly TimeSpan SelfTestAutoRerunAfter = TimeSpan.FromMinutes(1);

    private readonly ReactiveDictionary<string, SelfTestResult> _selfTestResults = new();
    private readonly Reactive<bool> _selfTestRunning = new(false);
    private readonly Reactive<string> _selfTestSummary = new("Self-test: not run");
    private readonly Reactive<string> _selfTestLastRun = new("");

    // Inbox-only (no push service) under its own storage key, so the battery never touches the
    // inbox the Notifications tab shows.
    private readonly NotificationInbox _selfTestInbox = new((NotificationService?)null, key: "validation.selftest.inbox");
    private readonly SelfTestNotificationChannel _selfTestChannel = new();

    private IReadOnlyList<SelfTestCheck>? _selfTestChecks;
    private int _selfTestGate;
    private DateTime _selfTestFinishedAt = DateTime.MinValue;

    private void InitSelfTest()
    {
        _selfTestInbox.Channels.Add(_selfTestChannel);
    }

    private void RenderSelfTestSection(UIView view)
    {
        MaybeAutoRunSelfTest();

        var checks = SelfTestChecks();

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Self-Test");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Run all",
                        icon: "play",
                        disabled: _selfTestRunning.Value,
                        props: TestId("selftest-run"),
                        onClick: async () => StartSelfTest(fromClick: true));

                    if (_selfTestRunning.Value)
                    {
                        view.Spinner();
                    }

                    var summary = _selfTestSummary.Value;
                    var summaryColor = summary.Contains(" 0 failed", StringComparison.Ordinal) ? "text-success-primary"
                        : summary.Contains(" failed", StringComparison.Ordinal) ? "text-error-primary"
                        : "text-muted-foreground";
                    view.Text([Text.BodyStrong, summaryColor], summary, props: TestId("selftest-summary"));
                });

                if (_selfTestLastRun.Value.Length > 0)
                {
                    view.Text([Text.Caption, "mt-2"], _selfTestLastRun.Value);
                }
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Checks");
                view.Column([Layout.Column.Xs], content: view =>
                {
                    var running = _selfTestRunning.Value;

                    foreach (var check in checks)
                    {
                        if (_selfTestResults.TryGetValue(check.Id, out var result))
                        {
                            var verdict = result.Passed ? "PASS" : "FAIL";
                            view.Text(
                                [Text.Body, "text-sm break-words", result.Passed ? "text-success-primary" : "text-error-primary"],
                                $"{verdict} {check.Id}: {result.Detail}",
                                props: TestId($"selftest-{check.Id}"));
                        }
                        else
                        {
                            view.Text(
                                [Text.Body, "text-sm text-muted-foreground"],
                                running ? $"PENDING {check.Id}" : $"NOT RUN {check.Id}",
                                props: TestId($"selftest-{check.Id}"));
                        }
                    }
                });
            });
        });
    }

    private void MaybeAutoRunSelfTest()
    {
        if (Volatile.Read(ref _selfTestGate) != 0)
        {
            return;
        }

        if (DateTime.UtcNow - _selfTestFinishedAt < SelfTestAutoRerunAfter)
        {
            return;
        }

        StartSelfTest();
    }

    private void StartSelfTest(bool fromClick = false)
    {
        if (Interlocked.CompareExchange(ref _selfTestGate, 1, 0) != 0)
        {
            return;
        }

        // A click is a handler, where writes are legal: flipping to "running" before the click
        // returns means a script reading the summary right after clicking never sees the previous
        // run's verdict. A render cannot write, so the battery flips it itself moments later.
        if (fromClick)
        {
            _selfTestResults.Clear();
            _selfTestRunning.Value = true;
            _selfTestSummary.Value = "Self-test: running";
        }

        _ = Task.Run(async () =>
        {
            // Started from a render or a click, whose reactive tracking flows in through the
            // ExecutionContext. Detached, the result writes land as ordinary updates instead of
            // being swallowed as re-entrant writes of the render that started them.
            using var reactiveDetach = ReactiveManager.SuppressCallbackTracking();

            try
            {
                await RunSelfTestAsync();
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Validation self-test battery aborted: {ex.Message}");
                _selfTestSummary.Value = $"Self-test: aborted ({ex.GetType().Name}: {ex.Message})";
                _selfTestRunning.Value = false;
            }
            finally
            {
                _selfTestFinishedAt = DateTime.UtcNow;
                Interlocked.Exchange(ref _selfTestGate, 0);
            }
        });
    }

    private async Task RunSelfTestAsync()
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        _selfTestRunning.Value = true;
        _selfTestSummary.Value = "Self-test: running";
        _selfTestResults.Clear();

        var checks = SelfTestChecks();
        await Task.WhenAll(checks.Select(RunSelfTestCheckAsync));

        var passed = checks.Count(check => _selfTestResults.TryGetValue(check.Id, out var result) && result.Passed);
        var failed = checks.Count - passed;

        _selfTestFinishedAt = DateTime.UtcNow;
        _selfTestLastRun.Value = $"Last run {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, {started.ElapsedMilliseconds} ms for {checks.Count} checks";
        _selfTestSummary.Value = $"Self-test: {passed} passed, {failed} failed";
        _selfTestRunning.Value = false;
    }

    private async Task RunSelfTestCheckAsync(SelfTestCheck check)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        SelfTestResult result;

        try
        {
            var detail = await Task.Run(check.Run).WaitAsync(SelfTestCheckTimeout);
            result = new SelfTestResult(check.Id, true, OneLine(detail), watch.ElapsedMilliseconds);
        }
        catch (TimeoutException)
        {
            result = new SelfTestResult(check.Id, false, $"timed out after {SelfTestCheckTimeout.TotalSeconds:0} s", watch.ElapsedMilliseconds);
        }
        catch (SelfTestFailure failure)
        {
            result = new SelfTestResult(check.Id, false, OneLine(failure.Message), watch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            result = new SelfTestResult(check.Id, false, OneLine($"{ex.GetType().Name}: {ex.Message}"), watch.ElapsedMilliseconds);
        }

        if (!result.Passed)
        {
            Log.Instance.Warning($"Validation self-test check {check.Id} failed: {result.Detail}");
        }

        _selfTestResults[check.Id] = result;
    }

    private IReadOnlyList<SelfTestCheck> SelfTestChecks()
    {
        return _selfTestChecks ??=
        [
            new("consent", SelfTestConsentAsync),
            new("costs-budget", SelfTestCostsBudgetAsync),
            new("costs-backend", SelfTestCostsBackendAsync),
            new("inbox", SelfTestInboxAsync),
            new("background-work", SelfTestBackgroundWorkAsync),
            new("uploads", SelfTestUploadsAsync),
            new("seed", SelfTestSeedAsync),
            new("shared-session", SelfTestSharedSessionAsync),
            new("reactive-list", SelfTestReactiveListAsync),
            new("reactive-hashset", SelfTestReactiveHashSetAsync),
            new("reactive-scope", SelfTestReactiveScopeAsync),
            new("reactive-readonly", SelfTestReadOnlyReactiveAsync),
            new("reactive-effect", SelfTestReactiveEffectAsync),
            new("user-reactive-list-seed", SelfTestUserReactiveListSeedAsync),
            new("retrier", SelfTestRetrierAsync),
            new("mime-types", SelfTestMimeTypesAsync),
            new("string-distance", SelfTestStringDistanceAsync),
            new("json5", SelfTestJson5Async),
            new("toml", SelfTestTomlAsync),
            new("name-conversions", SelfTestNameConversionsAsync),
            new("extended-cast", SelfTestExtendedCastAsync),
            new("teleport-message", SelfTestTeleportMessageAsync),
            new("pipeline-status", SelfTestPipelineStatusAsync),
            new("sdk-connection-state", SelfTestConnectionStateAsync),
            new("emerge-scripted-run", SelfTestEmergeScriptedRunAsync),
            new("emerge-bestof", SelfTestEmergeBestOfAsync),
            new("emerge-tool-endrun", SelfTestEmergeToolEndRunAsync),
            new("emerge-refine", SelfTestEmergeRefineAsync),
            new("emerge-mapreduce", SelfTestEmergeMapReduceAsync),
            new("llm-capabilities", SelfTestLlmCapabilitiesAsync),
            new("speech-capabilities", SelfTestSpeechCapabilitiesAsync),
            new("ocr-capabilities", SelfTestOcrCapabilitiesAsync),
            new("video-capabilities", SelfTestVideoCapabilitiesAsync),
            new("mesh-capabilities", SelfTestMeshCapabilitiesAsync),
            new("decider-capabilities", SelfTestDeciderCapabilitiesAsync),
            new("video-segmenter-models", SelfTestVideoSegmenterModelsAsync),
            new("region-refusal", SelfTestRegionRefusalAsync),
            new("kernel-context", SelfTestKernelContextAsync),
            new("image-provenance", SelfTestImageProvenanceAsync),
            new("media-provenance", SelfTestMediaProvenanceAsync),
            new("mulaw", SelfTestMuLawAsync),
            new("barge-in", SelfTestBargeInAsync),
            new("speech-mixer", SelfTestSpeechMixerAsync),
        ];
    }

    // Names what a check provokes on purpose — a retried failure, an unmarkable container, an
    // unregistered channel — so the warning the platform logs for it says so, and the validation
    // run's quiet check can tell it from one nobody meant.
    private const string SelfTestDeliberate = "validation-deliberate";

    private static string OneLine(string text) => text.ReplaceLineEndings(" ").Trim();

    private static void Expect(bool condition, string failure)
    {
        if (!condition)
        {
            throw new SelfTestFailure(failure);
        }
    }

    private static TException ExpectThrows<TException>(System.Action action, string what) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException expected)
        {
            return expected;
        }
        catch (Exception other)
        {
            throw new SelfTestFailure($"{what} threw {other.GetType().Name} instead of {typeof(TException).Name}: {other.Message}");
        }

        throw new SelfTestFailure($"{what} did not throw {typeof(TException).Name}");
    }

    private static async Task<TException> ExpectThrowsAsync<TException>(Func<Task> action, string what) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException expected)
        {
            return expected;
        }
        catch (Exception other)
        {
            throw new SelfTestFailure($"{what} threw {other.GetType().Name} instead of {typeof(TException).Name}: {other.Message}");
        }

        throw new SelfTestFailure($"{what} did not throw {typeof(TException).Name}");
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return condition();
    }
}
