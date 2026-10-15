<!-- checked-against: b32a792fa9ade70ded3dc268 -->

# IMAP Connector Guide

This guide covers `Ikon.Connectors.Imap` — reading, following, organising and sending the mail of any mailbox reached over IMAP and SMTP — for app developers whose users keep their mail somewhere other than Microsoft 365 or Google Workspace.

## Imap

`Imap` reads a mailbox over IMAP and sends from it over SMTP. It works with any standard mail server: a hosting company's, Fastmail, Zoho, iCloud, or Gmail and Outlook.com with an app password. The types are in namespace `Ikon.Connectors`, not the package's name, so `using Ikon.Connectors.Imap;` fails with CS0246.

A mailbox is described by `ImapCredentials`: the mailbox's own address, and an `ImapServer` for each side with its host, port, `ImapSecurity` (`SslOnConnect`, usually port 993 for IMAP and 465 for SMTP, or `StartTls`, usually 143 and 587) and sign-in. There is no unencrypted option. These details come from the person whose mailbox it is, typically through a settings form, and belong in your own encrypted storage. An account with two-factor sign-in needs an **app-specific password**, because IMAP and SMTP cannot ask for a second factor. Most providers create one in the account's security settings, so your form should say so.

<!-- ikon-example: connectors-imap -->
```csharp
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
```

Construct one `Imap` per mailbox and dispose it. It keeps one IMAP connection open, runs one command at a time on it, and opens it again if the server dropped it while idle. SMTP connects for each send.

`VerifyAsync` signs in to both servers and sends nothing. Run it when a person saves their settings, so a wrong password or port shows up at once and not on the first sync.

## Reaching the server safely

The host comes from a person, but the connection is made from inside the platform's network. So every connection resolves the host first and refuses it unless every address it resolves to is public: a host that points at a private, loopback or link-local address throws. The socket then connects to exactly the addresses that were checked, so a name that answers differently a second time cannot redirect it. Certificates are checked as a mail program checks them, and a self-signed one is refused.

## Errors

A failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"imap"`. Its `ErrorCode` names the server and the cause as `imap.<cause>` or `smtp.<cause>`, so a settings form can point at the right fields:

| Cause | `StatusCode` | Meaning |
|---|---|---|
| `sign_in_refused` | 401 | Wrong username or password, or the account needs an app password. |
| `address_not_allowed` | 403 | The host resolves to an address inside the platform's network. |
| `host_not_found` | 503 | The host name does not resolve. |
| `unreachable` | 503 | Nothing answered on that port, or the connection dropped. |
| `secure_connection_failed` | 503 | TLS failed: often the wrong port for the chosen encryption, or a certificate that does not match the host. |
| `not_a_mail_server` | 503 | Something answered, but not as an IMAP or SMTP server. |
| `folder_not_found`, `message_not_found` | 404 | The folder or message is not on the server. |
| `recipient_refused`, `sender_refused` | null | The SMTP server refused an address. |
| `command_refused` | null | The server refused a command; `Reason` carries its words. |

`IsReconnectRequired` (401 and 403) means the stored settings will keep failing until the person changes them. `IsTransient` (503) means the same call may work later. Inside a session, a `sign_in_refused` usually means the password was changed or the app password was revoked.

## Folders and messages

`ListFoldersAsync` returns every folder the account holds, subscribed or not, as `ImapFolder` records. `Name` is the full path that every other call takes. `Role` (`Inbox`, `Sent`, `Drafts`, `Archive`, `Trash`, `Junk`, `Other`) comes from the server's special-use flags where the server sets them, and otherwise from the folder's usual English names. A folder that only holds other folders has `Selectable` false.

A message is identified by `ImapMessageId`: its folder, the folder's UIDVALIDITY, and its UID. That is how IMAP numbers mail. A UID is unique only within one folder, so a message moved to another folder gets a new id. A server that renumbers a folder (a new UIDVALIDITY) invalidates every id it had. `ToString()` writes the id as `folder:uidvalidity:uid` for storage, and `ImapMessageId.Parse` reads it back.

`ListMessagesPageAsync(folder, since, before, limit, cursor)` lists a folder newest first, one `ImapMessagePage` at a time, as `ImapMessageSummary` envelopes: `ImapAddress` senders and recipients, subject, dates, threading headers, a preview, read and draft state, and whether there are attachments. `GetMessageAsync(id)` returns an `ImapMessage`, which adds the `Text` and `Html` bodies, the `Attachments` as `ImapAttachment` records, and the headers. A message that is gone, or whose folder was renumbered, throws with `StatusCode` 404. `DownloadMessageAsync` returns the raw message and `DownloadAttachmentAsync(id, attachment.Id)` one decoded file. Both are streams over a temporary file that is deleted when you dispose the stream.

## Following a folder

`MessagesDeltaAsync(folder, deltaToken, since, maxPages)` returns an `ImapDelta` of what changed since the token: new messages, messages that left the folder (moved, expunged, or flagged deleted and not yet expunged) as `Deleted`, and messages whose read state changed. Without a token it reports every message received since `since`, counted by day, as new. Store `DeltaToken` and pass it back on the next call. The token is opaque and belongs to its folder. Read each folder you follow with its own token.

IMAP has no push, so call it on a schedule. Where the server supports CONDSTORE, an unchanged folder costs a couple of short commands. Otherwise each call reads the flags of every message the token knows.

A token whose folder was renumbered or removed throws `ConnectorException` with `IsResyncRequired`: drop it and read again without one. More new mail than `maxPages` pages of 50 throws `ConnectorPageCapException<ImapMessageChange>`. Its `Items` are safe to apply, and its `ResumeFrom` is a token to call again with.

## Organising

`MarkReadAsync` and `MarkUnreadAsync` set the read state. `ArchiveAsync` and `DeleteAsync` move messages to the account's Archive and Trash folders (on Gmail, which has no Archive folder, archiving moves them to All Mail), and `MoveMessagesAsync` moves them to any folder. Each returns the messages it moved, with their new ids where the server reports them (UIDPLUS). An id the server no longer holds is skipped. An account without an Archive or Trash folder throws with `ErrorCode` `imap.folder_not_found` before anything moves. `PermanentDeleteAsync` removes messages for good.

## Sending

`SendAsync(NewImapMessage)` sends from the mailbox's address over SMTP, with any files as `NewImapAttachment` streams. It then appends a read copy to the Sent folder, because most SMTP servers keep no copy. Gmail, Outlook.com and iCloud file their own copy, and the connector uses that one instead of adding a second. `ReplyAsync(id, reply, replyAll)` quotes the original below the reply's text and sets `In-Reply-To` and `References`, so mail programs thread it. A reply that names recipients goes to exactly those; one with none goes to the original's Reply-To or sender. Reply All leaves out the mailbox's own address. `ForwardAsync(id, forward)` quotes the original under a "Forwarded message" block and carries its attachments.

The quoting lines ("On … wrote:", "Forwarded message") are in English, whatever language the mailbox's owner uses.

The result is an `ImapSentMessage` with the Message-ID the mail went out with and the `Id` of the copy in Sent. If the copy could not be made, `Id` is null and `SentCopyFailure` says why. The send still happened, so do not send it again.

<!-- ikon-example: connectors-imap-send -->
```csharp
var sent = await imap.ReplyAsync(messageId, new NewImapMessage("")
{
    Html = "<p>Thanks, got it.</p>",
});

if (sent.SentCopyFailure is { } failure)
{
    Log.Instance.Warning(failure, $"Reply {sent.InternetMessageId} went out but is missing from Sent");
}
```
