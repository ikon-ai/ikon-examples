<!-- checked-against: 5ad835b066f0fa0ebbbd8377 -->

# Ikon.App.WhatsApp Guide

Send WhatsApp messages from your app and read the ones people send to it, from the WhatsApp numbers
the platform holds for the app's space. The app never handles a WhatsApp Business account, a
provider credential or a webhook. `app.WhatsApp` (a `WhatsAppService`) is the entry point; the types
are in `Ikon.Common.Core.WhatsApp`, which the scaffold's global usings import (an app scaffolded
earlier gets it from `ikon update`).

The organisation needs the `whatsapp` feature, and the app needs a number connected to it (below).
`app.WhatsApp.GetStatusAsync()` says whether the space is configured and lists its sender numbers.
Each message sent or received is billed to the app's organisation at the price WhatsApp charges.

## Connecting a number

The app sends from a number of your own, or your customer's, registered with WhatsApp through Meta:

```bash
ikon whatsapp add +358401234567 --display-name "Demo Motors" --purpose "Answers questions about cars in stock"
ikon whatsapp status
ikon whatsapp remove
```

`whatsapp add` opens a signup page in the browser. There, whoever holds the business's Facebook
account continues with Facebook, chooses or creates the business portfolio and WhatsApp Business
Account, adds the number, and verifies it with a code Meta sends to it by SMS or a call. The link
works once and expires after 30 minutes, so it can be sent to the business owner (`--no-browser`
prints it without opening it). The number must not be in use on WhatsApp already, and every number
of one app goes into the same WhatsApp Business Account. The Portal's WhatsApp page does the same.

`--purpose` says what the number is for, in a sentence. Meta allows task bots such as support,
bookings and orders, but not general-purpose AI assistants.

A new number usually comes online within minutes. `whatsapp status` shows the state of each number
and what it is waiting for, such as Meta's review of the display name. Until the business completes Meta's
own business verification, a number reaches at most 250 people a day with templates.

`whatsapp remove` takes WhatsApp off the app: every number stops, and the app's WhatsApp account at
the provider is closed and cannot be reopened. The WhatsApp Business Account and its templates stay
in the business's Meta account. Stored messages are deleted on their 31-day schedule.

An app with WhatsApp connected cannot be moved to another organisation, because its numbers belong
to the WhatsApp Business Account of the business that connected them. The organisation that holds
the app removes WhatsApp first; after the move, the new organisation connects a number of its own.

### Testing before a number is connected

`ikon run --whatsapp capture` runs the app locally without sending anything, so it needs no number, no
template push and no approval. Each send is written to the app's `build/whatsapp/` folder as a JSON
file (`to`, `from`, `kind`, then the text, media, content or template fields) and returns `Status`
`Written`. A template send reads `whatsapp/<key>.json` and writes the `body` with its values filled
in, in the language asked for or else the file's first. The sink also refuses every send the
platform would refuse before passing it to WhatsApp: a number that is not bare E.164, a `from` the app does not
hold, text over 4096 characters, media that is not HTTPS, a template the app does not declare, and
template values Meta would not take. The 24-hour window and opt-out are not checked, and
`GetStatusAsync()` still reports only the real numbers. A deployed app has no sink.

## The 24-hour window

WhatsApp lets a business send free-form messages only within 24 hours of the person's last message
to it. Outside that window, and to someone who has never written, only an approved **template**
reaches them. The platform checks this before it passes the send to WhatsApp:

- `SendTextAsync` and `SendMediaAsync` throw `WhatsAppSendRefusedException` with `Reason`
  `WindowClosed` outside the window.
- `SendTemplateAsync` sends one of the app's templates (below) by key, which reaches the person
  whether or not the window is open.
- `GetConversationAsync(counterpart)` returns `WindowOpenUntil` (null when the window is closed) and
  `OptedOut`, so the app can pick the right send without trying one first.

<!-- ikon-example: whatsapp-send -->
```csharp
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
```

Numbers are bare E.164, `+358401234567`. A null `from` uses the space's first online sender; naming a
number the space does not hold throws with `Reason` `SenderNotHeld`.

## Opt-out

A person whose message is only the word `STOP`, `UNSUBSCRIBE`, `CANCEL`, `END`, `QUIT`, `STOPALL`
or `LOPETA` is opted out of messages from that sender, and every send to them throws
`WhatsAppSendRefusedException` with `Reason` `OptedOut` until they send `START`, `UNSTOP`,
`SUBSCRIBE` or `ALOITA`. The one exception is a template of category `utility`, which answers
something the person did, such as an order, and still reaches them. The platform records the
opt-out itself. The app sees only the refusal and `WhatsAppConversation.OptedOut`.

<!-- ikon-example: whatsapp-refused -->
```csharp
try
{
    await app.WhatsApp.SendTextAsync(customer, "Are you still interested?");
}
catch (WhatsAppSendRefusedException ex) when (ex.Reason == WhatsAppRefusalReason.WindowClosed)
{
    Log.Instance.Info($"Window closed for {customer}; queue a template instead");
}
```

A refused send was not sent and is not charged. `NotConfigured` means the space has no WhatsApp yet.

## Templates

A template is a message Meta has reviewed. It is the only kind that reaches a person outside the window.
The app declares each one as a JSON file in `whatsapp/` at the project root; the file name is its
key:

```json
{
  "category": "utility",
  "samples": { "name": "Liisa", "order": "A-1042" },
  "languages": {
    "en": {
      "body": "Hi {{name}}, your order {{order}} has shipped and arrives in two days.",
      "footer": "Demo Shop",
      "buttons": [{ "type": "url", "text": "Track order", "url": "https://shop.example/track/{{order}}" }]
    },
    "fi": { "body": "Hei {{name}}, tilauksesi {{order}} on lähetetty ja perillä kahden päivän päästä." }
  }
}
```

- `category` is `utility` (an answer to something the person did: an order, a booking) or
  `marketing`. Meta may move a template to marketing, which is priced higher; `template list` shows
  the category Meta gave it.
- Each language has a `body` of up to 1024 characters, an optional `footer` of up to 60 without
  variables, and up to 10 `buttons`: `quick_reply` (at most 10), `url` (at most 2), `phone` (one,
  with `phone` in E.164) and `copy_code` (one, with `code`). Button text is up to 25 characters.
- Variables are `{{name}}`: letters, digits and underscores, starting with a letter. Every
  language uses the same ones, and `samples` gives each an example value Meta reviews with.
  Meta refuses two variables next to each other, a body that starts or ends with one, fewer than
  2x+1 fixed words for x variables, and a URL variable anywhere but last after a `/`.
- Headers and authentication (one-time code) templates are not supported yet.

Submit them for review before the deploy that sends them:

```bash
ikon whatsapp template push --dry-run   # what would be submitted, changed nothing
ikon whatsapp template push
ikon whatsapp template list             # each language: approved, pending, rejected with the reason
```

`push` checks every file first and submits nothing if one is wrong, naming the file, the language
and the field. It submits only the languages that changed, each as a version of its own, and the
version already approved is still sent until Meta approves the new one, which takes minutes to
48 hours. A template whose file is gone is deleted from WhatsApp after a confirmation (`--yes`).
`ikon deploy` warns about a template that is not pushed or not approved yet, and deploys anyway.

<!-- ikon-example: whatsapp-template-send -->
```csharp
await app.WhatsApp.SendTemplateAsync(customer, "order_shipped", new Dictionary<string, string> { ["order"] = "A-1042", ["name"] = "Liisa" }, language: "fi");
```

A send with a missing or unknown variable, or a value holding a newline, a tab or more than four
spaces in a row, throws `WhatsAppSendRefusedException` with `Reason` `TemplateValuesInvalid`.
Meta does not fall back between languages, so the platform does: a language with no approved
version sends in the first language of the file that has one, and `WhatsAppSendResult.Language`
names it. With no approved language the send throws with `TemplateNotApproved`. Primary and staging
apps use the same validation and delivery rules, with their own WhatsApp configuration.

## Delivery status

A send returns a `WhatsAppSendResult` with the message id and `Status` `Queued`, on both primary
and staging apps.
A local run started with `ikon run --whatsapp capture` returns `Written` (see "Testing before a number
is connected"). Later status changes arrive as `TriggerEventType.WhatsAppStatus` events whose payload is a
`WhatsAppStatusPayload`: `Delivered`, `Read`, or `Failed` and `Undelivered` with the provider's
`ErrorCode`.

## React when a message arrives

Every message sent to one of the space's numbers is stored, encrypted, and offered to the app as a
`TriggerEventType.WhatsAppReceived` event, delivered to the app's userless instance the same way as
`EmailReceived` (see the email guide). The payload is a `WhatsAppReceivedPayload`, the envelope only
(`Id`, `Sender`, `Counterpart`, `ReceivedAt`, `MediaCount`, `IsButtonReply`); read the text with
`GetMessageAsync` and the files with `DownloadMediaAsync`.

<!-- ikon-example: whatsapp-trigger -->
```csharp
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
```

Delivery is at-least-once: a handler that throws sees the same event again after a backoff, so key
the work on the message id and do it once. The trigger fires only in a deployed app; a local
`ikon run` receives no events, and sends real messages through the platform unless it was started
with `--whatsapp capture`.

## Reading and deleting messages

`GetMessagesAsync` lists the space's messages newest first, both directions, without their text;
`EnumerateMessagesAsync` follows the pages. Messages and their files are kept for 31 days and then
removed. `DeleteAsync` removes a message's text and files at once.
