<!-- mined-from: NoBrainer -->
# Avatar With Image-Or-Pulsing-Box Fallback

Show a circular avatar from `byte[]`/MIME data when present; otherwise show a pulsing neutral box of identical size so the layout never shifts when the image arrives. The pulse uses the Crosswind motion grammar so it's a single style line, no JS.

## When to use

You generate or fetch an avatar/logo asynchronously (LLM, file load, profile lookup) and want a graceful placeholder. Same pattern works for any "image arrives later" slot — onboarding hero, header brand mark, empty state illustrations.

## Snippet

```csharp
// Header avatar — image or animated placeholder
if (_avatarImage.Value != null && _avatarMime.Value != null)
{
    view.Image(["w-7 h-7 rounded-full object-cover"],
        data: _avatarImage.Value, mimeType: _avatarMime.Value, alt: "No-Brainer");
}
else
{
    view.Box(["w-7 h-7 rounded-full bg-black/[0.04]",
        "motion-[0:opacity-40%,50:opacity-70%,100:opacity-40%] motion-duration-4000ms motion-loop motion-ease-[ease-in-out]"]);
}

// Same shape, larger, used in onboarding hero
view.Column(["items-center gap-6 max-w-sm w-full px-8"], content: view =>
{
    if (_avatarImage.Value != null && _avatarMime.Value != null)
    {
        view.Image(["w-20 h-20 rounded-full object-cover"],
            data: _avatarImage.Value, mimeType: _avatarMime.Value, alt: "No-Brainer");
    }
    else
    {
        view.Box(["w-20 h-20 rounded-full bg-black/[0.04]",
            "motion-[0:opacity-40%,50:opacity-70%,100:opacity-40%] motion-duration-4000ms motion-loop motion-ease-[ease-in-out]"]);
    }

    view.Text(["text-xl font-light text-black/40"], "Hi. I'm No-Brainer.");
});

// Background generation (called from Main once)
private async Task GenerateAvatarAsync()
{
    var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
    var results = await generator.GenerateImageAsync(new ImageGeneratorConfig
    {
        Prompt = "A small, soft, slightly worn teddy bear-like creature...",
        Width = 256, Height = 256
    });

    if (results.Count > 0)
    {
        _avatarImage.Value = results[0].Data;
        _avatarMime.Value = results[0].MimeType;
    }
}
```

## Notes

- Store image as two reactives: `Reactive<byte[]?> _avatarImage` and `Reactive<string?> _avatarMime`. Both null = placeholder; both set = render. Don't pack into a record unless you need atomic transitions.
- Match the placeholder's size class exactly to avoid layout pop on arrival.
- The four-second pulse is gentle enough to feel "alive" without distracting; faster pulses read as loading and should be reserved for active operations.
- For multi-user apps, generate the avatar once (in `Main`) and put it on a `Reactive<T>` (shared) so all clients see the same face.

## See also

- `busy-flag-loading` — for active operations rather than passive presence
