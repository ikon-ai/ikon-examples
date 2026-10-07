<!-- checked-against: 02ebfd6d9d0cbed8e3b0c442 -->

# Ikon Dev Channel Guide

Get a platform library or ikon tool fix without waiting for a release.

Ikon's libraries ship as one release train. Between releases, a fix that has already landed and passed
CI is unreachable to an app that builds against published packages. The **dev channel** publishes those
same libraries continuously so you can pick a fix up the day it merges — at the cost of running code
that has not been through a release.

> **The trade you are making.** Dev packages are unreleased, unsupported, carry no release notes, and
> are **deleted a week after publication**. An app pinned to a deleted version cannot restore and cannot
> be rebuilt. This is a channel for unblocking work in progress, not a place to leave an app parked.

## Switching an app to the dev channel

```bash
ikon update dev        # move to the newest dev builds
ikon update              # stay on your current channel, take what is newest on it
ikon update stable     # return to released packages
```

The channel is not stored anywhere — it is read back from what your project is pinned to. A plain
`ikon update` therefore keeps a dev app on dev and a stable app on stable, and the two can never
disagree about which channel you are really on.

`stable` **lowers** your pinned versions. That is expected: a dev build is a prerelease of the *next*
release, so `3.2.40-dev.87` outranks the `3.2.39` you are going back to.

## What changes in your project

**.NET** — package versions gain a prerelease label:

```xml
<PackageReference Include="Ikon.App" Version="3.2.40-dev.87" />
```

**TypeScript** — the SDK packages become aliases, because the dev builds are published under a
different scope (see below):

```json
"@ikonai/sdk": "npm:@ikon-ai/sdk@1.0.81-dev.87"
```

Your imports do **not** change. npm installs an alias to `node_modules/@ikonai/sdk`, so
`import { … } from '@ikonai/sdk'` keeps resolving exactly as before, and no tsconfig path or vite alias
has to know the dev channel exists.

`ikon update dev` also adds one line to `frontend-node/.npmrc`:

```ini
@ikon-ai:registry=https://npm.pkg.github.com
```

That line is safe to commit — it is a registry mapping, not a credential. The credential goes into your
own `~/.npmrc` and is written by `ikon login`.

## Reading a dev version

```text
3.2.40-dev.87
│     │   └── commits since the last release
│     └────── the dev channel label
└──────────── the release this build precedes (not the one it follows)
```

A dev build always sorts **above** the release before it and **below** the release it is heading
towards, so `dev` always moves forward and `stable` always walks back. When `3.2.40` is finally
released it outranks every `3.2.40-dev.*`, but a dev app's update takes the newest dev build, and
the first push to main after the release publishes a `3.2.41-dev.*`. The .NET packages roll onto
`3.2.40` only when an update runs before that; the app has then left the dev channel, and the next
plain `ikon update` brings its TypeScript SDK back to the release as well.

## The seven-day window

A new dev build is published on **every push to main that touches it** — the .NET libraries when the
push changed .NET sources, the TypeScript SDK when it changed TypeScript sources — so a fix is on the
feed as soon as it lands, which is the whole point of the channel. The version counter counts every
commit, including ones that touch neither half, so dev ordinals skip numbers; the newest version on the feed is
always the newest state of that half, whatever its ordinal.

A nightly retention job then deletes dev versions older than seven days, always keeping the newest few
whatever their age, so the channel can never be emptied even if nothing is pushed for a while. There
is no retention beyond that and no way to recover a deleted version.

What this means in practice:

- **Re-run `ikon update` at least weekly** while you are on the channel, or move back with `ikon update stable`.
- **A fix landed for you is one `ikon update` away** — you do not have to wait for a nightly or a
  release, only for the build that publishes it.
- **Already-deployed apps keep running.** A deployed bundle carries its own copies of the Ikon
  libraries and the built frontend, so deleting the package it was built from does not affect it. Only
  *rebuilding* breaks.
- **An npm install failing with a 404 on an `@ikon-ai/*` package almost always means an expired dev pin.**
  The npm alias names one exact version; the .NET pin is a minimum, so its restore takes the next
  newer build with warning NU1603 instead of failing. Run `ikon update` to move to a current build, or
  `ikon update stable` to leave the channel.

## The ikon tool on the dev channel

The tool has a dev channel of its own, published with the .NET libraries on every push to main that
touches them, so a fix to the tool itself is also one command away:

```bash
ikon self update dev        # move the tool to the newest dev build
ikon self update            # stay on your current channel, take what is newest on it
ikon self update stable     # return to the released tool
```

A dev build is the `ikon-dev` package on the Ikon package feed rather than `ikon` on nuget.org, which
is why moving onto it and updating it need you signed in (`ikon login`); going back to stable does not. It still installs as the `ikon` command, and the two
packages cannot be installed side by side — switching replaces one with the other, and puts the old one
back if the new one fails to install. Your tool packages, sign-in and settings carry over either way.

A dev build **stops working seven days after it was published**, matching the feed's retention. In its
last day it warns on every command; once expired it refuses everything except `ikon self update`, `ikon self reset`,
`ikon login`, `ikon logout` and `ikon --version`. Auto-update keeps an interactive dev install at most a day behind, and
an expired build run in a terminal while you are signed in updates itself, so you normally never see
either. If no newer dev build has been published, `ikon self update` says so and points you at
`ikon self update stable`. `ikon --version` prints the build's expiry on stderr.

If a dev build is ever too broken to update itself, leave the channel by hand:

```bash
dotnet tool uninstall ikon-dev --global
dotnet tool install ikon --global
```

The installer scripts and Ikon Desktop always install the released tool. Re-running an installer
replaces a dev build with it; Ikon Desktop leaves a dev build alone and does not update it.

## CI for a dev-channel app

Your pipeline needs credentials for both private feeds, because dev packages are never published to
nuget.org or npmjs. `ikon login` configures a developer machine; for CI, set the same GitHub Packages
token in `IKON_GITHUB_IKON_PACKAGES_READ_ACCESS_TOKEN` (or set `IKON_SERVICE_TOKEN` so the tool fetches
it) and make sure the `@ikon-ai` scope mapping in `frontend-node/.npmrc` is committed. The ikon tool
passes that token only to the `dotnet` and `npm` processes it starts itself, so restore through
`ikon build` rather than a bare `dotnet restore` or `npm ci`.

## What the dev channel cannot fix

It carries **libraries** — the .NET packages and the TypeScript SDK, which ship together so their
protocol bindings stay in step — and the ikon tool. It does **not** carry the backend, Canvas, or the servers, all of which
release on their own schedule. A fix that needs a change on those still waits for that component's
release, and a dev library that depends on such a change will not work against production until then.

If you are unsure whether the fix you need is in a library or the platform, ask before switching an app
over — the channel costs you the seven-day treadmill either way.
