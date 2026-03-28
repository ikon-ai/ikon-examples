using Ikon.AI.Emergence.Structured;
using Ikon.App.Examples.Emergence.UI;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    private readonly Reactive<string> _tagsExampleContent = new(SampleTagContent);
    private readonly Reactive<bool> _tagsShowParsed = new(true);

    private const string SampleTagContent = """
        I'll analyze this task and provide my reasoning.

        <thinking>
        This is a complex task that requires careful consideration. The user wants to build a math tutor app for a child with ADHD. Key considerations include:
        - Short attention spans require bite-sized content
        - Immediate feedback is crucial for engagement
        - Gamification can help maintain interest
        </thinking>

        <assumptions>
        - Target age: Elementary school (6-11 years)
        - Focus on basic arithmetic operations
        - Single HTML file for easy deployment
        - Mobile-friendly responsive design needed
        </assumptions>

        <decision>
        I'll create a colorful, gamified math tutor with short sessions (5 problems), immediate rewards, and celebration animations.
        </decision>

        Now I'll start implementing the solution.
        """;

    private void RenderTagsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Header
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Structured Tag Parser");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Parse and render structured XML-style tags from LLM responses. Supports reasoning tags (thinking, assumptions, decision) and interactive tags (question, options).");

                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "What this demonstrates:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "Robust parsing of XML tags with case-insensitive matching, extraction of content, and styled rendering. Edit the content below to see live parsing results.");
                    });

                    // Toggle between raw and parsed view
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.Text([Text.Body], "View mode:");
                        view.Button(
                            [_tagsShowParsed.Value ? Button.PrimaryMd : Button.OutlineMd],
                            label: "Parsed",
                            onClick: async () => _tagsShowParsed.Value = true);
                        view.Button(
                            [!_tagsShowParsed.Value ? Button.PrimaryMd : Button.OutlineMd],
                            label: "Raw",
                            onClick: async () => _tagsShowParsed.Value = false);
                    });
                });
            });

            // Split view: editor and result
            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                // Editor panel
                view.Box([Card.Default, "p-4 flex-1 min-h-[500px]"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Input Content");
                    view.TextArea(
                        [Input.Default, "h-[400px] font-mono text-sm"],
                        value: _tagsExampleContent.Value,
                        onValueChange: async value => _tagsExampleContent.Value = value ?? "");
                });

                // Result panel
                view.Box([Card.Default, "p-4 flex-1 min-h-[500px]"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], _tagsShowParsed.Value ? "Parsed Result" : "Raw Content");

                    if (_tagsShowParsed.Value)
                    {
                        RenderParsedContent(view);
                    }
                    else
                    {
                        view.Box(["bg-zinc-900/50 p-4 rounded-lg font-mono text-sm whitespace-pre-wrap"], content: view =>
                        {
                            view.Text([Text.Body, "text-zinc-300"], _tagsExampleContent.Value);
                        });
                    }
                });
            });

            // Tag reference
            view.Box([Card.Default, "p-6 mt-4"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Supported Tags");

                view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
                {
                    RenderTagReference(view, "thinking", "Analysis and reasoning");
                    RenderTagReference(view, "assumptions", "Assumptions made");
                    RenderTagReference(view, "decision", "Final decision");
                    RenderTagReference(view, "options", "Options considered");
                    RenderTagReference(view, "question", "Questions for the user");
                });
            });
        });
    }

    private void RenderParsedContent(UIView view)
    {
        var content = _tagsExampleContent.Value;
        var parsed = StructuredTags.ParseReasoning(content);

        view.Column([Layout.Column.Md], content: view =>
        {
            // Plain text before/after tags
            if (!string.IsNullOrWhiteSpace(parsed.PlainText))
            {
                view.Box(["bg-zinc-900/30 p-4 rounded-lg mb-4"], content: view =>
                {
                    view.Text([Text.Caption, "text-muted-foreground mb-1"], "Plain Text");
                    view.Markdown([Text.Body, "text-zinc-200 prose-invert prose-sm max-w-none"], parsed.PlainText);
                });
            }

            // Parsed blocks
            if (parsed.Blocks.Count > 0)
            {
                view.Text([Text.Caption, "text-muted-foreground mb-2"], $"Found {parsed.Blocks.Count} structured block(s):");

                foreach (var block in parsed.Blocks)
                {
                    RenderTagBlock(view, block);
                }
            }
            else
            {
                view.Box(["bg-yellow-500/10 border border-yellow-500/20 p-4 rounded-lg"], content: view =>
                {
                    view.Text([Text.Body, "text-yellow-400"], "No structured tags found in the content.");
                });
            }
        });
    }

    private static void RenderTagBlock(UIView view, StructuredTagParser.ParsedBlock block)
    {
        var style = TagStyles.GetStyle(block.TagName);

        view.Box([
            $"rounded-xl p-4 mb-3 border {style.BgColor} {style.BorderColor}"
        ], content: card =>
        {
            card.Row(["items-center gap-2 mb-2"], content: header =>
            {
                header.Icon([$"w-4 h-4 {style.TextColor}"], name: style.Icon);
                header.Text([Text.BodyStrong, style.TextColor], style.Title);
                header.Text([Text.Caption, "text-muted-foreground ml-auto"], $"chars {block.StartIndex}-{block.EndIndex}");
            });
            card.Markdown([Text.Body, "text-zinc-300 prose-invert prose-sm max-w-none"], block.Content);
        });
    }

    private static void RenderTagReference(UIView view, string tagName, string description)
    {
        var style = TagStyles.GetStyle(tagName);

        view.Box([
            $"rounded-lg p-3 border {style.BgColor} {style.BorderColor} min-w-[200px]"
        ], content: box =>
        {
            box.Row(["items-center gap-2 mb-1"], content: header =>
            {
                header.Icon([$"w-4 h-4 {style.TextColor}"], name: style.Icon);
                header.Text([Text.BodyStrong, style.TextColor], $"<{tagName}>");
            });
            box.Text([Text.Caption, "text-muted-foreground"], description);
        });
    }
}
