namespace Ikon.App.Patterns.Patterns;

// Pattern: push-to-talk-button — see docs/patterns/push-to-talk-button.md.
internal sealed class PushToTalkButton : IPatternDemo
{
    public string Slug => "push-to-talk-button";
    public string Title => "Push-to-talk button";
    public string Category => "Voice";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-push-to-talk-button
    private readonly ClientReactive<bool> _micBlocked = new(false);

    private void Render(IView view)
    {
        // `group` on the row is what lets the wave key off the button's own capture state.
        view.Row(["group items-center gap-3"], content: row =>
        {
            row.PushToTalkButton(
                text: "🎤",
                onPermissionChanged: async args =>
                {
                    _micBlocked.Value = args.State != MediaPermissionState.Granted;
                });

            row.Box([MicButton.WhileCapturing, "items-center"],
                content: box => box.AudioWave());

            if (_micBlocked.Value)
            {
                row.Text([Text.Caption, "text-error-primary"],
                    text: "Allow the microphone in your browser's site settings to talk");
            }
        });
    }
    #endregion
}
