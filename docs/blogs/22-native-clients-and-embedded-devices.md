# A Raspberry Pi with Cloud Intelligence

*Published 2026-03-31*

You build a small robot on a Saturday afternoon. A Raspberry Pi, a camera, a distance sensor, two motors. You set it on the living room floor and open your phone.

The robot starts moving. On your phone you see a live feed from its camera, with the AI's narration underneath. "Doorway ahead, entering kitchen. Table and chairs. Cat on counter." The robot pauses at the cat, takes a closer look, then continues past. You did not program this. The robot does not know what a cat is. It has a camera, a distance sensor and wheels.

One application runs on a server in the cloud, and it does three things at once. It talks to the robot over a network connection, telling it where to go and asking for camera frames. It runs AI models that look at the camera frames, recognize objects and plan a route. It also serves the web page on your phone, which shows the live feed and lets you tap where the robot should go next.

The robot and your phone are both connected to that one app on one server.

Your roommate hears the motors and opens the same URL on their laptop. They see the same feed. They tap on a room the robot has not visited yet, and the robot heads there. Two people on two devices and one robot on the floor are now sharing the same live session.

## How this works

The architecture is simple. There are three participants, and they all connect to the same application.

The robot is a C++ program on a Raspberry Pi. It connects to the server over a network socket. It can do four things: take a photo, read the distance sensor, move forward, and turn. It tells the server about these capabilities when it connects, and then it waits for instructions.

The application runs on the server. It runs the AI: vision models, planning logic and language models. It calls the robot's functions when it needs to: "take a photo" to see, "read distance" to check for obstacles, "move forward" to navigate. It also keeps the map, the AI's observations and the robot's position as live shared state.

Your phone browser connects to the same application over the web. It sees the shared state: the camera feed, the map, the AI narration. When you tap a location on the map, that becomes a waypoint in the shared state, and the AI picks it up on the next cycle.

The robot supplies the sensors and motors, the server does the thinking, and the browser shows what is happening and takes your input. All three are part of one application.

## What sixty euros of hardware can do

The Raspberry Pi costs $35. The camera module, sensor, and motors add maybe $25. There is no AI chip, GPU or other special hardware. The robot cannot run a vision model or a language model, and it does not need to, because all of that runs on the server.

That is why the robot seems far more capable than its hardware. The hardware is a hobby project, but the robot navigates rooms, recognizes objects, describes what it sees in plain English and builds a map. The server does everything the hardware cannot.

The device carries only one credential, an Ikon session key. Your OpenAI API key and your cloud secrets are not on it. If someone takes the robot apart, they find a key that can be revoked in seconds. The AI credentials stay on the server, where the device cannot reach them.

If the robot rolls behind the couch and loses Wi-Fi, the connection is restored automatically and the session continues where it left off.

## The pattern

The pattern has three parts. The device has the sensors and actuators, the server runs the AI, and the browser gives a live view and controls. All three connect to one application and share one live state.

The device does not need to run AI itself. It only needs a network connection to the application.

Next: [Three Interfaces and a TCP Socket](24-three-interfaces-and-a-tcp-socket.md) — the C++ code that connects a device. Then: [The Teddy Bear Has No Brain](25-the-teddy-bear-has-no-brain.md) — a talking toy that shows why the server should be in charge.
