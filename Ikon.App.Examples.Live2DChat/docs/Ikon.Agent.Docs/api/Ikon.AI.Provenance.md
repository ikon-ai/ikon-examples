namespace Ikon.AI.Provenance
  // The platform's EU AI Act Article 50 marking, applied identically for every provider. Three layers behind one call: an XMP metadata mark (always; IPTC DigitalSourceType=trainedAlgorithmicMedia), an imperceptible tiled pixel watermark (default on; detectable via MeasureInvisibleMark), and an optional visible corner badge. PNG and JPEG take all three; WebP takes the metadata mark alone; any other encoding passes through untouched. Ask GetMarkingSupport rather than assuming. Streamed media (WebRTC, TTS) is out of scope by design — disclosure there is interaction-level.
  static class ImageProvenance
    static byte[] Apply(byte[] data, string model, bool invisibleWatermark = true, string visibleWatermark = "")
    static ProvenanceMarking GetMarkingSupport(byte[] data)
    // At or above DetectionThreshold the image carries Ikon's mark; unmarked images score near zero.
    static double MeasureInvisibleMark(byte[] data)
    static string? ReadMetadataMark(byte[] data)
    // Scores are normal-deviates: an unmarked image scores |z| ≲ 3, a marked one scores in the tens to hundreds depending on size and recompression.
    const double DetectionThreshold = 12.0
  enum ProvenanceMarking
    None
    // Machine-readable and standards-compliant, but strippable by anything that rewrites the file's metadata.
    MetadataOnly
    // The pixel watermark survives a re-encode.
    Full
