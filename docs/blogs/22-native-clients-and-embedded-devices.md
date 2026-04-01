# A Raspberry Pi with Cloud Intelligence

You build a small robot on a Saturday afternoon. A Raspberry Pi, a camera, a distance sensor, two motors. You set it on the living room floor and open your phone.

The robot starts moving. On your phone you see what it sees — a live camera feed with the AI's narration underneath. "Doorway ahead, entering kitchen. Table and chairs. Cat on counter." The robot pauses at the cat, takes a closer look, then continues past. You did not program this. The robot does not know what a cat is. It has a camera and wheels — nothing else.

There is one application running on a server in the cloud. That application is doing three things at once: it talks to the robot over a network connection, telling it where to go and asking for camera frames. It runs AI models that see through the camera, recognize objects, and plan a route. And it serves a web page — the one on your phone — that shows the live feed and lets you tap where the robot should go next.

One app. One server. The robot and your phone are both connected to it.

Your roommate hears the motors and opens the same URL on their laptop. They see the same feed. They tap on a room the robot has not visited yet. The robot heads there. Two people on two devices, one robot on the floor, all sharing the same live session.

## How this works

The architecture is simple. There are three participants, and they all connect to the same application.

The robot is a C++ program on a Raspberry Pi. It connects to the server over a network socket. It can do four things: take a photo, read the distance sensor, move forward, and turn. It tells the server about these capabilities when it connects, and then it waits for instructions.

The application runs on the server. It has the AI — vision models, planning logic, language models. It calls the robot's functions when it needs to: "take a photo" to see, "read distance" to check for obstacles, "move forward" to navigate. It also keeps track of everything — the map, the AI's observations, the robot's position — as live shared state.

Your phone browser connects to the same application over the web. It sees the shared state: the camera feed, the map, the AI narration. When you tap a location on the map, that becomes a waypoint in the shared state, and the AI picks it up on the next cycle.

The robot provides eyes and wheels. The server provides the brain. The browser provides the window. They are all part of one application.

## Why sixty euros feels like magic

The Raspberry Pi costs $35. The camera module, sensor, and motors add maybe $25. There is no AI chip, no GPU, no fancy hardware. The robot cannot run a vision model or a language model. It does not need to — all of that runs on the server.

This is what makes it feel like something it is not. The hardware is a hobby project. The behavior — navigating rooms, recognizing objects, describing what it sees in plain English, building a map — is sophisticated. The gap between the hardware and the behavior is the server.

The device only carries one credential: an Ikon session key. Not your OpenAI API key. Not your cloud secrets. If someone takes the robot apart, they find a key that can be revoked in seconds. The AI credentials live on the server, where the device cannot reach them.

If the robot rolls behind the couch and loses Wi-Fi, the connection restores itself automatically. The session continues where it left off.

## The pattern

Device, server, browser. The device provides sensors and actuators. The server provides intelligence. The browser provides a live view and controls. All three connect to one application, share one live state, and work together.

The device does not need to be smart. It needs to be connected.

Next: [Three Interfaces and a TCP Socket](24-three-interfaces-and-a-tcp-socket.md) — the C++ code that connects a device. Then: [The Teddy Bear Has No Brain](25-the-teddy-bear-has-no-brain.md) — a talking toy that shows why the server should be in charge.
