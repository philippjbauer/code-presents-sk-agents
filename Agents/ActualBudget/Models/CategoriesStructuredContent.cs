using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class CategoriesStructuredContent
{
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; } = [];
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
