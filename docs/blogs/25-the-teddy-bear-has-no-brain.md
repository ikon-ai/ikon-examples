# The Teddy Bear Has No Brain

A four-year-old picks up a teddy bear and says, "Tell me a story about a dragon."

The bear answers. It has a warm, slightly goofy voice. "Okay! So there was this dragon named Pickles, and Pickles had a problem — every time he tried to breathe fire, he sneezed instead." The kid laughs. The bear continues, improvising, reacting to the giggles, weaving in the kid's name and the stuffed animals they mentioned yesterday.

Inside the bear: a Raspberry Pi Zero, a tiny microphone, and a small speaker. That is all. No AI chip. No language model. The bear cannot think. It cannot understand speech. It cannot make up stories. Everything it says comes from a server in the cloud.

The bear has two capabilities: record audio and play audio. The server decides when to use them. When the kid speaks, the server listens through the bear's microphone. The AI understands what the kid said, thinks about how to respond, generates a reply, turns it into speech, and sends the audio back. The bear plays it.

The bear is a puppet. The AI is the puppeteer.

## The server controls the conversation

The bear does not constantly stream audio to the cloud. It does not record everything and hope the server figures it out. Instead, the server orchestrates the conversation. It asks for audio when it is ready to listen. It detects when the kid has finished speaking. It takes a moment to think. Then it tells the bear what to say.

The server controls the flow because the server understands the flow. It knows when to pause for dramatic effect in a story. It knows when to ask a follow-up question. It knows when the kid has gone quiet and it is time to gently prompt: "What do you think Pickles did next?"

```csharp
while (!token.IsCancellationRequested)
{
    var audio = await FunctionRegistry.Instance.CallAsync<byte[]>(
        "RecordAudio", targetId: bearSessionId);

    var transcript = await SpeechRecognizer.TranscribeAsync(audio);

    var (reply, _) = await Emerge.Run<BearReply>(
        LLMModel.Claude46Sonnet, context, pass =>
    {
        pass.Command = $"You are a warm, playful teddy bear talking to a child. "
                     + $"The child said: '{transcript}'. "
                     + "Respond in character. Keep it short and fun.";
    }).FinalAsync();

    var speech = await TextToSpeech.GenerateAsync(reply.Text, voice: "warm-friendly");

    await FunctionRegistry.Instance.CallAsync(
        "PlayAudio", targetId: bearSessionId, args: [speech]);

    _conversationLog.Value = reply.Text;
}
```

Listen, understand, think, speak. The bear handles the microphone and the speaker. The server handles everything else.

In another room, a parent opens a browser and sees the conversation log updating in real time. The bear and the parent's browser are connected to the same server, the same session. The parent did not install an app. They opened a web page.

## The bear never changes

The stuffed animal you built on a weekend is finished hardware. You will probably never open it again. But the personality inside it — the one your kid talks to every night — keeps getting better.

This week, the bear tells simple stories. Next week, you tweak the prompt and it starts asking questions — "What color was the dragon?" — making stories interactive. A month later, you teach it to gently count along with the kid, turning bedtime into math practice. Eventually you swap in a better voice model and the bear sounds more natural.

Every improvement happens on the server. The bear's firmware — record audio, play audio — stays the same forever. The hardware never changes. The experience evolves.

This is the advantage of putting the brain on the server. The device is cheap, stable, and simple. The intelligence is flexible, upgradable, and powerful. You iterate on the interesting part — the personality, the teaching ability, the voice — without ever touching the toy.

A stuffed animal with a $10 computer inside. No AI. No smarts. And a kid who thinks it is their best friend.
