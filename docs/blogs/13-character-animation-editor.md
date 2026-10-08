# A Character Animation Editor for AI Video

*Published 2026-03-19*

This editor lets you define a character's expressions, such as happy, angry, and thinking, and upload a reference image for each. AI then generates a looping animation video for every expression and a transition video between every pair of them. The result is a complete set of character animations for an interactive character that can switch moods on command.

The whole application is under eleven hundred lines. That covers state management, prompt generation, image processing, video generation across six providers, bulk generation, cloud persistence, and the full editing interface. It is one project, with no separate backend, job queue, or image processing service.

## What you can do with it

The editor is built around two concepts: **states** and **transitions**.

A **state** is a character expression. You give it a name ("happy," "angry," "thinking"), upload a reference image, and the editor can generate a looping animation for it. The loop is a short video of the character in that expression with subtle idle motion like breathing, blinking, or a slight sway.

A **transition** connects two states. It is a video of the character moving from one expression to another, such as from angry to happy or from thinking to surprised.

When you add a new state, the editor adds a transition for every pair of states that does not have one yet. Videos you have already generated, such as "happy to angry," are kept. Three states need six transitions and four states need twelve, which is why the editor has bulk generation.

## Collaboration built in

Multiple people can work in the same editor simultaneously. One person uploads a reference image, and everyone else sees the thumbnail appear. Someone starts a bulk generation, and all viewers watch the progress indicators update in real time. This needs no setup, because the platform shares state with every viewer by default.

The generation continues even if everyone disconnects. You can start a sixteen-video bulk generation, close the browser, and come back later to find everything done and saved.

## AI writes the video prompts for you

The editor writes the prompts for the video generation models, so you do not need to know how.

When you press "Generate Loop" on a state, the editor sends the reference image and the state name to an AI model, which writes a short animation description for the video model. The AI also receives the character description from the settings, for example "a small cartoon fox with orange fur, standing on hind legs," so the framing stays consistent across all the animations.

Every generated prompt includes an instruction that the character must stay fully in frame with no camera movement or zoom. Video generation models tend to add cinematic camera moves unless told not to, and a loop video is useless if the character drifts off-screen.

Transition prompts work the same way but describe the movement between two expressions rather than idle motion.

## Padding images to the right aspect ratio

Video generation models expect specific aspect ratios, but character reference images come in any size. Cropping could cut off the character, so the editor pads the image to fit instead. The padding color is the average of the colors sampled from the four corners of the original image, so the padding blends into the background instead of leaving a hard border. This matters because a visible border in the reference image shows up in the generated video too.

## Six video generation models, one dropdown

The settings panel has a dropdown with six different video generation models. You pick one and generate. The same prompt, video length, aspect ratio, and input images go to whichever model you choose.

Video generation models differ a lot. Some keep the character more consistent, some produce smoother motion, and some are faster. You can generate the same transition with several models and compare the results by changing only the dropdown. On a traditional stack, six video generation providers would mean six separate integrations, six authentication setups, six response format handlers, and a layer to convert all their responses to one format. Here, switching provider is a dropdown selection.

## Bulk generation

For a four-state character, you need four loop videos and twelve transition videos. Generating them one at a time would be tedious. The "Generate All" buttons start all pending generations at once. Each generation has its own progress indicator, which changes from "Generating..." to complete when that video is done. If one fails, the rest keep going.

## Cloud persistence

The project saves to the cloud automatically every time you add a state, rename one, upload an image, or complete a generation. Images and videos are stored separately as files, so the saved project data stays small. When you reopen the editor, everything is exactly where you left it.

## State machine playback

The preview area plays the animations the way an interactive character would use them. Click "happy" while the character is in "angry," and the editor plays the angry-to-happy transition video, then switches to the happy loop. If there is no transition video yet, it jumps directly to the loop. If there is no loop video, it shows the static reference image. So the preview works at every stage, with whatever has been generated so far.

The state switching code finds the transition video, plays it, waits for it to finish, and then starts the loop:

```csharp
private async Task SwitchToState(string targetStateId)
{
    var transition = _transitions.Value.Find(t =>
        t.SourceStateId == currentStateId && t.TargetStateId == targetStateId);

    if (transition?.VideoUrl != null)
    {
        _playingVideoUrl.Value = transition.VideoUrl;
        await Task.Delay(TimeSpan.FromSeconds(_videoLength.Value));
    }

    if (targetState.LoopVideoUrl != null)
    {
        _playingLoop.Value = true;
        _playingVideoUrl.Value = targetState.LoopVideoUrl;
    }
}
```

Setting the reactive values updates the video player for every connected viewer.

## What would this normally require?

Building this as a standalone application would mean assembling several independent systems: a visual editor for creating states and transitions, an image processing service for padding images to the right aspect ratios, six separate video generation integrations, an AI pipeline for writing prompts, a job queue for managing long-running video generations, cloud storage for assets, a database for project state, and real-time infrastructure for pushing progress updates to connected viewers.

On Ikon, the platform provides multi-model AI, reactive state, cloud persistence, asset storage, and in-process image manipulation. What is left to write is the state machine logic, the prompt design, and the aspect ratio math, which is the part specific to giving a character expressions and moving it between them.
