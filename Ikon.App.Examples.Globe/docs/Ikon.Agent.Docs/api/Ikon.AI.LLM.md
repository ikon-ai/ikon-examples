namespace Ikon.AI.LLM
  enum CustomLLMApi
    OpenAICompletions
    OpenAIResponses
    Anthropic
    Google
    Cohere
  // Capability flags default to what a typical self-hosted OpenAI-compatible model supports; enable more (e.g. SupportsJsonSchema) when the endpoint provides them.
  sealed class CustomLLMModel : CustomModel
    ctor()
    required CustomLLMApi Api { get; init; }
    required int ContextWindowSize { get; init; }
    // Leave at 0 when the endpoint has no such cap: a request asking for more than the model can produce is capped at this value instead of being sent as-is, and 0 means "send the caller's value".
    int MaxOutputTokens { get; init; }
    bool SupportsCaching { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsStrictJsonSchema { get; init; }
    bool SupportsSystemMessages { get; init; }
    bool SupportsTemperature { get; init; }
  sealed class LLMCapabilities
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    int MaxOutputTokens { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // Distinct from SupportsInputImages: a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) accepts images in messages but not inside tool_result blocks.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsZeroDataRetention { get; init; }
    bool UsesInlineReasoning { get; init; }
  class LLMMaxOutputTokensException : NonRetryableLLMException
  enum LLMModel
    Gpt4OmniMini
    Gpt41
    Gpt41Mini
    Gpt5
    Gpt5Mini
    Gpt5Nano
    Gpt51
    Gpt52
    Gpt5Pro
    Gpt52Pro
    Gpt53Codex
    Gpt54
    Gpt54Mini
    Gpt54Nano
    Gpt54Pro
    Gpt55
    Gpt55Pro
    Gpt56Sol
    Gpt56Terra
    Gpt56Luna
    O3
    O3Pro
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Claude5Opus
    Claude5Fable
    Claude51Fable
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Gemini35FlashLite
    Gemini36Flash
    Gemini37Flash
    Gemini38Flash
    Grok43
    Grok45
    Grok46
    GrokBuild01
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    Ministral14B
    Ministral8B
    Ministral3B
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandRPlus
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    MuseSpark13
    KimiK25
    KimiK26
    KimiK27Code
    KimiK3
    Qwen36
    Qwen37
    Qwen37Max
    Qwen38Max
    Qwen37Flash
    Qwen3827B
    GptOss120B
    Glm5
    Glm51
    Glm52
    Glm53
    Glm53Flash
    Glm5VTurbo
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    DeepSeekV4FlashVision
    Seed21Turbo
    Seed20Code
    Seed20Lite
    Seed20Mini
    MiMoV25
    MiMoV25Pro
    Step37Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // In tokens. Returns 0 when the model can't be resolved — treat 0 as "unknown" and skip utilization math rather than dividing by zero.
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
    // In tokens. Returns 0 when the limit is unknown (unresolvable model, or a provider that publishes none) — treat 0 as "no cap known", not as a zero budget.
    static int MaxOutputTokens(this LLMModel model)
  class ModelOutputException : RetryableLLMException
  class NonRetryableLLMException : NonRetryableAIException
  class RetryableLLMException : RetryableAIException
