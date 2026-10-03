# Ikon Connectors Developer Guide
<!-- checked-against: a61cf745bf1d9122 -->
This guide covers what every connector shares — `Ikon.Connectors`, the package each connector package depends on — for app developers wiring external services into an Ikon app. Each service is a package of its own, with a guide of its own.

## Overview

Each connector is a **raw** client for one external service: a thin, typed wrapper over the service's API with no agent coupling. Only the browser connector has an agent skill, and it is internal: the one public route to it is `BrowserOperatorPersona.Create()`, which builds a persona around it. To give an agent Slack, GitHub, Drive or Gmail, write your own tools over the raw connectors.

An app references the package of each service it uses, and its docs then carry that package's guide:

| Package | Services | Guide |
|---|---|---|
| `Ikon.Connectors.Slack` | Slack | `slack-connector-guide` |
| `Ikon.Connectors.GitHub` | GitHub | `github-connector-guide` |
| `Ikon.Connectors.Procountor` | Procountor (Finago) | `procountor-connector-guide` |
| `Ikon.Connectors.Google` | Google Drive and Gmail | `google-connector-guide` |
| `Ikon.Connectors.Microsoft` | SharePoint, OneDrive, the Entra directory, Outlook mail | `microsoft-connector-guide` |
| `Ikon.Connectors.Browser` | agentic and scripted web automation | `browser-connector-guide` |

All of them report failures with `ConnectorException` (from `Ikon.Connectors`). It carries `Provider` (`"slack"`, `"github"`, `"procountor"`, `"google"` for Drive and Gmail alike, `"microsoft"`, `"browser"`) and, when the failure was an HTTP error, `StatusCode`, normalized so that `401`/`403` mean the credential must be reconnected: a Slack auth error (HTTP 200 with `ok:false`) is reported as `401`, and a GitHub rate-limit `403` as `429`. Any other Slack API error has a null `StatusCode` and its code (`channel_not_found`, `not_in_channel`, `is_archived`) in `ErrorCode`, which also carries Graph's `error.code` and an OAuth token endpoint's `error`. `IsReconnectRequired` is the `401`/`403` test — the credential is bad or revoked, so surface a "reconnect required" state instead of retrying — and `IsTransient` its counterpart for `408`, `429` and `5xx`, a busy or failing service where the same call may succeed later. A failure that is neither, a null `StatusCode` among them, will fail the same way again. Google's rate-limit `403` is reported as `429` too; Microsoft and Procountor report the raw status:

<!-- ikon-example: connectors-errors -->
```csharp
try
{
    await connectorCall();   // any connector's call
}
catch (ConnectorException ex) when (ex.IsReconnectRequired)
{
    // Permanent: the token is invalid, revoked or lacks access. Ask the user to reconnect.
}
catch (ConnectorException ex) when (ex.IsTransient)
{
    // Busy or failing service: safe to retry later.
}
catch (ConnectorException)
{
    // The request itself was refused (a missing channel, an unknown id): retrying will not help.
}
```

Every call honors rate limits: a `429` is retried up to three times, waiting the server's `Retry-After` (seconds or a date, bounded at two minutes), before it surfaces as a `ConnectorException`. The token requests of `Procountor` and of the Microsoft sign-in are retried too, and the Microsoft clients treat a `503` as they do a `429`, because Graph throttles with either, and a `504` the same way on a read only, since a gateway timeout may hide a write the service carried out.

One more exception type exists, and it is not a failure: the paged reads each take a `maxPages` bound, and a call that reaches it with the service still holding more throws `ConnectorPageCapException<T>` rather than handing back a shortened list as if it were complete. What it carries differs by service, because services page in different directions: `ResumeFrom` is set where a cursor can continue the read (GitHub issues, every Microsoft and Procountor listing) and null where it cannot (Slack), and `Items` holds what was read where those items are safe to process. Each service's guide says which. The platform's own backend listings (`IkonBackend` — spaces, databases, billing rows, release notes, everything an `ikon` verb or Studio lists) follow the same rule with `BackendPageCapException<T>`: a `maxResults` window that fills while the backend reports more throws with the `Items` read, the `TotalCount`, and the `NextCursor`, never a shortened list as the total.

`PkceCodes.Create()` makes the verifier and challenge of an OAuth sign-in with PKCE, which the Google and Microsoft sign-ins take: pass the `Challenge` to the authorize URL and keep the `Verifier` with the sign-in's state for the code exchange, so a code intercepted on its way back is useless to whoever took it.
