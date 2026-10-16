namespace Ikon.App.Patterns.Patterns;

// Sample files the demos open in place of an upload, since the headless render has no upload handler
internal static class SampleDocuments
{
    // A minimal PDF: one page per content stream, Letter size, Helvetica as /F1
    public static byte[] Pdf(IReadOnlyList<string> pageContents)
    {
        var fontObject = 3 + pageContents.Count * 2;
        var kids = string.Join(" ", Enumerable.Range(0, pageContents.Count).Select(i => $"{3 + i * 2} 0 R"));

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{kids}] /Count {pageContents.Count} >>",
        };

        for (var i = 0; i < pageContents.Count; i++)
        {
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObject} 0 R >> >> /Contents {4 + i * 2} 0 R >>");
            objects.Add($"<< /Length {pageContents[i].Length} >>\nstream\n{pageContents[i]}endstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            pdf.Append($"{offset:D10} 00000 n \n");
        }

        pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
