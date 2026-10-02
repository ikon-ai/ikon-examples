# Session issues
<!-- checked-against: 7a6ff24fe3b0c442 -->
Session issue analysis turns your app's warning and error logs into a short list of named problems
you can act on, without reading a log. It is off by default; an admin of the app switches it on with
`ikon autopilot set --analysis enable`. Analysis runs use AI models billed to your app, and
`ikon autopilot set --analysis disable` stops both the analysis and the charges. `ikon autopilot status` shows
the current settings.

## What an issue is

Every warning, error and critical line your app logs is grouped deterministically: values that vary
between occurrences — ids, timestamps, addresses, quoted literals, numbers — are normalized away,
stack traces group by their throw site rather than their message, and near-duplicates that differ
in a single varying word (`tenant acme` / `tenant globex`) merge into one issue that lists the
values it affects. One distinct problem becomes one issue, however many times it occurs.

Each **new** issue is classified once by an AI model into a title, a category (`app-defect`,
`platform-defect`, `configuration`, `integration`, `capacity`, or `noise`), a severity, who is
affected, a likely cause, and a suggested action. Known issues are counted without any model call,
except one still unnamed, which a later run with classification budget to spare offers to the model again.
An issue the model has not named yet shows its normalized template instead — unnamed, never
invisible.

Samples are redacted before they are stored: emails, tokens, credentials and long digit runs never
leave the analysis.

## Issue states

```text
open ──► acknowledged ──► resolved        plus muted, plus likely-fixed
```

- **open** — the default list. The bare view answers "what is wrong right now".
- **acknowledged** — someone owns it.
- **resolved** — somebody fixed something. A resolved issue reopens automatically on any new
  occurrence, and the reopen count is itself a signal that a fix did not hold. After 30 days resolved
  an issue is deleted, and a new occurrence opens a fresh issue instead.
- **muted** — counted but never surfaced again. Your own cost control.
- **likely-fixed** — an observation the platform made, never a state you set: the issue has been
  quiet while enough sessions ran to make the silence mean something, ideally on a release it has
  never been seen in. The evidence is always shown with its numbers — *"Not seen for 9 days.
  4,180 sessions since, none of them affected"* — so you can apply what you know and either
  confirm it resolved or reopen it. Left alone long enough, it resolves itself.

An issue quiet in an app that has run no sessions stays open: silence without opportunity is not
evidence.

## Where issues appear

- **CLI** — `ikon issue list` lists what is wrong right now; `--state likely-fixed` is the review
  queue, `--deployment-version` answers "did my deploy break this". `ikon issue show <id>`
  has the full detail and sample; `ikon issue set <id> --state …` changes the state;
  `ikon issue merge <id> --into <id>` folds an issue into another that is the same problem.
  `ikon autopilot history --job analysis` lists the analysis runs behind the issues — an empty issue list means
  either nothing is wrong or nothing has run yet, and only the run history tells them apart.
- **Platform events** — a `session_issue_opened` event accompanies every new issue, so you can
  drive your own alerting from `ikon events` or the events API.
- **Email digest** — off by default. `ikon autopilot set --digest enable` sends one email per day per
  app to your organisation's admins, only when error or critical issues have opened or issues have
  escalated in it (never for noise or muted ones), with counts
  and titles only; `ikon autopilot set --digest disable` stops it.

## Cadence and cost

Your app is analysed on a fixed cadence — daily by default; `ikon autopilot set --analysis-cadence 6h` or
`ikon autopilot set --analysis-cadence hourly` analyses it more often. Model usage appears in your app's
own costs (`ikon costs`) like any other AI call, bounded by one classification per distinct new
problem and a daily cap. An organisation out of credits is still analysed and counted, but its new
issues open unnamed until credit returns.
