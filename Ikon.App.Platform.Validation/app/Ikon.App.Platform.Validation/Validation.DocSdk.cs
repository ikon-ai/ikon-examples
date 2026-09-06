using Ikon.Sdk;

// The .NET SDK readme, as code that compiles.
//
// Unlike the app-facing guides this one is about a STANDALONE client — a console or desktop app that
// connects to a space — so its `Console.WriteLine` calls are correct as written and stay.

#region docsnippet:sdk-functions-class
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
    #region docsnippet:sdk-function-visibility
    // Local - only available in this process (default)
    [Function(Visibility = FunctionVisibility.Local)]
    public string LocalOnly() => "local";

    // External - advertised over the protocol and callable by other clients
    [Function(Visibility = FunctionVisibility.External)]
    public string SharedWithAll() => "shared";
    #endregion
}

file static class DocSdkReadme
{
    private static ReadOnlyMemory<float> GetAudioSamples() => new float[480];

    private static string SearchDatabase(string query) => query;

    private sealed class MyStaticFunctions;

    public static async Task QuickstartAsync()
    {
        #region docsnippet:sdk-quickstart
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
        #region docsnippet:sdk-disable-udp
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
        #region docsnippet:sdk-api-key-config
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
        #region docsnippet:sdk-local-config
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
        #region docsnippet:sdk-backend-config
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
        #region docsnippet:sdk-external-connect-url
        var config = new IkonClientConfig
        {
            ExternalConnectUrl = connectUrl
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static void UserLoginConfig()
    {
        #region docsnippet:sdk-user-login-config
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

    public static void Events(IkonClient client)
    {
        #region docsnippet:sdk-events
        // Connection state changes
        client.StateChangedAsync += async e =>
        {
            Console.WriteLine($"State: {e.State}");
        };

        // Connection established and ready
        client.ReadyAsync += async e =>
        {
            // Perform initialization here
            await client.SignalReadyAsync();  // Signal that this client is ready (mandatory)
        };

        // Server is stopping (can still send messages)
        client.StoppingAsync += async e =>
        {
            Console.WriteLine("Server stopping...");
        };

        // Disconnected from server
        client.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Disconnected");
        };

        // Error occurred
        client.ErrorOccurredAsync += async e =>
        {
            Console.WriteLine($"Error: {e.Error.Message}");
        };
        #endregion
    }

    public static async Task LifecycleAsync(IkonClient client)
    {
        #region docsnippet:sdk-lifecycle
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
        #region docsnippet:sdk-timeouts
        var config = new IkonClientConfig
        {
            // ... authentication config ...
            Timeouts = new TimeoutConfig
            {
                InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
                MaxReconnectAttempts = 4,                                 // Max attempts (default)
                MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
                ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per attempt (default)
                BackgroundReconnect = true                                // Keep retrying after max attempts (default)
            }
        };
        #endregion

        Log.Instance.Debug($"{config}");
    }

    public static async Task SendRawAsync(IkonClient client, IProtocolMessagePayload payload)
    {
        #region docsnippet:sdk-send-raw
        // Send a raw protocol message (on a connected client)
        var message = ProtocolMessage.Create(client.ClientContext!.SessionId, payload);
        await client.SendMessageAsync(message);
        #endregion
    }

    public static async Task SendAudioAsync(IkonClient client)
    {
        #region docsnippet:sdk-send-audio
        // Get audio samples (float PCM, range [-1.0, 1.0])
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

        // Optional: specify stream ID, total duration, encoder options, and target clients
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

        // Set default encoder options for all audio
        client.DefaultEncoderOptions = new AudioEncoderOptions(bitrate: 48000, complexity: 8);
        #endregion
    }

    public static void ReceiveAudio(IkonClient client)
    {
        #region docsnippet:sdk-receive-audio
        client.AudioInputStreamBeginAsync += async e =>
        {
            Console.WriteLine($"Audio stream started: {e.StreamId}");
            Console.WriteLine($"  Codec: {e.Codec}");
            Console.WriteLine($"  Sample rate: {e.SampleRate}");
            Console.WriteLine($"  Channel count: {e.ChannelCount}");

            // Optional: override sample rate (SDK will resample)
            // e.SampleRate = 44100;

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
        #region docsnippet:sdk-streaming-mode
        client.AudioInputStreamBeginAsync += async e =>
        {
            // Buffer audio for UI timeline display
            e.StreamingMode = AudioInputStreamingMode.DelayUntilTotalDurationKnown;
        };
        #endregion
    }

    public static void RegisterFunctions(IkonClient client)
    {
        #region docsnippet:sdk-register-functions
        // Register all [Function] methods from an instance
        var myFuncs = new MyFunctions();
        client.FunctionRegistry.RegisterFromInstance(myFuncs);

        // Or register from a type (static methods only)
        client.FunctionRegistry.RegisterFromType<MyStaticFunctions>();

        // Or scan entire assembly
        client.FunctionRegistry.RegisterFromAssembly(typeof(MyFunctions).Assembly);
        #endregion
    }

    public static void RegisterLambdas(IkonClient client)
    {
        #region docsnippet:sdk-register-lambdas
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

    public static void OverrideVisibility(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdk-override-visibility
        // Override visibility at registration time
        client.FunctionRegistry.RegisterFromInstance(myFuncs, FunctionVisibility.External);
        #endregion
    }

    public static async Task InspectRegistryAsync(IkonClient client)
    {
        #region docsnippet:sdk-inspect-registry
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

        // Wait for a function to become available (useful for coordination between clients)
        bool available = await client.FunctionRegistry.WaitForFunctionAsync(
            "RemoteFunc",
            timeout: TimeSpan.FromSeconds(30)
        );
        #endregion

        Log.Instance.Debug($"{allFuncs.Count} {clientIds.Count} {available}");
    }

    public static async Task CallFunctionsAsync(IkonClient client)
    {
        #region docsnippet:sdk-call-functions
        // Synchronous call
        string result = client.FunctionRegistry.Call<string>("Greet", args: new object?[] { "World" });

        // Async call
        int sum = await client.FunctionRegistry.CallAsync<int>("AddAsync", args: new object?[] { 1, 2 });

        // Void async call
        await client.FunctionRegistry.CallAsync("LogMessage", args: new object?[] { "Hello" });

        // Call a function on a specific remote client (uses targetId parameter)
        int remoteSum = await client.FunctionRegistry.CallAsync<int>("Calculate", targetId: 123, args: new object?[] { 5, 10 });

        // Streaming results (async enumerable)
        await foreach (var item in client.FunctionRegistry.CallAsyncEnumerable<int>("CountAsync", args: new object?[] { 10 }))
        {
            Console.WriteLine(item);
        }
        #endregion

        Log.Instance.Debug($"{result} {sum} {remoteSum}");
    }
}
