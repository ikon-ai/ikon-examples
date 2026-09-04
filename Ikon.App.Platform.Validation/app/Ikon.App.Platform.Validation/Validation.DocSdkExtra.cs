using Ikon.App.Platform.Validation.Protocol;
using Ikon.Sdk;

// Generated holder for the fences of ikon-sdk-dotnet-readme.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
file static class DocSdkExtra
{
    public static async Task SdkxEvents(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdkx-events
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

        // Protocol message received
        client.MessageReceivedAsync += async e =>
        {
            Console.WriteLine($"Message: {e.Message.Opcode}");
        };
        #endregion
    }

    public static async Task SdkxTypedPayloads(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdkx-typed-payloads
        // Send a typed payload (creates ProtocolMessage automatically)
        await client.SendMessageAsync(new MyCustomPayload { /* ... */ });
        #endregion
    }

    public static async Task SdkxFunctionVisibility(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdkx-function-visibility
        client.FunctionRegistry.RegisterFromInstance(myFuncs, FunctionVisibility.External);
        #endregion
    }

    public static async Task SdkxRemovingFunctions(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdkx-removing-functions
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
        #region docsnippet:sdkx-function-events
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
        #region docsnippet:sdkx-timeouts
        var config = new IkonClientConfig
        {
            // ... authentication ...
            Timeouts = new TimeoutConfig
            {
                InitialReconnectDelay = TimeSpan.FromMilliseconds(500),  // Initial backoff delay
                MaxReconnectAttempts = 4,                                 // Max reconnect attempts (default)
                MaxReconnectDelay = TimeSpan.FromSeconds(30),             // Backoff delay cap (default)
                ReconnectAttemptTimeout = TimeSpan.FromSeconds(30),       // Time budget per attempt (default)
                BackgroundReconnect = true                                // Keep retrying after max attempts (default)
            }
        };
        #endregion
    }

    public static async Task SdkxProtocolOptions(IkonClient client, MyFunctions myFuncs)
    {
        #region docsnippet:sdkx-protocol-options
        var config = new IkonClientConfig
        {
            // ... authentication ...

            // Filter which message types to receive/send
            OpcodeGroupsFromServer = Opcode.GROUP_ALL,
            OpcodeGroupsToServer = Opcode.GROUP_ALL,

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
        #region docsnippet:sdkx-client-identification
        var config = new IkonClientConfig
        {
            // ... authentication ...
            DeviceId = "unique-device-id",
            ProductId = "my-app",
            VersionId = "1.0.0",
            InstallId = "install-xyz",
            Locale = "en-US",
            Description = "My Application",
            UserAgent = "my-app/1.0.0",
            Parameters = new Dictionary<string, string>
            {
                ["custom_param"] = "value"
            }
        };
        #endregion
    }
}
