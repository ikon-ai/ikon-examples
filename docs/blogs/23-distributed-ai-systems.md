# When AI Apps Talk to Each Other

*Published 2026-03-31*

You built a talking teddy bear for your kid. It is a Pi Zero with a microphone and a speaker inside a stuffed animal, connected to an Ikon server. The bear tells stories, answers questions and remembers yesterday's conversation. It is its own app, and it works well.

Separately, you set up a room sensor in the kid's bedroom. It measures temperature, light level and noise. It is its own app too, and you check the readings on your phone. You used it to find out that the room gets too cold around 3 AM, and you adjusted the thermostat.

So you have two separate apps, each useful on its own.

Then you connect them.

## The bear uses the room sensor

The room sensor app makes its readings (temperature, light and noise level) available as functions that any connected client can call. You connect the bear's app to the sensor's app. Now the AI behind the bear can ask for the light level, the temperature and how quiet the room is.

The bear's behavior changes right away.

At night the room is dark and quiet. The kid picks up the bear and whispers. The bear whispers back, because the AI sees that the room is dark and concludes that it is probably bedtime. It tells a short, calm story instead of an exciting one.

In the morning the light sensor reads bright. The kid grabs the bear. "Good morning!" says the bear, cheerful and loud. "Did you sleep okay?" The bear behaves differently in the morning than at night. You did not program two modes. The AI reads the sensor values and adjusts how the bear talks.

The temperature drops. The bear says, "Brrr, it's getting chilly in here! Want me to tell Mom?" The kid says yes. The parent, watching the conversation log in a browser, sees the request and turns up the heat.

None of this was possible while the bear and the sensor were separate apps. The bear did not know whether the room was bright or dark, and the sensor did not know anyone was talking. Once the apps are connected, the bear can use the sensor's readings in what it says.

## How the connection works

The bear's app connects to the sensor's app the same way a browser would, with a few lines of code:

```csharp
var sensorClient = new IkonClient(new IkonClientConfig
{
    Backend = new BackendConfig
    {
        SpaceId = "kids-room",
        ExternalUserId = "teddy-bear-app",
        UserType = UserType.Machine
    }
});

await sensorClient.ConnectAsync();

var temp = await sensorClient.FunctionRegistry.CallAsync<float>("GetTemperature");
var light = await sensorClient.FunctionRegistry.CallAsync<float>("GetLightLevel");
```

The sensor app does not know the caller is a teddy bear. To the sensor app, the bear is one more client asking for readings. The connection uses the same protocol that browsers and devices use, so there is nothing extra to build.

## Each app stays independent

The bear still works without the sensor. If you unplug the sensor, the bear tells stories as it did before, but it no longer knows whether the room is dark or cold. The sensor also still works without the bear. Each app works on its own. The connection adds features, but neither app depends on the other.

Because of this, you do not have to design one central system that controls everything. You build small, independent projects. If two of them would benefit from knowing about each other, you connect them. If the connection turns out not to be useful, you disconnect them, and both apps keep working.

## Beyond the bedroom

The same approach works for larger setups. A maker space has an inventory tracker and a 3D printer queue. When they are connected, someone can ask "Can I print the drone frame today?" and get an answer that checks both the filament stock and whether a printer is free. A small farm has soil sensors and a weather station. When they are connected, the AI can say "Irrigate the north field tomorrow, rain is unlikely until Thursday." A group of friends each build a different device, such as a robot, a drone and a camera trap. Connecting the devices gives the group a system that none of them could build alone.
