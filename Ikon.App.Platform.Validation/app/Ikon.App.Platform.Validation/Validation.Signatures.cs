using System.Globalization;
using Ikon.Common.Core.Signing;

public partial class Validation
{
    private const string SigDocGenerated = "generated";
    private const string SigDocSample = "sample";
    private const string SigDocText = "text";

    private static readonly IReadOnlyList<SelectOption> SigDocumentOptions =
    [
        new SelectOption(SigDocGenerated, "Generated one-page PDF"),
        new SelectOption(SigDocSample, "data/sample.pdf"),
        new SelectOption(SigDocText, "Plain text (.txt)"),
    ];

    private static readonly IReadOnlyList<SelectOption> SigPolicyOptions =
    [
        new SelectOption(nameof(SignaturePolicy.EidHub), "EidHub (eID authentication)"),
        new SelectOption(nameof(SignaturePolicy.PkiSigning), "PkiSigning (qualified PKI)"),
    ];

    // The ceremony takes over the signer's tab, so the client that pressed the button is gone by the
    // time the order settles; the state is shared so whoever comes back to the tab sees the outcome.
    private readonly Reactive<string> _sigDocument = new(SigDocGenerated);
    private readonly Reactive<string> _sigPolicy = new(nameof(SignaturePolicy.EidHub));
    private readonly Reactive<string> _sigSchemes = new("");
    private readonly Reactive<bool> _sigAttrName = new(true);
    private readonly Reactive<bool> _sigAttrNationalId = new(true);
    private readonly Reactive<bool> _sigAttrDateOfBirth = new(true);
    private readonly Reactive<bool> _sigReturnToApp = new(true);
    private readonly Reactive<bool> _sigRunning = new(false);
    private readonly Reactive<string> _sigStatus = new("IDLE no signature order yet");
    private readonly Reactive<SignatureResult?> _sigResult = new(null);

    private readonly Lock _sigLock = new();
    private CancellationTokenSource? _sigCts;

    private void InitSignatures()
    {
    }

    private void RenderSignaturesSection(UIView view)
    {
        if (RenderSectionLocked(view, "Signatures"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Signatures");

            RenderSignatureOrderCard(view);
            RenderSignatureResultCard(view);
        });
    }

    private void RenderSignatureOrderCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Order");

            view.Row([Layout.Row.Md, "items-end mb-4 flex-wrap"], content: view =>
            {
                view.Select(
                    value: _sigDocument.Value,
                    options: SigDocumentOptions,
                    label: "Document",
                    onValueChange: async v => _sigDocument.Value = v);
                view.Select(
                    value: _sigPolicy.Value,
                    options: SigPolicyOptions,
                    label: "Policy",
                    onValueChange: async v => _sigPolicy.Value = v);
                view.TextField(
                    [Input.Default, "min-w-[260px]"],
                    bind: _sigSchemes,
                    label: "Identity schemes",
                    placeholder: "empty = all offered, e.g. nbid, ftn, mitid, simulator",
                    props: TestId("sig-schemes"));
            });

            view.Row([Layout.Row.Md, "items-center mb-4 flex-wrap"], content: view =>
            {
                view.Text([Text.Label], "Requested attributes");
                view.Checkbox(bind: _sigAttrName, label: "name");
                view.Checkbox(bind: _sigAttrNationalId, label: "nationalId");
                view.Checkbox(bind: _sigAttrDateOfBirth, label: "dateOfBirth");
                view.Checkbox(bind: _sigReturnToApp, label: "Return to this app afterwards");
            });

            view.Row([Layout.Row.Md, "items-center mb-4 flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "Sign this document",
                    disabled: _sigRunning.Value,
                    onClick: StartSignatureOrder,
                    props: TestId("sig-order-start"));

                if (_sigRunning.Value)
                {
                    view.Button(
                        [Button.ErrorMd],
                        text: "Stop waiting",
                        onClick: CancelSignatureOrder,
                        props: TestId("sig-order-cancel"));
                    view.Spinner();
                }
            });

            var status = _sigStatus.Value;
            view.Text(
                [Text.Body, "break-all", status.StartsWith("PASS", StringComparison.Ordinal) ? "text-success-primary" : status.StartsWith("FAIL", StringComparison.Ordinal) ? "text-error-primary" : "text-secondary"],
                status,
                props: TestId("sig-status"));
        });
    }

    private void RenderSignatureResultCard(UIView view)
    {
        if (_sigResult.Value is not { } result)
        {
            return;
        }

        view.Box([Card.Elevated, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Signed");
            view.Text([Text.Caption, "mb-4 break-all"], $"Order {result.OrderId} · signed {result.SignedAt:yyyy-MM-dd HH:mm:ss} UTC");

            foreach (var signatory in result.Signatories)
            {
                view.Box([Card.Subtle, "p-4 mb-3"], content: view =>
                {
                    view.Text([Text.BodyStrong], $"Signatory: {signatory.Status}", props: TestId("sig-signatory-status"));

                    if (!string.IsNullOrEmpty(signatory.RejectionReason))
                    {
                        view.Text([Text.Body, "text-error-primary"], $"Rejection reason: {signatory.RejectionReason}", props: TestId("sig-rejection-reason"));
                    }

                    if (signatory.Signer is not { } signer)
                    {
                        view.Text([Text.Caption], "No signer identity (this party has not signed).");
                        return;
                    }

                    view.Column([Layout.Column.Xs, "mt-2"], content: view =>
                    {
                        RenderSignerField(view, "Full name", signer.FullName);
                        RenderSignerField(view, "Given name", signer.GivenName);
                        RenderSignerField(view, "Family name", signer.FamilyName);
                        RenderSignerField(view, "Date of birth", signer.DateOfBirth);
                        RenderSignerField(view, "Identity scheme", signer.IdentityScheme);
                        RenderSignerField(view, "Assurance level", signer.AssuranceLevel);
                        RenderSignerField(view, "Signed at", signer.SignedAt?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
                        RenderSignerField(view, "National id hash", signer.NationalIdHash);
                        RenderSignerField(view, "Subject hash", signer.SubjectHash);
                        RenderSignerField(view, "Evidence token", signer.EvidenceToken is { Length: > 0 } token ? $"{token.Length} chars" : null);
                        RenderSignerField(view, "Evidence key set", signer.EvidenceKeySet);
                    });
                });
            }

            foreach (var document in result.Documents)
            {
                view.Row([Layout.Row.Md, "items-center flex-wrap mt-2"], content: view =>
                {
                    view.ActionButton(
                        [Button.PrimaryMd],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions { Filename = document.Filename, Data = document.Bytes },
                        content: v => v.Text([Text.BodySm], text: $"Download {document.Filename}"));
                    view.Text([Text.Caption, "break-all"], $"{document.MimeType} · {document.Bytes.Length:#,##0} bytes · sha256 {document.Hash}");
                });
            }
        });
    }

    private static void RenderSignerField(UIView view, string label, string? value)
    {
        view.Row(["gap-2 items-baseline min-w-0"], content: view =>
        {
            view.Text([Text.Caption, "text-tertiary shrink-0 w-36"], label);
            view.Text([Text.BodySm, "break-all"], value ?? "(not provided)");
        });
    }

    private async Task StartSignatureOrder()
    {
        var signerClientSessionId = ReactiveScope.ClientId;
        CancellationTokenSource cts;

        lock (_sigLock)
        {
            if (_sigRunning.Value)
            {
                return;
            }

            _sigCts?.Dispose();
            _sigCts = cts = new CancellationTokenSource();
            _sigRunning.Value = true;
        }

        _sigResult.Value = null;
        _sigStatus.Value = "PENDING creating the order";

        SignatureOrderRequest request;

        try
        {
            request = await BuildSignatureOrderRequestAsync();
        }
        catch (Exception ex)
        {
            FinishSignatureOrder(cts, $"FAIL could not prepare the order: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // The ceremony navigates this client away, which ends its session; the wait must outlive it.
        _ = Task.Run(() => RunSignatureOrderAsync(signerClientSessionId, request, cts));
    }

    private void CancelSignatureOrder()
    {
        lock (_sigLock)
        {
            _sigCts?.Cancel();
        }
    }

    private async Task RunSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationTokenSource cts)
    {
        _sigStatus.Value = $"PENDING order requested for client {signerClientSessionId}; the ceremony opens in the signer's tab";
        string outcome;

        try
        {
            var result = await app.CreateSignatureOrderAsync(signerClientSessionId, request, cts.Token);
            _sigResult.Value = result;
            var signatory = result.Signatories.FirstOrDefault();
            outcome = $"PASS signed: order {result.OrderId}, signatory {signatory?.Status.ToString() ?? "none"}, {result.Documents.Count} document(s)";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            outcome = "CANCELLED the app stopped waiting; the order stays open at the provider until it expires and is never billed";
        }
        catch (TimeoutException ex)
        {
            outcome = $"FAIL timed out: {ex.Message}";
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rejected", StringComparison.Ordinal))
        {
            outcome = $"REJECTED {ex.Message}";
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("cancelled", StringComparison.Ordinal))
        {
            outcome = $"CANCELLED {ex.Message}";
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired", StringComparison.Ordinal))
        {
            outcome = $"EXPIRED {ex.Message}";
        }
        catch (FeatureNotEnabledException ex)
        {
            outcome = $"FAIL feature not enabled: {ex.Message}";
        }
        catch (UserException ex)
        {
            outcome = $"FAIL order refused: {ex.Message}";
        }
        catch (Exception ex)
        {
            outcome = $"FAIL {ex.GetType().Name}: {ex.Message}";
        }

        FinishSignatureOrder(cts, outcome);
    }

    private void FinishSignatureOrder(CancellationTokenSource cts, string outcome)
    {
        lock (_sigLock)
        {
            if (ReferenceEquals(_sigCts, cts))
            {
                _sigCts = null;
            }

            _sigStatus.Value = outcome;
            _sigRunning.Value = false;
        }

        cts.Dispose();
    }

    private async Task<SignatureOrderRequest> BuildSignatureOrderRequestAsync()
    {
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var document = _sigDocument.Value switch
        {
            SigDocSample => new SignatureDocument("sample.pdf", "application/pdf", await File.ReadAllBytesAsync(Path.Combine(app.DataDirectory, "sample.pdf"))),
            SigDocText => new SignatureDocument("validation-agreement.txt", "text/plain", Encoding.UTF8.GetBytes($"Ikon platform validation agreement\n\nCreated {stamp}. Signing this text document has no legal effect.\n")),
            _ => new SignatureDocument("validation-agreement.pdf", "application/pdf", BuildValidationPdf(["Ikon platform validation agreement", $"Created {stamp}", "Signing this document has no legal effect."])),
        };

        var schemes = _sigSchemes.Value
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var attributes = new List<string>();

        if (_sigAttrName.Value)
        {
            attributes.Add("name");
        }

        if (_sigAttrNationalId.Value)
        {
            attributes.Add("nationalId");
        }

        if (_sigAttrDateOfBirth.Value)
        {
            attributes.Add("dateOfBirth");
        }

        var policy = _sigPolicy.Value == nameof(SignaturePolicy.PkiSigning) ? SignaturePolicy.PkiSigning : SignaturePolicy.EidHub;

        return new SignatureOrderRequest(
            Purpose: "validation.sign",
            Documents: [document],
            Signatory: new SignatureSignatory(
                Policy: policy,
                IdentitySchemes: schemes.Count > 0 ? schemes : null,
                RequestedAttributes: attributes),
            CostAttributionKey: "validation-signatures",
            Title: "Ikon validation: sign a test document",
            ClientReturnUrl: _sigReturnToApp.Value ? SignatureReturnUrl() : null);
    }

    // The provider appends ?signing=<outcome>&orderId=<id>; without a return URL the signer lands on
    // a static "close this tab" page.
    private string? SignatureReturnUrl()
    {
        var publicUrl = app.PublicUrl;

        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        return $"{publicUrl.TrimEnd('/')}/signatures";
    }

    // A hand-written single-page PDF: the xref table needs the exact byte offset of every object.
    private static byte[] BuildValidationPdf(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder("BT /F1 16 Tf 72 770 Td 22 TL\n");

        foreach (var line in lines)
        {
            var escaped = new string(line.Select(c => c < 128 ? c : '?').ToArray())
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
            content.Append('(').Append(escaped).Append(") Tj T*\n");
        }

        content.Append("ET");

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = pdf.Length;
        pdf.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        pdf.Append("trailer\n<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefOffset).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
