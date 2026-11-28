<!-- checked-against: e0bdd4fba34011693d001258 -->

# How IKON handles AI data

What happens to the prompts, files and answers of an AI call that an app makes through IKON: where
the call goes, what IKON keeps and for how long, what the model providers keep, and how to check
each. For app developers, and for anyone reviewing an app's data handling.

## Where a call goes

An app calls a model through the IKON AI library (`Emerge.Run`, `ImageGenerator`, `SpeechRecognizer`
and the other services). Where the call runs depends on whose provider key it uses:

- **On IKON's keys, the default.** The call goes from your app's server to IKON's AI host, an IKON
  service that holds IKON's provider keys, and the AI host calls the provider. The AI host runs in
  Google Cloud in Finland. The connection from your app's server to the AI host is encrypted with
  TLS, and the AI host calls the provider over HTTPS. Your app never receives IKON's provider keys.
- **On your own key.** When your organisation or app holds its own key for the provider as a secret
  (`IKON_<PROVIDER>_API_KEY`), the call runs in your app's server and goes straight to the
  provider on your account. The provider's terms for your account apply, not IKON's.
- **Under `ikon run --ai mock`**, no provider is called. The library answers with mock data in your
  app's own process.

Each call goes to one provider, chosen before the call is sent:

1. A route on your own key comes first.
2. The organisation's region policy narrows the regions: `ikon org set --ai-region eu-only` keeps
   every AI call of the organisation's apps on routes in the EU.
3. Otherwise the order in which IKON lists the model's routes decides.

A call that fails is not sent to another provider or another route. The error reaches your app,
which decides whether to retry or call another model.

Some providers are aggregators: they pass a call on to another company that runs the model, so that
company's terms apply too. `ikon model provider list` marks them in its Aggregator column.

## What the providers keep

Each route in IKON's model catalog records what its provider keeps of a call, for how long, why, and
whether the provider may train on it. The IKON tool prints it:

```bash
ikon model provider list     # each provider: aggregator, regions, zero data retention, retention
ikon model show <model>      # each route of one model, with its data policy
ikon model list --zdr        # the models with a route whose provider keeps nothing
ikon model list --zdr --eu   # the same, where that route is in the EU
```

A route is marked zero data retention (ZDR) only when the provider's own terms, or a setting on
IKON's account with the provider, show that the provider keeps nothing of a call once it has
answered. The logs some providers keep to check for abuse count as keeping data.

A model with a ZDR route can still be served by a route that keeps data, because the route is
chosen as described above. `ikon model show <model>` lists every route of a model with its region
and data policy. Under the EU-only policy, only the EU routes serve calls.

An aggregator's route that is not marked ZDR can pass the call to any company that runs the model,
and that company's terms decide what it keeps.

## What IKON keeps

IKON does not store the prompts, files or answers of AI calls as part of normal operation. It keeps
the following:

| What | Contains | Kept for |
|---|---|---|
| Usage records | Model, route, provider, region, amounts (tokens, characters, seconds, images) and credits, with the organisation, app and session they belong to | Each use for 25 months. Daily totals per organisation and app for the current year and six full years before it, for billing |
| Call records | Model, duration, amounts, finish reason, and for a failed call its error message | 30 days |
| Server logs | See "What reaches the logs" below | 30 days, in the EU |
| Records of long-running jobs (video, 3D models) | The provider's job id and the settings its price depends on, so the job is billed even if a server restarts | 30 days after the job ends, 90 days if it was abandoned |
| Generated files returned as a link | The generated image, audio, video, PDF or other file, when the app asks for a link (`ResultDelivery.Url`), when the result is over 5 MB, and for some video models always | 24 hours, as a private file reached through a signed link |
| Input files a video provider fetches | The images and videos sent to video generation, enhancement and segmentation, stored at an unguessable address the provider can read | Deleted when the call ends |
| Your own provider keys | App or organisation secrets, encrypted with AES-256-GCM | Until you delete them |

IKON's file storage keeps an earlier version of every changed or deleted file for 30 days, so the
generated files and input files above can be restored for up to 30 days after they are deleted.
That storage is in the EU.

The table above does not cover a provider's own copy of a file. Some providers fetch an input by
link, or return a result as a link on their own storage, and keep that file for the time
`ikon model show` states.

## What reaches the logs

Server logs are kept for 30 days in the EU. The calls that run in your app's server log there, and
you can read those lines with `ikon logs`. The calls that run on IKON's AI host log in the AI host's
log, which only IKON staff can read.

For each call, the AI library logs the model, the route, the time the call took and the amounts.
Prompts, answers, reasoning, tool calls and their results, search queries and scraped pages are
logged only as their length. An address the library fetched is logged only as its host name.

When a call fails, the provider's error message is logged and the call record keeps it for 30 days.
A provider sometimes repeats part of the request in its error message. The rest of the provider's
response is logged only as its error codes and the length of each longer text.

`LLMContext.LogFullRequest` and `LLMContext.LogFullResponse` log a language model call's whole
request or answer, for debugging. IKON's AI host turns both off. They work only for a call that runs
in your app's server, such as a call on your own key, and they write to your app's log.

Before a line is stored, API keys, tokens and password values in it are removed, and email
addresses and phone numbers are replaced with placeholders. Nothing else in a prompt or an answer
is changed.

## Who at IKON can read it

A small number of IKON staff have the platform administrator role, which gives access to every
organisation for support and operations. Reading the value of a secret, including your own provider
keys, is recorded in the audit log, and your organisation's administrators can see those records.
IKON's operators can read the production logs.

## Checking an app

- `ikon model show <model>` for each model the app calls: its routes, regions and data policy.
- `ikon org set --ai-region eu-only` to keep every AI call of the organisation in the EU.
- A secret holding your own provider key, to run calls on your own account and terms.
- `ikon logs` to see what your app's server logged, including the AI library's lines for calls that
  ran there.
