using Ikon.Common.Core.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

// The viewport section is about how an app lays out its whole screen, so the holder carries the
// render helpers the example calls — the example itself is the two Columns.
file sealed class ViewportExamples
{
    private sealed record Message(string Text);

    private readonly ReactiveList<Message> _messages = new();

    private static void RenderHeader(UIView view) => view.Text(text: "header");

    private static void RenderMessage(UIView view, Message msg) => view.Text(text: msg.Text);

    private static void RenderInput(UIView view) => view.Text(text: "input");

    public void Render(UIView view)
    {
        #region example:viewport-layout
        // WRONG — page grows forever, browser scrollbar appears
        view.Column(["min-h-screen"], content: view =>
        {
            RenderHeader(view);
            foreach (var msg in _messages) { RenderMessage(view, msg); }  // unbounded
            RenderInput(view);
        });

        // CORRECT — fixed viewport, chat area scrolls internally
        view.Column(["h-screen"], content: view =>
        {
            RenderHeader(view);                                           // flex-shrink-0
            view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
            {
                foreach (var msg in _messages) { RenderMessage(view, msg); }
            });
            RenderInput(view);                                            // flex-shrink-0
        });
        #endregion
    }
}

internal sealed partial class AgentGuideExamples
{

    private void DocConditionalRendering(UIView view)
    {
        #region example:conditional-rendering
        if (_imageData.Value != null)
        {
            view.Image(["max-w-full"], data: _imageData.Value, mimeType: _imageMime.Value);
        }
        #endregion
    }

    private async Task DocNavigationPathsAsync(string tab, int clientSessionId)
    {
        #region example:navigation-paths
        // Listen for path changes
        app.Navigation.PathChangedAsync += async args =>
        {
            var path = args.Path.TrimStart('/');
            _activeTab.Value = path;
        };

        // Change path programmatically
        await app.Navigation.SetPathAsync($"/{tab}");
        await app.Navigation.SetPathAsync(clientSessionId, $"/{tab}", replace: true);
        #endregion
    }

    private void DocNavigationInitialUrl()
    {
        #region example:navigation-initial-url
        app.OnClientJoined(async ctx =>
        {
            // Empty for every non-browser client, and client-supplied like InitialPath — treat the host as
            // a hint that selects what to show, and authorize the result server-side as usual.
            if (Uri.TryCreate(ctx.InitialUrl, UriKind.Absolute, out var url))
            {
                _host.SetFor(ctx.ClientSessionId, url.Host);
            }
        });
        #endregion
    }

    private async Task DocBackgroundWorkAsync()
    {
        #region example:background-work
        await using var work = await app.BackgroundWork.StartAsync(TimeSpan.FromMinutes(20.0), "nightly export");
        await LongRunningTask();
        // Disposing signals completion; the hold ends on its own after 20 minutes either way
        #endregion
    }

    private void DocJoinUrlAndQr(UIView view, string sessionId)
    {
        #region example:join-url-qr
        // Get the shareable join URL
        var joinUrl = app.PublicUrl;
        // with query parameters (URL-encoded name=value pairs from an anonymous object):
        var inviteUrl = app.JoinUrl(new { id = sessionId });
        // or session-specific:
        var sessionUrl = app.ReactiveGlobalState.SessionUrl.Value;

        // Render as QR code
        view.QR(["w-48 h-48"], value: joinUrl);

        // Or display as text
        view.Text([Text.Body, "text-primary underline"], joinUrl);
        #endregion

        _ = inviteUrl + sessionUrl;
    }

    private static async Task DocClientFunctionsAsync(
        ClientAudioCaptureOptions audioOptions,
        ClientVideoCaptureSource source,
        ClientVideoCaptureOptions videoOptions,
        ClientImageCaptureOptions imageOptions,
        string streamId,
        string playbackId,
        string url,
        byte[] data,
        string mimeType,
        int targetId)
    {
        #region example:client-functions
        // Every function targets the calling client (resolved via ReactiveScope.ClientId) by default
        await ClientFunctions.SetThemeAsync(Theme.Dark);           // persist: true by default; string overload for custom themes
        await ClientFunctions.GetMediaDevicesAsync();
        await ClientFunctions.StartAudioCaptureAsync(audioOptions);     // returns streamId
        await ClientFunctions.StartVideoCaptureAsync(source, videoOptions); // returns streamId
        await ClientFunctions.StopCaptureAsync(streamId);
        await ClientFunctions.CaptureImageAsync(imageOptions);          // returns ClientImageCapture
        await ClientFunctions.KeepScreenAwakeAsync(true);
        await ClientFunctions.GetLanguageAsync();
        await ClientFunctions.GetTimezoneAsync();
        await ClientFunctions.GetUrlAsync();
        await ClientFunctions.SetUrlAsync("/path");                // replace: false, preserveQueryParams: false
        await ClientFunctions.GetVisibilityAsync();                // ClientVisibility.Visible/Hidden/Unknown
        await ClientFunctions.GetBatteryLevelAsync();              // 0-100
        await ClientFunctions.GetNetworkTypeAsync();               // connection type
        await ClientFunctions.VibrateAsync(200);                   // or a pattern: VibrateAsync(new[] { 100, 50, 100 })
        await ClientFunctions.ScrollToAsync(x: 0, y: 0, smooth: true);
        await ClientFunctions.PlaySoundAsync(url, volume: 0.8, loop: false);
        await ClientFunctions.PlaySoundAsync(data, mimeType, volume: 0.8, loop: false); // from bytes
        await ClientFunctions.StopSoundAsync(playbackId);
        await ClientFunctions.RequestFullscreenAsync();
        await ClientFunctions.ExitFullscreenAsync();
        await ClientFunctions.LogoutAsync();

        // The signed-in user's own right to erasure, made with their client's own sign-in. Each returns a
        // ClientAccountRemoval (Succeeded, ScheduledFor, Error), or null when the client's SDK lacks it.
        await ClientFunctions.GetAccountRemovalAsync();
        await ClientFunctions.RequestAccountRemovalAsync();       // erased after the grace period
        await ClientFunctions.CancelAccountRemovalAsync();

        // Pass targetId to address another client session (all functions):
        await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: targetId);
        #endregion
    }

    private static async Task<string?> DocClientPlatformAsync(Context clientContext)
    {
        #region example:client-platform
        // At once, from the user agent: the OS, its version where the agent still says it, the browser
        var platform = ClientPlatform.FromContext(clientContext);

        // A Chromium browser adds Windows 10 or 11, the macOS version, Intel or Apple Silicon
        platform = await ClientFunctions.GetPlatformAsync(clientContext.SessionId) ?? platform;

        var installer = platform switch
        {
            { Os: ClientOs.Windows, Architecture: ClientArchitecture.Arm64 } => "setup-arm64.exe",
            { Os: ClientOs.Windows } => "setup-x64.exe",
            { Os: ClientOs.MacOs } => "app-universal.dmg",      // Architecture is Unknown outside Chromium
            { Os: ClientOs.Linux, Distribution: "ubuntu" or "debian" } => "app.deb",
            { Os: ClientOs.Linux, Distribution: "fedora" } => "app.rpm",
            { Os: ClientOs.Linux } => "app.AppImage",          // no browser names its distribution reliably
            _ => null,                                          // a phone: list every download
        };
        #endregion

        return installer;
    }

    private static void DocCallbackErrorHandling(UIView view)
    {
        #region example:callback-error-handling
        view.Button([Button.PrimaryMd], text: "Run", onClick: async () =>
        {
            try { await RiskyOperation(); }
            catch (Exception ex) { Log.Instance.Warning(ex, "Operation failed"); }
        });
        #endregion
    }

    private async Task DocMessagesAsync(int trackId, int clientSessionId)
    {
        #region example:messages
        app.MessageReceivedAsync += async args => { /* args.Message.Opcode, args.Message.TrackId */ };
        await app.SendMessageAsync(ProtocolMessage.Create(app.SessionId, new RequestIdrVideoFrame(),
            trackId: trackId, targetIds: [clientSessionId]));
        #endregion
    }

    private void DocHostServices(string gameId, int clientSessionId, int clientId)
    {
        #region example:host-services
        var spaceId = app.GlobalState.SpaceId;                  // Current space ID
        var ikonServerId = app.GlobalState.IkonServerId;     // Id of this Ikon server instance
        var sessionIdentityHash = app.GlobalState.SessionIdentityHash;  // Hash of session identity params (logical session id)
        var publicUrl = app.PublicUrl;                          // The app's public URL (space access URL)
        var joinUrl = app.JoinUrl(new { id = gameId });         // PublicUrl + URL-encoded query string from an anonymous object
        var sessionUrl = app.GlobalState.SessionUrl;            // Session-specific access URL
        var primaryUserId = app.GlobalState.PrimaryUserId;      // Static user ID of session owner
        var firstUserId = app.GlobalState.FirstUserId;          // First human user who joined (dynamically reassigned)
        var clientContext = app.GlobalState.GetClientContext(clientSessionId);  // null if no such client is connected
        var dataDirectory = app.DataDirectory;                  // Path to app's Data directory
        var databases = app.Databases;                          // Database connection info (see Databases section)
        var identity = app.SessionIdentity;                     // Current session identity
        var parameters = app.Clients[clientId]?.Parameters;     // Client parameters; the indexer is null when that client is gone
        var clients = app.ReactiveGlobalState.Clients;          // Reactive client state
        #endregion

        Log.Instance.Debug($"{spaceId} {ikonServerId} {sessionIdentityHash} {publicUrl} {joinUrl} "
            + $"{sessionUrl} {primaryUserId} {firstUserId} {clientContext} {dataDirectory} "
            + $"{databases} {identity} {parameters} {clients}");
    }

    #region example:loading-state-field
    private readonly Reactive<bool> _isLoading = new(false);
    #endregion

    private void DocLoadingState(UIView view)
    {
        #region example:loading-state
        view.Button([Button.PrimaryMd], _isLoading.Value ? "Loading..." : "Submit",
            disabled: _isLoading.Value,
            onClick: async () =>
            {
                _isLoading.Value = true;
                try { await DoWork(); }
                finally { _isLoading.Value = false; }
            });
        #endregion
    }

    private void DocLifecycleEvents()
    {
        #region example:lifecycle-events
        // Use the friendly extension helpers — NOT raw `app.StartingAsync += ...`.
        // The raw events take AsyncEventHandler<TEventArgs> (one-arg); subscribing to
        // them and typing the arg invents non-existent types like AppStartingEventArgs.
        app.OnStarting(async () => { /* app starting */ });
        app.OnStopping(async () => { /* app stopping, cleanup */ });
        app.OnClientJoined(async ctx =>
        {
            // ctx IS the Context: ctx.ClientSessionId (alias of ctx.SessionId), ctx.UserId,
            // ctx.Theme, ctx.Timezone, ctx.ClientType, ctx.InitialPath, ctx.InitialUrl, ctx.ViewportWidth
            var client = app.Clients[ctx.ClientSessionId];
        });
        app.OnClientLeft(async ctx => { /* cleanup client state */ });

        // For a periodic background loop (live clock, polling, game tick), start it
        // inside OnStarting and cancel it in OnStopping — there is no app.BackgroundWork
        // "start a task" API (BackgroundWork only ref-counts idle-shutdown prevention):
        var clockCts = new CancellationTokenSource();
        app.OnStarting(async () =>
        {
            _ = Task.Run(async () =>
            {
                while (!clockCts.Token.IsCancellationRequested)
                {
                    _now.Value = DateTime.Now;
                    await Task.Delay(1000, clockCts.Token);
                }
            }, clockCts.Token);
        });
        app.OnStopping(async () => clockCts.Cancel());
        // The loop's CancellationTokenSource is a plain local the lambdas capture
        // (`var clockCts = new CancellationTokenSource();`) — do NOT
        // declare the loop as a `readonly ClientReactiveEffect`/effect field and assign
        // it inside Main(): Main() is a normal method, not a constructor, so assigning a
        // `readonly` field there is CS0191/CS8618. A game tick / timer is just the
        // Task.Run loop above started from OnStarting (or directly in Main), not a
        // readonly effect object.
        #endregion
    }
}
