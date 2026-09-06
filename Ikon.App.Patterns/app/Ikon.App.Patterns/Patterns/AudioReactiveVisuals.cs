namespace Ikon.App.Patterns.Patterns;

// Pattern: audio-reactive-visuals — see docs/patterns/audio-reactive-visuals.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class AudioReactiveVisuals : IPatternDemo
{
    public string Slug => "audio-reactive-visuals";
    public string Title => "Visuals that move with the audio";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio => throw new NotImplementedException();

    #region docsnippet:pattern-audio-reactive-visuals
    // One analyzer instance for the app, not one per utterance: it declares the shape set once
    // and the client matches frames to that declaration by SetId.
    private readonly VisemeAnalyzer _visemes = new();

    /// <summary>
    /// Attaching an analyzer is the whole server side. The shape values ride ALONG with the audio
    /// in each PcmAudioFrame -- AnalysisResults for the per-frame numbers, ShapeSetDeclarations
    /// for what those numbers mean -- so the visual stays in sync with playback for free. The
    /// server never sees them; a custom client component reads them off the frame.
    /// </summary>
    private async Task SpeakWithLipSyncAsync(string text)
    {
        await Audio.SpeakAsync(MediaTargets.Everyone, text, analyzers: [_visemes]);
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                onClick: async () => await SpeakWithLipSyncAsync("Hello there."),
                content: v => v.Text(text: "Speak"));

            // The avatar/visualizer is a custom node: it subscribes to the audio frames and reads
            // AnalysisResults itself. Driving it from server-side Reactive state instead would
            // arrive a frame late and flood the channel.
            col.AddNode("app.lipSyncAvatar", props: new Dictionary<string, object>
            {
                ["shapeSet"] = _visemes.ShapeSetDeclaration.Name,
            });
        });
    }
    #endregion
}
