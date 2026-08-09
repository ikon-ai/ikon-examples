<!-- mined-from: Sensei -->
# Before/After Pane Comparison — Input vs Generated, Side-By-Side

A two-column row inside the work area: left pane shows the user's input image (Rhino render, sketch, photo) with a "replace / clear" affordance; right pane shows the AI-generated output with a download action. Both panes use the same square aspect frame, same ring/background, so visual diff is honest. The input pane's empty state is a dashed dropzone; the output pane renders nothing at all until a result exists.

## When to use

Image-to-image, before/after editing, AI restyling, code-vs-output, prompt-vs-render. Anytime users iterate by tweaking input and inspecting how the model responds — the spatial pairing makes the relationship obvious without a label.

## Snippet

```csharp
view.Row(["gap-6 items-stretch flex-1 min-h-[480px]"], content: view =>
{
    view.Column(["flex-1 min-w-0 gap-2"], content: RenderInputPane);
    view.Column(["flex-1 min-w-0 gap-2"], content: RenderOutputPane);
});

private void RenderInputPane(UIView view)
{
    FieldLabel(view, _mode.Value switch { InputMode.Rhino => "geometry · rhino", _ => "geometry" });

    if (_geometry.Value != null)
    {
        view.Box([
            "relative w-full aspect-square overflow-hidden rounded-sm",
            "bg-[#ece5d2] ring-1 ring-[#1a1a18]/15"
        ], content: v =>
        {
            v.Image(style: ["w-full h-full object-contain"],
                data: _geometry.Value!.Data,
                mimeType: _geometry.Value.Mime,
                alt: "input geometry");
        });

        view.Row(["gap-3 items-center"], content: v =>
        {
            v.FileUpload(
                accept: ["image/*"], multiple: false, maxFileSize: 25_000_000,
                onUploadComplete: HandleGeometryUpload,
                content: u => u.Text(["text-xs underline cursor-pointer opacity-80"], "replace"));

            v.Button(["text-xs underline opacity-80"],
                onClick: async () => { _geometry.Value = null; },
                content: b => b.Text(text: "clear"));
        });
    }
    else
    {
        view.FileUpload(
            accept: ["image/*"], multiple: false, maxFileSize: 25_000_000,
            onUploadComplete: HandleGeometryUpload,
            content: u =>
            {
                u.Box([
                    "w-full aspect-square cursor-pointer flex items-center justify-center",
                    "border border-dashed border-[#1a1a18]/30 hover:border-[#1a1a18] rounded-sm"
                ], content: b =>
                {
                    b.Column([Layout.Center, "gap-2 px-6 text-center"], content: c =>
                    {
                        c.Icon(["w-7 h-7 opacity-60"], name: "upload");
                        c.Text(["text-sm"], "drop a Rhino render or screenshot");
                    });
                });
            });
    }
}

private void RenderOutputPane(UIView view)
{
    FieldLabel(view, "rendered · realistic");

    if (_renderData.Value != null && _renderMime.Value != null)
    {
        view.Box([
            "relative w-full aspect-square overflow-hidden rounded-sm",
            "bg-[#ece5d2] ring-1 ring-[#1a1a18]/15"
        ], content: v =>
        {
            v.Image(style: ["w-full h-full object-contain"],
                data: _renderData.Value, mimeType: _renderMime.Value, alt: "rendered image");
        });

        view.Row(["gap-3 items-center"], content: v =>
        {
            v.ActionButton(["text-xs underline opacity-80"],
                action: ActionKind.DownloadFile,
                options: new DownloadFileActionOptions
                {
                    Filename = $"sensei-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png",
                    Data = _renderData.Value, MimeType = _renderMime.Value
                },
                content: b => b.Text(text: "download"));
        });
    }
}
```

## Notes

- Use `aspect-square overflow-hidden` on both panes so the layout is symmetric regardless of the image's true dimensions; `object-contain` keeps the actual image undistorted with letterboxing.
- `flex-1 min-w-0` on each column is required — without `min-w-0`, an oversized image won't allow its column to shrink and the layout breaks.
- Same ring/background tone on both panes signals "these are paired", different label colors (or italic on empty states) signal "input" vs "output" without forcing borders.

## See also

- `image-gallery` — for many outputs over time
- `inline-list-cell-edit` — when the input is text rather than an image
