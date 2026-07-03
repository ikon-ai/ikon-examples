# ikon-examples

Ikon AI App Examples — a collection of example apps built on the Ikon platform showcasing AI-powered experiences with server-driven UI, real-time multiplayer, and integrated AI services.

## Examples

| Example | Description |
|---------|-------------|
| [Ambient](Ikon.App.Examples.Ambient) | Atmospheric video experience generator with AI-generated visuals and audio. Includes preset ambient scenes (fireplace, rain, northern lights) and custom experience creation using AI video and audio generation. |
| [DepthParallax](Ikon.App.Examples.DepthParallax) | Turns a text prompt into a living 2.5D scene: generates an image, estimates a depth map, and renders a live mouse-driven parallax effect with a custom shader. Includes selectable image and depth models, parallax algorithms, and strength/steps controls. |
| [DynamicUI](Ikon.App.Examples.DynamicUI) | Generates custom UI components from natural language descriptions. Chat with AI to describe a UI and see it rendered live with light/dark theme support. |
| [Emergence](Ikon.App.Examples.Emergence) | Interactive demonstration of AI agent orchestration patterns including Run, BestOf, MapReduce, TaskGraph, Solver/Critic, AgenticCoder, TreeSearch, and Structured Tags with live execution visualization. |
| [Globe](Ikon.App.Examples.Globe) | Interactive 3D globe visualization powered by AI. Input natural language queries about global data and get spike-marker visualizations with colors and labels generated via LLM shaders. |
| [Kahoot](Ikon.App.Examples.Kahoot) | Multiplayer quiz game platform with AI-generated questions. Hosts create lobbies, players join and answer in real-time, with leaderboards and dynamic question generation from knowledge bases. |
| [Learning](Ikon.App.Examples.Learning) | Educational AI chat app with Live2D animated character support, text-to-speech with lip-sync animation, model selection, and multiple view modes. |
| [Live2DChat](Ikon.App.Examples.Live2DChat) | Conversational AI chatbot with Live2D character animation. Supports multiple character models, synchronized mouth movements via viseme analysis, and customizable settings. |
| [Tori](Ikon.App.Examples.Tori) | Multi-participant video/audio conferencing app with real-time stream management, screen sharing, and adaptive UI layouts. |
| [Transcript](Ikon.App.Examples.Transcript) | Audio transcription and analysis tool. Upload audio files for speech-to-text conversion via Whisper, then get AI-generated summaries and action items. |
| [VoiceTutor](Ikon.App.Examples.VoiceTutor) | Voice-based language tutoring app with speech recognition, AI-powered responses, and text-to-speech using multiple voice providers. |

## Platform reference apps

| App | Description |
|-----|-------------|
| [Platform Validation](Ikon.App.Platform.Validation) | The app the Ikon team uses to validate the platform itself — a tabbed showcase exercising nearly the entire feature surface: UI components (buttons, inputs, forms, overlays, charts, drag & drop, virtualization), Crosswind styling and theming, audio/video, Rive and Shadertoy, Ikon.AI, MCP, functions, identity, notifications, payments, email, database access, and profiling. A useful working reference for how any given feature is wired up. |

## Documentation

The [`docs/`](docs) directory contains guides and blog posts for building on the Ikon platform:

- **Guides** — Getting started, Crosswind styling and motion, theming, LLM shaders, Emergence patterns, asset system, pipeline, persistent state, platform events, payments, signatures, custom map components, and SDK references (C++, .NET, TypeScript, Flutter).
- **Blog posts** ([`docs/blogs/`](docs/blogs)) — 29 posts covering topics from server-driven UI and binary protocols to ambient cinema, AI game generation, animated voice chat, native clients and embedded devices, and building with AI.
