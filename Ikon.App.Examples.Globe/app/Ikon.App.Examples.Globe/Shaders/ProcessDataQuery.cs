using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.App.Examples.Globe.DataModels;

namespace Ikon.App.Examples.Globe.Shaders;

internal static class ProcessDataQueryShader
{
    public static async Task<DataQueryResult> GenerateAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            You are a data visualization assistant that interprets natural language queries about global geographic data.

            User query: "{query}"

            Interpret this query and determine:
            1. InterpretedQuery: A clear, standardized description of what data to show (e.g. "Population by country 2024")
            2. DisplayLabel: A concise label for the visualization (e.g. "World Population")
            3. DataSourceHint: One of: population, energy, gdp, temperature, emissions, trade, internet, custom
            4. SuggestedColor: A hex color that fits the data type (e.g. green for environment, blue for water/tech, orange for energy, purple for trade)

            Be creative in interpreting vague queries. For example:
            - "How hot is it?" -> Temperature data
            - "Who's online?" -> Internet usage
            - "Carbon footprint" -> CO2 emissions
            """;

        var (result, _) = await Emerge.Run<DataQueryResult>(
            LLMModel.Gpt41Mini,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.3f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
