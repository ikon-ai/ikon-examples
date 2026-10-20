namespace Ikon.App.Patterns.Patterns;

// Pattern: link-with-copy — see docs/patterns/link-with-copy.md.
// The link below stands in for an address the app made.
internal sealed class LinkWithCopy : IPatternDemo
{
    public string Slug => "link-with-copy";
    public string Title => "Link with a copy button";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => RenderLink((UIView)view, "Invite link", "https://example.ikon.live/?invite=3f9c1a7e5b2d4c86a0e1f2b3c4d5e6f7");

    #region example:pattern-link-with-copy
    private void RenderLink(UIView view, string label, string link)
    {
        view.Column(["gap-1 w-full max-w-md"], content: col =>
        {
            col.Text([Text.Label], label);

            col.Row(["gap-2 items-center w-full"], content: row =>
            {
                // min-w-0 lets the box shrink below the link's length, and break-all wraps the
                // unbroken address inside it instead of pushing the copy button off the screen
                row.Box(["flex-1 min-w-0 rounded-md border border-secondary bg-muted px-3 py-2"], content: field =>
                    field.Text([Text.Caption, "break-all line-clamp-2 select-all"], link));

                row.ActionButton([Button.OutlineMd, "shrink-0"], text: "Copy", ariaLabel: $"Copy {label}",
                    action: ActionKind.CopyToClipboard, options: new CopyToClipboardActionOptions { Text = link });
            });
        });
    }
    #endregion
}
