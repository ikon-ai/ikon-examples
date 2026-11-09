public partial class Validation
{
    private const string TelNoNumbersText = "No numbers";
    private const int TelLogCap = 50;

    // Numbers
    private readonly Reactive<bool> _telLoading = new(false);
    private readonly Reactive<string> _telStatusText = new("Not loaded");
    private readonly ReactiveList<TelephonyNumber> _telNumbers = new();

    // SMS send
    private readonly ClientReactive<string> _telSmsTo = new("");
    private readonly ClientReactive<string> _telSmsFrom = new("auto");
    private readonly ClientReactive<string> _telSmsText = new("Hello from the IKON validation app");
    private readonly Reactive<bool> _telSmsBusy = new(false);
    private readonly Reactive<string> _telSmsResult = new("");

    // Inbound. Global, because the trigger handler runs in the shared instance and the Inbound card
    // is open in a user's own.
    private readonly GlobalReactiveList<TelSmsEntry> _telInbox = new(key: "validation-tel-inbox");
    private readonly GlobalReactive<TelSmsEntry?> _telLastSms = new(null, key: "validation-tel-last-sms");
    private readonly Reactive<string> _telHandlerStatus = new("Not registered yet");

    private void InitTelephony()
    {
        // At startup rather than on first unlock: a call that wakes a stopped instance is answered
        // only if Main registered the handler. It installs the handler on the one endpoint host the
        // MCP surface also uses, waiting for that host's start, so it works the same with no numbers
        // or on a local run. Texts need no registration: the declared trigger below is what makes the
        // platform deliver them.
        _ = RegisterCallHandlerAsync();
    }

    private async Task RegisterCallHandlerAsync()
    {
        try
        {
            await app.Telephony.HandleCallsAsync(HandleIncomingCallAsync);
            _telHandlerStatus.Value = "Answered on this instance";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation telephony call handler was not registered, incoming calls are not answered: {ex.GetType().Name}: {ex.Message}");
            _telHandlerStatus.Value = $"Error: not registered: {ex.Message}";
        }
    }

    // Delivered to the shared instance, at least once. The loop that sent a text waits in a browser's
    // instance, so the arrival is written where every instance can read it.
    [Trigger(TriggerEventType.SmsReceived, MaxParallelism = 4)]
    internal async Task OnSmsReceivedAsync(TriggerContext context, CancellationToken ct)
    {
        var message = context.GetPayload<SmsReceivedPayload>();
        var entry = new TelSmsEntry(message.ReceivedAt.UtcDateTime, message.From, message.To, message.Text, message.Id);

        _telInbox.Insert(0, entry);

        while (_telInbox.Count > TelLogCap)
        {
            _telInbox.RemoveAt(_telInbox.Count - 1);
        }

        _telLastSms.Value = entry;
        CompleteSmsLoop(message);
        await RecordSmsLoopArrivalAsync(message);
        Log.Instance.Info($"Validation received SMS {message.Id} from {message.From} to {message.To} ({message.Parts} part)");
    }

    private void RenderTelephonySection(UIView view)
    {
        if (RenderSectionLocked(view, "Telephony"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Telephony");

            RenderTelephonyStatusCard(view);
            RenderTelephonyLoopCard(view);
            RenderTelephonySmsCard(view);
            RenderTelephonyInboundCard(view);
            RenderTelephonyCallCard(view);
            RenderTelephonyIncomingCard(view);
        });
    }

    private void RenderTelephonyStatusCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center justify-between flex-wrap mb-2"], content: view =>
            {
                view.Text([Text.H3], "Numbers");
                view.Button([Button.PrimaryMd],
                    text: _telLoading.Value ? "Loading…" : "Refresh",
                    disabled: _telLoading.Value,
                    props: TestId("tel-refresh"),
                    onClick: RefreshTelephonyAsync);
            });

            RenderFieldGrid(view,
                ("Status", v => v.Text([Text.Body], _telStatusText.Value, props: TestId("tel-status"))),
                ("Incoming calls", v => v.Text([Text.Body], _telHandlerStatus.Value, props: TestId("tel-handler-status"))));

            if (_telNumbers.Count > 0)
            {
                view.Box(["flex flex-col gap-2 mt-4"], props: TestId("tel-numbers"), content: view =>
                {
                    foreach (var number in _telNumbers)
                    {
                        view.Row([Layout.Row.Sm, "items-center flex-wrap"], key: number.Number, content: view =>
                        {
                            view.Text([Text.BodyStrong], number.Number);
                            view.Badge(number.Provider, SemanticTone.Neutral);
                            view.Badge(number.Country, SemanticTone.Neutral);

                            if (number.IsDefault)
                            {
                                view.Badge("default", SemanticTone.Success);
                            }

                            view.Text([Text.Caption], $"{string.Join(", ", number.Capabilities)} · calls to {DescribeInboundIdentity(number.SessionIdentity)}");
                        });
                    }
                });
            }
        });
    }

    private async Task RefreshTelephonyAsync()
    {
        if (_telLoading.Value)
        {
            return;
        }

        _telLoading.Value = true;

        try
        {
            var status = await app.Telephony.GetStatusAsync();
            var numbers = await app.Telephony.GetNumbersAsync();
            _telNumbers.ReplaceAll(numbers);

            _telStatusText.Value = !status.Enabled || numbers.Count == 0 ? TelNoNumbersText : "Enabled";
        }
        catch (TelephonyNumberNotAvailableException)
        {
            _telNumbers.Clear();
            _telStatusText.Value = TelNoNumbersText;
        }
        catch (FeatureNotEnabledException ex)
        {
            _telStatusText.Value = $"Error: the Telephony feature is not enabled for this organisation ({ex.Message})";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation telephony status could not be read: {ex.GetType().Name}: {ex.Message}");
            _telStatusText.Value = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _telLoading.Value = false;
        }
    }

    private static string DescribeInboundIdentity(IReadOnlyDictionary<string, string> identity)
    {
        return identity.Count == 0
            ? "shared instance"
            : string.Join(", ", identity.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private List<SelectOption> TelFromOptions()
    {
        var options = new List<SelectOption> { new("auto", "Platform picks") };

        foreach (var number in _telNumbers)
        {
            options.Add(new SelectOption(number.Number, $"{number.Number} ({number.Provider})"));
        }

        return options;
    }

    private static string? TelFrom(string selected)
    {
        return selected == "auto" || string.IsNullOrWhiteSpace(selected) ? null : selected;
    }

    private static bool IsE164(string number)
    {
        return number.Length is >= 8 and <= 16 && number[0] == '+' && number.Skip(1).All(char.IsDigit);
    }

    private void RenderTelephonySmsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Send SMS");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([FormField.Label], "To (E.164)");
                        view.TextField([Input.Default],
                            value: _telSmsTo.Value,
                            placeholder: "+358401234567",
                            props: TestId("tel-sms-to"),
                            onValueChange: async v => _telSmsTo.Value = (v ?? "").Trim());
                    });

                    view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([FormField.Label], "From");
                        view.Select(
                            ariaLabel: "From",
                            value: _telSmsFrom.Value,
                            options: TelFromOptions(),
                            props: TestId("tel-sms-from"),
                            onValueChange: async v => _telSmsFrom.Value = v ?? "auto");
                    });
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Message");
                    view.TextField([Input.Default],
                        ariaLabel: "Message",
                        value: _telSmsText.Value,
                        props: TestId("tel-sms-text"),
                        onValueChange: async v => _telSmsText.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: _telSmsBusy.Value ? "Sending…" : "Send SMS",
                        disabled: _telSmsBusy.Value || !IsE164(_telSmsTo.Value) || string.IsNullOrWhiteSpace(_telSmsText.Value),
                        props: TestId("tel-sms-send"),
                        onClick: SendTelephonySmsAsync);

                    if (_telSmsTo.Value.Length > 0 && !IsE164(_telSmsTo.Value))
                    {
                        view.Text([Text.Caption, "text-error-primary"], "Use bare E.164: +, country code, number");
                    }
                });

                if (_telSmsResult.Value.Length > 0)
                {
                    view.Text([Text.Body], _telSmsResult.Value, props: TestId("tel-sms-result"));
                }
            });
        });
    }

    private async Task SendTelephonySmsAsync()
    {
        _telSmsBusy.Value = true;
        _telSmsResult.Value = "";

        try
        {
            var result = await app.Telephony.SendSmsAsync(_telSmsTo.Value, _telSmsText.Value, from: TelFrom(_telSmsFrom.Value));
            _telSmsResult.Value = $"PASS sent id={result.MessageId} from={result.From} parts={result.Parts} status={result.Status} replyable={result.Replyable}";
        }
        catch (TelephonyNumberNotAvailableException ex)
        {
            _telSmsResult.Value = $"Error: {TelNoNumbersText} ({ex.Message})";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation SMS to {_telSmsTo.Value} failed: {ex.GetType().Name}: {ex.Message}");
            _telSmsResult.Value = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _telSmsBusy.Value = false;
        }
    }

    private void RenderTelephonyInboundCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Inbound");
            view.Text([Text.Caption, "mb-3"], "Texts reach the app's shared instance as ikon.sms.received events. The latest, then the last 50, shared with every instance:");

            if (_telLastSms.Value is { } last)
            {
                view.Text([Text.Body, "mb-3"], $"{last.AtUtc:yyyy-MM-dd HH:mm:ss} {last.From} → {last.To}: {last.Text}", props: TestId("tel-last-sms"));
            }

            if (_telInbox.Count == 0)
            {
                view.Text([Text.Caption, "text-muted-foreground"], "No messages", props: TestId("tel-inbox-empty"));
                return;
            }

            view.Column([Layout.Column.Sm], content: view =>
            {
                foreach (var entry in _telInbox)
                {
                    view.Box(["rounded-md border border-secondary p-2"], key: entry.MessageId, props: TestId("tel-inbox-item"), content: view =>
                    {
                        view.Text([Text.Caption], $"{entry.AtUtc:HH:mm:ss} {entry.From} → {entry.To}");
                        view.Text([Text.Body], entry.Text);
                    });
                }
            });
        });
    }
}

public sealed record TelSmsEntry(DateTime AtUtc, string From, string To, string Text, string MessageId);
