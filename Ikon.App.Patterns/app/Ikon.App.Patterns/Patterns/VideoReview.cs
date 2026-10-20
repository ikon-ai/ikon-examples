namespace Ikon.App.Patterns.Patterns;

// Pattern: video-review — see docs/patterns/video-review.md.
// The demo plays a short clip shipped in the gallery's public folder.
internal sealed class VideoReview : IPatternDemo
{
    public string Slug => "video-review";
    public string Title => "Video review with timed comments";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => RenderReview((UIView)view, "/review-sample.mp4");

    #region example:pattern-video-review
    private sealed record TimedComment(string Id, double At, string Text);

    // Shared by every reviewer of the video. A real app keeps them in a table keyed by the video's id.
    private readonly ReactiveList<TimedComment> _comments = new();

    // Where this viewer's player is, as it last reported
    private readonly ClientReactive<double> _position = new(0);
    private readonly ClientReactive<double> _duration = new(0);

    // Where this viewer's player seeks to: a new value seeks, the same value again does not
    private readonly ClientReactive<double?> _seekTo = new(null);
    // Per user, so a reload keeps a half-written comment
    private readonly UserReactive<string> _draft = new("");

    private void RenderReview(UIView view, string videoUrl)
    {
        view.Column(["gap-3 w-full max-w-3xl"], content: col =>
        {
            col.AspectRatio(ratio: 16.0 / 9.0, content: box =>
                box.VideoUrlPlayer(["w-full h-full rounded-lg bg-black"], url: videoUrl, controls: true, playsInline: true,
                    startTime: _seekTo.Value,
                    // At most once a second while playing, and at once on a pause or a seek
                    onTimeUpdate: async time =>
                    {
                        _position.Value = time.CurrentTime;
                        _duration.Value = time.Duration;
                    }));

            RenderTimeline(col);

            col.Row([Layout.Row.Form], content: row =>
            {
                row.TextField(["default", "flex-1"], label: $"Comment at {Timecode(_position.Value)}", placeholder: "Pause where the note belongs", bind: _draft);
                row.Button([Button.PrimaryMd], text: "Add", disabled: string.IsNullOrWhiteSpace(_draft.Value), onClick: AddComment);
            });

            foreach (var comment in _comments.OrderBy(c => c.At))
            {
                col.Row(["gap-2 items-baseline"], key: comment.Id, content: row =>
                {
                    row.Button([Button.GhostSm, "font-mono"], text: Timecode(comment.At), ariaLabel: $"Play from {Timecode(comment.At)}",
                        onClick: () => SeekTo(comment.At));
                    row.Text([Text.Body], comment.Text);
                });
            }
        });
    }

    // A mark for each comment along the video, and how far the player is
    private void RenderTimeline(UIView view)
    {
        if (_duration.Value <= 0)
        {
            return;
        }

        view.Box(["relative h-2 w-full rounded-full bg-muted"], content: bar =>
        {
            bar.Box(["absolute inset-y-0 left-0 rounded-full bg-brand-solid/40", $"w-[{_position.Value / _duration.Value * 100:0.#}%]"]);

            foreach (var comment in _comments)
            {
                bar.Box(["absolute -top-1 h-4 w-1 -translate-x-1/2 rounded-sm bg-brand-solid", $"left-[{comment.At / _duration.Value * 100:0.#}%]"], key: comment.Id);
            }
        });
    }

    private void AddComment()
    {
        if (_draft.Value.Trim() is not { Length: > 0 } text)
        {
            return;
        }

        // The position the player last reported, exact once it is paused
        _comments.Add(new TimedComment(Guid.NewGuid().ToString("N"), _position.Value, text));
        _draft.Value = "";
    }

    private void SeekTo(double seconds)
    {
        // Seeking twice to the same moment needs a different value, so the second one is nudged
        _seekTo.Value = _seekTo.Value == seconds ? seconds + 0.001 : seconds;
    }

    private static string Timecode(double seconds) => TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
    #endregion
}
