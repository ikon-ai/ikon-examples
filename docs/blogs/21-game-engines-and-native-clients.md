# The AI Multiplayer Server

A multiplayer game needs a server. It handles persistent connections, player sessions, shared world state, real-time synchronization, and reconnection. Now add AI-powered NPCs. That means a second server — or at least a separate backend — to run language models, manage prompts, and feed responses back into the game. Add a web-based companion app or spectator view and you need a third layer. Three systems. Three deployments. Three sets of state to keep in sync.

Or: one IkonServer that does all of it.

## What this unlocks

**Your game server is the AI server.** IkonServer already handles everything a multiplayer game server does — persistent connections, shared reactive state, player sessions, automatic reconnection, real-time sync across clients. AI capabilities are built into the same process. NPC dialogue, procedural content, adaptive difficulty — these are server-side function calls, not separate services to integrate.

**Unity and Unreal connect as thin clients.** The C# SDK targets .NET Standard 2.1, which means it works directly in Unity projects. The C++ SDK is a header-only library with zero external dependencies — it integrates into Unreal Engine or any custom engine. Both speak the same binary protocol. Both are thin clients: they send player input and render what the server tells them to render.

**Multiplayer works even without AI.** Shared reactive state means that when any value changes on the server, every connected client sees it immediately. This is not an AI feature — it is how the platform works. A game with no AI at all still gets real-time multiplayer synchronization for free. AI is the bonus, not the prerequisite.

**Web companions connect to the same session.** A TypeScript client in a browser can join the same server instance as the Unity game clients. A spectator view. A game master dashboard. A companion app that shows inventory or quest logs. No separate API, no separate backend. Same server, same state, different view.

## The server calls into the game

The relationship between server and client is not one-directional. The server can invoke functions that the game client registered.

A Unity game registers a function that returns the player's position and inventory. The server's AI logic calls that function when an NPC needs context for a conversation. An Unreal client registers a function that triggers a camera shake. The server calls it when a dragon lands nearby.

```csharp
// Unity client registers functions the server can call
[Function(Visibility = FunctionVisibility.Shared, Description = "Returns player position")]
public Vector3 GetPlayerPosition()
{
    return _player.transform.position;
}

[Function(Visibility = FunctionVisibility.Shared, Description = "Returns current inventory")]
public string[] GetInventory()
{
    return _inventory.Items.Select(i => i.Name).ToArray();
}

client.FunctionRegistry.RegisterFromInstance(this);
```

The server sees these functions. The AI logic calls them when it needs context. The game client does not decide when to send data — the server asks for it when the AI needs it. This inverts the traditional pattern where the client pushes state and the server reacts.

## World state lives on the server

In a traditional multiplayer game, the server maintains authoritative state and clients maintain local copies with prediction and reconciliation. On Ikon, the server maintains all state and clients render what they receive. There is no prediction, no reconciliation, no desync. When an NPC changes behavior, every player sees it in the same frame.

This is what AI game logic looks like on the server:

```csharp
var (dialogue, _) = await Emerge.Run<NPCDialogue>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = $"{npc.Name} responds to the player. "
                 + $"Personality: {npc.Personality}. "
                 + $"Recent world events: {worldState.RecentEvents}. "
                 + $"This player's history: {player.ConversationLog}";
}).FinalAsync();

npc.CurrentDialogue.Value = dialogue.Text;
npc.Mood.Value = dialogue.Mood;
```

The NPC has a personality that persists across sessions. It knows about world events caused by other players. It remembers past conversations with this specific player. The developer wrote the prompt. The platform resolved the credentials — the server code never sees an API key. The game client received the updated dialogue as a state change. Everyone connected to the session sees the NPC's mood shift.

## No credentials in the game build

Every game that integrates AI today faces the same problem: API keys need to go somewhere. Bundle them in the client and they get extracted within hours. Proxy them through a backend and you are maintaining a second codebase.

On Ikon, the game client never touches AI credentials — and neither does the server-side game code. When you call `Emerge.Run`, the platform resolves credentials internally. There is nothing in the game build to extract. There is nothing in the server code to leak. Swap a model or rotate an API key on the platform side and every game session picks it up. No new build. No patch.

## Scenarios

**AI NPCs with persistent memory.** Players connect from Unity clients. Each NPC is driven by an AI model with access to the full world state — the economy, faction relationships, recent player actions. A shopkeeper adjusts prices based on supply and demand created by player behavior. A guard mentions a disturbance that another player caused an hour ago. The NPCs share a world. The players shape it.

**Training simulation with instructor oversight.** An Unreal Engine environment simulates equipment failure scenarios. Trainees interact with the simulation. An instructor connects from a web browser and sees analytics — trainee decisions, timing, AI confidence scores — in real time. The trainee sees the 3D environment. The instructor sees the data. Same server. Same session. Different views. The instructor can inject scenarios, adjust difficulty, or override AI decisions mid-session without the trainee restarting.

**Live game master dashboard.** A designer connects to the running game server through a web interface. They see every active NPC, their current state, their recent conversations. They can adjust an NPC's personality, inject a world event, or override a dialogue response — and every player in the game sees the effect immediately. The AI continues from the new state. The designer is not editing files and redeploying. They are sculpting a living world.

## The key insight

A multiplayer game server and an AI backend solve overlapping problems — persistent connections, shared state, session management, real-time communication. Building them as separate systems means duplicating infrastructure and then building a bridge between them.

IkonServer is both. The game logic and the AI logic are the same code, running in the same process, operating on the same state. The game engine is a thin client that renders and reports. The AI is a function call away. Deploy once, and every connected client — Unity, Unreal, browser — gets the update.
