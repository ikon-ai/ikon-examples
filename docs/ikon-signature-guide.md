# Ikon Signature Guide

Server-initiated eID-backed document signing for Ikon apps. Drive a Signicat signing ceremony from your app server, navigate the recipient's browser through it, and receive a hash-verified signed document back — without owning any signing infrastructure. PDFs produce a PAdES container; plain-text and Markdown documents produce an XAdES signature.

## TL;DR — what you wire

```csharp
// In an app method that has the signer's client session id (int)
var pdfBytes = File.ReadAllBytes("contract.pdf");

var request = new SignatureOrderRequest(
    Purpose: "contract.sign",
    Documents: [new SignatureDocument("contract.pdf", "application/pdf", pdfBytes)],
    Signer: new SignatureSigner(
        Policy: SignaturePolicy.EidHub,
        IdpNames: ["nbid"],                  // optional IdP hints
        RequestedAttributes: ["name"]),       // optional claim hints
    Title: "Sign your contract",
    CostAttributionKey: "case-1234");

SignedDocument signed = await app.CreateSignatureOrderAsync(signerClientSessionId, request, ct);

// signed.Bytes — long-term-validation PAdES bytes (persist as system of record)
// signed.SignedAt, signed.SignedDocumentHash, signed.IdentityScheme, signed.EvidenceLevel
```

The platform's session retention for signed documents is short (45 days). Persist `signed.Bytes` yourself if you need it long-term.

## Supported document types

| MIME type | Output | Notes |
|---|---|---|
| `application/pdf` | PAdES container (long-term-validation when the scheme produces it) | Recommended for legally-binding contracts. |
| `text/plain` | XAdES signature | Suitable for terms-of-service confirmation, consent capture. |
| `text/markdown` | XAdES signature | Same as `text/plain` with Markdown rendering in the signing UI. |

All documents in a single signing session must share the same MIME type — a mixed PDF + Markdown batch is rejected. Submit them as separate signature orders if you need both.

## How the flow works

```
┌──────────────┐  1. POST /signatures/orders (space token)
│   Ikon app   │ ────────────────────────────────────────────────►  ┌──────────────────┐
│    server    │                                                    │  Ikon backend    │
│              │  2. { orderId, signatureUrl, expiresAt }            │  (NestJS)        │
│              │ ◄────────────────────────────────────────────────  │                  │
│              │                                                    │                  │
│              │  3. ClientFunctions.SetUrlAsync(signatureUrl)       │                  │
│              │                                                    │                  │
│   Recipient  │ ──── browser navigates to Signicat ───────────►   │   Signicat        │
│    browser   │                                                    │   signing         │
│              │ ◄──── browser redirects back to landing ──────────│   ceremony        │
│              │       /signatures/redirect-landing?outcome=…       │                  │
│              │                                                    │                  │
│              │  4. POST /webhooks/signicat (HMAC-verified)        │                  │
│              │ ◄────────────────────────────────────────────────  │  (Signicat → us) │
│              │                                                    │                  │
│              │  5. poll GET /signatures/orders/:id (every 10s)    │                  │
│              │ ────────────────────────────────────────────────►  │                  │
│              │  6. status=packaged + signedDocumentItemId + hash  │                  │
│              │ ◄────────────────────────────────────────────────  │                  │
└──────────────┘                                                    └──────────────────┘
```

`SignatureHelpers.CreateSignatureOrderAsync` does steps 1, 3, 5, and 6 for you — call `app.CreateSignatureOrderAsync` and await the result.

## Endpoint surface

The signature flow is always **server-initiated**: only an Ikon app server holding a space token can create, fetch, or cancel an order. The recipient's browser only ever lands on the public redirect/webhook endpoints.

| Endpoint | Auth | Caller |
|---|---|---|
| `POST /signatures/orders` | space token | Ikon app server |
| `GET /signatures/orders/:orderId` | space token | Ikon app server |
| `POST /signatures/orders/:orderId/cancel` | space token | Ikon app server |
| `GET /signatures/redirect-landing` | public | recipient browser (post-ceremony) |
| `POST /webhooks/signicat` | HMAC-verified | Signicat backend |

`/signatures/redirect-landing` is intentionally public — the recipient's browser arrives here unauthenticated after Signicat finishes. The webhook is public but the request body is HMAC-verified against `SIGNATURE_SIGNICAT_WEBHOOK_SECRET`.

## Order status lifecycle

```
                  ┌──────────────────┐
                  │ pending_signature│  ← initial state after POST /signatures/orders
                  └──────────────────┘
                           │ (Signicat: signing-session.completed webhook)
                           ▼
                  ┌──────────────────┐
                  │     signed       │
                  └──────────────────┘
                           │ PDF/PAdES:  package.completed webhook → backend fetches container
                           │ Text/XAdES: no package.completed fires — the signing-session.completed
                           │             handler fetches the signature output directly
                           ▼
                  ┌──────────────────┐
                  │    packaged      │  ← terminal SUCCESS (signedDocumentItemId present)
                  └──────────────────┘

  Terminal failure modes:
    • cancelled        — recipient cancelled OR app called POST /signatures/orders/:id/cancel
    • expired          — order TTL elapsed
    • package_failed   — Signicat couldn't produce the signed document; `failureCode` set
```

The helper polls every 10s for up to 1 hour; on `packaged` it returns the `SignedDocument`, on any failure terminal it throws.

## Signed-document verification

The backend ships the signed document as an item reference (`signedDocumentItemId`) plus a SHA-256 hash (base64url, no padding). The .NET helper:

1. Resolves `signedDocumentItemId` to a signed download URL and downloads the bytes.
2. Recomputes SHA-256 over the bytes.
3. Compares against `signedDocumentHash` — throws on mismatch (transit corruption).

Apps don't need to re-verify; the helper has done it by the time `CreateSignatureOrderAsync` returns. The hash is also a good fingerprint to log alongside `signed.SignedAt` and `signed.IdentityScheme` for audit.

## Signing policies

| `SignaturePolicy` | Wire value | What it means |
|---|---|---|
| `PkiSigning` | `pki-signing` | Server-side PKI signing using a vendor key. No eID step required of the recipient. |
| `EidHub` | `eid-hub` | Signicat eID Hub — recipient authenticates with a national eID (BankID, Smart-ID, FTN, …) and the resulting evidence is bound to the signature. |

For most "user signs a document" flows, `EidHub` is the right policy. `PkiSigning` is for unattended / pre-authorized signing.

## Cost attribution

`CostAttributionKey` (optional) is an opaque app-defined label that the backend records on the order. Use it to correlate signing cost back to a domain entity (case ID, transaction ID, customer ID) for billing. See [Ikon.App Payments Guide](ikon-app-payments-guide.md) for the broader monetization model.

## Field reference

### `SignatureOrderRequest` (C#)

| Field | Required | Description |
|---|---|---|
| `Purpose` | yes | App-declared reason, e.g. `"contract.sign"`. Logged on the order. |
| `Documents` | yes | One or more `SignatureDocument(Filename, MimeType, Bytes)`. Backend caps at 10 documents / 25 MB each. All documents in one order must share a MIME type — see "Supported document types" above. |
| `Signer` | yes | One `SignatureSigner(Policy, Vendor?, IdpNames?, RequestedAttributes?)`. Multi-signer is not supported in this iteration. |
| `CostAttributionKey` | no | Opaque correlation key for billing. |
| `Title` | no | Display title for the signing ceremony (Signicat UI). Defaults to `Signature {Purpose}`. |
| `ClientReturnUrl` | no | URL the platform's `/signatures/redirect-landing` page forwards the recipient's browser to after the ceremony, with `signing=<outcome>&orderId=…` appended. When unset, the landing page shows a plain "you may close this window" page instead. |

### `SignedDocument` (returned to app)

| Field | Description |
|---|---|
| `OrderId` | Platform order ID. |
| `Bytes` | Signed document bytes — PAdES container for PDF input (long-term-validation when the scheme produces it), XAdES signature for text/Markdown input. |
| `MimeType` | `application/pdf` for PAdES, or the input MIME type for XAdES (`text/plain`, `text/markdown`). |
| `SignedAt` | Server-recorded completion timestamp. |
| `SignedDocumentHash` | SHA-256 (base64url) of `Bytes`, already verified by the helper. |
| `IdentityScheme` | eID scheme used (e.g. `nbid`, `ftn`, `bankid-se`). |
| `SignerName` | The signer's legal name as the eID reported it, when the order asked for the `name` attribute (the default). Null otherwise. This is what you show a user and check against the signer you expected. |
| `SignerNameHash` | Optional HMAC of the signer's legal name, keyed by a platform secret. Usable as an opaque fingerprint for correlating two ceremonies; it cannot be recomputed by your app, so it can neither be displayed nor compared against a name you hold — use `SignerName` for both. |
| `EvidenceLevel` | Optional `loa:` level reported by Signicat. |

`IdentityScheme` and `EvidenceLevel` describe *how strongly* somebody authenticated, never *who*. If your app hands out a link that anyone holding the URL can complete, compare `SignerName` against the party you addressed it to — otherwise a completed ceremony proves only that some real identity signed, not that it was the intended one. Platform retention for `SignerName` matches the session retention above; persist it yourself if you need it long-term.

## Webhook configuration

`/webhooks/signicat` is the platform-side webhook endpoint Signicat calls when an order transitions. The platform verifies HMAC-SHA256 against `SIGNATURE_SIGNICAT_WEBHOOK_SECRET` over the raw body. If the secret is unset on the platform, the endpoint returns `503` — webhooks are required for orders to progress past `pending_signature`.

## Failure handling

The helper throws on every terminal failure:

```csharp
try
{
    var signed = await app.CreateSignatureOrderAsync(signerClientSessionId, request, ct);
    // success path
}
catch (TimeoutException)
{
    // 1h cap elapsed without reaching `packaged`
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
    // package_failed; details in ex.Message
}
```

## Related

- [Asset System Developer Guide](asset-system-developer-guide.md) — how to persist `signed.Bytes` in app storage.
- [Ikon.App Payments Guide](ikon-app-payments-guide.md) — cost attribution and monetization context.
