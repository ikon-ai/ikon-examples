# The AI Multiplayer Server

*Published 2026-03-31*

A multiplayer game needs a server. It handles persistent connections, player sessions, shared world state, real-time synchronization and reconnection. Adding AI-powered NPCs means a second server, or at least a separate backend, to run language models, manage prompts and feed responses back into the game. A web-based companion app or spectator view needs a third layer. That makes three systems, three deployments and three sets of state to keep in sync.

With Ikon, one IkonServer does all of it.

## What this makes possible

**Your game server is the AI server.** IkonServer already does everything a multiplayer game server does: persistent connections, shared reactive state, player sessions, automatic reconnection and real-time sync across clients. AI runs in the same process. NPC dialogue, procedural content and adaptive difficulty are server-side function calls, so there are no separate services to integrate.

**Unity and Unreal connect as thin clients.** The C# SDK targets .NET Standard 2.1, which means it works directly in Unity projects. The C++ SDK is a header-only library with zero external dependencies, so it works in Unreal Engine or any custom engine. Both SDKs use the same binary protocol, and both are thin clients that send player input and render what the server tells them to render.

**Multiplayer works even without AI.** With shared reactive state, every connected client sees a change to any value on the server immediately. This is how the platform works whether or not you use AI, so a game with no AI at all still gets real-time multiplayer synchronization without extra work. You can add AI, but you don't have to.

**Web companions connect to the same session.** A TypeScript client in a browser can join the same server instance as the Unity game clients. It can be a spectator view, a game master dashboard or a companion app that shows inventory or quest logs. It uses the same server and the same state as the game, with its own view and no separate API or backend.

## The server calls into the game

Calls do not only go from the client to the server. The server can also call functions that the game client registered.

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

The server can see these functions, and the AI logic calls them when it needs context. The server asks the game client for data when the AI needs it, instead of the client deciding when to send it. This is the reverse of the traditional pattern, where the client pushes state and the server reacts.

## World state is kept on the server

In a traditional multiplayer game, the server keeps the authoritative state and clients keep local copies with prediction and reconciliation. On Ikon, the server keeps all state and clients render what they receive, so there is no prediction, reconciliation or desync. When an NPC changes behavior, every player sees it in the same frame.

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

The NPC has a personality that persists across sessions. It knows about world events that other players caused, and it remembers past conversations with this specific player. The developer wrote the prompt and the platform resolved the credentials, so the server code never sees an API key. The game client receives the updated dialogue as a state change, and everyone connected to the session sees the NPC's mood change.

## No credentials in the game build

Every game that uses AI today has to keep its API keys somewhere. Keys bundled in the client get extracted within hours. Proxying them through a backend means maintaining a second codebase.

On Ikon, neither the game client nor the server-side game code touches AI credentials. When you call `Emerge.Run`, the platform resolves the credentials internally, so there is nothing in the game build to extract and nothing in the server code to leak. If you swap a model or rotate an API key on the platform side, every game session picks up the change without a new build or patch.

## Scenarios

**AI NPCs with persistent memory.** Players connect from Unity clients. Each NPC is driven by an AI model that can read the full world state, including the economy, faction relationships and recent player actions. A shopkeeper adjusts prices based on supply and demand created by player behavior. A guard mentions a disturbance that another player caused an hour ago. The NPCs share one world, and the players' actions change it.

**Training simulation with instructor oversight.** An Unreal Engine environment simulates equipment failure scenarios, and trainees interact with the simulation. An instructor connects from a web browser and sees analytics in real time, such as trainee decisions, timing and AI confidence scores. The trainee sees the 3D environment and the instructor sees the data, both from the same server and the same session. The instructor can inject scenarios, adjust difficulty or override AI decisions during the session, and the trainee does not have to restart.

**Live game master dashboard.** A designer connects to the running game server through a web interface. They see every active NPC with its current state and recent conversations. They can adjust an NPC's personality, inject a world event or override a dialogue response, and every player in the game sees the effect immediately. The AI continues from the new state. The designer changes the running game directly, without editing files or redeploying.

## One server for the game and the AI

A multiplayer game server and an AI backend solve overlapping problems: persistent connections, shared state, session management and real-time communication. Building them as separate systems means duplicating that infrastructure and then connecting the two.

IkonServer is both. The game logic and the AI logic are the same code, running in the same process and working on the same state. The game engine is a thin client that renders the game and sends player input, and an AI call is one function call in the server code. When you deploy, every connected client gets the update, whether it runs in Unity, Unreal or a browser.
