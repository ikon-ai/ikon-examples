# Authenticating the Ikon tool in CI

How a build server, deployment pipeline, or any other automated caller authenticates the `ikon` tool. Read this if you deploy an Ikon app from CI rather than from your own terminal.

## The short version

```bash
# Once, from your own terminal:
ikon auth token create my-pipeline

# In CI, as a secret:
IKON_SERVICE_TOKEN=ikon_svc_...
IKON_SPACE_ID=<the space you deploy to>

# Then the usual commands work with no browser:
ikon app deploy
```

## Why there is a separate credential for this

`ikon login` signs a person in through a browser. That is the right flow for a human and the wrong one for a pipeline, so automation gets a credential of its own: a **service token**.

The two credentials a login leaves behind are both unsuitable for CI, and it is worth knowing why so you do not reach for them:

- The **access token** the tool sends with each request lives one hour. Put it in a secret store and your pipeline works until lunchtime.
- The **refresh token** lives 90 days but **rotates on every use** — using it returns a replacement and invalidates the one you presented. A CI job cannot write the replacement back into your secret store, so the second run would present a token that is no longer valid. The platform treats a rotated token presented again as a stolen one and revokes the whole chain, which would sign your own laptop out too.

A service token does not rotate. You present the same value on every run, and the tool exchanges it for a short-lived access token in memory.

## Creating one

From a terminal where you are already signed in:

```bash
ikon auth token create my-pipeline
```

It prints the token once:

```
Created service token 'my-pipeline' (id 68f2a1c9e4b17d3a5c9012ab), valid until 2026-09-09 14:32

ikon_svc_kZ8vQ2mR7tX...

This is the only time the token is shown. Store it in your CI secret store as IKON_SERVICE_TOKEN.
```

Only a hash of it is stored on our side, so it cannot be shown again — if you lose it, revoke it and create another.

Options:

| Flag | Meaning |
|---|---|
| `--expires-days <n>` | Lifetime in days, 1 to 90. Defaults to 30. |
| `--format json` | Machine-readable output, for scripting the setup. |

Pick the shortest lifetime you are willing to renew. Nothing rotates this credential, so its lifetime is exactly how long a leaked copy stays useful.

## Using it

Set two environment variables in your CI configuration:

| Variable | Value |
|---|---|
| `IKON_SERVICE_TOKEN` | The token you just created. **Store it as a secret**, never in a committed file. |
| `IKON_SPACE_ID` | The space to act on. |

`IKON_SPACE_ID` is needed because the organisation and space defaults that `ikon defaults` sets live in the login file on your own machine, and a CI runner has no login file. You can pass `--space-id` on each command instead.

Every command then works as usual — `ikon app deploy`, `ikon app bundle`, and so on. No browser, no prompts.

The token is only valid for the environment it was created against. Create it while signed in to the environment your pipeline deploys to, or pass `--prod` / `--dev` to be explicit.

### GitHub Actions

```yaml
- name: Deploy
  env:
    IKON_SERVICE_TOKEN: ${{ secrets.IKON_SERVICE_TOKEN }}
    IKON_SPACE_ID: ${{ vars.IKON_SPACE_ID }}
  run: ikon app deploy
```

## Managing them

```bash
ikon auth token list                # your service tokens, with ids and expiry
ikon auth token revoke <id>         # kill one immediately
```

`ikon auth token list` also shows when each token was last used, which is how you find one nothing needs any more.

To see which machines are signed in to your account with the tool:

```bash
ikon auth session list
ikon auth session revoke <id>
```

`ikon login` and `ikon logout` are also spelled `ikon auth login` and `ikon auth logout`, if you prefer the whole group under one word.

## What a service token can do, and what to watch

A service token acts as **you**. It carries the same access to the platform your own account has, for as long as it is valid. There is currently no way to narrow one to a single space or a single operation, so treat it exactly as you would treat your password:

- Store it in your CI provider's secret store, never in a committed file. It starts with `ikon_svc_` so that secret scanners — including GitHub's push protection — can recognise it if it ever reaches a repository.
- Give each pipeline its own token, named for that pipeline. Then revoking one does not break the others, and the last-used column tells you which is which.
- Revoke it when the pipeline that used it goes away, and when anyone with access to the secret store leaves.
- It cannot create or manage other service tokens. That is deliberate: a leaked token cannot mint itself a replacement to outlive your revoking it.

`ikon logout` on your own machine does **not** revoke your service tokens — signing out of a laptop should not break a running pipeline. Use `ikon auth token revoke` for that.

## Troubleshooting

**`The service token in IKON_SERVICE_TOKEN was refused`** — the token has been revoked, or has passed its expiry. Create a new one and update the secret.

**`Not logged in to the Ikon platform. Set IKON_SERVICE_TOKEN ...`** — the variable is not reaching the tool. In most CI systems a secret has to be named explicitly in the step's `env` block; check it is not only defined at the repository level.

**`This command needs a target space ...`** — set `IKON_SPACE_ID`, or pass `--space-id`.

**`A service token cannot manage service tokens`** — `ikon auth token create` was run in a job that authenticates with a service token. Mint tokens from a terminal where a person is signed in.
