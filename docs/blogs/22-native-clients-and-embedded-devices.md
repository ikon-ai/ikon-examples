# A Raspberry Pi with Cloud Intelligence

A Raspberry Pi with a camera and two motors. No GPU. No Python runtime. No API key. It connects to an Ikon server over a TCP socket and becomes an autonomous robot — because the intelligence runs in the cloud, and the device just executes.

The device registers functions: read the camera, check the distance sensor, move forward, turn left. The server calls those functions when it needs to. An AI vision model analyzes what the camera sees. A planning model decides where to go next. The server calls `moveForward` on the device. The robot moves.

An operator opens a web browser, connects to the same server, and sees live telemetry — camera feed, sensor readings, the AI's reasoning, a map of where the robot has been. They draw a waypoint on the map. The server adds it to the mission plan. The robot navigates there autonomously.

One server. Two clients. One speaks C++ on a Raspberry Pi. The other speaks TypeScript in a browser. Both connect to the same application, see the same state, and interact with the same AI.

## What this unlocks

**AI on devices that cannot run AI.** The device does not need a GPU, a language model, or even a significant CPU. It needs a network connection and a C++ compiler. The server handles all reasoning — vision, planning, natural language, anomaly detection — and the device handles what it is good at: reading sensors and moving actuators.

**A web UI for every device, for free.** Any device that connects to an Ikon server automatically gets a web interface. The operator connects a browser to the same session. They see what the server sees. No separate dashboard project. No API layer between the device and the UI. The browser and the device are peers on the same server.

**Fleet management from one server.** Ten robots connect to the same application. The server tracks each one, coordinates their movements, prevents collisions, and optimizes routes. The operator's web dashboard shows all ten. Adding the eleventh robot is connecting another C++ client. No configuration change. No new API endpoint.

**Reconnection is automatic.** The robot drives through a tunnel and loses signal. The C++ SDK reconnects with exponential backoff — 500ms, 1s, 2s, 4s. The server knows the device is temporarily offline. When the connection resumes, the session continues. No state is lost. The mission continues.

## The C++ SDK

The SDK is a header-only library with zero external dependencies. It requires C++17 and three interface implementations — logging, HTTP (for initial provisioning), and TCP (for the persistent connection). That is the entire integration surface.

```cpp
#include "ikon_sdk.h"

// Provide three interfaces — the SDK handles everything else
auto log = std::make_shared<DeviceLogger>();
auto http = std::make_shared<CurlHttpClient>();
auto tcp = std::make_shared<PosixTcpClient>();

IkonClientConfig config;
config.apiKey = ApiKeyConfig{
    .apiKey = "ikon-xxxxx",
    .spaceId = "fleet-space-id",
    .externalUserId = "robot-07",
    .userType = UserType::Machine,
    .clientType = ClientType::DesktopApp
};
config.description = "Warehouse Robot 07";

IkonClient client(config, log, http, tcp);
```

The SDK works on anything that compiles C++17 — Raspberry Pi, NVIDIA Jetson, industrial PLCs, medical devices, custom hardware. The three interfaces adapt it to whatever networking and logging the platform provides.

## The device registers functions

The robot does not push data. It exposes capabilities. The server decides when to call them.

On the C# SDK side (for devices using .NET), function registration is explicit:

```csharp
[Function(Visibility = FunctionVisibility.Shared, Description = "Captures a camera frame")]
public byte[] CaptureFrame()
{
    return _camera.CaptureJpeg();
}

[Function(Visibility = FunctionVisibility.Shared, Description = "Reads distance sensor in cm")]
public float ReadDistance()
{
    return _distanceSensor.Read();
}

[Function(Visibility = FunctionVisibility.Shared, Description = "Moves forward by distance in cm")]
public bool MoveForward(float distanceCm)
{
    return _motors.DriveForward(distanceCm);
}

[Function(Visibility = FunctionVisibility.Shared, Description = "Rotates by angle in degrees")]
public bool Rotate(float angleDegrees)
{
    return _motors.Rotate(angleDegrees);
}

client.FunctionRegistry.RegisterFromInstance(this);
```

The server sees four functions. It does not know they are running on a Raspberry Pi. It calls them exactly as it would call a function on a Unity game client or a web browser — the protocol is the same.

## The server runs the AI loop

On the server, an AI reasoning loop orchestrates the robot's behavior. It calls device functions to perceive, uses AI models to plan, and calls device functions to act:

```csharp
while (!token.IsCancellationRequested)
{
    var frame = await FunctionRegistry.Instance.CallAsync<byte[]>(
        "CaptureFrame", targetId: robotSessionId);

    var distance = await FunctionRegistry.Instance.CallAsync<float>(
        "ReadDistance", targetId: robotSessionId);

    var (decision, _) = await Emerge.Run<NavigationDecision>(
        LLMModel.Claude46Sonnet, context, pass =>
    {
        pass.Command = "Analyze the camera image and distance reading. "
                     + $"Current distance to obstacle: {distance}cm. "
                     + $"Mission waypoints remaining: {mission.RemainingWaypoints}. "
                     + "Decide: move forward, rotate, or stop.";
        pass.AddImage(frame);
    }).FinalAsync();

    if (decision.Action == "move")
    {
        await FunctionRegistry.Instance.CallAsync(
            "MoveForward", targetId: robotSessionId,
            args: [decision.DistanceCm]);
    }

    _robotStatus.Value = decision.Reasoning;
    await Task.Delay(500, token);
}
```

The AI sees through the robot's camera. It reads the distance sensor. It decides what to do. The robot moves. The operator's web dashboard updates with the AI's reasoning in real time — because `_robotStatus` is shared reactive state.

The server code never touches an API key. The robot never runs an AI model. The operator never writes backend code. Each component does one thing.

## Scenarios

**Warehouse inspection.** A fleet of small robots patrols a warehouse overnight. Each robot has a camera and basic mobility. The server runs vision AI to detect anomalies — spills, misplaced inventory, damaged shelving. The morning shift manager opens a browser dashboard and sees flagged issues with photos, locations, and AI-generated descriptions. The robots did not process any images locally. They captured frames and the server did the thinking.

**Agricultural monitoring.** Sensors scattered across a farm — soil moisture, temperature, camera traps — each run by a small C++ client on cheap hardware. The server aggregates readings, runs AI models to predict irrigation needs and detect crop disease from images. A farmer checks a web dashboard on their phone. The server has already generated recommendations. The sensors have no AI capability. They read values and the server makes decisions.

**Remote operations.** An inspection drone in a hard-to-reach location — an offshore wind turbine, a mining tunnel, a power line corridor — connects to an Ikon server. An operator hundreds of kilometers away plans the inspection route in a web interface. The server coordinates the drone's movements, runs AI analysis on the video feed, and flags issues for human review. The drone carries a camera and a radio. The intelligence is in the cloud.

## The pattern

The device is a thin client with sensors and actuators. The server is the brain. The web browser is the eyes. All three connect to the same application, share the same state, and interact through the same protocol. The device does not need to be smart. It needs to be connected.
