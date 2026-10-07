using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class SlackConnectorGuideExamples
{
    private static Task ProcessAsync(SlackMessage message) => Task.CompletedTask;

    private static Task SaveBotTokenAsync(string teamId, string botToken) => Task.CompletedTask;

    public void SlackClient(string botToken)
    {
        #region example:connectors-slack-client
        var slack = new Slack(botToken);
        #endregion

        Log.Instance.Debug($"{slack}");
    }

    public async Task SlackPostAsync(Slack slack, string rootTs)
    {
        #region example:connectors-slack-post
        var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
        #endregion

        Log.Instance.Debug($"{posted}");
    }

    public async Task SlackBlocksAsync(Slack slack, string channelId)
    {
        #region example:connectors-slack-blocks
        var content = new SlackMessageContent("Deploy 412 finished")
        {
            Blocks =
            [
                SlackBlock.Header("Deploy 412 finished"),
                new SlackSectionBlock(SlackText.Markdown("*api* is live on `prod`"))
                {
                    Accessory = new SlackButton("Roll back") { ActionId = "rollback", Value = "412", Style = SlackButtonStyle.Danger },
                },
                SlackBlock.Context(SlackText.Markdown("Started by <@U0123456789>")),
            ],
        };

        var posted = await slack.PostAsync(channelId, content, new SlackPostOptions { UnfurlLinks = false });
        #endregion

        Log.Instance.Debug($"{posted}");
    }

    public async Task SlackResponseUrlAsync(string responseUrl)
    {
        #region example:connectors-slack-response-url
        await SlackResponseUrl.SendAsync(responseUrl, "Rolled back to 411", SlackResponseType.InChannel, replaceOriginal: true);
        #endregion
    }

    public SlackMessageContent SlackReport()
    {
        #region example:connectors-slack-report
        var content = new SlackMessageContent("Weekly report")
        {
            Blocks =
            [
                new SlackContainerBlock(
                [
                    new SlackTableBlock(
                    [
                        [new SlackTextCell("Team"), new SlackTextCell("Closed")],
                        [new SlackTextCell("Platform"), new SlackTextCell("42")],
                    ])
                    {
                        ColumnSettings = [null, new SlackColumnSetting { Align = SlackColumnAlign.Right }],
                    },
                ])
                {
                    Title = "Tickets this week",
                    Width = SlackContainerWidth.Wide,
                    IsCollapsible = true,
                },
                new SlackDataTableBlock("Open tickets",
                [
                    [new SlackTextCell("Ticket"), new SlackTextCell("Age (days)"), new SlackTextCell("")],
                    [new SlackTextCell("DCW-1024"), new SlackNumberCell(3, "3"), new SlackActionCell(new SlackButton("Close") { ActionId = "close", Value = "DCW-1024" })],
                ])
                { PageSize = 10 },
                // SlackLineChart and SlackAreaChart take the same series and axis.
                new SlackDataVisualizationBlock("Tickets by day", new SlackBarChart(
                    [new SlackChartSeries("Opened", [new("Mon", 4), new("Tue", 7)]), new SlackChartSeries("Closed", [new("Mon", 3), new("Tue", 9)])],
                    new SlackChartAxis(["Mon", "Tue"]) { YLabel = "Tickets" })),
                new SlackDataVisualizationBlock("By severity", new SlackPieChart([new("High", 3), new("Low", 11)])),
                new SlackCarouselBlock(
                [
                    new SlackCardBlock
                    {
                        SlackIcon = new SlackIcon("bug"),
                        HeroImage = new SlackImageElement("https://ikon.live/screens/1024.png", "Screenshot of the failure"),
                        Title = SlackText.Plain("DCW-1024"),
                        Body = SlackText.Markdown("Login fails on *Safari*"),
                        Actions = [new SlackButton("Open") { ActionId = "open", Value = "DCW-1024", Style = SlackButtonStyle.Primary }],
                    },
                ]),
            ],
        };
        #endregion

        return content;
    }

    public SlackRichTextBlock SlackRichText()
    {
        #region example:connectors-slack-rich-text
        var notes = new SlackRichTextBlock(
        [
            new SlackRichTextSection(
            [
                new SlackRichTextRun("Release ") { Style = new SlackTextStyle { Bold = true } },
                new SlackRichTextLink("https://ikon.live/releases/412", "412"),
                new SlackRichTextRun(" is out, "),
                new SlackRichTextBroadcast(SlackBroadcastRange.Here),
                new SlackRichTextEmoji("tada"),
            ]),
            new SlackRichTextList(SlackListStyle.Bullet,
            [
                new SlackRichTextSection([new SlackRichTextRun("Reviewed by "), new SlackRichTextUser("U0123456789")]),
                new SlackRichTextSection([new SlackRichTextRun("Owned by "), new SlackRichTextUsergroup("S0123456789")]),
                new SlackRichTextSection([new SlackRichTextRun("Discussed in "), new SlackRichTextChannel("C0123456789")]),
            ]),
            new SlackRichTextQuote([new SlackRichTextRun("Ships at "), new SlackRichTextDate(1790000000, "{date_short_pretty} at {time}", "soon")]),
            new SlackRichTextPreformatted([new SlackRichTextRun("ikon deploy --target prod")]) { Language = "bash" },
            new SlackRichTextSection([new SlackRichTextRun("Brand colour "), new SlackRichTextColor("#F405B3")]),
        ]);
        #endregion

        return notes;
    }

    public IReadOnlyList<SlackBlock> SlackForm()
    {
        #region example:connectors-slack-form
        var priority = new SlackOption(SlackText.Plain("High"), "high");

        SlackBlock[] form =
        [
            new SlackInputBlock("Title", new SlackPlainTextInput { ActionId = "title", MaxLength = 120 }) { BlockId = "title" },
            new SlackInputBlock("Details", new SlackRichTextInput { ActionId = "details", MaxLines = 6 }) { BlockId = "details", Optional = true },
            new SlackInputBlock("Priority", new SlackStaticSelect("Pick one")
            {
                ActionId = "priority",
                OptionGroups = [new SlackOptionGroup("Urgent", [priority]), new SlackOptionGroup("Later", [new SlackOption(SlackText.Plain("Low"), "low")])],
                InitialOption = priority,
            })
            { BlockId = "priority" },
            new SlackInputBlock("Labels", new SlackMultiStaticSelect { ActionId = "labels", Options = [new SlackOption(SlackText.Plain("Bug"), "bug")] }) { BlockId = "labels", Optional = true },
            new SlackInputBlock("Customer", new SlackExternalSelect("Search customers") { ActionId = "customer", MinQueryLength = 2 }) { BlockId = "customer" },
            new SlackInputBlock("Related", new SlackMultiExternalSelect { ActionId = "related" }) { BlockId = "related", Optional = true },
            new SlackInputBlock("Assignee", new SlackUsersSelect { ActionId = "assignee" }) { BlockId = "assignee" },
            new SlackInputBlock("Watchers", new SlackMultiUsersSelect { ActionId = "watchers", MaxSelectedItems = 5 }) { BlockId = "watchers", Optional = true },
            new SlackInputBlock("Post to", new SlackConversationsSelect
            {
                ActionId = "post_to",
                DefaultToCurrentConversation = true,
                ResponseUrlEnabled = true,
                Filter = new SlackConversationFilter(["public", "private"]) { ExcludeBotUsers = true },
            })
            { BlockId = "post_to" },
            new SlackInputBlock("Also notify", new SlackMultiConversationsSelect { ActionId = "notify" }) { BlockId = "notify", Optional = true },
            new SlackInputBlock("Channel", new SlackChannelsSelect { ActionId = "channel" }) { BlockId = "channel", Optional = true },
            new SlackInputBlock("Channels", new SlackMultiChannelsSelect { ActionId = "channels" }) { BlockId = "channels", Optional = true },
            new SlackInputBlock("Due", new SlackDatePicker { ActionId = "due" }) { BlockId = "due" },
            new SlackInputBlock("At", new SlackTimePicker("09:00") { ActionId = "at" }) { BlockId = "at", Optional = true },
            new SlackInputBlock("Remind", new SlackDateTimePicker { ActionId = "remind" }) { BlockId = "remind", Optional = true },
            new SlackInputBlock("Estimate (h)", new SlackNumberInput(IsDecimalAllowed: true) { ActionId = "estimate", MinValue = "0" }) { BlockId = "estimate", Optional = true },
            new SlackInputBlock("Reporter email", new SlackEmailInput { ActionId = "email" }) { BlockId = "email", Optional = true },
            new SlackInputBlock("Link", new SlackUrlInput { ActionId = "link" }) { BlockId = "link", Optional = true },
            new SlackInputBlock("Screenshots", new SlackFileInput(["png", "jpg"]) { ActionId = "screenshots", MaxFiles = 3 }) { BlockId = "screenshots", Optional = true },
            new SlackInputBlock("Notify", new SlackCheckboxes([new SlackOption(SlackText.Markdown("*Email* me"), "email")]) { ActionId = "notify_me" }) { BlockId = "notify_me", Optional = true },
            new SlackInputBlock("Visibility", new SlackRadioButtons([new SlackOption(SlackText.Plain("Team"), "team")]) { ActionId = "visibility" }) { BlockId = "visibility" },
            new SlackInputBlock("Search", new SlackPlainTextInput
            {
                ActionId = "search",
                DispatchActionConfig = new SlackDispatchActionConfig(["on_character_entered"]),
            })
            { BlockId = "search", DispatchAction = true, Optional = true },
            new SlackSectionBlock(SlackText.Markdown("More"))
            {
                Accessory = new SlackOverflow([new SlackOption(SlackText.Plain("Open docs"), "docs") { Url = "https://ikon.live/docs" }]) { ActionId = "more" },
            },
            new SlackImageBlock("https://ikon.live/logo.png", "Ikon logo"),
            new SlackVideoBlock("Walkthrough", "https://ikon.live/embed/intro", "https://ikon.live/intro.jpg", "Intro video"),
            new SlackActionsBlock(
            [
                new SlackWorkflowButton("Escalate", new SlackWorkflow(new SlackWorkflowTrigger("https://slack.com/shortcuts/Ft0123ABC456/xyz")
                {
                    CustomizableInputParameters = [new SlackWorkflowInput("ticket", "DCW-1024")],
                }))
                { ActionId = "escalate" },
            ]),
        ];
        #endregion

        return form;
    }

    public IReadOnlyList<SlackBlock> SlackAgentReply(string answer)
    {
        #region example:connectors-slack-agent-blocks
        var details = new SlackRichTextBlock([new SlackRichTextSection([new SlackRichTextRun("Read 14 tickets")])]);

        SlackBlock[] reply =
        [
            new SlackPlanBlock("Looking into the outage",
            [
                new SlackPlanTask("search", "Searched the incident channel", SlackTaskStatus.Complete) { Output = details },
                new SlackPlanTask("draft", "Drafting a summary", SlackTaskStatus.InProgress),
            ]),
            new SlackTaskCardBlock("sources", "Checked the status page", SlackTaskStatus.Complete)
            {
                Sources = [new SlackUrlSource("https://status.ikon.live", "status.ikon.live")],
            },
            SlackBlock.Markdown(answer),
            new SlackContextActionsBlock(
            [
                new SlackFeedbackButtons(new SlackFeedbackButton("Good", "good"), new SlackFeedbackButton("Bad", "bad")) { ActionId = "feedback" },
                new SlackIconButton("trash", "Delete") { ActionId = "delete" },
            ]),
        ];

        // In a modal, an alert block draws attention to a notice.
        SlackBlock notice = new SlackAlertBlock(SlackText.Plain("This answer was drafted by an agent")) { Level = SlackAlertLevel.Warning };
        #endregion

        return [.. reply, notice];
    }

    public async Task SlackDirectMessageAsync(Slack slack, string email)
    {
        #region example:connectors-slack-dm
        if (await slack.LookupUserByEmailAsync(email) is { IsDeleted: false } person)
        {
            var dm = await slack.OpenDirectMessageAsync([person.Id]);
            await slack.PostAsync(dm, $"Hi {person.DisplayName ?? person.RealName}, your report is ready");
        }
        #endregion
    }

    public async Task SlackUploadAsync(Slack slack, string channelId, string threadTs, string path)
    {
        #region example:connectors-slack-upload
        await using var content = File.OpenRead(path);

        var file = await slack.UploadFileAsync(
            new SlackFileUpload(Path.GetFileName(path), content, content.Length) { Title = "Monthly report" },
            new SlackFileShare(channelId) { ThreadTs = threadTs, InitialComment = "Here is this month's report" });
        #endregion

        Log.Instance.Debug($"{file.Permalink}");
    }

    public async Task SlackHistoryAsync(Slack slack, string channelId, string lastSeenTs)
    {
        #region example:connectors-slack-history
        var messages = await slack.HistorySinceAsync(channelId, oldestTs: lastSeenTs);

        foreach (var message in messages)
        {
            await ProcessAsync(message);
            lastSeenTs = message.Ts;   // safe: oldest-first means no gap on interruption
        }
        #endregion
    }

    public async Task SlackSocketAsync(Slack slack, string appToken, CancellationToken stoppingToken)
    {
        #region example:connectors-slack-socket
        var socket = new SlackSocketClient(appToken, async (request, ct) =>   // xapp-..., not the xoxb- bot token
        {
            switch (request)
            {
                case SlackSocketEvent { Callback.Event: SlackAppMentionEvent mention }:
                    await slack.PostAsync(mention.Message.Channel, "On it", mention.Message.ThreadTs ?? mention.Message.Ts, ct);
                    return null;

                case SlackSocketSlashCommand { Command.Command: "/status" }:
                    return SlackAck.Message("All systems go");

                default:
                    return null;
            }
        });

        await socket.RunAsync(stoppingToken);
        #endregion
    }

    private readonly string _signingSecret = "";
    private readonly Slack _slack = new("xoxb-example");

    private bool Verified(Ikon.App.HttpRequest request) =>
        SlackAuth.VerifyRequest(_signingSecret, request.Headers.GetValueOrDefault("X-Slack-Request-Timestamp"), request.Headers.GetValueOrDefault("X-Slack-Signature"), request.Body);

    private static Task CreateTicketAsync(string title, string userId) => Task.CompletedTask;

    private static IReadOnlyList<SlackOption> FindCustomers(string typed) => [new SlackOption(SlackText.Plain(typed), typed)];

    #region example:connectors-slack-events
    [HttpPost("/slack/events", Auth = EndpointAuth.Public)]
    public HttpResult OnSlackEvent(Ikon.App.HttpRequest request)
    {
        if (!SlackAuth.VerifyRequest(_signingSecret, request.Headers.GetValueOrDefault("X-Slack-Request-Timestamp"),
                request.Headers.GetValueOrDefault("X-Slack-Signature"), request.Body))
        {
            return HttpResult.Unauthorized();
        }

        switch (SlackEvents.Parse(request.Body))
        {
            case SlackUrlVerification verification:
                return HttpResult.Text(verification.Challenge);

            case SlackEventCallback { Event: SlackAppMentionEvent mention }:
                // Slack wants its answer within three seconds, so the reply is posted after it.
                _ = Task.Run(() => _slack.PostAsync(mention.Message.Channel, "On it", mention.Message.ThreadTs ?? mention.Message.Ts));
                break;

            case SlackEventCallback { Event: SlackAppHomeOpenedEvent { Tab: "home" } home }:
                _ = Task.Run(() => _slack.PublishHomeAsync(home.User, SlackView.Home([SlackBlock.Header("Your tickets")])));
                break;
        }

        return HttpResult.Ok();
    }
    #endregion

    public async Task SlackStreamReplyAsync(Slack slack, SlackAppMentionEvent mention)
    {
        #region example:connectors-slack-stream
        var run = Emerge.Run<string>(LLMModel.Claude46Sonnet, pass => pass.Command = mention.Message.Text);
        var feedback = new SlackContextActionsBlock(
            [new SlackFeedbackButtons(new SlackFeedbackButton("Good", "good"), new SlackFeedbackButton("Bad", "bad")) { ActionId = "feedback" }]);

        await slack.StreamAsync(
            mention.Message.Channel,
            run.OfType<ModelText<string>>().Select(token => token.Text),
            new SlackStreamOptions { ThreadTs = mention.Message.ThreadTs ?? mention.Message.Ts },
            new SlackStreamStop { Blocks = [feedback] });
        #endregion
    }

    public async Task SlackStreamStepsAsync(Slack slack, string channel, string threadTs)
    {
        #region example:connectors-slack-stream-steps
        await using var stream = await slack.StartStreamAsync(channel, new SlackStreamOptions { ThreadTs = threadTs, TaskDisplayMode = SlackTaskDisplayMode.Plan });

        await stream.AppendAsync(SlackStreamChunk.Plan("Looking into the outage"));
        await stream.AppendAsync(new SlackTaskUpdateChunk("logs", "Reading the error logs", SlackTaskStatus.InProgress));
        // ... the work ...
        await stream.AppendAsync(new SlackTaskUpdateChunk("logs", "Read the error logs", SlackTaskStatus.Complete) { Output = "412 errors since 09:14" });
        await stream.AppendAsync(new SlackBlocksChunk([SlackBlock.Divider()]));
        await stream.AppendMarkdownAsync("The **api** deploy at 09:12 broke sign-in; rolling back fixes it.");

        await stream.StopAsync(new SlackStreamStop { SessionStatus = SlackSessionStatus.Active });
        #endregion
    }

    public async Task SlackAgentSessionAsync(Slack slack, SlackAssistantThreadEvent thread)
    {
        #region example:connectors-slack-agent-session
        await slack.SetSuggestedPromptsAsync(thread.Channel,
        [
            new SlackSuggestedPrompt("Summarise", "Summarise this channel"),
            new SlackSuggestedPrompt("Open tickets", "Which tickets are still open?"),
        ]);

        await slack.SetAgentStatusAsync(SlackSessionStatus.Processing, thread.Channel, thread.ThreadTs, title: "Ticket review");
        await slack.RenameAgentSessionAsync("Ticket review: platform", thread.Channel, thread.ThreadTs);
        #endregion
    }

    #region example:connectors-slack-event-kinds
    private static string Describe(SlackEnvelope? envelope) => envelope switch
    {
        SlackAppRateLimited limited => $"Slack paused events for {limited.TeamId} this minute",
        SlackEventCallback { Event: var slackEvent } => slackEvent switch
        {
            SlackMessageEvent { Message.BotId: not null } => "a bot's post, perhaps the app's own",
            SlackMessageEvent { ChannelType: "im" } message => $"DM from {message.Message.User}: {message.Message.Text}",
            SlackMessageEvent message => $"{message.Message.User} in {message.Message.Channel}",
            SlackMessageChangedEvent edit => $"edited from '{edit.Previous?.Text}' to '{edit.Message.Text}'",
            SlackMessageDeletedEvent deleted => $"{deleted.DeletedTs} deleted",
            SlackReactionEvent { Added: true } reaction => $":{reaction.Reaction}: on {reaction.Ts}",
            SlackPinEvent pin => $"{pin.Ts} {(pin.Added ? "pinned" : "unpinned")}",
            SlackMemberChannelEvent member => $"{member.User} {(member.Joined ? "joined" : "left")} {member.Channel}",
            SlackChannelEvent channel => $"{channel.Type}: {channel.Name ?? channel.Channel}",
            SlackFileEvent file => $"{file.Type} {file.FileId}",
            SlackLinkSharedEvent link => $"unfurl {string.Join(", ", link.Links.Select(shared => shared.Url))}",
            SlackUserEvent person => $"{person.Type}: {person.User.Name}",
            SlackAppUninstalledEvent or SlackTokensRevokedEvent => "forget this workspace's tokens",
            SlackAssistantThreadEvent thread => $"assistant thread {thread.ThreadTs}, looking at {thread.ContextChannel}",
            SlackAppContextChangedEvent context => $"viewing {context.Entities.FirstOrDefault()?.Value}",
            SlackAgentSessionStoppedEvent stopped => $"stop streaming {string.Join(", ", stopped.StreamingMessageTs)}",
            SlackAgentSessionTitleChangedEvent renamed => $"session renamed to {renamed.Title}",
            SlackUnknownEvent other => $"{other.Type}, read from Raw",
            _ => slackEvent.Type,
        },
        _ => "nothing to do",
    };

    private static string Describe(SlackInteraction interaction) => interaction switch
    {
        SlackShortcut shortcut => $"global shortcut {shortcut.CallbackId}",
        SlackMessageShortcut shortcut => $"{shortcut.CallbackId} on '{shortcut.Message.Text}'",
        SlackViewClosed closed => $"{closed.View.CallbackId} dismissed",
        SlackUnknownInteraction other => $"{other.Type}, read from Raw",
        _ => interaction.Type,
    };

    private static string Describe(SlackSocketRequest request) => request switch
    {
        SlackSocketEvent socketEvent => Describe(socketEvent.Callback),
        SlackSocketInteraction socketInteraction => Describe(socketInteraction.Interaction),
        SlackSocketSlashCommand socketCommand => socketCommand.Command.Command,
        _ => request.EnvelopeId,
    };
    #endregion

    #region example:connectors-slack-interactivity
    [HttpPost("/slack/interactivity", Auth = EndpointAuth.Public)]
    public HttpResult OnSlackInteraction(Ikon.App.HttpRequest request)
    {
        if (!Verified(request))
        {
            return HttpResult.Unauthorized();
        }

        switch (SlackInteractions.Parse(request.Body))
        {
            case SlackViewSubmission { View.CallbackId: "new_ticket" } submission:
                var title = submission.View.State.Get("title", "title")?.Value;

                if (string.IsNullOrWhiteSpace(title))
                {
                    return HttpResult.Json(SlackAck.ViewErrors(new Dictionary<string, string> { ["title"] = "Give the ticket a title" }));
                }

                _ = Task.Run(() => CreateTicketAsync(title, submission.User.Id));
                return HttpResult.Ok();   // an empty 200 closes the modal

            case SlackBlockActions { Actions: [{ ActionId: "rollback" } action], ResponseUrl: { } responseUrl }:
                _ = Task.Run(() => SlackResponseUrl.SendAsync(responseUrl, $"Rolling back {action.Value.Value}", replaceOriginal: true));
                return HttpResult.Ok();

            case SlackBlockSuggestion { ActionId: "customer" } suggestion:
                return HttpResult.Json(SlackAck.Options(FindCustomers(suggestion.Value)));

            default:
                return HttpResult.Ok();
        }
    }
    #endregion

    #region example:connectors-slack-command
    [HttpPost("/slack/commands", Auth = EndpointAuth.Public)]
    public async Task<HttpResult> OnSlackCommandAsync(Ikon.App.HttpRequest request)
    {
        if (!Verified(request))
        {
            return HttpResult.Unauthorized();
        }

        var command = SlackSlashCommand.Parse(request.Body);

        if (command.Command != "/ticket")
        {
            return HttpResult.Json(SlackAck.Message($"I don't know {command.Command}"));
        }

        // A trigger id works once, within three seconds: open the modal before anything slow.
        var form = new SlackInputBlock("Title", new SlackPlainTextInput { ActionId = "title" }) { BlockId = "title" };
        await _slack.OpenViewAsync(command.TriggerId, SlackView.Modal("New ticket", [form], submit: "Create") with { CallbackId = "new_ticket" });
        return HttpResult.Ok();
    }
    #endregion

    public async Task SlackInstallAsync(string clientId, string clientSecret, string redirectUri, string state, string code)
    {
        #region example:connectors-slack-install
        var installUrl = SlackAuth.AuthorizeUrl(clientId, ["channels:history", "chat:write"], redirectUri, state);

        // ... a workspace member installs the app; the redirect back carries `code`:
        var installation = await SlackAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri);
        await SaveBotTokenAsync(installation.TeamId, installation.BotToken);
        #endregion

        Log.Instance.Debug($"{installUrl}");
    }

    private static Task SaveBotTokensAsync(string teamId, SlackTokens tokens, CancellationToken ct) => Task.CompletedTask;

    public void SlackRotation(SlackInstallation installation, string clientId, string clientSecret)
    {
        #region example:connectors-slack-rotation
        var tokens = new SlackTokenProvider(installation.BotTokens, clientId, clientSecret,
            onTokensRotated: (rotated, ct) => SaveBotTokensAsync(installation.TeamId, rotated, ct));
        var slack = new Slack(tokens);
        #endregion

        Log.Instance.Debug($"{slack}");
    }

    public bool SlackVerify(string signingSecret, string? timestampHeader, string? signatureHeader, string body)
    {
        #region example:connectors-slack-verify
        if (!SlackAuth.VerifyRequest(signingSecret, timestampHeader, signatureHeader, body))
        {
            return false;   // not Slack's, or replayed: answer 401 and do nothing
        }
        #endregion

        return true;
    }
}
