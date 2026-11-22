public partial class Validation
{
    // The app fetches a provider's link itself instead of leaving it to the browser's player, so a link that is
    // dead, empty or serves an error page fails the card instead of showing an empty player
    private static readonly HttpClient _aiResultHttp = new() { Timeout = TimeSpan.FromSeconds(60) };

    // Enough of a file's start for SniffMediaType, including an ISO-BMFF ftyp box's first compatible brands
    private const int MediaHeadLength = 32;

    private sealed record RemoteMedia(string MediaType, long? Length);

    private static void RenderAIStatus(UIView view, Reactive<string?> status, string testId)
    {
        if (!string.IsNullOrEmpty(status.Value))
        {
            view.Text([Text.Caption, "mt-2"], status.Value, props: TestId(testId));
        }
    }

    // The battery reads a status as one whole line, so a provider error that spans several lines is joined into
    // one and cut to 140 characters
    private static string FailStatus(string reason)
    {
        var line = reason.ReplaceLineEndings(" ").Trim();
        return line.Length > 140 ? $"FAIL {line[..140]}..." : $"FAIL {line}";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture) + " MB";
        }

        return bytes >= 1024 ? $"{bytes / 1024} KB" : $"{bytes} bytes";
    }

    private static string FormatSeconds(double seconds) => seconds.ToString("F1", CultureInfo.InvariantCulture) + " s";

    // Silence from a decoder or provider still has a positive sample count, so the check uses the loudest sample
    private static float PeakOf(IReadOnlyList<float> samples)
    {
        var peak = 0f;

        foreach (var sample in samples)
        {
            peak = Math.Max(peak, Math.Abs(sample));
        }

        return peak;
    }

    private static bool HasMediaType(string? mimeType, string expectedPrefix) =>
        !string.IsNullOrEmpty(mimeType) && mimeType.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);

    // Providers often serve generated files as application/octet-stream, so the file type is read from the
    // file's first bytes when the Content-Type header does not give it
    private static string? SniffMediaType(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 12 && head[4] == 'f' && head[5] == 't' && head[6] == 'y' && head[7] == 'p')
        {
            return SniffIsoMediaType(head);
        }

        if (head.Length >= 4 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3)
        {
            return "video/webm";
        }

        if (head.Length >= 4 && head[0] == 'g' && head[1] == 'l' && head[2] == 'T' && head[3] == 'F')
        {
            return "model/gltf-binary";
        }

        if (head.Length >= 4 && head[0] == 0x89 && head[1] == 'P' && head[2] == 'N' && head[3] == 'G')
        {
            return "image/png";
        }

        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (head.Length >= 12 && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F')
        {
            if (head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
            {
                return "image/webp";
            }

            if (head[8] == 'W' && head[9] == 'A' && head[10] == 'V' && head[11] == 'E')
            {
                return "audio/wav";
            }
        }

        if (head.Length >= 3 && head[0] == 'I' && head[1] == 'D' && head[2] == '3')
        {
            return "audio/mpeg";
        }

        return null;
    }

    // MP4 shares its container with AVIF and HEIC images, which the ftyp box's major brand tells apart. An AVIF
    // file may carry the generic HEIF brand mif1 as its major brand and name avif only among the compatible ones
    private static string SniffIsoMediaType(ReadOnlySpan<byte> head)
    {
        var major = head.Slice(8, 4);

        if (major.SequenceEqual("avif"u8) || major.SequenceEqual("avis"u8))
        {
            return "image/avif";
        }

        if (major.SequenceEqual("heic"u8) || major.SequenceEqual("heix"u8) || major.SequenceEqual("mif1"u8) || major.SequenceEqual("msf1"u8))
        {
            var boxEnd = Math.Min(head.Length, (int)Math.Min(int.MaxValue, BinaryPrimitives.ReadUInt32BigEndian(head)));

            for (var offset = 16; offset + 4 <= boxEnd; offset += 4)
            {
                var brand = head.Slice(offset, 4);

                if (brand.SequenceEqual("avif"u8) || brand.SequenceEqual("avis"u8))
                {
                    return "image/avif";
                }
            }

            return "image/heic";
        }

        return "video/mp4";
    }

    /// <summary>Fetches the headers and first bytes of a provider's result link and checks it serves a non-empty file of the expected type.</summary>
    /// <remarks>A null <paramref name="expectedTypePrefix"/> accepts any type, for formats servers label inconsistently (OBJ, FBX).
    /// A data URL, which the AI mocks return, is decoded in place, since HttpClient cannot fetch one.</remarks>
    /// <returns>The media type and declared length, or the failure as text.</returns>
    private static async Task<(RemoteMedia? Media, string? Failure)> ProbeRemoteMediaAsync(string? url, string? expectedTypePrefix)
    {
        if (string.IsNullOrEmpty(url))
        {
            return (null, "no URL was returned");
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return CheckDataUrlMedia(url, expectedTypePrefix);
        }

        try
        {
            using var response = await _aiResultHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"the URL returned HTTP {(int)response.StatusCode}");
            }

            var headerType = response.Content.Headers.ContentType?.MediaType;
            var length = response.Content.Headers.ContentLength;

            if (length == 0)
            {
                return (null, "the URL serves an empty file");
            }

            var head = new byte[MediaHeadLength];
            var read = 0;

            await using (var stream = await response.Content.ReadAsStreamAsync())
            {
                while (read < head.Length)
                {
                    var n = await stream.ReadAsync(head.AsMemory(read));

                    if (n == 0)
                    {
                        break;
                    }

                    read += n;
                }
            }

            if (read == 0)
            {
                return (null, "the URL serves an empty file");
            }

            return TypeRemoteMedia(headerType, length, head.AsSpan(0, read), expectedTypePrefix);
        }
        catch (Exception ex)
        {
            return (null, $"fetching the URL failed: {ex.Message}");
        }
    }

    private static (RemoteMedia? Media, string? Failure) CheckDataUrlMedia(string url, string? expectedTypePrefix)
    {
        var comma = url.IndexOf(',');
        var header = comma > 5 ? url[5..comma] : "";

        if (comma < 0 || !header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "the data URL is not base64 encoded");
        }

        byte[] data;

        try
        {
            data = Convert.FromBase64String(url[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return (null, "the data URL's base64 does not decode");
        }

        if (data.Length == 0)
        {
            return (null, "the data URL holds an empty file");
        }

        var declared = header.Split(';')[0];
        var head = data.AsSpan(0, Math.Min(MediaHeadLength, data.Length));
        return TypeRemoteMedia(declared.Length == 0 ? null : declared, data.Length, head, expectedTypePrefix);
    }

    private static (RemoteMedia? Media, string? Failure) TypeRemoteMedia(string? declaredType, long? length, ReadOnlySpan<byte> head, string? expectedTypePrefix)
    {
        var sniffed = SniffMediaType(head);

        if (expectedTypePrefix == null)
        {
            return (new RemoteMedia(sniffed ?? declaredType ?? "unknown type", length), null);
        }

        if (HasMediaType(declaredType, expectedTypePrefix))
        {
            return (new RemoteMedia(declaredType!, length), null);
        }

        if (HasMediaType(sniffed, expectedTypePrefix))
        {
            return (new RemoteMedia(sniffed!, length), null);
        }

        return (null, $"the URL serves {declaredType ?? "no content type"}, not {expectedTypePrefix}*");
    }

    private static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string Shorten(string text, int max)
    {
        var line = text.ReplaceLineEndings(" ").Trim();
        return line.Length > max ? line[..max] + "..." : line;
    }

    private static string CheckClassification(ClassificationResult result)
    {
        if (result.Details.Count == 0)
        {
            return FailStatus("the classifier returned no categories");
        }

        foreach (var detail in result.Details)
        {
            if (double.IsNaN(detail.Score) ? !detail.IsFlagged : detail.Score is < 0 or > 1)
            {
                return FailStatus($"category {detail.Label} scored {detail.Score}, outside 0-1");
            }

            if (detail.IsFlagged && !result.IsFlagged)
            {
                return FailStatus($"category {detail.Label} is flagged but the overall result is not");
            }
        }

        return $"PASS Classified: flagged {result.IsFlagged}, {result.Details.Count} categories scored 0-1";
    }

    private static string CheckEmbedding(int count, float[] embedding, int declaredDimension)
    {
        if (count != 1)
        {
            return FailStatus($"one input gave {count} embeddings");
        }

        if (embedding.Length == 0)
        {
            return FailStatus("the embedding is empty");
        }

        if (declaredDimension > 0 && embedding.Length != declaredDimension)
        {
            return FailStatus($"the embedding has {embedding.Length} dimensions but the model declares {declaredDimension}");
        }

        var sumOfSquares = 0.0;

        foreach (var value in embedding)
        {
            if (!float.IsFinite(value))
            {
                return FailStatus("the embedding holds a value that is not finite");
            }

            sumOfSquares += (double)value * value;
        }

        if (sumOfSquares == 0)
        {
            return FailStatus("the embedding is all zeros");
        }

        return $"PASS Embedded: {embedding.Length} dimensions as declared, finite, norm {Math.Sqrt(sumOfSquares).ToString("F2", CultureInfo.InvariantCulture)}";
    }

    private static string CheckWebSearch(List<SearchResult> results, int maxResults)
    {
        if (results.Count == 0)
        {
            return FailStatus("the search returned no results");
        }

        if (results.Count > maxResults)
        {
            return FailStatus($"the search returned {results.Count} results, more than the {maxResults} asked for");
        }

        for (var i = 0; i < results.Count; i++)
        {
            if (!Uri.TryCreate(results[i].Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return FailStatus($"result {i + 1} has no http(s) URL ('{Shorten(results[i].Url, 60)}')");
            }
        }

        return $"PASS Found {results.Count} results (max {maxResults}), each with an http(s) URL";
    }

    private static string CheckScrapedPage(PageResult page)
    {
        if (!string.IsNullOrEmpty(page.Error))
        {
            return FailStatus($"the page could not be fetched: {page.Error}");
        }

        if (string.IsNullOrWhiteSpace(page.Content))
        {
            return FailStatus("the scraped page has no content");
        }

        var title = string.IsNullOrWhiteSpace(page.Title) ? "no title" : $"'{Shorten(page.Title, 50)}'";
        return $"PASS Scraped: {title}, {page.Content.Length} characters";
    }

    private static string CheckRerank(List<RerankItem> items, int documentCount)
    {
        if (items.Count != documentCount)
        {
            return FailStatus($"the reranker returned {items.Count} of {documentCount} documents");
        }

        var seen = new HashSet<int>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item.Index < 0 || item.Index >= documentCount || !seen.Add(item.Index))
            {
                return FailStatus($"item {i + 1} names document index {item.Index}, out of range or repeated");
            }

            if (!double.IsFinite(item.Score))
            {
                return FailStatus($"document {item.Index + 1} scored {item.Score}");
            }

            if (i > 0 && item.Score > items[i - 1].Score)
            {
                return FailStatus("the documents are not ordered by score");
            }
        }

        return $"PASS Reranked {documentCount} documents, each once, ordered by score; top is #{items[0].Index + 1} at {items[0].Score.ToString("F3", CultureInfo.InvariantCulture)}";
    }

    private static string CheckGeneratedImages(List<(ImageGeneratorResult Result, byte[] Data)> images, int requestedCount)
    {
        if (images.Count != requestedCount)
        {
            return FailStatus($"asked for {requestedCount} image(s), got {images.Count}");
        }

        long totalBytes = 0;
        var (firstWidth, firstHeight) = (0, 0);

        foreach (var (result, data) in images)
        {
            var (failure, width, height) = CheckImageBytes(result.MimeType, result.Width, result.Height, data);

            if (failure != null)
            {
                return FailStatus(failure);
            }

            if (totalBytes == 0)
            {
                (firstWidth, firstHeight) = (width, height);
            }

            totalBytes += data.Length;
        }

        var first = images[0].Result;
        return $"PASS Generated {images.Count} image{(images.Count == 1 ? "" : "s")}: {first.MimeType} {firstWidth}x{firstHeight}, {FormatSize(totalBytes)}";
    }

    // The declared type has to match the bytes, since a mislabelled image breaks every consumer that trusts the label
    private static (string? Failure, int Width, int Height) CheckImageBytes(string mimeType, int reportedWidth, int reportedHeight, byte[] data)
    {
        if (data.Length == 0)
        {
            return ("the image has no bytes", 0, 0);
        }

        if (!HasMediaType(mimeType, "image/"))
        {
            return ($"the image is labelled '{mimeType}', not image/*", 0, 0);
        }

        var (width, height) = MeasureImage(reportedWidth, reportedHeight, data);

        if (width <= 0 || height <= 0)
        {
            return ($"the image measures {reportedWidth}x{reportedHeight} and its bytes give no size", 0, 0);
        }

        var sniffed = SniffMediaType(data.AsSpan(0, Math.Min(MediaHeadLength, data.Length)));
        var declared = mimeType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : mimeType;

        if (sniffed != null && !sniffed.Equals(declared, StringComparison.OrdinalIgnoreCase))
        {
            return ($"the image is labelled {mimeType} but its bytes are {sniffed}", 0, 0);
        }

        return (null, width, height);
    }

    // fal's adapters report 0x0 when the provider leaves the size out, so the size is then read from the image itself
    private static (int Width, int Height) MeasureImage(int reportedWidth, int reportedHeight, byte[] data)
    {
        if ((reportedWidth > 0 && reportedHeight > 0) || data.Length == 0)
        {
            return (reportedWidth, reportedHeight);
        }

        try
        {
            return ImageUtils.GetImageDimensions(data);
        }
        catch (Exception)
        {
            // Bytes the decoder cannot read keep the reported size, which the caller then fails on
            return (reportedWidth, reportedHeight);
        }
    }

    private static string CheckAudioFile(string what, string mimeType, int byteCount, double durationSeconds)
    {
        if (byteCount == 0)
        {
            return FailStatus($"the {what} file has no bytes");
        }

        if (!HasMediaType(mimeType, "audio/"))
        {
            return FailStatus($"the {what} file is labelled '{mimeType}', not audio/*");
        }

        if (!(durationSeconds > 0))
        {
            return FailStatus($"the {what} file reports a duration of {durationSeconds} s");
        }

        return $"PASS Generated {what}: {mimeType}, {FormatSeconds(durationSeconds)}, {FormatSize(byteCount)}";
    }

    private static string CheckStreamedAudio(string what, IReadOnlyList<float> samples, int sampleRate, int channelCount)
    {
        if (samples.Count == 0)
        {
            return FailStatus($"the {what} stream had no samples");
        }

        if (sampleRate <= 0 || channelCount <= 0)
        {
            return FailStatus($"the {what} stream reports {sampleRate} Hz, {channelCount} channel(s)");
        }

        var peak = PeakOf(samples);

        if (!float.IsFinite(peak))
        {
            return FailStatus($"the {what} stream holds a sample that is not finite");
        }

        if (peak < 0.001f)
        {
            return FailStatus($"the {what} stream is silent (peak {peak.ToString("F4", CultureInfo.InvariantCulture)})");
        }

        var seconds = (double)samples.Count / sampleRate / channelCount;
        return $"PASS Generated {what}: {FormatSeconds(seconds)} at {sampleRate} Hz, {channelCount} ch, peak {peak.ToString("F2", CultureInfo.InvariantCulture)}";
    }

    private static string CheckTranscript(Transcript transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript.Text) || !transcript.Text.Any(char.IsLetter))
        {
            return FailStatus("no speech was recognized");
        }

        return $"PASS Recognized: {CountWords(transcript.Text)} words, {transcript.Text.Trim().Length} characters";
    }

    private static string CheckOCR(OCRResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Text) || !result.Text.Any(char.IsLetterOrDigit))
        {
            return FailStatus("no text was extracted");
        }

        return $"PASS Extracted: {CountWords(result.Text)} words, {result.Text.Length} characters, {result.Pages.Count} page(s)";
    }

    private static string CheckConvertedPdf(byte[] pdf)
    {
        // A PDF ends with %%EOF, so if the last kilobyte has none, the download was cut short
        var tail = pdf.AsSpan(Math.Max(0, pdf.Length - 1024));

        if (tail.IndexOf("%%EOF"u8) < 0)
        {
            return FailStatus($"the PDF ({FormatSize(pdf.Length)}) has no %%EOF trailer");
        }

        return $"PASS Converted to PDF: {FormatSize(pdf.Length)}, %PDF header and %%EOF trailer";
    }

    private static async Task<string> CheckGeneratedVideoAsync(string? url, int lengthSeconds, VideoGeneratorResolution resolution)
    {
        var (media, failure) = await ProbeRemoteMediaAsync(url, "video/");

        if (media == null)
        {
            return FailStatus($"generated video: {failure}");
        }

        var size = media.Length is { } length ? FormatSize(length) : "size not sent";
        return $"PASS Generated video: {media.MediaType}, {size} (requested {lengthSeconds} s, {resolution.ToString().Replace("Resolution", "")})";
    }

    private static async Task<string> CheckEnhancedVideoAsync(VideoEnhancerResult result)
    {
        if (result.OutputSizeBytes is <= 0)
        {
            return FailStatus($"the enhancer reports an output of {result.OutputSizeBytes} bytes");
        }

        if (result.OutputFps is <= 0)
        {
            return FailStatus($"the enhancer reports {result.OutputFps} fps");
        }

        if (result.OutputWidth is <= 0 || result.OutputHeight is <= 0)
        {
            return FailStatus($"the enhancer reports {result.OutputWidth}x{result.OutputHeight}");
        }

        var (media, failure) = await ProbeRemoteMediaAsync(result.Url, "video/");

        if (media == null)
        {
            return FailStatus($"enhanced video: {failure}");
        }

        var parts = new List<string> { media.MediaType };

        if ((media.Length ?? result.OutputSizeBytes) is { } bytes)
        {
            parts.Add(FormatSize(bytes));
        }

        if (result.OutputFps is { } fps)
        {
            parts.Add($"{fps} fps");
        }

        if (result.OutputWidth is { } width && result.OutputHeight is { } height)
        {
            parts.Add($"{width}x{height}");
        }

        return $"PASS Enhanced video: {string.Join(", ", parts)}";
    }

    private static async Task<string> CheckGeneratedMeshAsync(MeshGeneratorResult result)
    {
        (string Format, string? Url, string? TypePrefix)[] candidates =
        [
            ("GLB", result.GlbUrl, "model/gltf"),
            ("FBX", result.FbxUrl, null),
            ("OBJ", result.ObjUrl, null),
            ("USDZ", result.UsdzUrl, null),
            ("STL", result.StlUrl, null)
        ];

        var (format, url, typePrefix) = candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c.Url));

        if (url == null)
        {
            return FailStatus("the mesh result has no model file URL");
        }

        var (media, failure) = await ProbeRemoteMediaAsync(url, typePrefix);

        if (media == null)
        {
            return FailStatus($"{format} file: {failure}");
        }

        var size = media.Length is { } length ? FormatSize(length) : "size not sent";
        return $"PASS Generated mesh: {format} {media.MediaType}, {size}, {result.Files.Count} file(s)";
    }

    private static string CheckSegments(ImageSegmenterResult result, List<(OutputImage Mask, byte[] Data)> masks)
    {
        if (result.Segments.Count == 0)
        {
            return FailStatus("no segment was found");
        }

        var (firstWidth, firstHeight) = (0, 0);

        for (var i = 0; i < result.Segments.Count; i++)
        {
            var segment = result.Segments[i];
            var (mask, data) = masks[i];
            var (width, height) = MeasureImage(mask.Width, mask.Height, data);

            if (data.Length == 0 || !HasMediaType(mask.MimeType, "image/") || width <= 0 || height <= 0)
            {
                return FailStatus($"segment {i + 1} mask is not a usable image: {data.Length} bytes, type '{mask.MimeType}', {width}x{height}");
            }

            if (i == 0)
            {
                (firstWidth, firstHeight) = (width, height);
            }

            if (segment.Score is { } score && (double.IsNaN(score) || score < 0 || score > 1))
            {
                return FailStatus($"segment {i + 1} scored {score}, outside 0-1");
            }

            // Box values are fractions of the image size. The 0.01 margin allows for a provider's rounding at the edges
            if (segment.Box.Count != 0 && (segment.Box.Count != 4 || segment.Box.Any(v => double.IsNaN(v) || v < -0.01 || v > 1.01)))
            {
                return FailStatus($"segment {i + 1} box [{string.Join(", ", segment.Box)}] is not four fractions of the image");
            }
        }

        var first = masks[0].Mask;
        return $"PASS Segmented: {result.Segments.Count} segment(s), masks {first.MimeType} {firstWidth}x{firstHeight}, scores 0-1";
    }

    private static string CheckUpscaledImage(int inputWidth, int inputHeight, OutputImage image, byte[] data)
    {
        var (failure, width, height) = CheckImageBytes(image.MimeType, image.Width, image.Height, data);

        if (failure != null)
        {
            return FailStatus(failure);
        }

        if (width <= inputWidth || height <= inputHeight)
        {
            return FailStatus($"the output {width}x{height} is not larger than the input {inputWidth}x{inputHeight}");
        }

        // Upscalers round each side to their own block size, which changes the aspect ratio by a few percent at most
        var inputAspect = (double)inputWidth / inputHeight;
        var outputAspect = (double)width / height;

        if (Math.Abs(outputAspect - inputAspect) / inputAspect > 0.05)
        {
            return FailStatus($"the output {width}x{height} does not keep the aspect ratio of the input {inputWidth}x{inputHeight}");
        }

        var factor = ((double)width / inputWidth).ToString("F1", CultureInfo.InvariantCulture);
        return $"PASS Upscaled {inputWidth}x{inputHeight} to {width}x{height} ({factor}x): {image.MimeType}, {FormatSize(data.Length)}";
    }

    private static string CheckDepthMap(OutputImage depth, byte[] data)
    {
        var (failure, width, height) = CheckImageBytes(depth.MimeType, depth.Width, depth.Height, data);

        if (failure != null)
        {
            return FailStatus(failure);
        }

        return $"PASS Depth map: {depth.MimeType} {width}x{height}, {FormatSize(data.Length)}";
    }

    private static string CheckVideoDescription(string description)
    {
        var words = CountWords(description);

        if (words < 3)
        {
            return FailStatus($"the description has {words} word(s): '{Shorten(description, 60)}'");
        }

        return $"PASS Described the clip: {words} words, {description.Trim().Length} characters";
    }
}
