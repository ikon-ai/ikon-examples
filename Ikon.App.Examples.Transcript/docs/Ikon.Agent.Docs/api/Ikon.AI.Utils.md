namespace Ikon.AI.Utils
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Caps both dimensions at maxDimension (aspect preserved) and re-encodes as JPEG; returns the source bytes unchanged when the image already fits and is at most maxBytes.
    static (byte[] Bytes, string MimeType, int Width, int Height) EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static (int width, int height) GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)
    static bool IsWebP(byte[] data)
