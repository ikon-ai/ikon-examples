using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class CreateImageGeneratorPromptShader
{
    public static async Task<ImagePrompt> GenerateAsync(
        string modelName,
        string reasoningEffort,
        string description,
        string context,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            You are an image prompt engineer creating prompts for AI image generation.

            Context: {context}
            Description: {description}

            Create a detailed image generation prompt that:
            1. Describes the scene with specific visual details
            2. Uses a clean, modern Nordic aesthetic with soft colors
            3. Is appropriate for a language learning app (friendly, inviting, educational)
            4. Includes lighting, composition, and atmosphere details
            5. Avoids text, faces with specific identities, or sensitive content

            The prompt should be 2-3 sentences long and paint a clear visual picture.
            """;

        var (result, _) = await Emerge.Run<ImagePrompt>(
            LLMModel.Gpt41Mini,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.7f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
