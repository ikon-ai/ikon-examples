namespace Ikon.App.Examples.Emergence.UI;

/// <summary>
/// Visual styling information for structured tags
/// </summary>
public sealed record TagStyle(
    string Icon,
    string Title,
    string BgColor,
    string BorderColor,
    string TextColor
);

/// <summary>
/// Tag styles for the Emergence demo app
/// </summary>
public static class TagStyles
{
    public static TagStyle GetStyle(string tagName) => tagName.ToLowerInvariant() switch
    {
        "thinking" => new("brain", "Thinking", "bg-purple-500/10", "border-purple-500/30", "text-purple-400"),
        "assumptions" => new("list-checks", "Assumptions", "bg-blue-500/10", "border-blue-500/30", "text-blue-400"),
        "decision" => new("check-circle", "Decision", "bg-emerald-500/10", "border-emerald-500/30", "text-emerald-400"),
        "options" => new("git-branch", "Options", "bg-amber-500/10", "border-amber-500/30", "text-amber-400"),
        "question" => new("help-circle", "Question", "bg-cyan-500/10", "border-cyan-500/30", "text-cyan-400"),
        "code" => new("code", "Code", "bg-zinc-500/10", "border-zinc-500/30", "text-zinc-400"),
        _ => new("info", char.ToUpperInvariant(tagName[0]) + tagName[1..], "bg-zinc-500/10", "border-zinc-500/30", "text-zinc-400")
    };
}
