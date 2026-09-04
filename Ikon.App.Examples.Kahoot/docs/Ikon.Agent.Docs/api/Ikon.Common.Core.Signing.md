namespace Ikon.Common.Core.Signing
  enum SignatoryStatus
    Pending
    Signed
    Rejected
    Failed
  sealed record SignatureDocument
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  sealed record SignatureOrderRequest
    ctor(string Purpose, IReadOnlyList<SignatureDocument> Documents, SignatureSignatory Signatory, string? CostAttributionKey = null, string? Title = null, string? ClientReturnUrl = null)
    string? ClientReturnUrl { get; init; }
    string? CostAttributionKey { get; init; }
    IReadOnlyList<SignatureDocument> Documents { get; init; }
    string Purpose { get; init; }
    SignatureSignatory Signatory { get; init; }
    string? Title { get; init; }
  enum SignaturePolicy
    PkiSigning
    EidHub
  // The platform downloads the result from the signing provider, verifies it, and hands the signed bytes plus the evidence for each signatory to the requesting app. Apps should persist Documents as the system of record — the platform retention is short.
  sealed record SignatureResult
    ctor(string OrderId, DateTimeOffset SignedAt, IReadOnlyList<SignedDocument> Documents, IReadOnlyList<SignatureSignatoryResult> Signatories)
    IReadOnlyList<SignedDocument> Documents { get; init; }
    string OrderId { get; init; }
    IReadOnlyList<SignatureSignatoryResult> Signatories { get; init; }
    DateTimeOffset SignedAt { get; init; }
  // IdentitySchemes names the national eIDs the signatory may authenticate with, in the platform's vocabulary (bankid-se, nbid, mitid, ftn, …); leave it null to let the signing provider offer its full set. RequestedAttributes selects from name, nationalId and dateOfBirth, and defaults to all three — an attribute the order does not ask for is not retained even when the eID reports it.
  sealed record SignatureSignatory
    ctor(SignaturePolicy Policy, IReadOnlyList<string>? IdentitySchemes = null, IReadOnlyList<string>? RequestedAttributes = null)
    IReadOnlyList<string>? IdentitySchemes { get; init; }
    SignaturePolicy Policy { get; init; }
    IReadOnlyList<string>? RequestedAttributes { get; init; }
  // Signer is null until this party has actually signed. IdentityScheme and AssuranceLevel on it describe how strongly somebody authenticated, never who: if the ceremony link is a bearer token that anyone holding the URL can complete, compare SignatureSignerIdentity.FullName against the party you addressed it to.
  sealed record SignatureSignatoryResult
    ctor(SignatoryStatus Status, string? RejectionReason, SignatureSignerIdentity? Signer)
    string? RejectionReason { get; init; }
    SignatureSignerIdentity? Signer { get; init; }
    SignatoryStatus Status { get; init; }
  // FullName, GivenName, FamilyName and DateOfBirth are present only when the order requested the matching attribute and the eID supplied it; DateOfBirth is an ISO 8601 calendar date. The national identity number itself is never returned — NationalIdHash and SubjectHash are keyed by a platform secret, so they correlate two ceremonies by the same person but cannot be recomputed by an app or compared against a number it holds. EvidenceToken is the provider's own signed attestation of this identity, verifiable against EvidenceKeySet, where the provider issues one.
  sealed record SignatureSignerIdentity
    ctor(string? FullName, string? GivenName, string? FamilyName, string? DateOfBirth, string? NationalIdHash, string? SubjectHash, string? IdentityScheme, string? AssuranceLevel, DateTimeOffset? SignedAt, string? EvidenceToken, string? EvidenceKeySet)
    string? AssuranceLevel { get; init; }
    string? DateOfBirth { get; init; }
    string? EvidenceKeySet { get; init; }
    string? EvidenceToken { get; init; }
    string? FamilyName { get; init; }
    string? FullName { get; init; }
    string? GivenName { get; init; }
    string? IdentityScheme { get; init; }
    string? NationalIdHash { get; init; }
    DateTimeOffset? SignedAt { get; init; }
    string? SubjectHash { get; init; }
  // Hash is the SHA-256 of Bytes as base64url without padding, already verified against the platform's copy by the time an app receives it.
  sealed record SignedDocument
    ctor(string Filename, string MimeType, byte[] Bytes, string Hash)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
