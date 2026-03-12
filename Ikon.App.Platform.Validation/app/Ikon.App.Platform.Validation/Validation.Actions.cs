public partial class Validation
{
    private void RenderActionsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // ActionButton overview
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "ActionButton Overview");
                view.Text([Text.Caption, "mb-4"], "ActionButtons trigger client-side actions that require user gesture (clipboard, share, download, etc.)");
            });

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

                    view.ActionButton([Button.OutlineMd],
                        action: ActionKind.CopyToClipboard,
                        options: new CopyToClipboardActionOptions { Text = "Another copied text" },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "copy");
                            v.Text(text: "Copy Another");
                        });

                    view.ActionButton([Button.GhostMd],
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

                    view.ActionButton([Button.SecondaryMd],
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
                            Url = "https://via.placeholder.com/150",
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

                    view.ActionButton([Button.OutlineMd],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions
                        {
                            Url = "https://example.com/sample.pdf"
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "file");
                            v.Text(text: "Download PDF");
                        });
                });
            });

            // Show Notification
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Show Notification");
                view.Text([Text.Caption, "mb-4"], "Displays a system notification");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.ActionButton([Button.PrimaryMd],
                        action: ActionKind.ShowNotification,
                        options: new ShowNotificationActionOptions
                        {
                            Title = "Hello!",
                            Body = "This is a notification message"
                        },
                        onActionComplete: async e =>
                        {
                            _clientFunctionResultText.Value = e.Success ? "Notification: Shown" : "Notification: Failed/Denied";
                            _clientFunctionToastOpen.Value = true;
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "bell");
                            v.Text(text: "Show Notification");
                        });

                    view.ActionButton([Button.SecondaryMd],
                        action: ActionKind.ShowNotification,
                        options: new ShowNotificationActionOptions
                        {
                            Title = "Title Only"
                        },
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "bell-ring");
                            v.Text(text: "Title Only");
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

                    view.ActionButton([Button.OutlineMd],
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

                    view.ActionButton([Button.SecondaryMd],
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

                    view.ActionButton([Button.SecondaryMd],
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
                view.Text([Text.Caption, "mb-4"], "Get and set the current theme");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        label: "Get Theme",
                        onClick: async () =>
                        {
                            var theme = await ClientFunctions.GetThemeAsync(clientSessionId);
                            _clientFunctionResultText.Value = $"Theme: {theme ?? "(null)"}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Set Theme (dark)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetThemeAsync(clientSessionId, "dark");
                            _clientFunctionResultText.Value = success ? "SetTheme: Success (dark)" : "SetTheme: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.OutlineMd],
                        label: "Set Theme (light)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetThemeAsync(clientSessionId, "light");
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
                        label: "Get Language",
                        onClick: async () =>
                        {
                            var language = await ClientFunctions.GetLanguageAsync(clientSessionId);
                            _clientFunctionResultText.Value = $"Language: {language}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Get Timezone",
                        onClick: async () =>
                        {
                            var timezone = await ClientFunctions.GetTimezoneAsync(clientSessionId);
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
                        label: "Get URL",
                        onClick: async () =>
                        {
                            var url = await ClientFunctions.GetUrlAsync(clientSessionId);
                            _clientFunctionResultText.Value = $"URL: {url ?? "(null)"}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Set URL (/test)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.SetUrlAsync(clientSessionId, "/test?param=1");
                            _clientFunctionResultText.Value = success ? "SetUrl: Success" : "SetUrl: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Vibrate
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Vibrate");
                view.Text([Text.Caption, "mb-4"], "Trigger device vibration (mobile only)");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        label: "Vibrate (200ms)",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.VibrateAsync(clientSessionId, "200");
                            _clientFunctionResultText.Value = success ? "Vibrate: Success" : "Vibrate: Failed/Not Supported";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Vibrate Pattern",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.VibrateAsync(clientSessionId, "100,50,100,50,200");
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
                        label: "Keep Awake ON",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.KeepScreenAwakeAsync(clientSessionId, true);
                            _clientFunctionResultText.Value = success ? "KeepAwake: ON" : "KeepAwake: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Keep Awake OFF",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.KeepScreenAwakeAsync(clientSessionId, false);
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
                        label: "Get Visibility",
                        onClick: async () =>
                        {
                            var visibility = await ClientFunctions.GetVisibilityAsync(clientSessionId);
                            _clientFunctionResultText.Value = $"Visibility: {visibility ?? "(null)"}";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Scroll To Top",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.ScrollToAsync(clientSessionId, 0, 0, smooth: true);
                            _clientFunctionResultText.Value = success ? "ScrollTo: Success (0,0)" : "ScrollTo: Failed";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.OutlineMd],
                        label: "Scroll Down",
                        onClick: async () =>
                        {
                            var success = await ClientFunctions.ScrollToAsync(clientSessionId, 0, 500, smooth: true);
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
                        label: "Get Battery",
                        onClick: async () =>
                        {
                            var level = await ClientFunctions.GetBatteryLevelAsync(clientSessionId);
                            _clientFunctionResultText.Value = level.HasValue ? $"Battery: {level.Value}%" : "Battery: Not Available";
                            _clientFunctionToastOpen.Value = true;
                        });

                    view.Button([Button.SecondaryMd],
                        label: "Get Network",
                        onClick: async () =>
                        {
                            var networkType = await ClientFunctions.GetNetworkTypeAsync(clientSessionId);
                            _clientFunctionResultText.Value = $"Network: {networkType ?? "Not Available"}";
                            _clientFunctionToastOpen.Value = true;
                        });
                });
            });

            // Result Toast notification
            view.Toast(
                viewportStyle: [Toast.ViewportBottomCenter],
                open: _clientFunctionToastOpen.Value,
                onOpenChange: async open => _clientFunctionToastOpen.Value = open ?? false,
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
}
