public partial class Validation
{
    private void RenderVideoSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Camera Capture section
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Camera Capture");
                view.Text([Text.Caption, "mb-4"], "Capture video from camera and echo back to VideoStreamCanvas");

                view.Column([Layout.Column.Md], content: view =>
                {
                    var cameraDevices = _availableDevices.Value
                        .Where(d => d.Kind == "videoinput" && !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId != "default" && d.DeviceId != "communications")
                        .Select(d => new SelectOption(d.DeviceId, string.IsNullOrEmpty(d.Label) ? "Camera" : d.Label))
                        .ToArray();
                    var cameraOptions = new[] { new SelectOption("default", "Default") }.Concat(cameraDevices).ToArray();

                    view.Row([Layout.Row.Md, "flex-wrap mb-4 items-end"], content: view =>
                    {
                        view.Column(["flex-1 min-w-[200px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Camera");
                            view.Select(
                                value: _selectedCameraId.Value,
                                options: cameraOptions,
                                disabled: _devicesLoading.Value,
                                onValueChange: async v => _selectedCameraId.Value = v);
                        });
                        view.Button([Button.OutlineMd, Button.Size.Icon],
                            disabled: _devicesLoading.Value,
                            onClick: async () =>
                            {
                                var clientScope = ReactiveScope.TryGet<ClientScope>();

                                if (clientScope != null)
                                {
                                    await RefreshDevicesAsync(clientScope.Value.Id);
                                }
                            },
                            content: v => v.Icon([Icon.Default], name: "refresh-cw"));
                    });

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Codec");
                            view.Select(
                                value: _cameraCodec.Value,
                                options:
                                [
                                    new SelectOption("h264", "H.264"),
                                    new SelectOption("vp8", "VP8"),
                                    new SelectOption("vp9", "VP9"),
                                    new SelectOption("av1", "AV1"),
                                ],
                                onValueChange: async v => _cameraCodec.Value = v);
                        });
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Resolution");
                            view.Select(
                                value: _cameraResolution.Value,
                                options:
                                [
                                    new SelectOption("480p", "480p"),
                                    new SelectOption("720p", "720p"),
                                    new SelectOption("1080p", "1080p"),
                                    new SelectOption("1440p", "1440p"),
                                ],
                                onValueChange: async v => _cameraResolution.Value = v);
                        });
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Bitrate (Mbps)");
                            view.TextField([Input.Default], value: _cameraBitrate.Value, type: "number", step: "0.5", min: "0.1",
                                onValueChange: async v => _cameraBitrate.Value = v);
                        });
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Framerate");
                            view.TextField([Input.Default], value: _cameraFramerate.Value, type: "number", step: "1", min: "1", max: "120",
                                onValueChange: async v => _cameraFramerate.Value = v);
                        });
                    });

                    var camRes = ParseResolution(_cameraResolution.Value);

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.CaptureButton(
                            [_isCameraCaptureActive.Value
                                ? Button.DangerMd
                                : Button.PrimaryMd],
                            kind: MediaCaptureKind.Camera,
                            label: _isCameraCaptureActive.Value ? "Stop Camera" : "Start Camera",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            videoOptions: new ClientVideoCaptureOptions
                            {
                                PreferredCodecs = [ParseCodec(_cameraCodec.Value)],
                                Framerate = ParseFramerate(_cameraFramerate.Value),
                                Width = camRes.Width,
                                Height = camRes.Height,
                                Bitrate = ParseBitrateMbps(_cameraBitrate.Value),
                                DeviceId = GetSelectedDeviceId(_selectedCameraId.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async _ =>
                            {
                                _isCameraCaptureActive.Value = true;
                            },
                            onCaptureStop: async _ =>
                            {
                                _isCameraCaptureActive.Value = false;
                                CleanupCameraEcho();
                            });
                    });

                    if (_cameraEchoStreamId.Value != null)
                    {
                        view.Text([Text.Caption, "mb-2"], $"Camera dimensions: {_cameraWidth.Value}x{_cameraHeight.Value}");
                        view.Box([Media.VideoContainer], content: view =>
                        {
                            view.VideoStreamCanvas(
                                [Media.Fill, Media.Mirror],
                                streamId: _cameraEchoStreamId.Value,
                                width: _cameraWidth.Value > 0 ? _cameraWidth.Value : 640,
                                height: _cameraHeight.Value > 0 ? _cameraHeight.Value : 480);
                        });
                    }
                    else
                    {
                        view.Box([Media.EmptyState], content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "video-off");
                                view.Text([Media.PlaceholderText], "No camera stream");
                                view.Text([Media.PlaceholderHint], "Click Start Camera to begin");
                            });
                        });
                    }
                });
            });

            // Screen Capture section
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Screen Capture");
                view.Text([Text.Caption, "mb-4"], "Capture video from screen and echo back to VideoStreamCanvas");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Codec");
                            view.Select(
                                value: _screenCodec.Value,
                                options:
                                [
                                    new SelectOption("h264", "H.264"),
                                    new SelectOption("vp8", "VP8"),
                                    new SelectOption("vp9", "VP9"),
                                    new SelectOption("av1", "AV1"),
                                ],
                                onValueChange: async v => _screenCodec.Value = v);
                        });
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Bitrate (Mbps)");
                            view.TextField([Input.Default], value: _screenBitrate.Value, type: "number", step: "0.5", min: "0.1",
                                onValueChange: async v => _screenBitrate.Value = v);
                        });
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Framerate");
                            view.TextField([Input.Default], value: _screenFramerate.Value, type: "number", step: "1", min: "1", max: "120",
                                onValueChange: async v => _screenFramerate.Value = v);
                        });
                    });

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.CaptureButton(
                            [_isScreenCaptureActive.Value
                                ? Button.DangerMd
                                : Button.PrimaryMd],
                            kind: MediaCaptureKind.Screen,
                            label: _isScreenCaptureActive.Value ? "Stop Screen" : "Start Screen",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            videoOptions: new ClientVideoCaptureOptions
                            {
                                PreferredCodecs = [ParseCodec(_screenCodec.Value)],
                                Framerate = ParseFramerate(_screenFramerate.Value),
                                Bitrate = ParseBitrateMbps(_screenBitrate.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async _ =>
                            {
                                _isScreenCaptureActive.Value = true;
                            },
                            onCaptureStop: async _ =>
                            {
                                _isScreenCaptureActive.Value = false;
                                CleanupScreenEcho();
                            });
                    });

                    if (_screenEchoStreamId.Value != null)
                    {
                        view.Text([Text.Caption, "mb-2"], $"Screen dimensions: {_screenWidth.Value}x{_screenHeight.Value}");
                        view.Box([Media.VideoContainer], content: view =>
                        {
                            view.VideoStreamCanvas(
                                [Media.Fill],
                                streamId: _screenEchoStreamId.Value,
                                width: _screenWidth.Value > 0 ? _screenWidth.Value : 640,
                                height: _screenHeight.Value > 0 ? _screenHeight.Value : 480);
                        });
                    }
                    else
                    {
                        view.Box([Media.EmptyState], content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "desktop");
                                view.Text([Media.PlaceholderText], "No screen stream");
                                view.Text([Media.PlaceholderHint], "Click Start Screen to begin");
                            });
                        });
                    }
                });
            });

            // Image Capture section
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Image Capture");
                view.Text([Text.Caption, "mb-4"], "Capture still image from camera using ActionButton");

                view.Column([Layout.Column.Md], content: view =>
                {
                    var imageCameraDevices = _availableDevices.Value
                        .Where(d => d.Kind == "videoinput" && !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId != "default" && d.DeviceId != "communications")
                        .Select(d => new SelectOption(d.DeviceId, string.IsNullOrEmpty(d.Label) ? "Camera" : d.Label))
                        .ToArray();
                    var imageCameraOptions = new[] { new SelectOption("default", "Default") }.Concat(imageCameraDevices).ToArray();

                    view.Row([Layout.Row.Md, "flex-wrap mb-4 items-end"], content: view =>
                    {
                        view.Column(["flex-1 min-w-[200px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Camera");
                            view.Select(
                                value: _selectedImageCameraId.Value,
                                options: imageCameraOptions,
                                disabled: _devicesLoading.Value,
                                onValueChange: async v => _selectedImageCameraId.Value = v);
                        });
                        view.Button([Button.OutlineMd, Button.Size.Icon],
                            disabled: _devicesLoading.Value,
                            onClick: async () =>
                            {
                                var clientScope = ReactiveScope.TryGet<ClientScope>();

                                if (clientScope != null)
                                {
                                    await RefreshDevicesAsync(clientScope.Value.Id);
                                }
                            },
                            content: v => v.Icon([Icon.Default], name: "refresh-cw"));
                    });

                    var imageDeviceId = GetSelectedDeviceId(_selectedImageCameraId.Value);

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.ActionButton([Button.PrimaryMd],
                            action: ActionKind.CaptureImage,
                            label: "Capture Image",
                            options: new CaptureImageActionOptions
                            {
                                Width = 1920,
                                Height = 1080,
                                Format = ClientImageCaptureFormat.Jpeg,
                                Quality = 0.9,
                                Constraints = imageDeviceId != null ? new CaptureImageConstraints { DeviceId = imageDeviceId } : null
                            },
                            onActionComplete: async result =>
                            {
                                if (result is ImageCaptureActionEvent img && img.Success && img.Data != null)
                                {
                                    _capturedImageData.Value = img.Data;
                                    _capturedImageMime.Value = img.Mime;
                                    _capturedImageWidth.Value = img.Width;
                                    _capturedImageHeight.Value = img.Height;
                                }
                            });

                        if (_capturedImageData.Value != null)
                        {
                            view.Button(
                                [Button.OutlineMd],
                                label: "Clear Image",
                                onClick: async () =>
                                {
                                    _capturedImageData.Value = null;
                                    _capturedImageMime.Value = null;
                                    _capturedImageWidth.Value = 0;
                                    _capturedImageHeight.Value = 0;
                                });
                        }
                    });

                    if (_capturedImageData.Value != null && _capturedImageMime.Value != null)
                    {
                        var dataUrl = $"data:{_capturedImageMime.Value};base64,{_capturedImageData.Value}";
                        view.Text([Text.Caption, "mb-2"], $"Captured: {_capturedImageWidth.Value}x{_capturedImageHeight.Value}");
                        view.Box([Media.VideoContainer], content: view =>
                        {
                            view.Image([Media.Fill, Media.Mirror], src: dataUrl, alt: "Captured image");
                        });
                    }
                    else
                    {
                        view.Box([Media.EmptyState], content: view =>
                        {
                            view.Column([Layout.Column.Center], content: view =>
                            {
                                view.Icon([Media.PlaceholderIcon], name: "camera");
                                view.Text([Media.PlaceholderText], "No image captured");
                                view.Text([Media.PlaceholderHint], "Click Capture Image to take a photo");
                            });
                        });
                    }
                });
            });

            // Video URL Player section
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Video URL Player");
                view.Text([Text.Caption, "mb-4"], "Play video from URL with configurable options");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextField(
                        [Input.Default, "mb-4"],
                        defaultValue: _videoUrl.Value,
                        placeholder: "Enter video URL",
                        onValueChange: value =>
                        {
                            _videoUrl.Value = value;
                            return Task.CompletedTask;
                        });

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _videoUrlLoop.Value,
                                onCheckedChange: value =>
                                {
                                    _videoUrlLoop.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Loop");
                        });

                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _videoUrlMuted.Value,
                                onCheckedChange: value =>
                                {
                                    _videoUrlMuted.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Muted");
                        });

                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _videoUrlControls.Value,
                                onCheckedChange: value =>
                                {
                                    _videoUrlControls.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Controls");
                        });
                    });

                    if (!string.IsNullOrEmpty(_videoUrl.Value))
                    {
                        view.Box([Media.VideoContainer], content: view =>
                        {
                            view.VideoUrlPlayer(
                                ["w-full h-full"],
                                url: _videoUrl.Value,
                                controls: _videoUrlControls.Value,
                                loop: _videoUrlLoop.Value,
                                muted: _videoUrlMuted.Value,
                                playsInline: true);
                        });
                    }
                });
            });
        });
    }
}
