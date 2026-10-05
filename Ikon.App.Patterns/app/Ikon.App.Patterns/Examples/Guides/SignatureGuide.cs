using Ikon.Common.Core.Signing;

namespace Ikon.App.Patterns.Examples;

file sealed class SignatureExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task OrderAsync(int signerClientSessionId, CancellationToken ct)
    {
        #region example:signature-order
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
        #endregion

        Log.Instance.Debug($"{document} {signer}");
    }

    public async Task FailuresAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct)
    {
        #region example:signature-failures
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
        #endregion
    }
}
