using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class ImapConnectorGuideExamples
{
    public async Task ReadAsync(string appPassword)
    {
        #region example:connectors-imap
        var credentials = new ImapCredentials(
            "user@example.com",
            new ImapServer("imap.example.com", 993, ImapSecurity.SslOnConnect, "user@example.com", appPassword),
            new ImapServer("smtp.example.com", 465, ImapSecurity.SslOnConnect, "user@example.com", appPassword));

        await using var imap = new Imap(credentials);
        await imap.VerifyAsync();

        var folders = await imap.ListFoldersAsync();
        var inbox = folders.First(folder => folder.Role == ImapFolderRole.Inbox);

        var delta = await imap.MessagesDeltaAsync(inbox.Name, since: DateTimeOffset.UtcNow.AddDays(-30));

        foreach (var change in delta.Items.Where(change => !change.Deleted))
        {
            Log.Instance.Info($"{change.Message!.ReceivedAt:g} {change.Message.From?.Address}: {change.Message.Subject}");
        }

        // Keep delta.DeltaToken and pass it back to read what changed since.
        #endregion
    }

    public async Task ReplyAsync(Imap imap, ImapMessageId messageId)
    {
        #region example:connectors-imap-send
        var sent = await imap.ReplyAsync(messageId, new NewImapMessage("")
        {
            Html = "<p>Thanks, got it.</p>",
        });

        if (sent.SentCopyFailure is { } failure)
        {
            Log.Instance.Warning(failure, $"Reply {sent.InternetMessageId} went out but is missing from Sent");
        }
        #endregion
    }
}
