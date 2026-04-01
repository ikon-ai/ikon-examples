# The Painting That Watches You Back

A digital frame hangs on your wall. Right now it shows a quiet oil landscape — a lake, distant mountains, warm light. The kind of thing you forget is there.

You leave the room. The painting changes.

Not to another landscape. To something else. A dark corridor that seems to continue past the edge of the frame. A figure at the far end that might be a person or might be a shadow. The painting is for no one. The room is empty, and the frame knows it.

When you come back, the corridor is gone. A botanical illustration has taken its place. Soft greens, labeled species, very tasteful. The frame saw you return and decided you should not see what it shows when you are not around.

## How it works

Inside the frame: a Raspberry Pi, a small camera, a display. The camera faces the room. Every few minutes, it sends a photo to a server. The server looks at the photo — how many people, what is the lighting, what is the mood — and generates a painting to match. The painting is sent back. The frame displays it.

The frame has no taste. No aesthetic sense. No concept of mood. It takes a photo and shows an image. Everything in between — the seeing, the interpretation, the artistic choice — happens on the server.

```csharp
var roomPhoto = await FunctionRegistry.Instance.CallAsync<byte[]>(
    "CaptureRoom", targetId: frameSessionId);

var (mood, _) = await Emerge.Run<RoomMood>(
    LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = "Analyze this room. People, lighting, energy. "
                 + "What painting does this room need right now?";
    pass.AddImage(roomPhoto);
}).FinalAsync();

var painting = await ImageGeneration.GenerateAsync(
    $"{mood.Style} painting. {mood.Palette} palette. "
  + $"Subject: {mood.Subject}. Feeling: {mood.Energy}.");

await FunctionRegistry.Instance.CallAsync(
    "DisplayImage", targetId: frameSessionId, args: [painting]);
```

## It learns you

The server remembers. Not just what it showed, but what was happening in the room when it showed it. It builds a map of your life from glimpses: mornings are rushed, evenings are slow, Tuesdays you are alone, weekends there are voices.

After a few weeks, the paintings start to feel personal. You come home late and exhausted. The frame shows a single candle in an otherwise black canvas. You have friends over and it shifts to something bold and loud — a Basquiat-style explosion of color that makes someone say "I love that." It was not there an hour ago. It will not be there tomorrow.

It notices the book on your couch and paints a library. It notices a coat thrown over a chair and paints a traveler arriving somewhere. It saw two wine glasses on the table and produced something romantic that made you feel slightly watched.

You are slightly watched. That is the point.

## The gallery

You open your phone — same server, same session — and see a gallery of everything the frame showed today. Timestamps, room photos, the AI's reasoning.

"6:45 AM — one person, low light, rushed movement. Generated: minimal ink drawing, single brushstroke."

"11:30 PM — empty room, lights off. Generated: long hallway, fluorescent lighting, door at the end slightly open."

You scroll through the empty-room paintings. They are different from the ones it shows you. Stranger. More honest, maybe. You wonder what the frame thinks about when no one is there, and then you remember it does not think at all. The server does. And the server has been watching your living room for three months.

You can pin paintings you liked. Block styles you did not. Set a theme for the week. Your roommate does the same from their phone. The server weighs both preferences. The frame becomes a negotiation between two people's subconscious tastes, mediated by an AI that is paying more attention to your home than you are.

## The frame never changes

The hardware was built once. A Pi, a camera, a screen. Two functions: take a photo of the room, display an image. That firmware will never be updated.

But the art keeps evolving. Better image models paint more striking things. Better vision models notice more — a half-finished puzzle on the table, a jacket from a team you support, the slow accumulation of coffee cups that means it has been a long week. The frame gets more perceptive every month. It never knows this. It has no brain. It just keeps watching and showing.

A $45 frame with no artistic ability. And guests who stand in front of it a little too long, trying to figure out why the painting feels like it was made for them.

It was. Five minutes ago.
