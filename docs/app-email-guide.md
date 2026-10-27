<!-- checked-against: 086438d3e0133b05024025a6 -->

# Ikon.App.Email Guide

Send transactional email from your app and read the mail delivered to your app — through the
platform mailer, with no SMTP credentials, provider account, or DNS setup in the app itself.
`app.Email` (an `EmailService`) is the entry point, available to every app. Each message sent is
billed to the app's organisation. `EmailSendRequest`, `InboxQuery` and the other email types live in
`Ikon.Common.Core.Email`, which the scaffold's global usings import (an app scaffolded earlier gets
them from `ikon update`, or from `ikon update --migrate-global-usings` when its global usings file
predates the managed block).

## Send an email

<!-- ikon-example: email-send -->
```csharp
await app.Email.SendAsync(new EmailSendRequest(
    To: "customer@example.com",
    Subject: "Your report is ready",
    HtmlBody: "<p>Find the report attached.</p>",
    TextBody: "Find the report attached.",           // optional plain-text fallback
    ReplyTo: "reports@yourfirm.com",                 // optional; replies go here, not to the From address
    Attachments: [new EmailAttachment("report.pdf", "application/pdf", pdfBytes)],
    Metadata: new Dictionary<string, string> { ["kind"] = "report" }));
```

The send is **accepted, not delivered**: a successful return means the platform queued the message,
and transient delivery failures are retried server-side. A recipient the platform will not send to is
refused at send time with an `EmailRecipientUndeliverableException`. It carries the `Recipient` and
a `Reason` (an `EmailUndeliverableReason`): `ReservedDomain` for the domains `example.com`,
`example.net`, `example.org` and the `.test`, `.example`, `.invalid` and `.localhost` TLDs, and
`Suppressed` for an address that bounced or complained before. Retrying the same recipient fails the
same way. The exception is an `HttpRequestException` with status 422, so a handler for that still
catches it. Replace the `To` above with a real address before running it. Missing
fields and the limits below, except the metadata key count, throw `ArgumentException` locally, before
anything is sent; a malformed `To` or `ReplyTo` address and too many metadata keys are rejected by the
backend and surface as a `UserException`.

A local `ikon run` does not refuse a reserved-domain recipient: it writes the message to
`build/mail/<time>-<recipient>/` in the app folder (`message.json`, `body.html`, `attachments/`)
and the send succeeds, so the success path can be tested without sending anything.
`ikon run --mail-sink` writes every message there, whatever its recipient.

Any other recipient of a local `ikon run` gets real email from the app's cloud space, but in a local run `app.PublicUrl`
and `app.JoinUrl` are the address of that run, so a link built from them works only while the run is
up:

- By default the address is `https://localhost:…`, which opens only on your own machine.
- With `ikon run --host-lan` it is the LAN address, which opens on the same network.
- With `ikon run --host-public` it is the IKON relay address, which opens anywhere.

To test links that must keep working, send the email from a deployed app.

Limits, enforced by the backend and, except the metadata key count, also checked in the client:

| What | Limit |
| --- | --- |
| Total payload (subject, bodies, metadata, attachments as base64) | 10 MB |
| Subject | 200 characters |
| Attachments | 10 per email; filename ≤ 255, MIME type ≤ 100 characters |
| Metadata | string values only, at most 20 keys |

Attachments count at their base64-encoded size (~4/3 of the raw bytes), so the practical budget for
raw attachment bytes is roughly 7.5 MB.

The platform deliberately keeps no copy of the messages an app sends, including their subjects,
bodies and attachments. An app that needs a log of
sent mail, to show a history or to prove what went out, keeps its own, for example a database row
written when `SendAsync` returns.

### One-click unsubscribe headers

Mail sent in bulk, such as a newsletter, needs the `List-Unsubscribe` and `List-Unsubscribe-Post`
headers, or the large mailbox providers send it to spam. Set them with `Headers`:

<!-- ikon-example: email-unsubscribe-headers -->
```csharp
await app.Email.SendAsync(new EmailSendRequest(To: subscriber, Subject: "October news", HtmlBody: html)
{
    // unsubscribeUrl is a public [HttpPost] endpoint of the app; a mail provider posts the one-click body to it
    Headers = new Dictionary<string, string>
    {
        ["List-Unsubscribe"] = $"<{unsubscribeUrl}>",
        ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
    },
});
```

`List-Unsubscribe` takes one or more `<https://...>` or `<mailto:...>` targets separated by commas, and
`List-Unsubscribe-Post` must be exactly `List-Unsubscribe=One-Click` with an https target. These are the
only headers an app can set; any other name throws `ArgumentException`, because the platform sets the
From, routing and signing headers itself.

### Keep the list clean — sending can be paused

Receiving mail servers judge every message by the reputation of the servers it came from, so the
platform holds each app to the limits they apply. Over the last seven days, an app's messages must
stay under 0.3% spam complaints and 5% hard bounces (addresses that do not exist); a small sender is
not paused for fewer than 3 complaints or 10 hard bounces, whatever the rate. An app that crosses a
line has its email paused, and its owners are told why. Sign-in codes do not count toward it.

While paused, every `SendAsync` throws `EmailSendingRestrictedException` — nothing is queued, mail
already queued is dropped, and retrying does not help — until IKON support reviews the sending and restores it. Sign-in codes and
other account mail keep working. Send only to people who asked for the mail, and never to a list
bought or scraped from elsewhere.

## The From address and sender identity

The platform owns the From **domain** — it only ever sends from a domain the app has verified for
sending, or from its own default address, so an app cannot impersonate an address it does not control. Within that, the request
chooses the identity:

- **`SenderLocalPart`** — the part before the `@`. Lowercase letters, digits, dot, underscore and
  hyphen, starting and ending alphanumeric, with no two dots in a row, at most 64 characters. The
  value is trimmed and lowercased before it is checked. Names that belong to the mail
  infrastructure (`postmaster`, `abuse`, `security`, `mailer-daemon`, …) are rejected.
- **`SenderDisplayName`** — the name shown beside the address, at most 64 characters (measured in
  code points). Defaults to the app's own name.
- **`SenderDomain`** — for an app with more than one verified sending domain: name the one to send
  from. It must be a verified sending domain of the app.

With no `SenderDomain`, the platform picks the app's sending domain deterministically: the domain
designated as the app's **email sender** — chosen when the domain is set up — wins; without a
designation, a domain of your own beats the platform-provided one, then the earliest verified one.

### When the sender identity cannot be honoured

A request that names any sender identity needs a verified sending domain behind it. When the app
has none — or the requested `SenderDomain` is not a verified sending domain of the app — the send
fails with `EmailSenderNotAvailableException`. Nothing is sent in that case, so decide what matters
more, the identity or the delivery:

<!-- ikon-example: email-sender-fallback -->
```csharp
try
{
    await app.Email.SendAsync(request);
}
catch (EmailSenderNotAvailableException)
{
    // Deliver anyway, from the platform's own address.
    await app.Email.SendAsync(request with { SenderLocalPart = null, SenderDisplayName = null, SenderDomain = null });
}
```

A request with **no** sender identity fields never hits this: it sends from the app's verified
domain when one exists and from the platform's default address otherwise.

## Sending from your own domain

Out of the box an app with no verified sending domain sends from the platform's default address, so
the examples above work with no DNS setup at all. Sending from a domain of your own — so the From address reads `reports@yourfirm.com` —
is arranged with IKON rather than configured in the app: ask your IKON contact to add the domain to
your app, and you will be given the records to publish.

They come in two rounds:

| Round | Record | What it does |
| --- | --- | --- |
| Ownership | one `TXT` at `_ikon-verify.<your-domain>` | proves the domain is yours; nothing is provisioned before it resolves |
| Sending | `DKIM`, `SPF` and a return-path record | let receiving servers accept mail the platform sends as you |

Publish the ownership record first — the sending records are only issued once it has been confirmed.
The domain becomes usable when every sending record resolves publicly and passes verification. Both
rounds wait on your DNS provider to propagate, so expect the setup to span hours rather than minutes,
and keep the records in place afterwards: they are re-checked periodically, and a domain whose
records disappear stops being a valid sender.

If the sending records you are given include no `DMARC` policy, a `_dmarc.<your-domain>` `TXT` record
`v=DMARC1; p=none` is added to them for you to publish, so mail is not treated as unauthenticated by the mailbox providers that now require a record. Tightening it to
`quarantine` or `reject` is yours to do once your reports are clean — publish a stricter record of
your own and it takes precedence.

A verified domain is what the `SenderDomain`, `SenderLocalPart` and `SenderDisplayName` fields above
resolve against. A domain is verified for one environment at a time, so development keeps using the
platform default sender.

## Read the inbox

The app receives mail sent to any address on one of its hostnames that has the **Receive email**
capability turned on. A new app has it off on every hostname, so no mail arrives until you turn it
on with `ikon inbox enable` (the app's own hostname) or `ikon inbox enable <hostname>`, and check it
with `ikon inbox status`. To receive at a domain of your own, add it first with
`ikon hostname create <hostname> --email-receive`. `ikon app show <app>` lists the receiving domains under Inbound email,
and `app.Email.GetInboundDomainsAsync()` returns them to the app once their DNS is verified, its own
hostname first.

Inbound email delivered to the app is available as pages or as a lazy stream:

<!-- ikon-example: email-inbox -->
```csharp
// One page at a time
var page = await app.Email.GetInboxPageAsync(new InboxQuery { Limit = 50 });

// Or enumerate across pages; breaking out stops fetching
await foreach (var summary in app.Email.EnumerateInboxAsync(new InboxQuery()))
{
    var detail = await app.Email.GetMessageAsync(summary.Id);

    foreach (var attachment in detail.Attachments)
    {
        await using var download = await app.Email.DownloadAttachmentAsync(detail.Id, attachment.Id);
        using var buffer = new MemoryStream();
        await download.Content.CopyToAsync(buffer);

        // The filename is the sender's, so the stored path uses the attachment id
        await app.Files.Data.WriteBytesAsync($"inbox/{detail.Id}/{attachment.Id}", buffer.ToArray(), download.MimeType);
    }

    await app.Email.DeleteAsync(summary.Id);
}
```

`InboxQuery` filters by recipient, sender, and time window. Deleting a message frees its attachment
storage, so save the attachments you want to keep first, as above. `attachment.Filename` is the
name the sender gave the file; store it in your own record if you need it. Deleting an unknown id throws rather than succeeding silently.

## React when mail arrives

Instead of sweeping the inbox on a schedule, listen for the platform's `EmailReceived` event: the
backend delivers every stored message to the app's userless instance — starting one when none is
running — and keeps the event until the handler returns. That is the instance whose session identity
has no values. In an app whose `SessionIdentity` record has no fields, it is the instance every
visitor is in (the first shard of a `[Sharded]` app), so what the handler changes shows on their
screens at once. In an app keyed by user or by room, only a visitor with no user or no `?room=` is
in it: keep what the handler records in a database and read it from there. The wake-up itself is best-effort: if the
backend fails to create the event, the message is still stored and readable through `app.Email` but
the trigger never fires for it, so an app that must not miss mail still reads its inbox now and then.

The scaffold's global usings import `Ikon.App.Triggers` (`TriggerContext`) and alias `TriggerEventType`. A plain
`using Ikon.Common.Core.Protocol;` makes a bare `Action` ambiguous with `System.Action` (CS0104).

<!-- ikon-example: email-trigger -->
```csharp
[Trigger(TriggerEventType.EmailReceived, MaxParallelism = 4)]
internal async Task OnEmailReceivedAsync(TriggerContext context, CancellationToken ct)
{
    // The payload is the envelope only; the subject and body stay behind app.Email
    var envelope = context.GetPayload<EmailReceivedPayload>();
    var message = await app.Email.GetMessageAsync(envelope.Id, ct);

    // Returning acknowledges the event; throwing leaves it pending for redelivery with backoff
    await RememberLastEventAsync(context, message.Subject);
}
```

The payload is an `EmailReceivedPayload` — the envelope only (`Id`, `Recipient`, `From`, `ReceivedAt`,
`AttachmentCount`, `SpamScore`, `Tag`); read the subject, bodies and attachments with the message id.
Delivery is at-least-once: a handler that throws sees the same event again after a backoff, so keep
it idempotent, and `MaxParallelism` above 1 lets independent messages be handled concurrently.
The trigger and an inbox sweep, such as one run at startup, can also reach the same new message at
the same time, so a check that the message is not stored yet is not enough: both can pass it at once,
and the second insert fails on the primary key. Claim the message in one statement keyed by its id,
`INSERT INTO mail (id, …) VALUES (@id, …) ON CONFLICT (id) DO NOTHING`, and do the rest of the work
only when that statement inserted a row (`ExecuteAsync` returns 1); the path that gets 0 stops there.
When that work can fail, keep a `processed` flag on the row and have the sweep and the redelivered
event finish a row that is stored but not processed.
The trigger fires only in a deployed app; a local `ikon run` can read the inbox but gets no events.
A deployed app can send mail to its own inbox, so the whole path can be tested without an outside
mail account: once `ikon inbox enable` has run, send to any address at a receiving domain, such as
`$"test@{(await app.Email.GetInboundDomainsAsync())[0]}"`, with `app.Email.SendAsync`. A handler
that skips mail from the app's own address, as one that answers mail should, skips this test too.
Start the test message's subject with a marker of your own, such as `[self-test]`, and let mail from
the app's own address through only when its subject starts with that marker. An automatic reply's
subject starts with `Re:`, so the handler still skips it.

The ikon tool sends the same test message without code: `ikon inbox send --subject "[self-test] Lost umbrella" --text "…" --attach photo.jpg`
sends it to `test@` at the app's own hostname (`--to` names another address at a receiving domain, and no
other), as the app's own sent mail. `ikon inbox list` shows it once it arrives, and
`ikon inbox delete <id>`, or `--all`, removes test mail from the inbox.
