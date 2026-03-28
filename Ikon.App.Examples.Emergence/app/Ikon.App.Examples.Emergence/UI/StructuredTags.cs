using Ikon.AI.Emergence.Structured;

namespace Ikon.App.Examples.Emergence.UI;

/// <summary>
/// Structured tags helper for the Emergence demo app
/// </summary>
public static class StructuredTags
{
    public static readonly string[] ReasoningTags = ["thinking", "assumptions", "decision", "options"];

    public static StructuredTagParser.ParsedResponse ParseReasoning(string content)
    {
        return StructuredTagParser.Parse(content, ReasoningTags);
    }
}
