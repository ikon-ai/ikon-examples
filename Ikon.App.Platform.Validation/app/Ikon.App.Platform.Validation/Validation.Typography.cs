public partial class Validation
{
    private void RenderTypographySection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Text styles
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Text Styles");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Display], "Text.Display");
                    view.Text([Text.H1], "Text.H1");
                    view.Text([Text.H2], "Text.H2");
                    view.Text([Text.H3], "Text.H3");
                    view.Text([Text.Body], "Text.Body - Regular body text with relaxed line height.");
                    view.Text([Text.BodyStrong], "Text.BodyStrong - Bold body text.");
                    view.Text([Text.Caption], "Text.Caption - Small caption text");
                    view.Text([Text.Small], "Text.Small - Extra small text");
                    view.Text([Text.Muted], "Text.Muted - Muted text color");
                    view.Box([Text.Code], content: v => v.Text(text: "Text.Code - Monospace code style"));
                    view.Text([Text.Link], "Text.Link - Clickable link style");
                    view.Text([Text.Label], "Text.Label - Form label style");
                });
            });

            // Tone colors
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tone Colors");
                view.Text([Text.Caption, "mb-4"], "These tones can be applied to various components");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Tone.Primary, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Primary"));
                    view.Box([Tone.Secondary, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Secondary"));
                    view.Box([Tone.Muted, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Muted"));
                    view.Box([Tone.Success, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Success"));
                    view.Box([Tone.Warning, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Warning"));
                    view.Box([Tone.Danger, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Danger"));
                    view.Box([Tone.Info, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Info"));
                    view.Box([Tone.Ghost, "p-3 rounded-md border"], content: v => v.Text(text: "Tone.Ghost"));
                    view.Box([Tone.Outline, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Outline"));
                    view.Box([Tone.Link, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Link"));
                    view.Box([Tone.Subtle, "p-3 rounded-md"], content: v => v.Text(text: "Tone.Subtle"));
                });
            });

            // Layout utilities
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Layout Row Sizes");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Row.Xs (gap-1)");
                    view.Row([Layout.Row.Xs], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "A"));
                        view.Box([Badge.Default], content: v => v.Text(text: "B"));
                        view.Box([Badge.Default], content: v => v.Text(text: "C"));
                    });

                    view.Text([Text.Caption], "Row.Sm (gap-2)");
                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "A"));
                        view.Box([Badge.Default], content: v => v.Text(text: "B"));
                        view.Box([Badge.Default], content: v => v.Text(text: "C"));
                    });

                    view.Text([Text.Caption], "Row.Md (gap-3)");
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "A"));
                        view.Box([Badge.Default], content: v => v.Text(text: "B"));
                        view.Box([Badge.Default], content: v => v.Text(text: "C"));
                    });

                    view.Text([Text.Caption], "Row.Lg (gap-4)");
                    view.Row([Layout.Row.Lg], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "A"));
                        view.Box([Badge.Default], content: v => v.Text(text: "B"));
                        view.Box([Badge.Default], content: v => v.Text(text: "C"));
                    });

                    view.Text([Text.Caption], "Row.Xl (gap-6)");
                    view.Row([Layout.Row.Xl], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "A"));
                        view.Box([Badge.Default], content: v => v.Text(text: "B"));
                        view.Box([Badge.Default], content: v => v.Text(text: "C"));
                    });
                });
            });

            // Grid layouts
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Grid Layouts");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Grid.Cols2");
                    view.Box([Layout.Grid.Cols2], content: view =>
                    {
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "1"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "2"));
                    });

                    view.Text([Text.Caption], "Grid.Cols3");
                    view.Box([Layout.Grid.Cols3], content: view =>
                    {
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "1"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "2"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "3"));
                    });

                    view.Text([Text.Caption], "Grid.Cols4");
                    view.Box([Layout.Grid.Cols4], content: view =>
                    {
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "1"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "2"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "3"));
                        view.Box([Badge.Secondary, "justify-center"], content: v => v.Text(text: "4"));
                    });
                });
            });

            // Tokens - Radius
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tokens - Border Radius");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([Tone.Primary, Tokens.Radius.None, "p-4"], content: v => v.Text(text: "None"));
                    view.Box([Tone.Primary, Tokens.Radius.Sm, "p-4"], content: v => v.Text(text: "Sm"));
                    view.Box([Tone.Primary, Tokens.Radius.Md, "p-4"], content: v => v.Text(text: "Md"));
                    view.Box([Tone.Primary, Tokens.Radius.Lg, "p-4"], content: v => v.Text(text: "Lg"));
                    view.Box([Tone.Primary, Tokens.Radius.Xl, "p-4"], content: v => v.Text(text: "Xl"));
                    view.Box([Tone.Primary, Tokens.Radius.Xl2, "p-4"], content: v => v.Text(text: "2Xl"));
                    view.Box([Tone.Primary, Tokens.Radius.Full, "p-4"], content: v => v.Text(text: "Full"));
                });
            });

            // Tokens - Shadow
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tokens - Shadows");
                view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
                {
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.None], content: v => v.Text(text: "None"));
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.Sm], content: v => v.Text(text: "Sm"));
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.Md], content: v => v.Text(text: "Md"));
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.Lg], content: v => v.Text(text: "Lg"));
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.Xl], content: v => v.Text(text: "Xl"));
                    view.Box(["bg-card p-4 rounded-md", Tokens.Shadow.Xl2], content: v => v.Text(text: "2Xl"));
                });
            });

            // Icon Spinners
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Icon Spinners");
                view.Row([Layout.Row.Lg], content: view =>
                {
                    view.Column([Layout.Column.Sm, "items-center"], content: view =>
                    {
                        view.Box([Icon.SpinnerSm]);
                        view.Text([Text.Caption], "SpinnerSm");
                    });
                    view.Column([Layout.Column.Sm, "items-center"], content: view =>
                    {
                        view.Box([Icon.Spinner]);
                        view.Text([Text.Caption], "Spinner");
                    });
                    view.Column([Layout.Column.Sm, "items-center"], content: view =>
                    {
                        view.Box([Icon.SpinnerLg]);
                        view.Text([Text.Caption], "SpinnerLg");
                    });
                });
            });

            // Font Families
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Font Families");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box(["p-4 rounded-md bg-muted"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "font-sans (Default)");
                        view.Text(["font-sans text-lg"], "The quick brown fox jumps over the lazy dog. 0123456789");
                    });
                    view.Box(["p-4 rounded-md bg-muted"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "font-serif");
                        view.Text(["font-serif text-lg"], "The quick brown fox jumps over the lazy dog. 0123456789");
                    });
                    view.Box(["p-4 rounded-md bg-muted"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "font-mono");
                        view.Text(["font-mono text-lg"], "The quick brown fox jumps over the lazy dog. 0123456789");
                    });
                    view.Box(["p-4 rounded-md bg-muted"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "font-display");
                        view.Text(["font-display text-lg"], "The quick brown fox jumps over the lazy dog. 0123456789");
                    });
                });
            });

            // Markdown
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Markdown");
                view.Markdown(content: """
                    # Heading 1
                    ## Heading 2
                    ### Heading 3

                    This is a paragraph with **bold text**, *italic text*, and `inline code`.

                    Here's a [link to example](https://example.com) that opens in a new tab.

                    ### Lists

                    Unordered list:
                    - First item
                    - Second item
                    - Third item with **bold**

                    Ordered list:
                    1. First step
                    2. Second step
                    3. Third step

                    ### Code Block

                    ```
                    function hello() {
                        console.log("Hello, World!");
                    }
                    ```

                    ### Blockquote

                    > This is a blockquote. It can span multiple lines
                    > and contain **formatted** text.

                    ### Table

                    | Column 1 | Column 2 | Column 3 |
                    |----------|----------|----------|
                    | Cell 1   | Cell 2   | Cell 3   |
                    | Cell 4   | Cell 5   | Cell 6   |

                    ### Horizontal Rule

                    ---

                    That's all the markdown features!
                    """);
            });
        });
    }
}
