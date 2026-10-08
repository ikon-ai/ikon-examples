# The Painting That Watches You Back

*Published 2026-03-31*

A digital frame hangs on your wall. Right now it shows a quiet oil landscape of a lake and distant mountains in warm light, the kind of picture you forget is there.

You leave the room. The painting changes.

The new painting is not another landscape. It shows a dark corridor that seems to continue past the edge of the frame, with a figure at the far end that might be a person or might be a shadow. Nobody is there to see it. The room is empty, and the server knows this from the latest photo taken by the frame's camera.

When you come back, the corridor is gone and a botanical illustration is in its place, with soft greens and labeled species, very tasteful. The next photo showed you back in the room, and the server chose not to show you what it paints when you are not around.

## How it works

Inside the frame are a Raspberry Pi, a small camera and a display. The camera faces the room. Every few minutes, the frame sends a photo to a server. On the server, a language model looks at the photo and works out how many people are in the room, what the lighting is like and what the mood is. An image model then generates a painting to match. The server sends the painting back, and the frame displays it.

The frame itself makes no artistic decisions. It takes a photo and shows an image. Reading the photo, deciding on a mood and choosing what to paint all happen on the server.

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

## It learns your routines

The server stores each painting together with what was happening in the room when it was shown. From these photos it builds up a picture of your life: mornings are rushed, evenings are slow, on Tuesdays you are alone, and at weekends there are voices.

After a few weeks, the paintings start to feel personal. When you come home late and exhausted, the frame shows a single candle on an otherwise black canvas. When you have friends over, it switches to something bold and loud, a Basquiat-style painting full of color that makes someone say "I love that." The painting was not there an hour ago, and it will not be there tomorrow.

When the camera picks up a book on your couch, the server paints a library. A coat thrown over a chair becomes a traveler arriving somewhere. Two wine glasses on the table once led to something romantic that made you feel slightly watched.

You are slightly watched, because a camera photographs the room every few minutes. That is how the frame works.

## The gallery

On your phone you open the same app, connected to the same session on the server, and see a gallery of everything the frame showed today. Each entry has a timestamp, the room photo and the AI's reasoning.

"6:45 AM — one person, low light, rushed movement. Generated: minimal ink drawing, single brushstroke."

"11:30 PM — empty room, lights off. Generated: long hallway, fluorescent lighting, door at the end slightly open."

You scroll through the paintings made for the empty room. They are stranger than the ones you see, and maybe more honest. You wonder what the frame thinks about when no one is there, and then you remember that it does not think at all. The models on the server make every choice, and the server has been looking at photos of your living room for three months.

From the gallery you can pin paintings you liked, block styles you did not like, and set a theme for the week. Your roommate can do the same from their phone, and the server takes both sets of preferences into account when it chooses what to paint. The frame ends up showing a compromise between two people's tastes, worked out by an AI that pays more attention to your home than you do.

## The frame's hardware never changes

You build the hardware once: a Pi, a camera and a screen. Its firmware has two functions, one to take a photo of the room and one to display an image. That firmware will never be updated.

The paintings keep improving anyway, because the models on the server improve. Newer image models paint more striking pictures. Newer vision models notice more details, such as a half-finished puzzle on the table, a jacket from a team you support, or the coffee cups that pile up when it has been a long week. The paintings get more perceptive every month, while the frame itself keeps doing the same two things: taking photos and showing images.

The frame cost $45 and has no artistic ability of its own. Guests still stand in front of it a little too long, trying to work out why the painting feels like it was made for them. It was made for them. The server painted it five minutes ago, from a photo of the room they are standing in.
