public partial class Tori
{
    private readonly Dictionary<string, (VideoInput Input, VideoPlayback? Playback)> _videoRelays = new();

    private static string CameraSurface(int clientSessionId) => $"camera-{clientSessionId}";

    private static string ScreenSurface(int clientSessionId) => $"screen-{clientSessionId}";

    private void SetupVideoInputHandlers()
    {
        Video.InputStartedAsync += async input =>
        {
            _videoRelays[input.Id] = (input, null);

            if (input.Kind == VideoSourceKind.Screen)
            {
                UpdateParticipant(input.ClientSessionId, p => p with
                {
                    ScreenShareInputId = input.Id,
                    IsScreenSharing = true
                });
            }
            else
            {
                UpdateParticipant(input.ClientSessionId, p => p with
                {
                    CameraInputId = input.Id,
                    IsVideoEnabled = true
                });
            }

            SyncVideoAudiences();
        };

        Video.InputEndedAsync += async input =>
        {
            _videoRelays.Remove(input.Id);

            if (input.Kind == VideoSourceKind.Screen)
            {
                UpdateParticipant(input.ClientSessionId, p => p with
                {
                    ScreenShareInputId = null,
                    IsScreenSharing = false
                });
            }
            else
            {
                UpdateParticipant(input.ClientSessionId, p => p with
                {
                    CameraInputId = null,
                    IsVideoEnabled = false
                });
            }
        };
    }

    // The owner shows its own capture as a local preview, so each relay goes to the other participants only
    private void SyncVideoAudiences()
    {
        var participantIds = _participants.Value.Select(p => p.ClientSessionId).ToList();

        foreach (var (inputId, (input, playback)) in _videoRelays.ToList())
        {
            var viewers = participantIds.Where(id => id != input.ClientSessionId).ToList();

            if (playback is { IsEnded: false })
            {
                playback.SetAudience(MediaTargets.To(viewers));
                continue;
            }

            if (viewers.Count == 0)
            {
                continue;
            }

            var surface = input.Kind == VideoSourceKind.Screen ? ScreenSurface(input.ClientSessionId) : CameraSurface(input.ClientSessionId);
            _videoRelays[inputId] = (input, Video.Play(MediaTargets.To(viewers), surface, input));
        }
    }

    private async Task OnVideoCaptureStart(MediaCaptureEvent e)
    {
        _isVideoEnabled.Value = true;
        _activeVideoStreamId.Value = e.StreamId;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsVideoEnabled = true });
        }
    }

    private async Task OnVideoCaptureStop(MediaCaptureEvent e)
    {
        _isVideoEnabled.Value = false;
        _activeVideoStreamId.Value = null;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsVideoEnabled = false });
        }
    }

    private async Task OnScreenShareStart(MediaCaptureEvent e)
    {
        _isScreenShareEnabled.Value = true;
        _activeScreenShareStreamId.Value = e.StreamId;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsScreenSharing = true });
        }
    }

    private async Task OnScreenShareStop(MediaCaptureEvent e)
    {
        _isScreenShareEnabled.Value = false;
        _activeScreenShareStreamId.Value = null;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope != null)
        {
            UpdateParticipant(clientScope.Value.Id, p => p with { IsScreenSharing = false, ScreenShareInputId = null });
        }
    }
}
