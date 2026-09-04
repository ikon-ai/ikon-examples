using System.Diagnostics;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Ikon.AI.ImageUpscaling;

// The examples the AGENTS.md template teaches — as code this app actually runs.
//
// Compiling an example is not the same as it being real. A private method nobody calls proves the
// names and types still exist and nothing else, and a file of those is a harness wearing an app's
// clothes. So every example here is reachable from the Doc Examples tab: the free ones run when the
// card renders, and the ones that spend money on a provider run when a person presses their button
// — the same shape the Ikon.AI cards already use, for the same reason.
//
// A hand-written fence in the docs is a copy of code that once worked: nothing compiles it, so it
// stays exactly as written while the API beneath it is renamed or deleted. Each method here is
// wrapped in a `#region docsnippet:<id>` that the template splices in through
// `<!-- ikon-code: <id> -->`, so the example a reader copies IS this code and the build fails
// before it can drift.
//
// The rules that keep the splice honest:
//   * The region contains ONLY the lines the doc should show. Whatever the example needs but does
//     not teach — a parameter, a using, a surrounding method — lives outside it.
//   * The code is the shape the docs TEACH, not merely the shape that happens to exist elsewhere in
//     this app. The image one-shot is pinned here even though the IkonAI card drives the config
//     form, because the one-shot is what the guide shows.
//   * `TemplateSnippetsArePinnedTests` counts what is left; it may only fall.
public partial class Validation
{
    private readonly Reactive<string?> _docExampleResult = new(null);
    private readonly Reactive<string?> _docExampleError = new(null);
    private readonly Reactive<string?> _docExampleBusy = new(null);

    private void RenderDocExamplesSection(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Doc examples");
            view.Text([Text.Caption, "mb-4"],
                "Every snippet the AGENTS.md template splices, running here. The free ones ran when "
                + "this card rendered; the metered ones spend provider credit, so they wait for a press.");

            // These cost nothing, so running them on render is the strongest statement available:
            // the example is not merely compiled, it executed to draw what is on screen.
            DocValueMutation.Run();
            DocConditionalRendering(view);
            DocSortableList(view);
            DocJoinUrlAndQr(view, app.GlobalState.SpaceId);
            DocLogLevels();

            view.Text([Text.Caption, "mt-4 mb-2"], "Metered — each press calls a real provider");

            view.Row(["gap-2 flex-wrap"], content: view =>
            {
                RenderDocExampleButton(view, "Image one-shot", async () => await GenerateImageOneShotAsync("A neon-lit cyberpunk street"));
                RenderDocExampleButton(view, "Upscale", async () => await DocImageUpscaleConfigAsync());
                RenderDocExampleButton(view, "Sound effect", DocSoundEffectOneShotAsync);
                RenderDocExampleButton(view, "Speech", async () => (await DocSpeechGenerateOneShotAsync()).ToString());
                RenderDocExampleButton(view, "Video", DocVideoGenerateOneShotAsync);
                RenderDocExampleButton(view, "Web search", async () => (await DocWebSearchOneShotAsync()).ToString());
                RenderDocExampleButton(view, "Embeddings", async () => (await DocEmbeddingsOneShotAsync()).ToString());
            });

            if (_docExampleError.Value is { } error)
            {
                view.Text([Text.Caption, "mt-3 text-error-primary"], error);
            }
            else if (_docExampleResult.Value is { } result)
            {
                view.Text([Text.Caption, "mt-3"], result);
            }
        });
    }

    private void RenderDocExampleButton(UIView view, string label, Func<Task<string>> run)
    {
        var busy = _docExampleBusy.Value == label;

        view.Button([Button.OutlineSm], text: busy ? $"{label}…" : label, disabled: _docExampleBusy.Value != null,
            onClick: async () =>
            {
                _docExampleBusy.Value = label;
                _docExampleError.Value = null;
                _docExampleResult.Value = null;

                try
                {
                    _docExampleResult.Value = $"{label}: {await run()}";
                }
                catch (AIException)
                {
                    // The example is what is under test, not the provider. A short human line keeps
                    // the card usable when a model is unavailable.
                    _docExampleError.Value = $"{label} could not reach the provider — try again.";
                }
                finally
                {
                    _docExampleBusy.Value = null;
                }
            });
    }

    private static async Task<string> DocImageUpscaleOneShotAsync(byte[] imageBytes)
    {
        #region docsnippet:image-upscale-one-shot
        var result = await ImageUpscaler.UpscaleAsync(imageBytes, "image/png", scaleFactor: 4);  // SeedVr2 by default
        var bytes = await result.Image.GetDataAsync();
        #endregion

        return $"{bytes.Length} bytes";
    }

    private static async Task<string> DocImageUpscaleConfigAsync()
    {
        #region docsnippet:image-upscale-config
        using var imageUpscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);
        var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
        {
            InputImage = new InputImage { Url = "https://example.com/photo.png" },
            TargetResolution = UpscaleTargetResolution.Uhd2160
        });
        #endregion

        return result.Image.MimeType ?? "unknown";
    }

    private static async Task<string> DocSoundEffectOneShotAsync()
    {
        #region docsnippet:sound-effect-one-shot
        var effect = await SoundEffectGenerator.GenerateAsync("Thunder rumbling in the distance");
        var wavBytes = await effect.GetDataAsync();  // inline bytes, or downloaded when a large result was delivered as a URL (effect.Kind)
        // effect.MimeType, effect.DurationSeconds
        #endregion

        return $"{wavBytes.Length} bytes, {effect.MimeType}";
    }

    private async Task DocSoundEffectStreamedAsync()
    {
        #region docsnippet:sound-effect-streamed
        using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
        await foreach (var audio in generator.GenerateSoundEffectAsync(new SoundEffectGeneratorConfig
        {
            Prompt = "Thunder rumbling in the distance",
            DurationSeconds = 5.0
        }))
        {
            Audio.SendSpeech(audio);
        }
        #endregion
    }

    private static async Task<string> DocVideoGenerateOneShotAsync()
    {
        #region docsnippet:video-generate-one-shot
        var video = await VideoGenerator.GenerateAsync("A timelapse of a flower blooming");  // Veo31Fast (cheap+fast) by default
        // video.Url (string)
        #endregion

        return video.Url;
    }

    private static async Task<string> DocVideoGenerateConfigAsync()
    {
        #region docsnippet:video-generate-config
        using var generator = new VideoGenerator(VideoGeneratorModel.Veo31);
        var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
        {
            Prompt = "A timelapse of a flower blooming",
            AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
            Length = 6  // Veo31 supports lengths 4, 6, and 8 — unsupported values silently fall back to 4
        });
        // result.Url (string)
        #endregion

        return result.Url;
    }

    private static async Task<string> DocVideoEnhanceOneShotAsync(string clipUrl)
    {
        #region docsnippet:video-enhance-one-shot
        var enhanced = await VideoEnhancer.EnhanceAsync(clipUrl);
        // enhanced.Url (string), enhanced.OutputFps, enhanced.OutputSizeBytes
        #endregion

        return enhanced.Url;
    }

    private static async Task<string> DocVideoEnhanceConfigAsync(byte[] videoBytes)
    {
        #region docsnippet:video-enhance-config
        using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
        var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
        {
            Data = videoBytes,
            MimeType = "video/mp4"
        });
        // result.Url (string), result.OutputFps, result.OutputSizeBytes
        #endregion

        return result.Url;
    }

    private static async Task<int> DocWebSearchOneShotAsync()
    {
        var count = 0;

        #region docsnippet:web-search-one-shot
        var results = await WebSearcher.SearchAsync("latest AI news", maxResults: 5);  // Google by default
        foreach (var result in results) { /* result.Title, result.Url, result.Content */ }
        #endregion

        foreach (var result in results)
        {
            count += result.Title.Length;
        }

        return count;
    }

    private async Task DocSpeakAsync(int clientSessionId)
    {
        #region docsnippet:speak-one-call
        // Generate speech and play it to clients — one call. A new call fades out and
        // replaces whatever is still playing (the interrupt behavior a voice app wants).
        // Name a voice that fits the product: the bare default ("Aria") is a mature, hard read
        // that suits few apps — "Sarah" is a softer, modern one to reach for. Other voices:
        // Jessica, Lily, Matilda, Charlotte (female); George, Brian, Will (male).
        await Audio.SpeakAsync("Hello world", voice: "Sarah");

        // Pick a model, shape the delivery, or target specific clients:
        await Audio.SpeakAsync("Hello world", SpeechGeneratorModel.Eleven3, voice: "Sarah",
            instructions: "Soft and warm, almost a whisper", speed: 0.96, targetIds: [clientSessionId]);  // speed is a double, 1.0 = normal
        #endregion
    }

    private static async Task<int> DocSpeechGenerateOneShotAsync()
    {
        #region docsnippet:speech-generate-one-shot
        var audio = await SpeechGenerator.GenerateAsync("Hello world");  // ElevenFlash25 (cheap+fast) by default
        // audio.Samples (float[]), audio.SampleRate, audio.ChannelCount
        #endregion

        return audio.Samples.Length + audio.SampleRate + audio.ChannelCount;
    }

    private async Task DocSpeechGenerateStreamedAsync()
    {
        #region docsnippet:speech-generate-streamed
        using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
        await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hei maailma", Language = "fi" }))
        {
            Audio.SendSpeech(audio);  // Audio is an app service property
        }
        #endregion
    }

    private static async Task DocSpeechRecognizeBatchAsync(float[] samples)
    {
        #region docsnippet:speech-recognize-batch
        using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);

        var transcript = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
        {
            Samples = samples,
            SampleRate = 16000,
            ChannelCount = 1,
            Timestamps = SpeechTimestamps.Word,
        });

        foreach (var word in transcript.Words)   // SpeechWord: Text, Start, End, Confidence, Speaker
        {
            Log.Instance.Info($"[{word.Start.TotalSeconds:F2}] {word.Text}");
        }
        #endregion
    }

    private static void DocMicToggleButton(UIView view)
    {
        #region docsnippet:mic-toggle-button
        view.MicToggleButton();
        #endregion
    }

    private static async Task<int> DocEmbeddingsOneShotAsync()
    {
        #region docsnippet:embeddings-one-shot
        var embeddings = await EmbeddingGenerator.EmbedAsync(["Hello world", "Goodbye"]);  // OpenAI3Small (cheap+fast) by default
        // embeddings[0] is float[] vector
        #endregion

        return embeddings.Count;
    }

    // The app-side shapes these examples stand on. They are the reader's own records in the docs,
    // so they live outside every region — the example teaches the reactive, not the payload.
    private sealed record MyState(string Title = "");
    private sealed record Prefs(bool DarkMode = false);
    private sealed record TodoItem(string Text = "", bool Done = false);
    private sealed record Bookmark(string Url = "");

    private readonly Reactive<DateTime> _now = new(DateTime.Now);

    #region docsnippet:persistent-reactive-scopes
    // DEFAULT for app state — one bucket per SessionIdentity (the app's routing key)
    private readonly PersistentSessionReactive<MyState> _state = new(new MyState());

    // App-wide (rare) — same value for everyone in the space
    private readonly PersistentReactive<int> _totalVisits = new(0);

    // Follows a user across all of their client sessions
    private readonly PersistentUserReactive<Prefs> _prefs = new(new Prefs());

    // Persisted lists — same mutation-notifies contract as ReactiveList<T>
    private readonly PersistentReactiveList<TodoItem> _todos = new();        // app-wide
    private readonly PersistentUserReactiveList<Bookmark> _bookmarks = new(); // per-user
    #endregion

    #region docsnippet:persistent-reactive-backends
    // Default — Private S3-backed cloud asset
    private readonly PersistentSessionReactive<Prefs> _defaultBackend = new(new Prefs());

    // Public asset URL needed (uploaded images, published files — never sensitive data)
    private readonly PersistentSessionReactive<byte[]> _logo
        = new([], backend: PersistenceBackend.Public);

    // Small, frequently-mutated value (counters, status flags). Requires a postgres DB declared
    // created with 'ikon app db create --name main'. Omit postgresDatabase if there is only one.
    private readonly PersistentSessionReactive<long> _counter
        = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
    #endregion

    private static void DocLogLevels()
    {
        #region docsnippet:log-levels
        Log.Instance.Info("Processing started");
        Log.Instance.Debug("Detail info");
        #endregion
    }

    private static void DocLogExceptions(Exception ex)
    {
        #region docsnippet:log-exceptions
        Log.Instance.Error(ex, "AI cleanup failed");     // Serilog / Microsoft.Extensions.Logging idiom
        Log.Instance.Warning(ex, "Auto-retry failed");
        Log.Instance.Critical(ex, "Startup failed");
        #endregion
    }

    private string? DocLogoPublicUrl() => _logo.PublicUrl;

    private DateTime DocNow() => _now.Value;

    private long DocCounter() => _counter.Value;

    private int DocVisits() => _totalVisits.Value + _todos.Count + _bookmarks.Count
        + _state.Value.Title.Length + (_prefs.Value.DarkMode ? 1 : 0) + (_defaultBackend.Value.DarkMode ? 1 : 0);

    // Field-declaration examples live in their own holder, because two guide topics legitimately
    // use the same field name for different things — `_todos` is a PersistentReactiveList in the
    // persistence example and a ClientReactiveList in the reactive-types one. One class per topic
    // keeps both verbatim.
    private sealed class DocReactiveTypes
    {
        #region docsnippet:basic-reactive-types
        // Shared across all clients (global state)
        private readonly Reactive<int> _count = new(0);

        // Per-client state (each connected client sees their own value)
        private readonly ClientReactive<string> _theme = new("light");

        // Per-user state (shared across a user's multiple client sessions)
        // If a user connects from phone and desktop, both clients share the same UserReactive values
        private readonly UserReactive<string> _userPref = new("");

        // List state — ReactiveList<T> (shared) / ClientReactiveList<T> / UserReactiveList<T>.
        // Never Reactive<List<T>> in new code; ReactiveList<T> is the list type.
        private readonly ReactiveList<string> _messages = new();
        private readonly ClientReactiveList<TodoItem> _todos = new();
        #endregion

        public int Total => _count.Value + _theme.Value.Length + _userPref.Value.Length
            + _messages.Count + _todos.Count;
    }

    private readonly Reactive<byte[]?> _imageData = new(null);
    private readonly Reactive<string> _imageMime = new("image/png");
    private readonly ClientReactive<string> _host = new("");

    private void DocConditionalRendering(UIView view)
    {
        #region docsnippet:conditional-rendering
        if (_imageData.Value != null)
        {
            view.Image(["max-w-full"], data: _imageData.Value, mimeType: _imageMime.Value);
        }
        #endregion
    }

    private async Task DocNavigationPathsAsync(string tab, int clientSessionId)
    {
        #region docsnippet:navigation-paths
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
        #region docsnippet:navigation-initial-url
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
        #region docsnippet:background-work
        await using var work = await app.BackgroundWork.StartAsync();
        await LongRunningTask();
        // work.DisposeAsync() signals completion automatically
        #endregion
    }

    private static Task LongRunningTask() => Task.CompletedTask;

    private readonly ReactiveList<string> _items = new();

    private void DocSortableList(UIView view)
    {
        #region docsnippet:sortable-list
        view.SortableList(
            items: _items.Value,
            onReorder: async args => _items.ReplaceAll(args.NewOrder),
            itemContent: (v, id) => v.Text([Text.Body], id));
        #endregion
    }

    private async Task DocEndpointWebSocketAsync()
    {
        #region docsnippet:endpoint-websocket
        var endpoint = new AppEndpointHost(app);

        endpoint.MapWebSocket("/ws", async (ctx, webSocket) =>
        {
            var buffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) { break; }
                await webSocket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, true, CancellationToken.None);
            }
        });

        await endpoint.StartAsync();
        #endregion
    }

    private static void DocEndpointMapGet(AppEndpointHost endpoint)
    {
        #region docsnippet:endpoint-mapget
        // Write the response via ctx.Response.Body (a Stream). NOT ctx.Response.WriteAsync(string)
        // — that ASP.NET Core extension (Microsoft.AspNetCore.Http) is not in scope in a
        // generated app and produces CS1061. Write UTF-8 bytes to the body stream.
        endpoint.MapGet("/stream/{**path}", async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("OK"));
        });
        #endregion
    }

    private readonly NotificationInbox _inbox = new(app);

    private async Task DocNotifyPriorityAsync(string userId)
    {
        #region docsnippet:notify-priority
        await _inbox.NotifyAsync(userId, new NotificationContent("Payment failed", "Tap to fix", Priority: NotificationPriority.High), kind: "payment");
        #endregion
    }

    private void DocNotifyQuietHours(string userId)
    {
        #region docsnippet:notify-quiet-hours
        _inbox.SetQuietHoursFor(userId, new TimeOnly(21, 0), new TimeOnly(6, 0));   // 21:00–06:00 UTC
        // signed-in form: SetQuietHours(...) / QuietHours; read QuietHoursFor(userId); clear with ClearQuietHoursFor(userId)
        #endregion
    }

    private void DocJoinUrlAndQr(UIView view, string sessionId)
    {
        #region docsnippet:join-url-qr
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
        #region docsnippet:client-functions
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

        // Pass targetId to address another client session (all functions):
        await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: targetId);
        #endregion
    }

    private static async Task DocOtherDataServicesAsync(
        string userText, byte[] documentBytes, byte[] docxBytes, IReadOnlyList<string> documents, string query)
    {
        #region docsnippet:other-data-services
        var page = await WebScraper.ScrapeAsync("https://example.com");          // page.Content is Markdown
        var moderation = await Classifier.ClassifyAsync(userText);               // moderation.IsFlagged
        var ocr = await OCR.AnalyzeAsync(documentBytes);                         // ocr.Text
        var pdf = await FileConverter.ConvertToPdfAsync(docxBytes, "report.docx");
        var ranked = await Reranker.RerankAsync(documents, query);               // ranked[0].Index into documents
        #endregion

        _ = (page, moderation, ocr, pdf, ranked);
    }

    private static async Task DocAssetMetadataAsync(AssetUri uri)
    {
        #region docsnippet:asset-metadata
        bool exists = await Asset.Instance.ExistsAsync(uri);
        var metadata = await Asset.Instance.GetMetadataAsync(uri);  // .Size, .LastModified, .Url, .UrlIsTemporal, .MimeType
        #endregion

        _ = (exists, metadata);
    }

    private static async Task DocAssetListingAsync(AssetUri uri, AssetUri folderUri)
    {
        #region docsnippet:asset-listing
        var entries = await Asset.Instance.ListAsync(new AssetQuery(folderUri) { Limit = 50 });
        await Asset.Instance.DeleteAsync(uri);
        #endregion

        _ = entries;
    }

    private sealed record DocOrder(string Id);

    private async Task DocNotifyAllDevicesAsync(string userId, DocOrder order)
    {
        #region docsnippet:notify-all-devices
        await app.Notifications.SendToUserAsync(userId,
            new NotificationContent("Order delivered", "Enjoy!", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
            NotificationReach.AllDevices);
        #endregion
    }

    private async Task DocNotifyActionsAsync(string userId)
    {
        #region docsnippet:notify-actions
        await app.Notifications.SendToUserAsync(userId, new NotificationContent(
            "Ride arriving", "Petri is 2 min away",
            LaunchUrl: "/trip/847",
            Actions: [new NotificationAction("track", "Track", "/trip/847"),
                      new NotificationAction("cancel", "Cancel ride", "/trip/847/cancel")]));
        #endregion
    }

    private static async Task DocCallFunctionsAsync()
    {
        #region docsnippet:call-functions
        var sum = FunctionRegistry.Instance.Call<int>("Add", [2, 3]);
        var greeting = await FunctionRegistry.Instance.CallAsync<string>("Greet", args: ["World"]);
        #endregion

        _ = (sum, greeting);
    }

    private static void DocTextAndContent(UIView view)
    {
        #region docsnippet:text-and-content
        view.Text([Text.Display], "Large Title");
        view.Text([Text.H2], "Section Heading");
        view.Text([Text.Body], "Body text");
        view.Text([Text.Caption, "text-muted-foreground"], "Small caption");
        view.Heading([Text.H3], text: "Heading component");   // or positional: view.Heading("Title")
        view.Markdown("**Bold** and `code`");
        #endregion
    }

    private static void DocDisplayComponents(UIView view, string imageUrl, byte[] bytes)
    {
        #region docsnippet:display-components
        view.Icon([Icon.Default], name: "check");          // Lucide icon names
        view.Box([Icon.Spinner, "w-4 h-4"]);              // CSS-only spinning loader (use Box, not Icon)
        view.Icon([Icon.Spinner], name: "loader-2");       // Spinning icon (spinner animation + Lucide icon)
        view.Image(["max-w-full h-auto"], src: imageUrl);   // From URL
        view.Image(["rounded-lg"], data: bytes, mimeType: MimeTypes.ImageJpeg);  // From bytes
        #endregion
    }

    private static void DocLayoutComponents(UIView view)
    {
        #region docsnippet:layout-components
        view.Box([Card.Default, "p-6"], content: view => { /* ... */ });
        view.Row([Layout.Row.Md, "flex-wrap"], content: view => { /* ... */ });
        view.Column([Layout.Column.Lg], content: view => { /* ... */ });
        view.Flex(["flex gap-4"], content: view => { /* ... */ });
        view.ScrollArea(rootStyle: ["h-[400px]"], content: view => { /* ... */ });
        view.Separator(["my-4"]);
        view.AspectRatio(["w-full"], ratio: 16.0 / 9.0, content: view => { /* ... */ });
        #endregion
    }

    private sealed record DocCategory(string Name, double Total, string Hex);
    private sealed record DocDay(string Label, double Amount);
    private sealed record AnalysisResult(string Summary);

    private static void DocPieChart(UIView view, IReadOnlyList<DocCategory> categories)
    {
        #region docsnippet:pie-chart
        view.PieChart(
            ["h-72 w-72"],
            data: categories.Select(c => new PieChartDatum
            {
                Id = c.Name, Label = c.Name, Value = c.Total, Color = c.Hex
            }),
            innerRadius: 0.5);
        #endregion
    }

    private static void DocLineChart(UIView view, IReadOnlyList<DocDay> days)
    {
        #region docsnippet:line-chart
        view.LineChart(
            ["h-72 w-full"],
            data: [new LineChartSeries
            {
                Id = "Daily", Color = "#34d399",
                Data = days.Select(d => new LineChartPoint { X = d.Label, Y = d.Amount })
            }]);
        #endregion
    }

    private static async Task<AnalysisResult> DocCancellationAsync(string topic)
    {
        #region docsnippet:cancellation-timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var result = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
        }, cts.Token);
        #endregion

        return result;
    }

    private static async Task DocWebSearchConfigAsync()
    {
        #region docsnippet:web-search-config
        using var searcher = new WebSearcher(WebSearcherModel.Google);
        var results = await searcher.SearchPagesAsync(new SearchConfig { Query = "latest AI news", InSiteUrl = "https://example.com" });
        #endregion

        _ = results;
    }

    private void DocAssetUriConstruction()
    {
        #region docsnippet:asset-uri-construction
        // URIs use assets:// scheme with optional scope segments (space, user)
        var localFile = new AssetUri(AssetClass.LocalFile, "image.jpg");
        var cloudFile = new AssetUri(AssetClass.CloudFile, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
        var publicFile = new AssetUri(AssetClass.CloudFilePublic, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
        var cloudJson = new AssetUri(AssetClass.CloudJson, "path/data.json", spaceId: app.GlobalState.SpaceId);
        #endregion

        _ = (localFile, cloudFile, publicFile, cloudJson);
    }

    private async Task DocRawSqlAsync()
    {
        #region docsnippet:raw-sql
        await using var connection = await app.DatabaseAsync("mydb");
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS users (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL);";
        await cmd.ExecuteNonQueryAsync();
        #endregion
    }

    private static string SearchWeb(string query) => query;
    private static string GetData(string topic) => topic;

    private static void DocEmergeTools(EmergePass<string> pass)
    {
        #region docsnippet:emerge-tools
        pass.AddTool(Tool.Of("search", "Search the web", (string query) => SearchWeb(query)))
            .AddTool(Tool.Of("get_data", "Get statistics", (string topic) => GetData(topic)));
        pass.MaxToolCalls = 10;
        #endregion
    }

    private async Task DocRawEndpointAsync()
    {
        #region docsnippet:raw-endpoint
        await using var endpoint = await app.RequestEndpointAsync(EndpointProtocol.Udp);
        var udp = new UdpClient(endpoint.LocalPort);
        Log.Instance.Info($"Game server listening at udp://{endpoint.PublicHost}:{endpoint.PublicPort}");
        // `await using` above releases the endpoint when it goes out of scope.
        #endregion

        udp.Dispose();
    }

    private void DocEndpointCleanup(IAsyncDisposable endpoint)
    {
        #region docsnippet:endpoint-cleanup
        app.OnStopping(async () =>
        {
            await endpoint.DisposeAsync();
        });
        #endregion
    }

    private static int MyMethod(int a) => a;

    private static void DocDirectRegistration()
    {
        #region docsnippet:direct-registration
        FunctionRegistry.Instance.AddFunction(
            Function.Register(MyMethod, "my_function",
                new FunctionAttribute { Description = "Description of what it does" }),
            FunctionVisibility.External);
        #endregion
    }

    // The reader's own pipeline type. RegisterPipeline<T> only requires a class, so the example
    // teaches the registration rather than the pipeline.
    private sealed class MyPipeline;

    private static void DocPipelineRegistration()
    {
        #region docsnippet:pipeline-registration
        FunctionRegistry.Instance.RegisterPipeline<MyPipeline>("run_my_pipeline");
        #endregion
    }

    private static Task RiskyOperation() => Task.CompletedTask;

    private static void DocCallbackErrorHandling(UIView view)
    {
        #region docsnippet:callback-error-handling
        view.Button([Button.PrimaryMd], text: "Run", onClick: async () =>
        {
            try { await RiskyOperation(); }
            catch (Exception ex) { Log.Instance.Warning(ex, "Operation failed"); }
        });
        #endregion
    }

    private sealed record DocClip(string Url, string PosterUrl);

    private static void DocBarChart(UIView view, IReadOnlyList<DocCategory> categories)
    {
        #region docsnippet:bar-chart
        view.BarChart(
            ["h-72 w-full"],
            data: categories.Select(c => new Dictionary<string, object>
            {
                ["category"] = c.Name,
                ["spend"] = c.Total
            }),
            keys: ["spend"],
            indexBy: "category");
        #endregion
    }

    private static async Task DocImageGenerateConfigAsync()
    {
        #region docsnippet:image-generate-config
        using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
        var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
        {
            Prompt = "A neon-lit cyberpunk street",
            Width = 512,
            Height = 512
        });
        if (results.Count > 0) { var image = results[0]; /* await image.GetDataAsync(), image.MimeType */ }
        #endregion
    }

    private static void DocVideoPlayback(UIView view, DocClip clip)
    {
        #region docsnippet:video-playback
        view.VideoUrlPlayer(
            ["w-full rounded-xl"],
            url: clip.Url,
            controls: true,
            autoplay: false,
            loop: false,
            muted: false,
            poster: clip.PosterUrl);  // optional still-frame shown before play
        #endregion
    }

    private static void DocRegisterFromType()
    {
        #region docsnippet:register-from-type
        FunctionRegistry.Instance.RegisterFromType(typeof(MathFunctions));
        #endregion
    }

    private static void DocRegisterFromInstance()
    {
        #region docsnippet:register-from-instance
        FunctionRegistry.Instance.RegisterFromInstance(new GreetingFunctions("Hello"));
        #endregion
    }

    private readonly ClientReactive<string> _clientTheme = new("light");

    private void DocReactiveScope()
    {
        #region docsnippet:reactive-scope
        var clientId = ReactiveScope.ClientId;

        // From background code (or to reach another client): name the target, no scope needed
        _clientTheme.SetFor(clientId, "dark");
        var theme = _clientTheme.ValueFor(clientId);

        // Scope a whole region instead when several reads/writes belong to the same client
        using var _ = ReactiveScope.Use(new ClientScope(clientId));
        _clientTheme.Value = "dark"; // Now targets the specified client
        #endregion

        Log.Instance.Debug($"theme {theme}");
    }

    private static void DocSilenceTriggeredRecognition()
    {
        #region docsnippet:silence-triggered-recognition
        var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
        var adapter = new SpeechRecognizerAdapter(recognizer, new SpeechRecognizerAdapter.Config
        {
            Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
            SilenceDuration = TimeSpan.FromMilliseconds(750),
            SilenceThreshold = 0.01f,
            MaxSpeechDuration = TimeSpan.FromSeconds(30)
        });
        #endregion

        adapter.Dispose();
    }

    private sealed record Settings(string Theme)
    {
        public static Settings Default => new("light");
    }

    private static async Task DocAssetSubscriptionAsync(AssetUri uri, Dictionary<AssetUri, Settings> cache)
    {
        #region docsnippet:asset-change-subscription
        await Asset.Instance.GetOrUpdateWithMetadataAsync<Settings>(uri,
            async (args, content) =>
            {
                if (content is null) { cache.Remove(uri); return; }
                cache[uri] = content.Content;
            },
            async _ => await Asset.Instance.SetAsync(uri, Settings.Default));
        #endregion
    }

    private readonly Reactive<string?> _streamId = new(null);

    private static void DocActionButton(UIView view)
    {
        #region docsnippet:action-button
        view.ActionButton([Button.PrimaryMd],
            action: ActionKind.CopyToClipboard,
            options: new CopyToClipboardActionOptions { Text = "Copied text!" },
            onActionComplete: async e => { /* e.Success */ },
            content: v => { v.Icon([Icon.Default, "mr-2"], name: "clipboard-copy"); v.Text(text: "Copy"); });
        #endregion
    }

    private void DocCaptureButtons(UIView view)
    {
        #region docsnippet:capture-buttons
        // Audio capture (microphone)
        view.CaptureButton([Button.OutlineMd, Button.Icon],
            kind: MediaCaptureKind.Audio,
            captureMode: MediaCaptureButtonMode.Toggle,  // or Hold
            audioOptions: new ClientAudioCaptureOptions { /* ... */ },
            onCaptureStart: async args => { _streamId.Value = args.StreamId; },
            onCaptureStop: async args => { _streamId.Value = null; },
            content: v => v.Icon([Icon.Default], name: "mic"));

        // Video capture (camera). Capture media always routes to the app on the server,
        // never to the other clients — the app decides any fan-out.
        view.CaptureButton([Button.OutlineMd, Button.Icon],
            kind: MediaCaptureKind.Camera,
            captureMode: MediaCaptureButtonMode.Toggle,
            videoOptions: new ClientVideoCaptureOptions { Framerate = 10, Width = 1280, Height = 720 },
            onCaptureStart: async args => { /* args.StreamId — but prefer args.StreamId from VideoInputStreamBeginAsync */ },
            onCaptureStop: async args => { /* cleanup */ },
            content: v => v.Icon([Icon.Default], name: "video"));
        #endregion
    }

    private sealed record CreativeResponse(string Tagline);

    private static double ScoreResponse(CreativeResponse response) => response.Tagline.Length;

    private static async Task DocBestOfAsync(string prompt)
    {
        #region docsnippet:emerge-bestof
        await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude46Sonnet, new KernelContext(), bo =>
        {
            bo.Command = $"Write a tagline for: {prompt}\n\nReturn JSON:\n{bo.JsonSchema}";
            bo.Count = 3;
            bo.Score = (response, trace) => ScoreResponse(response);
            bo.Candidate(c => { c.Temperature = 0.5 + c.Index * 0.2; });
        }))
        {
            if (ev is Completed<CreativeResponse> completed) { /* best candidate */ }
        }
        #endregion
    }

    private sealed record ChatResponse(string Reply);

    private static async Task DocConversationHistoryAsync(string userMessage, string nextUserMessage)
    {
        #region docsnippet:conversation-history
        // First user message — start with a fresh KernelContext
        var (result1, context) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a friendly assistant.";
            pass.Command = userMessage;
        }).FinalAsync();

        // Second message — pass the returned context so it carries the full conversation history automatically
        var (result2, context2) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, context, pass =>
        {
            pass.Command = nextUserMessage;
        }).FinalAsync();
        #endregion

        _ = (result1, result2, context2);
    }

    private async Task DocNotificationSendingAsync(int sessionId, string userId)
    {
        #region docsnippet:notification-sending
        // One connected session — sessionId is an int (e.g. ReactiveScope.ClientId inside a UI / onClick handler).
        NotificationSendResult r = await app.Notifications.SendToSessionAsync(
            sessionId, new NotificationContent("Build finished", "Your app deployed successfully."));

        // A user's connected sessions — userId is a string. Falls back to offline push when the user has NO connected session (see below).
        await app.Notifications.SendToUserAsync(userId, new NotificationContent("New message", "Alice replied"));

        // Everyone currently connected.
        await app.Notifications.BroadcastAsync(new NotificationContent("Maintenance in 5 min"));

        // Read permission state without sending.
        NotificationPermission p = await app.Notifications.GetPermissionAsync(sessionId);
        #endregion

        Log.Instance.Debug($"{r} {p}");
    }
    private sealed class MyStreamState(Context clientContext)
    {
        private readonly List<float> _samples = [];
        public Context ClientContext { get; } = clientContext;
        public IReadOnlyList<float> Samples => _samples;
        public void AddSamples(float[] samples) => _samples.AddRange(samples);
    }

    private readonly Dictionary<string, MyStreamState> _myStreamStates = [];

    private void DocRawAudioHandling()
    {
        #region docsnippet:raw-audio-handling
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            // Snapshot per-stream state here. args.ClientSessionId / args.UserId identify the client.
            _myStreamStates[args.StreamId] = new MyStreamState(args.ClientContext);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (!_myStreamStates.TryGetValue(args.StreamId, out var state)) return;
            state.AddSamples(args.Samples);
            if (args.IsLast) { /* process state.Samples */ }
        };
        #endregion
    }

    private async Task DocMessagesAsync(int trackId, int clientSessionId)
    {
        #region docsnippet:messages
        app.MessageReceivedAsync += async args => { /* args.Message.Opcode, args.Message.TrackId */ };
        await app.SendMessageAsync(ProtocolMessage.Create(app.SessionId, new RequestIdrVideoFrame(),
            trackId: trackId, targetIds: [clientSessionId]));
        #endregion
    }

    private void DocSpeechRecognition(UIView view)
    {
        #region docsnippet:speech-recognition
        // One-time setup in the app
        Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.SpeechRecognizedAsync += async args =>
        {
            // args.Text — recognized speech
            // args.ClientSessionId / args.UserId — who said it
            // ClientScope is established automatically — per-client reactive writes route correctly.
            await SendChatMessageAsync(args.Text);
        };

        // In your UI lambda:
        view.PushToTalkButton(style: ["w-16 h-16 rounded-full bg-red-600"]);
        #endregion
    }

    private sealed record DashboardLayout
    {
        public int Columns { get; init; }
    }

    private void DocHostServices(string gameId, int clientSessionId, int clientId)
    {
        #region docsnippet:host-services
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

    private static async Task DocAssetReadWriteAsync(AssetUri uri, string jsonString)
    {
        #region docsnippet:asset-read-write
        // Bytes
        var bytes = await Asset.Instance.GetBytesAsync(uri);
        await Asset.Instance.SetBytesAsync(uri, bytes, new AssetMetadata(mimeType: MimeTypes.ImageJpeg));

        // Text
        var text = await Asset.Instance.GetTextAsync(uri);
        await Asset.Instance.SetTextAsync(uri, jsonString);

        // Typed objects (JSON serialization)
        var layout = await Asset.Instance.GetAsync<DashboardLayout>(uri);
        await Asset.Instance.SetAsync(uri, new DashboardLayout { Columns = 3 });

        // Streams
        await using var readStream = (await Asset.Instance.GetReadStreamAsync(uri)).Content;
        await using var writeStream = await Asset.Instance.GetWriteStreamAsync(uri, new AssetMetadata(mimeType: "image/png"));
        #endregion

        Log.Instance.Debug($"{text} {layout} {readStream} {writeStream}");
    }

    private sealed record Courier(int SessionId, double Lat, double Lon);

    private readonly ReactiveList<Courier> _couriers = new();

    private async Task DocLocationTrackingAsync(int sessionId)
    {
        #region docsnippet:location-tracking
        // Observe fixes once (e.g. in Main). Handlers run on the pushing client's scope, so writing
        // per-user / per-session reactive state from inside just works.
        app.Locations.OnUpdate(update =>
        {
            // update: SessionId, UserId, Latitude, Longitude, AccuracyMeters, SpeedMps, Heading, At (UTC)
            _couriers.Update(cs => cs.Select(c =>
                c.SessionId == update.SessionId ? c with { Lat = update.Latitude, Lon = update.Longitude } : c));
        });

        // Start streaming on a client session — e.g. when a courier goes on shift.
        await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(
            IntervalSeconds: 5, DistanceFilterMeters: 10, Background: true,
            NotificationTitle: "Sharing your location", NotificationBody: "Visible while you're delivering."));

        // Stop when the shift ends.
        await app.Locations.StopTrackingAsync(sessionId);
        #endregion
    }
    private static Task DoWork() => Task.CompletedTask;

    #region docsnippet:loading-state-field
    private readonly Reactive<bool> _isLoading = new(false);
    #endregion

    private void DocLoadingState(UIView view)
    {
        #region docsnippet:loading-state
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
        #region docsnippet:lifecycle-events
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
        // The loop's CancellationTokenSource is a plain field with a field initializer
        // (`private readonly CancellationTokenSource _clockCts = new();`) — do NOT
        // declare the loop as a `readonly ClientReactiveEffect`/effect field and assign
        // it inside Main(): Main() is a normal method, not a constructor, so assigning a
        // `readonly` field there is CS0191/CS8618. A game tick / timer is just the
        // Task.Run loop above started from OnStarting (or directly in Main), not a
        // readonly effect object.
        #endregion
    }
}

// The validation app runs a real drag-and-drop board of its own, down to the same field names, so
// the version the docs teach lives here rather than colliding with it.
file sealed class DocDragDrop
{
    private sealed record DragItem(string Id, string Title);

    private static IEnumerable<DragItem> GetColumnItems(string columnId) => [];

    private static DragItem GetItem(string id) => new(id, id);

    private static Task HandleDrop(string activeId, string overId) => Task.CompletedTask;

    private void DocDragAndDropState() => Log.Instance.Debug($"{_activeDragId} {_dragOverColumnId}");

    #region docsnippet:drag-and-drop-state
    // Reactive state for drag tracking (lightweight, only IDs)
    private readonly Reactive<string?> _activeDragId = new(null);
    private readonly Reactive<string?> _dragOverColumnId = new(null);
    #endregion

    private void DocDragAndDrop(UIView view)
    {
        #region docsnippet:drag-and-drop
        // DndContext wraps the entire drag area
        view.DndContext(
            collisionDetection: CollisionDetection.RectIntersection,
            onDragStart: async args => { _activeDragId.Value = args.ActiveId; },
            onDragOver: async args => { _dragOverColumnId.Value = args.OverId; },
            onDragEnd: async args =>
            {
                _activeDragId.Value = null;
                _dragOverColumnId.Value = null;
                if (args.OverId != null) { await HandleDrop(args.ActiveId, args.OverId); }
            },
            onDragCancel: async () => { _activeDragId.Value = null; _dragOverColumnId.Value = null; },
            content: view =>
            {
                // Each column is a Droppable
                view.Droppable(["min-h-[100px]"], id: "column-1", content: v =>
                {
                    foreach (var item in GetColumnItems("column-1"))
                    {
                        // Each card is a Draggable with hideOnDrag
                        v.Draggable(["p-2 cursor-grab"], id: item.Id, hideOnDrag: true,
                            content: card => card.Text(text: item.Title));
                    }
                });

                // DragOverlay renders the floating drag preview
                view.DragOverlay(["shadow-lg opacity-90"], dropAnimation: true,
                    activeDragId: _activeDragId.Value,
                    content: v =>
                    {
                        if (_activeDragId.Value != null) { v.Text(text: GetItem(_activeDragId.Value).Title); }
                    });
            });
        #endregion
    }

}

file sealed class DocEfCore(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:ef-core-setup
    public class Note { public long Id { get; set; } public string Text { get; set; } = ""; public DateTime CreatedAt { get; set; } }

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }

    private AppDbContext CreateDbContext()
    {
        var info = app.Databases.First(d => d.Name == "mydb");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(info.ConnectionString).Options;
        return new AppDbContext(options);
    }
    #endregion

    #region docsnippet:ef-core-design-time-factory
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("IKON_DB") ?? throw new InvalidOperationException("IKON_DB is not set");
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options);
        }
    }
    #endregion

    public void MigrateAtStartup()
    {
        #region docsnippet:ef-core-migrate
        // at startup — applies every pending migration before the app serves traffic:
        app.OnStarting(async () => { await using var db = CreateDbContext(); await db.Database.MigrateAsync(); });
        #endregion
    }

    public async Task<int> QueryAsync(string text)
    {
        #region docsnippet:ef-core-query
        await using var db = CreateDbContext();
        db.Notes.Add(new Note { Text = text, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var recent = await db.Notes.OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
        #endregion

        return recent.Count;
    }
}

// The mutation catalogue needs its own `_items` — an item type with a `Done` flag — where the
// sortable-list example needs a list of ids. Two topics, two shapes, both verbatim.
file sealed class DocValueMutation
{
    private sealed record Item(bool Done);
    private sealed record Config(string Theme);

    private readonly Reactive<int> _count = new(0);
    private readonly ReactiveList<Item> _items = new();
    private readonly Reactive<Config> _config = new(new Config("light"));

    public static void Run() => new DocValueMutation().Mutate(new Item(false), []);

    private void Mutate(Item newItem, List<Item> imported)
    {
        #region docsnippet:value-mutation
        // Simple assignment
        _count.Value = 42;

        // List mutation — call the method on the ReactiveList itself; each call notifies once
        _items.Add(newItem);
        _items.RemoveAll(i => i.Done);
        _items.Update(list => list.Select(i => i with { Done = true }));  // whole-list transform, one notification
        _items.Value = imported;  // assignment replaces the whole content (same as ReplaceAll)

        // Record mutation
        _config.Value = _config.Value with { Theme = "dark" };
        #endregion
    }
}

#region docsnippet:style-organization
internal static class Styles
{
    public static readonly string[] PageContainer = [Container.Xl2, "py-8 px-4 min-h-screen"];
    public static readonly string[] MainCard = [Card.Default, Layout.Column.Lg, "p-10 w-full"];
}
#endregion

#region docsnippet:functions-static-class
public class MathFunctions
{
    [Function(Name = "Add", Description = "Adds two numbers", Visibility = FunctionVisibility.External)]
    public static int Add(int a, int b) => a + b;
}
#endregion

#region docsnippet:functions-instance-class
[RegisterAll(Visibility = FunctionVisibility.External)]
public class GreetingFunctions(string greeting)
{
    [Function(Name = "Greet", Description = "Greets a person")]
    public string Greet(string name) => $"{greeting}, {name}!";
}
#endregion


// `UI` is an instance member of the app class, so an example that builds a whole screen needs an app
// to hang it off. The holder is that app — the example inside is what a reader writes.
file sealed class DocChat(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:shared-messages-example
    // Shared state — all clients see the same messages
    private readonly ReactiveList<string> _messages = new();

    // Per-client state — each client has their own input
    private readonly ClientReactive<string> _input = new("");

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["h-screen"], content: view =>
            {
                // All clients see the same messages
                view.ScrollArea(autoScroll: true, autoScrollKey: _messages,
                    rootStyle: ["flex-1 min-h-0 px-4"], content: view =>
                {
                    foreach (var msg in _messages)
                    {
                        view.Text([Text.Body, "py-1"], msg);
                    }
                });

                // Each client has their own input
                view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
                {
                    view.TextField(bind: _input, style: ["flex-1"],
                        onSubmit: async submitted =>
                        {
                            _messages.Add(submitted); // Mutation methods notify on their own
                        },
                        clearOnSubmit: true);
                });
            });
        });
    }
    #endregion
}

public class VisibilityFunctions
{
    #region docsnippet:function-visibility
    [Function(Visibility = FunctionVisibility.External)]
    [RequireLogin]
    public string GetUserSecret() => "for logged-in users only";

    [Function(Visibility = FunctionVisibility.External)]
    [AllowAnonymous]
    public string GetPublicStatus() => "anyone can call this";
    #endregion
}


// `Audio` and `Video` are accessors an app declares for itself, so each holder is the app class the
// docs are talking about — the declaration inside the first region is the line a reader adds.
file sealed class DocAudio(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:audio-accessor
    private Audio Audio { get; } = new(app);
    #endregion

    public async Task RunAsync(string text, AudioChunk audioChunk, float[] samples, int sampleRate,
        int channelCount, string streamId, bool isFirst, bool isLast, CancellationToken ct)
    {
        #region docsnippet:audio-usage
        // Three ways to send audio — pick by how delivery is paced:

        // 1. Speech (TTS or AudioChunks through the speech mixer): real-time paced, new speech
        //    interrupts current speech with a fade. The default for spoken replies.
        await Audio.SpeakAsync(text);
        Audio.SendSpeech(audioChunk);

        // 2. Complete clip (decoded file, generated music): real-time paced, no mixer interruption.
        //    Await completes when the clip has been fully sent (≈ clip duration).
        await Audio.StreamAsync(samples, sampleRate, channelCount, streamId, cancellationToken: ct);

        // 3. Immediate, UNPACED transmit — only for audio already produced in real time (e.g. echoing
        //    mic frames back out) or very short clips. A long clip sent this way arrives all at once
        //    and can overflow client audio buffers — use StreamAsync for clips instead.
        await Audio.SendImmediateAsync(samples, sampleRate, channelCount, isFirst, isLast, streamId);

        // Receive audio input from client microphone. args carry args.ClientContext /
        // args.ClientSessionId / args.UserId — use these directly; do NOT plumb state through
        // onCaptureStart to identify the client (use args.ClientSessionId in the handler instead).
        Audio.AudioInputStreamBeginAsync += async args => { /* args.StreamId, args.SampleRate, args.ClientSessionId */ };
        Audio.AudioInputFrameAsync += async args => { /* args.Samples, args.IsFirst, args.IsLast, args.ClientSessionId */ };
        Audio.AudioInputStreamEndAsync += async args => { /* cleanup */ };

        // For push-to-talk → chat, prefer the higher-level Audio.SpeechRecognizedAsync / PushToTalkButton —
        // see "AI Speech & Audio" section.

        // Stream info and cleanup
        var info = Audio.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, SampleRate, ChannelCount
        await Audio.CloseAsync(streamId);
        await Audio.CloseAllAsync();
        #endregion

        Log.Instance.Debug($"{info}");
    }
}

file sealed class DocVideo(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:video-accessor
    private Video Video { get; } = new(app);
    #endregion

    public async Task RunAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs,
        uint durationInUs, VideoCodec codec, int width, int height, int framerate, string streamId)
    {
        #region docsnippet:video-usage
        // Receive video input from client camera/screen
        Video.VideoInputStreamBeginAsync += async args => { /* args.StreamId, args.Codec, args.Width, args.Height */ };
        Video.VideoInputFrameAsync += async args => { /* args.Data, args.FrameNumber, args.IsKey */ };
        Video.VideoInputStreamEndAsync += async args => { /* cleanup */ };

        // Forward/echo video to other clients. Frames are transmitted immediately — call once per
        // frame at the source framerate (e.g. forward each incoming frame as it arrives); never loop
        // over a stored clip's frames without pacing.
        await Video.SendFrameAsync(data, frameNumber, isKey, timestampInUs, durationInUs, codec, width, height, framerate, streamId);

        // Stream info and cleanup
        var info = Video.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, Width, Height, Framerate
        await Video.CloseAsync(streamId);
        await Video.CloseAllAsync();
        #endregion

        Log.Instance.Debug($"{info}");
    }
}


// The theme lives on the app's own UI accessor, so this holder is the app class the section is
// describing.
file sealed class DocTheme(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:theme-customization
    private UI UI { get; } = new(app, new IkonTheme
    {
        // Brand cluster — every brand-tinted CSS var, set explicitly.
        ["primary"]              = "violet-500",
        ["bg-brand-solid"]       = "violet-500",
        ["bg-brand-solid-hover"] = "violet-600",
        ["text-brand"]           = "violet-500",
        ["border-brand"]         = "violet-500",
        ["primary-foreground"]   = "#ffffff",

        // Surfaces.
        ["background"]   = "slate-950",
        ["text-primary"] = "slate-50",
        ["card"]         = "slate-900",
        ["border-primary"] = "slate-700",

        // Type + shape.
        ["font-heading"] = "Inter",
        ["radius-base"]  = "rounded-lg",

        // Per-token Tailwind overrides (optional).
        ["amber-400"]  = "#F5A524",     // re-skin a Tailwind palette step app-wide
        ["rounded-lg"] = "1.25rem",     // tune one radius rung
        ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)", // bespoke decorative ("--" declares a custom variable on purpose)

        DarkMode = new IkonTheme { ["primary"] = "violet-400", ["background"] = "slate-950" },
    });
    #endregion

    public void Use() => Log.Instance.Debug($"{UI}");
}

#region docsnippet:emerge-result-type
// Both sealed class and record work as result types
public sealed class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
}

// Records also work:
// public record AnalysisResult(string Summary, List<string> KeyPoints);
#endregion

file static class DocEmergeBasic
{
    public static async Task RunAsync(string topic)
    {
        #region docsnippet:emerge-basic
        // Streaming (observe each event)
        await foreach (var ev in Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a helpful analyst.";
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
            pass.Temperature = 0.7;
            pass.MaxOutputTokens = 32000;
            pass.MaxIterations = 5;
        }))
        {
            if (ev is Completed<AnalysisResult> completed)
            {
                var result = completed.Result;
            }
        }

        // Direct result (no streaming) — awaiting the run returns non-null T or throws EmergenceStoppedException
        var analysis = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
            pass.Temperature = 0.3;
        });
        #endregion

        Log.Instance.Debug($"{analysis}");
    }
}

#region docsnippet:pipeline-example
[Pipeline(name: "example")]
public class ExamplePipeline
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        var outputItems = inputItems.Transform(item => Process(item, cancellationToken));
        outputItems.Output();
    }

    [Processor]
    private static async Task<List<Item>> Process(Item inputItem, CancellationToken cancellationToken)
    {
        var text = await inputItem.GetContentAsString();
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.result", $"Processed: {text}", MimeTypes.TextPlain);
        return [outputItem];
    }
}
#endregion


// The viewport section is about how an app lays out its whole screen, so the holder carries the
// render helpers the example calls — the example itself is the two Columns.
file sealed class DocViewport
{
    private sealed record Message(string Text);

    private readonly ReactiveList<Message> _messages = new();

    private static void RenderHeader(UIView view) => view.Text(text: "header");

    private static void RenderMessage(UIView view, Message msg) => view.Text(text: msg.Text);

    private static void RenderInput(UIView view) => view.Text(text: "input");

    public void Render(UIView view)
    {
        #region docsnippet:viewport-layout
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


// The inbox is a field on the app class and its channels are wired from Main, so the holder is that
// app class — and it declares the same field twice under two names because the throttle example
// shows the same line with initializers on it.
file sealed class DocNotifications(IApp<SessionIdentity, ClientParams> app)
{
    private sealed record Profile(string Email = "", string Phone = "", string TelegramChatId = "");

    private sealed record Order(string Id, string CustomerUserId);

    private readonly PersistentUserReactive<Profile> _profiles = new(new Profile());

    #region docsnippet:notification-inbox-field
    private readonly NotificationInbox _inbox = new(app);
    #endregion

    private async Task WireAsync(string botToken, string accessToken, string phoneNumberId, Order order)
    {
        #region docsnippet:notification-inbox-channels
        // In Main(): the platform does not know users' addresses, so each channel takes a resolver.
        _inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => _profiles.ValueFor(userId).Email));
        _inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => _profiles.ValueFor(userId).Phone));
        _inbox.Channels.Add(new TelegramNotificationChannel(botToken, userId => _profiles.ValueFor(userId).TelegramChatId));
        _inbox.Channels.Add(new WhatsAppNotificationChannel(accessToken, phoneNumberId, userId => _profiles.ValueFor(userId).Phone));

        // One call. The route says where it goes.
        var outcome = await _inbox.NotifyAsync(order.CustomerUserId,
            new NotificationContent("Order delivered", "Enjoy your meal", Tag: order.Id, LaunchUrl: $"/orders/{order.Id}"),
            kind: "order",
            route: NotificationRoute.Everywhere("email"));          // inbox + every device + email
        #endregion

        Log.Instance.Debug($"{outcome}");
    }
}

file sealed class DocNotificationThrottle(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:notification-inbox-throttle
    private readonly NotificationInbox _inbox = new(app) { MaxPushPerWindow = 5, PushWindow = TimeSpan.FromMinutes(10) };
    #endregion

    public void Use() => Log.Instance.Debug($"{_inbox}");
}


file static class DocMotion
{
    public static void Basics(UIView view)
    {
        #region docsnippet:motion-basics
        // Fade in
        view.Box(["motion-[0:opacity-0,100:opacity-100] motion-duration-500ms"], content: view =>
        {
            view.Text([Text.Body], "I fade in!");
        });

        // Slide up + fade in
        view.Box(["motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"]);

        // Glow pulse (looping)
        view.Box(["motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"]);

        // Per-letter wave animation (each letter animates independently)
        view.Text(["wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] wave:motion-duration-2500ms wave:motion-per-letter wave:motion-loop"], "Hello");

        // Per-letter fade-in with stagger delay (letters appear one by one)
        view.Text(["motion-[0:opacity-0,100:opacity-100] motion-duration-300ms motion-per-letter motion-letter-delay-60ms"], "Appearing!");

        // Per-word animation
        view.Text(["motion-[0:opacity-0_translate-y-[10px],100:opacity-100_translate-y-0] motion-duration-500ms motion-per-word motion-letter-delay-100ms"], "Each word slides in");
        #endregion
    }

    public static void Advanced(UIView view)
    {
        #region docsnippet:motion-advanced
        // Shimmer/loading effect — translate a gradient overlay
        view.Box(["w-full h-4 rounded bg-muted relative overflow-hidden " +
            "before:content-[''] before:absolute before:inset-0 " +
            "before:bg-[linear-gradient(90deg,transparent,rgba(255,255,255,0.5),transparent)] " +
            "before:w-[200%] " +
            "before:shimmer:motion-[0:translate-x-[-50%],100:translate-x-[0%]] " +
            "before:shimmer:motion-duration-1000ms before:shimmer:motion-ease-linear before:shimmer:motion-loop"]);

        // Scale + blur entrance
        view.Box(["motion-[0:opacity-0_scale-[0.5]_blur-[4px],100:opacity-100_scale-100_blur-0] motion-duration-500ms"]);
        #endregion
    }
}

file sealed class DocMotionApp(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:motion-main
    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["h-screen items-center justify-center gap-4"], content: view =>
            {
                // Animated heading with fade-in + slide
                view.Text([Text.H2, "motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"], "Welcome!");

                // Pulsing glow button
                view.Button([Button.PrimaryMd, "motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"],
                    text: "Click me");
            });
        });
    }
    #endregion
}

public sealed record MyClickEventArgs(string Id);

#region docsnippet:custom-component-extension
public static class MyComponentExtensions
{
    public static void MyComponent(
        this UIView view,
        string someProp,
        Func<MyClickEventArgs, Task>? onClick = null,
        string[]? style = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onClickId = null;

        if (onClick != null)
        {
            onClickId = view.CreateAction<MyClickEventArgs>(args => onClick(args.Value));
        }

        view.AddNode(
            "my-component",
            new Dictionary<string, object?>
            {
                ["someProp"] = someProp,
                ["onClickId"] = onClickId
            },
            style: style,
            file: file,
            line: line);
    }
}
#endregion

file static class DocCustomComponent
{
    private sealed record Bot(string Id, string DraftCode);

    private static void UpdateActiveDraftCode(string code) => Log.Instance.Debug($"draft {code}");

    public static void Use(UIView view)
    {
        #region docsnippet:custom-component-use
        view.MyComponent("Hello from custom component",
            onClick: async args => { Log.Instance.Info("Clicked!"); },
            style: ["w-full rounded-lg"]);
        #endregion
    }

    public static void Stateful(UIView view)
    {
        var activeBot = new Bot("bot-1", "");

        #region docsnippet:custom-component-key
        view.AddNode(
            type: "custom.lua-editor",
            key: $"editor:{activeBot.Id}",  // remount when activeBot changes
            props: new Dictionary<string, object?>
            {
                ["value"] = activeBot.DraftCode,
                ["onValueChangeId"] = view.CreateAction<string>(args =>
                {
                    UpdateActiveDraftCode(args.Value ?? "");
                    return Task.CompletedTask;
                }),
            });
        #endregion
    }
}


file sealed class DocMediaUpload(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:media-upload-state
    private readonly Reactive<AssetUri?> _mediaAssetUri = new(null);
    #endregion

    private static Task AnalyzeMediaAsync(AssetUri assetUri) => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region docsnippet:media-upload
        view.FileUpload(
            accept: ["video/*", "audio/*"],
            maxFileSize: 2L * 1024 * 1024 * 1024,
            onUploadStart: async args =>
            {
                var assetUri = new AssetUri(AssetClass.CloudFile, $"uploads/{args.Hash}/{args.FileName}", spaceId: app.GlobalState.SpaceId);
                return new FileUploadResult { Accepted = true, AssetUri = assetUri };
            },
            onUploadComplete: async args =>
            {
                if (args.AssetUri is not { } assetUri)
                {
                    return;
                }

                _mediaAssetUri.Value = assetUri;
                await AnalyzeMediaAsync(assetUri);
            });
        #endregion
    }
}

file static class DocMediaProbe
{
    #region docsnippet:media-probe
    private static async Task<JsonDocument?> ProbeMediaAsync(AssetUri assetUri)
    {
        // The signed URL is temporary (.UrlIsTemporal) — fetch it fresh
        // right before each ffprobe/ffmpeg invocation, never persist it
        var metadata = await Asset.Instance.GetMetadataAsync(assetUri);

        if (metadata.Url is null)
        {
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ArgumentList, never an interpolated Arguments string — URL characters can
        // otherwise be misparsed as ffprobe option flags
        foreach (var arg in new[] { "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", metadata.Url })
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return null;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            string output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0 ? JsonDocument.Parse(output) : null;
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { /* the probe is already being abandoned; a kill that fails changes nothing */ }

            return null;
        }
    }
    #endregion

    public static async Task ExtractAudioAsync(string url)
    {
        #region docsnippet:media-ffmpeg-pipe
        // Extract mono 16 kHz PCM audio (e.g. for speech recognition); for thumbnail
        // frames instead, swap the output args for "-f", "image2pipe", "-vcodec", "mjpeg"
        // with a select/fps filter
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in new[] { "-loglevel", "quiet", "-i", url, "-vn", "-f", "f32le", "-ac", "1", "-ar", "16000", "pipe:1" })
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return;
        }

        await using var audioStream = process.StandardOutput.BaseStream;
        // read fixed-size chunks from audioStream — do NOT ReadToEnd a long file
        #endregion

        await ProbeMediaAsync(default);
    }
}


file sealed class DocSecrets(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:secrets-runtime
    public async Task Main()
    {
        string token = app.Secrets["GITHUB_TOKEN"];

        if (app.Secrets.TryGet("SENTRY_DSN", out var dsn))
        {
            // wire up optional integration
        }
    }
    #endregion
}


// The scope contrast is the same method written two ways, so each way needs a class of its own.
file sealed class DocScopeWrong
{
    private readonly UserReactive<bool> _hasJoined = new(false);

    private static void RenderTavern() { }

    #region docsnippet:scope-requirements-wrong
    // WRONG — crashes at startup, no user scope active
    public async Task Main()
    {
        if (_hasJoined.Value) { /* ... */ }  // UserReactive — throws InvalidOperationException
        RenderTavern();
    }
    #endregion
}

file sealed class DocScopeRight(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    private readonly UserReactive<bool> _hasJoined = new(false);

    private static void RenderTavern(UIView view) { }

    private static void RenderEntry(UIView view) { }

    #region docsnippet:scope-requirements-right
    // CORRECT — branch inside UI.Root() where scopes are active
    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            if (_hasJoined.Value) { RenderTavern(view); }  // OK — inside UI.Root()
            else { RenderEntry(view); }
        });
    }
    #endregion
}


file static class DocSimpleStyling
{
    public static void Render(UIView view)
    {
        #region docsnippet:styling-oneliners
        view.Button([Button.PrimaryMd, "mt-2 w-fit self-center"], text: "Submit");
        view.Box([Card.Default, "p-6 mb-4"], content: view => { /* ... */ });
        #endregion
    }

    public static void Keyboard(UIView view)
    {
        #region docsnippet:keyboard-listener
        // KeyboardListener
        view.KeyboardListener(global: true,
            onKeyDown: async args => { /* args.Key, args.ShiftKey, args.CtrlKey */ },
            content: view => { /* ... */ });
        #endregion
    }

    public static async Task OptimisticConcurrencyAsync(AssetUri uri, string modified)
    {
        #region docsnippet:asset-optimistic-concurrency
        var content = await Asset.Instance.GetTextWithMetadataAsync(uri);
        // ... modify content.Content ...
        var result = await Asset.Instance.TrySetTextAsync(uri, modified, new AssetMetadata(lastModified: content.MetaData?.LastModified));
        if (result.IsConflict) { /* re-read and retry */ }
        #endregion
    }
}

file sealed class DocChatLayout
{
    private sealed record Message(string Author, string Text);

    private readonly ReactiveList<Message> _messages = new();

    private readonly ClientReactive<string> _input = new("");

    public void Render(UIView view)
    {
        #region docsnippet:chat-layout
        // Complete chat interface pattern:
        view.Column(["h-screen"], content: view =>
        {
            // Header
            view.Text([Text.H2, "p-4 flex-shrink-0"], "Chat");

            // Scrollable message area with auto-scroll
            view.ScrollArea(
                autoScroll: true,
                autoScrollKey: _messages,
                rootStyle: ["flex-1 min-h-0 px-4"],
                content: view =>
                {
                    foreach (var msg in _messages)
                    {
                        view.Box(["py-2"], content: view =>
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], msg.Author);
                            view.Text([Text.Body], msg.Text);
                        });
                    }
                });

            // Input area — Enter submits, auto-clears
            view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
            {
                view.TextField(bind: _input, style: ["flex-1"],
                    placeholder: "Type a message...",
                    onSubmit: async submitted =>
                    {
                        // Use the `submitted` parameter — NOT `_input.Value`. onValueChange round-trips
                        // separately and may not have landed when onSubmit fires for a fast typist.
                        if (!string.IsNullOrWhiteSpace(submitted))
                        {
                            _messages.Add(new Message("User", submitted));
                        }
                    },
                    clearOnSubmit: true);
            });
        });
        #endregion
    }
}

file sealed class DocInputs
{
    private readonly Reactive<string> _text = new("");
    private readonly Reactive<bool> _checked = new(false);
    private readonly Reactive<bool> _enabled = new(false);
    private readonly Reactive<double> _slider = new(0);
    private readonly Reactive<string> _selected = new("");
    private readonly Reactive<string> _radio = new("");
    private readonly Reactive<bool> _isDragging = new(false);

    private static Task HandleSubmit(string submitted) => Task.CompletedTask;

    public void Render(UIView view, bool isLoading)
    {
        #region docsnippet:input-components
        view.Button([Button.PrimaryMd], text: "Click", onClick: async () => { /* ... */ });
        view.Button([Button.OutlineMd], text: "Secondary", disabled: isLoading, onClick: async () => { /* ... */ });
        view.Button([Button.GhostMd, Button.Icon], onClick: async () => { /* ... */ },
            content: v => v.Icon([Icon.Default], name: "settings"));
        view.TextField(bind: _text, placeholder: "Enter text",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Enter submits; input auto-clears after submit
        view.TextArea(bind: _text, style: ["min-h-[100px]"], placeholder: "Type a message...",
            onSubmit: async submitted => { await HandleSubmit(submitted); });  // Ctrl+Enter submits; input auto-clears after submit
        // IMPORTANT — onSubmit's parameter is the submitted value. Always use it. Do NOT re-read the bound reactive
        // (`_text.Value`) inside onSubmit: onValueChange is a separate round-trip and may not have landed when
        // onSubmit fires for a fast typist, so the reactive can be one keystroke behind.
        // Note: both TextField and TextArea clear on submit only when an onSubmit handler is set; a bound field
        // with no onSubmit keeps its value. Pass clearOnSubmit: true/false to override either way.
        // Checkbox / Switch / Slider / RadioGroup / Toggle auto-render their inner part (the
        // check mark, the switch thumb, the slider track+thumb, the radio dot) AND their default
        // styling — the bare call below is all you need; you do NOT have to compose a
        // CheckboxIndicator / SwitchThumb / SliderTrack child or pass a [*.Default] style.
        // Pass a content: lambda only to put CUSTOM content inside (e.g. a different icon), or a
        // style: array only to override the default look. To render a checkbox with no check mark
        // at all, opt out explicitly with content: _ => { }.
        view.Checkbox(bind: _checked, label: "Enable feature");  // label: renders a clickable trailing label
        view.Switch(bind: _enabled);
        view.Slider(bind: _slider, min: 0, max: 100, step: 1);   // scalar single-thumb; pass value: [a, b] lists for multi-thumb
        view.Select(bind: _selected, placeholder: "Choose...",
            options: [new SelectOption("a", "Option A"), new SelectOption("b", "Option B")]);
        view.RadioGroup(bind: _radio,
            content: view =>
            {
                // The item is just the radio circle — render the label as a SIBLING, never as item content
                view.Row([Layout.Row.Sm], content: view =>
                {
                    view.RadioGroupItem([RadioGroup.Item], value: "opt1",
                        content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                    view.Text(text: "Option 1");
                });
                view.Row([Layout.Row.Sm], content: view =>
                {
                    view.RadioGroupItem([RadioGroup.Item], value: "opt2",
                        content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                    view.Text(text: "Option 2");
                });
            });
        view.FileUpload(
            onUploadComplete: async args => { /* args.UploadId, args.FileName, args.MimeType, args.Size, args.LocalTempFilePath, args.AssetUri */ },
            onUploadProgress: async args => { /* args.ProgressPercentage (0-100), args.BytesUploaded, args.Size */ },
            accept: [".jpg", ".png", ".pdf"],  // optional file type filter
            maxFileSize: 10 * 1024 * 1024);    // optional max size in bytes

        // FileUploadZone wraps any content with drag-drop + paste upload capability
        view.FileUploadZone(
            accept: ["video/*"],
            onUploadComplete: async args => { /* args.FileName, args.MimeType, args.Size, args.LocalTempFilePath, args.AssetUri */ },
            onUploadProgress: async args => { /* args.ProgressPercentage, args.BytesUploaded */ },
            onDragActiveChange: async isDragging => { _isDragging.Value = isDragging; },
            zoneStyle: [FileUpload.Zone.Base],
            activeStyle: [FileUpload.Zone.Active],
            content: v => v.Text([Text.Caption], "Drop files here"));
        #endregion
    }
}


file sealed class DocOverlays
{
    private readonly Reactive<bool> _open = new(false);
    private readonly Reactive<bool> _alertOpen = new(false);
    private readonly Reactive<bool> _popOpen = new(false);
    private readonly Reactive<bool> _toastOpen = new(false);

    public void Render(UIView view)
    {
        #region docsnippet:overlay-components
        // Dialog
        view.Dialog(open: _open.Value, onOpenChange: async o => _open.Value = o,
            overlayStyle: [Dialog.Overlay], contentStyle: [Dialog.Content],
            trigger: view => view.Button([Button.OutlineMd], text: "Open"),
            contentSlot: view =>
            {
                view.Box([Dialog.Header], content: view =>
                {
                    view.Text([Dialog.Title], "Title");
                    view.Text([Dialog.Description], "Description");
                });
                view.Text([Text.Body, "my-4"], "Content");
                view.Box([Dialog.Footer], content: view =>
                {
                    view.Button([Button.OutlineMd], text: "Cancel", onClick: async () => _open.Value = false);
                    view.Button([Button.PrimaryMd], text: "Confirm", onClick: async () => _open.Value = false);
                });
            });

        // AlertDialog
        view.AlertDialog(open: _alertOpen.Value, onOpenChange: async o => _alertOpen.Value = o,
            overlayStyle: [AlertDialog.Overlay], contentStyle: [AlertDialog.Content],
            trigger: view => view.Button([Button.ErrorMd], text: "Delete"),
            title: "Are you sure?", titleStyle: [AlertDialog.Title],
            description: "This action cannot be undone.", descriptionStyle: [AlertDialog.Description],
            footerStyle: [AlertDialog.Footer], cancelLabel: "Cancel", cancelStyle: [AlertDialog.Cancel],
            actionLabel: "Delete", actionStyle: [Button.ErrorMd]);

        // Popover, Tooltip, HoverCard
        view.Popover(open: _popOpen.Value, onOpenChange: async o => _popOpen.Value = o,
            contentStyle: [Popover.Content],
            trigger: view => view.Button([Button.OutlineMd], text: "Open"),
            contentSlot: view => { /* ... */ });
        view.Tooltip(contentStyle: [Tooltip.Content],
            trigger: view => view.Button([Button.OutlineMd], text: "Hover me"),
            contentSlot: view => view.Text(text: "Tooltip text"));
        view.HoverCard(contentStyle: [HoverCard.Content],
            trigger: view => view.Text([Text.Link], "@user"),
            contentSlot: view => { /* ... */ });

        // Toast
        view.Toast(open: _toastOpen.Value, onOpenChange: async o => _toastOpen.Value = o,
            viewportStyle: [Toast.ViewportBottomCenter], toastStyle: [Toast.Base],
            title: "Success", titleStyle: [Toast.Title],
            description: "Action completed", descriptionStyle: [Toast.Description],
            durationMs: 3000, showClose: true, closeStyle: [Toast.Close]);
        #endregion
    }
}

file sealed class DocNavigation(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<string> _tab = new("home");
    private readonly Reactive<string> _accordionValue = new("");
    private readonly Reactive<bool> _open = new(false);
    private readonly Reactive<bool> _hasMore = new(true);
    private readonly Reactive<bool> _loading = new(false);
    private readonly ReactiveList<string> _items = new();

    private static void RenderHome(UIView view) { }

    private static void RenderSettings(UIView view) { }

    private static Task LoadMoreItems() => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region docsnippet:navigation-components
        // Tabs with routing
        view.Tabs(value: _tab.Value, onValueChange: async v =>
            {
                _tab.Value = v;
                await app.Navigation.SetPathAsync($"/{v}");
            },
            listContainerStyle: [Card.Default, "p-2 mb-4"],
            listStyle: [Tabs.List], triggerStyle: [Tabs.Trigger], contentStyle: [Tabs.Content],
            tabs: [
                new TabItem("home", "Home", RenderHome),
                new TabItem("settings", "Settings", RenderSettings),
            ]);

        // Accordion (single open item at a time)
        view.AccordionSingle(value: _accordionValue.Value,
            onValueChange: async v => _accordionValue.Value = v,
            content: view =>
            {
                view.AccordionItem(value: "item1", content: view =>
                {
                    view.AccordionHeader(content: view =>
                    {
                        view.AccordionTrigger(content: view => view.Text(text: "Section 1"));
                    });
                    view.AccordionContent(content: view => view.Text(text: "Content 1"));
                });
            });

        // Collapsible
        view.Collapsible(open: _open.Value, onOpenChange: async o => _open.Value = o,
            content: view =>
            {
                view.CollapsibleTrigger(content: view => view.Button(text: "Toggle"));
                view.CollapsibleContent(content: view => { /* ... */ });
            });

        // InfiniteScrollView
        view.InfiniteScrollView(
            rootStyle: ["h-[400px]"],
            hasMore: _hasMore.Value,
            loading: _loading.Value,
            onNearEnd: async args => { await LoadMoreItems(); },
            content: view => { foreach (var item in _items) { view.Text(text: item); } });
        #endregion
    }
}

file sealed class DocEndpoints
{
    private static bool VerifyStripe(string? signature, string body) => signature != null && body.Length > 0;

    #region docsnippet:http-endpoints
    // The JSON body binds to your typed parameter. The binder is lenient — missing
    // fields default, unknown fields are ignored, and bad input returns a 4xx (it never throws a 500).
    [HttpPost("/sum")]
    public HttpResult Sum(SumRequest req) => HttpResult.Ok(new { sum = req.A + req.B });

    // Explicit verb, no body. Return a value (→ JSON), a string (→ text/plain), or an HttpResult.
    [HttpGet("/health")]
    public string Health() => "ok";

    // A third-party webhook is a normal [HttpPost]. It must be Auth = Public: the default (Grant)
    // makes the gateway reject the bare URL with 401 before the handler runs, and a provider like
    // Stripe calls a fixed URL it cannot carry a grant on. Read the signature header + raw body from
    // the injected Ikon.App.HttpRequest and verify it yourself — the signature IS the authorization.
    [HttpPost("/stripe", Auth = EndpointAuth.Public)]
    public async Task<HttpResult> Stripe(Ikon.App.HttpRequest req)
    {
        if (!VerifyStripe(req.Headers["Stripe-Signature"], req.Body)) return HttpResult.Unauthorized();
        // ... process req.Body ...
        return HttpResult.Ok();   // return 200 even on a skip to avoid the provider's retry storm
    }

    // An MCP tool, callable by an LLM / agent. Its JSON Schema is reflected from the signature.
    [Mcp(Name = "add_numbers", Description = "Adds two integers")]
    public int AddNumbers(int a, int b) => a + b;
    #endregion
}

#region docsnippet:http-request-record
public record SumRequest(int A, int B);
#endregion

file sealed class DocMinting(IApp<SessionIdentity, ClientParams> app)
{
    [HttpGet("/doc")]
    public string GetDocument() => "doc";

    [HttpGet("/doc-alias")]
    public string GetDoc() => "doc";

    [HttpPost("/doc")]
    public string UpdateDoc() => "ok";

    [HttpPost("/sum-alias")]
    public string Sum() => "ok";

    public async Task MintAsync()
    {
        #region docsnippet:url-minting
        // Pin a resource identity into a signed grant in the URL:
        MintedUrl minted = await app.MintUrlAsync(nameof(GetDocument), new { DocumentId = "doc-42" });
        string url = minted.Url;   // https://{space}.ikonai.app/api/...?ikon-grant=...

        // Omit the identity to pin THIS instance's own identity (the URL routes back here):
        MintedUrl self = await app.MintUrlAsync(nameof(Sum));

        // Batch several endpoints under one identity in a single backend round-trip:
        IReadOnlyDictionary<string, MintedUrl> urls = await app.MintUrlsAsync(
            new[] { nameof(GetDoc), nameof(UpdateDoc) }, new { DocumentId = "doc-42" });
        #endregion

        Log.Instance.Debug($"{url} {self} {urls.Count}");
    }
}

#region docsnippet:pipeline-secret
[Pipeline]
public class FetchFromGithub(IPipelineHost<EmptyPipelineConfig> host)
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        string token = host.Secrets["GITHUB_TOKEN"];

        if (host.Secrets.TryGet("GITHUB_API_BASE", out var apiBase))
        {
            Log.Instance.Info($"Using custom GitHub API base: {apiBase}");
        }

        Log.Instance.Info($"Running in organisation {host.OrganisationId} space {host.SpaceId}");

        // ...
        await Task.CompletedTask;
    }
}
#endregion

file sealed class DocInboxUi(IApp<SessionIdentity, ClientParams> app)
{
    private readonly NotificationInbox _inbox = new(app);

    private static Task NavigateAsync(string? launchUrl) => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region docsnippet:notification-inbox-ui
        view.Badge($"{_inbox.UnreadCount}");              // signed-in user
        foreach (var item in _inbox.Items)                // newest first
        {
            view.Box([Card.Default, "p-3 mb-2"], onClick: async () => { _inbox.MarkRead(item.Id); await NavigateAsync(item.LaunchUrl); });
        }
        #endregion
    }
}
