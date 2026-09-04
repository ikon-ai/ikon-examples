namespace Ikon.AI.Emergence.Structured
  // Tag matching is case-insensitive and tolerates attributes and formatting variations.
  static class StructuredTagParser
    // Returns the first occurrence's inner content, or null if the tag is absent.
    static string? GetTagContent(string content, string tagName)
    static bool HasTag(string content, string tagName)
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)
  sealed record StructuredTagParser.ParsedBlock
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  sealed record StructuredTagParser.ParsedResponse
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get; init; }
    string PlainText { get; init; }
