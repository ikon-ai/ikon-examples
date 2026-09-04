using System.Data.Common;
using Dapper;
using Ikon.Common.Core.Signing;

// The smaller published guides, as code that compiles.
//
// Each holder is the one file its guide reads as — the shared names are its fields, and each
// `#region docsnippet:` is one fence that CodegenPatternGenerator regenerates into the guide.
// A published guide keeps literal code in its fence, so the marker sits above it rather than
// replacing it.

file sealed class DocAppFiles(IApp<SessionIdentity, ClientParams> app)
{
    public async Task RunAsync(string id, byte[] bytes)
    {
        #region docsnippet:app-files
        // Read a shipped (or previously written) private file.
        var rules = await app.Files.Data.ReadTextAsync("rules.md");

        // Store a generated image and get the URL to show it.
        await app.Files.Public.WriteBytesAsync($"thumbnails/{id}.png", bytes, "image/png");
        var url = await app.Files.Public.GetUrlAsync($"thumbnails/{id}.png");
        #endregion

        Log.Instance.Debug($"{rules} {url}");
    }
}

file sealed class DocUserDataErasure(IApp<SessionIdentity, ClientParams> app)
{
    public void WithDatabase()
    {
        #region docsnippet:user-data-erasure-database
        app.OnUserDataErasure(async userId =>
        {
            await using var connection = await OpenAppDatabaseAsync();
            await connection.ExecuteAsync("DELETE FROM orders WHERE customer_id = @userId", new { userId });
        });
        #endregion
    }

    public void Bare()
    {
        #region docsnippet:user-data-erasure
        app.OnUserDataErasure(async userId =>
        {
            // Delete app-owned data for this user: rows in your own tables,
            // personal data embedded in Session/Global scoped values.
        });
        #endregion
    }

    private async Task<DbConnection> OpenAppDatabaseAsync() =>
        await app.DatabaseAsync(app.Databases.First().Name);
}

file sealed class DocSignature(IApp<SessionIdentity, ClientParams> app)
{
    public async Task OrderAsync(int signerClientSessionId, CancellationToken ct)
    {
        #region docsnippet:signature-order
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
        #region docsnippet:signature-failures
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

file static class DocFlutterTargets
{
    public static void Render(UIView view)
    {
        #region docsnippet:flutter-target-variants
        view.Box(
            style: [
                "px-3 py-2 rounded-md",                                  // shared
                "web:(bg-background text-secondary border border-input)",// web only
                "flutter:(bg-slate-900 text-slate-100 border border-slate-700)" // Flutter only
            ],
            content: view => view.Text(text: "Adapts per target"));
        #endregion
    }
}
