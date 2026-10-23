using Ikon.Pipelines.Public.Processors.Pdf;
using StbImageWriteSharp;

namespace Ikon.App.Patterns.Patterns;

// Pattern: invoice-extraction — see docs/patterns/invoice-extraction.md.
// The demo draws the review card, and its button reads a one-page invoice built here in place of an
// upload, since the headless render has no upload handler. The gallery's model is the mock, so the
// values it fills in are made up.
internal sealed class InvoiceExtraction : IPatternDemo
{
    public string Slug => "invoice-extraction";
    public string Title => "Invoice PDF to a reviewed record";
    public string Category => "AI";

    public void RenderDemo(IView view)
    {
        RenderReview((UIView)view);
        view.Button([Button.OutlineMd, "self-start mt-3"], text: "Read a sample invoice", disabled: _reading.Value,
            onClick: async () => await ReadInvoiceAsync(ReactiveScope.ClientId, SampleInvoice()));
    }

    #region example:pattern-invoice-extraction
    // Each value comes with the model's own confidence in it, from 0 to 1
    private sealed record Extracted<T>(T Value, double Confidence);

    private sealed record InvoiceLine(string Description, decimal Quantity, string Unit, decimal UnitPrice, double Confidence);

    private sealed record Invoice(
        bool DocumentSeen,
        Extracted<string> Supplier,
        Extracted<string> InvoiceNumber,
        Extracted<string> Date,
        Extracted<decimal> Total,
        List<InvoiceLine> Lines);

    private const LLMModel Model = LLMModel.Claude55Sonnet;
    private const double CheckBelow = 0.8;
    private const int MaxPages = 5;

    private readonly ClientReactive<Invoice?> _draft = new(null);
    private readonly ClientReactive<bool> _reading = new(false);
    private readonly ClientReactive<string?> _error = new(null);
    private readonly ReactiveList<Invoice> _approved = new();

    private void RenderUpload(UIView view)
    {
        view.FileUpload(accept: [".pdf", "application/pdf"], maxFileSize: 20 * 1024 * 1024, disabled: _reading.Value,
            onUploadComplete: async args =>
            {
                if (args.LocalTempFilePath is not { } path)
                {
                    return;
                }

                var pdf = await File.ReadAllBytesAsync(path);
                File.Delete(path);

                // The model takes seconds, and this hook must return within 50 s of the last chunk
                var clientSessionId = ReactiveScope.ClientId;
                _ = Task.Run(() => ReadInvoiceAsync(clientSessionId, pdf));
            },
            label: "Choose an invoice PDF", hint: "PDF, up to 20 MB");
    }

    private async Task ReadInvoiceAsync(int clientSessionId, byte[] pdf)
    {
        _reading.SetFor(clientSessionId, true);
        _error.SetFor(clientSessionId, null);
        _draft.SetFor(clientSessionId, null);

        try
        {
            // Opening it first refuses a damaged file or one with a password before any model call
            using (PdfDocument.Load(pdf))
            {
            }

            // A model that takes PDFs reads the file itself, its text and its layout
            var invoice = Emerge.GetCapabilities(Model).SupportsInputPdf
                ? await ExtractAsync([new PdfPart(pdf, "application/pdf")])
                : null;

            // A model answers even when the file did not reach it, so the record says whether it saw
            // one; if not, the pages go again as pictures, which every vision model reads
            if (invoice is not { DocumentSeen: true })
            {
                invoice = await ExtractAsync(PageImages(pdf));
            }

            _draft.SetFor(clientSessionId, invoice);
        }
        catch (Exception ex) when (ex is InvalidDataException or UnauthorizedAccessException)
        {
            _error.SetFor(clientSessionId, "That file is damaged or has a password, so it can't be read.");
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Reading an invoice of {pdf.Length} bytes for client {clientSessionId} failed, the user can upload it again: {ex.Message}");
            _error.SetFor(clientSessionId, "Couldn't read that invoice. Try again.");
        }
        finally
        {
            _reading.SetFor(clientSessionId, false);
        }
    }

    private static List<IMessagePart> PageImages(byte[] pdf)
    {
        var pages = new List<IMessagePart>();
        using var document = PdfDocument.Load(pdf);

        for (var i = 0; i < Math.Min(document.PageCount, MaxPages); i++)
        {
            using var page = document.GetPage(i);
            var (rgba, width, height) = page.GetPixels(1568);

            using var jpeg = new MemoryStream();
            new ImageWriter().WriteJpg(rgba, width, height, ColorComponents.RedGreenBlueAlpha, jpeg, 85);
            pages.Add(new ImagePart(jpeg.ToArray(), "image/jpeg"));
        }

        return pages;
    }

    private static async Task<Invoice> ExtractAsync(IReadOnlyList<IMessagePart> document)
    {
        var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, [new TextPart("The supplier invoice:"), .. document]));

        return await Emerge.Run<Invoice>(Model, context, pass =>
        {
            pass.SystemPrompt = """
                You read supplier invoices. Copy every value as printed; Date is YYYY-MM-DD.
                Confidence is how sure you are that the value is what the document says: 1 when it
                is printed clearly, below 0.5 when it is hard to read or you inferred it.
                DocumentSeen is false when no invoice reached you; then leave the values empty.
                """;
        });
    }

    private void RenderReview(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-xl"], content: col =>
        {
            if (_reading.Value)
            {
                col.Text([Text.Caption], "Reading the invoice…");
            }

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], error);
            }

            if (_draft.Value is not { } invoice)
            {
                col.Text([Text.Body], _approved.Count switch
                {
                    0 => "No invoice read yet.",
                    1 => "1 invoice approved.",
                    var count => $"{count} invoices approved.",
                });
                return;
            }

            // An edited value is the person's, so it counts as checked
            ReviewField(col, "Supplier", invoice.Supplier.Value, invoice.Supplier.Confidence,
                text => _draft.Value = invoice with { Supplier = new(text, 1) });
            ReviewField(col, "Invoice number", invoice.InvoiceNumber.Value, invoice.InvoiceNumber.Confidence,
                text => _draft.Value = invoice with { InvoiceNumber = new(text, 1) });
            ReviewField(col, "Date", invoice.Date.Value, invoice.Date.Confidence,
                text => _draft.Value = invoice with { Date = new(text, 1) });
            ReviewField(col, "Total", invoice.Total.Value.ToString("0.00", CultureInfo.InvariantCulture), invoice.Total.Confidence,
                text => _draft.Value = decimal.TryParse(text, CultureInfo.InvariantCulture, out var total) ? invoice with { Total = new(total, 1) } : invoice);

            foreach (var line in invoice.Lines)
            {
                col.Text(line.Confidence < CheckBelow ? [Text.Body, "text-destructive"] : [Text.Body],
                    $"{line.Quantity:0.##} {line.Unit} {line.Description} at {line.UnitPrice:0.00}");
            }

            // The model's confidence is its own estimate; a sum the code adds up is a fact
            var linesTotal = invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);

            if (Math.Abs(linesTotal - invoice.Total.Value) > 0.01m)
            {
                col.Text(["text-destructive text-sm"], $"The lines add up to {linesTotal:0.00}, not {invoice.Total.Value:0.00}. Check the total and the lines.");
            }

            col.Row(["gap-2"], content: row =>
            {
                // A real app writes the approved record to its database here, never before
                row.Button([Button.PrimaryMd], text: "Approve", onClick: async () =>
                {
                    _approved.Add(invoice);
                    _draft.Value = null;
                });
                row.Button([Button.OutlineErrorMd], text: "Discard", onClick: async () => _draft.Value = null);
            });
        });
    }

    private static void ReviewField(UIView view, string label, string value, double confidence, Action<string> onChange)
    {
        view.TextField(["default"], label: confidence < CheckBelow ? $"{label}: check this" : label, value: value, onValueChange: async text => onChange(text));
    }
    #endregion

    // A one-page invoice with three lines, written as a minimal PDF so the gallery needs no file
    private static byte[] SampleInvoice() => SampleDocuments.Pdf(
    [
        "BT /F1 20 Tf 72 720 Td (Mill & Grain Oy - Invoice 2026-0412) Tj ET\n" +
        "BT /F1 11 Tf 72 696 Td (Date 2026-10-02) Tj ET\n" +
        "BT /F1 11 Tf 72 650 Td (2 x 25 kg bag  Wheat flour T65  at 31.50) Tj ET\n" +
        "BT /F1 11 Tf 72 632 Td (1 x 10 kg block  Butter 82%  at 74.00) Tj ET\n" +
        "BT /F1 11 Tf 72 614 Td (3 x tray  Eggs, 30 pcs  at 9.80) Tj ET\n" +
        "BT /F1 13 Tf 72 580 Td (Total EUR 166.40) Tj ET\n",
    ]);
}
