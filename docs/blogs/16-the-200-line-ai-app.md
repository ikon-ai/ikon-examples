# The Two-Hundred-Line AI App

*Published 2026-03-19*

A platform's floor, the least code a working app needs, matters as much as its ceiling, the most complex app it can run. If the simplest possible app needs boilerplate and configuration files before anything works, something has gone wrong. The simplest app shows whether a platform's abstractions reduce complexity or only move it somewhere else.

On Ikon, the simplest AI app is a haiku generator of about two hundred lines. You type a topic, and it writes a haiku, generates a matching illustration, and shows both. It is one file, with no separate frontend, API routes, environment variables, or client-side state management, and no loading indicators you have to connect yourself.

The same platform also runs video conferencing apps, game generators, and animated characters with lip-synced speech. This post compares the two ends of that range.

## Two people, one haiku

The haiku generator is collaborative without any code written for that purpose. Say two people open the app, and one types "winter morning" and clicks generate. Both see the button change to "Creating...", then the haiku appear, then the illustration. The person who did not click saw it all happen in real time.

Nobody wrote this feature. The app runs as a persistent process, and changes to shared state reach every connected viewer automatically. Sharing is the default, and the developer has to opt out for values that should be private to each person, such as a theme preference.

For a haiku generator this is a nice extra. For a collaborative AI tool, a live dashboard, or a classroom application, it can turn a multi-month build into a weekend project.

## What the app does

You type a topic. The app sends it to an AI model, which writes a haiku following the traditional 5-7-5 syllable pattern and also writes a visual description of the haiku's mood. That visual description is then sent to an image generation model, which creates a matching illustration. Both the haiku and the image appear on screen as they are generated.

The app has six pieces of state: the topic you typed, the generated haiku, the image data, a flag tracking whether generation is in progress, and a per-user theme preference (light or dark mode). When any shared value changes, the interface updates for every connected viewer. The theme preference is per person, so one viewer can switch to dark mode without affecting anyone else.

The interface is declared in the same file as the logic. It has a text field for the topic, a generate button that is disabled and relabeled during generation, and a results area that appears when there is something to show. In the title, each letter bounces in a staggered wave animation. All styling uses utility classes, so there are no separate style files.

This is all the code that generates the haiku and the matching illustration:

```csharp
private async Task GenerateHaikuAndImageAsync()
{
    _isGenerating.Value = true;
    _generatedHaiku.Value = null;
    _generatedImageData.Value = null;

    try
    {
        var haikuResult = await GenerateHaikuAsync(_topic.Value);
        _generatedHaiku.Value = haikuResult.Haiku;

        var imagePrompt = $"{_topic.Value}. {haikuResult.ImagePrompt}";
        await GenerateImageAsync(imagePrompt);
    }
    finally
    {
        _isGenerating.Value = false;
    }
}
```

The method clears the previous results, generates a haiku, uses the haiku's mood to generate an image, and resets the loading state. Every value change updates every connected viewer's screen automatically.

The whole app is about two hundred lines.

## What the app contains and what it leaves out

Present: reactive state that automatically synchronizes across viewers, a way to call an AI model and get structured results back, a way to generate images, a way to display it all in a styled interface. This is what the application is made of.

Absent: project scaffolding, build configuration, client-server communication setup, API route definitions, environment variable management, real-time infrastructure, state synchronization code, loading state plumbing, error boundary boilerplate, image proxy endpoints. None of these are part of the application itself. They are extra work that the architecture of most platforms requires.

## The traditional stack equivalent

Building the same app on a conventional stack takes a surprising number of pieces for something this simple.

Before you write any application code, you need a project scaffold with configuration files. Then come at least two API endpoints, one for the AI call and one for image generation, each with its own error handling and response formatting. You also have to install and configure provider libraries and manage API keys, which are handled differently in development and production.

The client needs state variables for loading, the haiku, the image, and the topic. It also needs data fetching calls, loading and error states in the interface, and code that manages two network requests the browser makes one after the other.

Even without authentication, persistence, or retries, the smallest working version is probably three to five files and around four hundred lines split across frontend and backend. It also means thinking about two execution environments (browser and server), serializing data between them, and the lifecycle of network calls between them.

That version is also single-user, so two people who open the page each get their own independent copy. To show them the same haiku, you would add real-time communication infrastructure and a shared state store, which roughly doubles the complexity.

## The floor and the ceiling

The same platform that runs this two-hundred-line haiku generator also runs a video conferencing app with live transcription and AI meeting summaries. It runs a game generator that produces playable browser games from text descriptions, play-tests them automatically, critiques the results with vision models, and keeps iterating until the quality stops improving. It also runs animated characters with lip-synced speech and reactive facial expressions.

Supporting small apps does not hold the complex ones back, and supporting complex apps adds no setup to the haiku generator. Both use the same building blocks: reactive state, a server-driven interface, AI orchestration, and structured output. The haiku generator uses one AI call and one image generation call. The game generator uses AI calls with tool use, automated browser testing, and a loop that repeats until the quality stops improving. The larger app uses more of the same parts, not different ones.

Platforms built for complex apps often make simple apps hard. They provide abstractions for complex use cases, but even the simplest app needs a lot of setup and new concepts to learn. The opposite is just as common. Platforms built for quick starts become hard to use as an app grows, and the abstractions that made the simple case easy get in the way when you need real-time collaboration, long-running processes, or multi-model AI orchestration.

A haiku generator should be about two hundred lines. A video conferencing app should be about four thousand. The difference between those numbers should come from the difference between the two applications, not from platform overhead.

## What the floor reveals

The haiku generator is not a useful application, and nobody needs a platform to generate haiku. But it is a useful test of a platform, because it shows which code the platform makes you write and which code it handles for you.

The two hundred lines are not a marketing number. They are the code that is left once the platform handles the setup and infrastructure.
