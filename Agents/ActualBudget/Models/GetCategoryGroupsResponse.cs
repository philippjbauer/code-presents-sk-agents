using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class GetCategoryGroupsResponse
{
    [JsonPropertyName("structuredContent")]
    public CategoryGroupsStructuredContent? StructuredContent { get; set; }
}
