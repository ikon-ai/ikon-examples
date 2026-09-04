using Ikon.Common.Core.Protocol;
using Ikon.Common.Core.Signing;

namespace Ikon.App.Patterns;

// Stand-in IAppBase for the render smoke-test: the app-ctor patterns store the handle but only reach
// into it on interaction, never during a default-state render, so every member here throws. If a
// pattern ever dereferences the app from its render path, the smoke-test surfaces it as a throw.
public sealed class PatternStandInApp : IAppBase
{
    public int SessionId => throw new NotImplementedException();
    public ValueTask SendMessageAsync(ProtocolMessage message) => throw new NotImplementedException();
    public ValueTask SendMessageAsync(IProtocolMessagePayload payload) => throw new NotImplementedException();
    public IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null) => throw new NotImplementedException();
    public GlobalState GlobalState => throw new NotImplementedException();
    // Backed rather than thrown: a pattern that builds a `UI` (or other app-bound object) in a field
    // initializer touches these at construction time, before any render.
    public ReactiveGlobalState ReactiveGlobalState { get; } = new();
    private readonly Reactive<IReadOnlyList<string>> _mounts = new(["ikon-ui"]);
    public Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default) => throw new NotImplementedException();
    public IReadOnlyList<EndpointInfo> Endpoints => throw new NotImplementedException();
    public IReadOnlyList<DatabaseConnectionInfo> Databases => throw new NotImplementedException();
    public string DataDirectory => throw new NotImplementedException();
    public ReactiveRoot ReactiveRoot => throw new NotImplementedException();
    public BackgroundWork BackgroundWork => throw new NotImplementedException();
    public int MaxMemoryLimitMb => throw new NotImplementedException();
    public int MaxClients { get; set; }
    public bool DynamicMaxClientsEnabled { get; set; }
    public bool WebRtcEnabled { get; set; }
    public bool UdpEnabled { get; set; }
    public Navigation Navigation => throw new NotImplementedException();
    public Reactive<IReadOnlyList<string>> Mounts => _mounts;
    public Secrets Secrets => throw new NotImplementedException();
    public EmailService Email => throw new NotImplementedException();
    public TelephonyService Telephony => throw new NotImplementedException();
    public CostsService Costs => throw new NotImplementedException();
    public NotificationService Notifications => throw new NotImplementedException();
    public Ikon.App.Payments.PaymentsService Payments => throw new NotImplementedException();
    public event AsyncEventHandler<StartingEventArgs> StartingAsync { add { } remove { } }
    public event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync { add { } remove { } }
    public event AsyncEventHandler<StoppingEventArgs> StoppingAsync { add { } remove { } }
    public event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync { add { } remove { } }
    public event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync { add { } remove { } }
    public event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync { add { } remove { } }
    public Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    public Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<SignatureResult> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default) => throw new NotImplementedException();
}
