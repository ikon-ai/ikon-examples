# Multi-User Game — Host + Players + Per-Client Answers

A real-time multi-user trivia/quiz game (Kahoot-style). One client is the **host** (sees question + reveals); other clients are **players** (see same question, tap an answer, see leaderboard). Mixes shared state (current question, leaderboard) with per-client state (player name, selected answer, has-joined flag).

## When to use

Live trivia, multiplayer quiz, kahoot-style games, party quizzes, classroom quizzes, multi-user voting, crowd-sourced polls. Any app where one client orchestrates and others participate with their own choices visible only to themselves. These are **turn-based / low-frequency** — state changes a few times per question, so native `view.*` reactive UI is the right tool and this pattern applies.

## When NOT to use — real-time games need a custom .tp transport + local rendering

This pattern (native `view.*` + `Reactive`) is for turn-based/low-frequency games. **For a real-time game where state changes many times per second — a snake game, agar-style arena, moving players, live cursors, anything with a per-frame game loop — do NOT drive the playfield through Parallax UI / `Reactive` state.** Streaming high-frequency state through the reactive UI diff is too slow and floods the channel; it will feel laggy. Instead, for the live playfield use the real-time multi-user transport: a custom **Teleport `.tp` message** (`unreliable = true` for drop-tolerant position/tick streams) sent via the SDK `appMessaging` helper, with a **custom canvas component that renders locally** (each client draws its own frames from the streamed state). The app server is the router (`app.OnMessage<T>` → `app.SendMessageAsync(payload, targets)`). Keep only the non-realtime chrome (lobby, scoreboard, controls) in native `view.*`. Reference app: **Ikon.App.Arena**. `guide("real-time multi-user custom .tp app — schema/*.tp + appMessaging + custom canvas + server router")` before writing it.

**The exact trap to avoid (it compiles and even runs, so nothing flags it): mounting a custom canvas but feeding it per-tick state through `view.AddNode` props sourced from a server `Reactive`.** A "custom `.tp` component" is NOT the same as a custom *component* — `.tp` is the Teleport MESSAGE PROTOCOL (a `schema/<Name>.tp` type carried by `appMessaging` / `app.SendMessageAsync`), not the component file. Concretely:
- WRONG (the slow path): server holds `Reactive<GameState> _game`; the tick loop mutates it; `view.AddNode("custom.board", new(){ ["stateJson"] = JsonSerialize(_game.Value) })` pushes the whole state as a prop every tick. This routes per-frame game state through the Parallax UI-diff pipeline — exactly what kills performance. If your plan/code has `AddNode` props that change every tick, or a `Reactive` holding the live game state, you are on the slow path.
- RIGHT: there is a `schema/<Game>State.tp` message type. The server tick loop calls `await app.SendMessageAsync(state, app.Clients.Ids.ToList())` each tick; the canvas component subscribes with `appMessaging(client).on(StateType, s => render(s))` and renders on its own `<canvas>`; input goes back the same way (`appMessaging(client).send(InputType, dir)` → server `app.OnMessage<Input>`). `AddNode` mounts the canvas ONCE for layout/config — it is NOT the per-tick data channel.

**EXACT `schema/*.tp` file format — do NOT write an `opcode`; the compiler auto-assigns it.** Each `.tp` is a TOML-like file at the app root (`<appRoot>/schema/<Name>.tp`), ONE message type per file. **Omit the `opcode` field entirely** — the app-local compiler assigns each message a unique opcode in the `GROUP_APP_LOCAL` range automatically (so it can never collide with a system opcode, and C#/TS always agree). Do NOT hand-write an `opcode` and NEVER write a string name like `opcode = "GAME_SNAPSHOT"` (older builds defaulted that to `0x00000000`, outside the app-local group, so the message never routed and the client's `appMessaging.on(...)` never fired — "Connecting…" forever, with nothing in the build catching it). The fields use a `type`, `version`, optional `unreliable`, optional `[namespaces]`, `[fields]`, and `[nested.X]` tables:
```toml
# schema/GameSnapshot.tp — server → all clients, high-frequency tick state
type       = "GameSnapshot"
version    = 1
unreliable = true              # drop-tolerant: each tick carries the full state, so a lost datagram self-heals
# NO opcode line — the compiler auto-assigns one in the GROUP_APP_LOCAL range.

[namespaces]
csharp     = "MyApp.Protocol"
typescript = ""

[fields]
Tick   = "int32"
Snakes = "SnakeData[]"
Food   = "FoodCell[]"

[nested.SnakeData]
ClientId = "int32"
Body     = "CellPos[]"
Alive    = "bool"

[nested.FoodCell]
X = "int32"
Y = "int32"

[nested.CellPos]
X = "int32"
Y = "int32"
```
```toml
# schema/SnakeInput.tp — client → server, player steering (also no opcode)
type    = "SnakeInput"
version = 1

[fields]
Direction = "int32"            # 0=Up 1=Right 2=Down 3=Left
```
The generated codec still exports `<NAME>_OPCODE` (the auto-assigned value) for the `AppMessageType` descriptor below.

**EXACT server API shapes — copy these verbatim (codegen reliably hallucinates the arg counts here):**
```csharp
// schema/<Name>.tp lives at the APP ROOT (<appRoot>/schema/GameState.tp); the build generates
// app/<App>/Generated/Protocol/GameState.cs and you reference the type directly — `GameState`, no using.

// RECEIVE — OnMessage<T> takes ONE argument: the handler (payload, senderId). NO message-name string.
app.OnMessage<InputCommand>(async (cmd, senderId) => { ApplyInput(senderId, cmd); });

// SEND — SendMessageAsync takes TWO arguments: the message + the targets (a List<int> OR a single
// int session id). The type is INFERRED from the message — there is NO name string and NO 4th arg.
await app.SendMessageAsync(snapshot, app.Clients.Ids.ToList());   // broadcast to everyone
await app.SendMessageAsync(snapshot, senderId);                   // just one client

// TICK LOOP — `app.BackgroundWork` is a PROPERTY, not a method: never write app.BackgroundWork(...).
// Run the loop as a task and broadcast each tick:
_ = Task.Run(async () =>
{
    while (running)
    {
        Tick();
        await app.SendMessageAsync(BuildSnapshot(), app.Clients.Ids.ToList());
        await Task.Delay(100);
    }
});
```
Common build errors when these shapes are wrong: `CS1501 No overload for 'SendMessageAsync' takes 4 arguments` (you passed a name string / extra arg — pass only message + targets), `CS1501 No overload for 'OnMessage' takes 2 arguments` (you passed a name string — pass only the handler), `CS1955 BackgroundWork cannot be used like a method` (it's a property — use Task.Run), `CS0246 'GameState' not found` (the schema/*.tp isn't at the app root, or you referenced it before the first build generated its .cs). Reference app: **Ikon.App.Arena**.

**FRONTEND REGISTRATION — the 4 parts that MUST all line up, or `AddNode` renders BLANK (nothing in the build flags it; this is the #1 reason a realtime app "builds" but shows nothing).** The C# `view.AddNode("<type>", props)` node type is a PLAIN name (e.g. `"viperboard"`, NOT a dotted `"app.board"`), and the SAME string must be matched by a resolver that is registered into the UI registry via a module added to `app.tsx`. Mirror Ikon.App.Arena exactly:
```tsx
// 1) frontend-node/src/<name>/<Name>Board.tsx — the component + its resolver.
import { useEffect, useRef } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { appMessaging, type AppMessageType } from '@ikonai/sdk';
// Import the GENERATED codec for EACH .tp message: the X_OPCODE constant + toProtocolMessageX +
// fromProtocolMessageXAsync (kebab-case file, one per message type). Also import the type.
import { GAME_SNAPSHOT_OPCODE, toProtocolMessageGameSnapshot, fromProtocolMessageGameSnapshotAsync, type GameSnapshot } from '../generated/protocol/game-snapshot';
import { SNAKE_INPUT_OPCODE, toProtocolMessageSnakeInput, fromProtocolMessageSnakeInputAsync, type SnakeInput } from '../generated/protocol/snake-input';

// CRITICAL #1 — appMessaging.on/send take an AppMessageType<T> DESCRIPTOR built from the
// generated codec, NOT a string opcode name ('GAME_SNAPSHOT') and NOT the bare interface
// (GameSnapshot). Passing a string makes `.on()` silently NEVER fire — the canvas mounts and
// renders but no state ever arrives ("Connecting..." forever); nothing in the build catches it.
// Build ONE descriptor per message type:
const SnapshotMsg: AppMessageType<GameSnapshot> = {
  opcode: GAME_SNAPSHOT_OPCODE,
  toProtocolMessage: toProtocolMessageGameSnapshot,
  fromProtocolMessage: fromProtocolMessageGameSnapshotAsync,
};
const InputMsg: AppMessageType<SnakeInput> = {
  opcode: SNAKE_INPUT_OPCODE,
  toProtocolMessage: toProtocolMessageSnakeInput,
  fromProtocolMessage: fromProtocolMessageSnakeInputAsync,
};

// CRITICAL #2 — how the renderer gets its client and node. The props are
// { nodeId, context, className } (NOT { node }). Get the live client from
// `context.client`, and the node (for mount-time props) from
// `useUiNode(context.store, nodeId)`. Do NOT call `useIkonApp()` inside a custom
// node component — that is the APP-SHELL hook; it needs authConfig options and
// throws at runtime ("Cannot destructure property 'authConfig' of 'options'")
// when called here. The build can't catch this (it compiles); only the running
// app crashes. Mirror Ikon.App.Arena's arena-canvas.tsx exactly.
function ViperBoardRenderer({ nodeId, context }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client;
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const stateRef = useRef<GameSnapshot | null>(null);
  const gridW = (node?.props?.['gridW'] as number) ?? 30;

  useEffect(() => {
    const messaging = appMessaging(client);
    const sub = messaging.on(SnapshotMsg, (snap) => { stateRef.current = snap; });   // descriptor, not a string
    const onKey = (e: KeyboardEvent) => messaging.send(InputMsg, { Direction: e.key });
    window.addEventListener('keydown', onKey);
    let raf = 0;
    const draw = () => { /* render stateRef.current onto canvasRef from snapshot */ raf = requestAnimationFrame(draw); };
    raf = requestAnimationFrame(draw);
    return () => { sub.close(); window.removeEventListener('keydown', onKey); cancelAnimationFrame(raf); };  // sub.close(), not unsubscribe
  }, [client]);

  return <canvas ref={canvasRef} />;
}

export function createViperBoardResolver(): IkonUiComponentResolver {
  return (node) => (node.type !== 'viperboard' ? undefined : ViperBoardRenderer);  // SAME string as AddNode
}

// 2) frontend-node/src/<name>/<name>-module.ts — wraps the resolver as a registry module.
import { type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createViperBoardResolver } from './ViperBoard';
export const loadViperBoardModule: IkonUiModuleLoader = () => [createViperBoardResolver()];
export function registerViperBoardModule(registry: IkonUiRegistry): void {
  registry.registerModule('viperboard', loadViperBoardModule);
}

// 3) frontend-node/src/app.tsx — import the module fn and ADD it to the modules array (next to the
//    standard ones already there). WITHOUT this the resolver is never loaded → blank canvas.
import { registerViperBoardModule } from './<name>/<name>-module';
// ...in the IkonApp/registry config:  modules: [registerStandardUiModule, registerLucideIconsModule, registerViperBoardModule],
```
And (4) the C# side: `view.AddNode("viperboard", new Dictionary<string, object?> { /* mount-time config only, NOT per-tick state */ })`. The four `viperboard` strings (AddNode, resolver match, registerModule key, the component folder) must be identical. Imports come from `@ikonai/sdk-react-ui` (NOT `@ikonai/sdk-react`); `appMessaging` from `@ikonai/sdk`.

## Snippet

```csharp
// `Context` (the OnClientJoined/OnClientLeft payload) lives here — the scaffold does NOT import it.
using Ikon.Common.Core.Protocol;

return await App.Run(args);

// Host detection lives in ClientParams. The host client connects with `?host=true` query param.
public record SessionIdentity(string Id);
public record ClientParams(string Id = "", bool Host = false);

[App]
public partial class LiveQuizApp(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    // ── Shared state (all clients see the same values) ──────────────────────
    private readonly Reactive<GameStage> _stage = new(GameStage.Lobby);
    private readonly Reactive<int> _questionIndex = new(0);
    private readonly ReactiveList<Question> _questions = new();
    private readonly ReactiveList<Player> _players = new();

    // ── Per-client state (each player has their own copy) ───────────────────
    private readonly ClientReactive<string> _playerName = new("");
    private readonly ClientReactive<bool> _hasJoined = new(false);
    private readonly ClientReactive<int?> _selectedAnswer = new((int?)null);

    // ── Server-side bookkeeping (not reactive — used in handlers only) ──────
    private readonly ConcurrentDictionary<int, int> _playerAnswers = new();  // ClientSessionId → choice

    public async Task Main()
    {
        app.OnClientJoined(OnClientJoinedAsync);   // friendly extension — Func<Context, Task>
        app.OnClientLeft(OnClientLeftAsync);       // never raw `app.ClientJoinedAsync += ...`

        UI.Root([Page.Default], content: RenderUI);
    }

    // One screen per stage. Everyone renders from the same `_stage` reactive, so the host advancing
    // it moves every client at once.
    private void RenderUI(UIView view) =>
        view.Text([Text.H1], text: $"Stage: {_stage.Value}");

    private async Task OnClientJoinedAsync(Context ctx)
    {
        // ReactiveScope inside event handlers needs an explicit ClientScope —
        // ctx.ClientSessionId is the int identity for this client.
        using var _ = ReactiveScope.Use(new ClientScope(ctx.ClientSessionId));
        // Now ClientReactive<T>.Value reads/writes for THIS specific client.
    }

    private async Task OnClientLeftAsync(Context ctx)
    {
        _players.RemoveAll(p => p.ClientId == ctx.ClientSessionId);   // one notification
        _playerAnswers.TryRemove(ctx.ClientSessionId, out _);
    }

    // Host detection — read parameters of the CURRENT client through the indexer.
    // ReactiveScope.ClientId is int (not string).
    private bool IsHost()
    {
        var client = app.Clients[ReactiveScope.ClientId];
        return client?.Parameters.Host == true;
    }

    private Player? CurrentPlayer() =>
        _players.FirstOrDefault(p => p.ClientId == ReactiveScope.ClientId);

    private async Task JoinAsync(string name)
    {
        var clientId = ReactiveScope.ClientId;

        if (!_players.Any(p => p.ClientId == clientId))
        {
            _players.Add(new Player(clientId, name, Score: 0));   // mutator notifies; no list rebuild
        }
        _hasJoined.Value = true;        // per-client reactive — only THIS client's UI flips
        _playerName.Value = name;
    }

    private async Task SelectAnswerAsync(int choice)
    {
        if (_selectedAnswer.Value != null) return;     // per-client guard — already answered
        _selectedAnswer.Value = choice;
        _playerAnswers[ReactiveScope.ClientId] = choice;
    }

    private async Task HostStartNextQuestionAsync()
    {
        // Reset per-client state for everyone by walking the player list.
        // (Don't iterate `app.Clients` — there is no .All / .Current; use the
        // shared `_players` list instead and write to each client by id.)
        foreach (var player in _players)
        {
            _selectedAnswer.SetFor(player.ClientId, null);
        }
        _playerAnswers.Clear();
        _questionIndex.Value++;
        _stage.Value = GameStage.Question;
    }
}

public enum GameStage { Lobby, Question, Reveal, Leaderboard, GameOver }
public record Player(int ClientId, string Name, int Score);
public record Question(string Prompt, string[] Choices, int CorrectIndex);
```

## Notes

- **Host detection via `ClientParams.Host`** — the `Host` flag on `ClientParams` is set by query param (`?host=true`). All other clients default to `Host = false`. Don't try to make the first joiner the host; URL-driven role is robust and lets the host reload without losing the role.
- **`app.ClientJoinedAsync` and `ClientLeftAsync` are 1-arg async handlers** — `async args => { ... }`, never `async () => { ... }`. Wrong arity produces CS1593.
- **`ReactiveScope.ClientId` is `int`, not `string`.** `app.Clients[id]?.Parameters` is the read path. There is **NO** `app.ClientSessionId`, **NO** `app.ClientParameters`, **NO** `app.Clients.All`, **NO** `IClientCollection.Current`. In an event handler, a single write to the joining client is `_x.SetFor(args.ClientSessionId, value)`; wrap with `using var _ = ReactiveScope.Use(new ClientScope(args.ClientSessionId))` when the whole handler body belongs to that client.
- **Mix shared + per-client state explicitly.** The current question, players list, and game stage are shared — `Reactive<T>` for scalars, `ReactiveList<T>` for the lists (everyone sees the same value). Each player's name, has-joined state, and selected answer are `ClientReactive<T>` (each client sees their own). Trying to make a single `Reactive<Dictionary<int, T>>` work for per-client state defeats the purpose — `ClientReactive<T>` auto-scopes for free.
- **Shared lists are `ReactiveList<T>`, never `Reactive<List<T>>`.** Mutate the reactive itself — `_players.Add(player)`, `_players.RemoveAll(p => p.ClientId == id)` — one change notification each, no rebuild-and-reassign. Read it directly too: `foreach (var p in _players)`, `_players.FirstOrDefault(…)`, `_players.Count`. `_players.Value` is an `IReadOnlyList<T>` snapshot, so `.Value.Add(p)` does not compile.
- **Server-side bookkeeping uses `ConcurrentDictionary<int, …>`** keyed by `ClientSessionId` — for state that doesn't need to be reactive (vote tallies, timestamps, internal counters). Only push to `Reactive`/`ClientReactive` when the UI needs to refresh.
- **Don't iterate `app.Clients` to reset per-client state.** `IClientCollection<T>` exposes only `Count` and the indexer. To touch every client's `ClientReactive` value, walk your shared `_players` list and write to each id: `_selectedAnswer.SetFor(p.ClientId, null)`.
- **`Reactive<T>` constructor must take an explicit initial value** — `new Reactive<GameStage>(GameStage.Lobby)`, `new Reactive<int>(0)`, `new ClientReactive<int?>((int?)null)`. Bare `new Reactive<T>()` produces CS0121 ambiguous-call. The `ReactiveList<T>` family is the exception: it starts empty, so `private readonly ReactiveList<Player> _players = new();` takes no argument.
- **Game flow as a state machine via `Reactive<GameStage>`**: Lobby → Question → Reveal → Leaderboard → (next round or GameOver). UI branches on `_stage.Value`; transitions happen in host-only handlers.

## See also

- `chatbot-streaming` — the single-LLM-conversation single-client variant.
- `kanban-multi-column` — shared state with mutation buttons (no host role).
- `app-structure` (top-level guide) — `[App]`, partial class, `IApp<TSessionIdentity, TClientParameters>`, ClientParameters via query-param.
- `reactive-state` (top-level guide) — `Reactive<T>` vs `ClientReactive<T>` mechanics, `SetFor` / `ReactiveScope.Use`.
