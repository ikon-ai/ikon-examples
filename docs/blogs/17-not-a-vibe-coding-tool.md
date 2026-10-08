# Not a Vibe Coding Tool (But Also, Yes, a Vibe Coding Tool)

*Published 2026-03-25*

You built a project tracker in ten minutes with a vibe coding tool. The client loved the demo. Then they asked: "Can it join our standup call, transcribe what everyone says, and update the tasks automatically?" You stare at the chat prompt, because the platform that built your app in minutes cannot add a microphone.

Vibe coding tools are platforms like Lovable and Base44, where you describe an app and AI generates it. They run into this limit again and again. They are good at producing traditional web applications from a description, but those applications can only do what the stack they generate for can do. That stack was not designed for applications that listen, speak, watch, work in the background and collaborate in real time.

Ikon is a different kind of platform. It can also do what they do, and it builds apps they cannot.

## What Lovable and Base44 actually are

Both are "vibe coding" tools. Andrej Karpathy coined the term for building software entirely with AI, where you guide the result through conversation instead of writing code. You describe what you want in a chat interface, an LLM writes React + Tailwind + Supabase (or serverless) code, and you keep asking for changes. The AI writes the code and you decide what it should do.

They are good at one kind of app: forms, dashboards, admin panels, landing pages and internal tools. Describe a CRM or an inventory tracker and you get one. For simple CRUD apps they produce a working prototype in minutes.

But the apps they produce are traditional web applications, with AI added as a feature. Both tools build for the web only. Neither supports native audio or video, and neither has real-time multiplayer beyond basic data subscriptions. Neither runs persistent processes, so your app starts for each request, sends a response and stops. The AI builds the app, but it cannot run it, look at it or use it.

Even in business tools, which people assume these platforms are best at, the AI part is a form that calls an LLM API and shows the response. That is a limited kind of AI integration.

## What Ikon is

Ikon is a runtime for AI-native applications, meaning apps where AI runs inside the app. That covers business tools as well as immersive creative apps. The platform provides real-time transport over a single binary connection and server-driven UI that streams to every connected client. It has over 150 AI models in 14 categories, with multi-agent orchestration. It also handles two-way audio and video, runs apps as persistent processes that keep their state, and deploys to the web, game engines and native devices.

Here the AI does not build your app. It runs inside your app, as part of what the app does.

This is not only for showcase apps. Any kind of application can do more when AI is part of the runtime. A business dashboard can show data and also transcribe your meeting about that data, summarize the discussion and update the dashboard with what was decided. A customer support tool can listen to the call in real time, pull up relevant context and suggest responses while the agent is still talking. A project management app can track progress, spot risks and write status reports overnight, so they are ready when you open the app in the morning.

These are business tools. They are also AI-native applications, where the AI takes part in every interaction instead of sitting behind a "generate" button that calls an API.

The earlier posts in this series show the range: video conferencing with live AI transcription, animated characters with lip-synced speech, games that test themselves, 3D data globes and ambient cinema from text. All of them use multi-model orchestration, streaming, audio, video, persistent state and real-time multiplayer. Code generators cannot produce apps like these, because the web stack they generate for does not support those features. The features that make the showcase apps possible are available to business tools, internal apps and customer-facing frontends too, and they make those apps much more capable.

## The comparison most people expect

| | **Base44 / Lovable** | **Ikon** |
|---|---|---|
| How you build | Describe in chat, AI writes code | Describe it in Ikon Studio (or from a brief with the ikon CLI) and AI agents build it, or write code directly |
| What it produces | React + Supabase web apps | AI-native apps across web, Unity, and native |
| AI inside the app | API call integrations | 200+ models, multi-agent orchestration, streaming, structured output, tool use |
| Audio and video | Not supported | Native bidirectional streaming, STT, TTS, lip sync, audio effects |
| Real-time multiplayer | Basic data subscriptions | Automatic, with three scopes of shared state and no extra code |
| App lifecycle | Stateless, wakes per request | Persistent process that keeps running, and background work continues after everyone leaves |
| Platforms | Web only | Web + Unity + C++ native/embedded |
| Security | Client has database keys and API tokens | Client has no credentials. All credentials stay on the server. |
| Best for | CRMs, dashboards, admin panels, business tools | AI-powered experiences where intelligence is the product |

In this comparison Ikon looks like a different kind of product, and it is. If you want a business tool built fast, Base44 or Lovable will build it before you finish reading this post. If you want an AI-native app with voice, video, several models, real-time collaboration or persistent processes, they cannot help, because the stack they generate for does not support it.

But Ikon also builds apps from a description, the same way they do.

## Ikon Studio builds apps from a description

Ikon has a built-in development environment called Ikon Studio. Ikon Studio is itself an Ikon app, built on the same platform with the same reactive UI, AI orchestration and persistent processes.

You describe what you want to build, and several specialized AI agents build it together: a planner that designs the app, a coder that writes it, a designer that improves the interface, a critic that checks the result, and a "magician" that adds AI features. Each agent can read the full Ikon platform documentation and knows the correct APIs, patterns and conventions.

So far this sounds like Lovable or Base44. The difference is that the agents run and test the app they build.

### The AI can run what it builds

When the coder agent writes code, it compiles the app, launches it, and takes screenshots of the running interface. It can see what the app looks like. It can click buttons, fill out forms, and navigate between screens. The critic agent also opens the running app, uses it and compares what it sees against the original plan.

The agents write code, build it, run it, look at it, use it, check it, fix what is wrong, and repeat. They use the app as well as write it.

Base44 and Lovable generate code and show you a preview, but their AI does not use the running application. It cannot click a button to test it, or take a screenshot to check whether the layout matches the design. You have to look at the preview, describe what is wrong and wait for the AI to try again. In Studio, the agents do that checking themselves.

### Multiple agents with different expertise

Studio uses a team of specialized agents instead of one AI. Each agent has different skills and access levels:

The **planner** queries the platform documentation, designs the screens, defines the architecture, and creates a structured plan with scorable milestones.

The **coder** reads the plan, writes code into the workspace, compiles, runs, tests, and iterates. When it gets stuck, it can ask the documentation oracle, which answers questions from the platform documentation, how to use an API correctly.

The **designer** works on layout, styling and motion, so that the interface looks designed and not generated.

The **magician** adds AI features, and this is where Ikon differs most from other platforms. A vibe coding tool can generate a form that calls an LLM API. The magician agent can set up multi-model orchestration, add speech recognition to a voice interface, generate images from user input, or build a system that changes its behavior based on what it has learned so far. It can use every AI capability the platform provides, so it can turn a static app into one that reasons and adapts.

The **critic** checks each phase by running the app, using it and scoring it against the plan, so quality is measured instead of assumed.

The agents communicate through threads. They start child threads for subtasks, ask each other questions, share artifacts such as code and plans, and track progress with scored milestones. The planner starts the coder, and the coder starts a critic. The critic reports back, and the coder fixes the issues and starts the critic again. When the plan's quality scores reach the required threshold, the phase is complete and the next one begins.

### What this looks like in practice

Say you describe: "Build a multiplayer quiz app where players hear the questions read aloud and compete in real time."

The planner designs the screens, queries the platform documentation to understand the audio and multiplayer APIs, and creates a plan with milestones: lobby, question display, voice synthesis, scoring, leaderboard.

The coder implements the lobby and question flow, compiles, runs the app, and takes a screenshot. It sees the lobby rendering correctly. It writes the quiz logic, builds again, takes another screenshot. The score display is misaligned, so it fixes the layout and rebuilds.

The coder starts a critic. The critic launches the app, clicks "Start Quiz," hears audio play, sees the question appear, clicks an answer, and checks whether the score updates. It scores the functional milestone at 0.8 out of 1.0, because the quiz works but the timer is not visible. The coder receives the feedback and fixes the timer.

The designer improves the spacing, the transitions and the score animation. Then the magician adds the AI features. The app now generates quiz questions on the fly from a topic the host chooses, adjusts the difficulty to how well players are doing, and uses text-to-speech to read each question aloud at a suitable pace.

The result is a multiplayer quiz app with AI-generated questions, voice narration, adaptive difficulty and real-time collaboration. It runs as a persistent process, and the host and players share the same live state. None of that is possible on the stack that Lovable or Base44 generate for.

### You describe, the agents build

When you use Ikon Studio, you describe what you want in natural language, the same way you would on Lovable or Base44. The agents write the code. The difference is that the generated code targets the Ikon runtime instead of a traditional web stack. The agents writing it have been trained on the platform's documentation, know the correct APIs and can check their work by running the result.

You can open the generated code, read it and change it directly, because the code is yours. You do not have to, though. You say what the app should do and the agents do the rest.

### It builds on the full Ikon runtime

This is the biggest difference. A React + Supabase app that Lovable generates can do only what React + Supabase can do. An Ikon app that Studio builds can do everything described in this blog series.

The AI agents know the Ikon platform APIs. They can generate apps with real-time multiplayer out of the box. They can add speech recognition, text-to-speech, image generation, and multi-model AI orchestration. They can build apps with persistent background processes, live audio effects, and video streaming. They build on the same runtime that produced the video conferencing app, the animated voice chat, and the ambient cinema generator.

In a Lovable-generated app, "uses AI" typically means a text field, a fetch call to an LLM API and a response shown on screen. A Studio-generated Ikon app can have several AI models working together in an Emergence pattern. Emergence is the platform's library of multi-agent workflows that can be combined, such as draft-critique-verify loops, parallel best-of-N selection and debate-then-judge. The results stream to every connected user in real time, with synthesized audio and a reactive UI that updates while the AI works.

The generated app runs as a persistent process, handles multiplayer automatically, can run background tasks and can stream audio and video. Nobody added these features by hand. The runtime provides them by default.

## The updated comparison

| | **Base44 / Lovable** | **Ikon Studio** |
|---|---|---|
| Describe and build | Yes, chat-driven code generation | Yes, development by several orchestrated agents |
| AI tests its own work | No, you check the preview yourself | Yes, the AI runs the app, clicks, takes screenshots and evaluates |
| Development agents | Single LLM writing code | Specialized agents: planner, coder, designer, critic, magician |
| When it is done | You decide | Scored milestones reach a quality threshold |
| What generated apps can do | React + Supabase web apps | Full Ikon runtime: real-time multiplayer, audio/video, 200+ AI models, persistent processes, cross-platform |
| AI inside generated apps | API call integrations | Native multi-model orchestration, streaming, structured output, tool use |
| Audio/video in generated apps | Not possible | Speech, voice, video, lip sync and audio effects |
| Multiplayer in generated apps | Requires manual setup | Automatic by default |
| Generated app lifecycle | Stateless, per-request | Persistent process, background work continues |

## Where Base44 and Lovable are better

They are faster for a static prototype. If you need a basic admin panel in ten minutes and you are not a developer, Base44 or Lovable will get you there. Their chat interface is simpler and easier to learn. For a classic CRUD app, a form that reads and writes to a database with no real-time features and no AI beyond a single API call, they are fast and easy to use.

They are also more mature as "describe and build" tools. Their prompt-to-app process has been refined with hundreds of thousands of users. Studio is newer and its agent orchestration is more complex.

That changes when you want the app to listen, summarize, adapt, work in the background, collaborate in real time or handle audio and video. An AI-first business tool uses AI in every interaction, not only in a chat widget on a CRUD app. For that kind of app, the runtime matters more than the code generator.

## What Base44 and Lovable cannot build

They cannot build an app that needs real-time collaboration deeper than database subscriptions, audio or video, or several AI models working together. They also cannot build an app that keeps working after everyone closes their browser, runs on a game engine or an embedded device, or where the AI is the experience instead of a feature added to a form.

The reason is the runtime their generated code targets, not how well their AI writes code. React + Supabase is a capable stack for many kinds of applications, but it cannot produce a video conferencing app with live AI transcription, an animated character with lip-synced speech, or a game generator that critiques its own output. Better prompts or smarter models will not change that, because the limit is in the stack.

Ikon's runtime was built for these apps. With Studio you can build them the same way you would on Lovable, by describing what you want and letting AI agents write the code. The difference is that the agents can run, see and use what they build. The result runs on a platform where AI, media and real-time collaboration are part of the runtime instead of external services.

## Summary

Base44 and Lovable made it possible for non-developers to create working web apps from a description. For static CRUD applications such as forms, tables and admin panels, that is useful.

But more applications are becoming AI-first, and an AI-first application is more than a form with a generate button. Examples are a meeting tool with real-time transcription, a customer support dashboard that listens to calls and suggests responses, and a project tracker that monitors progress overnight and has a summary waiting in the morning. In a business frontend built this way, AI is part of every interaction from the start instead of being added later.

For these applications, whether they are business tools, internal apps or customer-facing products, the runtime decides what is possible. A React + Supabase stack cannot do real-time audio transcription, persistent background AI processing, or automatic multiplayer collaboration. Ikon can, because the platform was built for it.

Studio is vibe coding for applications that need a runtime built for AI. You describe what you want, and specialized agents plan, build, design and evaluate the app. They run it themselves, use it and score it against the plan. The result is an application that can listen, speak, reason, collaborate with its users in real time and keep working after everyone goes home.
