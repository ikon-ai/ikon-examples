using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class Scenario
{
    [Description("Unique identifier for the scenario")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Description("Display name for the scenario")]
    public string Name { get; set; } = string.Empty;

    [Description("Description of the scenario setting and context")]
    public string Description { get; set; } = string.Empty;

    [Description("The theme/category this scenario belongs to")]
    public string ThemeId { get; set; } = string.Empty;
}

public class GeneratedScenarios
{
    [Description("List of generated scenarios for the theme")]
    public List<Scenario> Scenarios { get; set; } = [];
}
