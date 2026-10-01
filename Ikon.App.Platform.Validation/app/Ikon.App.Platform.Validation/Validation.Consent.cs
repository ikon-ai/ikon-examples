public partial class Validation
{
    // A purpose of the app's own, so the one-click self-check can flip it freely without touching
    // the answers a tester gave for the two platform purposes.
    private const string ConsentSelfCheckPurpose = "validation-self-check";
    private const string ConsentNeverAskedPurpose = "validation-never-asked";
    private const int ConsentChangeLogLimit = 20;

    private static readonly string[] ConsentCardPurposes = [ConsentPurposes.UsageMeasurement, ConsentPurposes.AiProviderPersonalData];

    // Bumped on every OnChanged so the C# card re-reads the ledger and the React panel (which gets
    // it as a prop) re-reads its own answers through the SDK — the second half of the round trip.
    private readonly Reactive<int> _consentVersion = new(0);
    private readonly ReactiveList<ConsentChangeEntry> _consentChanges = new();
    private readonly ClientReactive<string> _consentGateResult = new("");
    private readonly ClientReactive<string> _consentSelfCheckResult = new("");
    private readonly ClientReactive<string> _consentActionResult = new("");
    private readonly Reactive<string> _lastRuntimeErasure = new("never");
    private int _consentChangeCount;

    private void InitConsent()
    {
        app.Consent.OnChanged(OnConsentChanged);
        app.OnUserDataErasure(RememberRuntimeErasureAsync);
    }

    private void OnConsentChanged(ConsentChange change)
    {
        Interlocked.Increment(ref _consentChangeCount);

        _consentChanges.Add(new ConsentChangeEntry(change.UserId, change.Record.Purpose, change.Record.State, DateTime.UtcNow));

        while (_consentChanges.Count > ConsentChangeLogLimit)
        {
            _consentChanges.RemoveAt(0);
        }

        _consentVersion.Value++;
    }

    // The [Trigger(UserErased)] declaration in Validation.Triggers.cs is what makes the platform
    // deliver the erasure at all; this runtime subscription rides the same delivery and proves the
    // OnUserDataErasure overload that carries the erasure id is invoked too.
    private Task RememberRuntimeErasureAsync(UserDataErasureEventArgs args)
    {
        _lastRuntimeErasure.Value = $"erasure {args.ErasureId} for user {args.UserId} at {DateTime.UtcNow:O}";
        Log.Instance.Info($"Runtime erasure handler saw erasure {args.ErasureId} for user {args.UserId}");
        return Task.CompletedTask;
    }

    // The same user the platform's consent functions attribute a client call to, so a grant made
    // from the browser and one made here land in the same ledger.
    private string ConsentUserId() => ReactiveScope.UserIdOrNull ?? app.CurrentUserId;

    private void RenderConsentSection(UIView view)
    {
        _ = _consentVersion.Value;
        string userId = ConsentUserId();

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Consent");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Answers");

                RenderFieldGrid(view,
                    ("User", v => v.Text([Text.Body, "break-all"], string.IsNullOrEmpty(userId) ? "(none)" : userId, props: TestId("consent-user-id"))),
                    ("Never asked", v => v.Text([Text.Body], app.Consent.StateOf(userId, ConsentNeverAskedPurpose).State.ToString(), props: TestId("consent-cs-never-asked"))));

                if (string.IsNullOrEmpty(userId))
                {
                    view.Text([Text.Body, "mt-3 text-error-primary"], "FAIL no user", props: TestId("consent-no-user"));
                }

                view.Column([Layout.Column.Sm, "mt-4"], content: view =>
                {
                    foreach (var purpose in ConsentCardPurposes)
                    {
                        RenderConsentPurposeRow(view, userId, purpose);
                    }
                });

                if (!string.IsNullOrEmpty(_consentActionResult.Value))
                {
                    view.Text([Text.Body, "mt-2"], _consentActionResult.Value, props: TestId("consent-cs-action-result"));
                }

                RenderConsentRecords(view, userId);
                RenderConsentChangeLog(view, userId);
            });

            RenderConsentGate(view);
            RenderConsentSelfCheck(view);
        });
    }

    private void RenderConsentPurposeRow(UIView view, string userId, string purpose)
    {
        var record = app.Consent.StateOf(userId, purpose);

        view.Box([Card.Elevated, "p-3"], key: $"consent-cs-{purpose}", content: view =>
        {
            view.Row(["items-center justify-between gap-3 flex-wrap"], content: view =>
            {
                view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                {
                    view.Text([Text.BodyStrong], purpose);
                    view.Text([Text.Caption], record.DecidedAt is { } decidedAt ? $"{decidedAt:yyyy-MM-dd HH:mm:ss} UTC" : "never answered");
                });

                view.Row([Layout.Row.Sm, "items-center flex-wrap"], content: view =>
                {
                    view.Text(
                        [Text.Body, record.Granted ? "text-success-primary" : "text-secondary"],
                        record.State.ToString(),
                        props: TestId($"consent-cs-state-{purpose}"));
                    view.Button([Button.PrimaryMd], text: "Grant", disabled: string.IsNullOrEmpty(userId),
                        onClick: () => ChangeConsentFromServer(purpose, grant: true), props: TestId($"consent-cs-grant-{purpose}"));
                    view.Button([Button.ErrorMd], text: "Revoke", disabled: string.IsNullOrEmpty(userId),
                        onClick: () => ChangeConsentFromServer(purpose, grant: false), props: TestId($"consent-cs-revoke-{purpose}"));
                });
            });
        });
    }

    private void RenderConsentRecords(UIView view, string userId)
    {
        var records = app.Consent.RecordsFor(userId);
        string summary = records.Count == 0
            ? "0 answers"
            : $"{records.Count} answers: " + string.Join(", ", records.Select(r => $"{r.Purpose}={r.State}"));

        view.Text([Text.H3, "mt-6 mb-2"], "Records");
        view.Text([Text.Body, "break-words"], summary, props: TestId("consent-records"));
    }

    private void RenderConsentChangeLog(UIView view, string userId)
    {
        var mine = _consentChanges.Where(c => c.UserId == userId).ToList();

        view.Text([Text.H3, "mt-6 mb-2"], "Changes");

        if (mine.Count == 0)
        {
            view.Text([Text.Caption], "none", props: TestId("consent-onchanged-last"));
            return;
        }

        var last = mine[^1];
        view.Text([Text.Body], $"{last.Purpose} {last.State}", props: TestId("consent-onchanged-last"));
        view.Text([Text.Caption, "mb-2"], $"{mine.Count} change(s)", props: TestId("consent-onchanged-count"));

        view.Column([Layout.Column.Xs], content: view =>
        {
            for (int i = mine.Count - 1; i >= 0; i--)
            {
                var change = mine[i];
                view.Text([Text.Caption], $"{change.At:HH:mm:ss} {change.Purpose} -> {change.State}", key: $"consent-change-{i}");
            }
        });
    }

    private void RenderConsentGate(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Gated work");
            view.Button([Button.PrimaryMd], text: "Run", onClick: RunConsentGatedWork, props: TestId("consent-gate-run"));

            if (!string.IsNullOrEmpty(_consentGateResult.Value))
            {
                bool ran = _consentGateResult.Value.StartsWith("RAN", StringComparison.Ordinal);
                view.Text([Text.Body, "mt-2", ran ? "text-success-primary" : "text-warning-primary"], _consentGateResult.Value, props: TestId("consent-gate-result"));
            }
        });
    }

    private void RenderConsentSelfCheck(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Self-check");
            view.Button([Button.PrimaryMd], text: "Run", onClick: RunConsentSelfCheck, props: TestId("consent-selfcheck-run"));

            if (!string.IsNullOrEmpty(_consentSelfCheckResult.Value))
            {
                bool passed = _consentSelfCheckResult.Value.StartsWith("PASS", StringComparison.Ordinal);
                view.Text([Text.Body, "mt-2", passed ? "text-success-primary" : "text-error-primary"], _consentSelfCheckResult.Value, props: TestId("consent-selfcheck-result"));
            }
        });
    }

    private void ChangeConsentFromServer(string purpose, bool grant)
    {
        string userId = ConsentUserId();

        try
        {
            var record = grant ? app.Consent.Grant(userId, purpose) : app.Consent.Revoke(userId, purpose);
            _consentActionResult.Value = $"{(grant ? "Grant" : "Revoke")} {purpose} -> {record.State}";
        }
        catch (Exception ex)
        {
            _consentActionResult.Value = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void RunConsentGatedWork()
    {
        try
        {
            app.Consent.Require(ConsentPurposes.UsageMeasurement);
            _consentGateResult.Value = $"RAN gated work: {ConsentPurposes.UsageMeasurement} is granted";
        }
        catch (ConsentRequiredException ex)
        {
            _consentGateResult.Value = $"BLOCKED ConsentRequiredException (purpose {ex.Purpose}): {ex.Message}";
        }
        catch (Exception ex)
        {
            _consentGateResult.Value = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void RunConsentSelfCheck()
    {
        try
        {
            _consentSelfCheckResult.Value = ConsentSelfCheck(ConsentUserId());
        }
        catch (Exception ex)
        {
            _consentSelfCheckResult.Value = $"FAIL {ex.GetType().Name}: {ex.Message}";
        }
    }

    private string ConsentSelfCheck(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return "FAIL no user id in scope";
        }

        var consent = app.Consent;

        if (consent.StateOf(userId, ConsentNeverAskedPurpose).State != ConsentAnswer.NotAsked)
        {
            return $"FAIL a purpose nobody answered read as {consent.StateOf(userId, ConsentNeverAskedPurpose).State}, expected NotAsked";
        }

        if (consent.IsGranted("", ConsentSelfCheckPurpose) || consent.StateOf("", ConsentSelfCheckPurpose).State != ConsentAnswer.NotAsked)
        {
            return "FAIL an anonymous (empty) user read as having answered";
        }

        int changesBefore = Volatile.Read(ref _consentChangeCount);

        var revoked = consent.Revoke(userId, ConsentSelfCheckPurpose);

        if (revoked.State != ConsentAnswer.Denied || revoked.Granted || consent.IsGranted(userId, ConsentSelfCheckPurpose))
        {
            return $"FAIL after Revoke the answer read as {consent.StateOf(userId, ConsentSelfCheckPurpose).State}";
        }

        try
        {
            consent.Require(userId, ConsentSelfCheckPurpose);
            return "FAIL Require did not throw while the purpose was denied";
        }
        catch (ConsentRequiredException ex) when (ex.Purpose == ConsentSelfCheckPurpose && ex.UserId == userId)
        {
            // The throw is the expected outcome; the next step grants and requires again.
        }

        var granted = consent.Grant(userId, ConsentSelfCheckPurpose);

        if (!granted.Granted || granted.DecidedAt is null || consent.StateOf(userId, ConsentSelfCheckPurpose).State != ConsentAnswer.Granted)
        {
            return $"FAIL after Grant the answer read as {consent.StateOf(userId, ConsentSelfCheckPurpose).State}";
        }

        consent.Require(userId, ConsentSelfCheckPurpose);

        if (!consent.RecordsFor(userId).Any(r => r.Purpose == ConsentSelfCheckPurpose && r.Granted))
        {
            return "FAIL RecordsFor does not list the granted answer";
        }

        consent.Revoke(userId, ConsentSelfCheckPurpose);
        int changes = Volatile.Read(ref _consentChangeCount) - changesBefore;

        if (changes < 3)
        {
            return $"FAIL OnChanged fired {changes} time(s) for 3 recorded answers";
        }

        return $"PASS Require threw ConsentRequiredException while denied and passed once granted; OnChanged fired {changes} times; RecordsFor lists {consent.RecordsFor(userId).Count} answers";
    }
}

public sealed record ConsentChangeEntry(string UserId, string Purpose, ConsentAnswer State, DateTime At);
