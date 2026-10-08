# Building an AI Game Generator

*Published 2026-03-19*

Describe a game in plain English, such as "a space shooter where you dodge asteroids and collect fuel cells", and the generator builds a playable browser game. The result is a finished game rather than a mockup, with a title screen, a game loop, collision detection, score tracking, particle effects and neon glow graphics. The generator then plays the game itself, critiques the result and revises the game until it meets quality thresholds.

The whole generator is under five thousand lines in one project, with no separate backend, job queue or cluster of services. This post covers what we learned building it, and why a system like this is now practical to build.

## What you get: describe, generate, play

A user types a game concept. The generator plans the game, builds it, tests it, scores the result and fixes the weakest areas, repeating this loop without any input from the user. A few minutes later, the game is ready to play in the browser. You can watch each step in real time as the generator builds, tests, critiques and revises the game.

Several people can watch the same generation at once. When one person starts a build, the others see its progress live: the iteration count going up, test screenshots appearing and scores improving. If everyone closes their browser and comes back later, the finished game is there. The generation keeps running whether anyone is watching or not.

## Multiuser without extra code

Nobody wrote code for multiuser support. The app runs as a persistent server process with shared reactive state, so every connected user sees the same game library, generation progress and test results, updated live. The app has no WebSocket server, event bus or pub/sub layer. When the status changes to "Iteration 3 of 5 -- play testing," every connected browser shows the change immediately.

## Plan first, then build

The generator does not jump straight into writing game code. It first produces a game design document: title, core mechanics, visual design, game objects, level design, HUD layout, polish details. Every later step checks its work against this plan.

Without the plan, the quality loop would have nothing to compare the game against. "Is this game good?" is subjective. "Does this game implement the mechanics described in the plan?" is measurable. Every critique and every fix refers back to the design document.

## How code generation works

Once the plan exists, an AI model builds the game piece by piece. It writes the code into named sections: styles, layout, configuration, entities, logic, rendering, input handling and game loop. Because the code is split into sections, the critique can name the section that needs improvement, and the fix changes only that section.

The model can also read back what it wrote earlier and review it before moving on. The output is a single self-contained file that runs directly in a browser frame, with no build step, dependencies or bundler.

Multiple AI providers are supported, and switching between them is a single configuration change. The same generation process works across all of them.

## Automated QA: the system plays its own games

Generating the code is the easier part. The harder part is knowing whether the code works.

After each generation round, the generator opens the game in a headless browser and tests it in several phases: the start screen, early gameplay and sustained input. Two kinds of test run together.

A metrics harness measures the numbers: frame rate, whether frames are changing, whether the score counter works, whether input handlers respond, and whether any errors occur.

An AI vision model looks at screenshots of the running game and decides what to do next: click the start button, press arrow keys, wait for something to happen, or declare the test complete. It reports what it sees at each step. The vision model catches problems the metrics miss, such as a game that runs at full speed but shows a blank screen, or a score counter that displays "NaN."

The metrics, in turn, catch problems a screenshot cannot show, such as a game that looks correct but rendered once and froze.

## Four-tier quality framework

After testing, a structured critique scores the game against the original plan across seven dimensions, which roll up into four tiers:

- **Functional** -- does the game run without errors and respond to input?
- **Visual** -- do the visual elements, HUD, and game objects match what was planned?
- **Gameplay** -- do the mechanics, progression, and collision detection work?
- **Polish** -- are particle effects, animations, and "juice" present?

The next round works on the lowest-scoring tier. Functional issues always come first, because there is no point refining particle effects in a game that crashes on load. Each fix prompt tells the model to address only the identified issues, make the smallest possible change and leave unrelated sections alone.

The loop stops, or converges, when all section scores are above 75% and the functional score is above 50%. The loop runs up to five rounds by default. Many games converge in two or three.

## The convergence loop

The whole loop of generating, testing and critiquing is a single `for` loop:

```csharp
for (int i = 1; i <= maxIterations; i++)
{
    _statusText.Value = $"ITERATION {i}/{maxIterations} - GENERATING...";

    (html, resultSections) = await RunAgenticCodeGenAsync(...);
    _currentGameHtml.Value = html;

    var testResult = await RunLLMGuidedTestAsync(html, testGoal, plan, 6);
    var structured = await RunStructuredCritiqueAsync(plan, testResult, testResult.HarnessReport);

    if (CheckConvergence(structured))
    {
        _statusText.Value = "CONVERGED - ALL SECTIONS ABOVE THRESHOLD";
        break;
    }

    iterationTarget = SelectIterationTarget(structured);
}
```

Each round generates the game, tests it, critiques it and picks the weakest area for the next round. Each step updates the UI in real time, and the user can cancel the loop at any point.

## What you would normally need

Building an equivalent system on a traditional stack would mean assembling and maintaining several independent services: code generation backends with multiple AI provider integrations, a sandboxed execution environment for safely running generated code, headless browser infrastructure for testing, an AI evaluation pipeline for feeding screenshots back to vision models, a prompt management framework, file storage for game libraries and version histories, and job orchestration with progress tracking for loops that run for minutes at a time.

On the Ikon platform, all of this is one project. AI orchestration handles multi-model calls. The persistent app process runs the long generation loops. Reactive state keeps the UI updated live. The asset system handles storage. The headless browser runs in the same process.

## What we learned

The game generator is an AI tool that generates software, built on an AI platform. It uses several AI models in four roles (planner, coder, tester and critic) in a loop that tests the real, running game between rounds.

The lesson we took from it is about infrastructure. Going from "I can call an AI" to "I have a production system that generates, tests, critiques, and iterates on generated software" is almost entirely infrastructure work. The AI calls themselves are straightforward. The hard part is everything around them: running untrusted code safely, capturing runtime metrics, sending screenshots back to evaluator models, keeping state across a multi-step loop, pushing live progress to users and saving the results.

When the platform provides that infrastructure, what is left is the domain logic: how games should be structured, what makes a good critique, and when the loop should stop. Those are design questions rather than infrastructure questions, and they are the ones that decide how good the games are.
