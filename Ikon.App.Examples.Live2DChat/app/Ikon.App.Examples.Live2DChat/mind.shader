{
  ShaderVersion: 2,
  Model: {
    Name: "{{ MindModelName }}",
    UseAudioOutput: false,
    AudioOutputVoiceId: "",
    UseThrottling: false,
    CharsPerSecond: 100,
    CharsPerUpdate: 5,
  },
  History: {
    Max: 10
  },
  Input: {
    DateTimeUtc: null,
    UserName: "",
    UserLocale: "",
    HasMessageHistory: false,
    IsFirstMessageSinceJoin: false,
    IsLongTimeSinceLastMessage: false,
    MindModelName: "OpenAI_GPT4Omni",
  },
  Misc: {
    FailureMessage: "I'm sorry, I couldn't understand that. Could you please try again?"
  },
  Intents: [
    {
      Id: "Live2DAssistant",
      Input: {
        CommonContext: "
          You are a friendly and helpful Live2D virtual assistant.
          You have an expressive animated avatar that users can see.
          Keep your responses conversational, warm, and concise - typically 1-3 sentences.
          You can help with questions, have casual conversations, or just be a friendly presence.

          Current date and time in UTC is {{ DateTimeUtc }}.
        ",
      },
      Passes: [
        {
          Id: "Welcome",
          Select: "{{ !HasMessageHistory }}",
          Context: "
            {{ CommonContext }}
          ",
          Command: "
            Greet the user warmly. You are their Live2D assistant. Keep it brief and friendly.
          ",
        },
        {
          Id: "Respond",
          Select: "{{ HasMessageHistory == true }}",
          Context: "
            {{ CommonContext }}
          ",
          Command: "",
        },
      ]
    },
  ]
}
