# Ikon.App.Telephony Guide

Send SMS and place phone calls from your app — through a phone number the platform holds for your
app's space, with no telephony provider account, API key, or contract of your own. `app.Telephony`
is the entry point; the space's organisation must have the **Telephony** feature enabled (calls
without it throw `FeatureNotEnabledException`).

## Give your app a phone number first

Nothing works until the space has a number, because a number is what makes a message replyable and
what lets calls happen at all. Allocating one is a single command:

```
ikon app telephony create --country se
```

It picks a number in that market and wires it to your app. A number **rents monthly** for as long as
the app holds it, charged to the app's credits, so the command asks for confirmation before
allocating (`--yes` skips the prompt, and is required when nothing can answer a prompt — in CI, or
when an agent runs it). What it came to shows up in `ikon app costs` like any other usage.

You choose the market and nothing else about the number. Neither provider will sell a *named*
number, so there is no area code to ask for and no list to pick from.

### Markets that ask who you are

Much of Europe will not sell a number until it knows which organisation holds it. `create` asks the
provider what the market wants and collects it — **your own details, never the platform's**, because
this is what a regulator reads to learn who is behind the number.

Interactively it simply prompts. For a script or an agent, pass the fields as flags:

```
ikon app telephony create --country fi --yes \
  --kyc customer_name="Acme Oy" \
  --kyc street="Kauppakatu 1" \
  --kyc city=Helsinki \
  --kyc postal_code=00100
```

Leave them out in a non-interactive run and the command names every missing field at once, rather
than failing on them one at a time. `region` is optional — most European countries have none.

A market that will only accept an uploaded document is refused here and pointed at the Portal,
where a file can be attached.

Run it again for a second number: an app may hold several, in different markets and on different
providers.

```
ikon app telephony status
```

lists every number the app holds — market, provider, which is the default sender, and where each
one's incoming traffic goes. `--format json` or `--format csv` gives the same answer to a script.

```
ikon app telephony delete --number +46766861234
```

gives one number up.

## Send an SMS

```csharp
// app.Telephony is a TelephonyService — no construction, no provider account of your own.
var result = await app.Telephony.SendSmsAsync("+358401234567", "Your table is ready.");

if (!result.Replyable)
{
    // The recipient got the message but cannot answer it — see "Markets" below.
}
```

The recipient number is always **bare E.164**: a `+`, the country code, then the number, with no
spaces, dashes or leading zeroes. `+358401234567`, never `040 123 4567`.

`SmsSendResult` reports what happened:

| Field | What it tells you |
| --- | --- |
| `MessageId` | The provider's id, for correlating delivery reports |
| `From` | The number the message was sent from |
| `Parts` | Billable segments — a long message is split, and non-GSM characters roughly halve what fits in one |
| `Replyable` | Whether the recipient can answer — see below |

## Markets, and why `Replyable` matters

A phone number is local to one market. When you message a handset in a **different** country from
the sending number, carriers commonly strip the sender in transit and the recipient sees "Unknown"
— the message arrives, but there is nothing to reply to and no thread to continue in.

This is invisible from the API: the send succeeds, the provider reports no error, and only a real
handset shows the problem. `Replyable` is how you find out. Treat it as the signal that a
conversation is not possible, and either say so in the message ("do not reply — call us on …") or
allocate a number in that market:

```
ikon app telephony create --country fi
```

The platform picks the number local to each recipient automatically when the app holds more than
one, so a second number is usually all it takes.

## Choosing which number to send from

By default you do not choose: the platform picks the app's default number if one is set, otherwise
one local to the recipient's market, otherwise the first the app holds. That is the right behaviour
for reaching people, and `SmsSendResult.From` always says which number was used.

Name one when it matters — replying as the same number a user last saw, for instance:

```csharp
var numbers = await app.Telephony.GetNumbersAsync();

await app.Telephony.SendSmsAsync("+358401234567", "Your table is ready.", from: numbers[0].Number);
```

Naming a number the app does not hold is refused rather than quietly substituted, because sending as
a different number reaches the recipient as a stranger.

To pin one number as the app's usual sender:

```
ikon app telephony default set +46766861234
```

## Place a call

A call is a live audio stream in both directions from the moment it connects — you listen to the
caller and speak back, rather than handing the provider a script to play.

```csharp
await using var call = await app.Telephony.CallAsync("+358401234567");

await foreach (var audio in call.ListenAsync())
{
    // … recognise speech, decide what to say …
}

await call.HangUpAsync();
```

`ListenAsync` yields the caller's audio as it is spoken, and `SpeakAsync` takes an
`IAsyncEnumerable<AudioChunk>` — the same shape `ISpeechGenerator.GenerateSpeechAsync` produces, so a
speech model plugs straight in with no adapter. `InterruptAsync` abandons audio already sent, which
is what makes barge-in work, and `WaitForPlaybackAsync` waits for what you sent to actually be heard.

Pass `from:` to choose which of the app's numbers to call from; omit it and the platform picks, the
same way sending does.

Two things to design around:

- **Every call has a duration limit** and is hung up when it is reached, so a call nobody ends
  cannot run up an unbounded charge.
- **Calls are billed per started minute** by some providers, so a four-second call and a
  fifty-second call both cost a full minute.

`HangUpAsync` on a call that has already finished succeeds and does nothing, so you never have to
race the caller hanging up.

## Receiving

You declare no webhook. The platform owns the endpoints an incoming message and an incoming call
arrive at, so what you write is a handler:

```csharp
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
```

Because a message can arrive while the app is idle, the platform cold-starts an instance to deliver
one. You do not authenticate anything yourself: the platform registers a **signed URL** with the
provider, so an unsigned request to the same path is rejected before it reaches your app.

Replying is an ordinary `SendSmsAsync`, which is priced, metered and refusable like any other send —
rather than a string returned from a handler, which one provider would have billed inside its own
network where nothing could account for it.

### Choosing which instance receives inbound

An incoming message carries no proof of who sent it beyond a phone number, and only your app knows
which of its users owns that number — so the platform cannot pick the destination for you. By
default inbound goes to the app's **shared instance**, the one an app gets when nothing identifies
the caller.

```
ikon app telephony bind --identity '{"UserId":"alice"}'
```

points **every** number the app holds at that identity. If the chosen instance is not running, the
message starts one.

**Each number can go somewhere different.** Name one and the rest stay where they are:

```
ikon app telephony bind --number +358401234567 --identity '{"UserId":"alice"}'
ikon app telephony bind --number +46766861234  --identity '{"UserId":"bob"}'
```

That is how one app answers as several users: a number per user, each routed to that user's own
instance. `ikon app telephony status` shows where each number currently points, and
`GetNumbersAsync` reports the same to the app.

A running app can also claim inbound for itself with
`app.Telephony.BindInboundToThisInstanceAsync()`, which is what a developer uses to receive on their
own machine; that binding is reverted when the process stops.

**Inbound is free** on both messages and calls; only what you send is charged.

## What it costs

Everything is charged in **platform credits**, and credits are the only figure the API and the CLI
report — read what a space has spent with `ikon app costs`. Metering follows the real cost of each
message and call rather than an average, so an expensive destination costs more than a cheap one
instead of being smoothed into a single rate.

A number is charged **by rental period, in full**: at allocation, and again at each renewal. Giving
one up part-way through a period refunds nothing, so allocating and releasing repeatedly costs a
full period each time.

A space that runs out of credits is suspended, which stops telephony along with everything else.

## Giving a number up

```
ikon app telephony delete --number +46766861234
```

gives up one number. To give up telephony entirely:

```
ikon app telephony disable
```

releases every number the app holds and stops the billing. Both confirm first, and for a reason
worth taking seriously: **the number goes back to the carrier and is never given back**. Anyone who
saved it, printed it, or published it loses the ability to reach your app, permanently.

Deleting the app's space does the same thing automatically, at the end of the platform's
normal removal grace period.

## Checking availability before you offer it

```csharp
var status = await app.Telephony.GetStatusAsync();

if (!status.Enabled)
{
    // Hide the "text me" option rather than letting the send fail.
}
```

`GetStatusAsync` is the supported way to find out whether the app has any number — better than
discovering it from a failed send, and better than assuming. When it has none, sending or calling
throws `TelephonyNumberNotAvailableException`, whose message names the command that fixes it.
