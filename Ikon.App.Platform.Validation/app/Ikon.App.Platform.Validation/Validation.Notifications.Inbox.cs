using System.Globalization;

public partial class Validation
{
    private const int NotificationInboxShown = 20;
    private const string NotificationSelfTestKind = "validation-selftest";

    private static readonly string[] NotificationMutableChannels = [NotificationInbox.PushChannel, "email", "sms"];

    private readonly NotificationInbox _notificationInbox = new(app, key: "validation.notifications.inbox")
    {
        MaxPushPerWindow = 20,
        PushWindow = TimeSpan.FromMinutes(10),
    };

    // Channel addresses exist only for the duration of the one send a person asked for, keyed
    // "channel:userId". The registered channels resolve through here, so with nothing entered they
    // have no address and cannot deliver anywhere.
    private readonly ConcurrentDictionary<string, string> _notifChannelTargets = new();
    private readonly object _notifChannelsLock = new();
    private bool _notifChannelsRegistered;

    private readonly ClientReactive<string> _notifPriority = new(nameof(NotificationPriority.Normal));
    private readonly ClientReactive<bool> _notifWithActions = new(true);
    private readonly ClientReactive<bool> _notifAllDevices = new(false);
    private readonly ClientReactive<string> _notifTag = new("");
    private readonly ClientReactive<string> _notifLaunchUrl = new("/notifications");
    private readonly ClientReactive<string> _notifSendResult = new("—");
    private readonly ClientReactive<string> _notifActionTapped = new("—");
    private int _notifActionSends;

    private readonly ClientReactive<string> _notifInboxStatus = new("");
    private readonly ClientReactive<string> _notifSelfTest = new("—");

    private readonly ClientReactive<string> _notifEmailTo = new("");
    private readonly ClientReactive<string> _notifSmsTo = new("");
    private readonly ClientReactive<string> _notifTelegramToken = new("");
    private readonly ClientReactive<string> _notifTelegramChatId = new("");
    private readonly ClientReactive<string> _notifWhatsAppToken = new("");
    private readonly ClientReactive<string> _notifWhatsAppPhoneNumberId = new("");
    private readonly ClientReactive<string> _notifWhatsAppTo = new("");
    private readonly ClientReactive<string> _notifChannelResult = new("—");

    private void RenderNotificationSendCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Send");

            view.Row([Layout.Row.Md, "flex-wrap items-center mb-4"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Check permission", icon: "bell", props: TestId("notif-permission-check"),
                    onClick: async () =>
                    {
                        var permission = await app.Notifications.GetPermissionAsync(ReactiveScope.ClientId);
                        _notificationPermission.Value = permission.ToString();
                    });
                view.Text([Text.Body], $"Permission: {_notificationPermission.Value}", props: TestId("notif-permission"));
            });

            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Priority");
                    view.Select(options: Enum.GetNames<NotificationPriority>().Select(n => new SelectOption(n, n)).ToList(), bind: _notifPriority, ariaLabel: "Notification priority");
                });
                view.TextField([Input.Default, "w-48"], bind: _notifTag, label: "Tag", placeholder: "none", props: TestId("notif-tag"));
                view.TextField([Input.Default, "w-48"], bind: _notifLaunchUrl, label: "Launch URL", placeholder: "none", props: TestId("notif-launch-url"));
            });
            view.Row([Layout.Row.Md, "flex-wrap items-center mb-4"], content: view =>
            {
                RenderDeviceToggle(view, _notifWithActions, "Action buttons", "notif-with-actions");
                RenderDeviceToggle(view, _notifAllDevices, "All devices", "notif-all-devices");
            });
            view.Text([Text.Label, "mb-2"], "Direct");
            view.Row([Layout.Row.Md, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "This session", icon: "bell", props: TestId("notif-session"),
                    onClick: () => SendNotificationAsync("session", async content => [await app.Notifications.SendToSessionAsync(ReactiveScope.ClientId, content)]));
                view.Button([Button.PrimaryMd], text: "All sessions", icon: "bell-ring", props: TestId("notif-broadcast"),
                    onClick: () => SendNotificationAsync("broadcast", content => app.Notifications.BroadcastAsync(content)));
                view.Button([Button.PrimaryMd], text: "My user", icon: "user", props: TestId("notif-send-options"),
                    onClick: SendNotificationToUserAsync);
            });
            view.Text([Text.Label, "mb-2"], "Through the inbox, with the delivery policy");
            view.Row([Layout.Row.Md, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "My inbox", icon: "inbox", props: TestId("notif-send-route"),
                    onClick: SendNotificationThroughInboxAsync);
            });
            view.Text([Text.Body], _notifSendResult.Value, props: TestId("notif-send-result"));
            view.Text([Text.Body, "mt-1"], $"Action tapped: {_notifActionTapped.Value}", props: TestId("notif-action-tapped"));
        });
    }

    private void RenderNotificationInboxCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Inbox");

            var items = _notificationInbox.Items;
            view.Text([Text.Body, "mb-3"], $"Unread: {_notificationInbox.UnreadCount} of {items.Count}", props: TestId("notif-inbox-unread"));

            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Add test item", icon: "plus", props: TestId("notif-inbox-add"),
                    onClick: AddNotificationInboxItemAsync);
                view.Button([Button.PrimaryMd], text: "Mark all read", icon: "check-check", props: TestId("notif-inbox-mark-all-read"),
                    onClick: async () =>
                    {
                        _notificationInbox.MarkAllRead();
                        _notifInboxStatus.Value = $"Marked all read (unread now {_notificationInbox.UnreadCount})";
                    });
                view.Button([Button.ErrorMd], text: "Clear inbox", icon: "trash-2", props: TestId("notif-inbox-clear"),
                    onClick: async () =>
                    {
                        _notificationInbox.Clear();
                        _notifInboxStatus.Value = "Cleared";
                    });
            });

            if (!string.IsNullOrEmpty(_notifInboxStatus.Value))
            {
                view.Text([Text.Caption, "mb-2"], _notifInboxStatus.Value, props: TestId("notif-inbox-status"));
            }

            if (items.Count == 0)
            {
                view.Text([Text.Caption], "Empty", props: TestId("notif-inbox-empty"));
            }

            foreach (var item in items.Take(NotificationInboxShown))
            {
                view.Row([Layout.Row.Md, "flex-wrap items-center border-b border-secondary py-1"], key: item.Id, content: view =>
                {
                    view.Text([item.Read ? Text.Caption : Text.BodyStrong, "flex-1"],
                        $"{(item.Read ? "read" : "UNREAD")} · {item.Title}{(item.Body is { Length: > 0 } body ? $" — {body}" : "")} · {item.Kind ?? "-"} · {item.CreatedAt:HH:mm:ss}Z",
                        props: TestId("notif-inbox-item"));

                    if (!item.Read)
                    {
                        view.Button([Button.PrimarySm], text: "Mark read", onClick: async () => _notificationInbox.MarkRead(item.Id));
                    }

                    view.Button([Button.ErrorSm], text: "Remove", onClick: async () => _notificationInbox.Remove(item.Id));
                });
            }

            if (items.Count > NotificationInboxShown)
            {
                view.Text([Text.Caption, "mt-2"], $"…and {items.Count - NotificationInboxShown} older.");
            }

            view.Row([Layout.Row.Md, "flex-wrap mt-4 mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Run self-test", icon: "flask-conical", props: TestId("notif-inbox-selftest-run"),
                    onClick: async () =>
                    {
                        _notifSelfTest.Value = "Running…";
                        _notifSelfTest.Value = await RunNotificationInboxSelfTestAsync();
                    });
            });
            view.Text([Text.Body], _notifSelfTest.Value, props: TestId("notif-inbox-selftest"));
        });
    }

    private void RenderNotificationPolicyCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Delivery policy");

            var muted = _notificationInbox.Muted;
            view.Text([Text.Body, "mb-2"], $"Muted: {(muted.Count == 0 ? "(none)" : string.Join(", ", muted))}", props: TestId("notif-inbox-muted"));
            view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
            {
                foreach (var channel in NotificationMutableChannels)
                {
                    bool isMuted = _notificationInbox.IsMuted(channel);
                    view.Button([isMuted ? Button.PrimarySm : Button.OutlineSm], text: isMuted ? $"Unmute {channel}" : $"Mute {channel}",
                        props: TestId($"notif-mute-{channel}"),
                        onClick: async () => _notificationInbox.Mute(channel, !isMuted));
                }
            });

            var quiet = _notificationInbox.QuietHours;
            string quietText = quiet == null
                ? "Quiet hours: none"
                : $"Quiet hours (UTC): {quiet.StartUtc:HH\\:mm}–{quiet.EndUtc:HH\\:mm}, {(quiet.Contains(TimeOnly.FromDateTime(DateTime.UtcNow)) ? "active now" : "not active now")}";
            view.Text([Text.Body, "mb-2"], quietText, props: TestId("notif-inbox-quiet"));
            view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Quiet hours around now (±1 h)", icon: "moon", props: TestId("notif-quiet-set"),
                    onClick: async () =>
                    {
                        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
                        _notificationInbox.SetQuietHours(now.AddHours(-1), now.AddHours(1));
                    });
                view.Button([Button.PrimaryMd], text: "Clear quiet hours", icon: "sun", props: TestId("notif-quiet-clear"),
                    onClick: async () => _notificationInbox.ClearQuietHours());
            });

            view.Text([Text.Body], $"Push cap: {_notificationInbox.MaxPushPerWindow} per {_notificationInbox.PushWindow.TotalMinutes.ToString(CultureInfo.InvariantCulture)} min", props: TestId("notif-push-cap"));
        });
    }

    private void RenderNotificationChannelsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Channels");

            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.TextField([Input.Default, "w-72"], bind: _notifEmailTo, label: "Email address", type: "email", placeholder: "you@example.com", props: TestId("notif-email-to"));
                view.Button([Button.PrimaryMd], text: "Send email", icon: "mail", props: TestId("notif-channel-email"),
                    onClick: () => SendNotificationOnChannelAsync("email", _notifEmailTo.Value));
            });
            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.TextField([Input.Default, "w-72"], bind: _notifSmsTo, label: "Phone number", type: "tel", placeholder: "+358401234567", props: TestId("notif-sms-to"));
                view.Button([Button.PrimaryMd], text: "Send SMS", icon: "message-square", props: TestId("notif-channel-sms"),
                    onClick: () => SendNotificationOnChannelAsync("sms", _notifSmsTo.Value));
            });
            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.TextField([Input.Default, "w-72"], bind: _notifTelegramToken, label: "Telegram bot token", type: "password", props: TestId("notif-telegram-token"));
                view.TextField([Input.Default, "w-48"], bind: _notifTelegramChatId, label: "Chat id", props: TestId("notif-telegram-chat"));
                view.Button([Button.PrimaryMd], text: "Send Telegram", icon: "send", props: TestId("notif-channel-telegram"),
                    onClick: SendNotificationToTelegramAsync);
            });
            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.TextField([Input.Default, "w-72"], bind: _notifWhatsAppToken, label: "WhatsApp Cloud API token", type: "password", props: TestId("notif-whatsapp-token"));
                view.TextField([Input.Default, "w-48"], bind: _notifWhatsAppPhoneNumberId, label: "Phone number id", props: TestId("notif-whatsapp-phone-id"));
                view.TextField([Input.Default, "w-48"], bind: _notifWhatsAppTo, label: "WhatsApp number", type: "tel", props: TestId("notif-whatsapp-to"));
                view.Button([Button.PrimaryMd], text: "Send WhatsApp", icon: "send", props: TestId("notif-channel-whatsapp"),
                    onClick: SendNotificationToWhatsAppAsync);
            });
            view.Text([Text.Body], _notifChannelResult.Value, props: TestId("notif-channel-result"));
        });
    }

    private void EnsureNotificationChannels()
    {
        lock (_notifChannelsLock)
        {
            if (_notifChannelsRegistered)
            {
                return;
            }

            _notificationInbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => NotificationChannelTarget("email", userId), senderDisplayName: "Ikon Validation"));
            _notificationInbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => NotificationChannelTarget("sms", userId)));
            _notificationInbox.Channels.Add(new TelegramNotificationChannel("", userId => NotificationChannelTarget("telegram", userId)));
            _notificationInbox.Channels.Add(new WhatsAppNotificationChannel("", "", userId => NotificationChannelTarget("whatsapp", userId)));
            _notifChannelsRegistered = true;
        }
    }

    private string? NotificationChannelTarget(string channel, string userId)
        => _notifChannelTargets.TryGetValue($"{channel}:{userId}", out var target) ? target : null;

    private NotificationContent BuildOptionsNotification()
    {
        var priority = Enum.TryParse<NotificationPriority>(_notifPriority.Value, out var parsed) ? parsed : NotificationPriority.Normal;
        IReadOnlyList<NotificationAction>? actions = _notifWithActions.Value
            ? [NotificationActionFor("acknowledge", "Acknowledge"), NotificationActionFor("snooze", "Snooze")]
            : null;

        return new NotificationContent(
            "Validation",
            $"{priority} priority, sent {DateTime.UtcNow:HH:mm:ss}Z",
            Tag: string.IsNullOrWhiteSpace(_notifTag.Value) ? null : _notifTag.Value.Trim(),
            LaunchUrl: string.IsNullOrWhiteSpace(_notifLaunchUrl.Value) ? null : _notifLaunchUrl.Value.Trim(),
            Data: "{\"source\":\"validation-options\"}",
            Priority: priority,
            Actions: actions);
    }

    // Each send gets its own number in the action paths, so tapping the same button on a later
    // notification still changes the URL and is reported again.
    private NotificationAction NotificationActionFor(string id, string title)
        => new(id, title, $"/notifications?notif-action={id}&send={Interlocked.Increment(ref _notifActionSends)}");

    private void RecordNotificationActionTap(int clientSessionId, string url)
    {
        int queryIndex = url.IndexOf('?');

        if (queryIndex < 0)
        {
            return;
        }

        string? action = System.Web.HttpUtility.ParseQueryString(url[(queryIndex + 1)..])["notif-action"];

        if (!string.IsNullOrEmpty(action))
        {
            _notifActionTapped.SetFor(clientSessionId, $"{action} at {DateTime.UtcNow:HH:mm:ss}Z");
        }
    }

    private async Task SendNotificationAsync(string target, Func<NotificationContent, Task<IReadOnlyList<NotificationSendResult>>> send)
    {
        try
        {
            var results = await send(BuildOptionsNotification());
            _notifSendResult.Value = $"Sent ({target}): {FormatNotificationResults(results)}";

            if (results.FirstOrDefault(r => r.SessionId == ReactiveScope.ClientId) is { } own)
            {
                _notificationPermission.Value = own.Permission.ToString();
            }
        }
        catch (Exception ex)
        {
            _notifSendResult.Value = $"Error: {target} send failed: {ex.Message}";
        }
    }

    private async Task SendNotificationToUserAsync()
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _notifSendResult.Value = "Error: no user id in this session";
            return;
        }

        var reach = _notifAllDevices.Value ? NotificationReach.AllDevices : NotificationReach.ConnectedFirst;
        await SendNotificationAsync(reach.ToString(), content => app.Notifications.SendToUserAsync(userId, content, reach));
    }

    private async Task SendNotificationThroughInboxAsync()
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _notifSendResult.Value = "Error: no user id in this session";
            return;
        }

        var route = _notifAllDevices.Value ? NotificationRoute.AllDevices : NotificationRoute.Default;

        try
        {
            var outcome = await _notificationInbox.NotifyAsync(userId, BuildOptionsNotification(), kind: "validation", route: route);
            _notifSendResult.Value = $"Sent (inbox): {FormatNotificationOutcome(outcome)}";
        }
        catch (Exception ex)
        {
            _notifSendResult.Value = $"Error: inbox send failed: {ex.Message}";
        }
    }

    private async Task AddNotificationInboxItemAsync()
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _notifInboxStatus.Value = "Error: no user id in this session";
            return;
        }

        try
        {
            var outcome = await _notificationInbox.NotifyAsync(userId,
                new NotificationContent("Inbox test", $"Added {DateTime.UtcNow:HH:mm:ss}Z", LaunchUrl: "/notifications"),
                kind: "validation", route: NotificationRoute.Silent);
            _notifInboxStatus.Value = $"Added item {ShortId(outcome.Item?.Id)} (unread now {_notificationInbox.UnreadCountFor(userId)})";
        }
        catch (Exception ex)
        {
            _notifInboxStatus.Value = $"Error: add failed: {ex.Message}";
        }
    }

    private async Task SendNotificationOnChannelAsync(string channel, string address)
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _notifChannelResult.Value = "Error: no user id in this session";
            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            _notifChannelResult.Value = $"{channel}: enter your own address first — nothing was sent";
            return;
        }

        EnsureNotificationChannels();
        string key = $"{channel}:{userId}";
        _notifChannelTargets[key] = address.Trim();

        try
        {
            var outcome = await _notificationInbox.NotifyAsync(userId,
                new NotificationContent("Validation", $"Channel test ({channel}) sent {DateTime.UtcNow:HH:mm:ss}Z"),
                kind: "validation", route: NotificationRoute.Silent.With(channel));
            _notifChannelResult.Value = $"{channel}: {FormatNotificationOutcome(outcome)}";
        }
        catch (Exception ex)
        {
            _notifChannelResult.Value = $"Error: {channel} send failed: {ex.Message}";
        }
        finally
        {
            _notifChannelTargets.TryRemove(key, out _);
        }
    }

    private async Task SendNotificationToTelegramAsync()
    {
        string token = _notifTelegramToken.Value.Trim();
        string chatId = _notifTelegramChatId.Value.Trim();

        if (token.Length == 0 || chatId.Length == 0)
        {
            _notifChannelResult.Value = "telegram: enter your bot token and chat id first — nothing was sent";
            return;
        }

        await SendOnOneOffChannelAsync(new TelegramNotificationChannel(token, _ => chatId));
    }

    private async Task SendNotificationToWhatsAppAsync()
    {
        string token = _notifWhatsAppToken.Value.Trim();
        string phoneNumberId = _notifWhatsAppPhoneNumberId.Value.Trim();
        string to = _notifWhatsAppTo.Value.Trim();

        if (token.Length == 0 || phoneNumberId.Length == 0 || to.Length == 0)
        {
            _notifChannelResult.Value = "whatsapp: enter your token, phone number id and number first — nothing was sent";
            return;
        }

        await SendOnOneOffChannelAsync(new WhatsAppNotificationChannel(token, phoneNumberId, _ => to));
    }

    // A person's own credentials build a channel for this one send; it is never added to the inbox,
    // so nothing else can route through it.
    private async Task SendOnOneOffChannelAsync(INotificationChannel channel)
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            _notifChannelResult.Value = "Error: no user id in this session";
            return;
        }

        try
        {
            bool sent = await channel.SendAsync(userId, new NotificationContent("Validation", $"Channel test ({channel.Name}) sent {DateTime.UtcNow:HH:mm:ss}Z", LaunchUrl: "/notifications"), CancellationToken.None);
            _notifChannelResult.Value = sent ? $"{channel.Name}: sent (true)" : $"{channel.Name}: not sent (false)";
        }
        catch (Exception ex)
        {
            _notifChannelResult.Value = $"Error: {channel.Name} send failed: {ex.Message}";
        }
    }

    private async Task<string> RunNotificationInboxSelfTestAsync()
    {
        string? userId = ReactiveScope.UserIdOrNull;

        if (string.IsNullOrEmpty(userId))
        {
            return "FAIL inbox: no user id in this session";
        }

        EnsureNotificationChannels();

        var created = new List<string>();
        var quietBefore = _notificationInbox.QuietHoursFor(userId);
        bool pushMutedBefore = _notificationInbox.IsMuted(NotificationInbox.PushChannel);
        string tag = $"validation-selftest-{Guid.NewGuid():N}";
        string step = "notify";

        async Task<NotificationOutcome> NotifyAsync(string body, NotificationPriority priority, NotificationRoute route, string? itemTag = null)
        {
            var outcome = await _notificationInbox.NotifyAsync(userId,
                new NotificationContent("Validation self-test", body, Tag: itemTag, Priority: priority),
                kind: NotificationSelfTestKind, route: route);

            if (outcome.Item != null)
            {
                created.Add(outcome.Item.Id);
            }

            return outcome;
        }

        try
        {
            int before = _notificationInbox.UnreadCountFor(userId);

            var first = await NotifyAsync("First item", NotificationPriority.Normal, NotificationRoute.Silent, tag);

            if (first.Item == null)
            {
                return "FAIL inbox: notify: no inbox item was recorded";
            }

            if (!_notificationInbox.ItemsFor(userId).Any(i => i.Id == first.Item.Id && !i.Read))
            {
                return "FAIL inbox: notify: the item is not in the inbox as unread";
            }

            int afterNotify = _notificationInbox.UnreadCountFor(userId);

            if (afterNotify != before + 1)
            {
                return $"FAIL inbox: notify: unread went {before}→{afterNotify}, expected {before + 1}";
            }

            step = "mark read";
            _notificationInbox.MarkReadFor(userId, first.Item.Id);
            int afterRead = _notificationInbox.UnreadCountFor(userId);

            if (!_notificationInbox.ItemsFor(userId).Any(i => i.Id == first.Item.Id && i.Read) || afterRead != before)
            {
                return $"FAIL inbox: mark read: unread is {afterRead}, expected {before}";
            }

            step = "tag collapse";
            var second = await NotifyAsync("Replaces the first", NotificationPriority.Normal, NotificationRoute.Silent, tag);
            var tagged = _notificationInbox.ItemsFor(userId).Where(i => i.Tag == tag).ToList();

            if (tagged.Count != 1 || tagged[0].Id != second.Item?.Id)
            {
                return $"FAIL inbox: tag collapse: {tagged.Count} items carry the tag, expected only the second";
            }

            _notificationInbox.MarkReadFor(userId, second.Item!.Id);

            step = "low priority";
            var low = await NotifyAsync("Low priority stays in the inbox", NotificationPriority.Low, NotificationRoute.Default);

            if (!low.Skipped.Contains(NotificationInbox.PushChannel) || low.PushResults.Count != 0 || low.Delivered.Count != 0)
            {
                return $"FAIL inbox: low priority: {FormatNotificationOutcome(low)}";
            }

            step = "channels";
            var channels = await NotifyAsync("Channels", NotificationPriority.Normal, NotificationRoute.Silent.With("telegram", "whatsapp", SelfTestDeliberate));

            if (!new[] { "telegram", "whatsapp", SelfTestDeliberate }.All(channels.Skipped.Contains) || channels.Delivered.Count != 0 || channels.Failed.Count != 0)
            {
                return $"FAIL inbox: channels: {FormatNotificationOutcome(channels)}";
            }

            step = "quiet hours";
            _notificationInbox.Mute(NotificationInbox.PushChannel, false);
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            _notificationInbox.SetQuietHoursFor(userId, now.AddHours(-1), now.AddHours(1));
            var quiet = await NotifyAsync("Held by quiet hours", NotificationPriority.Normal, NotificationRoute.Default);

            if (!quiet.Skipped.Contains(NotificationInbox.PushChannel) || quiet.PushResults.Count != 0)
            {
                return $"FAIL inbox: quiet hours: {FormatNotificationOutcome(quiet)}";
            }

            step = "high priority";
            var high = await NotifyAsync("High priority bypasses quiet hours", NotificationPriority.High, NotificationRoute.Default, $"{tag}-high");

            if (high.PushResults.Count == 0)
            {
                return $"FAIL inbox: high priority: no push was attempted inside quiet hours: {FormatNotificationOutcome(high)}";
            }

            step = "mute";
            _notificationInbox.MuteFor(userId, NotificationInbox.PushChannel);
            var muted = await NotifyAsync("Muted push", NotificationPriority.High, NotificationRoute.Default);

            if (!muted.Skipped.Contains(NotificationInbox.PushChannel) || muted.PushResults.Count != 0)
            {
                return $"FAIL inbox: mute: {FormatNotificationOutcome(muted)}";
            }

            return $"PASS inbox: unread {before}→{afterNotify}→{afterRead}, tag collapse, low stays in inbox, unconfigured and unknown channels skipped, quiet hours hold push, high bypasses quiet hours ({high.PushResults.Count} push row(s)), mute beats high";
        }
        catch (Exception ex)
        {
            return $"FAIL inbox: {step}: {ex.Message}";
        }
        finally
        {
            if (quietBefore == null)
            {
                _notificationInbox.ClearQuietHoursFor(userId);
            }
            else
            {
                _notificationInbox.SetQuietHoursFor(userId, quietBefore.StartUtc, quietBefore.EndUtc);
            }

            _notificationInbox.MuteFor(userId, NotificationInbox.PushChannel, pushMutedBefore);

            foreach (var id in created)
            {
                _notificationInbox.Remove(id);
            }
        }
    }

    private static string FormatNotificationOutcome(NotificationOutcome outcome)
        => $"item {ShortId(outcome.Item?.Id)}, delivered [{string.Join(", ", outcome.Delivered)}], skipped [{string.Join(", ", outcome.Skipped)}], failed [{string.Join(", ", outcome.Failed)}]; push: {FormatNotificationResults(outcome.PushResults)}";

    private static string FormatNotificationResults(IReadOnlyList<NotificationSendResult> results)
    {
        if (results.Count == 0)
        {
            return "no rows";
        }

        return string.Join("; ", results.Select(r => r.Channel == NotificationSendChannel.OfflinePush
            ? $"offline push: {(r.Delivered ? "accepted" : "refused")}{(r.Error != null ? $" ({r.Error})" : "")}"
            : $"session {r.SessionId}: {(r.Delivered ? "delivered" : "not delivered")} (permission: {r.Permission})"));
    }

    private static string ShortId(string? id) => id == null ? "(none)" : id[..Math.Min(8, id.Length)];
}
