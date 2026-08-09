<!-- mined-from: Sensei -->
# Three-Pane Desktop Layout — Site / Canvas / Library

A full-height row with a fixed-width left panel (context/inputs), a flexible center (work area), and a fixed-width right panel (settings/library). Each side panel has a 1px border on its inside edge and `shrink-0` so the center collapses first when the viewport narrows.

## When to use

Pro-tool style apps (architectural visualizers, image studios, IDE-like editors) where the user wants persistent context on the left, persistent stylistic / library controls on the right, and a large workspace in the middle. Choose this over a top tab bar when the side panels should always be visible, not behind a tab.

## Snippet

```csharp
private void RenderMain(UIView view)
{
    view.Row(["w-full items-stretch min-h-[calc(100vh-72px)]"], content: view =>
    {
        view.Column([
            "w-[300px] shrink-0 px-6 py-6 gap-5",
            "border-r border-[#1a1a18]/10 theme-dark:border-[#e9e3d3]/10"
        ], content: RenderSitePanel);

        view.Column(["flex-1 min-w-0 px-8 py-6 gap-5"], content: RenderCenter);

        view.Column([
            "w-[340px] shrink-0 px-6 py-6 gap-5",
            "border-l border-[#1a1a18]/10 theme-dark:border-[#e9e3d3]/10"
        ], content: RenderLibrary);
    });
}

private void RenderCenter(UIView view)
{
    view.Row(["gap-1"], content: view =>
    {
        ModeTab(view, InputMode.Rhino, "Rhino", "rhino model, the controllable input");
        ModeTab(view, InputMode.Sketch, "Sketch", "a scanned drawing, a phone photo of a sketchbook page");
        ModeTab(view, InputMode.Model, "Physical", "a photo of cardboard, foam, clay, plaster on the table");
    });

    view.Row(["gap-6 items-stretch flex-1 min-h-[480px]"], content: view =>
    {
        view.Column(["flex-1 min-w-0 gap-2"], content: RenderInputPane);
        view.Column(["flex-1 min-w-0 gap-2"], content: RenderOutputPane);
    });
}
```

## Notes

- `shrink-0` on the side panels and `min-w-0` on the center is the magic combo: without `min-w-0`, flex children with text/images refuse to shrink below their intrinsic width.
- Use `min-h-[calc(100vh-72px)]` (or whatever your top-bar height is) to make the layout fill the viewport even when content is short.
- Borders on the inside edges (`border-r` on left, `border-l` on right) avoid double-borders at the seams.
- For mobile responsiveness, hide one side panel at small breakpoints with `hidden md:flex` rather than reflowing.

## See also

- `bottom-tab-bar-nav` — mobile-shell counterpart
