using Ikon.Connectors;
using Ikon.Connectors.Google;

namespace Ikon.App.Patterns.Examples;

file sealed class GoogleConnectorGuideExamples
{
    private static Task SaveSignInAsync(string state, PkceCodes pkce) => Task.CompletedTask;

    private static Task<PkceCodes> LoadSignInAsync(string state) => Task.FromResult(PkceCodes.Create());

    private static Task IngestMailAsync(string messageId) => Task.CompletedTask;

    public void GoogleClients(string clientId, string clientSecret, string refreshToken)
    {
        #region example:connectors-google-clients
        var credentials = new GoogleCredentials(clientId, clientSecret, refreshToken);
        using var drive = new Drive(credentials);
        using var gmail = new Gmail(credentials);
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
        using var gmail = new Gmail(credentials);
        #endregion

        Log.Instance.Debug($"{signInUrl}");
    }

    public async Task DriveTransferAsync(Drive drive, string folderId)
    {
        #region example:connectors-drive-transfer
        await using var content = File.OpenRead("./report.pdf");
        var uploaded = await drive.UploadAsync("report.pdf", "application/pdf", content, folderId);

        await using var download = await drive.DownloadAsync(uploaded.Id);
        #endregion
    }

    public async Task DriveExportAsync(Drive drive, DriveFile file)
    {
        #region example:connectors-drive-export
        if (file.MimeType == "application/vnd.google-apps.document")
        {
            await using var exported = await drive.ExportAsync(file.Id, "text/plain");
            using var reader = new StreamReader(exported);
            var text = await reader.ReadToEndAsync();
        }
        #endregion
    }

    public async Task DriveListAsync(Drive drive, string folderId)
    {
        #region example:connectors-drive-list
        await foreach (var file in drive.ListAllAsync(folderId, extraQuery: "trashed = false"))
        {
            Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
        }
        #endregion
    }

    public async Task GmailAsync(Gmail gmail, string bodyText)
    {
        #region example:connectors-gmail
        var unread = await gmail.ListAsync("is:unread", limit: 10);

        foreach (var email in unread)
        {
            var body = await gmail.GetBodyAsync(email.Id);
            Log.Instance.Info($"{email.From}: {email.Subject}");
        }

        var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
        #endregion

        Log.Instance.Debug($"{sentId}");
    }

    public async Task GmailHistoryAsync(Gmail gmail, ulong? storedHistoryId)
    {
        #region example:connectors-gmail-history
        var start = storedHistoryId ?? await gmail.GetHistoryIdAsync();   // first run: from now on

        try
        {
            var history = await gmail.HistoryAsync(start);

            foreach (var messageId in history.AddedMessageIds)
            {
                await IngestMailAsync(messageId);
            }

            storedHistoryId = history.HistoryId;
        }
        catch (ConnectorException ex) when (ex.StatusCode == 404)
        {
            storedHistoryId = null;   // older than Gmail keeps: read again with a query, then start fresh
        }
        #endregion
    }

    public async Task DriveChangesAsync(Drive drive, string folderId, string? storedToken)
    {
        #region example:connectors-drive-changes
        var changes = await drive.ChangesAsync(storedToken ?? await drive.GetStartPageTokenAsync());

        foreach (var change in changes.Items)
        {
            if (change.File is { Trashed: false } file && file.Parents?.Contains(folderId) == true)
            {
                Log.Instance.Info($"changed: {file.Name}");
            }
        }

        storedToken = changes.NewStartPageToken;
        #endregion
    }
}
