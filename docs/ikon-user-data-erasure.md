# User Data Erasure

When a user account is deleted — by the user themselves or by a platform administrator — the platform
erases the user's personal data centrally, across every space and organisation the user touched. This
page describes what the platform erases, what stays and why, and what your app is responsible for.

## Self-service deletion and the grace period

A signed-in user can request account deletion from the Portal's Account page
(`POST /users/me/removal`). The deletion is **not immediate**: it is scheduled after a grace period
(14 days by default), a confirmation email is sent (accounts without an email address are scheduled
silently), and the user can cancel at any time before the scheduled date (`DELETE /users/me/removal`
or the Cancel button on the Account page). When the grace period elapses, the account is removed and
the erasure below runs automatically.

Platform administrators can delete an account immediately (`DELETE /users/:id`), which runs the same
erasure right away, or schedule/cancel a deferred removal on behalf of a user
(`POST`/`DELETE /users/:id/removal`).

## What the platform erases

Deletion covers the account's whole identity closure: when an anonymous account was upgraded or
merged into the account, the previous ids are erased together with it.

- **The user account** — the user document with its credentials, plus passkeys, profiles, and
  organisation memberships (the existing removal cascade).
- **Per-user app state** — rows in every space database whose reactive-storage scope belongs to the
  user (`UserScope` values persisted by apps).
- **User asset folders** — `user/{id}` folders (and their storage blobs) in the asset hub of every
  affected space and organisation.
- **Push subscriptions** — web push and FCM registrations for the user's devices.
- **Pending invitations** — space and organisation invitations addressed to the account's email.
- **Session participation** — the user's id is removed from app-session participant lists.
- **Payment customer PII** — the name and email on the app-payments customer records are cleared in
  place. The customer record itself, its provider linkage, and the financial documents (payments,
  invoices, refunds, subscriptions) are retained under legal (bookkeeping) retention.
- **Merge records** — as the final step, the records linking the account to its merged-away ids are
  destroyed. Analytics events keyed by user id (BigQuery) are not deleted; destroying the linkage is
  what leaves those ids pseudonymous — after erasure nothing on the platform can map them back to a
  person.

Every erasure writes a persisted report (per-space and per-step outcomes and counts), and the job is
idempotent: a partial failure is retried and finishes what is left.

## What is not erased

- **Audit events** — kept for legal compliance and security accountability (GDPR Article 17(3)),
  with a 13-month TTL. Stored events are immutable by design, so they are minimized at write time
  instead: user actors and user targets are recorded as user ids only (never email or name), and the
  Portal resolves the id to a live name at view time — for an erased user the views fall back to the
  bare id, which the erasure has left unlinkable. The request IP on an event is a short-lived
  operational value bounded by the same TTL. Invitation events keep the invited email: that address
  is the audited fact itself, and it may never correspond to a user account.
- **Financial records** — kept under statutory bookkeeping retention, with the customer PII cleared
  as described above.
- **BigQuery analytics** — event rows are kept but pseudonymized by destroying the id linkage (see
  above).

## Retention windows for residual copies

Erasure removes the source of truth immediately; two bounded windows cover copies that live outside
it:

- **App-session logs** — session console output and crash logs are unstructured app output that can
  embed personal data. A daily sweep clears both from session records older than the log retention
  window (90 days by default, `APP_SESSION_LOG_RETENTION_DAYS`); the session record itself and its
  metadata are kept under the separate session retention.
- **Public asset caches** — public (non-private) assets are served through a CDN, and there is no
  cache invalidation: deleting the object removes it from storage but not from caches that already
  hold it. Both the CDN edge TTL and the `Cache-Control` max-age stamped on uploaded public objects
  are bounded to 24 hours, so after erasure a cached public asset can remain servable for at most
  24 hours at the edge plus 24 hours in a browser that fetched it last-minute (~48 hours worst
  case). Private assets are unaffected — they are served via short-lived signed URLs.

## What your app is responsible for

The platform erases what it stores on the user's behalf. Data your app copied elsewhere is yours to
erase:

- Rows your app wrote to its own database tables that embed a user id or personal data outside the
  per-user reactive scope.
- Personal data (names, emails, free text about a person) your app stored in `Session` or `Global`
  scoped values — those are shared state, not per-user state, so the platform cannot know what
  inside them belongs to whom.

Subscribe to the app-side erasure hook to clean this data when one of your users is erased. The
helper hands you the user id directly; the underlying event carries a `UserDataErasureEventArgs`
with the same `UserId` on it, which is what to subscribe to if you attach a handler by hand. (This
example uses Dapper's `ExecuteAsync`, so it needs the `Dapper` package and `using Dapper;`.)

<!-- ikon-code: user-data-erasure-database -->
```csharp
app.OnUserDataErasure(async userId =>
{
    await using var connection = await OpenAppDatabaseAsync();
    await connection.ExecuteAsync("DELETE FROM orders WHERE customer_id = @userId", new { userId });
});
```

The hook fires for every id in the erased user's identity closure (merged accounts included). By
the time it runs, the platform has already re-erased the user's platform-managed state on the app
side (`EraseUserStateAsync`), so the handler only needs to cover app-owned data. Delivery is
durable and at-least-once: the request is stored per space and redelivered on every session start
until a run completes without throwing — write the handler to be idempotent, and let exceptions
propagate so an incomplete cleanup is retried instead of being acknowledged. Not registering a
handler is fine when your app stores no user data outside the per-user reactive scope; the
platform-managed erasure still runs and the request is acknowledged. See the "User data erasure"
section in [Ikon Platform Events](ikon-platform-events.md) for the exact delivery semantics.
