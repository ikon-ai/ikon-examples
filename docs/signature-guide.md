# Ikon Signature Guide
<!-- checked-against: 44671146d26ce88a -->
Server-initiated eID-backed document signing for Ikon apps. Drive a signing ceremony from your app server, navigate the recipient's browser through it, and receive hash-verified signed documents back — without owning any signing infrastructure. PDFs produce a PAdES container; plain-text and Markdown documents produce an XAdES signature. The platform talks to the signing provider for you, so nothing here names one.

## TL;DR — what you wire

The signing types live in `Ikon.Common.Core.Signing`, which the scaffold's global usings do not carry — add `using Ikon.Common.Core.Signing;` to the file.

<!-- ikon-example: signature-order -->
```csharp
// In an app method that has the signer's client session id (int)
var pdfBytes = File.ReadAllBytes("contract.pdf");

var request = new SignatureOrderRequest(
    Purpose: "contract.sign",
    Documents: [new SignatureDocument("contract.pdf", "application/pdf", pdfBytes)],
    Signatory: new SignatureSignatory(
        Policy: SignaturePolicy.EidHub,
        IdentitySchemes: ["nbid"],                                  // optional; provider offers all when omitted
        RequestedAttributes: ["name", "nationalId", "dateOfBirth"]), // optional; this is the default
    Title: "Sign your contract",
    CostAttributionKey: "case-1234");

SignatureResult signed = await app.CreateSignatureOrderAsync(signerClientSessionId, request, ct);

var document = signed.Documents[0];   // long-term-validation PAdES bytes (persist as system of record)
var signer = signed.Signatories[0].Signer;
// document.Bytes, document.Hash, signed.SignedAt
// signer?.FullName, signer?.DateOfBirth, signer?.IdentityScheme, signer?.NationalIdHash
```

The platform keeps the signed documents as the app's private files, with no expiry. Persist the document bytes in your own storage if they are your system of record.

## Supported document types

| MIME type | Output | Notes |
|---|---|---|
| `application/pdf` | PAdES container (long-term-validation when the scheme produces it) | Recommended for legally-binding contracts. |
| `text/plain` | XAdES signature | Suitable for terms-of-service confirmation, consent capture. |
| `text/markdown` | XAdES signature | Same as `text/plain` with Markdown rendering in the signing UI. |

All documents in a single signing session must share the same MIME type — a mixed PDF + Markdown batch is rejected. Submit them as separate signature orders if you need both.

## How the flow works

```text
┌──────────────┐  1. POST /signatures/orders (app token)  
│   Ikon app   │ ────────────────────────────────────────────────►  ┌──────────────────┐
│    server    │                                                    │  Ikon backend    │
│              │  2. { orderId, signatureUrl, expiresAt }            │  (NestJS)        │
│              │ ◄────────────────────────────────────────────────  │                  │
│              │                                                    │                  │
│              │  3. ClientFunctions.NavigateToAsync(signatureUrl)   │                  │
│              │                                                    │                  │
│   Recipient  │ ──── browser navigates to the provider ───────►   │  signing         │
│    browser   │                                                    │  provider        │
│              │ ◄──── browser redirects back to landing ──────────│  ceremony        │
│              │       /signatures/redirect-landing?outcome=…       │                  │
│              │                                                    │                  │
│              │  4. POST /webhooks/<provider> (HMAC-verified)      │                  │
│              │ ◄────────────────────────────────────────────────  │ (provider → us)  │
│              │                                                    │                  │
│              │  5. poll GET /signatures/orders/:id (every 10s)    │                  │
│              │ ────────────────────────────────────────────────►  │                  │
│              │  6. status=completed + signedDocuments + signer    │                  │
│              │ ◄────────────────────────────────────────────────  │                  │
└──────────────┘                                                    └──────────────────┘
```

`SignatureHelpers.CreateSignatureOrderAsync` does steps 1, 3, 5, and 6 for you — call `app.CreateSignatureOrderAsync` and await the result.

## Endpoint surface

The signature flow is always **server-initiated**: only an Ikon app server holding the app's own platform token (a space token, in the API's words) can create, fetch, or cancel an order. Creating one requires the app's organisation to have the `document_signature` feature, which is off by default. The recipient's browser only ever lands on the public redirect/webhook endpoints.

| Endpoint | Auth | Caller |
|---|---|---|
| `POST /signatures/orders` | app token | Ikon app server |
| `GET /signatures/orders/:orderId` | app token | Ikon app server |
| `POST /signatures/orders/:orderId/cancel` | app token | Ikon app server |
| `GET /signatures/redirect-landing` | public | recipient browser (post-ceremony) |
| `POST /webhooks/<provider>` | HMAC-verified | the signing provider |

`/signatures/redirect-landing` is intentionally public — the recipient's browser arrives here unauthenticated after the ceremony finishes. Each provider has its own webhook route, public but HMAC-verified over the raw body.

## Order status lifecycle

```text
                  ┌──────────────────┐
                  │ pending_signature│  ← initial state after POST /signatures/orders
                  └──────────────────┘
                           │ (provider reports the ceremony finished)
                           ▼
                  ┌──────────────────┐
                  │     signed       │
                  └──────────────────┘
                           │ the backend finalizes with the provider and stores what it returns.
                           │ Some providers seal the artefact in a step of their own and report it
                           │ separately; others produce it as the session closes.
                           ▼
                  ┌──────────────────┐
                  │    completed     │  ← terminal SUCCESS (signedDocuments present)
                  └──────────────────┘

  Terminal failure modes:
    • rejected         — a signatory declined to sign; `rejectionReason` set where reported
    • cancelled        — recipient cancelled OR app called POST /signatures/orders/:id/cancel
    • expired          — order TTL elapsed
    • failed           — the provider could not produce the signed document; `failureCode` set
```

The helper polls every 10s for up to 1 hour; on `completed` it returns the `SignatureResult`, on any terminal failure it throws.

## Signed-document verification

The backend ships each signed document as an item reference plus a SHA-256 hash (base64url, no padding). The .NET helper:

1. Resolves the item id to a signed download URL and downloads the bytes.
2. Recomputes SHA-256 over the bytes.
3. Compares against the recorded hash — throws on mismatch (transit corruption).

Apps don't need to re-verify; the helper has done it by the time `CreateSignatureOrderAsync` returns. The hash is also a good fingerprint to log alongside `signed.SignedAt` and the signer's `IdentityScheme` for audit.

## Signing policies

| `SignaturePolicy` | Wire value | What it means |
|---|---|---|
| `PkiSigning` | `pki-signing` | Server-side PKI signing under the certificate vendor the platform deployment configures. Refused (503) where none is configured. |
| `EidHub` | `eid-hub` | The recipient authenticates with a national eID (BankID, MitID, FTN, …) and the resulting evidence is bound to the signature. |

For most "user signs a document" flows, `EidHub` is the right policy. `PkiSigning` still sends the recipient through the ceremony.

## Cost attribution

`CostAttributionKey` (optional) is an opaque app-defined label that the backend records on the order. When the ceremony completes, the platform meters it as a `signature.*` usage event and stamps the key verbatim as that row's `TenantScope` id — so the cost lands in the same per-tenant bucket as the app's AI spend, and `app.Costs.GetDailyCostsAsync` returns it under a `CostScopeFilter(nameof(TenantScope), key)` with no further wiring. Nothing reinterprets the value: pass the exact string the app's own cost queries join on (for a multi-tenant app, the tenant id it already pushes as a `TenantScope`). An order without a key is still metered; its cost simply belongs to the space rather than to anything inside it.

An abandoned, declined or expired order is never metered. See [Ikon.App Payments Guide](app-payments-guide.md) for the broader monetization model.

## Field reference

### `SignatureOrderRequest` (C#)

| Field | Required | Description |
|---|---|---|
| `Purpose` | yes | App-declared reason, e.g. `"contract.sign"`. Logged on the order. |
| `Documents` | yes | One or more `SignatureDocument(Filename, MimeType, Bytes)`. Backend caps at 10 documents / 25 MB each. All documents in one order must share a MIME type — see "Supported document types" above. |
| `Signatory` | yes | One `SignatureSignatory(Policy, IdentitySchemes?, RequestedAttributes?)`. More than one signatory is not supported in this iteration. |
| `CostAttributionKey` | no | Opaque correlation key for billing. |
| `Title` | no | Display title for the signing ceremony. Defaults to `Signature {Purpose}`. |
| `ClientReturnUrl` | no | URL the platform's `/signatures/redirect-landing` page forwards the recipient's browser to after the ceremony, with `signing=<outcome>&orderId=…` appended. Must be an allowed redirect target on the platform, or order creation is refused (400); the landing page re-checks it. When unset or no longer allowed, the landing page shows a plain "you may close this window" page instead. |

### `SignatureResult` (returned to app)

| Field | Description |
|---|---|
| `OrderId` | Platform order ID. |
| `SignedAt` | When the signing provider recorded the signature. |
| `Documents` | One `SignedDocument` per signed artefact — usually one, since a PDF order seals the whole collection into a single container. |
| `Signatories` | One `SignatureSignatoryResult` per party to the order. |

### `SignedDocument`

| Field | Description |
|---|---|
| `Filename` | Name the platform stored the artefact under. |
| `MimeType` | The content type the provider returned the signed artefact with: `application/pdf` for PAdES, the provider's type for an XAdES signature, `application/octet-stream` when it gave none. |
| `Bytes` | Signed document bytes — PAdES container for PDF input (long-term-validation when the scheme produces it), XAdES signature for text/Markdown input. |
| `Hash` | SHA-256 (base64url) of `Bytes`, already verified by the helper. |

### `SignatureSignatoryResult`

| Field | Description |
|---|---|
| `Status` | A `SignatoryStatus` — `Pending`, `Signed`, `Rejected` or `Failed`. |
| `RejectionReason` | What the signatory gave for declining, when they did and the provider reports one. |
| `Signer` | The identity behind the signature; null until this party has signed. |

### `SignatureSignerIdentity`

| Field | Description |
|---|---|
| `FullName`, `GivenName`, `FamilyName` | The signer's legal name as the eID reported it, when the order asked for the `name` attribute. Null otherwise. This is what you show a user and check against the signer you expected. |
| `DateOfBirth` | ISO 8601 calendar date, when the order asked for `dateOfBirth` and the eID supplied it. |
| `NationalIdHash` | Keyed hash of the signer's national identity number. The number itself never leaves the platform. |
| `SubjectHash` | Keyed hash of the provider's stable identifier for this person. |
| `IdentityScheme` | eID scheme used, in the platform's own vocabulary (e.g. `nbid`, `ftn`, `bankid-se`, `mitid`). |
| `AssuranceLevel` | How strongly the identity was proven, when the provider reports it. Not every provider does. |
| `SignedAt` | When this signatory signed. |
| `EvidenceToken`, `EvidenceKeySet` | The provider's own signed attestation of this identity and the key set that verifies it, where it issues one. Verifiable without trusting the platform's copy of the fields above. |

`IdentityScheme` and `AssuranceLevel` describe *how strongly* somebody authenticated, never *who*. If your app hands out a link that anyone holding the URL can complete, compare `FullName` against the party you addressed it to — otherwise a completed ceremony proves only that some real identity signed, not that it was the intended one.

The hashes are keyed by a platform secret, so an app can compare two ceremonies for the same person but cannot recompute one from a national identity number it already holds. The platform keeps the identity on the order; persist what you need long-term yourself.

## Webhook configuration

Each signing provider calls its own platform-side webhook route when an order transitions, and the platform verifies HMAC-SHA256 over the raw body before acting on anything. Webhooks are required for orders to progress past `pending_signature`. Signicat is offered for new orders only when its webhook secret is configured on the platform; Idura generates a fresh webhook secret for each order.

## Failure handling

The helper throws on every terminal failure. Refusals before polling starts pass through the catches below: `ArgumentException` for a blank `Purpose` or no `Documents`, `FeatureNotEnabledException` when `document_signature` is off, `HttpRequestException` when the platform rejects the order (400 for unsupported, mixed, empty or oversized documents, more than 10 documents or a disallowed `ClientReturnUrl`; 503 when no configured provider can sign it), and an `InvalidOperationException` starting "Failed to navigate" that the case-sensitive `"failed"` filter does not match.

<!-- ikon-example: signature-failures -->
```csharp
try
{
    var signed = await app.CreateSignatureOrderAsync(signerClientSessionId, request, ct);
    // success path
}
catch (TimeoutException)
{
    // 1h cap elapsed without reaching `completed`
}
catch (InvalidOperationException ex) when (ex.Message.Contains("rejected"))
{
    // the signatory declined to sign
}
catch (InvalidOperationException ex) when (ex.Message.Contains("cancelled"))
{
    // recipient cancelled, or app called POST /signatures/orders/:id/cancel
}
catch (InvalidOperationException ex) when (ex.Message.Contains("expired"))
{
    // order TTL elapsed
}
catch (InvalidOperationException ex) when (ex.Message.Contains("failed"))
{
    // the provider could not produce the signed document; details in ex.Message
}
```

## Related

- [Asset System Developer Guide](asset-system-guide.md) — how to persist the signed bytes in app storage.
- [Ikon.App Payments Guide](app-payments-guide.md) — cost attribution and monetization context.
