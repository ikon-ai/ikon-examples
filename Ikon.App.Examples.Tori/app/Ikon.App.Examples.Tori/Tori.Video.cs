public partial class Tori
{
    private void SetupVideoInputHandlers()
    {
        Video.VideoInputStreamBeginAsync += async args =>
        {
            if (_videoStreamStates.TryGetValue(args.StreamId, out var existingInfo))
            {
                existingInfo.Codec = args.Codec;
                existingInfo.Width = args.Width;
                existingInfo.Height = args.Height;
                existingInfo.Framerate = args.Framerate;
            }
            else
            {
                var info = new VideoStreamInfo(args.Codec, args.Width, args.Height, args.Framerate, args.ClientSessionId, args.TrackId);
                _videoStreamStates[args.StreamId] = info;

                var isScreenShare = args.SourceType == "screen";

                if (isScreenShare)
                {
                    UpdateParticipant(args.ClientSessionId, p => p with
                    {
                        ScreenShareStreamId = args.StreamId,
                        IsScreenSharing = true
                    });
                }
                else
                {
                    UpdateParticipant(args.ClientSessionId, p => p with
                    {
                        VideoStreamId = args.StreamId,
                        EchoVideoStreamId = args.StreamId,
                        IsVideoEnabled = true
                    });
                }
            }
        };

        Video.VideoInputFrameAsync += async args =>
        {
            if (!_videoStreamStates.TryGetValue(args.StreamId, out var info))
            {
                return;
            }

            // Broadcast to ALL participants (including sender for self-view)
            var targetIds = _participants.Value
                .Select(p => p.ClientSessionId)
                .ToList();

            if (targetIds.Count > 0)
            {
                await Video.SendAsync(
                    args.Data,
                    args.FrameNumber,
                    args.IsKey,
                    args.TimestampInUs,
                    args.DurationInUs,
                    info.Codec,
                    info.Width,
                    info.Height,
                    info.Framerate,
                    args.StreamId,
                    targetIds);

                // Register output→input mapping after first Send (when output stream is created)
                if (!info.OutputMappingRegistered)
                {
                    var outputInfo = Video.GetOutputStreamInfo(args.StreamId);

                    if (outputInfo != null)
                    {
                        _outputToInputTrack[outputInfo.TrackId] = (info.ClientSessionId, info.InputTrackId);
                        info.OutputMappingRegistered = true;
                    }
                }
            }
        };

        Video.VideoInputStreamEndAsync += async args =>
        {
            // Clean up output→input mapping
            var outputInfo = Video.GetOutputStreamInfo(args.StreamId);

            if (outputInfo != null)
            {
                _outputToInputTrack.Remove(outputInfo.TrackId);
            }

            await Video.CloseAsync(args.StreamId);
            _videoStreamStates.Remove(args.StreamId);

            // Check if this is a screen share or camera stream ending by comparing stream IDs
            var participant = _participants.Value.FirstOrDefault(p => p.ClientSessionId == args.ClientSessionId);
            var isScreenShare = participant?.ScreenShareStreamId == args.StreamId;

            if (isScreenShare)
            {
                UpdateParticipant(args.ClientSessionId, p => p with
                {
                    ScreenShareStreamId = null,
                    IsScreenSharing = false
                });
            }
            else
            {
                UpdateParticipant(args.ClientSessionId, p => p with
                {
                    VideoStreamId = null,
                    EchoVideoStreamId = null,
                    IsVideoEnabled = false
                });
            }
        };
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
            UpdateParticipant(clientScope.Value.Id, p => p with { IsScreenSharing = false, ScreenShareStreamId = null });
        }
    }
}
