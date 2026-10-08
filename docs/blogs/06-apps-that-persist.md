# Apps That Persist

*Published 2026-03-19*

Close your browser and come back tomorrow morning. Your AI research assistant has worked through the night, analyzing documents, monitoring sources and building a digest of what happened while you were away. When you open the app, the results are there, the conversation continues from where you left off, and the analysis has moved on since you left. An Ikon app keeps running after you close the browser.

## What you can build

**AI agents that work while you sleep.** A research assistant that monitors sources and builds a daily briefing runs as a background task within the application itself. It has direct access to the app's state. When it finds something, the results appear the next time any user opens the app.

**Apps that keep their context over time.** The app keeps its context in memory, so each interaction builds on all the ones before it. The AI remembers your project instead of rebuilding its context from saved notes every time you return.

**Code changes that keep your place.** During development, changing your code does not restart the app, and all state is kept across code changes. If you are in the middle of a long AI workflow and change the interface, the workflow continues from where it was when you save.

**Long-running AI workflows without extra infrastructure.** A document analysis that takes 30 minutes, a research task that runs for hours or a data pipeline that runs overnight is a task in the app that runs until it is done. You don't set up job queues, worker processes or orchestration for it.

**Apps that keep working when nobody is using them.** With background processing, live state and persistence together, an app can do work between your visits instead of only answering requests. For example, an AI character can follow the news and have opinions about what happened while you were away, or an AI in a shared workspace can organize and connect ideas between visits.

## Why most apps cannot do this

In a typical web architecture, the server is stateless by design. A request arrives, the server processes it, sends a response, and forgets everything. State is kept in databases, caches, and session stores. The server itself is short-lived, designed to be killed and replaced at any moment.

This model works well for traditional applications but poorly for AI applications.

AI workflows carry state. A conversation has context that spans turns. An analysis pipeline has intermediate results. A multi-agent orchestration has tasks in various stages of completion. Saving all of this to a database and rebuilding it on every request adds latency, complexity and new ways to fail, and none of that helps with the problem you are trying to solve.

## How Ikon applications work differently

An Ikon application is a long-lived process that holds its state. It keeps running when no users are connected.

**State is kept in the application.** The app holds its state in memory: conversations, analysis results, queued tasks and accumulated context. When a user disconnects and reconnects later, the application is still running and everything is where they left it. The app does not need to load state from a database, rebuild it, or start up cold.

**Background work is built in.** In traditional architectures, running something in the background requires a job queue, a message broker, worker processes, and orchestration to tie them together. In Ikon, background work is a task that runs within the application process. It has direct access to the app's live state. When it updates something, every connected user sees the change immediately. If no users are connected, the task keeps running anyway.

This background AI task gathers sources, analyzes each one, and updates the interface as each result comes in:

```csharp
app.BackgroundWork.Start("deep-analysis", async ct =>
{
    var sources = await GatherSources(query);

    foreach (var source in sources)
    {
        var analysis = await Emerge.Run<SourceAnalysis>(
            LLMModel.Claude46Sonnet, context, pass =>
            {
                pass.Command = $"Analyze this source: {source.Content}";
            }).FinalAsync();

        // UI updates in real-time as each source is analyzed
        _currentAnalysis.Value = analysis.Result.Summary;
    }
});
```

When someone reconnects, they see how far the analysis has got.

**Scheduled and recurring work runs inside the app.** For structured background processing such as daily digests, periodic data pulls and recurring analysis, the platform provides a pipeline system with scheduling, data transformation, parallel processing, and error handling. All of it runs within the persistent application process, not in a separate service.

## State survives hot reloads

Keeping state across code changes helps most during development. In most frameworks, changing your code means restarting the server and losing all state. You are in the middle of testing a multi-turn AI conversation, you notice a UI issue, you fix it, and now you have to recreate the entire conversation from scratch.

Ikon's hot reload keeps state. When you change your code, the platform saves the current state, compiles the new code, creates a new instance of the application, restores all state, and resumes. An AI conversation in progress continues, and background tasks continue from where they were.

This changes how you develop AI applications, because you can change behavior and presentation while a long workflow is running, without restarting the workflow.

## What persistence makes possible

**Long-running AI workflows.** An AI research agent that takes 30 minutes to analyze a corpus of documents runs as one continuous process and updates the interface as it goes. You can close your browser, come back later, and see the results.

**Conversation context stays in memory.** A multi-turn conversation and its context are kept in the application's memory. The app does not save the conversation to a database and load it back on the next request, because the context is already in memory for the next turn.

**Collaborative state without extra infrastructure.** Multiple users connect to the same running application. They see each other's contributions and watch the AI work in real time. You don't configure pub/sub or build a real-time sync layer, because state is shared by default.

**Context that grows over time.** Because context builds up in memory, the application gets more useful the longer it runs. The AI knows more about what it is working on, keeps a longer history, and can find more relevant connections.

## The infrastructure behind it

Keeping applications running needs infrastructure underneath. Each application runs in its own isolated container. The platform manages the lifecycle: it starts apps on demand, monitors their health, and shuts them down gracefully when needed. For state that must outlive the application process, and not only survive hot reloads, the platform provides persistent storage for files, structured data, and database connections. You decide what to store long-term, and the architecture does not force you to store everything.

## A different kind of application

Moving from stateless request handlers to long-lived processes that hold state changes what applications can do. An app no longer only responds to requests. An Ikon application can keep working on a problem after you close the tab.
