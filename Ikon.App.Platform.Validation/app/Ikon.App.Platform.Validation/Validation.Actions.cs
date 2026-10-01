public partial class Validation
{
    private readonly ClientReactive<string> _feedbackOpenResult = new("OpenFeedback: not clicked yet");
    private readonly ClientReactive<string> _lazyDownloadResult = new("Download: not clicked yet");
    private readonly ClientReactive<string> _navigateToResult = new("NavigateToAsync: not clicked yet");
    private readonly ClientReactive<string> _actionFailedResult = new("Action failed: not triggered yet");
    private int _actionFailedHooked;

    private void RenderActionsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Copy to Clipboard
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Copy to Clipboard");
                view.Text([Text.Caption, "mb-4"], "Copies text to the system clipboard");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.CopyToClipboard,
                        options: new CopyToClipboardActionOptions { Text = "Hello, this is copied text!" },
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Copy: Success" : "Copy: Failed";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "clipboard-copy");
                            v.Text(text: "Copy Text");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.CopyToClipboard,
                        options: new CopyToClipboardActionOptions { Text = "Another copied text" },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "copy");
                            v.Text(text: "Copy Another");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.CopyToClipboard,
                        disabled: true,
                        options: new CopyToClipboardActionOptions { Text = "Disabled" },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "clipboard");
                            v.Text(text: "Disabled");
                        });
                });
            });

            // Share
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Share");
                view.Text([Text.Caption, "mb-4"], "Opens the native share dialog");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.Share,
                        options: new ShareActionOptions
                        {
                            Title = "Check this out!",
                            Text = "I found something interesting",
                            Url = "https://example.com"
                        },
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Share: Success" : "Share: Failed/Cancelled";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "share");
                            v.Text(text: "Share Link");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.Share,
                        options: new ShareActionOptions
                        {
                            Title = "Plain Text Share",
                            Text = "Just sharing some plain text content"
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "share-2");
                            v.Text(text: "Share Text");
                        });
                });
            });

            // Download File
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Download File");
                view.Text([Text.Caption, "mb-4"], "Downloads a file from a URL");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions
                        {
                            Url = "/test-images/square-a.svg",
                            Filename = "sample.png"
                        },
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Download: Started" : "Download: Failed";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "download");
                            v.Text(text: "Download Image");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions
                        {
                            Url = "/test-media/sample.pdf"
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "file");
                            v.Text(text: "Download PDF");
                        });
                });
            });

            // Get Location
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Get Location");
                view.Text([Text.Caption, "mb-4"], "Requests the user's current location");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.GetLocation,
                        onActionComplete: async e =>
                        {
                            if (e is LocationActionEvent loc && loc.Success)
                            {
                                _clientFunctionResultText.Value = $"Location: {loc.Latitude:F6}, {loc.Longitude:F6}";
                            }
                            else
                            {
                                _clientFunctionResultText.Value = "Location: Failed";
                            }

                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "map-pin");
                            v.Text(text: "Get My Location");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.GetLocation,
                        disabled: true,
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "map-pin-off");
                            v.Text(text: "Disabled");
                        });
                });
            });

            // Pick Contacts
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Pick Contacts");
                view.Text([Text.Caption, "mb-4"], "Opens the contact picker");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.PickContacts,
                        options: new PickContactsActionOptions { Multiple = false },
                        onActionComplete: async e =>
                        {
                            if (e is ContactsActionEvent contacts && contacts.Success)
                            {
                                _clientFunctionResultText.Value = $"Contacts: {contacts.Contacts?.Count ?? 0} selected";
                            }
                            else
                            {
                                _clientFunctionResultText.Value = "Contacts: Failed/Not Supported";
                            }

                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "user");
                            v.Text(text: "Pick One Contact");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.PickContacts,
                        options: new PickContactsActionOptions { Multiple = true },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "users");
                            v.Text(text: "Pick Multiple");
                        });
                });
            });

            // Fullscreen
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Fullscreen");
                view.Text([Text.Caption, "mb-4"], "Request and exit fullscreen mode");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.RequestFullscreen,
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Fullscreen: Entered" : "Fullscreen: Failed";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "enter-full-screen");
                            v.Text(text: "Enter Fullscreen");
                        });

                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.ExitFullscreen,
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Fullscreen: Exited" : "Fullscreen: Failed";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "exit-full-screen");
                            v.Text(text: "Exit Fullscreen");
                        });
                });
            });

            // ClientFunctions Section
            view.Box([Card.Default, "p-6 mt-8"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "ClientFunctions Overview");
                view.Text([Text.Caption, "mb-4"], "ClientFunctions are non-gesture APIs that can be called programmatically without user interaction");
            });

            // Theme functions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Theme");
                view.Text([Text.Caption, "mb-4"], "Set the current theme");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Set Theme (dark)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "SetTheme: Success (dark)" : "SetTheme: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Set Theme (light)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetThemeAsync(Theme.Light, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "SetTheme: Success (light)" : "SetTheme: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Language and Timezone
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Language & Timezone");
                view.Text([Text.Caption, "mb-4"], "Get client language and timezone information");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Get Language",
                        onClick: async () =>
                        {
                            var language = await ClientFunctions.GetLanguageAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = $"Language: {language}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Get Timezone",
                        onClick: async () =>
                        {
                            var timezone = await ClientFunctions.GetTimezoneAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = $"Timezone: {timezone}";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // URL functions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "URL");
                view.Text([Text.Caption, "mb-4"], "Get and set the browser URL");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Get URL",
                        onClick: async () =>
                        {
                            var url = await ClientFunctions.GetUrlAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = $"URL: {url ?? "(null)"}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Set URL (/test)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetUrlAsync("/test?param=1", targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "SetUrl: Success" : "SetUrl: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Feedback
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Feedback");
                view.Text([Text.Caption, "mb-4"], "Open the platform's feedback sheet from an app-placed control");

                if (!view.CanOpenFeedback())
                {
                    view.Text([Text.Muted], "Feedback is not offered to this session", props: TestId("feedback-not-offered"));
                    return;
                }

                view.Row([Layout.Row.Md, "flex-wrap items-center"], content: row =>
                {
                    row.Button([Button.PrimaryMd],
                        text: "Send feedback",
                        props: TestId("feedback-open"),
                        onClick: async () =>
                        {
                            var opened = await ClientFunctions.OpenFeedbackAsync(clientSessionId);
                            var result = opened ? "OpenFeedback: Opened" : "OpenFeedback: Not offered";
                            _feedbackOpenResult.Value = result;
                            _clientFunctionResultText.Value = result;
                            _clientFunctionToastOpen.Value = true;
                        });
                    row.Text([Text.Body], _feedbackOpenResult.Value, props: TestId("feedback-open-result"));
                });
            });

            view.Feature("actions/downloads", content: RenderLazyDownloadsCard);
            RenderNavigateToCard(view);
            RenderActionFailedCard(view);

            // Vibrate
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Vibrate");
                view.Text([Text.Caption, "mb-4"], "Trigger device vibration (mobile only)");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Vibrate (200ms)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.VibrateAsync(200, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "Vibrate: Success" : "Vibrate: Failed/Not Supported";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Vibrate Pattern",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.VibrateAsync(new[] { 100, 50, 100, 50, 200 }, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "Vibrate: Success (pattern)" : "Vibrate: Failed/Not Supported";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Keep Screen Awake
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Keep Screen Awake");
                view.Text([Text.Caption, "mb-4"], "Prevent the screen from sleeping");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Keep Awake ON",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.KeepScreenAwakeAsync(true, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "KeepAwake: ON" : "KeepAwake: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Keep Awake OFF",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.KeepScreenAwakeAsync(false, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "KeepAwake: OFF" : "KeepAwake: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Visibility and Scroll
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Visibility & Scroll");
                view.Text([Text.Caption, "mb-4"], "Get visibility state and control scrolling");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Get Visibility",
                        onClick: async () =>
                        {
                            var visibility = await ClientFunctions.GetVisibilityAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = $"Visibility: {visibility}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Scroll To Top",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.ScrollToAsync(0, 0, smooth: true, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "ScrollTo: Success (0,0)" : "ScrollTo: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Scroll Down",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.ScrollToAsync(0, 500, smooth: true, targetId: clientSessionId);
                            _clientFunctionResultText.Value = success ? "ScrollTo: Success (0,500)" : "ScrollTo: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Battery and Network
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Battery & Network");
                view.Text([Text.Caption, "mb-4"], "Get device battery and network information");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Get Battery",
                        onClick: async () =>
                        {
                            var level = await ClientFunctions.GetBatteryLevelAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = level.HasValue ? $"Battery: {level.Value}%" : "Battery: Not Available";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.PrimaryMd],
                        text: "Get Network",
                        onClick: async () =>
                        {
                            var networkType = await ClientFunctions.GetNetworkTypeAsync(targetId: clientSessionId);
                            _clientFunctionResultText.Value = $"Network: {networkType ?? "Not Available"}";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Last result
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Last result");
                view.Text([Text.Caption], _clientFunctionResultText.Value);
            });

            // Result Toast notification
            view.Toast(
                viewportStyle: [Toast.ViewportBottomCenter],
                open: _clientFunctionToastOpen.Value,
                onOpenChange: async open => _clientFunctionToastOpen.Value = open,
                durationMs: 3000,
                toastStyle: [Toast.Base],
                title: "Result",
                titleStyle: [Toast.Title],
                description: _clientFunctionResultText.Value,
                descriptionStyle: [Toast.Description],
                showClose: true,
                closeStyle: [Toast.Close]);
        });
    }

    private void RenderLazyDownloadsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            var clientSessionId = ReactiveScope.ClientId;

            view.Text([Text.H2, "mb-1"], "Lazy Downloads");
            view.Text([Text.Caption, "mb-4"], "UrlProvider and AssetProvider run on the server only when the button is clicked, so nothing ships with the render; ClientFunctions.DownloadFileAsync pushes a download from any handler.");

            view.Row([Layout.Row.Md, "flex-wrap"], content: row =>
            {
                row.ActionButton([Button.PrimaryMd],
                    action: ActionKind.DownloadFile,
                    options: new DownloadFileActionOptions
                    {
                        Filename = "lazy-url.svg",
                        UrlProvider = () =>
                        {
                            _lazyDownloadResult.Value = "Download (UrlProvider): PASS provider ran on click";
                            return Task.FromResult<string?>("/test-images/square-a.svg");
                        }
                    },
                    props: TestId("download-url-provider"),
                    content: v =>
                    {
                        v.Icon([Icon.Default, "mr-2"], name: "link");
                        v.Text(text: "UrlProvider");
                    });

                row.ActionButton([Button.PrimaryMd],
                    action: ActionKind.DownloadFile,
                    options: new DownloadFileActionOptions
                    {
                        Filename = "lazy-asset.txt",
                        AssetProvider = StageLazyDownloadAssetAsync
                    },
                    props: TestId("download-asset-provider"),
                    content: v =>
                    {
                        v.Icon([Icon.Default, "mr-2"], name: "cloud-download");
                        v.Text(text: "AssetProvider");
                    });

                row.Button([Button.PrimaryMd],
                    text: "DownloadFileAsync",
                    icon: "download",
                    props: TestId("download-client-function"),
                    onClick: async () =>
                    {
                        var accepted = await ClientFunctions.DownloadFileAsync("/test-images/square-a.svg", "pushed.svg", targetId: clientSessionId);
                        _lazyDownloadResult.Value = accepted
                            ? "DownloadFileAsync: PASS client accepted the download"
                            : "DownloadFileAsync: FAIL client has no download function";
                    });
            });

            view.Text([Text.Body, "mt-3"], _lazyDownloadResult.Value, props: TestId("download-result"));
        });
    }

    private async Task<AssetUri> StageLazyDownloadAssetAsync()
    {
        try
        {
            var uri = new AssetUri(AssetClass.CloudFile, "validation/lazy-download.txt", spaceId: app.GlobalState.SpaceId);
            var bytes = Encoding.UTF8.GetBytes($"Staged by the Validation app's AssetProvider at {DateTime.UtcNow:O}\n");
            await Asset.Instance.SetBytesAsync(uri, bytes, new AssetMetadata(mimeType: "text/plain"));
            _lazyDownloadResult.Value = "Download (AssetProvider): PASS asset staged on click";
            return uri;
        }
        catch (Exception ex)
        {
            _lazyDownloadResult.Value = $"Download (AssetProvider): FAIL {ex.Message}";
            throw;
        }
    }

    private void RenderNavigateToCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            var clientSessionId = ReactiveScope.ClientId;

            view.Text([Text.H2, "mb-1"], "Navigate Away");
            view.Text([Text.Caption, "mb-4"], "ClientFunctions.NavigateToAsync leaves the app for an absolute http(s) URL and ends the session; anything else is refused and returns false.");

            view.Row([Layout.Row.Md, "flex-wrap"], content: row =>
            {
                row.Button([Button.PrimaryMd],
                    text: "Try a javascript: URL",
                    props: TestId("navigate-refused"),
                    onClick: async () =>
                    {
                        var left = await ClientFunctions.NavigateToAsync("javascript:alert(1)", targetId: clientSessionId);
                        _navigateToResult.Value = left
                            ? "NavigateToAsync: FAIL a javascript: URL was followed"
                            : "NavigateToAsync: PASS javascript: URL refused";
                    });

                row.Button([Button.PrimaryMd],
                    text: "Leave for example.com",
                    icon: "external-link",
                    onClick: async () =>
                    {
                        _navigateToResult.Value = "NavigateToAsync: leaving…";
                        await ClientFunctions.NavigateToAsync("https://example.com/", targetId: clientSessionId);
                    });
            });

            view.Text([Text.Body, "mt-3"], _navigateToResult.Value, props: TestId("navigate-result"));
        });
    }

    private void RenderActionFailedCard(UIView view)
    {
        if (Interlocked.Exchange(ref _actionFailedHooked, 1) == 0)
        {
            UI.ActionFailedAsync += OnDeliberateActionFailedAsync;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-1"], "Action Failures");
            view.Text([Text.Caption, "mb-4"], "A handler that throws is reported to UI.ActionFailedAsync inside the clicking client's scope. This button throws on purpose; the subscriber renders the outcome below.");

            view.Row([Layout.Row.Md, "flex-wrap items-center"], content: row =>
            {
                row.Button([Button.PrimaryMd],
                    text: "Run a failing action",
                    icon: "triangle-alert",
                    props: TestId("action-fail-trigger"),
                    onClick: ThrowDeliberateActionFailure);
                row.Text([Text.Body], _actionFailedResult.Value, props: TestId("action-fail-result"));
            });
        });
    }

    private Task OnDeliberateActionFailedAsync(ActionFailedEventArgs args)
    {
        // Every other tab's failures reach this subscriber too; only the deliberate one is ours to report
        if (args.Exception is DeliberateActionFailureException deliberate)
        {
            _actionFailedResult.Value = $"PASS: Action failed: {deliberate.Message} · action {args.ActionId.ToString()[..8]} · {Path.GetFileName(args.CallSite)}";
        }

        return Task.CompletedTask;
    }

    private static Task ThrowDeliberateActionFailure() =>
        throw new DeliberateActionFailureException("deliberate failure from the Actions tab");

    private sealed class DeliberateActionFailureException(string message) : Exception(message);
}
