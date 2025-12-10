using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class Account
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("offbudget")]
    public bool OffBudget { get; set; } = false;
    [JsonPropertyName("closed")]
    public bool Closed { get; set; } = false;
}
