using Ikon.Connectors;
using Ikon.Connectors.Google;

namespace Ikon.App.Patterns.Examples;

file sealed class GoogleConnectorGuideExamples
{
    private static Task SaveSignInAsync(string state, PkceCodes pkce) => Task.CompletedTask;

    private static Task<PkceCodes> LoadSignInAsync(string state) => Task.FromResult(PkceCodes.Create());

    private static Task SaveRefreshTokenAsync(string refreshToken, CancellationToken ct) => Task.CompletedTask;

    private static Task IngestMailAsync(string messageId) => Task.CompletedTask;

    private static Task ForgetMailAsync(string messageId) => Task.CompletedTask;

    public void GoogleClients(string clientId, string clientSecret, string refreshToken)
    {
        #region example:connectors-google-clients
        var tokens = new GoogleTokenProvider(new GoogleCredentials(clientId, clientSecret, refreshToken), onRefreshTokenRotated: SaveRefreshTokenAsync);
        var drive = new Drive(tokens);
        var gmail = new Gmail(tokens);
        #endregion
    }

    public async Task GoogleSignInAsync(string clientId, string clientSecret, string redirectUri, string state, string code)
    {
        #region example:connectors-google-signin
        var pkce = PkceCodes.Create();
        await SaveSignInAsync(state, pkce);
        var signInUrl = GoogleAuth.AuthorizeUrl(clientId, redirectUri, ["https://www.googleapis.com/auth/gmail.readonly"], state, pkce.Challenge);

        // ... the person consents; the redirect back carries `code` and `state`:
        var verifier = (await LoadSignInAsync(state)).Verifier;
        var credentials = await GoogleAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri, codeVerifier: verifier);
        var tokens = new GoogleTokenProvider(credentials);

        if (!(await tokens.GetGrantedScopesAsync()).Contains("https://www.googleapis.com/auth/gmail.readonly"))
        {
            Log.Instance.Info("Mail access was not granted; ask again or carry on without it");
        }
        #endregion

        Log.Instance.Debug($"{signInUrl}");
    }

    public async Task DriveTransferAsync(Drive drive, string folderId)
    {
        #region example:connectors-drive-transfer
        await using var content = File.OpenRead("./report.pdf");
        var uploaded = await drive.UploadAsync(folderId, "report.pdf", "application/pdf", content, content.Length);

        await using var download = await drive.DownloadAsync(uploaded.Id);
        #endregion
    }

    public async Task DriveExportAsync(Drive drive, DriveFile file)
    {
        #region example:connectors-drive-export
        if (file.MimeType == "application/vnd.google-apps.document")
        {
            await using var exported = await drive.ExportAsync(file.Id, "text/markdown");
            using var reader = new StreamReader(exported);
            var markdown = await reader.ReadToEndAsync();
        }
        #endregion
    }

    public async Task DriveListAsync(Drive drive, string folderId)
    {
        #region example:connectors-drive-list
        foreach (var file in await drive.ListChildrenAsync(folderId))
        {
            Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
        }

        var budgets = await drive.SearchAsync(DriveQuery.And(
            DriveQuery.FullTextContains("budget"),
            DriveQuery.ModifiedAfter(DateTimeOffset.UtcNow.AddDays(-30)),
            DriveQuery.NotTrashed));
        #endregion

        Log.Instance.Debug($"{budgets.Count}");
    }

    public async Task DriveShareAsync(Drive drive, string fileId)
    {
        #region example:connectors-drive-share
        await drive.AddPermissionAsync(fileId, NewDrivePermission.User("ada@example.com", DriveRole.Commenter) with { EmailMessage = "Your thoughts by Friday?" });

        foreach (var permission in await drive.ListPermissionsAsync(fileId))
        {
            if (permission.Type == DrivePermissionType.Anyone && !permission.Inherited)
            {
                await drive.RemovePermissionAsync(fileId, permission.Id);   // close the public link
            }
        }
        #endregion
    }

    public async Task DriveWatchAsync(Drive drive, GoogleWatches watches, string notificationUrl, string channelToken, IReadOnlyDictionary<string, string> headers, string storedToken)
    {
        #region example:connectors-drive-watch
        var start = (await drive.DeltaAsync(fromNow: true)).DeltaToken;
        var channel = await watches.CreateAsync(WatchTarget.DriveChanges(start), notificationUrl, channelToken);

        // In the [HttpPost] endpoint at notificationUrl:
        if (GoogleNotifications.ParseChannel(headers, channelToken) is { ResourceState: "change" })
        {
            var delta = await drive.DeltaAsync(storedToken);   // the notification only says that something changed
        }
        #endregion

        Log.Instance.Debug($"{channel.ExpiresAt}");
    }

    public async Task DriveActivityAsync(DriveActivity activity, string folderId)
    {
        #region example:connectors-drive-activity
        foreach (var activityEvent in await activity.QueryFolderAsync(folderId, since: DateTimeOffset.UtcNow.AddDays(-7)))
        {
            Log.Instance.Info($"{activityEvent.Time:u} {activityEvent.Action}: {string.Join(", ", activityEvent.TargetTitles)}");
        }
        #endregion
    }

    public async Task GmailAsync(Gmail gmail, string bodyText)
    {
        #region example:connectors-gmail-read
        var unread = await gmail.ListMessagesPageAsync("is:unread", limit: 10);

        foreach (var summary in unread.Items)
        {
            var message = await gmail.GetMessageAsync(summary.Id);
            Log.Instance.Info($"{message.From}: {message.Subject} ({message.Attachments.Count} attachments)");
        }

        var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
        #endregion

        Log.Instance.Debug($"{sentId}");
    }

    public async Task GmailSendAsync(Gmail gmail, string messageId, byte[] reportPdf)
    {
        #region example:connectors-gmail-send
        await gmail.SendAsync(new NewGmailMessage("Q3 report")
        {
            To = ["grace@example.com"],
            Cc = ["finance@example.com"],
            Html = "<p>The Q3 report is attached.</p>",
            Attachments = [new NewGmailAttachment("q3.pdf", "application/pdf", reportPdf)],
        });

        // Into the original's thread, to its sender, with "Re:" before its subject:
        await gmail.ReplyAsync(messageId, new NewGmailMessage("") { Text = "Thanks, received." });
        #endregion
    }

    public async Task GmailOrganiseAsync(Gmail gmail)
    {
        #region example:connectors-gmail-organise
        var clients = (await gmail.ListLabelsAsync()).FirstOrDefault(label => label.Name == "Clients")
            ?? await gmail.CreateLabelAsync("Clients");

        var fromClients = await gmail.ListMessagesAsync("from:@client.example is:unread");
        await gmail.ModifyLabelsAsync([.. fromClients.Select(message => message.Id)], addLabelIds: [clients.Id], removeLabelIds: ["UNREAD"]);
        #endregion
    }

    public async Task GmailWatchAsync(Gmail gmail, GooglePubSubVerifier verifier, string? authorizationHeader, string body, string storedToken)
    {
        #region example:connectors-gmail-watch
        await gmail.WatchAsync("projects/my-project/topics/gmail");   // again daily: a watch lasts a week

        // In the [HttpPost] endpoint the Pub/Sub push subscription posts to:
        if (await verifier.VerifyAsync(authorizationHeader)
            && GoogleNotifications.ParsePubSub(body) is { } message
            && GoogleNotifications.ParseGmail(message) is { } notification)
        {
            var delta = await gmail.MessagesDeltaAsync(storedToken);   // read what changed
        }
        #endregion
    }

    public async Task CalendarMeetingAsync(GoogleCalendar calendar)
    {
        #region example:connectors-calendar-meeting
        var helsinki = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
        var free = await calendar.FindFreeTimeAsync(
            ["primary", "grace@example.com"], TimeSpan.FromMinutes(45),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
            workdayStart: new TimeOnly(9, 0), workdayEnd: new TimeOnly(17, 0), timeZone: helsinki);

        if (free.FirstOrDefault() is { } slot)
        {
            var meeting = await calendar.CreateEventAsync("primary", new NewCalendarEvent("Plan review", CalendarTime.At(slot.Start), CalendarTime.At(slot.Start.AddMinutes(45)))
            {
                Attendees = ["grace@example.com"],
                WithMeet = true,
            }, notify: CalendarNotify.All);

            Log.Instance.Info($"Booked {meeting.Start?.DateTime:u}, join at {meeting.MeetLink}");
        }
        #endregion
    }

    public async Task CalendarDeltaAsync(GoogleCalendar calendar, string? storedToken)
    {
        #region example:connectors-calendar-delta
        CalendarEventDelta delta;

        try
        {
            delta = await calendar.EventsDeltaAsync("primary", storedToken);
        }
        catch (ConnectorException ex) when (ex.IsResyncRequired)
        {
            delta = await calendar.EventsDeltaAsync("primary");   // the token expired: read the calendar again
        }

        foreach (var changed in delta.Items)
        {
            Log.Instance.Info(changed.Deleted ? $"cancelled: {changed.Id}" : $"{changed.Summary} at {changed.Start?.DateTime:u}");
        }

        storedToken = delta.DeltaToken;   // store it for the next call
        #endregion
    }

    public async Task DocsMarkdownAsync(GoogleDocs docs, string documentId)
    {
        #region example:connectors-docs-markdown
        var markdown = await docs.GetMarkdownAsync(documentId);

        await docs.AppendMarkdownAsync(documentId, """
            ## Decisions

            - Ship the **new API** in August
            - Review [the spec](https://example.com/spec) first
            """);
        #endregion

        Log.Instance.Debug(markdown);
    }

    public async Task SheetsValuesAsync(GoogleSheets sheets, string spreadsheetId)
    {
        #region example:connectors-sheets-values
        var budget = await sheets.GetValuesAsync(spreadsheetId, "Budget!A1:C50");

        foreach (var row in budget.Rows.Skip(1))
        {
            Log.Instance.Info($"{row[0]}: {row.ElementAtOrDefault(1)}");
        }

        await sheets.AppendRowsAsync(spreadsheetId, "Budget", [["Laptop", 1200, "=B2*1.24"]]);
        #endregion
    }

    public async Task SlidesTemplateAsync(Drive drive, GoogleSlides slides, string templateId, string customer)
    {
        #region example:connectors-slides-template
        var copy = await drive.CopyAsync(templateId, newName: $"Proposal for {customer}");

        await slides.ReplaceAllTextAsync(copy.Id, new Dictionary<string, string>
        {
            ["{{customer}}"] = customer,
            ["{{date}}"] = DateTime.UtcNow.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
        });
        #endregion
    }

    public async Task ContactsSearchAsync(GoogleContacts contacts, string typed)
    {
        #region example:connectors-contacts-search
        var matches = await contacts.SearchContactsAsync(typed, limit: 5);

        if (matches.Count == 0)
        {
            matches = await contacts.SearchOtherContactsAsync(typed, limit: 5);   // people emailed, never saved
        }

        foreach (var person in matches)
        {
            Log.Instance.Info($"{person.DisplayName} <{person.Emails.FirstOrDefault()}>");
        }
        #endregion
    }

    public async Task TasksAsync(GoogleTasks tasks)
    {
        #region example:connectors-tasks-subtasks
        var parent = await tasks.CreateTaskAsync("@default", new NewGoogleTask("Prepare the launch") { Due = new DateOnly(2026, 11, 2) });
        await tasks.CreateTaskAsync("@default", new NewGoogleTask("Book the venue") { ParentId = parent.Id });

        foreach (var task in await tasks.ListTasksAsync("@default", new GoogleTaskQuery { ShowCompleted = false }))
        {
            Log.Instance.Info($"{task.Title} due {task.Due}");
        }

        await tasks.CompleteTaskAsync("@default", parent.Id);
        #endregion
    }

    public async Task MeetAsync(GoogleMeet meet)
    {
        #region example:connectors-meet-transcript
        var space = await meet.CreateSpaceAsync(new MeetSpaceSettings { Access = MeetAccess.Trusted, AutoTranscription = true });
        Log.Instance.Info($"Join at {space.MeetingUri}");

        // Later, once the meeting is over:
        var conference = (await meet.ListConferencesAsync(space.Name)).First();   // newest first

        foreach (var transcript in await meet.ListTranscriptsAsync(conference.Name))
        {
            foreach (var entry in await meet.ListTranscriptEntriesAsync(transcript.Name))
            {
                Log.Instance.Info($"{entry.Participant}: {entry.Text}");
            }
        }
        #endregion
    }

    public async Task ChatAsync(GoogleChat chat, string spaceName)
    {
        #region example:connectors-chat-reply
        var recent = await chat.ListMessagesPageAsync(spaceName, limit: 20);

        if (recent.Items.FirstOrDefault(m => m.Text.Contains("deploy?", StringComparison.OrdinalIgnoreCase)) is { } question)
        {
            await chat.PostMessageAsync(spaceName, "Deploy finished *without errors*", thread: question.Thread);
            await chat.AddReactionAsync(question.Name, "✅");
        }
        #endregion
    }

    public async Task FormsAsync(GoogleForms forms)
    {
        #region example:connectors-forms-feedback
        var form = await forms.CreateAsync("Workshop feedback");
        var rating = await forms.AddQuestionAsync(form.FormId, new NewGoogleFormQuestion("How was it?", GoogleFormQuestionKind.Scale) { ScaleHigh = 5, Required = true });
        await forms.AddQuestionAsync(form.FormId, new NewGoogleFormQuestion("What should change?", GoogleFormQuestionKind.Paragraph));
        await forms.SetPublishedAsync(form.FormId, published: true);

        // Later:
        foreach (var response in await forms.ListResponsesAsync(form.FormId))
        {
            if (response.Answers.TryGetValue(rating.QuestionId!, out var answer))
            {
                Log.Instance.Info($"Rated {answer.Values.Single()}");
            }
        }
        #endregion
    }

    public async Task WorkspaceEventsAsync(WorkspaceEvents events, GooglePubSubVerifier verifier, string? authorizationHeader, string body, string folderId)
    {
        #region example:connectors-workspace-events
        await events.CreateAsync(
            WorkspaceEventTarget.DriveFile(folderId, includeDescendants: true),
            ["google.workspace.drive.file.v3.created", "google.workspace.drive.file.v3.contentChanged"],
            "projects/my-project/topics/drive-events");

        // In the Pub/Sub push endpoint:
        if (await verifier.VerifyAsync(authorizationHeader) && GoogleNotifications.ParsePubSub(body) is { } message)
        {
            switch (GoogleNotifications.ParseWorkspaceEvent(message))
            {
                case WorkspaceResourceEvent change:
                    Log.Instance.Info($"{change.Type} on {change.Subject}");
                    break;
                case WorkspaceExpirationReminder reminder:
                    await events.RenewAsync(reminder.Subscription);
                    break;
                case WorkspaceSubscriptionSuspended suspended:
                    Log.Instance.Warning($"Subscription {suspended.Subscription} suspended: {suspended.Reason}");
                    break;
                case WorkspaceSubscriptionExpired expired:
                    Log.Instance.Warning($"Subscription {expired.Subscription} expired; create it again");
                    break;
            }
        }
        #endregion
    }

    public async Task GmailDeltaAsync(Gmail gmail, string? storedToken)
    {
        #region example:connectors-gmail-delta
        GmailDelta delta;

        try
        {
            delta = await gmail.MessagesDeltaAsync(storedToken, fromNow: storedToken is null);
        }
        catch (ConnectorException ex) when (ex.IsResyncRequired)
        {
            delta = await gmail.MessagesDeltaAsync(fromNow: true);   // older than Gmail keeps: catch up with a search, then follow from now
        }

        foreach (var change in delta.Items)
        {
            if (change.Deleted)
            {
                await ForgetMailAsync(change.Id);
            }
            else if (change.Added)
            {
                await IngestMailAsync(change.Id);
            }
        }

        storedToken = delta.DeltaToken;   // store it for the next call
        #endregion
    }

    public async Task DriveDeltaAsync(Drive drive, string folderId, string? storedToken)
    {
        #region example:connectors-drive-delta
        var delta = await drive.DeltaAsync(storedToken, fromNow: storedToken is null);

        foreach (var file in delta.Items)
        {
            if (file.Deleted || file.Trashed)
            {
                Log.Instance.Info($"gone: {file.Id}");
            }
            else if (file.Parents.Contains(folderId))
            {
                Log.Instance.Info($"changed: {file.Name}");
            }
        }

        storedToken = delta.DeltaToken;   // store it for the next call
        #endregion
    }
}
