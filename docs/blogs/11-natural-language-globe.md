# A Natural Language 3D Globe

*Published 2026-03-19*

Type "CO2 emissions" into a text field, and a spinning 3D globe shows animated spikes at locations around the world. Each spike's size shows the magnitude and its color shows the type of data, and you can click a spike to see its value. Type "who's online?" and the globe redraws with internet usage data in blue. Type "how hot is it?" and temperature data appears in red. Several people can connect at once, ask different questions and watch the globe update together.

The backend is about four hundred lines and the 3D rendering component about six hundred, with no API layer, state synchronization code or WebSocket configuration. This post explains how the app works and why it needs so little code.

## What you experience

You type a question in plain language about anything in the world that varies by location. The app interprets your question, generates geographically accurate data points, and renders them as animated spikes on a slowly rotating globe. Click any spike to see the details: location name, coordinates, and value. A side panel shows the current visualization, your query history, and detail cards for selected data points.

Questions can be casual. "Carbon footprint" becomes CO2 emissions data. "Who's online?" becomes internet usage by country. "Show me the money" becomes GDP data. The app works out what you mean and picks a color for the data: green for environmental data, blue for technology, orange for energy.

## Multiuser without extra code

There is no multiuser code in this application. When one person asks "GDP per capita," every connected user sees the globe update with the new data. The server owns the state, and the platform sends every change to all connected clients.

So the app works as a presentation tool without changes. One person types the queries while a room full of people watches the globe on their own screens. Several analysts can also explore different questions and see each other's results. The query history is shared, so it records everything the group has asked.

Some state is per user: the text in your query field and the spike you have selected are yours alone. The globe data, the visualization and the history are shared.

## Two-stage AI pipeline

The app splits the AI work into two passes instead of asking one model to do everything at once.

In the first pass, a fast, cheap model works out what you are asking:

```csharp
var (result, _) = await Emerge.Run<DataQueryResult>(
    LLMModel.Gpt41Mini,
    new KernelContext(),
    pass =>
    {
        pass.Command = command;
        pass.Temperature = 0.3f;
    },
    cancellationToken
).FinalAsync();
```

The result is a typed object with the interpreted query, a display label, a suggested color, and a data category, so the app does not parse JSON or extract anything from response text.

The second pass uses a more capable model to generate the actual geographic data. It produces fifty to a hundred data points, each with realistic latitude and longitude coordinates and proportional magnitudes. Because this model is synthesizing plausible data for dozens of locations, it runs with more freedom to vary its output.

The split is about cost and precision. The interpretation step is cheap and deterministic, while the data generation step needs a bigger, more capable model. Splitting also means you can swap or upgrade either model independently, or add caching on the interpretation layer without affecting data freshness.

Both passes return structured, typed output. The app asks for specific fields (interpreted query, display label, color, data source category in the first pass; location coordinates, magnitudes, and labels in the second) and gets them back ready to use.

## Custom 3D rendering inside a server-driven UI

The globe is a custom 3D scene with atmospheric glow effects, continent outlines drawn from coordinate data, animated spikes, a latitude/longitude grid, orbit controls for spinning and zooming, and click detection that identifies which spike you tapped.

The globe shows that these apps are not limited to forms and charts. The 3D component is built with standard web 3D technology (THREE.js), and the server-driven UI uses it like any other component. For the creator, using the custom globe component is the same as using a built-in text field: you pass it parameters (data points, colors, rotation speed) and handle callbacks (spike clicks). The framework sends the data to the client and routes interactions back to the server.

When you click a spike on the globe, the click is detected on the client and sent to the server. The server updates its state, and a detail card appears in the side panel. The creator does not write any code for this network round trip. In the app's code it is an ordinary callback.

## What this would take on a traditional stack

Building the equivalent without a server-driven UI framework means assembling several independent systems: a frontend application with 3D rendering, a backend API, an AI integration layer with prompt management and structured output parsing, a state management library for the frontend, WebSocket infrastructure for real-time updates across clients, session management, serialization logic between API and frontend, error handling on both sides, and deployment configuration for at least two services.

The custom 3D component would be about the same size on any framework. The code around it is what grows. The API boundary, state synchronization, real-time broadcast and callback routing add hundreds or thousands of lines, and they bring their own kinds of bugs, such as stale state, race conditions, reconnection handling and schema drift between client and server.

Here, the backend is about four hundred lines across seven files. There is no API layer because there is no API. It has no state sync code, because the server owns the state. It has no WebSocket configuration, because the framework handles the transport.

## Takeaway

Plenty of 3D globe visualizations exist. What is unusual about this app is the combination. Natural language input drives a two-stage AI pipeline, the pipeline's output goes straight into a custom 3D renderer, several users share the globe live, and the whole codebase is small enough to read in one sitting. AI orchestration, custom rendering and real-time collaboration usually belong to different teams or different services. Here they run in the same process and are connected by shared reactive values instead of network calls, which is why the backend fits in four hundred lines.
