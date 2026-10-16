<!-- checked-against: 885ea1bb59a1df1ae3b0c442 -->

# Authenticating the IKON tool in CI

How a build server, deployment pipeline, or any other automated caller authenticates the `ikon` tool. Read this if you deploy an IKON app from CI rather than from your own terminal.

## The short version

```bash
# Once, from your own terminal:
ikon auth token create my-pipeline

# In CI, as a secret:
IKON_SERVICE_TOKEN=ikon_svc_...
IKON_BACKEND_ENV=<dev or prod>

# Then, in an app project whose committed ikon-config is linked to the app, the usual commands work with no browser:
ikon deploy
```

## Why there is a separate credential for this

`ikon login` signs a person in through a browser. That is the right flow for a human and the wrong one for a pipeline, so automation gets a credential of its own: a **service token**.

The two credentials signing in leaves behind are both unsuitable for CI:

- The **access token** the tool sends with each request lives one hour. If you put it in a secret store, your pipeline stops working within the hour.
- The **refresh token** lives 90 days but **rotates on every use** — using it returns a replacement and invalidates the one you presented. A CI job cannot write the replacement back into your secret store, so the second run would present a token that is no longer valid. The platform treats a rotated token presented again as a stolen one and revokes the whole chain, which would sign your own laptop out too.

A service token does not rotate. You present the same value on every run, and the tool exchanges it for a short-lived access token in memory.

## Creating one

From a terminal where you are already signed in:

```bash
ikon auth token create my-pipeline
```

It prints the token once:

```text
Created service token 'my-pipeline' (id 68f2a1c9e4b17d3a5c9012ab), valid until 2026-09-09 14:32

ikon_svc_kZ8vQ2mR7tX...

This is the only time the token is shown. Store it in your CI secret store as IKON_SERVICE_TOKEN — the tool reads it from there and exchanges it for a short-lived token on each run.
It carries your own access to the platform, so treat it as a password. Revoke it with 'ikon auth token revoke'.
```

Only a hash of it is stored on our side, so it cannot be shown again — if you lose it, revoke it and create another. To write it to a file instead of the terminal, pass `--format json --output <file>`; the file holds it as `token`. Keep that file out of the repository.

Options:

| Flag | Meaning |
|---|---|
| `--expires-days <n>` | Lifetime in days, 1 to 90. Defaults to 30. A platform administrator can issue up to 365. |
| `--format json` | Machine-readable output, for scripting the setup. |
| `--output <file>` | With `--format json`, writes the output to the file rather than the terminal. |

Pick the shortest lifetime you are willing to renew. Nothing rotates this credential, so its lifetime is exactly how long a leaked copy stays useful.

If a dedicated account runs your pipelines, a platform administrator can create the token for that account on its behalf; the token then carries that account's access, not the administrator's. Ask them rather than sharing a person's token.

## Using it

Set two environment variables in your CI configuration, and a third only for commands that act on an app from outside a linked project:

| Variable | Value |
|---|---|
| `IKON_SERVICE_TOKEN` | The token you just created. **Store it as a secret**, never in a committed file. |
| `IKON_SPACE_ID` | Optional. The id of the app to act on, for commands run outside a linked app project; `ikon deploy` and `ikon bundle` never need it. The variable keeps the platform's own word for a cloud app, a space. |
| `IKON_BACKEND_ENV` | `dev` or `prod` — which platform the pipeline runs against. |

`ikon deploy` and `ikon bundle` do not read `IKON_SPACE_ID` or take `--app-id`: they act only on the app the project's `ikon-config` file for the environment is linked to, so commit that linked file and the variable is not needed for them. For commands that resolve an app outside a project, `IKON_SPACE_ID` stands in for the organisation and app defaults that `ikon default set` sets, which live in the sign-in file on your own machine that a CI runner does not have; you can pass `--app-id` on each such command instead. A command run inside an app project that is linked to a cloud app acts on that app whatever `IKON_SPACE_ID` says; the variable names the app for commands run outside one. Inside a project that is not linked on the environment the command runs against, a command that acts on an app refuses rather than use the variable: link the project with `ikon link`, or pass `--app-id`. `ikon release` and `ikon pipeline --dll-path` are the exceptions: in an unlinked project they fall back to `IKON_SPACE_ID`, then the saved default, and act on that app.

`IKON_BACKEND_ENV` tells the tool which platform to authenticate against. The token itself does not say: the environment is chosen **before** the token is exchanged, and it decides which service the exchange goes to. Without the variable or a flag, the tool first looks at your app project's `ikon-config` files, which answers the question only when exactly one environment's config is present; only then does it use the default your sign-in or `ikon default set` saved on your own machine. A CI runner has neither, so there the tool uses production. You can pass `--dev` / `--prod` on each command instead.

Every command then works as usual — `ikon deploy`, `ikon bundle`, and so on — with no browser and no prompts.

The token is only valid for the environment it was created against, so `IKON_BACKEND_ENV` must name that same environment.

Do not also set `IKON_BACKEND_URL` or `IKON_BACKEND_TOKEN`. That pair takes an access token from a sign-in, and the tool refuses to run when `IKON_BACKEND_TOKEN` holds a service token or is set without `IKON_BACKEND_URL`.

### Installing the tool on a runner

The install script installs .NET, Node.js, Git and the tool. On a runner (`CI=true`) it asks nothing and does not sign in. With `--format json` it prints one JSON event per line, and the last one, `result`, lists in `paths` the folders it added to `PATH`, which later steps need:

```bash
curl -fsSL https://ikonai.com/install.sh | bash -s -- --format json > ikon-install.jsonl
```

### GitHub Actions

```yaml
- uses: actions/checkout@v5
- name: Install the IKON tool
  run: |
    set -euo pipefail
    curl -fsSL https://ikonai.com/install.sh | bash -s -- --format json > "$RUNNER_TEMP/ikon-install.jsonl"
    jq -r 'select(.event == "result") | .paths[]' "$RUNNER_TEMP/ikon-install.jsonl" >> "$GITHUB_PATH"
- name: Deploy
  env:
    IKON_SERVICE_TOKEN: ${{ secrets.IKON_SERVICE_TOKEN }}
    IKON_BACKEND_ENV: prod
  run: ikon deploy
```

## Managing them

```bash
ikon auth token list                # your service tokens, with ids and expiry
ikon auth token revoke <id>         # kill one immediately
```

`ikon auth token list` also shows when each token was last used, so you can find tokens that nothing uses any more.

To see which machines are signed in to your account with the tool:

```bash
ikon auth session list
ikon auth session revoke <id>
```

`ikon login` and `ikon logout` are also spelled `ikon auth login` and `ikon auth logout`, if you prefer the whole group under one word.

## What a service token can do, and what to watch

A service token acts as **you**. It carries the same access to the platform your own account has, for as long as it is valid. There is currently no way to narrow one to a single app or a single operation, so treat it exactly as you would treat your password:

- Store it in your CI provider's secret store, never in a committed file. It starts with `ikon_svc_` so that secret scanners — including GitHub's push protection — can recognise it if it ever reaches a repository.
- Give each pipeline its own token, named for that pipeline. Then revoking one does not break the others, and the last-used column tells you which is which. One account can hold at most 25 live service tokens; revoke one before creating more.
- Revoke it when the pipeline that used it goes away, and when anyone with access to the secret store leaves.
- It cannot create or manage other service tokens. This is deliberate, so that a leaked token cannot create a replacement for itself that still works after you revoke it.

`ikon logout` on your own machine does **not** revoke your service tokens — signing out of a laptop should not break a running pipeline. Use `ikon auth token revoke` for that.

## Troubleshooting

**`The ... service token in IKON_SERVICE_TOKEN was refused`** — the token has been revoked, has passed its expiry, or its account no longer exists. Create a new one and update the secret. The same warning follows a `429` from the exchange's rate limit (300 a minute per caller), which many runners behind one address can hit while the token is still valid; a new token does not help there.

**`The ... service token in IKON_SERVICE_TOKEN expires on ...`** — advisory, printed once per run inside the token's last 14 days. Nothing is wrong yet: create the replacement and update the secret before that date.

**`Not signed in to the IKON platform ... Set IKON_SERVICE_TOKEN ...`** — the variable is not reaching the tool. In most CI systems a secret has to be named explicitly in the step's `env` block; check it is not only defined at the repository level.

**`NU1101: Unable to find package Ikon.…`** — the build could not authenticate to the private package feed: the tool could not exchange your service token, so it has no feed credential to pass on. Check that `IKON_SERVICE_TOKEN` reaches the step, and that `IKON_BACKEND_ENV`, when set, names the environment the token was created against.

**`IKON_BACKEND_TOKEN holds a service token`** or **`IKON_BACKEND_TOKEN is set without IKON_BACKEND_URL`** — the token is in the wrong variable. Put the service token in `IKON_SERVICE_TOKEN` and remove `IKON_BACKEND_TOKEN`.

**`This command needs a target app ...`** — set `IKON_SPACE_ID`, or pass `--app-id`.

**`A service token cannot manage service tokens`** — `ikon auth token create` was run in a job that authenticates with a service token. Mint tokens from a terminal where a person is signed in.
