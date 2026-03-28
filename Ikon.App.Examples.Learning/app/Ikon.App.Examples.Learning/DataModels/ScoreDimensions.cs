using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class ScoreDimensions
{
    [Description("A score given for Task Fulfillment and relevance to the question. A score out of 3.")]
    public float TaskFulfillment { get; set; } = 0.0f;

    [Description("A score given for Organization and Structure. A score out of 3.")]
    public float OrganizationAndStructure { get; set; } = 0.0f;

    [Description("A score given for Linguistic Resource and Accuracy. A score out of 3.")]
    public float LinguisticResourceAndAccuracy { get; set; } = 0.0f;
}
