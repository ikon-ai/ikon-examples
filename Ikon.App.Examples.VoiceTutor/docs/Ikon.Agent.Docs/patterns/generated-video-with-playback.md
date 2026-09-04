<!-- mined-from: Ikon.App.Patterns -->
# Generated Video With Playback — A URL, Not Bytes

`VideoGeneratorResult` carries a `Url` and nothing else. Generated video is far too large to hold
in app memory, so the result is always something the player streams — which also means the video
never passes through the app's own memory on its way to the screen.

Capability varies by model more than for any other generator. Ask the instance
(`SupportsAudio`, `SupportsImageToVideo`, `SupportedLengths`, `SupportedResolutions`) rather than
discovering a limit as a provider error.

## When to use

Any AI-generated clip: a scene, an animation, a product shot, a background loop. For playback of a
video the app did not generate, the same `VideoUrlPlayer` applies — this pattern is the generation
half plus the display that goes with it.

## Notes

- **`GenerateAudio` is a `bool?` on `VideoGeneratorConfig`**, and only some models honour it. Leave
  it null when the model does not support audio rather than passing `false`.
- `AspectRatio` on the config is `VideoGeneratorAspectRatio` (`Ratio16x9`, `Ratio9x16`, …) — the
  enum, not a string or a double. The `view.AspectRatio` layout component takes a `double` ratio;
  the two are unrelated despite the shared word.
- Wrap the player in `view.AspectRatio` so the box is reserved before the first frame and the
  layout does not jump. Match it to the ratio the video was generated at.
- `playsInline: true` matters on mobile Safari, which otherwise takes the video fullscreen on play.
- `VideoEnhancer` upscales an existing video **by URL** (`EnhanceAsync(videoUrl)`), so a generated
  clip can be fed straight in without ever being downloaded.
- The static one-shots (`VideoGenerator.GenerateAsync(prompt)`,
  `VideoEnhancer.EnhanceAsync(url)`) construct and dispose per call — right for a single clip, and
  the constructor plus config form is for everything else.
- Generation is slow enough that the busy state is not optional; render waiting, failed-with-retry
  and empty, and never show the raw exception.

## Snippet

```csharp
private readonly Reactive<string?> _videoUrl = new(null);
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _error = new(null);

private async Task GenerateAsync(string prompt)
{
    if (_busy.Value)
    {
        return;
    }

    _error.Value = null;
    using var _ = _busy.AsToken();

    try
    {
        using var generator = new VideoGenerator(VideoGeneratorModel.Veo31Fast);

        // Not every model does everything: ask the instance before requesting audio or
        // image-to-video, rather than discovering it as a provider error.
        var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
        {
            Prompt = prompt,
            AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
            Length = 8,
            GenerateAudio = generator.SupportsAudio ? true : null,
        });

        // A video result is a URL, never bytes -- generated video is far too big to hold in
        // app memory, and the player streams it.
        _videoUrl.Value = result.Url;
    }
    catch (AIException)
    {
        _error.Value = "Couldn't create the video — try again.";
    }
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        col.Button(
            disabled: _busy.Value,
            onClick: async () => await GenerateAsync("a paper boat on a still lake at dawn"),
            content: v => v.Text(text: _busy.Value ? "Generating…" : "Generate"));

        if (_error.Value is { } error)
        {
            col.Text(["text-destructive text-sm"], text: error);
        }

        if (_videoUrl.Value is { } url)
        {
            // Reserve the box before playback so the layout does not jump when the first
            // frame arrives, and keep the aspect the video was generated at.
            col.AspectRatio(ratio: 16.0 / 9.0, content: box =>
                box.VideoUrlPlayer(["w-full rounded-lg"], url: url, controls: true, playsInline: true));
        }
    });
}
```

## See also

- `generated-image-with-result` — the image equivalent, where the result may be bytes or a URL.
- `busy-flag-loading` — the guard around a long generation.
