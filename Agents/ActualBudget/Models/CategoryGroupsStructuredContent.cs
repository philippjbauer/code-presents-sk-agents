using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class CategoryGroupsStructuredContent
{
    [JsonPropertyName("groups")]
    public List<CategoryGroup> Groups { get; set; } = [];
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
