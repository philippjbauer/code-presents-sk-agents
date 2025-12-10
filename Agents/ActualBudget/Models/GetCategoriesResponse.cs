using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class GetCategoriesResponse
{
    [JsonPropertyName("structuredContent")]
    public CategoriesStructuredContent? StructuredContent { get; set; }
}
