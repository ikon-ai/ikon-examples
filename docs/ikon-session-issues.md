# Session issues

Session issue analysis turns your app's warning and error logs into a short list of named problems
you can act on, without reading a log. It is off by default; a space admin switches it on with
`ikon app issues enable`. Analysis runs use AI models billed to your space, and
`ikon app issues disable` stops both the analysis and the charges. `ikon app issues status` shows
the current settings.

## What an issue is

Every warning, error and critical line your app logs is grouped deterministically: values that vary
between occurrences — ids, timestamps, addresses, quoted literals, numbers — are normalized away,
stack traces group by their throw site rather than their message, and near-duplicates that differ
in a single varying word (`tenant acme` / `tenant globex`) merge into one issue that lists the
values it affects. One distinct problem becomes one issue, however many times it occurs.

Each **new** issue is classified once by an AI model into a title, a category (`app-defect`,
`platform-defect`, `configuration`, `integration`, `capacity`, or `noise`), a severity, who is
affected, a likely cause, and a suggested action. Known issues are counted without any model call.
An issue the model has not named yet shows its normalized template instead — unnamed, never
invisible.

Samples are redacted before they are stored: emails, tokens, credentials and long digit runs never
leave the analysis.

## Issue states

```
open ──► acknowledged ──► resolved        plus muted, plus likely-fixed
```

- **open** — the default list. The bare view answers "what is wrong right now".
- **acknowledged** — someone owns it.
- **resolved** — somebody fixed something. A resolved issue reopens automatically on any new
  occurrence, and the reopen count is itself a signal that a fix did not hold.
- **muted** — counted but never surfaced again. Your own cost control.
- **likely-fixed** — an observation the platform made, never a state you set: the issue has been
  quiet while enough sessions ran to make the silence mean something, ideally on a release it has
  never been seen in. The evidence is always shown with its numbers — *"Not seen for 9 days.
  4,180 sessions since, none of them affected"* — so you can apply what you know and either
  confirm it resolved or reopen it. Left alone long enough, it resolves itself.

An issue quiet in a space that has run no sessions stays open: silence without opportunity is not
evidence.

## Where issues appear

- **CLI** — `ikon app issues` lists what is wrong right now; `--state likely-fixed` is the review
  queue, `--deployment-version` answers "did my deploy break this". `ikon app issues show <id>`
  has the full detail and sample; `ikon app issues set <id> --state …` changes the state.
  `ikon app issues runs` lists the analysis runs behind the issues — an empty issue list means
  either nothing is wrong or nothing has run yet, and only the run history tells them apart.
- **Platform events** — a `session_issue_opened` event accompanies every new issue, so you can
  drive your own alerting from `ikon app events` or the events API.
- **Email digest** — off by default. `ikon app issues digest enable` sends one email per day to
  your organisation's admins, only when there is something new, with counts and titles only;
  `ikon app issues digest disable` stops it.

## Cadence and cost

Your space is analysed on a fixed cadence — daily by default; `ikon app issues cadence set 6h` or
`ikon app issues cadence set hourly` analyses it more often. Model usage appears in your space's
own costs (`ikon app costs`) like any other AI call, bounded by one classification per distinct new
problem and a daily cap. An organisation out of credits stops being analysed by the same mechanism
that stops everything else.
