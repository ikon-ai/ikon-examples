<!-- mined-from: QTribunal -->
# Command-Verb Input With Hint Chips — Text-Adventure Style

A single `TextField` accepts free-form `verb argument` input (`examine the witness`, `propose the universe is unfair`). A row of small hint chips below pre-fills the verb (`examine `, `ask `, `reflect `, `look `, `propose `) so the user can discover commands without memorizing them. A static `ParseCommand` switch maps the leading word — including a one-letter alias — to a `CommandType` enum.

## When to use

Apps where the user is expected to type natural-ish commands (text adventures, agent CLIs, scripting consoles, search bars with leading operators). The chip row is essential — a verb-driven input without affordance for "what verbs work?" is a cliff. Avoid for free-text chat.

## Snippet

```csharp
public enum CommandType { Examine, Ask, Reflect, Look, Propose }
public record ParsedCommand(CommandType Type, string Argument);

private void RenderInput(UIView view)
{
    var placeholder = _phase.Value == GamePhase.Proposal
        ? "propose [your theory about the hidden law]"
        : "examine, ask, reflect, look, or propose...";

    view.Column(style: [Styles.Input.Container], content: view =>
    {
        view.Row(style: [Styles.Input.Row], content: view =>
        {
            view.TextField(
                style: [Styles.Input.Field],
                value: _inputText.Value,
                placeholder: placeholder,
                onValueChange: value => { _inputText.Value = value; return Task.CompletedTask; },
                onSubmit: async submitted => SubmitCommand(submitted));

            var canSend = !_isProcessing.Value && !string.IsNullOrWhiteSpace(_inputText.Value);
            view.Button(
                style: [canSend ? Styles.Input.SendButton : Styles.Input.SendButtonDisabled],
                text: _isProcessing.Value ? "..." : "Send",
                disabled: !canSend,
                onClick: async () => SubmitCommand());
        });

        if (_phase.Value == GamePhase.Investigation)
        {
            view.Row(style: [Styles.Input.HintRow], content: view =>
            {
                string[] hints = ["examine", "ask", "reflect", "look", "propose"];
                foreach (var hint in hints)
                {
                    view.Button(
                        style: [Styles.Input.HintButton],
                        text: hint,
                        onClick: async () => _inputText.Value = hint + " ");
                }
            });
        }
    });
}

private static ParsedCommand? ParseCommand(string input)
{
    var trimmed = input.Trim();
    var spaceIndex = trimmed.IndexOf(' ');
    var verb = spaceIndex > 0 ? trimmed[..spaceIndex].ToLowerInvariant() : trimmed.ToLowerInvariant();
    var argument = spaceIndex > 0 ? trimmed[(spaceIndex + 1)..].Trim() : "";

    return verb switch
    {
        "examine" or "x" => new ParsedCommand(CommandType.Examine, string.IsNullOrEmpty(argument) ? "surroundings" : argument),
        "ask" or "a"     => new ParsedCommand(CommandType.Ask, string.IsNullOrEmpty(argument) ? "the nearest witness" : argument),
        "reflect" or "r" => new ParsedCommand(CommandType.Reflect, string.IsNullOrEmpty(argument) ? "on what I've seen" : argument),
        "look" or "l"    => new ParsedCommand(CommandType.Look, string.IsNullOrEmpty(argument) ? "around" : argument),
        "propose" or "p" => new ParsedCommand(CommandType.Propose, argument),
        _ => null
    };
}
```

## Notes

- The hint button appends `verb + " "` (with trailing space) — the user lands ready to type the argument. Without the space, they have to delete-and-retype.
- One-letter aliases (`x`, `a`, `r`, `l`, `p`) are free with the `or` pattern in the switch — power users get fewer keystrokes, beginners still see full words.
- Default arguments (`"surroundings"`, `"the nearest witness"`) make bare verbs do the obvious thing instead of erroring.

## See also

- `command-palette-jump` — when commands are a closed enumerated set the user picks from, not types
- `quick-reply-options-from-llm` — when the suggestions are LLM-generated rather than static
