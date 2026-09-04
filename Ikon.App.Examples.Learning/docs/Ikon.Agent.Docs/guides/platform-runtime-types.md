# Platform Runtime Types

## Platform Runtime Types

The small shared vocabulary an app meets without going looking for it.

### Errors an App Should Catch

`UserException` is the one to throw for an expected failure — invalid input, a missing file, an
operation that could not complete. It renders cleanly to the user, without a stack trace, which is
what separates it from an ordinary exception escaping into the UI.

`BackendQuotaExceededException` derives from it and is thrown when the app hits a platform quota:
its `Key` names the quota, and `Current` and `Limit` are the numbers to show. Catch it where the
user can act on the answer rather than letting a generic error swallow the number.

`AsyncLocalInstanceNotSetException` means an `AsyncLocalInstance<T>` singleton was read where none
is installed — reaching for `Log.Instance` or `Resources.Instance` from a thread the platform did
not set up. It is a wiring bug, not a runtime condition to handle.

### Environment

`ServerRunType` distinguishes `Local` from `Cloud`, which is the honest way to gate a
development-only affordance instead of sniffing an environment variable. `SdkType` names the client
that connected (`DotNet`, `TypeScript`, `Cpp`, `Dart`, `Rust`, or `Unknown`), and
`ProtocolVersion.Version` is the wire version this build speaks.

### Conversions

`Toml.From<T>(text)` and `Toml.To(obj)` round-trip a config class through TOML — the same format
`ikon-config.toml` uses, so an app storing structured settings does not need a second serializer.

`NameConversions` renames one string convention to another: `ToCamelCase`, `ToPascalCase`,
`ToSnakeCase`, `ToKebabCase`, `ToSlug(input, maxLength)`, and `ToDisplayName` for turning an
identifier into something a person reads.

`ExtendedCast.Convert<T>(value)` is the tolerant conversion the AI layer uses on loosely-typed
values, and `FromJsonElement` extends it to the placeholders an LLM emits when a schema marks every
property required — `""` for a collection or object becomes null, `""` for a bool becomes false.
**One trap:** a null value converted to a NON-nullable value type yields that type's default — `0`
for `Int32`, `false` for `Boolean` — not null, so a missing LLM field is indistinguishable from a
real zero. Make the target nullable (`int?`) whenever "absent" and "zero" mean different things.

### Streams and Channels

An app announcing media on the wire sends a `UIStreamBegin` (a `Category`) or a `VideoStreamBegin`
(stream id, description, source type, codec and codec details, width, height, framerate, and an
optional correlation id). `IMessageChannel` is the interface behind anything that carries protocol
messages — a `SessionId`, `SendMessageAsync`, and `RegisterMessageHandler` with an optional opcode
group mask or explicit opcode list; `Opcodes.IsOpcodeInAnyGroup` tests one against a group mask.
`IAppBase` implements it, which is why an app can send and receive without a separate channel
object.

### The Connect Plane

An app does not open its own transport, but the types that describe one show up in SDK work, in a
`/connect` response, and in logs.

An `Entrypoint` is one way in: an `EntrypointType` (`WebSocket`, `WebSocketProxy`, `WebTransport`,
`WebTransportProxy`, `Tcp`, `TcpProxy`, `TcpTls`, `Https`, `WebRTC`, or `None`), the `Uri`, the
opcode groups it carries in each direction, a `Priority` the client sorts on, a `Description`, an
`AuthTicket`, and whether it is unreliable. A `RouteToken` addresses the instance a connection
should land on. `ServerStatus` is the lifecycle a server reports — `Unknown`, `Starting`, `Running`,
`Stopping`, `Stopped`.

`ActionFunctionRegister` is how a function advertises itself over the protocol: its `FunctionId` and
`FunctionName`, its `Parameters` as `FunctionRegisterParameter` records, the result type, whether it
is enumerable or cancellable, and the LLM hints (`LlmInlineResult`, `LlmCallOnlyOnce`). This is the
wire form of what `[Function(Visibility = FunctionVisibility.External)]` produces — you read it, you
do not build it.

Two constant sets name the strings either side of the wire agrees on: `UIStreamCategories` (`App`,
`Chat`, `Header`, `Footer`, `Collapsed`, `DebugOverlay`, …) is what goes in a `UIStreamBegin`'s
`Category`, and `UIStylesKeys` (`Common`, `Crosswind`, `Css`, `Flutter`, `ReactNative`) names the
style dialects a client can be sent.

Three more belong to the hosting layer rather than to an app, and are public so the servers, the
proxy and the SDKs can reach them across assemblies. `IPlugin` is the connection seam an SDK
implements — `ConnectAsync2` fetches the `AuthResponse` (entrypoints, auth ticket, client session)
through the `/connect` GET and opens the transport, and `ReconnectWithAuthResponseAsync` reopens it
against a cached one so the server resumes the same session inside its disconnect grace. `PortLease`
claims ports for servers starting in one process: `Take(startPort)` scans under a process-wide gate
for a port free on both TCP and UDP, `TakeSpecific` registers one something else chose, and
`Dispose` releases them — call it once the owning server's sockets are closed, never at the end of
configuration, which reopens the race. `ServiceTokenExchanger` turns the machine credential in
`IKON_SERVICE_TOKEN` into a short-lived access token cached per environment, because a token is only
valid for the environment that minted it.

### Boot Snapshots

`SnapshotCapture` holds the constants of the pre-render pass that produces a page's boot snapshot:
`IsCaptureProcess` tells app code it is running under one, `MaxRoutes` (50) and `MaxVariants` (16)
bound how much is captured, `RouteTimeoutMs` and the whole-run `CaptureBudgetMs` bound how long, and
`SettleMs` (750) is the quiet window after which a route counts as settled — though the app's own
ready signal always wins the race when it arrives first.
