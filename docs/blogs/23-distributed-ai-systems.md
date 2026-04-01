# When AI Apps Talk to Each Other

You built a talking teddy bear for your kid. A Pi Zero with a microphone and speaker inside a stuffed animal, connected to an Ikon server. The bear tells stories, answers questions, remembers yesterday's conversation. It is its own app, and it works well.

Separately, you set up a room sensor in the kid's bedroom. Temperature, light level, and noise. It is its own app too — you check the readings on your phone. You used it to figure out that the room gets too cold around 3 AM, and you adjusted the thermostat.

Two projects. Two apps. Each useful on its own.

Then you connect them.

## The bear learns the room

The room sensor app exposes its readings — temperature, light, noise level — as functions any connected client can call. You connect the bear's app to the sensor's app. Now the bear's AI can ask: what is the light level? What is the temperature? How quiet is the room?

The behavior changes immediately.

It is dark and quiet. The kid picks up the bear and whispers. The bear whispers back — because the AI knows the room is dark, so it is probably bedtime. It tells a calm, short story instead of an exciting one.

Morning. The light sensor reads bright. The kid grabs the bear. "Good morning!" says the bear, cheerful and loud. "Did you sleep okay?" It is a different bear in the morning than at night — not because you programmed two modes, but because the AI sees the room and adjusts.

The temperature drops. The bear says, "Brrr, it's getting chilly in here! Want me to tell Mom?" The kid says yes. The parent, watching the conversation log in a browser, sees the request and turns up the heat.

None of this was possible when the bear and the sensor were separate apps. The bear did not know if it was bright or dark. The sensor did not know anyone was talking. Connected, the bear gains awareness it could not have alone.

## How the connection works

The code is short. The bear's app connects to the sensor's app the same way a browser would:

```csharp
var sensorClient = new IkonClient(new IkonClientConfig
{
    Backend = new BackendConfig
    {
        SpaceId = "kids-room",
        ChannelKey = "room-sensor",
        ExternalUserId = "teddy-bear-app",
        UserType = UserType.Machine
    }
});

await sensorClient.ConnectAsync();

var temp = await sensorClient.FunctionRegistry.CallAsync<float>("GetTemperature");
var light = await sensorClient.FunctionRegistry.CallAsync<float>("GetLightLevel");
```

The sensor app does not know the caller is a teddy bear. It just sees another client asking for readings. The connection uses the same protocol that browsers and devices use. Nothing special to build.

## Each app stays independent

The bear still works without the sensor. If you unplug the sensor, the bear tells stories the same way it always did — it just does not know if it is dark or cold. The sensor still works without the bear. Each app is a complete system on its own. The connection adds something, but it does not create a dependency.

This is what makes it practical. You do not have to design a master system that controls everything. You build small, independent projects. If two of them would benefit from knowing about each other, you connect them. If it does not work out, you disconnect them. Nothing breaks.

## Beyond the bedroom

The pattern scales. A maker space has an inventory tracker and a 3D printer queue — connected, someone can ask "Can I print the drone frame today?" and get an answer that checks both filament stock and printer availability. A small farm has soil sensors and a weather station — connected, the AI can say "Irrigate the north field tomorrow, rain is unlikely until Thursday." A group of friends each builds a different device — a robot, a drone, a camera trap — and connecting them creates a system none of them could build alone.

One app is a project. Connected apps are a system.
