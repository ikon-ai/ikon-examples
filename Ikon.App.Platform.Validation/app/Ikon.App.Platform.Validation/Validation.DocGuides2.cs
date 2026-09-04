using Ikon.Common.Core.Email;

// The email, telephony, device-capture and persistent-state guides, as code that compiles.
// Each holder is the one file its guide reads as; see Validation.DocGuides.cs for the mechanism.

file sealed class DocEmailGuide(IApp<SessionIdentity, ClientParams> app)
{
    public async Task SendAsync(byte[] pdfBytes)
    {
        #region docsnippet:email-send
        await app.Email.SendAsync(new EmailSendRequest(
            To: "customer@example.com",
            Subject: "Your report is ready",
            HtmlBody: "<p>Find the report attached.</p>",
            TextBody: "Find the report attached.",           // optional plain-text fallback
            ReplyTo: "reports@yourfirm.com",                 // optional; replies go here, not to the From address
            Attachments: [new EmailAttachment("report.pdf", "application/pdf", pdfBytes)],
            Metadata: new Dictionary<string, string> { ["kind"] = "report" }));
        #endregion
    }

    public async Task FallbackAsync(EmailSendRequest request)
    {
        #region docsnippet:email-sender-fallback
        try
        {
            await app.Email.SendAsync(request);
        }
        catch (EmailSenderNotAvailableException)
        {
            // Deliver anyway, from the platform's own address.
            await app.Email.SendAsync(request with { SenderLocalPart = null, SenderDisplayName = null, SenderDomain = null });
        }
        #endregion
    }

    public async Task InboxAsync()
    {
        #region docsnippet:email-inbox
        // One page at a time
        var page = await app.Email.GetInboxPageAsync(new InboxQuery { Limit = 50 });

        // Or enumerate across pages; breaking out stops fetching
        await foreach (var summary in app.Email.EnumerateInboxAsync(new InboxQuery()))
        {
            var detail = await app.Email.GetMessageAsync(summary.Id);

            foreach (var attachment in detail.Attachments)
            {
                await using var download = await app.Email.DownloadAttachmentAsync(detail.Id, attachment.Id);
                // download.Content is the decrypted stream
            }

            await app.Email.DeleteAsync(summary.Id);
        }
        #endregion

        Log.Instance.Debug($"{page}");
    }
}

file sealed class DocTelephonyGuide(IApp<SessionIdentity, ClientParams> app)
{
    public async Task SendAsync()
    {
        #region docsnippet:telephony-send-sms
        // app.Telephony is a TelephonyService — no construction, no provider account of your own.
        var result = await app.Telephony.SendSmsAsync("+358401234567", "Your table is ready.");

        if (!result.Replyable)
        {
            // The recipient got the message but cannot answer it — see "Markets" below.
        }
        #endregion
    }

    public async Task FromNumberAsync()
    {
        #region docsnippet:telephony-numbers
        var numbers = await app.Telephony.GetNumbersAsync();

        await app.Telephony.SendSmsAsync("+358401234567", "Your table is ready.", from: numbers[0].Number);
        #endregion
    }

    public async Task CallAsync()
    {
        #region docsnippet:telephony-call
        await using var call = await app.Telephony.CallAsync("+358401234567");

        await foreach (var audio in call.ListenAsync())
        {
            // … recognise speech, decide what to say …
        }

        await call.HangUpAsync();
        #endregion
    }

    public async Task InboundAsync()
    {
        #region docsnippet:telephony-inbound
        app.Telephony.SmsReceived += async message =>
        {
            await app.Telephony.SendSmsAsync(message.From, $"Thanks — we got: {message.Text}");
        };

        await app.Telephony.HandleCallsAsync(async call =>
        {
            await foreach (var audio in call.ListenAsync())
            {
                // … the caller is speaking …
            }
        });
        #endregion
    }

    public async Task StatusAsync()
    {
        #region docsnippet:telephony-status
        var status = await app.Telephony.GetStatusAsync();

        if (!status.Enabled)
        {
            // Hide the "text me" option rather than letting the send fail.
        }
        #endregion
    }
}

file sealed class DocDeviceCapture(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<double> _peak = new(0);

    private static void Repair(object archive) { }

    private static Task ProcessAsync(AssetUri uri) => Task.CompletedTask;

    public async Task MotionAsync(int sessionId)
    {
        #region docsnippet:device-motion
        app.Motion.OnBatch(batch =>
        {
            foreach (var sample in batch.Samples)
            {
                _peak.Value = Math.Max(_peak.Value, sample.Magnitude);
            }
        });

        await app.Motion.StartTrackingAsync(sessionId, new MotionOptions(
            Hertz: 50,
            Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope,
            BatchMilliseconds: 200));
        #endregion
    }

    public async Task RecordingsAsync(int sessionId, string outingId)
    {
        #region docsnippet:device-recordings
        app.Recordings.OnArchive(archive => Repair(archive));

        await app.Recordings.StartAsync(sessionId, outingId, new RecordingOptions(
            Fixes: true, Motion: true, MaxBytes: 128L * 1024 * 1024));
        #endregion
    }

    public async Task LiveActivityAsync(IReadOnlyList<LiveMetric> metrics)
    {
        #region docsnippet:device-live-activity
        await app.LiveActivity.StartAsync("Momentum", "#db176e",
            [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");

        await app.LiveActivity.UpdateAsync(metrics, status: "Run");
        await app.LiveActivity.EndAsync();
        #endregion
    }

    public void Uploads()
    {
        #region docsnippet:device-uploads
        app.Uploads.Register("my-app.telemetry",
            onStart: args => Task.FromResult(new FileUploadResult
            {
                AssetUri = new AssetUri(AssetClass.CloudFile, $"telemetry/{args.FileName}", app.GlobalState.SpaceId),
            }),
            onComplete: async args =>
            {
                if (args.AssetUri is { } uri) { await ProcessAsync(uri); }
            });
        #endregion
    }
}

file sealed class DocPersistentState(IApp<SessionIdentity, ClientParams> app)
{
    private sealed record MyState(string Title = "");

    private sealed record Prefs(bool DarkMode = false);

    private sealed record Camera(string Id);

    #region docsnippet:persistent-default
    // Default for almost everything you want to persist:
    private readonly PersistentSessionReactive<MyState> _state = new(new MyState());
    #endregion

    #region docsnippet:persistent-backends
    // Default — structured state lands in the app's built-in postgres database
    private readonly PersistentSessionReactive<Prefs> _prefs = new(new Prefs());

    // byte[] payloads stay on asset storage automatically — no backend parameter needed
    private readonly PersistentSessionReactive<byte[]> _snapshot = new([]);

    // Public asset URL needed (uploaded images, published files)
    private readonly PersistentSessionReactive<byte[]> _logo
        = new([], backend: PersistenceBackend.Public);

    // Explicitly target a postgres DB of the app's own
    private readonly PersistentSessionReactive<long> _counter
        = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
    #endregion

    private void ReadWrite(MyState next)
    {
        #region docsnippet:persistent-read-write
        // Read and write like any reactive:
        _state.Value = next;
        var current = _state.Value;
        #endregion

        Log.Instance.Debug($"{current} {_prefs} {_snapshot} {_counter}");
    }

    public void PublicUrl()
    {
        #region docsnippet:persistent-public-url
        var url = _logo.PublicUrl;  // null until first save completes
        #endregion

        Log.Instance.Debug($"{url}");
    }

    private void DynamicKeys(IReadOnlyList<Camera> cameras)
    {
        #region docsnippet:persistent-dynamic-keys
        // WRONG — every loop iteration creates a reactive with the SAME stable id.
        foreach (var camera in cameras)
        {
            var baseline = new PersistentSessionReactive<byte[]>([]);  // collisions!
        }

        // RIGHT — explicit stable key derived from the dynamic identity.
        foreach (var camera in cameras)
        {
            var baseline = new PersistentSessionReactive<byte[]>(
                [],
                key: $"baseline:{camera.Id}");
        }
        #endregion
    }

    public async Task EraseAsync(string userId)
    {
        #region docsnippet:persistent-erase-user
        await app.EraseUserStateAsync(userId);
        #endregion
    }
}
