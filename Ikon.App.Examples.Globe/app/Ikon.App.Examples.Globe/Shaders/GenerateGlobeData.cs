using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.App.Examples.Globe.DataModels;

namespace Ikon.App.Examples.Globe.Shaders;

internal static class GenerateGlobeDataShader
{
    public static async Task<GlobeDataSet> GenerateAsync(
        string interpretedQuery,
        string dataSourceHint,
        string suggestedColor,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            You are a geographic data generator for a 3D globe visualization.

            Generate realistic data points for: "{interpretedQuery}"
            Data type hint: {dataSourceHint}

            Requirements:
            1. Generate 50-100 data points representing major cities, countries, or regions
            2. Use accurate real-world latitude/longitude coordinates
            3. Magnitude values should be normalized 0.0 to 1.0 where 1.0 is the highest value
            4. Include geographic diversity - don't cluster all points in one region
            5. Make the data realistic and proportional to real-world statistics

            For the response:
            - SeriesName: A descriptive name for the data series
            - Color: Use "{suggestedColor}" as the base color
            - Points: Array of data points with Latitude, Longitude, Magnitude, and optional Label

            Key coordinates for reference:
            - New York: 40.7, -74.0
            - London: 51.5, -0.1
            - Tokyo: 35.7, 139.7
            - Sydney: -33.9, 151.2
            - Mumbai: 19.1, 72.9
            - Sao Paulo: -23.6, -46.6
            - Lagos: 6.5, 3.4
            - Moscow: 55.8, 37.6
            - Beijing: 39.9, 116.4
            - Cairo: 30.0, 31.2

            Include points across all continents with realistic relative magnitudes based on the data type.
            For population, more populated areas should have higher magnitudes.
            For energy/emissions, industrial nations should have higher values.
            For internet, developed regions should show higher connectivity.
            """;

        var (result, _) = await Emerge.Run<GlobeDataSet>(
            LLMModel.Gpt41,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.7f;
                pass.MaxOutputTokens = 8000;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
