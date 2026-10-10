public partial class Validation
{
    private readonly Reactive<string> _echoAudience = new("everyone");
    private readonly Reactive<string> _echoAudienceResult = new("");
    private readonly Reactive<string> _echoKeyFrameResult = new("");
    private readonly ClientReactive<string> _echoFit = new("contain");

    private long _echoKeyFrameRequestedAt;

    private VideoFit EchoFit => _echoFit.Value == "cover" ? VideoFit.Cover : VideoFit.Contain;

    private void RenderEchoControls(UIView view)
    {
        view.Row([Layout.Row.Md, "flex-wrap items-center mb-2"], content: view =>
        {
            view.ToggleGroupSingle([Layout.Row.Sm],
                value: _echoAudience.Value,
                onValueChange: async value => await SetEchoAudienceAsync(string.IsNullOrEmpty(value) ? "everyone" : value),
                props: TestId("video-echo-audience"),
                content: view =>
                {
                    view.ToggleGroupItem([Toggle.DefaultMd], value: "everyone", content: v => v.Text(text: "Everyone"));
                    view.ToggleGroupItem([Toggle.DefaultMd], value: "owner", content: v => v.Text(text: "Owner only"));
                    view.ToggleGroupItem([Toggle.DefaultMd], value: "others", content: v => v.Text(text: "All but owner"));
                });

            view.ToggleGroupSingle([Layout.Row.Sm],
                value: _echoFit.Value,
                onValueChange: async value => _echoFit.Value = string.IsNullOrEmpty(value) ? "contain" : value,
                content: view =>
                {
                    view.ToggleGroupItem([Toggle.DefaultMd], value: "contain", content: v => v.Text(text: "Contain"));
                    view.ToggleGroupItem([Toggle.DefaultMd], value: "cover", content: v => v.Text(text: "Cover"));
                });

            view.Button([Button.OutlineMd],
                text: "Request keyframe",
                onClick: async () => RequestEchoKeyFrame(),
                props: TestId("video-echo-keyframe-run"));
        });

        if (_echoAudienceResult.Value.Length > 0)
        {
            view.Text([Text.Caption, "mb-1"], _echoAudienceResult.Value, props: TestId("video-echo-audience-result"));
        }

        if (_echoKeyFrameResult.Value.Length > 0)
        {
            view.Text([Text.Caption, "mb-2"], _echoKeyFrameResult.Value, props: TestId("video-echo-keyframe"));
        }
    }

    private async Task SetEchoAudienceAsync(string audience)
    {
        _echoAudience.Value = audience;

        if (_cameraEcho is not { } echo || echo.Playback.IsEnded)
        {
            _echoAudienceResult.Value = "FAIL audience: no camera echo is playing";
            return;
        }

        int owner = echo.Input.ClientSessionId;
        var targets = audience switch
        {
            "owner" => MediaTargets.To(owner),
            "others" => MediaTargets.EveryoneExcept(owner),
            _ => MediaTargets.Everyone
        };

        echo.Playback.SetAudience(targets);

        // The owner leaving the audience must clear its surface and leave the playback running for the rest
        _echoAudienceResult.Value = echo.Playback.IsEnded
            ? $"FAIL audience {audience}: the echo ended {await echo.Playback.Completion}"
            : $"PASS audience {audience}: {echo.Playback.Audience}, still playing";
    }

    private void RequestEchoKeyFrame()
    {
        if (_cameraEcho is not { } echo)
        {
            _echoKeyFrameResult.Value = "FAIL keyframe: no camera echo is playing";
            return;
        }

        Interlocked.Exchange(ref _echoKeyFrameRequestedAt, System.Diagnostics.Stopwatch.GetTimestamp());
        _echoKeyFrameResult.Value = "keyframe: requested, waiting for one";
        echo.Input.RequestKeyFrame();
    }

    private void NoteEchoFrame(VideoInputFrame frame)
    {
        if (!frame.IsKey)
        {
            return;
        }

        long requestedAt = Interlocked.Exchange(ref _echoKeyFrameRequestedAt, 0);

        if (requestedAt != 0)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(requestedAt);
            _echoKeyFrameResult.Value = $"PASS keyframe: one arrived {elapsed.TotalMilliseconds:F0} ms after the request";
        }
    }
}
