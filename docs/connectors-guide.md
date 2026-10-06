<!-- checked-against: d63c2ba56982ef053dd457b6 -->

# Ikon Connectors Developer Guide

This guide covers what every connector shares — `Ikon.Connectors`, the package each connector package depends on — for app developers wiring external services into an Ikon app. Each service is a package of its own, with a guide of its own.

## Overview

Each connector is a **raw** client for one external service: a thin, typed wrapper over the service's API with no agent coupling. Only the browser connector has an agent skill, and it is internal: the one public route to it is `BrowserOperatorPersona.Create()`, which builds a persona around it. To give an agent Slack, GitHub, Drive or Gmail, write your own tools over the raw connectors.

An app references the package of each service it uses, and its docs then carry that package's guide:

| Package | Services | Guide |
|---|---|---|
| `Ikon.Connectors.Slack` | Slack | `slack-connector-guide` |
| `Ikon.Connectors.GitHub` | GitHub | `github-connector-guide` |
| `Ikon.Connectors.Procountor` | Procountor (Finago) | `procountor-connector-guide` |
| `Ikon.Connectors.Google` | Google Workspace: Drive, Gmail, Calendar, Docs, Sheets, Slides, Contacts, Tasks, Meet, Chat, Forms | `google-connector-guide` |
| `Ikon.Connectors.Microsoft` | SharePoint, OneDrive, the Entra directory, Outlook mail | `microsoft-connector-guide` |
| `Ikon.Connectors.Browser` | agentic and scripted web automation | `browser-connector-guide` |

All of them report failures with `ConnectorException` (from `Ikon.Connectors`), except that the browser connector throws it only to refuse a replay (an input missing or unknown, or a flow that fills a saved card or identity detail) and otherwise throws `InvalidOperationException` or Playwright's `PlaywrightException`. It carries `Provider` (`"slack"`, `"github"`, `"procountor"`, `"google"` for every Google Workspace client, `"microsoft"`, `"browser"`) and, when the failure was an HTTP error, `StatusCode`, normalized so that `401`/`403` mean the credential must be reconnected: a Slack auth error (HTTP 200 with `ok:false`) is reported as `401`, a Slack `missing_scope` as `403`, and a GitHub rate-limit `403` as `429`. A Slack API error that says nothing about the credential or the service's health has a null `StatusCode` and its code (`channel_not_found`, `not_in_channel`, `is_archived`) in `ErrorCode`, which also carries Graph's `error.code`, SharePoint's `odata.error.code`, GitHub's validation `code` or GraphQL `type`, and an OAuth token endpoint's `error`. `Reason` is the service's own explanation in a sentence — GitHub's `message`, Graph's `error.message`, an OAuth `error_description`, a Slack error's detail — at most 200 characters, and the connector's own sentence when the service gave none, so it is never null; show it to the person who owns the connected account, not to the public, since it is the vendor's unfiltered English. `IsReconnectRequired` is the `401`/`403` test — the credential is bad or revoked, so surface a "reconnect required" state instead of retrying — and `IsTransient` its counterpart for `408`, `429` and `5xx`, a busy or failing service where the same call may succeed later. A failure that is neither, a null `StatusCode` among them, will fail the same way again. Google's rate-limit `403` is reported as `429` too, a token request that Google or the Microsoft sign-in refuses for the credential (`invalid_grant`, `invalid_client`, `unauthorized_client`, and on Microsoft `consent_required` or `interaction_required`) as `401`, and a Microsoft `401`/`403` on a request that carried no token (a pre-authenticated URL, a followed redirect) with a null `StatusCode`; otherwise Microsoft and Procountor report the raw status:

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
    // Busy or failing service: the call may succeed later, but a write that timed out (504) may already have taken effect.
}
catch (ConnectorException)
{
    // The request itself was refused (a missing channel, an unknown id): retrying will not help.
}
```

Every call honors rate limits: a `429` is retried up to three times, waiting the server's `Retry-After` (seconds or a date, bounded at two minutes), before it surfaces as a `ConnectorException`; a `GitHub` limit that names no `Retry-After` and resets more than two minutes away surfaces at once. The token requests of `Procountor`, `GitHub` and the Google and Microsoft sign-ins are retried too, and the Google and Microsoft clients treat a `503` as they do a `429`, because both services throttle with either (the GitHub client retries a `503` on reads only), and a `504` the same way on a read only, since a gateway timeout may hide a write the service carried out.

One more exception type exists, and it is not a failure: the paged reads each take a `maxPages` bound, and a call that reaches it with the service still holding more throws `ConnectorPageCapException<T>` rather than handing back a shortened list as if it were complete. What it carries differs by service, because services page in different directions: `ResumeFrom` is set where a cursor can continue the read (every GitHub, Microsoft and Procountor listing) and null where it cannot (Slack), and `Items` holds what was read where those items are safe to process. Each service's guide says which. The platform's own backend listings (`IkonBackend` — spaces, databases, billing rows, release notes, everything an `ikon` verb or Studio lists) follow the same rule with `BackendPageCapException<T>`: a `maxResults` window that fills while the backend reports more throws with the `Items` read, the `TotalCount`, and the `NextCursor`, never a shortened list as the total.

A change feed — a `DeltaAsync` such as `OneDrive.DeltaAsync`, `Outlook.MessagesDeltaAsync`, `Drive.DeltaAsync` or `Gmail.MessagesDeltaAsync` — hands back the changes and a cursor to store for the next call. A stored cursor the service no longer honours throws `ConnectorException` with `IsResyncRequired` set (`StatusCode` `410`): drop it and read again without one. `Drive.DeltaAsync` alone does not mark it: a token Google refuses there throws a plain `ConnectorException` with Google's own status. Nothing but a change feed sets `IsResyncRequired`.

The connectors are shaped alike, so one learned is most of the next: a client takes its credentials and an optional `HttpClient` (a shared one otherwise) and needs no disposing; `Get…Async` throws when the thing is missing unless its remarks say it returns null, and a `Find…Async` that looks for one thing returns null when nothing matches; `Delete…Async` removes a thing and `Remove…Async` a relation, such as a permission; a download is a `Stream` to dispose, and a `…BytesAsync` variant takes a `maxBytes` it refuses rather than truncates; and the `CancellationToken` comes last, named `ct`.

`PkceCodes.Create()` makes the verifier and challenge of an OAuth sign-in with PKCE, which the Google and Microsoft sign-ins take: pass the `Challenge` to the authorize URL and keep the `Verifier` with the sign-in's state for the code exchange, so a code intercepted on its way back is useless to whoever took it.
