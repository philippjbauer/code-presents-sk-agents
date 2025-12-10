using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class HealthStatusResponse
{
    [JsonPropertyName("structuredContent")]
    public HealthStatus? StructuredContent { get; set; }
}
