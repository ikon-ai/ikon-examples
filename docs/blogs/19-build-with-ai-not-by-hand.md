# Build With AI, Not by Hand

*Published 2026-03-28*

You do not need to know how to code to build an Ikon app. You need two terminals and the ability to describe what you want.

## Start

Create your app:

```
ikon new Ikon.App.MyProject
```

This gives you a working project that compiles, runs and shows a page in your browser. The page is almost empty, but the whole Ikon runtime is available to it.

## Two terminals

Open two terminal windows side by side.

In the first terminal, start your app:

```
cd Ikon.App.MyProject
ikon run
```

Your app is now running at `localhost:5000`. Open it in a browser. You will see a blank page with the project name. The app is running and reloads when its files change.

In the second terminal, start Claude Code in the same folder:

```
cd Ikon.App.MyProject
claude
```

That is your whole development environment. The app runs in one terminal and Claude Code runs in the other.

## Build by describing

Now you talk to Claude. Tell it what you want.

"Add a text field where I can paste a URL, and a button that says Analyze."

Claude writes the code. The running app detects the change and reloads. The text field and button appear in your browser. You do not restart anything, run a build command or copy and paste code.

"When I click Analyze, scrape the page and give me a summary with three key takeaways."

Claude adds web scraping and a call to an AI model. The app reloads. You paste a URL, click the button, and the summary appears.

"Put the takeaways in cards. Add a copy button for the summary."

The app reloads with the takeaways in cards and a copy button.

"Remember the last five URLs I analyzed so I can go back to them."

The app reloads and a history sidebar appears. It already shows the analysis you just ran, because the app's state survived the reload.

Each round takes seconds: you describe a change, the app reloads and you see it.

## What happens when you change something

The app hot-reloads. This means the server restarts but your data stays. If you had items on screen, they are still there after the reload. If a user was interacting with the app, their session continues. You do not lose state every time Claude makes a change.

So each change improves the app that is already running, and you never start over.

## When something breaks

Errors show up in the first terminal where the app is running. You can tell Claude "there is an error in the terminal" and it will read the logs, figure out what went wrong, and fix it.

## What you can ask for

You can ask for anything the Ikon platform supports, for example:

- "Add voice input so I can speak instead of typing"
- "Generate an image based on the user's description"
- "Make it multiplayer so everyone sees the same thing"
- "Add a background task that checks for updates every hour"
- "Let the AI critique its own output and improve it"
- "Add text-to-speech so the app reads the summary aloud"

Each of these is a single request, not months of work. Claude knows the platform APIs and writes the code, then the app reloads and you see the result.

## Deploy

When you are happy with what you have built:

```
ikon link
ikon deploy
```

The app you built locally is the production app. There is no rewrite step. What worked on your machine works when deployed.

## Compared with the traditional way

The traditional way to build software is to learn a programming language, set up a development environment, write code, debug, iterate manually, deploy through a pipeline.

With Ikon, you open two terminals, describe what you want and watch it appear. The AI writes the code, the platform runs the app, and you decide what to build.
