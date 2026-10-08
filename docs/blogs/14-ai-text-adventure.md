# Building an AI Text Adventure

*Published 2026-03-19*

We built a scored narrative game where you investigate surreal crime scenes, interrogate witnesses, and try to deduce hidden universal laws. Every playthrough is different, because the AI generates the accusations, scenes, witnesses, and images as you play. The whole game is under a thousand lines.

This post shows how the game combines several kinds of AI calls into one experience, and why the code stays small.

## What it feels like to play

A cosmic judge puts you on trial for abstract, metaphysical crimes, such as "The Hoarding of Silence" or "The Weaponization of Nostalgia." Each crime was committed against a hidden universal law that you do not know. You are taken to a surreal scene where every object, witness, and detail is a physical metaphor for that law. Your job is to figure out what the law is.

You get five actions per round to investigate. You can examine objects, ask witnesses questions, reflect on what you have seen, look around for new details, or propose your theory of the hidden law. A trial has three rounds, each with a different crime and scene, and the round scores are added up at the end.

## The scene changes as you get closer to the answer

The scene image, a generated illustration of the surreal location, changes as you get closer to the truth.

At the start of each round the scene is dark and foggy, and details are barely visible. As you ask questions and examine objects, the AI scores how close your line of inquiry is to the hidden law. When the score crosses a threshold, the image is generated again, with less fog, amber light breaking through, and sharper details. When you are close to the truth, the scene becomes bright and golden, with every element clearly visible.

One proximity value, which measures how close you are to the truth, sets the atmosphere:

```csharp
var atmosphereSuffix = proximity switch
{
    < 0.3f => ", dark and obscured atmosphere, thick fog, deep shadows, mysterious and foreboding, barely visible details",
    < 0.6f => ", partially illuminated, some fog lifting, amber light breaking through, details becoming clearer",
    _ => ", radiant and illuminated, crystal clear details, golden light, truth revealed in every element"
};
```

Below 0.3 the scene is dark and foggy, between 0.3 and 0.6 amber light breaks through, and above 0.6 it turns golden. The matching phrase is added to the end of the image generation prompt.

So the game rewards good reasoning visually. You can tell you are getting warmer from how the scene looks, not from a score counter.

## Spectators watch the investigation

Because the game runs as a persistent process with shared state, multiple people can connect and watch the same trial. One person plays and the others watch. Everyone sees the same transcript and the same scene images, updated in real time as the investigation goes on.

When the player examines a witness and the fog lifts, every connected viewer sees the new image appear. Letting a group watch the same game live would normally need dedicated real-time infrastructure. In this game it needs no extra code.

## How each round works

At the start of each round, the AI generates a new set of content: an abstract crime, a hidden universal law that was violated, a surreal scene where every element is a physical metaphor for the law, three witnesses who are part of that metaphor, and a list of objects you can examine. The generated round also includes the hidden connections between the scene elements and the law, which you never see and have to work out yourself.

Earlier rounds are fed back to the AI so it avoids repeating themes across the trial.

## Five commands

During an investigation there are five commands. Each one gets a different kind of response from the AI:

- **Examine** -- describes observable clues about an object or element in the scene
- **Ask** -- a witness responds in character, dropping hints through their perspective
- **Reflect** -- philosophical musing that connects scene elements you have encountered
- **Look** -- reveals new details in the environment you had not noticed before
- **Propose** -- put forward your theory of the hidden law

Each response also includes a proximity score, the AI's estimate of how close you are to the truth, which sets the scene atmosphere described above.

## Judgment with partial credit

When you propose a theory (or run out of actions), the AI judges your proposal against the actual hidden law. Scoring is not pass/fail. A player who identifies the right general area but misses the specific principle can get partial credit. The game adds up the results of all three rounds, and the text of the final verdict depends on your total score.

## What would this normally require?

Building this as a standalone application would require several independent systems: an AI orchestration layer for managing multiple types of AI calls (generating accusations, narrating investigations, judging proposals), an image generation service that responds to game state, a state management system tracking phase, round, actions, proximity, transcript, and results across a multi-step session, real-time infrastructure for pushing updates to spectators, and a frontend application with components for the transcript, image display, proximity indicator, input field, and verdict screen.

On the Ikon platform, all of these are methods on a single class. They run in the same process and share the same state and lifecycle. Going from "the player examined the broken clock" to "the scene image is generated again with amber light breaking through fog" takes one method call and one value update.

## Several kinds of AI in one file

The game uses many different kinds of AI call in a small amount of code:

- structured generation, which creates accusations with specific fields and relationships
- contextual generation, which writes investigation responses that follow the full transcript and adjust to how close you are to the answer
- evaluation, which judges your proposal against a hidden answer and gives partial credit
- image generation, which changes the scene to match the game state

In a traditional architecture, each of these would likely be a separate service or module with its own integration. Here the accusation generator, the investigation narrator, the judge, and the image generator are methods in the same file, in the same process and with the same state. With no integration boundaries between these AI calls, the game feels like one piece rather than separate parts stitched together.

The complete game, with generated images, five command types, an atmosphere that follows your proximity score, multiple rounds, and a final verdict, is under a thousand lines. The code is not compressed to get there. The platform already provides the infrastructure that would normally take most of the effort.
