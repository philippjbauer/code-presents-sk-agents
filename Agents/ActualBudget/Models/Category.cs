using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = "";
    [JsonPropertyName("is_income")]
    public bool IsIncome { get; set; } = false;
}
