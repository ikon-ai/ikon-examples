<!-- checked-against: e7dc5d999f11d28f2ba2921c -->

# Ikon.App.Sso Guide

Let your business customers sign in to your app through their own directory — Microsoft Entra ID,
Google Workspace, or any OpenID Connect provider (Okta, Auth0, Keycloak, PingFederate). Their users
then authenticate under their own conditional-access rules, MFA and offboarding: someone their IT
disables cannot start a new session. `app.SsoConnections` (an `SsoConnectionsService`) is the
entry point, so an app's own tenant-admin screen can configure it without anyone touching the
Portal.

A connection belongs to the app. An app that serves many customers holds one
connection per customer and decides for itself which connection belongs to which of its tenants.
Creating one needs the app's `sso-connection.per-space` quota, which the plans that include SSO
raise.

## Turn on the sign-in UI

Add `sso` to the app's sign-in methods in `ikon-config.toml`:

```toml
[Auth]
Methods = ["sso", "google", "email"]
```

The sign-in screen then offers the customer's IdP in two ways: a "Continue with …" button on a host
bound to a connection (`SsoConnectionsService.SetHostsAsync`), and an email field that routes an
address to its company's IdP once that company's domain is proven by DNS. The auth service refuses a
method the app has not declared, so an app that lists only `sso` cannot be entered with a consumer
account through any URL.

## Which domains a connection may speak for

A connection only ever signs in people whose email is in one of its domains, and only while that
domain holds one of two kinds of proof:

| Proof | Available on | What it grants |
|---|---|---|
| The IdP's own domain attestation | `MicrosoftEntra` (the token's `xms_edov`, from the pinned directory), `GoogleWorkspace` (the token's `hd`) | Sign-in — nothing to publish |
| A TXT record at `_ikon-verify.<domain>` | Every preset; the only proof `Oidc` accepts | Sign-in, email routing on the sign-in screen, and `SsoRequiredMode` |

Anything else — an address outside the connection's domains, a guest from another organisation in the
customer's Entra directory, a token from another directory — is refused, never let in as an
unverified user. A DNS proof is re-checked every day; if the record goes missing it keeps working for
72 hours and is revoked after that.

`SsoConnection.AttestsDomains` says whether a connection's domains are expected to sign in without DNS
proof: always on Google Workspace and on the platform's Entra registration, and on a customer's own
Entra registration when it was created or updated with `EntraDomainAttestation = true`. It reports
that setting, not what decides a sign-in: an Entra token that carries `xms_edov` is let in without DNS
proof whatever the flag says. Where it is true, treat the TXT
record as optional and do not ask the customer for it: nothing is looked up until
`VerifyDomainAsync` is called. Each `SsoEmailDomain` records `LastSignInAt` and `AttestedAt`, the last
sign-in whose token carried the provider's verification — an `AttestedAt` older than `LastSignInAt`
means the registration stopped sending it.

## Microsoft Entra ID

The quickest setup uses the platform's own registration: the customer's admin consents once, and no
client secret changes hands. Creating the connection, or adding a domain to it, is refused when
Microsoft maps an email domain to a directory other than `EntraTenantId`; a domain Microsoft does not
know is allowed.

<!-- ikon-example: sso-create-entra -->
```csharp
// The customer's admin consents to the platform's registration in their directory, so
// there is no client secret to collect — only the directory id.
var connection = await app.SsoConnections.CreateAsync(new SsoConnectionOptions
{
    Name = tenantName,
    Preset = SsoPreset.MicrosoftEntra,
    Registration = SsoRegistration.Platform,
    EntraTenantId = directoryId,
    EmailDomains = ["lawfirm.example"],
    SessionMaxAge = TimeSpan.FromHours(10),
});
```

Then show the customer's admin the consent link,
`https://auth.ikonai.com/oauth/sso/admin-consent?connection=<connection id>`
(`auth.dev.ikonai.com` on development). Once they accept, their users can sign in.

To use the customer's own app registration instead, set `Registration = SsoRegistration.Own`, pass
its `ClientId` and `ClientSecret`, and ask whether it sends `xms_edov`, recording the answer in
`EntraDomainAttestation`. In Entra, the registration needs:

- a **Web** redirect URI of `https://auth.ikonai.com/oauth/sso/callback` — the same for every customer;
- the `email` optional claim on the ID token, and `xms_edov` too for `EntraDomainAttestation = true`.
  Without `xms_edov` a domain signs no one in until it is proven by DNS.

## Google Workspace

`Preset = SsoPreset.GoogleWorkspace` with `Registration = SsoRegistration.Platform` needs no setup on
the customer's side: Google only puts `hd` in a token for a Workspace domain it has verified, and the
connection requires it to be one of the connection's domains. A personal Google account is refused.
An own registration is an OAuth client in the customer's Google Cloud project with the same redirect
URI as above.

## Any OpenID Connect provider

Create a web application in the IdP with the redirect URI above and the scopes `openid email profile`,
then:

<!-- ikon-example: sso-create-oidc -->
```csharp
var connection = await app.SsoConnections.CreateAsync(new SsoConnectionOptions
{
    Name = "Law Firm",
    Preset = SsoPreset.Oidc,
    Issuer = "https://lawfirm.okta.com",
    ClientId = "0oa1b2c3d4",
    ClientSecret = clientSecret,
    EmailDomains = ["lawfirm.example"],
});

// A generic identity provider signs no one in until its domain is proven. Show the admin the record to publish.
foreach (var domain in connection.EmailDomains)
{
    Log.Instance.Info($"Publish TXT {domain.RecordName} = {domain.Value}");
}
```

The issuer must be `https` on the public internet and publish a discovery document at
`<issuer>/.well-known/openid-configuration`; both are checked when the connection is saved. Saving is
also refused when that document names a different `issuer` (only a trailing slash is ignored) or lacks
a public `https` `authorization_endpoint`, `token_endpoint` or `jwks_uri`. The
client secret is write-only: `SsoConnection.ClientSecretSet` says whether one is stored, never what
it is.

## Proving a domain and binding a host

<!-- ikon-example: sso-verify-domain -->
```csharp
// Once the TXT record is published. The platform keeps looking for about two hours.
connection = await app.SsoConnections.VerifyDomainAsync(connection.Id, "lawfirm.example");

// Offer the identity provider up front on the tenant's own host.
connection = await app.SsoConnections.SetHostsAsync(connection.Id, ["lawfirm.yourapp.com"]);
```

`SsoEmailDomain.State`, an `SsoDomainState`, moves from `Pending` to `DnsPending` to `Verified` (or `Failed`, when the
record was not found in time — publish it and call `VerifyDomainAsync` again). A `Verified` domain
whose record the daily re-check finds missing stays `Verified` for 72 hours, then moves to `Failed`
too. A host must be one the app serves. `RemoveDomainVerificationAsync` withdraws a proof no longer wanted: the domain
returns to `Pending` and is not looked up again, and where the connection `AttestsDomains` its users
keep signing in. Losing the connection's last DNS-proven domain this way, by removal or by the re-check
revoking it, also sets `RequiredMode` back to `Off`.

## Requiring SSO

Once a domain is proven by DNS, a connection can require its users to come in through it. Move in two
steps, so nobody is surprised:

<!-- ikon-example: sso-require -->
```csharp
// Admit everyone, but record who still signs in some other way…
await app.SsoConnections.UpdateAsync(connection.Id, new SsoConnectionUpdate { RequiredMode = SsoRequiredMode.Warn });

var stragglers = await app.SsoConnections.ListRequiredWarningsAsync(connection.Id);

// …then, once nobody is left, send the domain's users to their identity provider.
if (stragglers.Count == 0)
{
    await app.SsoConnections.UpdateAsync(connection.Id, new SsoConnectionUpdate { RequiredMode = SsoRequiredMode.Enforce });
}
```

Each `SsoRequiredWarning` names a user who entered in the last 30 days by another method, and which
one. Under `Enforce`, a session for that domain signed in any other way — Google, Microsoft, an email
code, a passkey — is refused when it enters the app, and the sign-in screen sends the person to their
IdP. If the directory asserts the same address the person's existing account already holds, that
account is kept and joined to the connection, so their data in the app follows them.

The requirement covers the whole app: every hostname the app answers on, every route, and MCP
clients authorizing against it. It knows nothing of the app's own tenants, so use it when everyone on
the domain must use the IdP everywhere in the app — an internal tool, or an app one organisation owns.
An app serving several organisations, where only one of them requires SSO, keeps
`RequiredMode` at `Off` and enforces per tenant itself (below).

## Reading how a user signed in

`Context.AuthProvider` names the method a session signed in with (`sso`, `google`, `email`, …) and
`Context.SsoConnectionId` names the connection. Both come from the platform's signed token, so a
policy finer than `Enforce` — one tenant requiring SSO, another not — is the app's to decide:

<!-- ikon-example: sso-read-context -->
```csharp
// A softer policy than `Enforce`, decided per tenant by the app itself.
if (context.AuthProvider != "sso" || context.SsoConnectionId != tenantConnectionId)
{
    await ClientFunctions.LoginAsync($"sso:{tenantConnectionId}", context.SessionId);
}
```

## Sessions and offboarding

- `SessionMaxAge` (one hour to seven days) ends a session signed in through the connection however
  active it is, sending the person back to their IdP — which is what makes a directory disable take
  effect within that time.
- Disabling a connection (`Status = SsoConnectionStatus.Disabled`) stops new sign-ins at once and stops
  its sessions renewing within fifteen minutes.
- Deleting a connection leaves its users' accounts intact; they can still sign in by any other method
  the app offers.
