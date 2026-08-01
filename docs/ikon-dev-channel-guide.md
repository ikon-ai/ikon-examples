# Ikon Dev Channel Guide

Get a platform library fix into your app without waiting for a release.

Ikon's libraries ship as one release train. Between releases, a fix that has already landed and passed
CI is unreachable to an app that builds against published packages. The **dev channel** publishes those
same libraries continuously so you can pick a fix up the day it merges — at the cost of running code
that has not been through a release.

> **The trade you are making.** Dev packages are unreleased, unsupported, carry no release notes, and
> are **deleted a week after publication**. An app pinned to a deleted version cannot restore and cannot
> be rebuilt. This is a channel for unblocking work in progress, not a place to leave an app parked.

## Switching an app to the dev channel

```bash
ikon app update dev        # move to the newest dev builds
ikon app update              # stay on your current channel, take what is newest on it
ikon app update stable     # return to released packages
```

The channel is not stored anywhere — it is read back from what your project is pinned to. A plain
`ikon app update` therefore keeps a dev app on dev and a stable app on stable, and the two can never
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

`ikon app update dev` also adds one line to `frontend-node/.npmrc`:

```
@ikon-ai:registry=https://npm.pkg.github.com
```

That line is safe to commit — it is a registry mapping, not a credential. The credential goes into your
own `~/.npmrc` and is written by `ikon login`.

## Reading a dev version

```
3.2.40-dev.87
│     │   └── commits since the last release
│     └────── the dev channel label
└──────────── the release this build precedes (not the one it follows)
```

A dev build always sorts **above** the release before it and **below** the release it is heading
towards, so `dev` always moves forward and `stable` always walks back. When `3.2.40` is finally
released it outranks every `3.2.40-dev.*`, and a dev app rolls onto the release automatically at the
next update.

## The seven-day window

A new dev build is published on **every push to main**, so a fix is on the feed as soon as it lands —
that immediacy is the whole point of the channel. Retention deletes dev versions older than seven
days, always keeping the newest few whatever their age, so the channel can never be emptied even if
nothing is pushed for a while. There is no retention beyond that and no way to recover a deleted
version.

What this means in practice:

- **Re-run `ikon app update` at least weekly** while you are on the channel, or move back with `ikon app update stable`.
- **A fix landed for you is one `ikon app update` away** — you do not have to wait for a nightly or a
  release, only for the build that publishes it.
- **Already-deployed apps keep running.** A deployed bundle carries its own copies of the Ikon
  libraries and the built frontend, so deleting the package it was built from does not affect it. Only
  *rebuilding* breaks.
- **A restore failing with a 404 on an `Ikon.*` package almost always means an expired dev pin.** Run
  `ikon app update` to move to a current build, or `ikon app update stable` to leave the channel.

## CI for a dev-channel app

Your pipeline needs credentials for both private feeds, because dev packages are never published to
nuget.org or npmjs. `ikon login` configures a developer machine; for CI, set the same GitHub Packages
token in the environment and make sure the `@ikon-ai` scope mapping in `frontend-node/.npmrc` is
committed.

## What the dev channel cannot fix

It carries **libraries** — the .NET packages and the TypeScript SDK, which ship together so their
protocol bindings stay in step. It does **not** carry the backend, Canvas, or the servers, all of which
release on their own schedule. A fix that needs a change on those still waits for that component's
release, and a dev library that depends on such a change will not work against production until then.

If you are unsure whether the fix you need is in a library or the platform, ask before switching an app
over — the channel costs you the seven-day treadmill either way.
