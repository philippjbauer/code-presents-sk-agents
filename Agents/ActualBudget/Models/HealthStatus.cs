using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class HealthStatus
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("initialized")]
    public bool Initialized { get; set; } = false;
}
