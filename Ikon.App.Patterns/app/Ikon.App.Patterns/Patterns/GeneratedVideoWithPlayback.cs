namespace Ikon.App.Patterns.Patterns;

// Pattern: generated-video-with-playback — see docs/patterns/generated-video-with-playback.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class GeneratedVideoWithPlayback : IPatternDemo
{
    public string Slug => "generated-video-with-playback";
    public string Title => "Generated video, played back";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-generated-video-with-playback
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
    #endregion
}
