# The Teddy Bear Has No Brain

*Published 2026-03-31*

A four-year-old picks up a teddy bear and says, "Tell me a story about a dragon."

The bear answers. It has a warm, slightly goofy voice. "Okay! So there was this dragon named Pickles, and Pickles had a problem — every time he tried to breathe fire, he sneezed instead." The kid laughs. The bear goes on with the story. It makes the story up as it goes, reacts when the kid giggles, and includes the kid's name and the stuffed animals the kid mentioned yesterday.

Inside the bear are a Raspberry Pi Zero, a tiny microphone and a small speaker, and nothing else. There is no AI chip and no language model on the device, so the bear cannot recognize speech or make up stories by itself. Everything it says comes from a server in the cloud.

The bear can do two things: record audio and play audio. The server decides when it does each. When the kid speaks, the server asks the bear to record and receives the audio. The server transcribes the audio, has a language model write a reply, converts the reply to speech and sends the speech back. The bear plays it.

In that sense the bear is a puppet, and the AI on the server is the puppeteer.

## The server controls the conversation

The bear does not stream audio to the cloud all the time. Instead, the server runs the conversation. It asks the bear for audio when it is ready to listen, detects when the kid has finished speaking, generates a reply, and then sends the bear the audio to play.

Because the server decides when to listen and when to speak, it can pause for effect in a story, ask a follow-up question, or, when the kid has gone quiet, prompt gently: "What do you think Pickles did next?"

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

Each pass of the loop records audio on the bear, transcribes it, generates a reply with Emerge, converts the reply to speech and plays the speech on the bear. The bear handles only the microphone and the speaker, and the server does everything else.

In another room, a parent opens a browser and sees the conversation log update in real time. The bear and the parent's browser are connected to the same session on the same server. The parent did not install an app, only opened a web page.

## The bear's hardware never changes

Once you have built the bear over a weekend, the hardware is finished, and you will probably never open it again. The bear's personality, which your kid talks to every night, runs on the server, and you can keep improving it there.

This week, the bear tells simple stories. Next week, you change the prompt so that the bear asks questions during a story, such as "What color was the dragon?" A month later, you change it again so that the bear counts along with the kid at bedtime, as some math practice. Later still, you switch to a better voice model and the bear sounds more natural.

All of these changes are made on the server. The bear's firmware only records and plays audio, and it stays the same.

Running the AI on the server keeps the device cheap and simple, and its firmware never needs an update. You can change the bear's personality, what it teaches and its voice without touching the toy.

The bear is a stuffed animal with a $10 computer inside and no AI of its own, and the kid thinks it is their best friend.
