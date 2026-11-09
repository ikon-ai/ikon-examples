namespace Ikon.App.Platform.Patterns.Examples;

// The WhatsApp guide, as code that compiles. The holder is the one file the guide reads as: its
// shared names are the holder's fields.

file sealed class WhatsAppGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task ReplyAsync(string customer)
    {
        #region example:whatsapp-send
        var conversation = await app.WhatsApp.GetConversationAsync(customer);

        if (conversation.WindowOpenUntil is not null && !conversation.OptedOut)
        {
            await app.WhatsApp.SendTextAsync(customer, "Your order A-1042 has shipped.");
        }
        else
        {
            // Outside the 24-hour window only an approved template reaches them: whatsapp/order_shipped.json
            await app.WhatsApp.SendTemplateAsync(customer, "order_shipped", new Dictionary<string, string> { ["name"] = "Liisa", ["order"] = "A-1042" });
        }
        #endregion
    }

    public async Task TemplateAsync(string customer)
    {
        #region example:whatsapp-template-send
        await app.WhatsApp.SendTemplateAsync(customer, "order_shipped", new Dictionary<string, string> { ["order"] = "A-1042", ["name"] = "Liisa" }, language: "fi");
        #endregion
    }

    public async Task RefusedAsync(string customer)
    {
        #region example:whatsapp-refused
        try
        {
            await app.WhatsApp.SendTextAsync(customer, "Are you still interested?");
        }
        catch (WhatsAppSendRefusedException ex) when (ex.Reason == WhatsAppRefusalReason.WindowClosed)
        {
            Log.Instance.Info($"Window closed for {customer}; queue a template instead");
        }
        #endregion
    }

    #region example:whatsapp-trigger
    [Trigger(TriggerEventType.WhatsAppReceived, MaxParallelism = 4)]
    internal async Task OnWhatsAppReceivedAsync(TriggerContext context, CancellationToken ct)
    {
        // The payload is the envelope only; the text stays behind app.WhatsApp
        var envelope = context.GetPayload<WhatsAppReceivedPayload>();
        var message = await app.WhatsApp.GetMessageAsync(envelope.Id, ct);

        foreach (var media in message.Media)
        {
            await using var download = await app.WhatsApp.DownloadMediaAsync(message.Id, media.Id, ct);
            using var buffer = new MemoryStream();
            await download.Content.CopyToAsync(buffer, ct);
            await app.Files.Data.WriteBytesAsync($"whatsapp/{message.Id}/{media.Id}", buffer.ToArray(), download.ContentType);
        }

        // Returning acknowledges the event; throwing leaves it pending for redelivery with backoff
        Log.Instance.Info($"WhatsApp from {message.Counterpart}: {message.Text?.Length ?? 0} characters, {message.Media.Count} files");
    }
    #endregion
}
