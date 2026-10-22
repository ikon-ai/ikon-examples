namespace Ikon.App.Patterns.Patterns;

// Pattern: link-with-copy — see docs/patterns/link-with-copy.md.
// The link below stands in for an address the app made.
internal sealed class LinkWithCopy : IPatternDemo
{
    public string Slug => "link-with-copy";
    public string Title => "Link with a copy button";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => RenderLink((UIView)view, "https://example.ikon.live/?invite=3f9c1a7e5b2d4c86a0e1f2b3c4d5e6f7", "D4K-92X");

    #region example:pattern-link-with-copy
    private void RenderLink(UIView view, string inviteLink, string joinCode)
    {
        view.Column(["gap-4 w-full max-w-md"], content: col =>
        {
            col.CopyField(inviteLink, label: "Invite link");

            // A short code is easier to read whole and in larger text, so it turns off the two-line clamp
            col.CopyField(joinCode, label: "Join code", valueStyle: ["default", "text-base tracking-widest"]);
        });
    }
    #endregion
}
