using Ikon.App.Patterns.Protocol;
using Ikon.Common.Core.Protocol;
using Ikon.Sdk;

namespace Ikon.App.Patterns.Examples;

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
    // Local - not advertised (default); only an Ikon AI App keeps remote callers out
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
                ExternalUserId = "user-123"
            },
            Description = "My App"
        };

        // Create and connect the client
        await using var client = new IkonClient(config);

        client.ReadyAsync += async e =>
        {
            Console.WriteLine("Connected!");
            await client.SignalReadyAsync();
        };

        client.MessageReceivedAsync += async e =>
        {
            Console.WriteLine($"Received: {e.Message.Opcode}");
        };

        await client.ConnectAsync();
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

    public static void ApiKeyConfig()
    {
        #region example:sdk-api-key-config
        var config = new IkonClientConfig
        {
            ApiKey = new ApiKeyConfig
            {
                ApiKey = "ikon-xxxxx",           // API key from portal
                SpaceId = "...",                  // Space ID
                ExternalUserId = "user-123",      // Your user identifier
                SessionIdentityHash = "...",      // Optional: attach to a specific live session (connect fails if none owns this hash)
                BackendType = BackendType.Production,
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
                HttpsPort = 8443,
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
                UserType = UserType.Human,
                ClientType = ClientType.DesktopApp
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
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
                MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
                ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per reconnect tier (default)
                BackgroundReconnect = true                                // Keep retrying after max attempts (default)
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

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

        // Optional: specify stream ID, total duration, encoder options, and target clients.
        // A stream's sample rate and channel count are fixed by its first send, and every call
        // without a streamId shares one stream, so audio in another format needs its own streamId
        await client.SendAudioAsync(
            MediaTargets.To(123, 456),                // Target specific session IDs
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

        // Find which client sessions have a specific function
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

        // Call a function on a specific remote client (uses targetId parameter)
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

        // Connection established and ready, raised again on every reconnect
        client.ReadyAsync += async e =>
        {
            // Perform initialization here. e.IsReconnect is false only for the first connect;
            // e.IsNewSession says the server session changed and its server-side state is gone
            await client.SignalReadyAsync();  // Signal that this client is ready (mandatory)
        };

        // Server is stopping (can still send messages)
        client.StoppingAsync += async e =>
        {
            Console.WriteLine("Server stopping...");
        };

        // Disconnected: the client gave up and went Offline — every reconnect attempt failed, or
        // the server is stopping. With BackgroundReconnect on (the default) a failed reconnect keeps
        // retrying from Offline, so this is final only when the server is stopping or
        // BackgroundReconnect is off. Not raised by a failed IkonClient.ConnectAsync (that is the
        // call's exception), by IkonClient.DisconnectAsync or IkonClient.DisposeAsync, nor by a
        // drop that reconnects
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
                MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
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

            // How this connection identifies to the server.
            // Default Plugin connects as a backend component (no UI).
            // Native or Browser connects as a first-class player client that receives streamed UI.
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
