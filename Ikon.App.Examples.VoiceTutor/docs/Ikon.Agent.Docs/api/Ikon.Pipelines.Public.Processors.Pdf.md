namespace Ikon.Pipelines.Public.Processors.Pdf
  interface IPdfDocument : IDisposable
    int PageCount { get; }
    IPdfPage GetPage(int index)
  interface IPdfPage : IDisposable
    double Height { get; }
    int Index { get; }
    double Width { get; }
    void CreateCopy(Stream output)
    // Renders the page scaled so its longest side is at most maxDimension pixels, preserving aspect ratio. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int maxDimension)
    // Renders the page at the exact width by height pixel size. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int width, int height, bool hasAlpha)
    string GetText()
  static class PdfDocument
    static IPdfDocument Load(byte[] bytes, string? password = null)
