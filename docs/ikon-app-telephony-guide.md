# Ikon.App.Telephony Guide

Send SMS and place phone calls from your app — through a phone number the platform holds for your
app's space, with no telephony provider account, API key, or contract of your own. `app.Telephony`
is the entry point; the space's organisation must have the **Telephony** feature enabled (calls
without it throw `FeatureNotEnabledException`).

## Give your app a phone number first

Nothing works until the space has a number, because a number is what makes a message replyable and
what lets calls happen at all. Allocating one is a single command:

```
ikon app telephony enable --country se
```

It picks a number in that market, wires it to your app, and prints it. A number **rents monthly**
for as long as the app holds it, charged to the app's credits, so the command asks for confirmation
before allocating (`--yes` skips the prompt in scripts).

```
ikon app telephony status
```

shows whether the app has telephony and which numbers it holds. `--format json` gives the same
answer to a script.

## Send an SMS

```csharp
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
| `Cost` / `Currency` | What the provider charged, when it can say at send time |
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
ikon app telephony enable --country fi
```

The platform picks the number local to each recipient automatically when the space holds more than
one.

## Place a call

```csharp
var call = await app.Telephony.CallAsync(
    "+358401234567",
    """{"play":"https://example.com/greeting.mp3"}""");

// … later
await app.Telephony.HangUpAsync(call.CallId);
```

The second argument is the provider's own call instruction — what should happen when the call is
answered, such as playing audio or running a menu that captures keypresses.

Two things to design around:

- **Every call has a duration limit** and is hung up when it is reached, so a call nobody ends
  cannot run up an unbounded charge. You can ask for a *shorter* limit with `maxDurationSeconds`;
  asking for a longer one does not extend it.
- **Calls are billed per started minute** by some providers, so a four-second call and a
  fifty-second call both cost a full minute.

`HangUpAsync` on a call that has already finished succeeds and does nothing, so you never have to
race the caller hanging up.

## Receiving

Incoming messages are delivered to your app's own HTTP endpoint at **`/sms`**, and incoming calls to
**`/voice`**. Because a message can arrive while the app is idle, the platform cold-starts it to
deliver one.

The reply is the response body: return a string from the handler and it is sent back to the sender
as an SMS, with no second API call.

Providers post **form-encoded** bodies, and the fields differ between them, so the handler takes the
request and reads the fields it wants. Take a single `HttpRequest` parameter — an endpoint that
declares two body parameters does not bundle.

```csharp
[HttpPost("/sms")]
public string OnSms(HttpRequest request)
{
    var form = System.Web.HttpUtility.ParseQueryString(request.Body);

    return $"Thanks — we got: {form["message"]}";
}
```

You do not need to authenticate the endpoint yourself. The platform registers a **signed URL** with
the provider, so an unsigned request to the same path is rejected before it reaches your app, and
the endpoint keeps its default auth. This matters because there is **no callback signature** to
verify with these providers — allowing their published source addresses would otherwise be the only
option, and those addresses change by email notice.

### Choosing which instance receives inbound

An incoming message carries no proof of who sent it beyond a phone number, and only your app knows
which of its users owns that number — so the platform cannot pick the destination for you. By
default inbound goes to the app's **shared instance**, the one an app gets when nothing identifies
the caller.

```
ikon app telephony bind --sms-endpoint OnInboundSms
ikon app telephony bind --identity '{"UserId":"alice"}'
```

The first form also records which endpoint receives messages, so later rebinds only have to say
where to send them. If the chosen instance is not running, the message starts one.

Pass `--sms-path` / `--voice-path` to `ikon app telephony enable` if you want different paths. They
are set on the number when it is allocated, so changing them later means re-running `enable`.

**Inbound is free** on both messages and calls; only what you send is charged.

## What it costs

Sending is metered at the provider's real cost and charged to the app's credits like any other
platform usage, so an expensive destination costs what it costs rather than an average. There is no
markup table to look up and no rate card to keep in sync.

The number itself is charged per second it is held, so allocating one mid-month costs the part of
the month you had it, and releasing it stops the charge immediately.

A space that runs out of credits is suspended, which stops telephony along with everything else.

## Giving a number up

```
ikon app telephony disable
```

releases every number the app holds and stops the billing. It confirms first, and for a
reason worth taking seriously: **the same number is never given back**. Anyone who saved it,
printed it, or published it loses the ability to reach your app, permanently.

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

`GetStatusAsync` is the supported way to find out whether the space has telephony — better than
discovering it from a failed send, and better than assuming.
