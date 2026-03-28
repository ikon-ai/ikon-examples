public partial class Tori
{
    private void RenderApp(UIView view)
    {
        var clientScope = ReactiveScope.TryGet<ClientScope>();
        var clientParams = clientScope != null ? app.Clients[clientScope.Value.Id]?.Parameters : null;
        var meetId = clientParams?.Id ?? "";
        var clientName = clientParams?.Name ?? "";

        // State 0: User has left the meeting
        if (_hasLeft.Value)
        {
            RenderLeftMeetingPage(view);
            return;
        }

        // State 1: No valid meet ID - show Create Meet page
        if (!IsValidMeetId(meetId))
        {
            RenderCreateMeetPage(view);
            return;
        }

        // State 2: Valid ID but no name - show Enter Name page
        if (string.IsNullOrWhiteSpace(clientName) && !_hasJoined.Value)
        {
            RenderEnterNamePage(view);
            return;
        }

        // State 3: Valid ID and name - show meeting UI
        RenderMeetingPage(view);
    }

    private string GetMeetingUrl(string meetId)
    {
        var channelUrl = app.GlobalState.ChannelUrl;

        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            return $"?id={meetId}";
        }

        var separator = channelUrl.Contains('?') ? "&" : "?";
        return $"{channelUrl}{separator}id={meetId}";
    }

    private void RenderCreateMeetPage(UIView view)
    {
        var isDark = _currentTheme.Value == Constants.DarkTheme;

        view.Column(["h-full flex flex-col items-center justify-center gap-6 relative"], content: col =>
        {
            // Theme toggle in upper right
            col.Box(["absolute top-4 right-4"], content: corner =>
            {
                corner.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.Button(
                        [Button.GhostMd, Button.Size.Icon],
                        onClick: ToggleThemeAsync,
                        content: vv => vv.Icon([Icon.Default], name: isDark ? "sun" : "moon")),
                    contentSlot: v => v.Text([Text.Caption], isDark ? "Light mode" : "Dark mode"));
            });

            // Main content
            col.Text(["text-6xl font-bold text-foreground tracking-tight"], "Tori");

            // New meeting dropdown
            col.Popover(
                open: _newMeetMenuOpen.Value,
                onOpenChange: async open => _newMeetMenuOpen.Value = open ?? false,
                contentStyle: [DropdownMenu.Content, "w-56 translate-y-4"],
                trigger: v => v.Button(
                    [Button.PrimaryLg, "mt-4 gap-2 items-center"],
                    onClick: async () => _newMeetMenuOpen.Value = !_newMeetMenuOpen.Value,
                    content: btn =>
                    {
                        btn.Icon([Icon.Default], name: "video");
                        btn.Text(text: "New meeting");
                        btn.Icon([Icon.Default], name: "chevron-down");
                    }),
                content: menu =>
                {
                    menu.Column(["p-1"], content: items =>
                    {
                        // Create a meeting for later
                        items.Button(
                            [DropdownMenu.Item, "w-full justify-start font-sans text-popover-foreground focus:bg-transparent focus:text-popover-foreground focus-visible:bg-transparent hover:bg-accent hover:text-accent-foreground"],
                            onClick: async () =>
                            {
                                _newMeetMenuOpen.Value = false;
                                _generatedMeetLink.Value = GetMeetingUrl(GenerateMeetId());
                                _meetLinkDialogOpen.Value = true;
                            },
                            content: btn =>
                            {
                                btn.Icon(["mr-2 h-4 w-4"], name: "link");
                                btn.Text(text: "Create a meeting for later");
                            });

                        // Start an instant meeting
                        items.Button(
                            [DropdownMenu.Item, "w-full justify-start font-sans text-popover-foreground focus:bg-transparent focus:text-popover-foreground focus-visible:bg-transparent hover:bg-accent hover:text-accent-foreground"],
                            href: $"?id={GenerateMeetId()}",
                            content: btn =>
                            {
                                btn.Icon(["mr-2 h-4 w-4"], name: "play");
                                btn.Text(text: "Start an instant meeting");
                            });
                    });
                });

            // Easter egg at the bottom
            col.Box(["absolute bottom-8 left-0 right-0 flex justify-center"], content: bottom =>
            {
                bottom.Text(["text-sm text-muted-foreground/30 select-none"], "Torilla tavataan");
            });
        });

        // Meeting link dialog
        if (_meetLinkDialogOpen.Value)
        {
            RenderMeetLinkDialog(view);
        }
    }

    private void RenderMeetLinkDialog(UIView view)
    {
        var meetLink = _generatedMeetLink.Value;

        view.Dialog(
            open: _meetLinkDialogOpen.Value,
            onOpenChange: async open => _meetLinkDialogOpen.Value = open ?? false,
            overlayStyle: [AlertDialog.Overlay],
            contentStyle: [AlertDialog.Content, "max-w-md"],
            contentSlot: content =>
            {
                content.Column([Layout.Column.Md], content: col =>
                {
                    // Header
                    col.Row([Layout.Row.SpaceBetween, "items-center mb-4"], content: row =>
                    {
                        row.Text([Text.H3], "Meeting link created");
                        row.Button(
                            [Button.GhostMd, Button.Size.Icon],
                            onClick: async () => _meetLinkDialogOpen.Value = false,
                            content: v => v.Icon([Icon.Default], name: "x"));
                    });

                    // Description
                    col.Text([Text.Body, "mb-4"], "Share this link with others to invite them to your meeting:");

                    // Link display with copy and share buttons
                    col.Row([Card.Default, "p-3 gap-2 items-center"], content: row =>
                    {
                        row.Text([Text.Body, "flex-1 truncate"], meetLink);
                        row.Tooltip(
                            contentStyle: [Tooltip.Content, "z-[100]"],
                            trigger: v => v.ActionButton(
                                [Button.GhostMd, Button.Size.Icon],
                                action: ActionKind.CopyToClipboard,
                                options: new CopyToClipboardActionOptions { Text = meetLink },
                                content: vv => vv.Icon([Icon.Default], name: "clipboard-copy")),
                            contentSlot: v => v.Text([Text.Caption], "Copy link"));
                        row.Tooltip(
                            contentStyle: [Tooltip.Content, "z-[100]"],
                            trigger: v => v.ActionButton(
                                [Button.GhostMd, Button.Size.Icon],
                                action: ActionKind.Share,
                                options: new ShareActionOptions { Title = "Join my meeting", Text = meetLink },
                                content: vv => vv.Icon([Icon.Default], name: "share")),
                            contentSlot: v => v.Text([Text.Caption], "Share link"));
                    });

                    // Buttons
                    col.Row(["justify-end mt-6"], content: row =>
                    {
                        row.Button(
                            [Button.PrimaryMd, "font-sans text-primary-foreground"],
                            label: "Start meeting",
                            href: meetLink);
                    });
                });
            });
    }

    private void RenderEnterNamePage(UIView view)
    {
        var isDark = _currentTheme.Value == Constants.DarkTheme;
        var nameValue = _nameInput.Value.Trim();
        var hasValidName = !string.IsNullOrWhiteSpace(nameValue);

        view.Column(["h-full flex flex-col items-center justify-center gap-6 relative"], content: col =>
        {
            // Theme toggle in upper right
            col.Box(["absolute top-4 right-4"], content: corner =>
            {
                corner.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.Button(
                        [Button.GhostMd, Button.Size.Icon],
                        onClick: ToggleThemeAsync,
                        content: vv => vv.Icon([Icon.Default], name: isDark ? "sun" : "moon")),
                    contentSlot: v => v.Text([Text.Caption], isDark ? "Light mode" : "Dark mode"));
            });

            col.Text(["text-6xl font-bold text-foreground tracking-tight"], "Tori");
            col.Text([Text.Muted, "mt-2"], "Enter your name to join the meeting");

            col.Row(["gap-3 items-center mt-4"], content: row =>
            {
                row.TextField(
                    [Input.Default, "w-64"],
                    placeholder: "Your name",
                    value: _nameInput.Value,
                    props: new Dictionary<string, object?>
                    {
                        ["autoFocus"] = true,
                        ["autoComplete"] = "off",
                        ["data-lpignore"] = "true",
                        ["data-1p-ignore"] = "true",
                        ["data-form-type"] = "other"
                    },
                    onValueChange: async value => _nameInput.Value = value,
                    onSubmit: async _ =>
                    {
                        var currentName = _nameInput.Value.Trim();
                        if (!string.IsNullOrWhiteSpace(currentName))
                        {
                            JoinMeeting(currentName);
                        }
                    });

                row.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.Button(
                        [Button.PrimaryMd, Button.Size.Icon],
                        disabled: !hasValidName,
                        onClick: async () => JoinMeeting(nameValue),
                        content: vv => vv.Icon([Icon.Default], name: "arrow-right")),
                    contentSlot: v => v.Text([Text.Caption], "Join meeting"));
            });

            // Easter egg at the bottom
            col.Box(["absolute bottom-8 left-0 right-0 flex justify-center"], content: bottom =>
            {
                bottom.Text(["text-sm text-muted-foreground/30 select-none"], "Torilla tavataan");
            });
        });
    }

    private void RenderLeftMeetingPage(UIView view)
    {
        var isDark = _currentTheme.Value == Constants.DarkTheme;

        view.Column(["h-full flex flex-col items-center justify-center gap-6 relative"], content: col =>
        {
            // Theme toggle in upper right
            col.Box(["absolute top-4 right-4"], content: corner =>
            {
                corner.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.Button(
                        [Button.GhostMd, Button.Size.Icon],
                        onClick: ToggleThemeAsync,
                        content: vv => vv.Icon([Icon.Default], name: isDark ? "sun" : "moon")),
                    contentSlot: v => v.Text([Text.Caption], isDark ? "Light mode" : "Dark mode"));
            });

            col.Text(["text-6xl font-bold text-foreground tracking-tight"], "Tori");
            col.Text([Text.Muted, "mt-2"], "You left the meeting");

            // Easter egg at the bottom
            col.Box(["absolute bottom-8 left-0 right-0 flex justify-center"], content: bottom =>
            {
                bottom.Text(["text-sm text-muted-foreground/30 select-none"], "Torilla tavataan");
            });
        });
    }

    private void RenderMeetingPage(UIView view)
    {
        _ = _participantsVersion.Value;

        if (IsMobileLayout())
        {
            RenderMobileMeetingPage(view);
        }
        else
        {
            RenderDesktopMeetingPage(view);
        }

        if (_settingsOpen.Value)
        {
            RenderSettingsDialog(view);
        }

        if (_leaveConfirmDialogOpen.Value)
        {
            RenderLeaveConfirmDialog(view);
        }
    }

    private void RenderDesktopMeetingPage(UIView view)
    {
        view.Box(["absolute inset-4 flex flex-row gap-4"], content: wrapper =>
        {
            // Main area (video grid + controls)
            wrapper.Column(["flex-1 min-w-0 flex flex-col gap-4"], content: col =>
            {
                col.Box([Card.Default, "flex-1 min-h-0 p-4 overflow-auto"], content: RenderParticipantGrid);
                col.Row([Card.Default, "h-20 shrink-0 justify-center items-center gap-4 px-4"], content: RenderControlBar);
            });

            // Right panel: Transcript + Chat
            wrapper.Column(["w-96 shrink-0 h-full flex flex-col gap-4"], content: RenderRightPanel);
        });
    }

    private void RenderMobileMeetingPage(UIView view)
    {
        view.Box(["absolute inset-0 flex flex-col p-2 pb-safe"], content: wrapper =>
        {
            // Participant grid (full width, fills remaining space)
            wrapper.Box([Card.Default, "flex-1 min-h-0 p-2 overflow-auto"], content: RenderParticipantGrid);

            // Mobile control bar
            wrapper.Row([Card.Default, "h-14 shrink-0 justify-center items-center gap-1.5 px-2 mt-2"], content: RenderMobileControlBar);
        });

        // Mobile panel overlay (bottom sheet drawer)
        if (_mobilePanelOpen.Value)
        {
            RenderMobilePanel(view);
        }
    }

    private void RenderMobileControlBar(UIView view)
    {
        var isVideoOn = _isVideoEnabled.Value;
        var isAudioOn = _isAudioEnabled.Value;

        // Audio toggle
        view.CaptureButton(
            [Button.OutlineMd, Button.Size.Icon, "w-11 h-11"],
            kind: MediaCaptureKind.Audio,
            label: isAudioOn ? "Mute" : "Unmute",
            captureMode: MediaCaptureButtonMode.Toggle,
            audioOptions: GetAudioCaptureOptions(),
            onCaptureStart: OnAudioCaptureStart,
            onCaptureStop: OnAudioCaptureStop,
            content: vv => vv.Icon([Icon.Default, "w-5 h-5", isAudioOn ? "text-emerald-600 dark:text-success" : "text-danger"], name: isAudioOn ? "mic" : "mic-off"));

        // Video toggle
        view.CaptureButton(
            [Button.OutlineMd, Button.Size.Icon, "w-11 h-11"],
            kind: MediaCaptureKind.Camera,
            label: isVideoOn ? "Camera off" : "Camera on",
            captureMode: MediaCaptureButtonMode.Toggle,
            videoOptions: GetCameraCaptureOptions(),
            onCaptureStart: OnVideoCaptureStart,
            onCaptureStop: OnVideoCaptureStop,
            content: vv => vv.Icon([Icon.Default, "w-5 h-5", isVideoOn ? "text-emerald-600 dark:text-success" : "text-danger"], name: isVideoOn ? "video" : "video-off"));

        // Panel toggle button
        view.Button(
            [Button.OutlineMd, Button.Size.Icon, "w-11 h-11"],
            onClick: async () => _mobilePanelOpen.Value = true,
            content: vv => vv.Icon([Icon.Default], name: "message-square"));

        // Leave meeting button
        view.Button(
            [Button.DangerMd, Button.Size.Icon, "w-11 h-11"],
            onClick: async () => _leaveConfirmDialogOpen.Value = true,
            content: vv => vv.Icon([Icon.Default], name: "phone-off"));

        // Settings button
        view.Button(
            [Button.GhostMd, Button.Size.Icon, "w-10 h-10"],
            onClick: async () => _settingsOpen.Value = true,
            content: vv => vv.Icon([Icon.Default], name: "settings"));
    }

    private void RenderMobilePanel(UIView view)
    {
        _ = _chatMessagesVersion.Value;
        _ = _recognizedSpeechVersion.Value;
        _ = _summaryVersion.Value;

        // Overlay backdrop
        view.Box([Drawer.Overlay], content: overlay =>
        {
            overlay.Button(
                ["absolute inset-0 w-full h-full bg-transparent"],
                onClick: async () => _mobilePanelOpen.Value = false,
                content: _ => { });
        });

        // Bottom sheet drawer with fixed height (60vh) and margins from all edges
        view.Box(["fixed bottom-2 left-2 right-2 z-50 h-[60vh] flex flex-col rounded-xl border border-secondary bg-background shadow-lg"], content: drawer =>
        {
            // Close button in top right corner
            drawer.Button(
                [Button.GhostMd, Button.Size.Icon, "absolute top-1 right-1 h-8 w-8 z-10"],
                onClick: async () => _mobilePanelOpen.Value = false,
                content: v => v.Icon([Icon.Size.Xs], name: "x"));

            // Drag handle
            drawer.Box([Layout.Center, "py-1.5"], content: v =>
            {
                v.Box([Drawer.Handle]);
            });

            // Tab buttons
            drawer.Row(["px-3 py-1 gap-1 bg-muted/50 rounded-lg mx-3 mt-3"], content: tabs =>
            {
                RenderMobilePanelTabButton(tabs, "chat", "Chat");
                RenderMobilePanelTabButton(tabs, "people", "People");
                RenderMobilePanelTabButton(tabs, "summary", "Summary");
                RenderMobilePanelTabButton(tabs, "transcript", "Transcript");
            });

            drawer.Separator([Separator.Horizontal, "mt-2"]);

            // Panel content based on selected tab - fills remaining space
            drawer.Box(["flex-1 min-h-0 overflow-hidden"], content: content =>
            {
                switch (_mobilePanelTab.Value)
                {
                    case "chat":
                        RenderMobileChatContent(content);
                        break;
                    case "people":
                        RenderMobilePeopleContent(content);
                        break;
                    case "transcript":
                        RenderMobileTranscriptContent(content);
                        break;
                    case "summary":
                        RenderMobileSummaryContent(content);
                        break;
                }
            });
        });
    }

    private void RenderMobilePanelTabButton(UIView view, string tabId, string label)
    {
        var isActive = _mobilePanelTab.Value == tabId;
        var activeStyle = isActive
            ? "flex-1 px-2 py-1.5 text-xs font-medium rounded-md bg-background text-foreground shadow-sm"
            : "flex-1 px-2 py-1.5 text-xs font-medium rounded-md text-muted-foreground hover:text-foreground";

        view.Button(
            [activeStyle],
            label: label,
            onClick: async () => _mobilePanelTab.Value = tabId);
    }

    private void RenderMobileChatContent(UIView view)
    {
        view.Column(["h-full flex flex-col"], content: col =>
        {
            // Scrollable messages area
            col.Box(["flex-1 min-h-0 overflow-hidden"], content: v =>
            {
                v.ScrollArea(
                    autoScroll: true,
                    autoScrollKey: $"mobile-chat-{_chatMessagesVersion.Value}",
                    rootStyle: ["h-full"],
                    viewportStyle: ["h-full"],
                    content: scroll =>
                    {
                        scroll.Column(["p-3 gap-2"], content: msgs =>
                        {
                            if (_chatMessages.Value.Count == 0)
                            {
                                msgs.Text([Text.Muted, "text-center py-4"], "No messages yet");
                            }
                            else
                            {
                                foreach (var message in _chatMessages.Value)
                                {
                                    RenderChatMessage(msgs, message);
                                }
                            }
                        });
                    });
            });

            col.Separator([Separator.Horizontal]);

            // Input row with padding at bottom
            col.Row(["px-4 pt-3 pb-4 gap-2"], content: row =>
            {
                row.TextField(
                    [Input.Default, "flex-1 h-9"],
                    placeholder: "Type a message",
                    value: _chatInputText.Value,
                    onValueChange: async value => _chatInputText.Value = value,
                    onSubmit: async _ => await SendChatMessage());

                row.Button(
                    [Button.PrimaryMd, Button.Size.Icon, "h-9 w-9"],
                    disabled: string.IsNullOrWhiteSpace(_chatInputText.Value),
                    onClick: SendChatMessage,
                    content: vv => vv.Icon([Icon.Size.Xs], name: "send"));
            });
        });
    }

    private void RenderMobilePeopleContent(UIView view)
    {
        view.ScrollArea(
            rootStyle: ["h-full"],
            viewportStyle: ["h-full"],
            content: scroll =>
            {
                scroll.Box(["p-3"], content: RenderPeopleContent);
            });
    }

    private void RenderMobileTranscriptContent(UIView view)
    {
        view.ScrollArea(
            rootStyle: ["h-full"],
            viewportStyle: ["h-full"],
            content: scroll =>
            {
                scroll.Box(["p-3"], content: RenderTranscriptContent);
            });
    }

    private void RenderMobileSummaryContent(UIView view)
    {
        view.ScrollArea(
            rootStyle: ["h-full"],
            viewportStyle: ["h-full"],
            content: scroll =>
            {
                scroll.Box(["p-3"], content: RenderSummaryContent);
            });
    }

    private void RenderLeaveConfirmDialog(UIView view)
    {
        view.Dialog(
            open: _leaveConfirmDialogOpen.Value,
            onOpenChange: async open => _leaveConfirmDialogOpen.Value = open ?? false,
            overlayStyle: [AlertDialog.Overlay],
            contentStyle: [AlertDialog.Content, "max-w-sm"],
            contentSlot: content =>
            {
                content.Column([Layout.Column.Md], content: col =>
                {
                    col.Text([Text.H3, "mb-2"], "Leave meeting?");
                    col.Text([Text.Body, "text-muted-foreground mb-6"], "Are you sure you want to leave this meeting?");

                    col.Row(["justify-end gap-3"], content: row =>
                    {
                        row.Button(
                            [Button.OutlineMd],
                            label: "Cancel",
                            onClick: async () => _leaveConfirmDialogOpen.Value = false);
                        row.Button(
                            [Button.DangerMd],
                            label: "Leave",
                            onClick: async () =>
                            {
                                _leaveConfirmDialogOpen.Value = false;
                                await LeaveCallAsync();
                            });
                    });
                });
            });
    }

    private void RenderParticipantGrid(UIView view)
    {
        _ = _speakingVersion.Value;
        var participants = _participants.Value;
        var count = participants.Count;

        if (count == 0)
        {
            view.Box(["flex-1 flex items-center justify-center"], content: v =>
            {
                v.Text([Text.Muted], "Waiting for participants to join");
            });
            return;
        }

        // Check if anyone is screen sharing
        var screenSharer = participants.FirstOrDefault(p => p.IsScreenSharing && p.ScreenShareStreamId != null);

        if (screenSharer != null)
        {
            // Show screen share full-size
            RenderScreenShareView(view, screenSharer);
            return;
        }

        var gridCols = count switch
        {
            1 => "grid-cols-1",
            2 => "grid-cols-1 md:grid-cols-2",
            <= 4 => "grid-cols-2",
            <= 6 => "grid-cols-2 md:grid-cols-3",
            <= 9 => "grid-cols-3",
            _ => "grid-cols-3 md:grid-cols-4"
        };

        view.Grid([$"gap-4 {gridCols} auto-rows-fr h-full"], content: grid =>
        {
            foreach (var participant in participants)
            {
                RenderParticipantTile(grid, participant);
            }
        });
    }

    private void RenderScreenShareView(UIView view, Participant screenSharer)
    {
        view.Box(["relative w-full h-full flex items-center justify-center bg-black rounded-lg overflow-hidden"], content: container =>
        {
            // Screen share video
            if (screenSharer.ScreenShareStreamId != null)
            {
                container.VideoStreamCanvas(
                    ["w-full h-full object-contain"],
                    streamId: screenSharer.ScreenShareStreamId);
            }

            // Presenter name badge at top left
            container.Box(["absolute top-3 left-3"], content: v =>
            {
                v.Row([Card.Default, "h-7 items-center gap-2 px-3 rounded-full bg-background/80"], content: r =>
                {
                    r.Icon([Icon.Size.Sm], name: "monitor");
                    r.Text([Text.Caption], $"{screenSharer.Name} is presenting");
                });
            });
        });
    }

    // Speaking indicator styles - CSS transition for smooth fade in/out
    private const string SpeakingRingBase = "absolute -inset-1 rounded-full transition-all duration-500 ease-out";
    private const string SpeakingRingStatic = SpeakingRingBase + " border-2 border-white/20 dark:border-white/15 opacity-50";
    private const string SpeakingRingPulsing = SpeakingRingBase + " border-[3px] border-emerald-400 opacity-100 " +
        "shadow-[0_0_16px_rgba(52,211,153,0.7)] dark:shadow-[0_0_20px_rgba(52,211,153,0.9)]";

    private void RenderParticipantTile(UIView view, Participant participant)
    {
        var isDark = _currentTheme.Value == Constants.DarkTheme;
        var gradient = GetParticipantGradient(participant.UserId, isDark);
        var isSpeaking = GetIsSpeaking(participant.ClientSessionId);
        var participantKey = participant.ClientSessionId.ToString();

        view.Box([Card.Default, gradient, "relative rounded-lg overflow-hidden min-h-[200px]"], key: $"participant-{participantKey}", content: tile =>
        {
            if (participant.IsVideoEnabled && participant.EchoVideoStreamId != null)
            {
                tile.VideoStreamCanvas(
                    ["absolute inset-0 w-full h-full object-cover"],
                    key: $"video-{participantKey}",
                    streamId: participant.EchoVideoStreamId);
            }
            else
            {
                tile.Box(["absolute inset-0 flex items-center justify-center"], key: $"avatar-wrapper-{participantKey}", content: v =>
                {
                    var initials = GetInitials(participant.Name);

                    v.Box(["relative"], key: $"avatar-container-{participantKey}", content: container =>
                    {
                        container.Box([isSpeaking ? SpeakingRingPulsing : SpeakingRingStatic], key: $"speaking-ring-{participantKey}");

                        container.Box([Avatar.Default, "w-24 h-24 rounded-full flex items-center justify-center bg-background/80 dark:bg-background/60"], key: $"avatar-{participantKey}", content: vv =>
                        {
                            vv.Text([Text.H1], initials, key: $"initials-{participantKey}");
                        });
                    });
                });
            }

            tile.Box(["absolute bottom-3 left-3"], key: $"badge-wrapper-{participantKey}", content: v =>
            {
                v.Row([Card.Default, "h-7 items-center gap-1.5 px-3 rounded-full"], key: $"badge-{participantKey}", content: r =>
                {
                    r.Text([Text.Caption], participant.Name, key: $"name-{participantKey}");

                    if (!participant.IsAudioEnabled)
                    {
                        r.Icon(["text-danger w-3.5 h-3.5 shrink-0"], name: "mic-off", key: $"mic-off-{participantKey}");
                    }
                });
            });
        });
    }

    private void RenderControlBar(UIView view)
    {
        var isVideoOn = _isVideoEnabled.Value;
        var isAudioOn = _isAudioEnabled.Value;
        var isScreenShareOn = _isScreenShareEnabled.Value;
        var isDark = _currentTheme.Value == Constants.DarkTheme;

        // Left spacer for centering
        view.Box(["flex-1"], content: _ => { });

        // Center group - Media controls
        view.Row(["items-center gap-3"], content: center =>
        {
            // Audio toggle
            center.Tooltip(
                contentStyle: [Tooltip.Content],
                trigger: v => v.CaptureButton(
                    [Button.OutlineMd, Button.Size.Icon, "w-14 h-14"],
                    kind: MediaCaptureKind.Audio,
                    label: isAudioOn ? "Unmute" : "Mute",
                    captureMode: MediaCaptureButtonMode.Toggle,
                    audioOptions: GetAudioCaptureOptions(),
                    onCaptureStart: OnAudioCaptureStart,
                    onCaptureStop: OnAudioCaptureStop,
                    content: vv => vv.Icon([Icon.Size.Md, isAudioOn ? "text-emerald-600 dark:text-success" : "text-danger"], name: isAudioOn ? "mic" : "mic-off")),
                contentSlot: v => v.Text([Text.Caption], isAudioOn ? "Mute" : "Unmute"));

            // Video toggle
            center.Tooltip(
                contentStyle: [Tooltip.Content],
                trigger: v => v.CaptureButton(
                    [Button.OutlineMd, Button.Size.Icon, "w-14 h-14"],
                    kind: MediaCaptureKind.Camera,
                    label: isVideoOn ? "Video On" : "Video Off",
                    captureMode: MediaCaptureButtonMode.Toggle,
                    videoOptions: GetCameraCaptureOptions(),
                    onCaptureStart: OnVideoCaptureStart,
                    onCaptureStop: OnVideoCaptureStop,
                    content: vv => vv.Icon([Icon.Size.Md, isVideoOn ? "text-emerald-600 dark:text-success" : "text-danger"], name: isVideoOn ? "video" : "video-off")),
                contentSlot: v => v.Text([Text.Caption], isVideoOn ? "Turn off camera" : "Turn on camera"));

            // Screen share toggle (not available on mobile layout)
            if (!IsMobileLayout())
            {
                center.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.CaptureButton(
                        [Button.OutlineMd, Button.Size.Icon, "w-14 h-14"],
                        kind: MediaCaptureKind.Screen,
                        label: isScreenShareOn ? "Stop Sharing" : "Share Screen",
                        captureMode: MediaCaptureButtonMode.Toggle,
                        videoOptions: GetScreenCaptureOptions(),
                        onCaptureStart: OnScreenShareStart,
                        onCaptureStop: OnScreenShareStop,
                        content: vv => vv.Icon([Icon.Size.Md, isScreenShareOn ? "text-emerald-600 dark:text-success" : "text-danger"], name: isScreenShareOn ? "monitor" : "monitor-off")),
                    contentSlot: v => v.Text([Text.Caption], isScreenShareOn ? "Stop sharing" : "Share screen"));
            }

            // Leave meeting button
            center.Tooltip(
                contentStyle: [Tooltip.Content],
                trigger: v => v.Button(
                    [Button.DangerMd, Button.Size.Icon, "w-14 h-14 ml-3"],
                    onClick: async () => _leaveConfirmDialogOpen.Value = true,
                    content: vv => vv.Icon([Icon.Size.Md], name: "phone-off")),
                contentSlot: v => v.Text([Text.Caption], "Leave meeting"));
        });

        // Right group - Settings and Theme toggle
        view.Row(["flex-1 justify-end items-center gap-2"], content: right =>
        {
            // Dark mode toggle
            right.Tooltip(
                contentStyle: [Tooltip.Content],
                trigger: v => v.Button(
                    [Button.GhostMd, Button.Size.Icon, "w-10 h-10"],
                    onClick: ToggleThemeAsync,
                    content: vv => vv.Icon([Icon.Default], name: isDark ? "sun" : "moon")),
                contentSlot: v => v.Text([Text.Caption], isDark ? "Light mode" : "Dark mode"));

            // Settings button
            right.Tooltip(
                contentStyle: [Tooltip.Content],
                trigger: v => v.Button(
                    [Button.GhostMd, Button.Size.Icon, "w-10 h-10"],
                    onClick: async () => _settingsOpen.Value = true,
                    content: vv => vv.Icon([Icon.Default], name: "settings")),
                contentSlot: v => v.Text([Text.Caption], "Settings"));
        });
    }

    private void RenderRightPanel(UIView view)
    {
        _ = _chatMessagesVersion.Value;
        _ = _recognizedSpeechVersion.Value;
        _ = _summaryVersion.Value;

        // People/Summary/Transcript section (top half)
        view.Column([Card.Default, "flex-1 min-h-0 flex flex-col"], content: col =>
        {
            col.Tabs(
                value: _rightPanelTab.Value,
                onValueChange: async value => _rightPanelTab.Value = value,
                rootStyle: ["flex-1 min-h-0 flex flex-col"],
                listContainerStyle: ["p-3 shrink-0"],
                listStyle: ["w-full inline-flex h-9 items-center justify-center rounded-lg bg-muted p-1"],
                triggerStyle: ["flex-1 inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-sm font-medium text-muted-foreground transition-all focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm"],
                contentContainerStyle: ["flex-1 min-h-0 overflow-hidden"],
                contentStyle: ["h-full"],
                tabs:
                [
                    new TabItem("people", "People", RenderPeopleContent),
                    new TabItem("summary", "Summary", RenderSummaryContent),
                    new TabItem("transcript", "Transcript", RenderTranscriptContent)
                ]);
        });

        // Chat section (bottom half)
        view.Column([Card.Default, "flex-1 min-h-0"], content: col =>
        {
            col.Box(["px-4 py-3"], content: v =>
            {
                v.Text([Text.Label], "Chat");
            });

            col.Separator([Separator.Horizontal]);

            col.Box(["flex-1 overflow-hidden"], content: v =>
            {
                v.ScrollArea(
                    autoScroll: true,
                    autoScrollKey: $"chat-{_chatMessagesVersion.Value}",
                    rootStyle: ["h-full"],
                    viewportStyle: ["h-full"],
                    content: scroll =>
                    {
                        scroll.Column(["p-4 gap-3"], content: msgs =>
                        {
                            if (_chatMessages.Value.Count == 0)
                            {
                                msgs.Text([Text.Muted, "text-center py-8"], "No messages yet");
                            }
                            else
                            {
                                foreach (var message in _chatMessages.Value)
                                {
                                    RenderChatMessage(msgs, message);
                                }
                            }
                        });
                    });
            });

            col.Separator([Separator.Horizontal]);

            col.Row(["px-4 py-3 gap-2"], content: row =>
            {
                row.TextField(
                    [Input.Default, "flex-1"],
                    placeholder: "Type a message",
                    value: _chatInputText.Value,
                    onValueChange: async value => _chatInputText.Value = value,
                    onSubmit: async _ => await SendChatMessage());

                row.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.Button(
                        [Button.PrimaryMd, Button.Size.Icon],
                        disabled: string.IsNullOrWhiteSpace(_chatInputText.Value),
                        onClick: SendChatMessage,
                        content: vv => vv.Icon([Icon.Default], name: "send")),
                    contentSlot: v => v.Text([Text.Caption], "Send message"));
            });
        });
    }

    private void RenderPeopleContent(UIView view)
    {
        var participants = _participants.Value;

        view.Box(["h-full overflow-hidden"], content: box =>
        {
            box.ScrollArea(
                rootStyle: ["h-full"],
                viewportStyle: ["h-full"],
                content: scroll =>
                {
                    scroll.Column(["p-4 gap-2"], content: list =>
                    {
                        if (participants.Count == 0)
                        {
                            list.Box(["flex-1 flex flex-col items-center justify-center py-8 gap-3"], content: empty =>
                            {
                                empty.Icon([Text.Muted, Icon.Size.Lg], name: "users");
                                empty.Text([Text.Muted, "text-center"], "No participants yet");
                            });
                        }
                        else
                        {
                            foreach (var participant in participants)
                            {
                                list.Row(["items-center gap-3 p-2 rounded-lg hover:bg-muted/50"], content: row =>
                                {
                                    var initials = GetInitials(participant.Name);

                                    row.Box([Avatar.Default, "w-10 h-10 rounded-full flex items-center justify-center bg-muted"], content: av =>
                                    {
                                        av.Text([Text.Caption], initials);
                                    });

                                    row.Column(["flex-1 min-w-0"], content: info =>
                                    {
                                        info.Text([Text.BodyStrong, "truncate"], participant.Name);

                                        var statusParts = new List<string>();

                                        if (participant.IsVideoEnabled)
                                        {
                                            statusParts.Add("Video on");
                                        }

                                        if (participant.IsAudioEnabled)
                                        {
                                            statusParts.Add("Audio on");
                                        }

                                        if (participant.IsScreenSharing)
                                        {
                                            statusParts.Add("Presenting");
                                        }

                                        if (statusParts.Count == 0)
                                        {
                                            statusParts.Add("Joined");
                                        }

                                        info.Text([Text.Caption], string.Join(" • ", statusParts));
                                    });

                                    row.Row(["gap-2"], content: icons =>
                                    {
                                        if (participant.IsMobile)
                                        {
                                            icons.Icon(["text-muted-foreground", Icon.Size.Xs], name: "smartphone");
                                        }

                                        var micIcon = participant.IsAudioEnabled ? "mic" : "mic-off";
                                        var micColor = participant.IsAudioEnabled ? "text-emerald-600 dark:text-success" : "text-danger";
                                        icons.Icon([micColor, Icon.Size.Xs], name: micIcon);

                                        var videoIcon = participant.IsVideoEnabled ? "video" : "video-off";
                                        var videoColor = participant.IsVideoEnabled ? "text-emerald-600 dark:text-success" : "text-danger";
                                        icons.Icon([videoColor, Icon.Size.Xs], name: videoIcon);

                                        if (participant.IsScreenSharing)
                                        {
                                            icons.Icon(["text-primary", Icon.Size.Xs], name: "monitor");
                                        }
                                    });
                                });
                            }
                        }
                    });
                });
        });
    }

    private void RenderSummaryContent(UIView view)
    {
        var hasContent = !string.IsNullOrWhiteSpace(_summary.Value);
        var hasSource = _recognizedSpeech.Value.Count > 0 || _chatMessages.Value.Count > 0;
        var isExtracting = _summaryExtracting.Value;

        view.Box(["h-full overflow-hidden relative"], content: box =>
        {
            box.ScrollArea(
                autoScroll: false,
                rootStyle: ["h-full"],
                viewportStyle: ["h-full"],
                content: scroll =>
                {
                    scroll.Column(["p-4 pb-14"], content: content =>
                    {
                        if (!hasContent)
                        {
                            content.Box(["flex-1 flex flex-col items-center justify-center py-8 gap-3"], content: empty =>
                            {
                                empty.Icon([Text.Muted, Icon.Size.Lg], name: "file-text");
                                empty.Text([Text.Muted, "text-center"], "Summary will appear here");
                            });
                        }
                        else
                        {
                            content.Markdown([Text.Body], _summary.Value);
                        }
                    });
                });

            if (hasSource || hasContent)
            {
                box.Row(["absolute bottom-2 right-4 gap-1 rounded-lg backdrop-blur-md p-1"], content: actions =>
                {
                    if (hasSource)
                    {
                        var extractBtnStyle = isExtracting
                            ? "h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors opacity-50"
                            : "h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors";
                        actions.Tooltip(
                            contentStyle: [Tooltip.Content],
                            trigger: v => v.Button(
                                [extractBtnStyle],
                                disabled: isExtracting,
                                onClick: async () => _ = ExtractSummaryAsync(force: true),
                                content: vv => vv.Icon(["text-foreground", Icon.Size.Sm], name: "sparkles")),
                            contentSlot: v => v.Text([Text.Caption], isExtracting ? "Generating..." : "Generate summary"));
                    }

                    if (hasContent)
                    {
                        actions.Tooltip(
                            contentStyle: [Tooltip.Content],
                            trigger: v => v.ActionButton(
                                ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                                action: ActionKind.CopyToClipboard,
                                options: new CopyToClipboardActionOptions { Text = _summary.Value },
                                content: vv => vv.Icon(["text-foreground", Icon.Size.Sm], name: "clipboard-copy")),
                            contentSlot: v => v.Text([Text.Caption], "Copy summary"));

                        actions.Tooltip(
                            contentStyle: [Tooltip.Content],
                            trigger: v => v.ActionButton(
                                ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                                action: ActionKind.Share,
                                options: new ShareActionOptions { Title = "Meeting Summary", Text = _summary.Value },
                                content: vv => vv.Icon(["text-foreground", Icon.Size.Sm], name: "share")),
                            contentSlot: v => v.Text([Text.Caption], "Share summary"));
                    }
                });
            }
        });
    }

    private void RenderTranscriptContent(UIView view)
    {
        var transcriptText = GetTranscriptAsText();
        var hasContent = _recognizedSpeech.Value.Count > 0;

        view.Box(["h-full overflow-hidden relative"], content: box =>
        {
            box.ScrollArea(
                autoScroll: true,
                autoScrollKey: $"transcript-{_recognizedSpeechVersion.Value}",
                rootStyle: ["h-full"],
                viewportStyle: ["h-full"],
                content: scroll =>
                {
                    scroll.Column(["p-4 pb-14 gap-3"], content: msgs =>
                    {
                        if (!hasContent)
                        {
                            msgs.Box(["flex-1 flex flex-col items-center justify-center py-8 gap-3"], content: empty =>
                            {
                                empty.Icon([Text.Muted, Icon.Size.Lg], name: "message-square-text");
                                empty.Text([Text.Muted, "text-center"], "Transcript will appear here");
                            });
                        }
                        else
                        {
                            foreach (var entry in _recognizedSpeech.Value)
                            {
                                var timeDisplay = FormatTimeInClientTimezone(entry.Timestamp);
                                msgs.Column(["gap-0.5"], content: col =>
                                {
                                    col.Row(["gap-2 items-baseline"], content: row =>
                                    {
                                        row.Text([Text.Label], entry.ParticipantName);
                                        row.Text([Text.Caption], timeDisplay);
                                    });
                                    col.Text([Text.Body], entry.Text);
                                });
                            }
                        }
                    });
                });

            if (hasContent)
            {
                box.Row(["absolute bottom-2 right-4 gap-1 rounded-lg backdrop-blur-md p-1"], content: actions =>
                {
                    actions.Tooltip(
                        contentStyle: [Tooltip.Content],
                        trigger: v => v.ActionButton(
                            ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                            action: ActionKind.CopyToClipboard,
                            options: new CopyToClipboardActionOptions { Text = transcriptText },
                            content: vv => vv.Icon(["text-foreground", Icon.Size.Sm], name: "clipboard-copy")),
                        contentSlot: v => v.Text([Text.Caption], "Copy transcript"));

                    actions.Tooltip(
                        contentStyle: [Tooltip.Content],
                        trigger: v => v.ActionButton(
                            ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                            action: ActionKind.Share,
                            options: new ShareActionOptions { Title = "Transcript", Text = transcriptText },
                            content: vv => vv.Icon(["text-foreground", Icon.Size.Sm], name: "share")),
                        contentSlot: v => v.Text([Text.Caption], "Share transcript"));
                });
            }
        });
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
    }

    private ClientAudioCaptureOptions GetAudioCaptureOptions()
    {
        var microphoneId = _selectedMicrophoneId.Value;
        var deviceId = string.IsNullOrEmpty(microphoneId) || microphoneId == "default" ? null : microphoneId;

        return new ClientAudioCaptureOptions
        {
            Bitrate = GetAudioBitrate(_audioQuality.Value),
            EchoCancellation = _audioEchoCancellation.Value,
            NoiseSuppression = _audioNoiseSuppression.Value,
            AutoGainControl = _audioAutoGainControl.Value,
            TargetIds = [app.ClientContext.SessionId],
            DeviceId = deviceId
        };
    }

    private static int GetAudioBitrate(string quality) => quality switch
    {
        "very_low" => 8000,   // minimum intelligible speech
        "low" => 16000,
        "medium" => 32000,
        "high" => 64000,
        "ultra" => 128000,    // near CD quality
        _ => 32000
    };

    private ClientVideoCaptureOptions GetCameraCaptureOptions()
    {
        var (width, height) = ParseResolution(_cameraResolution.Value);
        var framerate = int.TryParse(_cameraFramerate.Value, out var fps) ? fps : 30;
        var codec = Enum.TryParse<ClientVideoCaptureCodec>(_cameraCodec.Value, out var c) ? c : ClientVideoCaptureCodec.H264;
        var bitrate = GetVideoBitrate(height, _cameraQuality.Value, codec);
        var cameraId = _selectedCameraId.Value;
        var deviceId = string.IsNullOrEmpty(cameraId) || cameraId == "default" ? null : cameraId;

        return new ClientVideoCaptureOptions
        {
            Width = width,
            Height = height,
            Framerate = framerate,
            Bitrate = bitrate,
            PreferredCodecs = [codec],
            TargetIds = [app.ClientContext.SessionId],
            DeviceId = deviceId
        };
    }

    private ClientVideoCaptureOptions GetScreenCaptureOptions()
    {
        var framerate = int.TryParse(_screenFramerate.Value, out var fps) ? fps : 30;
        var codec = Enum.TryParse<ClientVideoCaptureCodec>(_screenCodec.Value, out var c) ? c : ClientVideoCaptureCodec.H264;
        var bitrate = GetVideoBitrate(1080, _screenQuality.Value, codec);

        return new ClientVideoCaptureOptions
        {
            Framerate = framerate,
            Bitrate = bitrate,
            PreferredCodecs = [codec],
            TargetIds = [app.ClientContext.SessionId]
        };
    }

    private static int GetVideoBitrate(int height, string quality, ClientVideoCaptureCodec codec)
    {
        // Base bitrates for H.264 (in bps) based on resolution and quality
        // Very Low: minimum usable for constrained networks
        // Low: suitable for bandwidth-constrained situations
        // Medium: good balance of quality and bandwidth
        // High: best quality for most use cases
        // Ultra: maximum quality for high-bandwidth scenarios
        var baseBitrate = (height, quality) switch
        {
            (<= 480, "very_low") => 250_000,
            (<= 480, "low") => 500_000,
            (<= 480, "medium") => 1_000_000,
            (<= 480, "high") => 1_500_000,
            (<= 480, "ultra") => 2_500_000,
            (<= 720, "very_low") => 800_000,
            (<= 720, "low") => 1_500_000,
            (<= 720, "medium") => 2_500_000,
            (<= 720, "high") => 4_000_000,
            (<= 720, "ultra") => 6_000_000,
            (<= 1080, "very_low") => 1_500_000,
            (<= 1080, "low") => 3_000_000,
            (<= 1080, "medium") => 5_000_000,
            (<= 1080, "high") => 8_000_000,
            (<= 1080, "ultra") => 12_000_000,
            (_, "very_low") => 3_000_000,   // 1440p+
            (_, "low") => 6_000_000,
            (_, "medium") => 10_000_000,
            (_, "high") => 16_000_000,
            (_, "ultra") => 25_000_000,
            _ => 2_500_000                   // fallback to 720p medium
        };

        // Apply codec efficiency multiplier
        // More efficient codecs can achieve same quality at lower bitrates
        var multiplier = codec switch
        {
            ClientVideoCaptureCodec.H264 => 1.0,   // baseline
            ClientVideoCaptureCodec.Vp8 => 0.95,   // slightly better than H.264
            ClientVideoCaptureCodec.Vp9 => 0.65,   // ~35% more efficient than H.264
            ClientVideoCaptureCodec.Av1 => 0.45,   // ~55% more efficient than H.264
            _ => 1.0
        };

        return (int)(baseBitrate * multiplier);
    }

    private static (int Width, int Height) ParseResolution(string resolution)
    {
        var parts = resolution.Split('x');

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var width) &&
            int.TryParse(parts[1], out var height))
        {
            return (width, height);
        }

        return (1280, 720);
    }
}
