# Not an API Wrapper — An AI Orchestration Engine

*Published 2026-03-19*

When someone starts building an AI application, they usually begin the same way: pick a provider, write some calls to its API, and read the responses. That works for a prototype.

Then the requirements grow. You want to try a different model. You need structured data back, not raw text. You want the AI to use tools, or to run the same task several ways and pick the best result. Soon you are building and maintaining a whole layer of code you never planned for.

Instead of wrapping each provider's API separately, Ikon.AI gives you one orchestration engine for 30+ providers, 165+ models, and 14 capability categories. It also includes multi-agent patterns that are ready for production use.

## What you can build

With Ikon.AI, one person can build AI applications that previously required a dedicated ML engineering team.

**Use many models in one app through one library.** A language learning app can use one model for conversation, another for pronunciation scoring, a third for generating lesson images, and a fourth for text-to-speech. All four go through the same library, with the same error handling and the same streaming behavior. Switching any of those models is a one-word change.

**Get production AI workflows without building the infrastructure.** The orchestration patterns described below cover task decomposition, parallel execution, evaluation from several perspectives, and iterative refinement. AI teams at large companies spend months building these from scratch. A solo creator gets them as building blocks that can be combined.

**Compare models without rebuilding.** Every pattern works with any provider, so you can run the same workflow against Claude, GPT-5, and Gemini and compare the results on quality, speed, and cost. To switch models in production, you change a single value.

**Small apps and large apps use the same library.** A haiku generator with AI-created illustrations is about 200 lines. A full language learning platform with 20+ voices, speech recognition, image generation, AI-driven conversation, and gamification uses the same library. The larger app combines more patterns and more capabilities, but it does not need more infrastructure.

## One interface for every model

At the simplest level, Ikon.AI lets you switch between supported models by changing a single value, for example from Claude to GPT-5 or Gemini 3 Pro. The interface stays the same. Tool definitions, structured output, streaming, and retry logic adapt to whichever provider you choose.

Here is what an AI call looks like. One call runs the model, streams progress, and returns a typed result:

```csharp
var result = await Emerge.Run<Analysis>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = "Analyze this dataset and identify trends";
}).FinalAsync();
```

Change `LLMModel.Claude46Sonnet` to `LLMModel.GPT5` and nothing else in your code changes. The library adapts the prompt format, tool definitions, and response parsing to the new provider.

The library knows what each model supports: streaming, parallel tool use, structured output, reasoning tokens, or image input. Your application doesn't need to know any of that, because Ikon.AI translates each call for the model you picked.

## Beyond text: 14 capability categories

Besides language models, Ikon.AI gives you one interface to:

- **Image generation** — DALL-E, FLUX, Gemini, Grok Imagine (23 models)
- **Video generation** — Veo, Kling, Seedance, Runway, Luma, and more (18 models)
- **Speech synthesis** — OpenAI TTS, ElevenLabs, Google Chirp (13 models)
- **Speech recognition** — Whisper, Deepgram, AssemblyAI (9 models)
- **Embeddings** — OpenAI, Cohere, Google, Jina, Voyage (11 models)
- **Reranking** — Cohere, Jina, Voyage (5 models)
- **OCR** — Azure Document Intelligence, Mistral OCR
- **Classification** — Content moderation with score-level transparency
- **Web search and scraping** — Google, Bing, Jina, Spider
- **Video enhancement** — Upscaling and frame interpolation
- **Sound effects** — Text-to-sound synthesis
- **File conversion** — Converting documents between formats

Every category works the same way. You describe what you want and get typed results back. Results stream where that makes sense, failed calls are retried automatically, and none of your code is tied to a specific provider. Once you have learned one category, you know how the others work.

## Emergence: orchestration patterns

Many libraries give access to many models. What Ikon.AI adds is **Emergence**, a library of 15+ orchestration patterns for production AI workflows, which you can combine with each other. They are the patterns that come up again and again when you build real applications.

### Run the same task several times and pick the best result

**BestOf** runs several independent attempts at a temperature you set, scores each result, and returns the highest-scoring one. You write the scoring function, and BestOf runs the attempts in parallel, collects the results, and ranks them. You don't write any task management or comparison code.

```csharp
var (best, _) = await Emerge.BestOf<Solution>(model, context, opt =>
{
    opt.Count = 5;
    opt.Temperature = 0.8f;
    opt.Score = (solution, trace) => solution.Confidence;
}).FinalAsync();
```

This code runs five attempts, scores each by its confidence, and returns the best one. You don't need any other code.

### Multi-stage refinement

**SolverCriticVerifier** runs a loop of drafting, critiquing, and verifying. A solver produces a first attempt. A critic reviews it and suggests improvements. A verifier checks the final result. Each stage can use a different model and different settings. Each stage automatically gets the output of the stages before it, so the critic sees the solver's output and the verifier sees both. You define the stages, and the framework passes the results from one stage to the next.

### Competing perspectives

**Debate** gives multiple agents different viewpoints and has them write competing proposals. A judge combines the best parts into one answer. Each participant works independently, so the proposals differ from each other instead of converging on the same idea.

### Parallel tasks with dependencies

**TaskGraph** breaks a complex goal into subtasks and works out which ones depend on each other. It runs independent tasks in parallel and starts a task only when the tasks it depends on are done. If a review step finds problems, TaskGraph can revise the plan while it is running.

### Other patterns

- **MapReduce** — splits work across parallel agents, then combines their results into one output
- **TreeOfThought** — explores several lines of reasoning as branches and drops the weak ones, for complex reasoning problems
- **PlanAndExecute** — generates a step-by-step plan, then carries it out with tools
- **Router** — sends each query to the model or sub-agent best suited to it
- **EnsembleMerge** — several different solvers answer independently, and a merger combines their answers into one
- **Refine** — improves a result over several rounds based on structured feedback
- **TestRefine** — an agentic loop that writes, tests, and fixes until the tests pass

The patterns can be combined. A Router can hand work to a SolverCriticVerifier that uses BestOf internally, and budget limits carry through to the nested patterns automatically. You spend your time on what the AI should do instead of on connecting the steps.

## Everything streams

Streaming is built into every pattern. Each one reports progress as it happens: text as it is generated word by word, each tool call, each move from one stage to the next, and each retry. You can build interfaces that show the user what the AI is doing at every moment, instead of a loading indicator followed by a wall of text.

## Structured output

Getting structured data out of an AI model usually means carefully writing prompt instructions, parsing responses, handling malformed output, and building retry logic for when things go wrong. With Ikon.AI, you define the shape of the data you want as a C# type. The library generates the right schema for the provider, parses the response, and validates the result, and you get back a typed object that is ready to use.
