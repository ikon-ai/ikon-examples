namespace Ikon.App.Patterns.Examples;

file sealed class TelephonyGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task SendAsync()
    {
        #region example:telephony-send-sms
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
        #region example:telephony-numbers
        var numbers = await app.Telephony.GetNumbersAsync();

        await app.Telephony.SendSmsAsync("+358401234567", "Your table is ready.", from: numbers[0].Number);
        #endregion
    }

    public async Task CallAsync()
    {
        #region example:telephony-call
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
        #region example:telephony-inbound
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
        #region example:telephony-status
        var status = await app.Telephony.GetStatusAsync();

        if (!status.Enabled)
        {
            // Hide the "text me" option rather than letting the send fail.
        }
        #endregion
    }
}
