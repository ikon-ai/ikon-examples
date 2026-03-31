# When AI Apps Talk to Each Other

An Ikon AI app is a server. It handles connections, runs AI models, manages state, and serves a UI. That is the normal case — a browser or a game client connects to an app and interacts with it.

But an Ikon app can also be a client. Using the same SDK that connects a browser or a Unity game to a server, one AI app can connect to another AI app — calling its functions, reading its state, reacting to its events. This turns individual applications into components of a larger distributed system.

## What this unlocks

**Composable AI services.** Each app is a self-contained system — its own container, its own AI models, its own connected devices, its own UI. But it can expose functions that other apps call. A climate optimization app exposes `getZoneTemperature`. A security app exposes `getOccupancyMap`. A third app connects to both and correlates their data. Each app is independently deployable and independently useful. Together they are something larger.

**Specialization without silos.** Traditional enterprise systems integrate through message queues, REST APIs, and event buses. Each integration is a custom project. On Ikon, the integration mechanism is the same protocol and function registry that connects any client to any server. An app-to-app connection is identical to a browser-to-server connection. No new protocol. No new infrastructure.

**AI that reasons across systems.** When one app can call functions on another, an AI model in the first app can query data from the second. A natural language question like "Why is Floor 3 so warm?" can trigger function calls to a climate system, a scheduling system, and an occupancy tracker — correlating data that lives in separate services, through a single AI reasoning pass.

## How it works

The C# SDK supports a Backend authentication mode designed for server-to-server connections. One app creates an `IkonClient`, connects to another app's server, and interacts with it exactly as any other client would:

```csharp
var climateClient = new IkonClient(new IkonClientConfig
{
    Backend = new BackendConfig
    {
        SpaceId = "building-ops-space",
        ChannelKey = "climate-system",
        ExternalUserId = "dashboard-app",
        UserType = UserType.Machine,
        ClientType = ClientType.DesktopApp
    },
    Description = "Dashboard → Climate"
});

await climateClient.ConnectAsync();
```

Once connected, the dashboard app can call any shared function the climate app has registered:

```csharp
var temperature = await climateClient.FunctionRegistry.CallAsync<float>(
    "GetZoneTemperature", args: ["floor-3"]);

var forecast = await climateClient.FunctionRegistry.CallAsync<EnergyForecast>(
    "GetEnergyForecast", args: ["next-24h"]);
```

The climate app does not know or care that the caller is another AI app rather than a browser. The function registry is the same. The protocol is the same. The authentication is the same.

## A distributed building operations center

Here is a concrete example of what this architecture enables. A building management system composed of four specialized AI apps, each a complete system on its own, connected into a coherent whole.

**Climate App.** Monitors HVAC systems through C++ clients running on zone controllers throughout the building. Runs AI optimization for energy efficiency — balancing comfort against cost, predicting demand based on weather and occupancy patterns. Exposes functions: `GetZoneTemperature`, `SetTargetTemp`, `GetEnergyForecast`, `GetComfortScore`. Has its own web UI for HVAC engineers.

**Security App.** Processes camera feeds for anomaly detection. Manages access control through badge readers connected as native clients. Tracks occupancy across floors and zones. Exposes functions: `GetActiveAlerts`, `GetOccupancyMap`, `GetAccessLog`, `GrantTemporaryAccess`. Has its own web UI for security staff.

**Scheduling App.** Manages room bookings and space allocation. Uses AI to predict usage patterns and suggest optimal room assignments. Integrates with calendar systems. Exposes functions: `GetRoomAvailability`, `BookRoom`, `GetUsagePrediction`, `GetUpcomingEvents`. Has its own web UI for office managers.

**Operations Dashboard.** Connects to all three apps as a client using Backend auth. Aggregates their data into a unified view for building managers. And this is where it gets interesting — the dashboard has its own AI assistant that can reason across all three systems:

```csharp
pass.Command = "The building manager asks: 'Why is Floor 3 so warm?'";
pass.AddTool("climate", "Get temperature and HVAC data",
    async (string zone) => await _climateClient.FunctionRegistry
        .CallAsync<ZoneData>("GetZoneData", args: [zone]));

pass.AddTool("occupancy", "Get current occupancy",
    async (string floor) => await _securityClient.FunctionRegistry
        .CallAsync<OccupancyData>("GetOccupancyMap", args: [floor]));

pass.AddTool("schedule", "Get room bookings",
    async (string floor) => await _schedulingClient.FunctionRegistry
        .CallAsync<BookingData>("GetUpcomingEvents", args: [floor]));
```

The AI calls `GetZoneData("floor-3")` on the climate app — the HVAC is running normally, target temperature is met. It calls `GetOccupancyMap("floor-3")` on the security app — occupancy is at 180% of expected. It calls `GetUpcomingEvents("floor-3")` on the scheduling app — there is an all-hands meeting in the large conference room that was not accounted for in the climate model.

The answer: "Floor 3 is warm because the large conference room has 90 people in it for an all-hands meeting, but the HVAC system was configured for normal occupancy. I can ask the climate system to boost cooling for that zone — should I proceed?"

One AI reasoning pass. Three separate systems queried. A coherent answer that no single system could produce on its own.

## Each app is independent

This is not a monolith split across containers. Each app is a genuinely independent system.

The climate app works on its own — HVAC engineers use it daily without the dashboard. The security app processes alerts independently. The scheduling app manages bookings without knowing the climate system exists. Each has its own AI models tuned for its domain. Each has its own connected hardware. Each can be deployed, updated, and scaled independently.

The connections between them are optional. If the dashboard goes down, the other three systems continue operating. If the climate app is updated with a new energy optimization model, the dashboard picks up the new behavior through the same function calls — no API version negotiation, no schema migration.

## Beyond buildings

The pattern is general. Any domain where specialized AI systems need to share data and coordinate decisions can use the same architecture.

**Hospital operations.** A patient monitoring app tracks vitals through bedside devices. A staffing app optimizes nurse assignments. A pharmacy app manages medication scheduling. A clinical dashboard connects to all three — an AI assistant can answer "Is Room 412 ready for the incoming transfer?" by checking vitals status, nurse availability, and medication preparation across systems.

**Supply chain.** A warehouse app manages inventory through sensor-equipped shelves. A logistics app coordinates delivery vehicles. A demand forecasting app analyzes sales patterns. A planning dashboard connects to all three and reasons about the full picture.

**Smart campus.** An energy app manages solar panels and battery storage. A transport app coordinates autonomous shuttles. A facilities app tracks maintenance schedules. A campus operations dashboard unifies them.

## The insight

The same protocol and function registry that connects a browser to a server also connects one server to another. An AI app that is a complete system on its own becomes a composable service when another app connects to it. No new infrastructure. No integration layer. No message queue. Just another client connection — except the client happens to be running AI models of its own.

Individual AI applications are useful. Connected AI applications are transformative. The distance between the two is one SDK connection.
