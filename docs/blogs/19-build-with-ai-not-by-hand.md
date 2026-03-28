# Build With AI, Not by Hand

You do not need to know how to code to build an Ikon app. You need two terminals and the ability to describe what you want.

## Start

Create your app:

```
ikon app new Ikon.App.MyProject
```

This gives you a working project. It compiles, it runs, it shows up in a browser. A blank canvas with the entire Ikon runtime behind it.

## Two terminals

Open two terminal windows side by side.

In the first terminal, start your app:

```
cd Ikon.App.MyProject
ikon app run
```

Your app is now running at `localhost:5000`. Open it in a browser. You will see a blank page with the project name. The app is live and watching for changes.

In the second terminal, start Claude Code in the same folder:

```
cd Ikon.App.MyProject
claude
```

That is your entire development environment. The app is running in one terminal. Your AI collaborator is ready in the other.

## Build by describing

Now you talk to Claude. Tell it what you want.

"Add a text field where I can paste a URL, and a button that says Analyze."

Claude writes the code. The running app detects the change and reloads. The text field and button appear in your browser. No restart. No build command. No copy-pasting code.

"When I click Analyze, scrape the page and give me a summary with three key takeaways."

Claude wires up web scraping and an AI model. The app reloads. You paste a URL, click the button, and the summary appears.

"Put the takeaways in cards. Add a copy button for the summary."

Reload. Cards. Copy button. Done.

"Remember the last five URLs I analyzed so I can go back to them."

Reload. A history sidebar appears -- and it already shows the analysis you just ran, because the app's state survived the reload.

Each round takes seconds. Describe, reload, see.

## What happens when you change something

The app hot-reloads. This means the server restarts but your data stays. If you had items on screen, they are still there after the reload. If a user was interacting with the app, their session continues. You do not lose state every time Claude makes a change.

This is what makes the loop feel like sculpting rather than rebuilding. You are always refining what is already there, not starting over.

## When something breaks

Errors show up in the first terminal where the app is running. You can tell Claude "there is an error in the terminal" and it will read the logs, figure out what went wrong, and fix it. The loop continues.

## What you can ask for

Anything the Ikon platform supports. Which is a lot.

- "Add voice input so I can speak instead of typing"
- "Generate an image based on the user's description"
- "Make it multiplayer so everyone sees the same thing"
- "Add a background task that checks for updates every hour"
- "Let the AI critique its own output and improve it"
- "Add text-to-speech so the app reads the summary aloud"

These are not months-long features. They are single requests. Claude knows the platform APIs and wires them up. The app reloads. You see the result.

## Deploy

When you are happy with what you have built:

```
ikon app config
ikon app deploy
```

The app you built locally is the production app. There is no rewrite step. What worked on your machine works when deployed.

## The point

The traditional way to build software: learn a programming language, set up a development environment, write code, debug, iterate manually, deploy through a pipeline.

The Ikon way: open two terminals, describe what you want, watch it appear. The AI writes the code. The platform handles the runtime. You handle the vision.

Two terminals. One runs the app. One talks to the AI. Everything else is a conversation.
