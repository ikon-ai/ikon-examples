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

file sealed class LocalRoomExamples(IApp<LocalRoomExamples.SessionIdentity, LocalRoomExamples.ClientParameters> app)
{
    #region example:room-identity-local-fallback
    // In the cloud the identity's Room gives each room its own instance. In a local run every identity
    // field is null, so the client's own ?room= on ClientParameters says which room it asked for.
    public record SessionIdentity(string? Room);

    public record ClientParameters(string? Room = null);

    private string? CurrentRoom()
    {
        var room = app.SessionIdentity.Room;
        return string.IsNullOrEmpty(room) ? app.Clients[ReactiveScope.ClientId]?.Parameters.Room : room;
    }
    #endregion
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
            // Empty when the client sends none. Like InitialPath, the client supplies it, so treat the host
            // as a hint that selects what to show, and authorize the result server-side as usual.
            if (Uri.TryCreate(ctx.InitialUrl, UriKind.Absolute, out var url))
            {
                _host.SetFor(ctx.ClientSessionId, url.Host);
            }
        });
        #endregion
    }

    #region example:client-local-time
    // DateTime.ToLocalTime() and DateTime.Now use the server's zone, which is UTC in a deployed app.
    // In a render or a handler the viewer's zone is on the current client's context, which is null
    // outside a client's scope; its Timezone is empty when the client reported none.
    private string ViewerTime(DateTime utc)
    {
        var zoneId = app.CurrentClientContext?.Timezone;
        var zone = !string.IsNullOrEmpty(zoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var found)
            ? found
            : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone).ToString("d MMM HH:mm");
    }
    #endregion

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

    private async Task DocClientFunctionsAsync(
        ClientAudioCaptureOptions audioOptions,
        ClientVideoCaptureSource source,
        ClientVideoCaptureOptions videoOptions,
        ClientImageCaptureOptions imageOptions,
        string streamId,
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
        await ClientFunctions.KeepScreenAwakeAsync(true);          // until KeepScreenAwakeAsync(false); returns false when the client refuses
        await ClientFunctions.GetLanguageAsync();
        await ClientFunctions.GetTimezoneAsync();
        await ClientFunctions.GetUrlAsync();
        await ClientFunctions.SetUrlAsync("/path");                // replace: false, preserveQueryParams: false
        // Loads an absolute http(s) URL in this tab with a full page load. Use it to enter another instance (?room=)
        await ClientFunctions.NavigateToAsync(app.JoinUrl(new { room = "AB12" }));
        await ClientFunctions.OpenExternalUrlAsync("https://example.com"); // a new tab; false when the browser blocks it
        await ClientFunctions.GetVisibilityAsync();                // ClientVisibility.Visible/Hidden/Unknown
        await ClientFunctions.GetBatteryLevelAsync();              // 0-100; null in Safari and Firefox; 100 with no battery
        await ClientFunctions.GetNetworkTypeAsync();               // "slow-2g", "2g", "3g" or "4g" in Chromium (fast wifi reads "4g"); null in Safari and Firefox
        // False without vibrating in a web client that has had no tap or click yet (browsers block it until then)
        await ClientFunctions.VibrateAsync(200);                   // or a pattern: VibrateAsync(new[] { 100, 50, 100 }); VibrateAsync(0) stops one
        await ClientFunctions.ScrollToAsync(x: 0, y: 0, smooth: true);
        // A browser enters fullscreen only during a tap, and this call arrives after the tap. For a button
        // that goes fullscreen, use view.ActionButton(action: ActionKind.RequestFullscreen, ...) (ui-components guide)
        await ClientFunctions.ExitFullscreenAsync();
        await ClientFunctions.LoginShowAsync("Sign in to save your event");   // the app's sign-in screen: email, passkey, providers
        await ClientFunctions.LoginAsync("google");                            // straight to one OAuth provider (not email or passkey)
        await ClientFunctions.LogoutAsync();

        // The signed-in user's own right to erasure, made with their client's own sign-in. Each returns a
        // ClientAccountRemoval (Succeeded, ScheduledFor, Error), or null when the client's SDK lacks it.
        await ClientFunctions.GetAccountRemovalAsync();
        await ClientFunctions.RequestAccountRemovalAsync();       // erased after the grace period
        await ClientFunctions.CancelAccountRemovalAsync();

        // Pass targetId to address another client session (all functions):
        await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: targetId);

        // Sounds are not a client function; they play through Audio. Make a short sound once
        // (WAV, MP3 or Ogg (Vorbis or Opus), up to 30 s), keep it, and play it as often as needed:
        var sound = await Audio.CreateSoundAsync(data, mimeType);
        var playback = Audio.Play(MediaTargets.To(ReactiveScope.ClientId), sound, new PlayOptions { Volume = 0.8f });
        playback.Stop();
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
        var ikonServerId = app.GlobalState.IkonServerId;     // Id of this IKON server instance
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
            // ctx is a Context, the parameter type of a separate handler method, as in Task Greet(Context ctx).
            // ctx.ClientSessionId (alias of ctx.SessionId), ctx.UserId, ctx.Theme, ctx.Timezone,
            // ctx.ClientType, ctx.InitialPath, ctx.InitialUrl, ctx.ViewportWidth
            var client = app.Clients[ctx.ClientSessionId];
        });
        app.OnClientLeft(async ctx => { /* cleanup client state */ });

        // A periodic loop (a live clock, polling, a game tick) is a Task.Run started in
        // OnStarting that ends on app.StoppingToken, which a stop, a deploy and a hot reload
        // cancel. app.BackgroundWork, a property, does not run a loop: BackgroundWork.StartAsync()
        // keeps the instance running while nobody is connected ("BackgroundWork" below).
        app.OnStarting(async () =>
        {
            _ = Task.Run(async () =>
            {
                while (!app.StoppingToken.IsCancellationRequested)
                {
                    _now.Value = DateTime.UtcNow;
                    await Task.Delay(1000, app.StoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            });
        });
        // Main() is a method, not a constructor, so assigning a `readonly` field there is
        // CS0191; the loop needs no field at all.
        #endregion
    }
}
