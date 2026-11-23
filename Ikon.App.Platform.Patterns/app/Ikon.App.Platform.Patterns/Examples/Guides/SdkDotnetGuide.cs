using System.Runtime.InteropServices;
using Ikon.App.Platform.Patterns.Protocol;
#region example:sdk-console-usings
using Ikon.Common.Core;
using Ikon.Common.Core.Protocol;
using Ikon.Sdk;
#endregion

namespace Ikon.App.Platform.Patterns.Examples;

// The .NET SDK guide, as code that compiles.
//
// Unlike the app-facing guides this one is about a STANDALONE client — a console or desktop app that
// connects to a space — so its `Console.WriteLine` calls are correct as written and stay.

#region example:sdk-functions-class
public class MyFunctions
{
    [Function(Description = "Greets a user by name")]
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    [Function(Description = "Calculates sum", Visibility = FunctionVisibility.External)]
    public async Task<int> AddAsync(int a, int b)
    {
        return a + b;
    }

    [Function(Description = "Streams numbers")]
    public async IAsyncEnumerable<int> CountAsync(int max)
    {
        for (int i = 0; i < max; i++)
            yield return i;
    }
}
#endregion

public class MyVisibilityFunctions
{
    #region example:sdk-function-visibility
    // Local - not advertised (default); only an IKON app keeps remote callers out
    [Function(Visibility = FunctionVisibility.Local)]
    public string LocalOnly() => "local";

    // External - advertised over the protocol; the app can call it
    [Function(Visibility = FunctionVisibility.External)]
    public string SharedWithAll() => "shared";
    #endregion
}

file static class SdkReadmeExamples
{
    private static ReadOnlyMemory<float> GetAudioSamples() => new float[480];

    private static string SearchDatabase(string query) => query;

    private sealed class MyStaticFunctions;

    public static async Task QuickstartAsync()
    {
        #region example:sdk-quickstart
        // Create configuration with API key authentication
        var config = new IkonClientConfig
        {
            ApiKey = new ApiKeyConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("IKON_API_KEY")!,
                SpaceId = "your-space-id",
                ExternalUserId = "user-123",
                BackendType = BackendType.Production // The default; Development for an app `ikon status` lists under development (*.dev.ikonai.app)
            },
            Description = "My App"
        };

        // Create and connect the client
        await using var client = new IkonClient(config);

        client.ReadyAsync += async e =>
        {
            Console.WriteLine("Connected!");
        };

        client.MessageReceivedAsync += async e =>
        {
            Console.WriteLine($"Received: {e.Message.Opcode}");
        };

        try
        {
            await client.ConnectAsync();
        }
        catch (HttpRequestException ex)
        {
            // A first connect that is refused throws, such as 403 Invalid key for a deleted key
            Console.WriteLine($"Connect refused: {ex.Message}");
        }
        #endregion
    }

    public static void DisableUdp()
    {
        #region example:sdk-disable-udp
        var config = new IkonClientConfig
        {
            // ... authentication ...
            EnableUdpChannel = false,
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void RequirePostQuantum()
    {
        #region example:sdk-require-post-quantum
        var config = new IkonClientConfig
        {
            // ... authentication ...
            RequirePostQuantum = true,
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void ApiKeyConfig()
    {
        #region example:sdk-api-key-config
        var config = new IkonClientConfig
        {
            ApiKey = new ApiKeyConfig
            {
                ApiKey = "ikon-xxxxx",           // From `ikon api-key create` in the app's folder
                SpaceId = "...",                  // The app's id
                ExternalUserId = "user-123",      // Your user identifier
                SessionIdentityHash = "...",      // Optional: attach to a specific live session (connect fails if none owns this hash)
                BackendType = BackendType.Production, // The default; Development for an app on *.dev.ikonai.app
                UserType = UserType.Human,
                ClientType = ClientType.DesktopApp
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void LocalConfig()
    {
        #region example:sdk-local-config
        var config = new IkonClientConfig
        {
            Local = new LocalConfig
            {
                Host = "localhost",
                // The port of the Server URL that `ikon run` prints, which is not always 8443
                HttpsPort = int.Parse(Environment.GetEnvironmentVariable("IKON_LOCAL_PORT") ?? "8443"),
                // The app id: a connect to a run of another app on that port throws instead of joining it
                SpaceId = "your-app-id",
                UserId = "dev-user"
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void BackendConfig()
    {
        #region example:sdk-backend-config
        var config = new IkonClientConfig
        {
            Backend = new BackendConfig
            {
                SpaceId = "...",
                ExternalUserId = "user-123",     // Your user identifier
                SessionIdentityHash = "...",     // Optional: attach to a specific live session (connect fails if none owns this hash)
                UserType = UserType.Human,
                ClientType = ClientType.DesktopApp
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void ExternalConnectUrl(string connectUrl)
    {
        #region example:sdk-external-connect-url
        var config = new IkonClientConfig
        {
            ExternalConnectUrl = connectUrl
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void UserLoginConfig()
    {
        #region example:sdk-user-login-config
        var config = new IkonClientConfig
        {
            UserLogin = new UserLoginConfig
            {
                SpaceId = "...",              // required
                // An app on *.dev.ikonai.app, as `ikon status` shows its environment; null for the process's own platform
                Environment = IkonBackend.EnvironmentType.Development,
                UserType = UserType.Human,
                ClientType = ClientType.DesktopApp
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void ClientEvents(IkonClient client)
    {
        #region example:sdk-client-events
        // The other clients in the session, and the app itself (IsInternal, a new session after each restart or hot reload)
        client.ClientJoinedAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} joined as {e.ClientContext.ContextType}, user {e.ClientContext.UserId}");
        client.ClientUpdatedAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} now uses the {e.ClientContext.Theme} theme");
        client.ClientLeftAsync += async e => Console.WriteLine($"{e.ClientContext.SessionId} left");
        #endregion
    }

    public static async Task LifecycleAsync(IkonClient client)
    {
        #region example:sdk-lifecycle
        // Connect (will throw on failure)
        await client.ConnectAsync();

        // Wait for a specific client to connect
        bool found = await client.WaitForClientAsync(
            productId: "my-product",
            userId: "user-123",
            timeout: TimeSpan.FromSeconds(30)
        );

        // Disconnect
        await client.DisconnectAsync();

        // Or dispose (also disconnects)
        await client.DisposeAsync();
        #endregion

        Log.Instance.Debug($"{found}");
    }

    public static void Timeouts()
    {
        #region example:sdk-timeouts
        var config = new IkonClientConfig
        {
            // ... authentication config ...
            Timeouts = new TimeoutConfig
            {
                InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
                MaxReconnectAttempts = 4,                                 // Max attempts (default)
                MaxReconnectDelay = TimeSpan.FromMinutes(5),              // Background backoff delay cap (default)
                ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
                BackgroundReconnect = true                                // Keep retrying after max attempts (default)
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void CredentialRejected(IkonClient client)
    {
        #region example:sdk-credential-rejected
        client.CredentialRejectedAsync += async e =>
        {
            // A UserToken client resumes with a new token; any client can call ConnectAsync again
            client.UpdateAuthToken(await GetNewTokenAsync());
        };
        #endregion
    }

    // Stands for the program's own way of getting a token
    private static Task<string> GetNewTokenAsync() => Task.FromResult(string.Empty);

    public static async Task SendRawAsync(IkonClient client, IProtocolMessagePayload payload)
    {
        #region example:sdk-send-raw
        // Send a raw protocol message (on a connected client)
        var message = ProtocolMessage.Create(client.ClientContext!.SessionId, payload);
        await client.SendMessageAsync(message);
        #endregion
    }

    public static async Task SendAudioAsync(IkonClient client)
    {
        #region example:sdk-send-audio
        // Default encoder options for every stream that sends no encoderOptions of its own. A
        // stream's encoder is created on its first SendAudioAsync and keeps the options in force
        // then, so set this before the first send — not after.
        client.DefaultEncoderOptions = new AudioEncoderOptions(bitrate: 48000, complexity: 8);

        // Get audio samples (float PCM, range [-1.0, 1.0]) at 8, 12, 16, 24 or 48 kHz in 1 or 2 channels;
        // the SDK does not resample, and a stream's first send throws ArgumentOutOfRangeException otherwise
        ReadOnlyMemory<float> samples = GetAudioSamples();

        // Send audio
        await client.SendAudioAsync(
            MediaTargets.Everyone,
            samples: samples,
            sampleRate: 48000,
            channelCount: 1,
            isFirst: true,      // First chunk of this stream
            isLast: false       // More chunks coming
        );

        // Send final chunk
        await client.SendAudioAsync(MediaTargets.Everyone, samples, 48000, 1, isFirst: false, isLast: true);

        // An app's speech recognition skips a stream with no correlation id unless the app turned
        // that off. Pass the id the app gave for the capture, or any id of your own. When a new
        // segment uses another id, the SDK announces the stream again with that id
        await client.SendAudioAsync(MediaTargets.Everyone, samples, 48000, 1, isFirst: true, isLast: true, correlationId: "question-1");

        // Optional: specify stream ID, total duration and encoder options.
        // A stream's sample rate and channel count are fixed by its first send, and every call
        // without a streamId shares one stream, so audio in another format needs its own streamId
        await client.SendAudioAsync(
            MediaTargets.Everyone,                    // A client's audio reaches the app
            samples: samples,
            sampleRate: 48000,
            channelCount: 1,
            isFirst: true,
            isLast: true,
            streamId: "my-audio-stream",              // Unique stream identifier
            totalDuration: TimeSpan.FromSeconds(5),
            encoderOptions: new AudioEncoderOptions(  // Custom encoder settings
                bitrate: 64000,
                complexity: 10
            ));

        #endregion
    }

    public static void ReceiveAudio(IkonClient client)
    {
        #region example:sdk-receive-audio
        client.AudioInputStreamBeginAsync += async e =>
        {
            Console.WriteLine($"Audio stream started: {e.StreamId}");
            Console.WriteLine($"  Codec: {e.Codec}");
            Console.WriteLine($"  Sample rate: {e.SampleRate}");
            Console.WriteLine($"  Channel count: {e.ChannelCount}");

            // Optional: choose the decode rate (Opus accepts 8, 12, 16, 24 or 48 kHz)
            // e.SampleRate = 24000;

            // Optional: change streaming mode
            // e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
        };

        client.AudioInputFrameAsync += async e =>
        {
            // e.Samples contains decoded PCM float samples
            float[] samples = e.Samples;

            Console.WriteLine($"Frame: {e.StreamId}");
            Console.WriteLine($"  Samples: {samples.Length}");
            Console.WriteLine($"  IsFirst: {e.IsFirst}");
            Console.WriteLine($"  IsLast: {e.IsLast}");
            Console.WriteLine($"  Total duration: {e.TotalDuration}");  // Zero if unknown

            // Process or play the audio samples...
        };

        client.AudioInputStreamEndAsync += async e =>
        {
            Console.WriteLine($"Audio stream ended: {e.StreamId}");
        };
        #endregion
    }

    public static void BufferedStreamingMode(IkonClient client)
    {
        #region example:sdk-streaming-mode
        client.AudioInputStreamBeginAsync += async e =>
        {
            // Buffer audio for UI timeline display
            e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
        };
        #endregion
    }

    public static async Task AudioShapesAndPlaybackAsync(IkonClient client, string streamId)
    {
        #region example:sdk-audio-shapes
        client.AudioShapeFrameAsync += async e =>
        {
            // Values the app analysed from its speech, such as visemes, for the audio of the same
            // stream; Timestamp is from the start of the segment
            foreach (var set in e.Sets)
            {
                for (int i = 0; i < set.Values.Count; i++)
                {
                    Console.WriteLine($"{e.StreamId} at {e.Timestamp}: {set.Name}.{set.ShapeNames.ElementAtOrDefault(i)} = {set.Values[i]}");
                }
            }
        };

        // A program that plays the audio tells the app how far it got, about every 500 ms and at once
        // when it stops being audible, so the app can pace what it says to what was heard
        await client.SendAudioPlaybackReportAsync(streamId, played: TimeSpan.FromSeconds(1.5), buffered: TimeSpan.FromMilliseconds(120));
        await client.SendAudioPlaybackReportAsync(streamId, played: null, buffered: TimeSpan.Zero, AudioPlaybackState.Blocked);
        #endregion
    }

    private static (byte[] Data, bool IsKey) EncodeNextFrame(bool forceKeyFrame) => (new byte[16], forceKeyFrame);

    public static async Task SendVideoAsync(IkonClient client)
    {
        #region example:sdk-send-video
        // The SDK sends frames you have already encoded (H.264 in Annex B form, VP8 or VP9); it
        // does not encode. The app gets the stream as a VideoInput: "camera" makes its Kind Camera
        var keyFrameRequested = true;
        client.VideoKeyFrameRequestedAsync += async e => keyFrameRequested = true;

        for (int frameNumber = 0; frameNumber < 300; frameNumber++)
        {
            // Start with a keyframe, and send one whenever the app asks for it
            var (data, isKey) = EncodeNextFrame(forceKeyFrame: keyFrameRequested);
            keyFrameRequested = false;

            await client.SendVideoFrameAsync(
                MediaTargets.Everyone,                // A client's video reaches the app
                streamId: "camera-1",
                data: data,
                isKey: isKey,
                timestamp: TimeSpan.FromSeconds(frameNumber / 30.0),
                codec: VideoCodec.H264,
                width: 1280,
                height: 720,
                correlationId: "capture-1");          // Ties the stream to the app's capture
        }

        await client.EndVideoStreamAsync("camera-1");
        #endregion
    }

    public static void ReceiveVideo(IkonClient client)
    {
        #region example:sdk-receive-video
        client.VideoInputStreamBeginAsync += async e =>
        {
            // Raised again when the sender changes the codec or size; reconfigure the decoder then
            Console.WriteLine($"Video stream {e.StreamId} ({e.SenderStreamId}): {e.Codec} {e.Width}x{e.Height}");
        };

        client.VideoInputFrameAsync += async e =>
        {
            // e.Data is one encoded frame; decode it with a decoder of your own
            Console.WriteLine($"Frame {e.FrameNumber} of {e.StreamId}: {e.Data.Length} bytes, key: {e.IsKey}");
        };

        client.VideoInputStreamEndAsync += async e => Console.WriteLine($"Video stream ended: {e.StreamId}");

        // A decoder that lost a frame or joined late asks the sender for a keyframe:
        // await client.RequestVideoKeyFrameAsync(streamId);
        #endregion
    }

    public static async Task UpdateClientContextAsync(IkonClient client)
    {
        #region example:sdk-client-context
        // What a browser reports about itself. A call names only what changed; the SDK
        // sends the other values again, because the app's copy is replaced on each update
        await client.UpdateClientContextAsync(viewportWidth: 390, viewportHeight: 844, theme: "dark", timezone: "Europe/Helsinki", visibility: "visible");
        await client.UpdateClientContextAsync(theme: "light");

        // Move to another page of the app, as a browser does when it navigates
        await client.SetPathAsync("/room/7");
        #endregion
    }

    public static async Task UploadFileAsync(IkonClient client, string uploadActionId)
    {
        #region example:sdk-upload-file
        // The app's upload control (view.FileUpload or view.FileUploadZone) carries the id
        // in the uploadActionId prop of its node in the UI tree
        await using var file = File.OpenRead("report.pdf");

        try
        {
            var result = await client.UploadFileAsync(
                uploadActionId,
                file,                                 // Readable and seekable
                fileName: "report.pdf",
                mimeType: "application/pdf",
                progress: new System.Progress<IkonClient.UploadProgress>(p => Console.WriteLine($"{p.AcknowledgedBytes}/{p.TotalBytes}")));

            // Returns once the app's onUploadComplete has run
            Console.WriteLine($"Uploaded {result.Size} bytes, SHA-256 {result.Hash}");
        }
        catch (UploadFailedException ex)
        {
            // The app refused it, or it failed; Retriable says whether sending it again can help
            Console.WriteLine($"Upload {ex.UploadId} failed: {ex.Message} (retriable: {ex.Retriable})");
        }
        #endregion
    }

    public static async Task DriveUIAsync()
    {
        #region example:sdk-ui
        await using var client = new IkonClient(new IkonClientConfig
        {
            Description = "My UI",
            // The port `ikon run` prints, and the app id, as in Local Development above
            Local = new LocalConfig { Host = "localhost", HttpsPort = int.Parse(Environment.GetEnvironmentVariable("IKON_LOCAL_PORT") ?? "8443"), SpaceId = "your-app-id" },
        });

        // Raised after every change to a stream's tree, with the whole tree as it now is.
        // A node that did not change keeps its object, so a renderer can skip it by reference
        client.UI.StreamUpdatedAsync += async e =>
        {
            foreach (var node in e.Stream.Root?.DescendantsAndSelf() ?? [])
            {
                // node.Type is the component ("std.button"), node.Props its props as JSON,
                // node.StyleIds its styles (client.UI.Styles)
                Console.WriteLine($"{node.Type} {node.GetText("label") ?? node.GetText("content")}");
            }
        };

        await client.ConnectAsync();

        // Use a control as a person would: find its node and call its action
        var save = await client.UI.WaitForNodeAsync(node => node.Type == "std.button" && node.GetText("label") == "Save");
        bool completed = await client.UI.CallActionAsync(save, "onInvokeId");

        // A text field's action takes the new text as it is
        var name = await client.UI.WaitForNodeAsync(node => node.Type == "std.text-field");
        await client.UI.CallActionAsync(name, "onValueChangeId", "Ada");
        #endregion
    }

    public static void RegisterFunctions(IkonClient client)
    {
        #region example:sdk-register-functions
        // Register all [Function] methods from an instance
        var myFuncs = new MyFunctions();
        client.FunctionRegistry.RegisterFromInstance(myFuncs);

        // Or register from a type: only its static methods (and a [RegisterAll] class's constructors) become callable
        client.FunctionRegistry.RegisterFromType<MyStaticFunctions>();

        // Or scan entire assembly (same rule as RegisterFromType)
        client.FunctionRegistry.RegisterFromAssembly(typeof(MyFunctions).Assembly);
        #endregion
    }

    public static void RegisterLambdas(IkonClient client)
    {
        #region example:sdk-register-lambdas
        // Simple synchronous function
        client.FunctionRegistry.AddFunction(
            Function.Register((string name) => $"Hello, {name}!", "Greet")
        );

        // Async function
        client.FunctionRegistry.AddFunction(
            Function.Register(async (int a, int b) =>
            {
                await Task.Delay(10);
                return a + b;
            }, "AddAsync")
        );

        // With attributes (description, visibility, etc.)
        client.FunctionRegistry.AddFunction(
            Function.Register(
                (string query) => SearchDatabase(query),
                "Search",
                new FunctionAttribute { Description = "Searches the database", Visibility = FunctionVisibility.External }
            )
        );
        #endregion
    }

    public static async Task InspectRegistryAsync(IkonClient client)
    {
        #region example:sdk-inspect-registry
        // Check if a function exists
        if (client.FunctionRegistry.HasFunction("MyFunc"))
        {
            var func = client.FunctionRegistry.GetFunction("MyFunc");
            Console.WriteLine($"Found: {func?.Name}, Params: {func?.Parameters.Length}");
        }

        // Get all functions grouped by name (including remote)
        var allFuncs = client.FunctionRegistry.Functions;

        // Find which sessions (the app's and the server's) have a specific function
        var clientIds = client.FunctionRegistry.GetClientSessionsWithFunction("SharedFunc");

        // Wait for a function the app registers to become available (other clients' functions never reach this registry)
        bool available = await client.FunctionRegistry.WaitForFunctionAsync(
            "RemoteFunc",
            timeout: TimeSpan.FromSeconds(30)
        );
        #endregion

        Log.Instance.Debug($"{allFuncs.Count} {clientIds.Count} {available}");
    }

    public static async Task CallFunctionsAsync(IkonClient client)
    {
        #region example:sdk-call-functions
        // Synchronous call
        string result = client.FunctionRegistry.Call<string>("Greet", args: new object?[] { "World" });

        // Async call
        int sum = await client.FunctionRegistry.CallAsync<int>("AddAsync", args: new object?[] { 1, 2 });

        // Void async call
        await client.FunctionRegistry.CallAsync("LogMessage", args: new object?[] { "Hello" });

        // Call a function on one session (targetId: the app's session id; another client's functions
        // never reach this registry, so naming its session fails with "Function 'Calculate' not found")
        int remoteSum = await client.FunctionRegistry.CallAsync<int>("Calculate", targetId: 123, args: new object?[] { 5, 10 });

        // Streaming results (async enumerable). A [Function] with no Name is registered under its type's full name
        await foreach (var item in client.FunctionRegistry.CallAsyncEnumerable<int>($"{typeof(MyFunctions).FullName}.CountAsync", args: new object?[] { 10 }))
        {
            Console.WriteLine(item);
        }
        #endregion

        Log.Instance.Debug($"{result} {sum} {remoteSum}");
    }
}

// Generated holder for the fences of sdk-dotnet-guide.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
file static class SdkExtraExamples
{
    public static async Task SdkxEvents(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-events
        // Connection state changes
        client.StateChangedAsync += async e =>
        {
            Console.WriteLine($"State: {e.State}");
        };

        // Raised once the SDK has told the server this client is ready, and again on every reconnect.
        // The app knows the client from here on, so calls made here reach it with the client's identity.
        client.ReadyAsync += async e =>
        {
            // e.IsReconnect is false only for the first connect; e.IsNewSession says the server
            // session changed and its server-side state is gone
        };

        // The server is stopping: the app restarts, is deployed again or its session ends. Messages can
        // still be sent here. Not raised by the program's own IkonClient.DisconnectAsync or by a dropped
        // connection
        client.StoppingAsync += async e =>
        {
            Console.WriteLine("Server stopping...");
        };

        // The client went Offline. After a server stop, a refused sign-in or a required upgrade it stays
        // there until the program calls IkonClient.ConnectAsync; after failed reconnects BackgroundReconnect
        // (on by default) keeps trying. Not raised by the program's own IkonClient.DisconnectAsync, a failed
        // IkonClient.ConnectAsync or a drop that reconnects
        client.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Disconnected");
        };

        // Error occurred
        client.ErrorOccurredAsync += async e =>
        {
            Console.WriteLine($"Error: {e.Error.Message}");
        };

        // Protocol message received
        client.MessageReceivedAsync += async e =>
        {
            Console.WriteLine($"Message: {e.Message.Opcode}");
        };
        #endregion
    }

    public static async Task SdkxTypedPayloads(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-typed-payloads
        // Send a typed payload (creates ProtocolMessage automatically)
        await client.SendMessageAsync(new MyCustomPayload { /* ... */ });
        #endregion
    }

    public static void ReceiveTypedMessages(IkonClient client)
    {
        #region example:sdk-receive-typed
        // The handler gets only messages of this type, decoded, with the sender's session id
        var subscription = client.OnMessage<MyCustomPayload>(async (message, senderId) =>
        {
            Console.WriteLine($"{senderId}: {message.Text}");
        });

        // Later, to stop receiving them
        subscription.Dispose();
        #endregion
    }

    public static async Task SdkxFunctionVisibility(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-function-visibility
        client.FunctionRegistry.RegisterFromInstance(myFuncs, FunctionVisibility.External);
        #endregion
    }

    public static async Task SdkxRemovingFunctions(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-removing-functions
        // Remove a specific function by name (local functions only)
        client.FunctionRegistry.RemoveFunction("MyFunc");

        // Remove a function with specific visibility
        client.FunctionRegistry.RemoveFunction("MyFunc", FunctionVisibility.External);

        // Clear all local functions
        client.FunctionRegistry.ClearLocalFunctions();
        #endregion
    }

    public static async Task SdkxFunctionEvents(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-function-events
        client.FunctionRegistry.FunctionRegistered += func =>
        {
            Console.WriteLine($"Registered: {func.Name} ({func.Visibility})");
        };

        client.FunctionRegistry.FunctionUnregistered += name =>
        {
            Console.WriteLine($"Unregistered: {name}");
        };
        #endregion
    }

    public static async Task SdkxTimeouts(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-timeouts
        var config = new IkonClientConfig
        {
            // ... authentication ...
            Timeouts = new TimeoutConfig
            {
                InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
                MaxReconnectAttempts = 4,                                 // Max reconnect attempts (default)
                MaxReconnectDelay = TimeSpan.FromMinutes(5),              // Background backoff delay cap (default)
                ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
                BackgroundReconnect = true                                // Keep retrying after max attempts (default)
            }
        };
        #endregion
    }

    public static async Task SdkxProtocolOptions(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-protocol-options
        var config = new IkonClientConfig
        {
            // ... authentication ...

            // Filter which message types to receive/send (default; leaving out GROUP_APP_LOCAL drops the app's own schema messages)
            OpcodeGroupsFromServer = Opcode.GROUP_ALL | Opcode.GROUP_APP_LOCAL,
            OpcodeGroupsToServer = Opcode.GROUP_ALL | Opcode.GROUP_APP_LOCAL,

            // Payload serialization format
            PayloadType = PayloadType.Teleport,  // Default

            // How this connection identifies to the server: Plugin for a program,
            // Native for a client a person uses. Each gets the app's UI.
            ContextType = ContextType.Plugin
        };
        #endregion
    }

    public static async Task SdkxClientIdentification(IkonClient client, MyFunctions myFuncs)
    {
        #region example:sdkx-client-identification
        var config = new IkonClientConfig
        {
            // ... authentication ...
            DeviceId = "unique-device-id",
            ProductId = "my-app",
            VersionId = "3",                  // a whole number
            InstallId = "install-xyz",
            Locale = "en-US",
            Description = "My Application",
            UserAgent = "my-app/1.0.0",       // replaces the default, which names this machine's OS and architecture
            Parameters = new Dictionary<string, string>
            {
                ["custom_param"] = "value"
            }
        };
        #endregion
    }
}

file static class SdkConsoleClientExamples
{
    public static async Task JoinAppAsync(string[] args)
    {
        #region example:sdk-console-client
        // The app id: [Target] SpaceId in the app's ikon-config.<environment>.toml
        const string appId = "your-app-id";

        // For a local `ikon run`, use Local with the port of the Server URL that the run prints (pass it as
        // `--local 8443`); with SpaceId set, a port that serves another app makes the connect throw. For the
        // deployed app, join as a guest, which needs no key; Environment is required.
        var localIndex = Array.IndexOf(args, "--local");
        var local = localIndex >= 0;
        var localPort = local && localIndex + 1 < args.Length ? int.Parse(args[localIndex + 1]) : 8443;
        var config = local
            ? new IkonClientConfig { Local = new LocalConfig { Host = "localhost", HttpsPort = localPort, UserId = "door-kiosk", SpaceId = appId } }
            : new IkonClientConfig
            {
                Guest = new GuestConfig { SpaceId = appId, Environment = IkonBackend.EnvironmentType.Development },
                DeviceId = "door-kiosk",
            };

        await using var client = new IkonClient(config);
        await client.ConnectAsync();

        // A reactive of the app's [App] class, found by the member's name. The callback runs once with
        // the current value, then after changes; changes close together arrive as one call with the latest value
        var reactives = new ReactiveRegistry(client);
        await using var visitors = await reactives.SubscribeByNameAsync<int>("_visitorCount", count => Console.WriteLine($"Visitors today: {count}"));

        // Call an External [Function] of the app, passing the key it checks
        var answer = await client.FunctionRegistry.CallAsync<string>("RecordVisit",
            args: [Environment.GetEnvironmentVariable("KIOSK_KEY"), "Front door"]);
        #endregion

        Log.Instance.Debug($"{answer}");
    }

    public static async Task RunUntilStoppedAsync(IkonClientConfig config)
    {
        #region example:sdk-console-shutdown
        // Ctrl+C (SIGINT) and a service manager's stop (SIGTERM) both end the wait below, and leaving the
        // scope disposes the client, which tells the server the program is leaving: the others get
        // ClientLeftAsync at once instead of after the 5-minute soft-retry window
        using var stop = new CancellationTokenSource();
        using var onTerminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
        {
            context.Cancel = true;
            stop.Cancel();
        });
        using var onInterrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
        {
            context.Cancel = true;
            stop.Cancel();
        });

        await using var client = new IkonClient(config);
        await client.ConnectAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stop.Token);
        }
        catch (OperationCanceledException)
        {
            // The stop signal: fall through to the disposal
        }
        #endregion
    }

    public static async Task StayConnectedAsync(IkonClient client)
    {
        #region example:sdk-console-stay-connected
        // Connecting again joins the app's next server, and the ReactiveRegistry subscribes again by itself
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(15));

            if (!client.State.IsDisconnected())
            {
                continue;
            }

            try
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Not connected: {ex.Message}");
            }
        }
        #endregion
    }
}

file sealed class SdkConsoleAppSide(IAppBase app)
{
    #region example:sdk-console-app-side
    // On the [App] class: a shared reactive the program reads by its name
    private readonly Reactive<int> _visitorCount = new(0);

    // Any program can join as a guest, so a call that changes data sends a key the app checks
    [Function(Name = "RecordVisit", Visibility = FunctionVisibility.External)]
    [AllowAnonymous]
    public string RecordVisit(string key, string door)
    {
        if (!app.Secrets.TryGet("KIOSK_KEY", out var expected)
            || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(expected)))
        {
            return "denied";
        }

        _visitorCount.Value++;
        return $"ok, {door}";
    }
    #endregion
}
