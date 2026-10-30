<!-- checked-against: acf2bcb64a42dd1ca4e4e53a -->

# Session issues

Session issue analysis turns your app's warning and error logs into a short list of named problems
you can act on, without reading a log. It is off by default; an admin of the app switches it on with
`ikon autopilot set --analysis enable`. Logs from before that are never analysed: the first run
covers the period from the opt-in, one cadence period later. Analysis runs use AI models billed to your app, and
`ikon autopilot set --analysis disable` stops both the analysis and the charges. `ikon autopilot status` shows
the current settings.

## What an issue is

Every warning, error and critical line an analysis run reads is grouped deterministically. One run reads at
most 50,000 lines of its window, newest first; past that the oldest lines go unread and the run
history marks the run "window truncated". Each window ends a few minutes before the run starts, so
lines still on their way into the log store are read by the next run instead of being missed.
Values that vary between occurrences (ids, timestamps, addresses, quoted literals, numbers) are
normalized away. Stack traces are grouped by their throw site rather than their message. A line
logged with an interpolated string (`Log.Instance.Warning($"…")`) is grouped by the logging
statement that wrote it, whatever values it carried, so two statements in one method are two issues.
A logging helper your app's code shares between many problems can say which problem a line reports
by labelling it `logKey` with `Log.Label` (logging guide). Near-duplicates that differ in a single
varying word (`tenant acme` / `tenant globex`) merge into one issue that lists the values it
affects. One distinct problem becomes one issue, however many times it occurs.

Issues whose lines name the same error code, such as a database's `SQLSTATE 08P01` or an HTTP
status in an exception, are shown as **related**: one cause often fails in several places, each
logging its own line.

Each **new** issue is classified once by an AI model into a title, a category (`app-defect`,
`platform-defect`, `configuration`, `integration`, `capacity`, or `noise`), a severity, who is
affected, a likely cause, and a suggested action. The model reads up to three differing lines of
the problem, so the title names what they share. The severity is how urgently you should act, which
can differ from the level the lines were logged at; before an issue is named, it is that level.
Known issues are counted without any model call, with two exceptions.
The first is an issue that is still unnamed: if it recurs in a later run's window and that run
has classification budget to spare, the run sends it to the model again. An issue that never recurs
stays unnamed. The second is an issue that was grouped by its method rather than its logging
statement, when one statement's lines take it over: its name may describe more than it now holds,
so the run sends it to the model again, or, with no budget left, clears the name for a later run
to fill. Until the model names an issue, the issue is listed under its normalized template.

Samples are redacted before they are stored: emails, tokens, credentials and long digit runs never
leave the analysis.

## Issue states

```text
open ──► acknowledged ──► resolved        plus muted, plus likely-fixed
```

- **open** — the default list. Without filters it shows what is wrong right now.
- **acknowledged** — someone owns it.
- **resolved** — somebody fixed something. A resolved issue reopens automatically on any new
  occurrence, and the reopen count is itself a signal that a fix did not hold. After 30 days resolved
  an issue is deleted, and a new occurrence opens a fresh issue instead.
- **muted** — counted but never shown again. Your own cost control.
- **likely-fixed** — a state the platform sets, never one you set. The issue has not occurred while
  enough sessions ran for that to mean something, ideally on a release it has never been seen in.
  The evidence is always shown with its numbers, such as *"Not seen for 9 days. 4,180 sessions
  since, none of them affected"*, so you can use what you know to either confirm it resolved or
  reopen it. If you leave it alone long enough, it resolves itself.

An issue stays open while the app runs no sessions, because the issue has had no chance to occur.

## Where issues appear

- **CLI** — `ikon issue list` lists what is wrong right now; `--state likely-fixed` lists the
  issues waiting for your review, and `--deployment-version` shows whether a deploy broke something. `ikon issue show <id>`
  has the full detail and sample, and lists the related issues; `ikon issue set <id> --state …` changes the state
  (muted and likely-fixed go only to open or resolved, and resolved goes nowhere — only a new
  occurrence reopens it);
  `ikon issue merge <id> --into <id>` merges an issue into another that is the same problem.
  `ikon autopilot history --job analysis` lists the analysis runs behind the issues. An empty issue list means
  either nothing is wrong or nothing has run yet, and only the run history tells them apart.
- **Platform events** — a `session_issue_opened` event accompanies every new issue, so you can
  drive your own alerting from `ikon events` or the events API.
- **Email digest** — off by default. `ikon autopilot set --digest enable` sends one email per day per
  app to your organisation's admins, only when error or critical issues have opened or issues have
  escalated in it (never for noise or muted ones), with counts
  and titles only. Related issues share one line, under the title of the most severe.
  `ikon autopilot set --digest disable` stops it.

## Cadence and cost

Your app is analysed on a fixed cadence — daily by default; `ikon autopilot set --analysis-cadence 6h` or
`ikon autopilot set --analysis-cadence hourly` analyses it more often. Model usage appears in your app's
own costs (`ikon costs`) like any other AI call, bounded by the classifications above and a
daily cap. An organisation out of credits is still analysed and counted, but its new
issues open unnamed, and one is named only when it recurs in a later run that has credit.
